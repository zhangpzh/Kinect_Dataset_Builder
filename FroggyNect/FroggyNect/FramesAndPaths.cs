//------------------------------------------------------------------------------
// <copyright file="FramesAndPaths.cs" Lab="isee" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FroggyNect
{
    /// <summary>
    /// Global frame information class
    /// 全局帧信息
    /// </summary>
    public class AllFrameInfos
    {
        /// <summary>
        /// Global frame number
        /// </summary>
        public String frameNumber = new String(new char[] { '0', '0', '0', '0', '1' });

        /// <summary>
        /// frame flag used for judging whether a specific kind of frame has been written this round
        /// </summary>
        public byte allFrameFlag = 0;
    }
    /* (00000001  & allFrameFlag) != 0  标记 本回 color 已写        ( color has already been written this round)
     * (00000010  & allFrameFlag) != 0 	标记 本回 depth 已写        ( depth has already been written this round)
     * (00000100  & allFrameFlag) != 0 	标记 本回 body 已写         ( body has already been written this round)
     * (00001000  & allFrameFlag) != 0 	标记 本回 body index 已写   ( body index has already been written this round)
     * (00010000  & allFrameFlag) != 0 	标记 本回 infrared 已写     ( infrared has already been written this round)
     * (00011111  & allFrameFlag) != 0 	标记 本回 所有帧 已写        (all have already been written this round)
     * */

    /// <summary>
    /// Frame operation class
    /// 帧号, 路径的操作类
    /// </summary>
    public static class FramesAndPaths
    {
        // Dataset's root
        // 数据库根目录
        private static String datasetRootDirectory = @"K:\\RGBD_dataset\\";
        
        /// <summary>
        /// folder name
        /// </summary>
        public static String[] fileCategories = { "\\ColorImage\\", "\\DepthImage\\", "\\SkeletonInfo\\", "\\BodyIndexImage\\", "\\InfraredImage\\" };

        /// <summary>
        /// Global frame information object, used for synchronizing five tasks' access to it
        /// 全局信号量, 用于同步多线程对全局唯一帧号的访问
        /// </summary>
        public static AllFrameInfos allFrameInfo = null;

        // Current recording video number
        private static String videoNumber = "video01";
        private static int currentVideoNumber = 1;

        // Total number of videos in the dataset
        private static int videoCount = 0;

        // Two dictionaries used for acquiring next frame number
        private static Dictionary<int, char> intToChar = new Dictionary<int, char>();
        private static Dictionary<char, int> charToInt = new Dictionary<char, int>();

        /// <summary>
        /// file type enumeration
        /// </summary>
        public enum FileType { ColorImage, DepthImage, SkeletonInfo, BodyIndexImage, InfraredImage };

        /// <summary>
        /// Initialize the global frame data
        /// </summary>
        public static void initializeFrameData()
        {
            // Initialize the two dictionaries
            char[] alpha = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            for (int i = 0; i < 10; ++i)
            {
                intToChar.Add(i, alpha[i]);
                charToInt.Add(alpha[i], i);
            }

            allFrameInfo = new AllFrameInfos();
        }

        /// <summary>
        /// Set path of dataset root directory
        /// </summary>
        /// <param name="str">path</param>
        public static void SetDatasetRootDirectory(String str)
        {
            datasetRootDirectory = (str + "\\");
        }

        /// <summary>
        /// Get path of dataset root directory
        /// </summary>
        /// <returns></returns>
        public static String GetDatasetRootDirectory()
        {
            return datasetRootDirectory;
        }

        /// <summary>
        /// Set total video count
        /// </summary>
        /// <param name="cnt">total video count</param>
        public static void SetVideoCount(int cnt)
        {
            videoCount = cnt;
        }

        /// <summary>
        /// Get total video count
        /// </summary>
        /// <returns></returns>
        public static int GetVideoCount()
        {
            return videoCount;
        }

        /// <summary>
        /// Reset global frame info object
        /// </summary>
        public static void ResetAllFrameInfos()
        {
            allFrameInfo = new AllFrameInfos();
        }

        /// <summary>
        /// Update current video number according to num
        /// </summary>
        /// <param name="num">current video number to record</param>
        public static void RefreshCurrentVideoNumber(int num)
        {
            currentVideoNumber = num;
            videoNumber = "video" + (currentVideoNumber < 10 ? "0" : "") + currentVideoNumber.ToString();
        }

        /// <summary>
        /// Increment the global frame number
        /// 增加全局帧号
        /// </summary>
        public static void FrameNumberIncrement()
        {
            bool hasNext = true;
            StringBuilder sb = new StringBuilder(allFrameInfo.frameNumber);
            // Five digits
            // 帧号有5位
            for (int i = 4; i >= 0; --i)
            {
                if (hasNext == false)
                {
                    break;
                }
                if (sb[i].Equals('9'))
                {
                    sb[i] = '0';
                    hasNext = true;
                }
                else
                {
                    sb[i] = intToChar[charToInt[sb[i]] + 1];
                    break;
                }
            }
            allFrameInfo.frameNumber = sb.ToString();
        }

        /// <summary>
        /// Get image files' storing path
        /// 获得图片的存储路径
        /// </summary>
        /// <param name="enum_ImageType"></param>
        /// <param name="argFrameNumber"></param>
        /// <returns></returns>
        public static String GetImageFilePath(FileType enum_ImageType, String argFrameNumber)
        {
            String path = datasetRootDirectory + videoNumber + fileCategories[(int)enum_ImageType] + argFrameNumber;
            return path;
        }

        /// <summary>
        /// Get skeleton information's storing path
        /// 获得骨骼信息文本文件的存储路径
        /// </summary>
        /// <param name="enum_SkeletonType"></param>
        /// <param name="argFrameNumber"></param>
        /// <returns></returns>
        public static String GetSkeletonFilePath(FileType enum_SkeletonType, String argFrameNumber)
        {
            String path = datasetRootDirectory + videoNumber + fileCategories[(int)enum_SkeletonType] + argFrameNumber;
            return path;
        }
    }
}
