//------------------------------------------------------------------------------
// <copyright file="MonitorFramesData.cs" Lab="isee" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FroggyNect
{
    public partial class MainWindow
    {
        private WriteableBitmap colorBitmap = null;

        private WriteableBitmap depthBitmap = null;

        /// <summary>
        /// Initialize the color bitmap and depth bitmap to display later
        /// </summary>
        private void InitializePart_DisplayingImgsHandler()
        {
            colorBitmap = new WriteableBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Bgr32, null);
            // Not tempting to show the real 16-bits depth bitmaps but 8-bits ones which are easier to observe by naked eyes
            // 真正存储的是 16 位深度图, 这里的深度图是8位的用于显示到界面上, 因为 16 位的深度图太漆黑了, 根本看不清
            depthBitmap = new WriteableBitmap(512, 424, 96.0, 96.0, PixelFormats.Gray8, null);
        }

        /// <summary>
        /// Display color image on image control
        /// </summary>
        /// <param name="bitmap">color bitmap</param>
        /// <param name="image">color image control</param>
        /// <param name="args">frame args</param>
        private void RenderColorImage(ref WriteableBitmap bitmap,
            System.Windows.Controls.Image image, ColorFrameArrivedEventArgs args)
        {
            using (ColorFrame colorFrame = args.FrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }
                else
                {
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        bitmap.Lock();
                        colorFrame.CopyConvertedFrameDataToIntPtr(
                            bitmap.BackBuffer,
                            (uint)(1920 * 1080 * 4),
                            ColorImageFormat.Bgra);
                        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                        bitmap.Unlock();

                        image.Source = bitmap;
                    }
                }
            }
        }

        /// <summary>
        /// Display depth image on image control
        /// </summary>
        /// <param name="bitmap">depth bitmap</param>
        /// <param name="image">depth image control</param>
        /// <param name="args">frame args</param>
        private unsafe void RenderDepthImage(ref WriteableBitmap bitmap,
            System.Windows.Controls.Image image, DepthFrameArrivedEventArgs args)
        {
            using (DepthFrame depthFrame = args.FrameReference.AcquireFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }
                else
                {
                    using(KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        Byte[] depthPixels = new Byte[512 * 424];
                        ushort* frameData = (ushort*)depthBuffer.UnderlyingBuffer;
                        ushort minDepth = depthFrame.DepthMinReliableDistance;
                        ushort maxDepth = ushort.MaxValue;
                        const int mapDepthToByte = 8000 / 256;

                        //convert depth to a visual respresention
                        //如果不把循环加入 Parallel 异步循环块的话, 直接写循环会卡死整个 UI界面
                        Parallel.For(
                            0,
                            (int)depthBuffer.Size / depthFrame.FrameDescription.BytesPerPixel,
                            i=>
                            {
                                ushort depth = frameData[i];
                                depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / mapDepthToByte) : 0);
                            });

                        //Rendering
                        bitmap.WritePixels(
                            new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                            depthPixels,
                            bitmap.PixelWidth,
                            0);
                        image.Source = bitmap;
                    }
                }
            }
        }

        /// <summary>
        /// Display skeleton image on image control
        /// </summary>
        /// <param name="image">skeleton image control</param>
        /// <param name="args">args</param>
        private void RenderSkeletonImage(System.Windows.Controls.Image image, 
            BodyFrameArrivedEventArgs args)
        { 
            using(BodyFrame bodyFrame = args.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }
                else
                {
                    if (this.bodies == null)
                    { 
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    using (DrawingContext dc = this.drawingGroup.Open())
                    {
                        // Draw a transparent background to set the render size
                        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                        int penIndex = 0;
                        foreach (Body body in this.bodies)
                        {
                            Pen drawPen = this.bodyColors[penIndex++];

                            if (body.IsTracked)
                            {
                                this.DrawClippedEdges(body, dc);

                                IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                // convert the joint points to depth (display) space
                                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                                foreach (JointType jointType in joints.Keys)
                                {
                                    // sometimes the depth(Z) of an inferred joint may show as negative
                                    // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                    CameraSpacePoint position = joints[jointType].Position;
                                    if (position.Z < 0)
                                    {
                                        position.Z = InferredZPositionClamp;
                                    }

                                    DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(position);
                                    jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                }

                                this.DrawBody(joints, jointPoints, dc, drawPen);

                                this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                            }
                        }

                        // prevent drawing outside of our render area
                        this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                        Vector4 floor = bodyFrame.FloorClipPlane;

                        this.floorTextBlock.Text = String.Format("({0}, {1}, {2}, {3})", floor.W, floor.X, floor.Y, floor.Z);
                        this.floorTextBlock.TextAlignment = TextAlignment.Center;

                        this.skeletonImage.Source = this.skeletonImageSource;
                    }
                }
            }
        }

        /// <summary>
        /// Dispatcher to update the color image control
        /// 用 Dispatcher 更新主线程 (UI 线程) 组件
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Color_ShowImage(Object sender, ColorFrameArrivedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(
                (Action)
                (() => this.RenderColorImage(ref this.colorBitmap, this.colorImage, e))
                );
        }

        /// <summary>
        /// Dispatcher to update the depth image control
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Depth_ShowImage(Object sender, DepthFrameArrivedEventArgs e)
        {
            Console.WriteLine("Just for test !");
            this.Dispatcher.BeginInvoke(
                (Action)
                (() => this.RenderDepthImage(ref this.depthBitmap, this.depthImage, e))
            );
        }

        /// <summary>
        /// Dispatcher to update the skeleton(body) image control
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Skeleton_ShowImage(Object sender, BodyFrameArrivedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(
                (Action)
                (() => this.RenderSkeletonImage(this.skeletonImage, e))
            );
        }
    }
}
