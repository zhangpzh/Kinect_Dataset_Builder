//This program is used to align image files among multiple views in which photos are from multiple kinects respectively

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace MultiViewAligner
{
    class Range
    {
        //Begin frame number and end frame number
        public int begin;
        public int end;
        public Range() { }
        public Range(int a, int b)
        {
            begin = a;
            end = b;
        }
    }

    class Program
    {
        private static String m_configureTxtPth = String.Empty;

        //Root directories' path under which, there are multi-views' sub-directories
        private static String m_sourceRootDirectPth = String.Empty;
        private static String m_destRootDirectPth = String.Empty;
        private static String[] m_viewNames;
        private static Range[] m_imageRanges;
        private static int m_viewNumber;
        private static String[] leadingZeros = { "", "0", "00", "000", "0000", "00000" };

        //Image categories to align, you can modify it
        private static String[] imageSubDirectoriesName = { "BodyIndexImage", "DepthImage", "InfraredImage" , "ColorImage"};


        static int Min(int a, int b)
        {
            if (a < b)
                return a;
            return b;
        }

        enum sourceOrDest: byte { source, dest};

        /// <summary>
        /// 获得所有view的, 当前记录的源video路径或者目的video路径
        /// </summary>
        /// <param name="enumValue">是源还是目的</param>
        /// <param name="videoName">video名称</param>
        /// <returns>所有view的, 当前记录的源video路径数组或者目的video路径数组</returns>
        static String[] GetFullVideoPths(sourceOrDest enumValue, String videoName)
        {
            String[] fullVideoPths = new String[m_viewNumber];
            String rootDirectPth = (enumValue == sourceOrDest.source ? m_sourceRootDirectPth : m_destRootDirectPth);

            for (int i = 0; i < m_viewNumber; ++i)
            {
                fullVideoPths[i] = System.IO.Path.Combine(rootDirectPth, m_viewNames[i], videoName);
            }
            return fullVideoPths;
        }


        /// <summary>
        /// 打乱列表中的元素
        /// </summary>
        /// <param name="list">输入列表</param>
        public static void Shuffle(List<Int32> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                --n;
                int k = rng.Next(n + 1);
                Int32 value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// 从给定帧范围中, 挑出给定数目的帧号 "xxxxx"
        /// </summary>
        /// <param name="beginNum">开始帧号</param>
        /// <param name="endNum">结束帧号</param>
        /// <param name="selectedNum">选择帧数</param>
        /// <returns></returns>
        static String[] GetSelectedFrames(Int32 beginNum, Int32 endNum, Int32 selectedNum)
        {
            List<Int32> selectedFrames = new List<Int32>();
            List<Int32> leftFrames = new List<Int32>();
            int i = beginNum;
            double coefficient = ((double)(endNum - beginNum + 1)) / selectedNum;
            int fixed_int_offset = (int)coefficient;
            double fixed_frac_offset = coefficient - (int)coefficient;
            double frac_offset = 0.0;

            int curSelectedCnt = 0, lastSelectedFrame;
            while (i <= endNum && curSelectedCnt < selectedNum)
            {
                selectedFrames.Add(i);
                lastSelectedFrame = i;
                ++curSelectedCnt;
                frac_offset += fixed_frac_offset;
                i += fixed_int_offset;
                if (frac_offset >= 1.0)
                {
                    frac_offset -= 1.0;
                    i += 1;
                }
                for (int j = lastSelectedFrame + 1; j < i && j <= selectedNum; ++j)
                {
                    leftFrames.Add(j);
                }
            }
            //对leftFrames执行 shuffle 算法
            Shuffle(leftFrames);

            //挑选出剩余的帧号
            for (i = 0; i < selectedNum - curSelectedCnt; ++i)
            {
                selectedFrames.Add(leftFrames[0]);
                leftFrames.RemoveAt(0);
            }

            //对选择的帧号从小到大进行排序
            selectedFrames.Sort();

            String[] selectedFrameStrings = new String[selectedFrames.Count];
            
            for (i = 0; i < selectedFrames.Count; ++i)
            {
                String tmpStr = selectedFrames[i].ToString();
                selectedFrameStrings[i] = leadingZeros[5 - tmpStr.Length] + tmpStr;
            }

            //返回所选择的帧号
            return selectedFrameStrings;
        }

        /// <summary>
        /// 从一个旧video采样到新video
        /// </summary>
        /// <param name="sourceVideoPath">源video路径</param>
        /// <param name="destVideoPath">目的video路径</param>
        /// <param name="beginNum">源video中采样开始帧的帧号</param>
        /// <param name="endNum">源video中采样结束帧的帧号</param>
        /// <param name="selectedNum">源video的采样帧数 (也是目的video中每种图片的总帧数)</param>
        static void SamplingASingleVideo(String sourceVideoPath, String destVideoPath, Int32 beginNum, Int32 endNum, Int32 selectedNum)
        {
            //从源video中采样所得的帧号
            String[] selectedFrameStrings = GetSelectedFrames(beginNum, endNum, selectedNum);

            //获得采样帧在源video中帧号到此帧在目的帧号的映射器 -> mapper
            Dictionary<String, String> mapper = new Dictionary<string, string>();
            int cnt = 0;
            foreach (String str in selectedFrameStrings)
            {
                ++cnt;
                String tmpStr = cnt.ToString();
                mapper[str] = leadingZeros[5 - tmpStr.Length] + tmpStr;
                //Console.WriteLine("{0} is re-mapped onto {1}", str, mapper[str]);
            }


            //将从源video中采样的图片复制到目的video中, 新的帧号由 mapper 决定
            for (int i = 0; i < imageSubDirectoriesName.Length; ++i)
            {
                String sourceDirects = System.IO.Path.Combine(sourceVideoPath, imageSubDirectoriesName[i]);
                String destDirects = System.IO.Path.Combine(destVideoPath, imageSubDirectoriesName[i]);

                foreach (String str in selectedFrameStrings)
                {
                    //其实这个列表中只有一个文件, 这样写为的是能够让这个模块能处理不同格式的图片(jpg, png ...)
                    List<String> sourceImageFilePths= new List<String>(Directory.EnumerateFiles(sourceDirects, String.Format("{0}.*", str)));
                    String sourceImageFilePth = sourceImageFilePths[0];
                    //文件后缀 -> ".xxxx"
                    String imageFileSuffix = sourceImageFilePth.Substring(sourceImageFilePth.LastIndexOf('.'));
                    //获得目的文件的存储路径
                    String destImageFilePth = System.IO.Path.Combine(destDirects, mapper[str] + imageFileSuffix);

                    //执行拷贝
                    File.Copy(sourceImageFilePth, destImageFilePth, true);

                }
                Console.WriteLine("\t\t{0} is completed !", imageSubDirectoriesName[i]);
            }

            //将从源video中采样的骨骼数据复制到目的video中, 新的帧号由 mapper 决定
            String sourceSkeletonInfoText = System.IO.Path.Combine(sourceVideoPath,"SkeletonInfo","SkeletonInfo.txt");
            String destSkeletonInfoText = System.IO.Path.Combine(destVideoPath, "SkeletonInfo", "SkeletonInfo.txt"); 

            using (StreamReader streamReader = new StreamReader(sourceSkeletonInfoText))
            {
                using (StreamWriter streamWriter = new StreamWriter(destSkeletonInfoText))
                {
                    String skeletonHeader;
                    String allInfo;
                    while ((skeletonHeader = streamReader.ReadLine()) != null)
                    {
                        allInfo = String.Empty;
                        allInfo = skeletonHeader + "\r\n";
                        //读取剩余4行
                        for (int i = 0; i < 4; ++i)
                        {
                            allInfo += streamReader.ReadLine();
                            allInfo += "\r\n";
                        }
                        //若该帧号是被选择的帧号, 则 remap 帧号 并把该条记录写入到目的路径的 txt 文件中
                        String frameNumber = skeletonHeader.Substring(0, 5);
                        if (mapper.ContainsKey(frameNumber))
                        {
                            streamWriter.Write(mapper[frameNumber] + allInfo.Substring(5));
                        }
                    }
                    Console.WriteLine("skeleton information is completed !");
                }
            }
        }

       
        /// <summary>
        /// 对齐几个 view 的源video, 对齐结果生成到 目的video中
        /// </summary>
        /// <param name="fullSourceVideoPths">所有view的源video路径</param>
        /// <param name="fullDestVideoPths">所有view的目的video路径</param>
        /// <param name="sampleImageNum">采样帧数</param>
        static void HandleAlignment(String[] fullSourceVideoPths, String[] fullDestVideoPths, int sampleImageNum)
        {
            for (int i = 0; i < m_viewNumber; ++i)
            {
                SamplingASingleVideo(fullSourceVideoPths[i], fullDestVideoPths[i], 
                    m_imageRanges[i].begin, m_imageRanges[i].end, sampleImageNum);
                Console.WriteLine("{0}\tfinished !",m_viewNames[i]);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Please input the configure text's path:  ");
            m_configureTxtPth = Console.ReadLine();

            String line;
            String[] entries;
            using(StreamReader sr = new StreamReader(m_configureTxtPth,Encoding.UTF8))
            {
                String pattern = "\\s+";
                String replacement = " ";

                //Read source and destination root directories' pathes of two multi-view datasets
                line = sr.ReadLine();
                //字符串替换, 把所有空白符替换成一个空格
                line = Regex.Replace(line, pattern, replacement);
                entries = line.Split(' ');
                m_sourceRootDirectPth = entries[0];
                m_destRootDirectPth = entries[1];


                //Read view number and corresponding views' names
                String m_viewNumberInStr = String.Empty;
                line = sr.ReadLine();
                //字符串替换, 把所有空白符替换成一个空格
                line = Regex.Replace(line, pattern, replacement);
                entries = line.Split(' ');
                int.TryParse(entries[0], out m_viewNumber);

                m_viewNames = new String[m_viewNumber];
                m_imageRanges = new Range[m_viewNumber];
                
                for (int i = 1; i <= m_viewNumber; ++i)
                {
                    m_viewNames[i-1] = entries[i];
                }

                //Read ranges and align
                String sourceVideoName = String.Empty;
                String destVideoName = String.Empty;
                int imageNumberToSampleFromVideos;
                while ((line = sr.ReadLine()) != null)
                {
                    //字符串替换, 把所有空白符替换成一个空格
                	line = Regex.Replace(line, pattern, replacement);
                    entries = line.Split(' ');
                    sourceVideoName = entries[0];
                    destVideoName = entries[1];
                    imageNumberToSampleFromVideos = 9999;

                    //Fill the ranges
                    for (int i = 2; i < entries.Length; ++i)
                    {
                        String rangeInStr = entries[i];
                        String[] beginAndEnd = rangeInStr.Split('-');

                        m_imageRanges[i - 2] = new Range();
                        int.TryParse(beginAndEnd[0], out m_imageRanges[i - 2].begin);
                        int.TryParse(beginAndEnd[1], out m_imageRanges[i - 2].end);
                        int rangeSize = m_imageRanges[i - 2].end - m_imageRanges[i - 2].begin + 1;

                        imageNumberToSampleFromVideos = Min(imageNumberToSampleFromVideos, rangeSize);
                    }

                    //Get full video paths of all views
                    String[] fullSourceVideoPths = GetFullVideoPths(sourceOrDest.source, sourceVideoName);
                    String[] fullDestVideoPths = GetFullVideoPths(sourceOrDest.dest, destVideoName);

                    Console.WriteLine("------------------------------");
                    Console.WriteLine("Source: {0}\tDestination: {1}\r\n", sourceVideoName, destVideoName);
                    Console.WriteLine("Sampling ranges:");
                    for (int i = 0; i < m_viewNumber; ++i)
                    {
                        Console.WriteLine("{0}: {1}--{2}",m_viewNames[i],m_imageRanges[i].begin,m_imageRanges[i].end);
                    }
                    Console.WriteLine("Selected frame count: {0}", imageNumberToSampleFromVideos);
                    Console.WriteLine("\r\nwait .......\r\n");
                    HandleAlignment(fullSourceVideoPths, fullDestVideoPths, imageNumberToSampleFromVideos);
                    Console.WriteLine("------------------------------\r\n");

                    //把取帧数写入 txt 文件中
                    using (StreamWriter sw = new StreamWriter(@"C:\Users\Zhang\Desktop\record.txt",true))
                    {
                        sw.WriteLine(imageNumberToSampleFromVideos);
                    }
                }
            }
        }
    }
}