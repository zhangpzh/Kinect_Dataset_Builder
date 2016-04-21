//------------------------------------------------------------------------------
// <copyright file="StoreFramesData.cs" Lab="isee" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Coding4Fun.Kinect.Wpf;
using System.IO;

namespace FroggyNect
{
    /// <summary>
    /// Static image operation class
    /// 静态图像操作类
    /// </summary>
    public static class StoreFramesData
    {
        //It's faster for this program to run with certain frame parameters compared with acquiring them through programming
        /* 每一种帧的固有大小, 可以从 kinect 传感器获得. 但是手动标出来可以让程序快一些 */

        /// <summary>
        /// Color frame width
        /// </summary>
        public const int colorWidth = 1920;
        /// <summary>
        /// Color frame height
        /// </summary>
        public const int colorHeight = 1080;

        /// <summary>
        /// Depth frame width
        /// </summary>
        public const int depthWidth = 512;
        /// <summary>
        /// Depth frame height
        /// </summary>
        public const int depthHeight = 424;

        /// <summary>
        /// Body index frame width
        /// </summary>
        public const int bodyIndexWidth = 512;
        /// <summary>
        /// Body index frame height
        /// </summary>
        public const int bodyIndexHeight = 424;

        /// <summary>
        /// Infrared frame width
        /// </summary>
        public const int infraredWidth = 512;
        /// <summary>
        /// Infrared frame height
        /// </summary>
        public const int infraredHeight = 424;

        private static byte[] bgraColor = new byte[colorWidth * colorHeight << 2];

        /// <summary>
        /// Store color image
        /// </summary>
        /// <param name="colorFrame">color frame to be stored</param>
        /// <param name="frameNumber">frame number</param>
        public static void Handle_ColorFrame(ColorFrame colorFrame, String frameNumber)
        {
            colorFrame.CopyConvertedFrameDataToArray(bgraColor, ColorImageFormat.Bgra);
            BitmapSource bitmapSource = BitmapSource.Create(colorWidth, colorHeight, 96.0, 96.0,
                            PixelFormats.Bgra32, null, bgraColor, colorWidth << 2);
            String colorPath = FramesAndPaths.GetImageFilePath(FramesAndPaths.FileType.ColorImage, frameNumber);
            bitmapSource.Save(colorPath + ".jpg", ImageFormat.Jpeg);

            // Release colorFrame
            colorFrame.Dispose();
        }


        /// <summary>
        /// Store depth image
        /// </summary>
        /// <param name="depthFrame">depth frame to be stored</param>
        /// <param name="frameNumber">frame number</param>
        public static void Handle_DepthFrame(DepthFrame depthFrame, String frameNumber)
        {
            using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
            {
                BitmapSource bitmapSource = BitmapSource.Create(depthWidth, depthHeight, 96.0, 96.0,
                    PixelFormats.Gray16, null, depthBuffer.UnderlyingBuffer, (int)depthBuffer.Size, depthWidth << 1);

                String depthPath = FramesAndPaths.GetImageFilePath(FramesAndPaths.FileType.DepthImage, frameNumber);
                bitmapSource.Save(depthPath + ".png", ImageFormat.Png);
            }
            // Release depthFrame
            depthFrame.Dispose();
        }


        /// <summary>
        /// Store body index image
        /// </summary>
        /// <param name="bodyIndexFrame">body index frame to be stored</param>
        /// <param name="frameNumber">frame number</param>
        public static void Handle_BodyIndexFrame(BodyIndexFrame bodyIndexFrame, String frameNumber)
        {
            using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
            {
                BitmapSource bitmapSource = BitmapSource.Create(bodyIndexWidth, bodyIndexHeight, 96.0, 96.0,
                        PixelFormats.Gray8, null, bodyIndexBuffer.UnderlyingBuffer, (int)bodyIndexBuffer.Size, bodyIndexWidth * 1);
                String bodyIndexPath = FramesAndPaths.GetImageFilePath(FramesAndPaths.FileType.BodyIndexImage, frameNumber);
                bitmapSource.Save(bodyIndexPath + ".jpg", ImageFormat.Jpeg);
            }
            // Release bodyIndexFrame
            bodyIndexFrame.Dispose();
        }


        private static CameraSpacePoint[] cameraSpacePositions = new CameraSpacePoint[25]; //25个 JointType 枚举量
        private static ColorSpacePoint[] colorSpacePositions = new ColorSpacePoint[25];
        private static DepthSpacePoint[] depthSpacePositions = new DepthSpacePoint[25];
        private static Vector4[] orientations = new Vector4[25];

