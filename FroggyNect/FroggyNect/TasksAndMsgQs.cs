//------------------------------------------------------------------------------
// <copyright file="TasksAndMsgQs.cs" Lab="isee" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Kinect;

namespace FroggyNect
{
    public partial class MainWindow
    {
        // Five Message Queues corresponding to five categories of frame storing transcation

        /// <summary>
        /// Queue that stores color frame arrived events
        /// </summary>
        public static Queue<ColorFrameArrivedEventArgs> colorFrameQueue = new Queue<ColorFrameArrivedEventArgs>();
        /// <summary>
        /// Queue that stores depth frame arrived events
        /// </summary>
        public static Queue<DepthFrameArrivedEventArgs> depthFrameQueue = new Queue<DepthFrameArrivedEventArgs>();
        /// <summary>
        /// Queue that stores body frame arrived events
        /// </summary>
        public static Queue<BodyFrameArrivedEventArgs> bodyFrameQueue = new Queue<BodyFrameArrivedEventArgs>();
        /// <summary>
        /// Queue that stores body index frame arrived events
        /// </summary>
        public static Queue<BodyIndexFrameArrivedEventArgs> bodyIndexFrameQueue = new Queue<BodyIndexFrameArrivedEventArgs>();
        /// <summary>
        /// Queue that stores infrared frame arrived events
        /// </summary>
        public static Queue<InfraredFrameArrivedEventArgs> infraredFrameQueue = new Queue<InfraredFrameArrivedEventArgs>();

        // Five storing tasks
        Task colorTask = null;
        Task depthTask = null;
        Task bodyTask = null;
        Task bodyIndexTask = null;
        Task infraredTask = null;

        // Five CancellationTokenSource

        /// <summary>
        /// Color cancellation token source used to cancel the color task
        /// </summary>
        public static CancellationTokenSource colorCollectedCancellationTokenSource     = new CancellationTokenSource();
        /// <summary>
        /// Depth cancellation token source used to cancel the depth task
        /// </summary>
        public static CancellationTokenSource depthCollectedCancellationTokenSource     = new CancellationTokenSource();
        /// <summary>
        /// Body cancellation token source used to cancel the body task
        /// </summary>
        public static CancellationTokenSource bodyCollectedCancellationTokenSource      = new CancellationTokenSource();
        /// <summary>
        /// Body index cancellation token source used to cancel the body index task
        /// </summary>
        public static CancellationTokenSource bodyIndexCollectedCancellationTokenSource = new CancellationTokenSource();
        /// <summary>
        /// Infrared cancellation token source used to cancel the infrared task
        /// </summary>
        public static CancellationTokenSource infraredCollectedCancellationTokenSource  = new CancellationTokenSource();

        /// <summary>
        /// Clear five queues
        /// </summary>
        public static void ClearQueues()
        {
            colorFrameQueue.Clear();
            depthFrameQueue.Clear();
            bodyFrameQueue.Clear();
            bodyIndexFrameQueue.Clear();
            infraredFrameQueue.Clear();
        }

        //当帧来时, 参数进队, 等待诸线程获取

        /// <summary>
        /// Enqueue depth frame events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Depth_EnqueueEventArgs(Object sender, DepthFrameArrivedEventArgs e)
        {
            depthFrameQueue.Enqueue(e);
        }

        /// <summary>
        /// Enqueue color frame events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Color_EnqueueEventArgs(Object sender, ColorFrameArrivedEventArgs e)
        {
            colorFrameQueue.Enqueue(e);
        }

        /// <summary>
        /// Enqueue body frame events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Body_EnqueueEventArgs(Object sender, BodyFrameArrivedEventArgs e)
        {
            bodyFrameQueue.Enqueue(e);
        }

        /// <summary>
        /// Enqueue body index frame events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void BodyIndex_EnqueueEventArgs(Object sender, BodyIndexFrameArrivedEventArgs e)
        {
            bodyIndexFrameQueue.Enqueue(e);
        }

        /// <summary>
        /// Enqueue infrared frame events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Infrared_EnqueueEventArgs(Object sender, InfraredFrameArrivedEventArgs e)
        {
            infraredFrameQueue.Enqueue(e);
        }
    }
}
