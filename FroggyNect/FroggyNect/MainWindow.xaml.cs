//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" Lab="isee" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
//     Copyright (c) Peizhen Zhang.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Kinect;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace FroggyNect
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The timer to calculate written fps of this program which is shown on the main user interface at the same time
        /// 用于计算本程序写入帧率的计时器, 写入帧率会被显示在界面中
        /// </summary>
        private DispatcherTimer writtenFpsTimer;

        /// <summary>
        /// Record how many series of whole five categories of frame this program processes in one second
        /// 记录本程序一秒能处理多少套帧 (一套为5种不同帧各一帧)
        /// </summary>
        private static volatile int writtenCount;

        /// <summary>
        /// Timer stamp of last computation of written fps
        /// 上一次处理写入帧率时的时间戳
        /// </summary>
        private DateTime lastWrittenFpsTimerStamp;

        /// <summary>
        /// Main Window
        /// </summary>
        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();
            coordinateMapper = kinectSensor.CoordinateMapper;

            // Initialize the global frame information
            // 初始化帧相关的全局信息
            FramesAndPaths.initializeFrameData();

            // Initialize the components (controls) of the window
            // 初始化窗口组件
            this.InitializeComponent();

            // Use the window object as the view model in this simple example
            this.DataContext = this;

            this.InitializePart_DisplayingImgsHandler();
            this.InitializePart_DrawingSkeletonHandler();

            // Open readers
            OpenFrameReaders();

            // Show three kinds of image information(here are the color, depth and skeleton) onto three image controls
            //this.RegisterMonitors();

            // Open the kinect sensor
            kinectSensor.Open();

            // Initialize and start the written fps timer
            this.writtenFpsTimer = new DispatcherTimer();
            this.writtenFpsTimer.Tick += new EventHandler(this.WrittenFpsTimerTick);
            this.writtenFpsTimer.Interval = new TimeSpan(0,0,1);
            this.writtenFpsTimer.Start();
            this.lastWrittenFpsTimerStamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Handler for written fps timer tick event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WrittenFpsTimerTick(Object sender, EventArgs e)
        {
            // Calculate time span from last calculation of FPS
            double intervalSeconds = (DateTime.UtcNow - this.lastWrittenFpsTimerStamp).TotalSeconds;

            Decimal tmpDecimal = new Decimal((double)writtenCount/intervalSeconds);
            this.fpsTextBlock.Text = String.Format("{0}",Decimal.Round(tmpDecimal,2));

            // Reset frame counter
            writtenCount = 0;
            this.lastWrittenFpsTimerStamp = DateTime.UtcNow;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // 停止计时
            if (this.writtenFpsTimer != null)
            {
                this.writtenFpsTimer.Stop();
            }

            if (m_ColorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                m_ColorFrameReader.Dispose();
                m_ColorFrameReader = null;
            }

            if (m_DepthFrameReader != null)
            {
                // DepthFrameReder is IDisposable
                m_DepthFrameReader.Dispose();
                m_DepthFrameReader = null;
            }

            if (m_BodyFrameReader != null)
            {
                // BodyFrameReder is IDisposable
                m_BodyFrameReader.Dispose();
                m_BodyFrameReader = null;
            }

            if (m_BodyIndexFrameReader != null)
            {
                // BodyIndexFrameReder is IDisposable
                m_BodyIndexFrameReader.Dispose();
                m_BodyIndexFrameReader = null;
            }

            if (m_InfraredFrameReader != null)
            {
                // InfraredFrameReader is IDisposable
                m_InfraredFrameReader.Dispose();
                m_InfraredFrameReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        } 


        /// <summary>
        /// Handles the user clicking on the start button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            this.startButton.IsEnabled = false;
            this.stopButton.IsEnabled = true;
            this.incrementVideoBtn.IsEnabled = false;
            this.decrementVideoBtn.IsEnabled = false;

            // Register events' enqueueing method
            RegisterEnqueueEvents();


            // Handle cancellationTokenSource objects of five tasks
            if(colorCollectedCancellationTokenSource.IsCancellationRequested)
            {
                colorCollectedCancellationTokenSource.Dispose();
                colorCollectedCancellationTokenSource = new CancellationTokenSource();
            }
            if( depthCollectedCancellationTokenSource.IsCancellationRequested)
            {
                depthCollectedCancellationTokenSource.Dispose();
                depthCollectedCancellationTokenSource = new CancellationTokenSource();
            }
            if( bodyCollectedCancellationTokenSource.IsCancellationRequested)
            {
                bodyCollectedCancellationTokenSource.Dispose();
                bodyCollectedCancellationTokenSource = new CancellationTokenSource();
            }
            if( bodyIndexCollectedCancellationTokenSource.IsCancellationRequested)
            {
                bodyIndexCollectedCancellationTokenSource.Dispose();
                bodyIndexCollectedCancellationTokenSource = new CancellationTokenSource();
            }
            if (infraredCollectedCancellationTokenSource.IsCancellationRequested)
            {
                infraredCollectedCancellationTokenSource.Dispose();
                infraredCollectedCancellationTokenSource = new CancellationTokenSource();
            }

            // Start five tasks, which are running on five separate threads, to store frame information

            // Color
            if (colorTask != null && colorTask.IsCompleted)
            {
                colorTask.Dispose();
                colorTask = null;
            }
            colorTask = new Task(() => HandleColor(colorCollectedCancellationTokenSource));

            // Depth
            if (depthTask != null && depthTask.IsCompleted)
            {
                depthTask.Dispose();
                depthTask = null;
            }
            depthTask = new Task(() => HandleDepth(depthCollectedCancellationTokenSource));

            // Body
            if (bodyTask != null && bodyTask.IsCompleted)
            {
                bodyTask.Dispose();
                bodyTask = null;
            }
            bodyTask = new Task(() => HandleBody(bodyCollectedCancellationTokenSource));

            // BodyIndex
            if (bodyIndexTask != null && bodyIndexTask.IsCompleted)
            {
                bodyIndexTask.Dispose();
                bodyIndexTask = null;
            }
            bodyIndexTask = new Task(() => HandleBodyIndex(bodyIndexCollectedCancellationTokenSource));

            // Infrared
            if (infraredTask != null && infraredTask.IsCompleted)
            {
                infraredTask.Dispose();
                infraredTask = null;
            }
            infraredTask = new Task(() => HandleInfrared(infraredCollectedCancellationTokenSource));
            
            colorTask.Start();
            depthTask.Start();
            bodyTask.Start();
            bodyIndexTask.Start();
            infraredTask.Start();

        }

        /// <summary>
        /// Wait for the five queues to be empty by giving the five storing threads 5 more seconds
        /// </summary>
        private void stopButtonSleep()
        {
            Thread.Sleep(5000);
        }

        /// <summary>
        /// Handles the user clicking on the stop button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private async void stopButton_Click(object sender, RoutedEventArgs e)
        {
            this.stopButton.IsEnabled = false;

            // Log off events' enqueueing method to stop receiving new frames
            LogoffEnqueueEvents();

            String msg = "The \"start\" button is uable until the left images have already been written to disk !";
            System.Windows.MessageBox.Show(msg);

            await Task.Run((Action)(() => stopButtonSleep()));

            // Cancel five storing threads' task
            colorCollectedCancellationTokenSource.Cancel();
            depthCollectedCancellationTokenSource.Cancel();
            bodyCollectedCancellationTokenSource.Cancel();
            bodyIndexCollectedCancellationTokenSource.Cancel();
            infraredCollectedCancellationTokenSource.Cancel();

            // Clear five event args' queues
            // 清空队列
            ClearQueues();

            // Reset global frame information used for regularizing the frame number
            lock(FramesAndPaths.allFrameInfo)
            {
                FramesAndPaths.ResetAllFrameInfos();
            }

            this.startButton.IsEnabled = true;
            this.decrementVideoBtn.IsEnabled = true;
            this.incrementVideoBtn.IsEnabled = true;
        }


        /// <summary>
        /// Handles the user clicking on the create button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog selectFolderDialog = new FolderBrowserDialog();
            selectFolderDialog.ShowDialog();

            if(selectFolderDialog.SelectedPath != String.Empty)
            {
                // Set root directory of your dataset. SSD is prefered which supplies higher IO efficiency than a normal disk
                FramesAndPaths.SetDatasetRootDirectory(System.IO.Path.Combine(selectFolderDialog.SelectedPath,"KinectDataSet"));

                // Display the root directory of your dataset
                String str = FramesAndPaths.GetDatasetRootDirectory();
                this.datasetRootTextBlock.Text = str;
                
                // Ask video number
                int result = 0;
                while (result < 1 || result >= 100)
                {
                    // Input video number
                    String userInput = Microsoft.VisualBasic.Interaction.InputBox("video number: 1 to 99 is valid", "Please input the video number", "1");
                    int.TryParse(userInput, out result);
                }
                FramesAndPaths.SetVideoCount(result);
                videoTextBoxBorder.BorderBrush = Brushes.Blue;

                // Create file structure under the root directory of the dataset

                // Create root directory
                String root = FramesAndPaths.GetDatasetRootDirectory();
                System.IO.Directory.CreateDirectory(root);

                for(int i = 0 ; i < result ; ++ i)
                {
                    // Create video directories
                    String videoFolderPath = System.IO.Path.Combine(FramesAndPaths.GetDatasetRootDirectory(),(i < 9 ? "video0" : "video")+(i+1).ToString());
                    System.IO.Directory.CreateDirectory(videoFolderPath);

                    // Create image info sub-directories under the corresponding video directory
                    for (int j = 0; j < FramesAndPaths.fileCategories.Length; ++j)
                    {
                        String imagePath = videoFolderPath + FramesAndPaths.fileCategories[j];
                        System.IO.Directory.CreateDirectory(imagePath);
                    }
                    // Create Skeleton text under the SkeletonInfo folder to record skeleton information
                    String skeletonInfoPath = System.IO.Path.Combine(videoFolderPath,
                        "SkeletonInfo", "SkeletonInfo.txt");
                    System.IO.File.Create(skeletonInfoPath);
                }

                startButton.IsEnabled = true;
                createButton.IsEnabled = false;

                // Set videoText uneditable
                //videoText.IsEnabled = true;
                incrementVideoBtn.IsEnabled = true;
                decrementVideoBtn.IsEnabled = true;
            }
            // Release the dialog
            selectFolderDialog.Dispose();
        }

        /// <summary>
        /// Handles the user clicking on the increment button
        /// Select next video folder to record
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void incrementVideoBtn_Click(object sender, RoutedEventArgs e)
        {
            int result;
            int.TryParse(this.videoText.Text, out result);
            this.videoText.Text = (++ result).ToString();
            if (result >= 1 && result <= FramesAndPaths.GetVideoCount())
            {
                videoTextBoxBorder.BorderBrush = Brushes.Blue;
                FramesAndPaths.RefreshCurrentVideoNumber(result);
            }
            else 
            {
                videoTextBoxBorder.BorderBrush = Brushes.Red;
            }
        }

        /// <summary>
        /// Handles the user clicking on the decrement button
        /// Select previous video folder to record
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void decrementVideoBtn_Click(object sender, RoutedEventArgs e)
        {
            int result;
            int.TryParse(this.videoText.Text, out result);
            this.videoText.Text = (-- result).ToString();
            if (result >= 1 && result <= FramesAndPaths.GetVideoCount())
            {
                videoTextBoxBorder.BorderBrush = Brushes.Blue;
                FramesAndPaths.RefreshCurrentVideoNumber(result);
            }
            else
            {
                videoTextBoxBorder.BorderBrush = Brushes.Red;
            }
        }

        /// <summary>
        /// Show depth
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void depthCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                m_DepthFrameReader.FrameArrived += this.Depth_ShowImage;
            }
            catch (System.NullReferenceException)
            { }
        }

        /// <summary>
        /// Stop showing depth
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void depthCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Just for test !");
            try
            {
                m_DepthFrameReader.FrameArrived -= this.Depth_ShowImage;
                //this.Dispatcher.BeginInvokeShutdown(DispatcherPriority.);
            }
            catch (System.NullReferenceException)
            { }
        }

        /// <summary>
        /// Show color
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void colorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                m_ColorFrameReader.FrameArrived += this.Color_ShowImage;
            }
            catch(System.NullReferenceException)
            { }
        }

        /// <summary>
        /// Stop showing color
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void colorCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                m_ColorFrameReader.FrameArrived -= this.Color_ShowImage;
            }
            catch (System.NullReferenceException)
            { }
        }

        /// <summary>
        /// Show skeleton
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void skeletonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                m_BodyFrameReader.FrameArrived += this.Skeleton_ShowImage;
            }
            catch(System.NullReferenceException)
            { }
        }

        /// <summary>
        /// Stop showing skeleton
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void skeletonCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                m_BodyFrameReader.FrameArrived -= this.Skeleton_ShowImage;
            }
            catch (System.NullReferenceException)
            { }
        }
    }
}