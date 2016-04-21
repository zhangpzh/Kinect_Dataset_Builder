//------------------------------------------------------------------------------
// <copyright file="KinectReader.cs" Lab="isee" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace FroggyNect
{
    public partial class MainWindow
    {
        /// <summary>
        /// Kinect sensor
        /// </summary>
        public static KinectSensor kinectSensor = null;
        /// <summary>
        /// Coordinate mapper used to map points among three spaces (camera space, color space and depth space)
        /// </summary>
        public static CoordinateMapper coordinateMapper = null;

        // Frame readers
        private static ColorFrameReader m_ColorFrameReader = null;
        private static DepthFrameReader m_DepthFrameReader = null;
        private static BodyFrameReader m_BodyFrameReader = null;
        private static BodyIndexFrameReader m_BodyIndexFrameReader = null;
        private static InfraredFrameReader m_InfraredFrameReader = null;

        /// <summary>
        /// Open readers
        /// </summary>
        private static void OpenFrameReaders()
        {
            // Open readers
            m_ColorFrameReader = kinectSensor.ColorFrameSource.OpenReader();
            m_DepthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
            m_BodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
            m_BodyIndexFrameReader = kinectSensor.BodyIndexFrameSource.OpenReader();
            m_InfraredFrameReader = kinectSensor.InfraredFrameSource.OpenReader();
        }

        /// <summary>
        /// Show three kinds of image information(here are the color, depth and skeleton) onto three image controls
        /// 在三个 Image 控件上显示三种图片, 这会在主线程派生的子线程上运行, 对于并行线程数较少的机器, 还是尽量少选一些显示。
        /// 因为这些线程会和五条存储线程竞争, 争夺 CPU 时间, 使得 CPU 来回切换, 增大开销. 最终影响存储性能
        /// </summary>
        private void RegisterMonitors()
        {
            // Display images
            if(true == this.depthCheckBox.IsChecked)
            {
                m_DepthFrameReader.FrameArrived += this.Depth_ShowImage;
            }
            if(true == this.colorCheckBox.IsChecked)
            {
                m_ColorFrameReader.FrameArrived += this.Color_ShowImage;
            }
            if(true == this.skeletonCheckBox.IsChecked)
            {
                m_BodyFrameReader.FrameArrived += this.Skeleton_ShowImage;
            }
        }

        /// <summary>
        /// Register events' enqueueing method
        /// </summary>
        private static void RegisterEnqueueEvents()
        {
            m_ColorFrameReader.FrameArrived +=      Color_EnqueueEventArgs;
            m_DepthFrameReader.FrameArrived +=      Depth_EnqueueEventArgs;
            m_BodyFrameReader.FrameArrived +=       Body_EnqueueEventArgs;
            m_BodyIndexFrameReader.FrameArrived +=  BodyIndex_EnqueueEventArgs;
            m_InfraredFrameReader.FrameArrived +=   Infrared_EnqueueEventArgs;
        }

        /// <summary>
        /// Log off events' enqueueing method to stop receiving new frames
        /// </summary>
        private static void LogoffEnqueueEvents()
        {
            m_ColorFrameReader.FrameArrived -= Color_EnqueueEventArgs;
            m_DepthFrameReader.FrameArrived -= Depth_EnqueueEventArgs;
            m_BodyFrameReader.FrameArrived -= Body_EnqueueEventArgs;
            m_BodyIndexFrameReader.FrameArrived -= BodyIndex_EnqueueEventArgs;
            m_InfraredFrameReader.FrameArrived -= Infrared_EnqueueEventArgs; 
        }
    }
}
