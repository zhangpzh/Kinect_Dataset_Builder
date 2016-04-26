//------------------------------------------------------------------------------
// <copyright file="HandleFramesData.cs" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Threading;

namespace FroggyNect
{
    // Five methods bind to five storing threads. Only the color handling method has annotation to which the other handling methods's are similar
    // 5 个存储线程的方法. 只有存储彩色图的方法才有注释, 其他帧种的方法的语句都和这个方法类似, 就不再重复注释了

    public partial class MainWindow
    {
        /// <summary>
        /// Handles color
        /// </summary>
        /// <param name="colorCollectedCancelTokenSource">cancelTokenSource used to stop the task</param>
        private static void HandleColor(CancellationTokenSource colorCollectedCancelTokenSource)
        {
            ColorFrameArrivedEventArgs e = null;
            String frameNumber = String.Empty;
            ColorFrame colorFrame;
            while (true)
            {
                colorFrame = null;
                // Whether task is requested to be canceled or not
                // 检查线程是否被请求中止
                if(colorCollectedCancelTokenSource.IsCancellationRequested)
                {
                    break;
                }

                // Queue not empty
                // 若队列不空
                if (colorFrameQueue.Count != 0)
                {
                    // Only one thread is allowed to access the global frame information object each time
                    // 加互斥锁, 一次只允许一个线程访问
                    lock (FramesAndPaths.allFrameInfo)
                    {
                        // The frame information has already been written to disk this round, continue
                        // 若本回写过, 则下一次循环
                        if ((FramesAndPaths.allFrameInfo.allFrameFlag & 1) != 0)
                        {
                            continue;
                        }
                        // Fetch a frame
                        try
                        {
                            e = colorFrameQueue.Dequeue();
                        }
                        catch (InvalidOperationException)
                        {
                            continue;
                        }
                        try
                        {
                            colorFrame = e.FrameReference.AcquireFrame();
                        }
                        catch (NullReferenceException)
                        { }
                    
                        // Null frame, continue
                        if (colorFrame == null)
                        {
                            continue;
                        }
                        // Fetch global frame number
                        // 拿帧号
                        frameNumber = FramesAndPaths.allFrameInfo.frameNumber;

                        // Label current frame to be written this around
                        // 标记 rgb 帧已写
                        FramesAndPaths.allFrameInfo.allFrameFlag |= 1;

                        // All kinds of frame information have been written to disk
                        // 本回各种帧都已写完
                        if ((FramesAndPaths.allFrameInfo.allFrameFlag ^ 31) == 0)
                        {
                            // Set global flag to zero
                            // 全局flag置0
                            FramesAndPaths.allFrameInfo.allFrameFlag = 0;

                            // Increment the frame number
                            // frameNumber 增加
                            FramesAndPaths.FrameNumberIncrement();

                            // Increment the number of series of frames this program processes this second
                            ++writtenCount;
                        }
                    }
                    // Write to disk
                    // 写图
                    StoreFramesData.Handle_ColorFrame(colorFrame, frameNumber);
                }
            }
        }