        //constant for clamping Z values of camera space points from being negative
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Store body(skeleton) information
        /// </summary>
        /// <param name="bodyFrame">body(skeleton) information to be stored</param>
        /// <param name="frameNumber">frame number</param>
        public static void Handle_BodyFrame(BodyFrame bodyFrame, String frameNumber)
        {
            String skeletonInfoPath = FramesAndPaths.GetSkeletonFilePath(FramesAndPaths.FileType.SkeletonInfo, "SkeletonInfo.txt");
            try
            {
                using (StreamWriter skeletonWriter = new StreamWriter(skeletonInfoPath, true))
                {
                    Body[] bodies = new Body[bodyFrame.BodyCount];

                    bodyFrame.GetAndRefreshBodyData(bodies);

                    // A string to store all the skeletons' information in this frame
                    // 要写入 txt 文件的本帧所有骨骼信息
                    String peopleInfo = String.Empty;

                    foreach (Body body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                            IReadOnlyDictionary<JointType, JointOrientation> jointOrientations = body.JointOrientations;

                            // Acquire coordinates on camera space
                            // 获得 camera space 上关节点的三维坐标
                            int jointIndex = 0;
                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an) inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                cameraSpacePositions[jointIndex] = joints[jointType].Position;

                                if (cameraSpacePositions[jointIndex].Z < 0)
                                {
                                    cameraSpacePositions[jointIndex].Z = InferredZPositionClamp;
                                }
                                ++jointIndex;
                            }

                            // Acquire coordinates on color space
                            // 获得 color space 上的 25 个关节点的二维坐标
                            MainWindow.coordinateMapper.MapCameraPointsToColorSpace(cameraSpacePositions, colorSpacePositions);

                            // Acquire coordinates on depth space
                            // 获得 depth space 上的 25 个关节点的二维坐标
                            MainWindow.coordinateMapper.MapCameraPointsToDepthSpace(cameraSpacePositions, depthSpacePositions);

                            // Acquire orientation information
                            // 获得关节点的旋转信息
                            jointIndex = 0;
                            foreach (JointType jointType in jointOrientations.Keys)
                            {
                                JointOrientation tmpOrientation = jointOrientations[jointType];
                                orientations[jointIndex++] = tmpOrientation.Orientation;
                            }

                            // frame number、tracking % 6、floor
                            ulong resizeId = body.TrackingId % 6;
                            Vector4 floor = bodyFrame.FloorClipPlane;
                            //String personInfo = String.Format("{0}, id = {1}, color = {2},\r\n",GlobalData.FrameNumberIncrement(GlobalData.FileType.SkeletonInfo),resizeId,bodyColors[resizeId]);


                            // A string to store current skeleton's information in this frame
                            // 要加入 peopleInfo 的本帧当前一具骨骼信息 personInfo
                            String personInfo = String.Format("{0}, id = {1}, floor = {2} {3} {4} {5}", frameNumber, resizeId,
                                floor.W, floor.X, floor.Y, floor.Z);

                            personInfo += "\r\n";

                            // Append coordinates on camera space
                            // 相机空间三维坐标
                            personInfo += "\tcamera_space_coordinates =";
                            personInfo += String.Format(" {0} {1} {2}", cameraSpacePositions[0].X, cameraSpacePositions[0].Y, cameraSpacePositions[0].Z);
                            for (int i = 1; i < cameraSpacePositions.Length; ++i)
                            {
                                personInfo += String.Format(", {0} {1} {2}", cameraSpacePositions[i].X, cameraSpacePositions[i].Y, cameraSpacePositions[i].Z);
                            }
                            personInfo += "\r\n";

                            // Append coordiantes on color space
                            // rgb骨架二维坐标
                            personInfo += "\tcolor_skeleton_coordinates =";
                            personInfo += String.Format(" {0} {1}", colorSpacePositions[0].X, colorSpacePositions[0].Y);
                            for (int i = 1; i < colorSpacePositions.Length; ++i)
                            {
                                personInfo += String.Format(", {0} {1}", colorSpacePositions[i].X, colorSpacePositions[i].Y);
                            }
                            personInfo += "\r\n";

                            // Append coordinates on depth space
                            // depth骨架二维坐标
                            personInfo += "\tdepth_skeleton_coordinates =";
                            personInfo += String.Format(" {0} {1}", depthSpacePositions[0].X, depthSpacePositions[0].Y);
                            for (int i = 1; i < depthSpacePositions.Length; ++i)
                            {
                                personInfo += String.Format(", {0} {1}", depthSpacePositions[i].X, depthSpacePositions[i].Y);
                            }
                            personInfo += "\r\n";

                            personInfo += "\tskeleton_orientations =";
                            personInfo += String.Format(" {0} {1} {2} {3}", orientations[0].W, orientations[0].X, orientations[0].Y, orientations[0].Z);
                            for (int i = 1; i < orientations.Length; ++i)
                            {
                                personInfo += String.Format(", {0} {1} {2} {3}", orientations[i].W, orientations[i].X, orientations[i].Y, orientations[i].Z);
                            }
                            peopleInfo += personInfo;
                            peopleInfo += "\r\n";
                        }
                    }
                    skeletonWriter.Write(peopleInfo);
                }
            }
            catch (System.IO.IOException)
            { }
            // Release bodyFrame
            bodyFrame.Dispose();
        }


        /// <summary>
        /// Store infrared image
        /// </summary>
        /// <param name="infraredFrame">infrared frame to be stored</param>
        /// <param name="frameNumber">frame number</param>
        public static void Handle_InfraredFrame(InfraredFrame infraredFrame, String frameNumber)
        {
            using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
            {
                BitmapSource bitmapSource = BitmapSource.Create(infraredWidth, infraredHeight, 96.0, 96.0,
                    PixelFormats.Gray16, null, infraredBuffer.UnderlyingBuffer, (int)infraredBuffer.Size, infraredWidth << 1);

                String infraredPath = FramesAndPaths.GetImageFilePath(FramesAndPaths.FileType.InfraredImage, frameNumber);
                bitmapSource.Save(infraredPath + ".jpg", ImageFormat.Jpeg);
            }
            // Release infraredFrame
            infraredFrame.Dispose();
        }
    }
}