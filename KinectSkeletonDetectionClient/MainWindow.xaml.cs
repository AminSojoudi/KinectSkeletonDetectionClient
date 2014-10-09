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
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.IO;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;

namespace KinectSkeletonDetectionClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// random generator
        /// </summary>
        static Random randomGen = new Random();

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 354.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 244.0f;

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float SecondRenderWidth = 217.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float SecondRenderHeight = 217.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        /// <summary>
        /// Kinect Sensor
        /// </summary>
        private KinectSensor sensor;
        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup mainDrawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage mainImageSource;
        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup secondDrawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage secondImageSource;

        /// <summary>
        /// Hip Center Point
        /// </summary>
        private DepthImagePoint HipCenter;

        /// <summary>
        /// Skeleton Scale
        /// </summary>
        private double Scale = 1;

        /// <summary>
        /// Hip Center Skeleton Point
        /// </summary>
        private SkeletonPoint CoordinateBase;

        /// <summary>
        /// Hip Center Joint refrence
        /// </summary>
        private float HipCenterJointRotation;

        /// <summary>
        /// Current user id
        /// </summary>
        private string CurrentUserID;

        /// <summary>
        /// timer for users
        /// </summary>
        private Stopwatch stop;
        /// <summary>
        /// Object that handles data transfer to kafka server
        /// </summary>
        private KafkaSender kafkaSender;

        /// <summary>
        /// Current Frame Data
        /// </summary>
        private FrameData currentFrameData;

        /// <summary>
        /// boolean to define is kinect recoding current skeleton data
        /// </summary>
        private bool isRecording;
        /// <summary>
        /// collection to store temp skeleton data and save it on disk
        /// </summary>
        private List<FrameData> skeletonCollection;
        /// <summary>
        /// is sample data is on then program will not connet to kafka sender and it can playback sample data
        /// </summary>
        private bool isSampleData = true;
        /// <summary>
        /// is playing sample data
        /// </summary>
        private bool isPlayingSampleData = false;
        /// <summary>
        /// stop replay loop
        /// </summary>
        private bool stopReplayLoop;
        /// <summary>
        /// can send data to kafka server
        /// </summary>
        private bool canSendData = false;
        /// <summary>
        /// is client connected to kafka server
        /// </summary>
        private bool connectedToServer;


        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the Stop Watch
            stop = Stopwatch.StartNew();


            NewUser();

            isRecording = false;

            skeletonCollection = new List<FrameData>();

            currentFrameData = new FrameData(CurrentUserID);

            // Create the drawing group we'll use for drawing
            this.mainDrawingGroup = new DrawingGroup();
            this.secondDrawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.mainImageSource = new DrawingImage(this.mainDrawingGroup);
            this.secondImageSource = new DrawingImage(this.secondDrawingGroup);

            // Display the drawing using our image control
            MainViewPort.Source = this.mainImageSource;
            SecondaryViewPort.Source = this.secondImageSource;


            this.sensor = KinectSensor.KinectSensors.Where(item => item.Status == KinectStatus.Connected).FirstOrDefault();
            if (this.sensor != null)
            {
            if (!this.sensor.SkeletonStream.IsEnabled)
            {
                TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.5f;
                    smoothingParam.Correction = 0.1f;
                    smoothingParam.Prediction = 0.5f;
                    smoothingParam.JitterRadius = 0.1f;
                    smoothingParam.MaxDeviationRadius = 0.1f;
                };
                this.sensor.SkeletonStream.Enable(smoothingParam);
               // this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonFrameReady += sensor_SkeletonFrameReady;
                }

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                    if (this.sensor.ElevationAngle != 0)
                    {
                        this.sensor.ElevationAngle = 0;
                    }
                    StatusBar.Content = "Kinect loaded sucessfully";
                    Scale = 0.4f;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    this.sensor = null;
                    isSampleData = true;
                }
            }
            else if (this.sensor == null)
            {
                StatusBar.Content = "Kinect not loaded";
                isSampleData = true;
            }  
        }

        private void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] totalSkeleton = new Skeleton[6];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return;
                }
                skeletonFrame.CopySkeletonDataTo(totalSkeleton);
                Skeleton firstSkeleton = (from trackSkeleton in totalSkeleton
                                          where trackSkeleton.TrackingState == SkeletonTrackingState.Tracked
                                          select trackSkeleton).FirstOrDefault();
                if (firstSkeleton == null)
                {
                    return;
                }

                if (!isPlayingSampleData)
                {
                    using (DrawingContext dc = this.mainDrawingGroup.Open())
                    {
                        // Draw a transparent background to set the render size
                        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                        this.DrawBonesAndJoints(firstSkeleton, dc);

                        // prevent drawing outside of our render area
                        this.mainDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                    }
                }
            }
        }


        private SkeletonPoint HipLeft, HipRight;

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {



            currentFrameData = new FrameData(CurrentUserID);
            currentFrameData.Time = stop.ElapsedMilliseconds.ToString();
            foreach (Joint joint in skeleton.Joints)
            {
                currentFrameData.AddJoint(new MyJoint(joint.JointType.ToString(), new Transform(joint.Position.X, joint.Position.Y, joint.Position.Z)));
            }

            if (canSendData)
            {
                //send data to kafka server
                string messageJson = JsonConvert.SerializeObject(currentFrameData);
                kafkaSender.sendMessage(messageJson);
            }
            // record
            if (isRecording == true)
            {
                skeletonCollection.Add(currentFrameData);
            }

            CoordinateBase = skeleton.Joints[JointType.HipCenter].Position;
            HipLeft = skeleton.Joints[JointType.HipLeft].Position;
            HipRight = skeleton.Joints[JointType.HipRight].Position;
            

            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked || isSampleData)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(ChangeCoordinate(joint.Position)), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                if (!isSampleData)
                {
                    return;
                }
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked || isSampleData)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(ChangeCoordinate(joint0.Position)), this.SkeletonPointToScreen(ChangeCoordinate(joint1.Position)));
        }


        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="frame">frame to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(FrameData frame, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Transform joint0 = frame.getTransformByJointame(jointType0);
            Transform joint1 = frame.getTransformByJointame(jointType1); 

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.trackedBonePen;
            

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(ChangeCoordinate(joint0)), this.SkeletonPointToScreen(ChangeCoordinate(joint1)));
        }


        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution. 
            //DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            Point depthPoint = MapSkeletonPoint(skelpoint, RenderWidth , RenderHeight);
            //return new Point((depthPoint.X - HipCenter.X) * Scale + RenderWidth * 0.5f, (depthPoint.Y-HipCenter.Y) * Scale + RenderHeight * 0.5f);
            return new Point((depthPoint.X) * Scale + RenderWidth * 0.5f, (depthPoint.Y) * Scale + RenderHeight * 0.5f);
            
        }

        private Point TransformToScreen(Transform transform)
        {
            SkeletonPoint sp = new SkeletonPoint();
            sp.X = transform.X;
            sp.Y = transform.Y;
            sp.Z = transform.Z;
            Point depthPoint = MapSkeletonPoint(sp, RenderWidth, RenderHeight);
            return new Point((depthPoint.X) * Scale + RenderWidth * 0.5f, (depthPoint.Y) * Scale + RenderHeight * 0.5f);
        }



        private SkeletonPoint ChangeCoordinate(SkeletonPoint skelPoint)
        {
            SkeletonPoint sp = new SkeletonPoint();
            // Transform to HipCenter position
            sp.X = skelPoint.X - CoordinateBase.X;
            sp.Y = skelPoint.Y - CoordinateBase.Y;
            sp.Z = skelPoint.Z - CoordinateBase.Z;

            return sp;
        }

        private SkeletonPoint ChangeCoordinate(Transform transform)
        {
            SkeletonPoint sp = new SkeletonPoint();
            // Transform to HipCenter position
            sp.X = transform.X - CoordinateBase.X;
            sp.Y = transform.Y - CoordinateBase.Y;
            sp.Z = transform.Z - CoordinateBase.Z;

            return sp;
        }


        private Vector4 Conjugate(Vector4 q)
        {
            Vector4 vec = new Vector4();
            vec.X = -q.X;
            vec.Y = -q.Y;
            vec.Z = -q.Z;
            vec.W = q.W;
            return vec;
        }

        private Point MapSkeletonPoint(SkeletonPoint spoint , float ViewPortWidth , float ViewPortHeight)
        {
            return new Point(spoint.X * ViewPortWidth , -spoint.Y * ViewPortHeight);
        }


        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            Scale = slider.Value;
        }

        public void NewUser()
        {  
            NewUser(randomGen.Next(1, int.MaxValue).ToString());
        }
        public void NewUser(string userID)
        {
            stop.Restart();
            CurrentUserID = userID;
            UserIDLabel.Content = CurrentUserID;
        }

        private void SampleData1_Click(object sender, RoutedEventArgs e)
        {
            stopReplayLoop = false;
            isPlayingSampleData = true;
            // Read in file with File class.
            string text1 = File.ReadAllText("Sample1.txt");
           // List<Skeleton> skeletons = (List<Skeleton>) JsonConvert.DeserializeObject(text1);
            List<FrameData> frames = JsonConvert.DeserializeObject<List<FrameData>>(text1);
            replay(frames , 30 , 5);
            
            

        }

        private async Task replay(List<FrameData> frames , int initialFrameNumber , int finalPaddingFrames)
        {

            // looping play back
            while(true)
            {
                for (int i = initialFrameNumber ; i < frames.Count - finalPaddingFrames ; i++)// FrameData item in frames)
                {
                    using (DrawingContext dc = this.mainDrawingGroup.Open())
                    {
                        // Draw a transparent background to set the render size
                        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                        if (stopReplayLoop)
                        {
                            return;
                        }

                        if (canSendData)
                        {
                            string messageJson = JsonConvert.SerializeObject(frames[i]);
                            //send data to kafka server
                            kafkaSender.sendMessage(messageJson);
                        }

                        await Task.Delay(35);

                        this.drawBonesAndJointsForReplay(frames[i], dc);


                        // prevent drawing outside of our render area
                        this.mainDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                    }
                }
                Console.WriteLine("a loop finished");
                isPlayingSampleData = false;
            }
        }


        private void drawBonesAndJointsForReplay(FrameData frame, DrawingContext drawingContext)
        {

            foreach (MyJoint _joint in frame.Joints)
            {
                if (_joint.jointName == JointType.HipCenter.ToString())
                {

                    CoordinateBase.X = _joint.transform.X;
                    CoordinateBase.Y = _joint.transform.Y;
                    CoordinateBase.Z = _joint.transform.Z;
                }
            }


            // Render Torso
            this.DrawBone(frame, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(frame, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(frame, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(frame, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(frame, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(frame, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(frame, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(frame, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(frame, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(frame, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(frame, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(frame, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(frame, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(frame, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(frame, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(frame, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(frame, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(frame, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(frame, drawingContext, JointType.AnkleRight, JointType.FootRight);



            // Render Joints
            foreach (MyJoint joint in frame.Joints)
            {
                Brush drawBrush = this.trackedJointBrush;
                drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(ChangeCoordinate(joint.transform)), JointThickness, JointThickness);
            }
        }

        private void SampleData2_Click(object sender, RoutedEventArgs e)
        {
            stopReplayLoop = false;
            isPlayingSampleData = true;
            // Read in file with File class.
            string text1 = File.ReadAllText("Sample2.txt");
            List<FrameData> frames = JsonConvert.DeserializeObject<List<FrameData>>(text1);
            replay(frames, 30, 5);
        }

        private void SampleData3_Click(object sender, RoutedEventArgs e)
        {
            stopReplayLoop = false;
            isPlayingSampleData = true;
            // Read in file with File class.
            string text1 = File.ReadAllText("Sample3.txt");
            List<FrameData> frames = JsonConvert.DeserializeObject<List<FrameData>>(text1);
            replay(frames, 30, 5);
        }

        private void SampleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SampleButton.Content.ToString() == "Record")
            {
                isRecording = true;
                SampleButton.Content = "Stop";
            }
            else
            {
                isRecording = false;
                SampleButton.Content = "Record";
                StatusBar.Content = "Saving";
                //save data to file
                string recordJson = JsonConvert.SerializeObject(skeletonCollection , Formatting.Indented);
                using (FileStream fs1 = new FileStream("RecordData.txt", FileMode.Create,FileAccess.Write))
                using (StreamWriter writer = new StreamWriter(fs1))
                {
                    writer.Write(recordJson);
                }

                skeletonCollection.Clear();
                StatusBar.Content = "Saved!";
            }
        }

        private void StopReplay_Click(object sender, RoutedEventArgs e)
        {
            stopReplayLoop = true;
            isPlayingSampleData = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (connectedToServer)
            {
                canSendData = true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            canSendData = false;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // instantiate kafka sender class
            kafkaSender = new KafkaSender();
            connectedToServer = kafkaSender.getStatus();
        }

        private void NewUserButton_Click(object sender, RoutedEventArgs e)
        {
            NewUser();
        }

    }
}
