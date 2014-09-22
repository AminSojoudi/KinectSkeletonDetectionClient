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

namespace KinectSkeletonDetectionClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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


        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
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
                this.sensor.SkeletonFrameReady += sensor_SkeletonFrameReady;
            }

            // Start the sensor!
            try
            {
                this.sensor.Start();
            }
            catch (IOException)
            {
                this.sensor = null;
            }
            if (this.sensor == null)
            {
                StatusBar.Content = "Kinect not loaded";
            }
            else
            {
                StatusBar.Content = "Kinect loaded sucessfully";
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
                    StatusBar.Content = "Sekeleton Lost!!";
                    return;
                }

                StatusBar.Content = "Skeleton Found , trying to find a loop in walking";

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


        private SkeletonPoint HipLeft, HipRight;

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            CoordinateBase = skeleton.Joints[JointType.HipCenter].Position;
            HipLeft = skeleton.Joints[JointType.HipLeft].Position;
            HipRight = skeleton.Joints[JointType.HipRight].Position;

            HipCenterJointRotation = skeleton.BoneOrientations[JointType.Spine].AbsoluteRotation.Quaternion.W;
            HipCenter = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skeleton.Joints[JointType.HipCenter].Position, DepthImageFormat.Resolution640x480Fps30);

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

                if (joint.TrackingState == JointTrackingState.Tracked)
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
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(ChangeCoordinate(joint0.Position)), this.SkeletonPointToScreen(ChangeCoordinate(joint1.Position)));
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



        private SkeletonPoint ChangeCoordinate(SkeletonPoint skelPoint)
        {

            double q = HipCenterJointRotation;
            q = (double)(q * Math.PI * 50)/(double) 45;
            float a = (HipRight.X - CoordinateBase.X + HipLeft.X - CoordinateBase.X) * 0.5f;
            ScaleLabel.Content = q.ToString();
            SkeletonPoint sp = new SkeletonPoint();
            // Transform to HipCenter position
            sp.X = skelPoint.X - CoordinateBase.X;
            sp.Y = skelPoint.Y - CoordinateBase.Y;
            sp.Z = skelPoint.Z - CoordinateBase.Z;


            // Rotation based on Y axis
            sp.X = sp.Z * (float)Math.Sin(q) + sp.X * (float)Math.Cos(q);
            sp.Z = sp.Z * (float)Math.Cos(q) - sp.X * (float)Math.Sin(q);
            sp.Y = sp.Y;
            return sp;
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
            ScaleLabel.Content = Scale.ToString();
        }
    }
}
