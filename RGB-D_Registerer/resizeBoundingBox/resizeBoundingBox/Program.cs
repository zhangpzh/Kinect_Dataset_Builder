using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace resizeBoundingBox
{
    class Program
    {
        static String sourceDirect = @"H:\peizhen\kinect-dataset(multi-view for activity)\view-yongyi"; 
        static String destDirect = @"I:\kinect-dataset(multi-view for activity)\view-yongyi"; 
        static String BoundingBoxesTxtName = "BoundingBoxes(new).txt"; 
        static String BBoxesTxtName = "BBoxes.txt";

        const double scaleRate = 2.8856393571727597;
        //由于 bounding box 是 color image 中的, 所以只需要剪裁 列就好
        const int leftMostColumnIndex = 97;
        const int rightMostColumnIndex = 608;

        /// <summary>
        /// 根据 video name 对 源 video 中的 boundingboxes 做 resize 和 剪裁, 并生成到 目的 video 中.
        /// </summary>
        /// <param name="videoName"></param>
        static void ResizeBoundingBoxesToNewVideo(String videoName)
        {
            String sourceBboxesTxtPth = System.IO.Path.Combine(sourceDirect, videoName, BoundingBoxesTxtName);
            String destBboxesTxtPth = System.IO.Path.Combine(destDirect,videoName,BBoxesTxtName);
            String line = String.Empty;
            String[] entries;
            using(StreamReader sourceReader = new StreamReader(sourceBboxesTxtPth))
            {
                using (StreamWriter destWriter = new StreamWriter(destBboxesTxtPth,false))
                {
                    while((line = sourceReader.ReadLine()) != null)
                    {
                        entries = line.Split(',');
                    	//Get frame number
                    	String[] frameNumberEntries = entries[0].Split(' ');
                    	String frameNumberInStr = frameNumberEntries[1];
                    	//Get person id
                    	String[] personIdEntries = entries[1].Split(' ');
                    	String personIdInStr = personIdEntries[2];
                    	//Get top left point x
                    	String[] topLeftPointEntries = entries[2].Split('(');
                    	String topLeftPointXInStr = topLeftPointEntries[1];
                    	//Get top left point y
                    	topLeftPointEntries = entries[3].Split(')');
                    	String topLeftPointYInStr = topLeftPointEntries[0];
                    	//Get bounding box width
                    	String[] widthEntries = entries[4].Split(' ');
                    	String widthInStr = widthEntries[2];
                    	//Get bounding box height
                    	String[] heightEntries = entries[5].Split(' ');
                    	String heightInStr = heightEntries[2];

                    	double topLeftPointX, topLeftPointY, width, height;
                    	double.TryParse(topLeftPointXInStr, out topLeftPointX);
                    	double.TryParse(topLeftPointYInStr, out topLeftPointY);
                    	double.TryParse(widthInStr, out width);
                    	double.TryParse(heightInStr, out height);

                    	//Resize current box and store to destination text
                    	//每个 view 的缩放参数 和 裁剪参数不同, 这里使用的是 yongyi-view 的参数
                    	topLeftPointX /= scaleRate;
                    	topLeftPointY /= scaleRate;
                    	width /= scaleRate;
                    	height /= scaleRate;

                    	int x1 = (int)(topLeftPointX);
                    	int y1 = (int)(topLeftPointY);
                    	int boxWidth = (int)(width);
                    	int boxHeight = (int)(height);
                    	int x2 = x1 + boxWidth - 1;
                    	int y2 = y1 + boxHeight - 1;
                
                    	//如果框和 video 对应帧相交的面积还不到原来的一半，那么抛弃此记录, 否则按相交部分写入
                        if(x2 < leftMostColumnIndex || x1 > rightMostColumnIndex)
                        {}
                        else
                        {
                            //左边在范围内
                            if(x1 >= leftMostColumnIndex && x1 <= rightMostColumnIndex)
                            {
                                //右边也在范围内
                                if(x2 >= leftMostColumnIndex && x2 <= rightMostColumnIndex)
                                {
                                    //向左移动 leftMostColumnIndex (97) 个 pixel 写入 txt
                                    destWriter.WriteLine(String.Format("{0} {1} {2} {3} {4} {5}",frameNumberInStr,
                                        personIdInStr,x1-leftMostColumnIndex,y1,boxWidth,boxHeight));
                                }
                                //右边不在范围内
                                else
                                {
                                    //若在范围内的 box 宽 至少为原来的一半, 则写入, 否则抛弃
                                    if(rightMostColumnIndex-x1+1 >= width/2)
                                    {
                                        //向左移动 leftMostColumnIndex (97) 个 pixel 写入 txt
                                        destWriter.WriteLine(String.Format("{0} {1} {2} {3} {4} {5}", frameNumberInStr,
                                            personIdInStr, x1-leftMostColumnIndex, y1, rightMostColumnIndex - x1 + 1, boxHeight));
                                    }
                                }
                            }
                            //左边不在范围内, 则右边必在范围内, 否则不会进入此分支
                            else
                            {
                                //若在范围内的 box 宽 至少为原来的一半, 则写入, 否则抛弃
                                if(x2-leftMostColumnIndex+1>=width/2)
                                {
                                    //向左移动 leftMostColumnIndex (97) 个 pixel 写入 txt
                                    destWriter.WriteLine(String.Format("{0} {1} {2} {3} {4} {5}", frameNumberInStr,
                                            personIdInStr, leftMostColumnIndex - leftMostColumnIndex, y1, x2 - leftMostColumnIndex + 1, boxHeight));
                                }
                            }
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            String videoName = String.Empty;
            //video 1~6
            for (int i = 1; i <= 6; ++i)
            {
                ResizeBoundingBoxesToNewVideo("video0" + i.ToString());
            }
            //video 15~95
            for (int i = 15; i <= 95; ++i)
            {
                ResizeBoundingBoxesToNewVideo("video" + i.ToString());
            }
        }
    }
}
