//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
        /// 记录本Video的标框进度的文本文件路径
        /// </summary>
        private static String m_BoundingProcessTxtPth = String.Empty;
        
        /// <summary>
        /// image路径名中, image帧号出现的第一个index, 比如 'xxx/001.jpg', 则 该值为 4 -> 即字符串中第一个'0'的index (这个值对于一个 video 中的 image 而言都是一样的)
        /// </summary>
        private static int m_imageNumberBeginIndex;

        /// <summary>
        /// 存储video的bounding boxes 的文本文件 "BBoxes.txt" 的绝对路径
        /// </summary>
        private static String m_BoundingBoxesTxtPath = String.Empty;

        /// <summary>
        /// 用于记录本次 Video 已经标了多远, 这决定了关闭程序, 再打开程序时要工作的图片的第一张图片
        /// </summary>
        private static int m_indexReachedSoFar;

        /// <summary>
        /// 用于记录本次打开程序, 工作的第一张图片, 到了这张图片, previous 按钮就不能跳转到之前图片了.
        /// </summary>
        private static int m_firstIndexWorkThisTime;

        /// <summary>
        /// image 帧号 (string)
        /// </summary>
        private static String m_imageNumberInString = String.Empty;
        /// <summary>
        /// image 索引 (int, begin from zero)
        /// </summary>
        private static int m_imageNumberInIndex;
        /// <summary>
        /// 当前 video 中的 color 文件夹中的 image 总数
        /// </summary>
        private static int m_totalImgCnt;
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
        /// total id count
        /// </summary>
        private const int m_totalIdCnt = 10;
        /// <summary>
        /// 人物 id
        /// </summary>
        private static bool[] m_idPool = new bool[m_totalIdCnt + 1];        //默认只提供1-10个id

        /// <summary>
        /// 存储 id 到人名的映射
        /// </summary>
        private static Dictionary<int, String> m_idToName = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeIdToNameDict();
        }

        /// <summary>
        /// 初始化 id 到人名的映射
        /// </summary>
        private void InitializeIdToNameDict()
        {
            m_idToName = new Dictionary<int, string>();
            m_idToName.Add(1,"伟宏");
            m_idToName.Add(2,"东程");
            m_idToName.Add(3,"宏伟");
            m_idToName.Add(4,"杰超");
            m_idToName.Add(5,"瑾洁");
            m_idToName.Add(6,"卢曦");
            m_idToName.Add(7,"培圳");
            m_idToName.Add(8,"尚轩");
            m_idToName.Add(9,"亚芳");
            m_idToName.Add(10,"永毅");
        }
        
        /// <summary>
        /// 用 BBoxes.txt 初始化内存中的 m_AllBoxesInVideo
        /// </summary>
        /// <param name="boundingBoxesPath">BBoxes.txt 文件的路径</param>
        private void Initm_AllBoxesInVideo(String boundingBoxesPath)
        {
            //为每一帧申请一个 bounding boxes 列表
            m_AllBoxesInVideo = new List<List<BoundingBox>>();
            for (int i = 0; i < m_totalImgCnt; ++i)
            {
                m_AllBoxesInVideo.Add(new List<BoundingBox>());
            }

            using (StreamReader boxesReader = new StreamReader(boundingBoxesPath, Encoding.UTF8))
            {
                int frameNumber;
                int person_id;
                double topLeftPointX, topLeftPointY, width, height;
                String line;
                while ((line = boxesReader.ReadLine()) != null)
                {
                    //Get information within a record
                    String[] entries = line.Split(' ');
                    int.TryParse(entries[0], out frameNumber);
                    int.TryParse(entries[1], out person_id);
                    double.TryParse(entries[2], out topLeftPointX);
                    double.TryParse(entries[3], out topLeftPointY);
                    double.TryParse(entries[4], out width);
                    double.TryParse(entries[5], out height);

                    //Add bounding boxes into m_AllBoxesInVideo
                    m_AllBoxesInVideo[frameNumber - 1].Add(new BoundingBox(person_id,new Rec(new Point(topLeftPointX,topLeftPointY),width,height)));
                }
            }
        }

        /// <summary>
        /// Write rectification history into bounding_process.txt
        /// </summary>
        /// <param name="nextImageIndexToWorkWith">image index to work with next time</param>
        private void WriteRectifyHistory(int nextImageIndexToWorkWith)
        { 
            using(StreamWriter boundingProcessWriter = new StreamWriter(m_BoundingProcessTxtPth,false,Encoding.UTF8))
            {
                boundingProcessWriter.WriteLine(nextImageIndexToWorkWith.ToString());
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

                //获得 BBoxes 文本文件的路径
                m_BoundingBoxesTxtPath = System.IO.Path.Combine(videoPath, "BBoxes.txt");


                //开始工作的图片设为第一帧
                m_imageNumberBeginIndex = m_imageFiles[0].LastIndexOf('\\')+1;
                //记录当前 video 的 color 文件夹中的 图片总数
                m_totalImgCnt = m_imageFiles.Count();


                //检查此 video 是否有标框记录
                m_BoundingProcessTxtPth = System.IO.Path.Combine(videoPath, "bounding_process.txt");

                if (!File.Exists(m_BoundingProcessTxtPth))
                {
                    //生成空的 框文件 (BBoxes.txt) 和 标框记录文件 (bounding_process.txt)
                    //File.Create(m_BoundingBoxesTxtPath);
                    //File.Create(m_BoundingProcessTxtPth);
                    using (System.IO.FileStream bboxesFs = File.Open(m_BoundingBoxesTxtPath,FileMode.Create))
                    {
                        using (System.IO.FileStream boundignProcessFs = File.Open(m_BoundingProcessTxtPth,FileMode.Create))
                        { 
                        }
                    }

                    //初始化 m_AllBoxesInVideo
                    Initm_AllBoxesInVideo(m_BoundingBoxesTxtPath);

                    //从第一帧开始工作
                    m_imageNumberInIndex = m_indexReachedSoFar = m_firstIndexWorkThisTime = 0;
                    m_imageNumberInString = m_imageFiles[0].Substring(m_imageNumberBeginIndex, 5);
                }
                else 
                {
                    //初始化 m_AllBoxesInVideo
                    Initm_AllBoxesInVideo(m_BoundingBoxesTxtPath);

                    //读取此前标框进度
                    using (StreamReader boundingProcessReader = new StreamReader(m_BoundingProcessTxtPth, Encoding.UTF8))
                    {
                        String lastWorkIndex = boundingProcessReader.ReadLine();
                        int.TryParse(lastWorkIndex, out m_indexReachedSoFar);
                        //如果显示 Video 已经检查完毕 -> 进度值为 "-1"
                        if (m_indexReachedSoFar == -1)
                        {
                            testBlock.Text = "This video has been rectified !";
                            BrowseButton.IsEnabled = false;
                            return;
                        }
                        //设置工作进度
                        m_imageNumberInIndex = m_firstIndexWorkThisTime = m_indexReachedSoFar;
                        m_imageNumberInString = m_imageFiles[m_imageNumberInIndex].Substring(m_imageNumberBeginIndex, 5);
                    }
                }
                //把画布的大小设置得和图片一样
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new System.Uri(m_imageFiles[m_imageNumberInIndex]);
                bitmapImage.EndInit();
                displayingCanvas.Width = displayingCanvasBorder.Width = bitmapImage.PixelWidth;
                displayingCanvas.Height = displayingCanvasBorder.Height = bitmapImage.PixelHeight;

                //把开始工作的第一张帧的信息 (图像、bounding boxes)显示在画布上
                WashDisplayingCanvas(m_imageNumberInIndex);

                //将 video number 和 image number 显示在 文本框中
                videoNumberBlock.Text = videoPath.Substring(videoPath.LastIndexOf('\\') + 1);
                ImageNumberBlock.Text = m_imageNumberInString;
                
                //唤醒 next, del 和 app button, 催眠 browse button, cancel button, previous button
                nextButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
                AppendButton.IsEnabled = true;
                BrowseButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
                previousButton.IsEnabled = false;
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

                double displayY = bboxes.rectangle.m_topLeftPoint.Y;
                double displayX = bboxes.rectangle.m_topLeftPoint.X;

                Canvas.SetTop(border, displayY);
                Canvas.SetLeft(border, displayX);
                displayingCanvas.Children.Add(border);

                //Add borders to control list
                m_AllBordersOfCurrentImage.Add(border);

                //Draw id text onto canvas
                TextBlock textBlock = new TextBlock();
                textBlock.Text = bboxes.m_personId.ToString() + ". " + m_idToName[bboxes.m_personId];
                textBlock.Foreground = Brushes.Red;

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

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            //若本帧是倒数第二帧或最后一帧
            if (m_imageNumberInIndex >= m_totalImgCnt - 2)
            {
                //设置 next button 不可按
                nextButton.IsEnabled = false;

                m_indexReachedSoFar = -1;

                //通知用户已经标过最后一帧, 此时仍然可以编辑不满意的一些帧(上一次关闭程序(如果是在本video的话)的后两帧到最后一帧), 但是关闭程序后将不能再编辑本 video 的任何帧
                currentVideoIsFinishing();
            }
            else
            {
                //增加 imageNumber (int 和 string)
                m_imageNumberInIndex += 2;
                m_imageNumberInString = m_imageFiles[m_imageNumberInIndex].Substring(m_imageNumberBeginIndex, 5);

                //更新维护 m_indexReachedSoFar
                if (m_indexReachedSoFar != -1)
                {
                    m_indexReachedSoFar = (m_indexReachedSoFar < m_imageNumberInIndex ? m_imageNumberInIndex : m_indexReachedSoFar);
                }

                //改变 imageNumberBlock 中的内容
                this.ImageNumberBlock.Text = m_imageNumberInString;

                //到下一次的任务
                WashDisplayingCanvas(m_imageNumberInIndex);
            }
            //previous button 是否可按, 未验证此条判断式 是否 完全正确
            if (m_imageNumberInIndex - 2 >= m_firstIndexWorkThisTime)
            {
                previousButton.IsEnabled = true;
            }
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            //减少 imageNumber (int 和 string)
            m_imageNumberInIndex -= 2;
            m_imageNumberInString = m_imageFiles[m_imageNumberInIndex].Substring(m_imageNumberBeginIndex, 5);

            //改变 imageNumberBlock 中的内容
            this.ImageNumberBlock.Text = m_imageNumberInString;

            //到上一次的任务
            WashDisplayingCanvas(m_imageNumberInIndex);

            //已经到本次窗口任务能改变的第一帧了，就设置 previous button 不可按
            if(m_imageNumberInIndex <= m_firstIndexWorkThisTime)
            {
                previousButton.IsEnabled = false;
            }

            //设置 next button 可按
            nextButton.IsEnabled = true;
        }

        /// <summary>
        /// 通知用户已经标过最后一帧, 此时仍然可以编辑不满意的一些帧(上一次关闭程序(如果是在本video的话)的后两帧到最后一帧), 但是关闭程序后将不能再编辑本 video 的任何帧 
        /// </summary>
        private void currentVideoIsFinishing()
        {
            testBlock.Text = "已经标过最后一帧, 此时仍然可以编辑不满意的一些帧(上一次关闭程序(如果是在本video的话)的后两帧到最后一帧), 但是关闭程序后将不能再编辑本 video 的任何帧";
        }

        private void CopyBboxes(int fromIndex, int toIndex)
        { 
            m_AllBoxesInVideo[toIndex] = m_AllBoxesInVideo[fromIndex];
        }

        /// <summary>
        /// 窗口关闭时的任务: 
        /// 1. 将 m_AllBoxesInVideo 存储到 BBoxes.txt 中; 
        /// 2. 写入标框进度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //打开程序, 然后直接关闭窗口时, m_BoundingBoxesTxtPath 由于还是空字符串, 会引发空路径名的 System.ArgumentException. 因此做异常处理如下
            try
            {
                //将 m_AllBoxesInVideo 存储到 boundingboxes(new).txt 中
                using (StreamWriter currentBoundingBoxesWriter = new StreamWriter(m_BoundingBoxesTxtPath, false, Encoding.UTF8))
                {
                    for (int i = 0; i < m_totalImgCnt; ++i)
                    {
                        String frameNumber = m_imageFiles[i].Substring(m_imageNumberBeginIndex, 5);
                        foreach (BoundingBox box in m_AllBoxesInVideo[i])
                        {
                            String info = String.Format("{0} {1} {2} {3} {4} {5}",
                                frameNumber, box.m_personId, box.rectangle.m_topLeftPoint.X, box.rectangle.m_topLeftPoint.Y, box.rectangle.width, box.rectangle.height);
                            currentBoundingBoxesWriter.WriteLine(info);
                        }
                    }
                }

                //写回校正记录
                WriteRectifyHistory(m_indexReachedSoFar);
            }
            catch (ArgumentException)
            { 
            }
        }
    }
}