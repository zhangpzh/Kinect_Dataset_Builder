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
using System.IO;
using System.Windows.Shapes;
using Coding4Fun.Kinect.Wpf;

namespace RegisterColorAndDepth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Point[] color_skl = new Point[25];
        public static Point[] depth_skl = new Point[25];

        public static double DistBtwnPoints(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2.0) + Math.Pow(a.Y - b.Y, 2.0));
        }

        public static double GetScaleRate()
        {
            //计算300对关节点的平均距离
            double average_color_joints_dist, average_depth_joints_dist;
            double sum_color_joints_dist = 0.0, sum_depth_joints_dist = 0.0;
            for (int i = 0; i < 25; ++i)
            {
                for (int j = i + 1; j < 25; ++j)
                {
                    sum_color_joints_dist += DistBtwnPoints(color_skl[i], color_skl[j]);
                    sum_depth_joints_dist += DistBtwnPoints(depth_skl[i], depth_skl[j]);
                }
            }
            average_color_joints_dist = sum_color_joints_dist / 300.0;
            average_depth_joints_dist = sum_depth_joints_dist / 300.0;

            double scaleRate_ColorToDepth = average_color_joints_dist / average_depth_joints_dist;
            return scaleRate_ColorToDepth;
        }

        public MainWindow()
        {
            InitializeComponent();
            
            String line = String.Empty;
            String[] entries;
            double mean_scaleRate_ColorToDepth = 0.0;
            int bodyCnt = 0;

            //Get color skeleton joints' coordinate
            using (StreamReader sklReader = new StreamReader(@"C:\Users\Zhang\Desktop\inputs.txt", Encoding.UTF8))
            {
                while ((line = sklReader.ReadLine()) != null)
                {
                    ++bodyCnt;
                    entries = line.Split(',');
                    for (int i = 0; i < 25; ++i)
                    {
                        String[] jointCoor = entries[i].Split(' ');
                        double x, y;
                        int indexOffset = i == 0 ? 1 : 0;
                        double.TryParse(jointCoor[1 - indexOffset], out x);
                        double.TryParse(jointCoor[2 - indexOffset], out y);
                        color_skl[i].X = x;
                        color_skl[i].Y = y;
                    }

                    //Get depth skeleton joints' coordinate
                    line = sklReader.ReadLine();
                    entries = line.Split(',');
                    for (int i = 0; i < 25; ++i)
                    {
                        String[] jointCoor = entries[i].Split(' ');
                        double x, y;
                        int indexOffset = i == 0 ? 1 : 0;
                        double.TryParse(jointCoor[1 - indexOffset], out x);
                        double.TryParse(jointCoor[2 - indexOffset], out y);
                        depth_skl[i].X = x;
                        depth_skl[i].Y = y;
                    }
                    mean_scaleRate_ColorToDepth += GetScaleRate(); 
                }
            }
            //求出平均的 scale rate
            mean_scaleRate_ColorToDepth /= bodyCnt;


            //分别求rgb骨骼和depth骨骼的中心点
            Point rgb_center = new Point(0.0, 0.0), depth_center = new Point(0.0, 0.0);
            for (int i = 0; i < 25; ++i)
            {
                rgb_center.X += color_skl[i].X;
                rgb_center.Y += color_skl[i].Y;
                depth_center.X += depth_skl[i].X;
                depth_center.Y += depth_skl[i].Y;
            }
            rgb_center.X /= 25.0;
            rgb_center.Y /= 25.0;
            depth_center.X /= 25.0;
            depth_center.Y /= 25.0;

            depth_center.X = (int)depth_center.X;
            depth_center.Y = (int)depth_center.Y;

            Point rgb_center_resized = new Point((int)(rgb_center.X / mean_scaleRate_ColorToDepth), (int)(rgb_center.Y / mean_scaleRate_ColorToDepth));

            //原 color 图 (1920 x 1080) resize 后的大小
            int rgb_width_resized = (int)(1920.0/mean_scaleRate_ColorToDepth);
            int rgb_height_resized = (int)(1080.0/mean_scaleRate_ColorToDepth);

            //假设 pixel 的 index 是从0数起的

            //中心店离四周的距离
            int rgbToTop = (int)(rgb_center_resized.Y);
            int rgbToBottom = (int)((rgb_height_resized - 1) - rgb_center_resized.Y);
            int rgbToLeft = (int)(rgb_center_resized.X);
            int rgbToRight = (int)((rgb_width_resized - 1) - rgb_center_resized.X);

            int depthToTop = (int)(depth_center.Y);
            int depthToBottom = (int)((424 - 1) - depth_center.Y);
            int depthToLeft = (int)depth_center.X;
            int depthToRight = (int)((512 - 1) - depth_center.X);

            //容易得知 color image (resize后) 的左右两端需要裁剪, 而 depth image 的上下两端需要裁剪

            //color image (reisze后) 裁剪后的第一列和最后一列在裁剪前的 index
            int beginColumnIndex = (int)rgb_center_resized.X - depthToLeft;
            int endColumnIndex = (int)rgb_center_resized.X + depthToRight;

            //depth image 裁剪后的第一行和最后一行在裁剪前的 index
            int beginRowIndex = (int)depth_center.Y - rgbToTop;
            int endRowIndex = (int)depth_center.Y + rgbToBottom;

            //裁剪(color 和 depth 已对其)后的图的大小 (本输入样例是 512 * 374)
            int newImage_Width = endColumnIndex - beginColumnIndex + 1;
            int newImage_Height = endRowIndex - beginRowIndex + 1;


            //本篇代码最重要的地方就是 求出 beginColumnIndex、endColumnIndex、beginRowIndex 和 endRowIndex 还有图的大小 newImage_Width、newImage_Height
        }
    }
}
