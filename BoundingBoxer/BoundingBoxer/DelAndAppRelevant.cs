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
using System.Windows.Forms;

namespace BoundingBoxer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 在 m_EditMode 的二进制情况下 : 00 代表 view mode、01 代表 delete mode、 10 代表 append mode
        /// </summary>
        private static int m_EditMode = 0;

        /// <summary>
        /// 当前帧中鼠标在其中的框的索引列表
        /// </summary>
        private static List<int> m_indexesOfBoundingBoxesContainingMouse = new List<int>();
        /// <summary>
        /// 当前帧中鼠标在其外的框的索引列表
        /// </summary>
        private static List<int> m_indexesOfBoundingBoxesNotContainingMouse = new List<int>();

        /// <summary>
        /// 用于判断 append mode 下, 所增添的 Border 的第一个点是否选好
        /// </summary>
        private static bool m_isFirstPointConfirmOrNot = false;

        /// <summary>
        /// 当前 Border 的 id (用户输入)
        /// </summary>
        private static int m_currentSelectedId;

        /// <summary>
        /// Border 的 topLeft 点 和 bottomRight 点
        /// </summary>
        private static Point m_pre_pt;
        private static Point m_nxt_pt;

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            m_EditMode = 1;
            DeleteButton.IsEnabled = false;
            AppendButton.IsEnabled = false;
            nextButton.IsEnabled = false;
            previousButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
        }

        private void AppendButton_Click(object sender, RoutedEventArgs e)
        {
            m_EditMode = 2;
            DeleteButton.IsEnabled = false;
            AppendButton.IsEnabled = false;
            nextButton.IsEnabled = false;
            previousButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
        }

        /// <summary>
        /// Canvas 鼠标左键点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void displayingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //若为 delete mode
            if ((m_EditMode & 1) != 0)
            { 
                //删除当前鼠标在其中(标红)的 bounding boxes
                int cnt = 0;
                foreach (int index in m_indexesOfBoundingBoxesContainingMouse)
                {
                    //获得当前要删掉的框的 id 并回收
                    int currentId = m_AllBoxesInVideo[m_imageNumberInIndex][index - cnt].m_personId;
                    m_idPool[currentId] = false;

                    //因为 list 调用 removeAt 会减少 index, 原来的 index 要向前移位, 所以加一个 cnt 来控制
                    m_AllBoxesInVideo[m_imageNumberInIndex].RemoveAt(index - cnt);

                    //从 canvas 中删除掉 框 和 文本
                    displayingCanvas.Children.Remove(m_AllBordersOfCurrentImage[index-cnt]);
                    displayingCanvas.Children.Remove(m_AllTextBlocksOfCurrentImage[index - cnt]);

                    //从 控制列表中删除掉 框 和 文本
                    m_AllBordersOfCurrentImage.RemoveAt(index - cnt);
                    m_AllTextBlocksOfCurrentImage.RemoveAt(index - cnt);

                    ++cnt;
                }
                //清空列表
                m_indexesOfBoundingBoxesContainingMouse.Clear();
                m_indexesOfBoundingBoxesNotContainingMouse.Clear();

                //若本帧后面还有帧, 则复制当前帧的 bounding box 情况到下一帧
                if (m_imageNumberInIndex <= m_totalImgCnt - 2)
                {
                    //把本帧 bboxes 情况,复制到下一帧
                    CopyBboxes(m_imageNumberInIndex, m_imageNumberInIndex + 1);
                }
            }
            //若为 append mode
            else if ((m_EditMode & 2) != 0)
            {
                //说明两个点都已经选好
                if (true == m_isFirstPointConfirmOrNot)
                {
                    //1. 设置 m_isFirstPointConfirmOrNot 为假, 把Border插入到 Canvas.Children末尾, 把 border 插入到 border 控制列表末尾. 
                    //2. 输入可用id, 从 id 池分配此可用id, 
                    //3. 把文本框插入到 Canvas.Children末尾, 把文本框插入到 textBlock 控制列表末尾.
                    //4. 插入当前Border对应的bounding box(根据 border 宽高, m_pre_p, m_nxt_p 确定) 到 m_AllBoxesInVideo[m_imageNumberInIndex] 末尾. 

                    //1. 设置 m_isFirstPointConfirmOrNot 为假, 把Border插入到 Canvas.Children末尾, 把 border 插入到 border 控制列表末尾. 
                    m_isFirstPointConfirmOrNot = false;

                    Border border = DrawABorderThroughTwoPoint(m_pre_pt, m_nxt_pt);
                    m_AllBordersOfCurrentImage.Add(border);

                    //2. 输入可用id, 从 id 池分配此可用id, 
                    int id = 0;
                    //若 id 超出范围或已经在用了, 就要求重输
                    while ((id < 1 || id > m_totalIdCnt) || m_idPool[id] == true)
                    {
                        // Input person id
                        String userInput = Microsoft.VisualBasic.Interaction.InputBox("Please input a valid person id !");
                        int.TryParse(userInput, out id);
                    }
                    m_idPool[id] = true;

                    //3. 把文本框插入到 Canvas.Children末尾, 把文本框插入到 textBlock 控制列表末尾.
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = id.ToString() + ". " + m_idToName[id];
                    textBlock.Foreground = Brushes.Red;

                    Point topLeftPoint = GetTopLeftPointByTwoPoints(m_pre_pt, m_nxt_pt);
                    Canvas.SetTop(textBlock, topLeftPoint.Y);
                    Canvas.SetLeft(textBlock, topLeftPoint.X + border.Width / 2.0);
                    displayingCanvas.Children.Add(textBlock);

                    m_AllTextBlocksOfCurrentImage.Add(textBlock);

                    //4. 插入当前Border对应的bounding box(根据 border 宽高, m_pre_p, m_nxt_p 确定) 到 m_AllBoxesInVideo[m_imageNumberInIndex] 末尾. 
                    m_AllBoxesInVideo[m_imageNumberInIndex].Add(new BoundingBox(id, new Rec(new Point(topLeftPoint.X , topLeftPoint.Y),
                        border.Width, border.Height)));

                    //若本帧后面还有帧, 则复制当前帧的 bounding box 情况到下一帧
                    if (m_imageNumberInIndex <= m_totalImgCnt - 2)
                    {
                        //把本帧 bboxes 情况,复制到下一帧
                        CopyBboxes(m_imageNumberInIndex, m_imageNumberInIndex + 1);
                    }
                }
                //选好第一个点
                else 
                {
                    //确定 m_pre_pt, 设置 m_isFirstPointConfirmOrNot 为真
                    m_pre_pt = e.GetPosition(displayingCanvas);
                    m_isFirstPointConfirmOrNot = true;
                }
            }
            //若为 view mode
            else 
            { 
                //doing nothing
            }
        }

        /// <summary>
        /// Canvas 鼠标移动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void displayingCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //若为 delete mode
            if ((m_EditMode & 1) != 0)
            {
                //把上一次鼠标移动的包含鼠标的 bounding boxes 的边框颜色染回绿色
            	DyeBoundingBoxesContainingMouse(Brushes.Green);

                getContainAndNotContainBboxes(e.GetPosition(displayingCanvas));

                //把本次鼠标移动的包含鼠标的 bounding boxes 的边框颜色染成红色
                DyeBoundingBoxesContainingMouse(Brushes.Red);
            }
            //若为 append mode
            else if ((m_EditMode & 2) != 0)
            {
                //刷新当前鼠标位置
                m_nxt_pt = e.GetPosition(displayingCanvas);
            }
            //若为 view mode
            else
            {
                //doing nothing
            }
        }


        /// <summary>
        /// 通过两个给定的点, 可以确定一个Border, 该函数返回这个Border的左上角点
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        private Point GetTopLeftPointByTwoPoints(Point point1, Point point2)
        { 
            double minX = (point1.X < point2.X ? point1.X : point2.X);
            double minY = (point1.Y < point2.Y ? point1.Y : point2.Y);
            return new Point(minX, minY);
        }

        /// <summary>
        /// 通过两个给定的点, 画一个 Border 把 这个 Border 显示在 Canvas 上 (加入到 Canvas 的 children 列表中), 并返回这个 Border
        /// </summary>
        /// <param name="point1">第一个点</param>
        /// <param name="point2">第二个点</param>
        /// <returns></returns>
        private Border DrawABorderThroughTwoPoint(Point point1, Point point2)
        {
            double minX, maxX, minY, maxY, WIDTH, HEIGHT;
            if (point1.X < point2.X)
            {
                minX = point1.X;
                maxX = point2.X;
            }
            else 
            {
                minX = point2.X;
                maxX = point1.X;
            }

            if (point1.Y < point2.Y)
            {
                minY = point1.Y;
                maxY = point2.Y;
            }
            else 
            { 
                minY = point2.Y;
                maxY = point1.Y;
            }

            WIDTH = maxX - minX;
            HEIGHT = maxY - minY;

            //Draw borders onto canvas
            Border border = new Border();
            border.Width = WIDTH;
            border.Height = HEIGHT;
            border.BorderBrush = Brushes.Green;
            border.BorderThickness = new Thickness(2);

            Canvas.SetTop(border, minY);
            Canvas.SetLeft(border, minX);
            displayingCanvas.Children.Add(border);

            return border;
        }

        /// <summary>
        /// 从 Edit Mode 进入 View Mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //若为 delete mode
            if ((m_EditMode & 1) != 0)
            {
                //把上一次鼠标移动的包含鼠标的 bounding boxes 的边框颜色染回绿色
                DyeBoundingBoxesContainingMouse(Brushes.Green);
                //清空列表
                m_indexesOfBoundingBoxesContainingMouse.Clear();
                m_indexesOfBoundingBoxesNotContainingMouse.Clear();
                //改成 view mode
                m_EditMode = 0;

                //如果已经触碰到底, 而且已经是本 video 最后一次图片了, 则 next 为 false
                if (m_indexReachedSoFar == -1 && m_imageNumberInIndex >= m_totalImgCnt - 2)
                {
                    nextButton.IsEnabled = false;
                }
                else 
                {
                    nextButton.IsEnabled = true;
                }

                //已经到本次窗口任务能改变的第一帧了，就设置 previous button 不可按
                if (m_imageNumberInIndex <= m_firstIndexWorkThisTime)
                {
                    previousButton.IsEnabled = false;
                }
                else 
                { 
                    previousButton.IsEnabled = true;
                }

                DeleteButton.IsEnabled = true;
                AppendButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
            }
            //若为 append mode
            else if ((m_EditMode & 2) != 0)
            {
                //没有完整地完成当前 append 事务, 不予离开
                if (true == m_isFirstPointConfirmOrNot)
                {
                    System.Windows.Forms.MessageBox.Show("当前框没有完成, 不予离开");
                    return;
                }

                //改成 view mode
                m_EditMode = 0;

                //如果已经触碰到底, 而且已经是本 video 最后一次图片了, 则 next 为 false
                if (m_indexReachedSoFar == -1 && m_imageNumberInIndex >= m_totalImgCnt - 2)
                {
                    nextButton.IsEnabled = false;
                }
                else
                {
                    nextButton.IsEnabled = true;
                }

                //已经到本次窗口任务能改变的第一帧了，就设置 previous button 不可按
                if (m_imageNumberInIndex <= m_firstIndexWorkThisTime)
                {
                    previousButton.IsEnabled = false;
                }
                else
                {
                    previousButton.IsEnabled = true;
                }

                DeleteButton.IsEnabled = true;
                AppendButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
            }
            //若为 view mode, 这个分支是不会被执行的
            else
            {
                //Nothing
            }
        }

        /// <summary>
        /// //Add the indexes of bounding boxes containing the mouse right now and not containing the mouse right now into two list
        /// </summary>
        /// <param name="curMousePos"></param>
        void getContainAndNotContainBboxes(Point curMousePos)
        {
            m_indexesOfBoundingBoxesContainingMouse.Clear();
            m_indexesOfBoundingBoxesNotContainingMouse.Clear();

            for(int i = 0 ; i < m_AllBoxesInVideo[m_imageNumberInIndex].Count ; ++i)
            {
                BoundingBox box = m_AllBoxesInVideo[m_imageNumberInIndex][i];
                double X, Y, WIDTH, HEIGHT;

                X = box.rectangle.m_topLeftPoint.X;
                Y = box.rectangle.m_topLeftPoint.Y;
                WIDTH = box.rectangle.width;
                HEIGHT = box.rectangle.height;
                
                //If current box contains the mouse
                if (curMousePos.Y >= Y && curMousePos.Y <= Y + HEIGHT && curMousePos.X >= X && curMousePos.X <= X + WIDTH)
                {
                    m_indexesOfBoundingBoxesContainingMouse.Add(i);
                }
                else 
                {
                    m_indexesOfBoundingBoxesNotContainingMouse.Add(i);
                }
            }
        }

        /// <summary>
        /// Dye the color of boxes whose indexes are included in the list "m_indexOfBoundingBoxesContainingMouse"
        /// </summary>
        /// <param name="colorBrush"></param>
        void DyeBoundingBoxesContainingMouse(SolidColorBrush colorBrush)
        {
            foreach (int index in m_indexesOfBoundingBoxesContainingMouse)
            {
                m_AllBordersOfCurrentImage[index].BorderBrush = colorBrush;
            }
        }

    }
}
