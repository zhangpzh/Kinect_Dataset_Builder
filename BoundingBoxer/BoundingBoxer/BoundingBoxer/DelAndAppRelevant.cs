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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            m_EditMode = 1;
            DeleteButton.IsEnabled = false;
            AppendButton.IsEnabled = false;
            saveAndNextButton.IsEnabled = false;
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
            }
            //若为 append mode
            else if ((m_EditMode & 2) != 0)
            { 
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
            }
            //若为 view mode
            else
            {
                //doing nothing
            }
        }

        /*
        /// <summary>
        /// Canvas 键盘按键事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void displayingCanvas_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //捕捉 Esc 按钮
            if (e.Key == Key.Escape)
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

                    //Just for test
                    this.Close();
                }
                //若为 append mode
                else if ((m_EditMode & 2) != 0)
                {
                }
                //若为 view mode
                else 
                { 
                    //Nothing
                }
            }
        }
        */

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
