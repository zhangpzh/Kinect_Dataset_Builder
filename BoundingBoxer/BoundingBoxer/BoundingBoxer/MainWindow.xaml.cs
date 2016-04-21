using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace BoundingBoxer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Video中的image路径
        /// </summary>
        private static List<String> m_imageFiles = null;

        /// <summary>
        /// people 子文件夹下 person_xx.txt 的路径, (每个txt用来记录每个人每次出现时对应的帧号和框)
        /// </summary>
        private static List<String> m_peopleTxtFiles = null;

        /// <summary>
        /// people 子文件夹的路径
        /// </summary>
        private static String m_peopleSubDirectPath = String.Empty;
        /// <summary>
        /// 本Video的校正进度相关
        /// </summary>
        private static String m_rectifyTxtPath = String.Empty;
        
        /// <summary>
        /// image路径名中, image帧号出现的第一个index, 比如 'xxx/001.jpg', 则 该值为 4 (这个值对于一个 video 中的 image 而言都是一样的)
        /// </summary>
        private static int m_imageNumberBeginIndex;
        /// <summary>
        /// Video的bounding boxes 文件路径
        /// </summary>
        private static String m_boundingBoxesTxtPath = String.Empty;
        /// <summary>
        /// image 帧号 (string)
        /// </summary>
        private static String m_imageNumberInString = String.Empty;
        /// <summary>
        /// image 索引 (int, begin from zero)
        /// </summary>
        private static int m_imageNumberInIndex;
        /// <summary>
        /// image 总数
        /// </summary>
        private static int m_imageCount;
        /// <summary>
        /// Store all bounding boxes of current video
        /// </summary>
        private static List<List<BoundingBox>> m_AllBoxesInVideo = null;

        /// <summary>
        /// Store all borders of current image for control
        /// </summary>
        private static List<Border> m_AllBordersOfCurrentImage = new List<Border>();

        /// <summary>
        /// Store all text blocks of current image for control
        /// </summary>
        private static List<TextBlock> m_AllTextBlocksOfCurrentImage = new List<TextBlock>();

        /// <summary>
        /// 画布
        /// </summary>
        private static ImageBrush m_canvasBrush = new ImageBrush();
        /// <summary>
        /// 展示比, 实际图像的大小是 1920x1080, 而此中 Canvas 的大小是 640x360
        /// </summary>
        private const double m_actualToDisplayRate = 360.0 / 1080;

        /// <summary>
        /// 是否仅确定了矩形的 topLeft 点
        /// </summary>
        private static bool m_isFirstCornerOfRectConfirm = false;
        /// <summary>
        /// total id count
        /// </summary>
        private const int m_totalIdCnt = 10;
        /// <summary>
        /// 人物 id
        /// </summary>
        private static bool[] m_idPool = new bool[m_totalIdCnt];        //默认只提供1-10个id
        /// <summary>
        /// 当前的矩形的 id (用户输入)
        /// </summary>
        private static int m_currentSelectedId;

        /// <summary>
        /// 矩形的 topLeft 点 和 bottomRight 点
        /// </summary>
        private static Point m_pre_pt;
        private static Point m_nxt_pt;


        public MainWindow()
        {
            InitializeComponent();

        }
        /// <summary>
        /// Initialize m_AllBoxesInVideo with all information in BoundingBoxes.txt 
        /// </summary>
        private void Initm_AllBoxesInVideo()
        {
            //为每一帧申请一个 bounding boxes 列表
            m_AllBoxesInVideo = new List<List<BoundingBox>>();
            for (int i = 0; i < m_imageCount; ++i)
            {
                m_AllBoxesInVideo.Add(new List<BoundingBox>());
            }

            using (StreamReader boxesReader = new StreamReader(m_boundingBoxesTxtPath, Encoding.UTF8))
            {
                int frameNumber;
                int person_id;
                double topLeftPointX, topLeftPointY, width, height;
                String line;
                while ((line = boxesReader.ReadLine()) != null)
                {
                    //Get information within a record
                    line = line.Replace('(', '\t');
                    line = line.Replace(')', '\t');
                    String[] entries = line.Split(',');
                    int.TryParse(entries[0].Substring(entries[0].LastIndexOf(':') + 1), out frameNumber);
                    int.TryParse(entries[1].Substring(entries[1].LastIndexOf(':') + 1), out person_id);
                    double.TryParse(entries[2].Substring(entries[2].LastIndexOf(':') + 1), out topLeftPointX);
                    double.TryParse(entries[3], out topLeftPointY);
                    double.TryParse(entries[4].Substring(entries[4].LastIndexOf(':') + 1), out width);
                    double.TryParse(entries[5].Substring(entries[5].LastIndexOf(':') + 1), out height);

                    //正确的border 宽高要乘以 1080/432.0, 缩放到本canvas的比例是原图的 1/3, 因此本canvas中显示的 border 宽高是 txt 数据中的 360/432.0, 这是标框程序的一个问题遗留 !!!
                    width *= (360 / 432.0);
                    height *= (360 / 432.0);
                    //Add bounding boxes into m_AllBoxesInVideo
                    m_AllBoxesInVideo[frameNumber - 1].Add(new BoundingBox(person_id,new Rec(new Point(topLeftPointX,topLeftPointY),width,height)));
                }
            }
        }
        /// <summary>
        /// Write rectification history into rectify_process.txt
        /// </summary>
        /// <param name="nextImageIndexToWorkWith">image index to work with next time</param>
        private void WriteRectifyHistory(int nextImageIndexToWorkWith)
        { 
            using(StreamWriter rectifyProcessWriter = new StreamWriter(m_rectifyTxtPath,false,Encoding.UTF8))
            {
                rectifyProcessWriter.WriteLine(nextImageIndexToWorkWith.ToString());
            }
        }

        /// <summary>
        /// BrowseButton click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog browseDialog = new System.Windows.Forms.FolderBrowserDialog();
            browseDialog.ShowDialog();
            String videoPath = browseDialog.SelectedPath;
            //判断是否是合法video路径 '/videoxx'
            if (videoPath.Length != 0 && videoPath.Substring(videoPath.LastIndexOf('\\') + 1, 5).Equals("video"))
            {

                String colorImageFolderPath = System.IO.Path.Combine(videoPath,"ColorImage");

                m_imageFiles = new List<String>(System.IO.Directory.EnumerateFiles(colorImageFolderPath,"*.jpg"));
                m_imageFiles.Sort();

                m_peopleTxtFiles = new List<string>();
                m_peopleSubDirectPath = System.IO.Path.Combine(videoPath, "people");

                //确定好每个 person_xx.txt 文件的绝对路径
                for (int i = 0; i < m_totalIdCnt; ++i)
                {
                    String txtName = String.Format("person_{0}.txt", (i + 1 < 10 ? "0" : "") + (i + 1).ToString());
                    m_peopleTxtFiles.Add(System.IO.Path.Combine(m_peopleSubDirectPath,txtName));
                }
                m_peopleTxtFiles.Sort();
                m_boundingBoxesTxtPath = System.IO.Path.Combine(videoPath, "BoundingBoxes.txt");
                m_imageNumberBeginIndex = m_imageFiles[0].LastIndexOf('\\')+1;
                m_imageCount = m_imageFiles.Count();

                //Initialize m_AllBoxesInVideo with all information in BoundingBoxes.txt
                Initm_AllBoxesInVideo();

                //若此 video 之前没有校正记录
                m_rectifyTxtPath = System.IO.Path.Combine(videoPath, "rectify_process.txt");
                if (!File.Exists(m_rectifyTxtPath))
                {
                    m_imageNumberInIndex = 0;
                    m_imageNumberInString = m_imageFiles[0].Substring(m_imageNumberBeginIndex, 5);
                    //写入最初的校正记录
                    WriteRectifyHistory(m_imageNumberInIndex);

                    //为每个 id 在 people 子文件夹下创建 person_xx.txt 文件，以记录 第 xx 个人 每次出现对应的帧号和框 (初始只写入对应的id)
                    //注意 ! 这里由于文件名的原因, id 最多为两位数 !
                    Directory.CreateDirectory(m_peopleSubDirectPath);
                    for(int i = 0 ; i < m_totalIdCnt; ++ i)
                    { 
                        using (StreamWriter peopleInformationWriter = new StreamWriter(m_peopleTxtFiles[i],true,Encoding.UTF8))
                        {
                            peopleInformationWriter.WriteLine((i+1).ToString());
                        }
                    }
                }
                else 
                {
                    //读取此前校正记录
                    using (StreamReader rectifyProcessReader = new StreamReader(m_rectifyTxtPath, Encoding.UTF8))
                    {
                        String lastWorkIndex = rectifyProcessReader.ReadLine();
                        int.TryParse(lastWorkIndex, out m_imageNumberInIndex);
                        //如果显示 Video 已经检查完毕
                        if (m_imageNumberInIndex == -1)
                        {
                            testBlock.Text = "This video has been rectified !";
                            BrowseButton.IsEnabled = false;
                            return;
                        }
                        m_imageNumberInString = m_imageFiles[m_imageNumberInIndex].Substring(m_imageNumberBeginIndex, 5);
                    }
                }
                //把本次开始处理的第一张图片的信息 (image、bounding boxes)显示在 Canvas 上
                WashDisplayingCanvas(m_imageNumberInIndex);

                //将 video number 和 image number 显示在 文本框中
                videoNumberBlock.Text = videoPath.Substring(videoPath.LastIndexOf('\\') + 1);
                ImageNumberBlock.Text = m_imageNumberInString;
                
                //唤醒 next button, del button 和 app button, 催眠 browse button
                saveAndNextButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
                AppendButton.IsEnabled = true;
                BrowseButton.IsEnabled = false;
            }
            else 
            {
                System.Windows.Forms.MessageBox.Show("Please choose a valid folder path !");
            }
        }


        /// <summary>
        /// Display an image onto canvas, delete all children of it, and show corresponding bounding boxes of
        /// current image onto it
        /// </summary>
        /// <param name="imageIndex">Image index</param>
        private void WashDisplayingCanvas(int imageIndex)
        {
            //Wash the canvas
            displayingCanvas.Children.Clear();
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new System.Uri(m_imageFiles[imageIndex]);
            bitmapImage.EndInit();
            m_canvasBrush.ImageSource = bitmapImage;
            displayingCanvas.Background = m_canvasBrush;

            //清空 id 池
            ClearIdPool();

            //清空当前帧的图片框和文字框列表
            m_AllBordersOfCurrentImage.Clear();
            m_AllTextBlocksOfCurrentImage.Clear();

            //Show the bounding boxes
            foreach (BoundingBox bboxes in m_AllBoxesInVideo[imageIndex])
            {
                //标记 id 已使用
                m_idPool[bboxes.m_personId] = true;

                //Draw borders onto canvas
                Border border = new Border();
                border.Width = bboxes.rectangle.width;
                border.Height = bboxes.rectangle.height;
                border.BorderBrush = Brushes.Green;
                border.BorderThickness = new Thickness(2);

                double displayY = bboxes.rectangle.m_topLeftPoint.Y*m_actualToDisplayRate;
                double displayX = bboxes.rectangle.m_topLeftPoint.X*m_actualToDisplayRate;

                Canvas.SetTop(border, displayY);
                Canvas.SetLeft(border, displayX);
                displayingCanvas.Children.Add(border);

                //Add borders to control list
                m_AllBordersOfCurrentImage.Add(border);

                //Draw id text onto canvas
                TextBlock textBlock = new TextBlock();
                textBlock.Text = bboxes.m_personId.ToString();
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0,175,251));

                Canvas.SetTop(textBlock, displayY);
                Canvas.SetLeft(textBlock, displayX + border.Width / 2.0);
                displayingCanvas.Children.Add(textBlock);

                //Add text blocks to control list
                m_AllTextBlocksOfCurrentImage.Add(textBlock);
            }
        }

        /// <summary>
        /// Clear the id pool of an image
        /// </summary>
        private void ClearIdPool()
        {
            for (int i = 0; i < m_idPool.Length; ++i)
            {
                m_idPool[i] = false;
            }
        }

        /// <summary>
        /// Next button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAndNextButton_Click(object sender, RoutedEventArgs e)
        {
            //若本帧是倒数第二帧或最后一帧
            if (m_imageNumberInIndex >= m_imageCount - 2)
            {
                //写下本帧的 m_AllBoxesInVideo[m_imageNumberInIndex] 中的信息写入person_xx.txt
                WritingBboxesInPersonTxt(m_imageNumberInIndex);

                //若本帧是倒数第二帧, 则把本帧 bboxes 情况,复制到下一帧, 并补上下一帧的帧号的 m_AllBoxesInVideo[...]中的信息
                //其实不复制, 直接把本帧信息再写一次也行, 反正m_AllBoxesInVideo[m_imageNumberInIndex+1]中的信息再也不需要了
                if (m_imageNumberInIndex == m_imageCount - 2)
                {
                    CopyBboxesToNextFrame(m_imageNumberInIndex);
                    WritingBboxesInPersonTxt(m_imageNumberInIndex + 1);
                }
                //设置 button 不可按
                saveAndNextButton.IsEnabled = false;
                //通知用户标记结束
                rectifyAccomplished();
            }
            else
            {
                //把本帧 bboxes 情况,复制到下一帧, 再把本帧和下一帧的 AllBoxesInVieo[]中的信息写入 person_xx.txt
                CopyBboxesToNextFrame(m_imageNumberInIndex);
                WritingBboxesInPersonTxt(m_imageNumberInIndex);
                WritingBboxesInPersonTxt(m_imageNumberInIndex + 1);

                //增加 imageNumber (int 和 string)
                m_imageNumberInIndex += 2;
                m_imageNumberInString = m_imageFiles[m_imageNumberInIndex].Substring(m_imageNumberBeginIndex, 5);

                //改变 imageNumberBlock 中的内容
                this.ImageNumberBlock.Text = m_imageNumberInString;

                //更改rectify_process.txt中的进度到下一张 (实际上是每隔两张处理一次) 要处理的图片
                WriteRectifyHistory(m_imageNumberInIndex);
                
                //到下一次的任务
                WashDisplayingCanvas(m_imageNumberInIndex);
            }
        }

        /// <summary>
        /// Write bounding boxes of m_AllBoxesInVideo[index] into person_xx.txt
        /// </summary>
        /// <param name="index">Current frame's index</param>
        private void WritingBboxesInPersonTxt(int index)
        {
            foreach (BoundingBox box in m_AllBoxesInVideo[index])
            {
                using (StreamWriter confirmBoxStreamWriter = new StreamWriter(m_peopleTxtFiles[box.m_personId - 1], true, Encoding.UTF8))
                {
                    //注意: 这里修正了以前 矩形宽高存储的时候没有 resize 到 1920 x 1080 的区域的缺陷
                    confirmBoxStreamWriter.WriteLine(String.Format("{0}, {1} {2} {3} {4}", index + 1, box.rectangle.m_topLeftPoint.X,
                        box.rectangle.m_topLeftPoint.Y, box.rectangle.width / m_actualToDisplayRate, box.rectangle.height / m_actualToDisplayRate));
                }
            }
        }

        /// <summary>
        /// After user has finished the rectification work
        /// </summary>
        private void rectifyAccomplished()
        {
            saveAndNextButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            AppendButton.IsEnabled = false;
            testBlock.Text = "Rectification task is finished !";
            WriteRectifyHistory(-1);
        }

        /// <summary>
        /// Copy bounding boxes of m_AllBoxesInVideo[index] into m_AllBoxesInVideo[index+1]
        /// </summary>
        /// <param name="index">Current frame's index</param>
        private void CopyBboxesToNextFrame(int index)
        {
            m_AllBoxesInVideo[index + 1] = m_AllBoxesInVideo[index];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        
    }
}