        /// <summary>
        /// Handles depth
        /// </summary>
        /// <param name="depthCollectedCancelTokenSource">cancelTokenSource used to stop the task</param>
        private static void HandleDepth(CancellationTokenSource depthCollectedCancelTokenSource)
        {
            DepthFrameArrivedEventArgs e = null;
            String frameNumber = String.Empty;
            DepthFrame depthFrame;
            while (true)
            {
                depthFrame = null;
                if(depthCollectedCancelTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (depthFrameQueue.Count != 0)
                {
                    lock (FramesAndPaths.allFrameInfo)
                    {
                        if ((FramesAndPaths.allFrameInfo.allFrameFlag & 2) != 0)
                        {
                            continue;
                        }
                        try
                        {
                            e = depthFrameQueue.Dequeue();
                        }
                        catch (InvalidOperationException)
                        {
                            continue;
                        }
                        try
                        {
                            depthFrame = e.FrameReference.AcquireFrame();
                        }
                        catch(NullReferenceException)
                        {}

                        if (depthFrame == null)
                        {
                            continue;
                        }
                        frameNumber = FramesAndPaths.allFrameInfo.frameNumber;
                        FramesAndPaths.allFrameInfo.allFrameFlag |= 2;

                        if ((FramesAndPaths.allFrameInfo.allFrameFlag ^ 31) == 0)
                        {
                            FramesAndPaths.allFrameInfo.allFrameFlag = 0;
                            FramesAndPaths.FrameNumberIncrement();
                            ++writtenCount;
                        }
                    }
                    StoreFramesData.Handle_DepthFrame(depthFrame, frameNumber);
                }
            }
        }

        /// <summary>
        /// Handles body(skeleton)
        /// </summary>
        /// <param name="bodyCollectedCancelTokenSource">cancelTokenSource used to stop the task</param>
        private static void HandleBody(CancellationTokenSource bodyCollectedCancelTokenSource)
        {
            BodyFrameArrivedEventArgs e = null;
            String frameNumber = String.Empty;
            BodyFrame bodyFrame;
            while (true)
            {
                bodyFrame = null;
                if(bodyCollectedCancelTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (bodyFrameQueue.Count != 0)
                {
                    lock (FramesAndPaths.allFrameInfo)
                    {
                        if ((FramesAndPaths.allFrameInfo.allFrameFlag & 4) != 0)
                        {
                            continue;
                        }

                        try
                        {
                            e = bodyFrameQueue.Dequeue();
                        }
                        catch (InvalidOperationException)
                        {
                            continue;
                        }
                        try
                        {
                            bodyFrame = e.FrameReference.AcquireFrame();
                        }
                        catch(NullReferenceException)
                        {}

                        if (bodyFrame == null)
                        {
                            continue;
                        }

                        frameNumber = FramesAndPaths.allFrameInfo.frameNumber;
                        FramesAndPaths.allFrameInfo.allFrameFlag |= 4;

                        if ((FramesAndPaths.allFrameInfo.allFrameFlag ^ 31) == 0)
                        {
                            FramesAndPaths.allFrameInfo.allFrameFlag = 0;
                            FramesAndPaths.FrameNumberIncrement();
                            ++writtenCount;
                        }
                    }
                    StoreFramesData.Handle_BodyFrame(bodyFrame, frameNumber);
                }
            }
        }

        /// <summary>
        /// Handles body index
        /// </summary>
        /// <param name="bodyIndexCollectedCancelTokenSource">cancelTokenSource used to stop the task</param>
        private static void HandleBodyIndex(CancellationTokenSource bodyIndexCollectedCancelTokenSource)
        {
            BodyIndexFrameArrivedEventArgs e = null;
            String frameNumber = String.Empty;
            BodyIndexFrame bodyIndexFrame;
            while (true)
            {
                bodyIndexFrame = null;
                if (bodyIndexCollectedCancelTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (bodyIndexFrameQueue.Count != 0)
                {
                    lock (FramesAndPaths.allFrameInfo)
                    {
                        if ((FramesAndPaths.allFrameInfo.allFrameFlag & 8) != 0)
                        {
                            continue;
                        }
                        try
                        {
                            e = bodyIndexFrameQueue.Dequeue();
                        }
                        catch (InvalidOperationException)
                        {
                            continue;
                        }
                        try
                        {
                            bodyIndexFrame = e.FrameReference.AcquireFrame();
                        }
                        catch (NullReferenceException)
                        { }

                        if (bodyIndexFrame == null)
                        {
                            continue;
                        }

                        frameNumber = FramesAndPaths.allFrameInfo.frameNumber;
                        FramesAndPaths.allFrameInfo.allFrameFlag |= 8;

                        if ((FramesAndPaths.allFrameInfo.allFrameFlag ^ 31) == 0)
                        {
                            FramesAndPaths.allFrameInfo.allFrameFlag = 0;
                            FramesAndPaths.FrameNumberIncrement();
                            ++writtenCount;
                        }
                    }
                    StoreFramesData.Handle_BodyIndexFrame(bodyIndexFrame, frameNumber);
                }
            }
        }

        /// <summary>
        /// Handles infrared
        /// </summary>
        /// <param name="infraredCollectedCancelTokenSource">cancelTokenSource used to stop the task</param>
        private static void HandleInfrared(CancellationTokenSource infraredCollectedCancelTokenSource)
        {
            InfraredFrameArrivedEventArgs e = null;
            String frameNumber = String.Empty;
            InfraredFrame infraredFrame;
            while (true)
            {
                infraredFrame = null;
                if (infraredCollectedCancelTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (infraredFrameQueue.Count != 0)
                {
                    lock (FramesAndPaths.allFrameInfo)
                    {
                        if ((FramesAndPaths.allFrameInfo.allFrameFlag & 16) != 0)
                        {
                            continue;
                        }
                        try
                        {
                            e = infraredFrameQueue.Dequeue();
                        }
                        catch(InvalidOperationException)
                        {
                            continue;
                        }
                        try
                        {
                            infraredFrame = e.FrameReference.AcquireFrame();
                        }
                        catch (NullReferenceException)
                        { }

                        if (infraredFrame == null)
                        {
                            continue;
                        }

                        frameNumber = FramesAndPaths.allFrameInfo.frameNumber;
                        FramesAndPaths.allFrameInfo.allFrameFlag |= 16;

                        if ((FramesAndPaths.allFrameInfo.allFrameFlag ^ 31) == 0)
                        {
                            FramesAndPaths.allFrameInfo.allFrameFlag = 0;
                            FramesAndPaths.FrameNumberIncrement();
                            ++writtenCount;
                        }
                    }
                    StoreFramesData.Handle_InfraredFrame(infraredFrame, frameNumber);
                }
            }
        }
    }
}
