namespace RoomAliveToolkit
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using System.Drawing;
    using System.ComponentModel;
    using System.Data;
    using System.Linq;
    using System.Text;
    using SharpDX;
    using SharpDX.Direct3D11;
    using SharpDX.DXGI;
    using SharpDX.D3DCompiler;
    using SharpDX.WIC;
    using RoomAliveToolkit;
    using System.Diagnostics;
    using Microsoft.Kinect;
    using Kinect2Serializer;
    using Kinect2SimpleServer;

    public partial class Form1 : Form
    {

        private readonly Brush dangerousJointBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 255, 0, 0));
        private readonly Brush trackedJointBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 68, 192, 68));
        private Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private int jointThickness = 30;

        private List<Pen> bodyColors;
        private List<Tuple<JointType, JointType>> bones;
        private List<JointType> headRegion = new List<JointType>() { JointType.Head, JointType.Neck };
        private List<JointType> leftArmRegion = new List<JointType>() { JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft };
        private List<JointType> rightArmRegion = new List<JointType>() { JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight };
        private List<JointType> trunkRegion = new List<JointType>() { JointType.ShoulderLeft, JointType.ShoulderRight, JointType.SpineShoulder, JointType.SpineBase, JointType.SpineMid };
        private List<JointType> leftLegRegion = new List<JointType>() { JointType.KneeLeft, JointType.HipLeft, JointType.AnkleLeft, JointType.FootLeft };
        private List<JointType> rightLegRegion = new List<JointType>() { JointType.KneeRight, JointType.HipRight, JointType.AnkleRight, JointType.FootRight };


        bool angelShow = false;
        bool devilShow = false;

        public class Skeleton
        {
            public ProjectionMappingSample.ProjectionFeedback Feedback { get; set; }
            public IReadOnlyDictionary<JointType, Kinect2SJoint> Joints { get; set; }
            public IDictionary<JointType, System.Drawing.Point> JointPoints { get; set; }
            public Pen DrawingPen { get; set; }

            public Skeleton(ProjectionMappingSample.ProjectionFeedback feedback, IReadOnlyDictionary<JointType, Kinect2SJoint> joints, IDictionary<JointType, System.Drawing.Point> jointPoints, Pen drawingPen)
            {
                this.Feedback = feedback;
                this.Joints = joints;
                this.JointPoints = jointPoints;
                this.DrawingPen = drawingPen;
            }
        }

        private Dictionary<int, Skeleton> skeletons;

        public Form1()
        {
            InitializeComponent();
            createSkeleton();
        }

        public Form1(Factory factory, SharpDX.Direct3D11.Device device, Object renderLock)
        {
            InitializeComponent();
            this.factory = factory;
            this.device = device;
            this.renderLock = renderLock;
            createSkeleton();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.UserPaint |
              ControlStyles.AllPaintingInWmPaint |
              ControlStyles.ResizeRedraw |
              ControlStyles.ContainerControl |
              ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.SupportsTransparentBackColor
              , true);

            this.videoPanel1.Paint += panel1_Paint;

            angelRiseTimer = new Timer();
            angelRiseTimer.Tick += angelRiseTimer_Tick;
            angelRiseTimer.Interval = 170; // milliseconds
            angelRiseWatch = new Stopwatch();

            devilRiseTimer = new Timer();
            devilRiseTimer.Tick += devilRiseTimer_Tick;
            devilRiseTimer.Interval = 170; // milliseconds
            devilRiseWatch = new Stopwatch();
        }

        public void createSkeleton()
        {
            this.skeletons = new Dictionary<int, Skeleton>();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();
            this.bodyColors.Add(new Pen(Brushes.PaleGreen, 6));
            this.bodyColors.Add(new Pen(Brushes.PaleTurquoise, 6));
            this.bodyColors.Add(new Pen(Brushes.PaleGoldenrod, 6));
            this.bodyColors.Add(new Pen(Brushes.LightCoral, 6));
            this.bodyColors.Add(new Pen(Brushes.Plum, 6));
            this.bodyColors.Add(new Pen(Brushes.Pink, 6));
        }

        bool devilPicture1 = true;
        Timer devilRiseTimer;
        Stopwatch devilRiseWatch;

        bool angelPicture1 = true;
        Timer angelRiseTimer;
        Stopwatch angelRiseWatch;

        Timer devilTimer;
        Timer angelTimer;
        Stopwatch angelTimerWatch;
        Stopwatch devilTimerWatch;

        public int current_bad_user_count = 0;
        public int current_good_user_count = 0;

        public void On_ClockedChanged(List<int> a, List<int> d)
        {
            current_good_user_count = a[a.Count-1];
            current_bad_user_count = d[d.Count-1];
            System.Diagnostics.Debug.WriteLine(current_good_user_count);

            if (badPostureLabel.Visible)
            {
                this.badPostureLabel.BeginInvoke((Action)(() =>
                {
                    badPostureLabel.Visible = false;
                    this.badPostureLabel.Invalidate();
                }));
            }
            if (d.Count >= 2 && d[d.Count - 1] > d[d.Count - 2])
            {
                this.badPostureLabel.BeginInvoke((Action)(() =>
                {
                    badPostureLabel.Visible = true;
                    this.badPostureLabel.Invalidate();
                }));
            }

            if (goodPostureLabel.Visible)
            {
                this.goodPostureLabel.BeginInvoke((Action)(() =>
                {
                    goodPostureLabel.Visible = false;
                    this.goodPostureLabel.Refresh();
                }));
            }
            if (a.Count >= 2 && a[a.Count - 1] > a[a.Count - 2])
            {
                this.goodPostureLabel.BeginInvoke((Action)(() =>
                {
                    goodPostureLabel.Visible = true;
                    this.goodPostureLabel.Refresh();
                }));
            }
            if (angelShow && !devilShow && !angelRiseTimer.Enabled)
            {
                if (a[a.Count-1] == 0)
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock_witheredFlower.png");
                        //this.pictureBox2.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        angelTimer.Stop();
                        angelTimerWatch.Stop();
                    }));
                }
                else if (a[a.Count-1] == 1)
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock1_flower.png");
                        //this.pictureBox2.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        angelTimer.Start();
                        angelTimerWatch.Start();
                    }));
                }
                else if (a[a.Count-1] == 2)
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock2_flower.png");
                        //this.pictureBox2.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        angelTimer.Start();
                        angelTimerWatch.Start();
                    }));
                }
                else
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock3_flower.png");
                        //this.pictureBox2.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        angelTimer.Start();
                        angelTimerWatch.Start();
                    }));
                }
            }
            else if (!angelShow && devilShow && !devilRiseTimer.Enabled)
            {
                if (d[d.Count-1] == 0)
                {
                    this.pictureBox3.BeginInvoke((Action)(() =>
                    {
                        pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devillClock.png");
                        //this.pictureBox3.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        devilTimer.Stop();
                        devilTimerWatch.Stop();
                    }));
                }
                else if (d[d.Count-1] == 1)
                {
                    this.pictureBox3.BeginInvoke((Action)(() =>
                    {
                        pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devillClock1.png");
                        //this.pictureBox3.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        devilTimer.Start();
                        devilTimerWatch.Start();
                    }));
                }
                else if (d[d.Count-1] == 2)
                {
                    this.pictureBox3.BeginInvoke((Action)(() =>
                    {
                        pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devillClock2.png");
                        //this.pictureBox3.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        devilTimer.Start();
                        devilTimerWatch.Start();
                    }));
                }
                else
                {
                    this.pictureBox3.BeginInvoke((Action)(() =>
                    {
                        pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devillClock3.png");
                        //this.pictureBox3.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        devilTimer.Start();
                        devilTimerWatch.Start();
                    }));
                }
            }
            else if (angelShow && devilShow && !angelRiseTimer.Enabled && !devilRiseTimer.Enabled)
            {
                if (a[a.Count-1] == 0)
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock.png");
                        //this.pictureBox2.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        angelTimer.Stop();
                        angelTimerWatch.Stop();
                    }));
                    if (d[d.Count - 1] == 0)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Stop();
                            devilTimerWatch.Stop();
                        }));
                    }
                    else if (d[d.Count - 1] == 1)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock1.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                    else if (d[d.Count - 1] == 2)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock2.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                    else
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock3.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }

                }
                else if (a[a.Count-1] == 1)
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock1.png");
                        //this.pictureBox2.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        angelTimer.Start();
                        angelTimerWatch.Start();
                    }));
                    if (d[d.Count - 1] == 0)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Stop();
                            devilTimerWatch.Stop();
                        }));
                    }
                    else if (d[d.Count - 1] == 1)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock1.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                    else if (d[d.Count - 1] == 2)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock2.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                    else
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock3.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                }
                else if (a[a.Count-1] == 2)
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock2.png");
                        //this.pictureBox2.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        angelTimer.Start();
                        angelTimerWatch.Start();
                    }));
                    if (d[d.Count - 1] == 0)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Stop();
                            devilTimerWatch.Stop();
                        }));
                    }
                    else if (d[d.Count - 1] == 1)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock1.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                    else if (d[d.Count - 1] == 2)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock2.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                    else
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock3.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                }
                else
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock3.png");
                        //this.pictureBox2.Invalidate();
                    }));
                    this.BeginInvoke((Action)(() =>
                    {
                        angelTimer.Start();
                        angelTimerWatch.Start();
                    }));
                    if (d[d.Count - 1] == 0)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Stop();
                            devilTimerWatch.Stop();
                        }));
                    }
                    else if (d[d.Count - 1] == 1)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock1.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                    else if (d[d.Count - 1] == 2)
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock2.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                    else
                    {
                        this.pictureBox3.BeginInvoke((Action)(() =>
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock3.png");
                            //this.pictureBox3.Invalidate();
                        }));
                        this.BeginInvoke((Action)(() =>
                        {
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }));
                    }
                }
            }


            if (!angelShow)
            {
                if (a[a.Count-1] > 0)
                {
                    this.pictureBox2.BeginInvoke((Action)(() =>
                    {
                        this.pictureBox2.Visible = true;
                        //this.pictureBox2.Invalidate();
                    }));

                    this.BeginInvoke((Action)(() =>
                    {
                        angelRiseTimer.Start();
                        angelRiseWatch.Start();
                    }));

                    angelShow = true;
                }
            }

            if (!devilShow)
            {
                if (d[d.Count-1] > 0)
                {
                    this.pictureBox3.BeginInvoke((Action)(() =>
                    {
                        //System.Diagnostics.Debug.WriteLine("devil appear");
                        this.pictureBox3.Visible = true;
                        //this.pictureBox3.Invalidate();
                    }));

                    this.BeginInvoke((Action)(() =>
                    {
                        devilRiseTimer.Start();
                        devilRiseWatch.Start();
                    }));

                    devilShow = true;
                }
            }
        }

        private void devilRiseTimer_Tick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("devil rise timer...");
            if (devilRiseWatch.ElapsedMilliseconds >= 10000)
            {
                devilRiseTimer.Stop();
                devilRiseWatch.Stop();
                label1.Visible = false;
                label1.Invalidate();
                pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock1.png");
                //pictureBox3.Invalidate();
                devilTimer = new Timer();
                devilTimer.Tick += devilTimer_Tick;
                devilTimer.Interval = 1000;
                devilTimerWatch = new Stopwatch();
                devilTimer.Start();
                devilTimerWatch.Start();
                return;
            }

            devilPicture1 = !devilPicture1;
            if (devilPicture1)
            {
                pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock.png");
            }
            else
            {
                pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock_.png");
            }

            this.pictureBox3.BeginInvoke((Action)(() =>
            {
                pictureBox3.Location = new System.Drawing.Point(pictureBox3.Location.X, pictureBox3.Location.Y - 12);
                pictureBox3.Invalidate();
            }));
        }

        private void angelRiseTimer_Tick(object sender, EventArgs e)
        {
            if (angelRiseWatch.ElapsedMilliseconds >= 10000)
            {
                angelRiseTimer.Stop();
                angelRiseWatch.Stop();
                label2.Visible = false;
                label2.Invalidate();
                pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock1.png");
                //pictureBox2.Invalidate();
                angelTimer = new Timer();
                angelTimer.Tick += angelTimer_Tick;
                angelTimer.Interval = 1000;
                angelTimerWatch = new Stopwatch();
                angelTimer.Start();
                angelTimerWatch.Start();
                return;
            }
            angelPicture1 = !angelPicture1;
            if (angelPicture1)
            {
                pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock.png");
            }
            else
            {
                pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock_.png");
            }

            this.pictureBox2.BeginInvoke((Action)(() =>
            {
                pictureBox2.Location = new System.Drawing.Point(pictureBox2.Location.X, pictureBox2.Location.Y - 12);
                pictureBox2.Invalidate();
            }));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode)
                return;

            // create swap chain, rendertarget
            var swapChainDesc = new SwapChainDescription()
            {
                BufferCount = 1,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = videoPanel1.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard,
                SampleDescription = new SampleDescription(1, 0),
            };

            swapChain = new SwapChain(factory, device, swapChainDesc);

            // render target
            renderTarget = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderTargetView = new RenderTargetView(device, renderTarget);

            // depth buffer
            var depthBufferDesc = new Texture2DDescription()
            {
                Width = videoPanel1.Width,
                Height = videoPanel1.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D32_Float, // necessary?
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None
            };
            depthStencil = new Texture2D(device, depthBufferDesc);
            depthStencilView = new DepthStencilView(device, depthStencil);

            // viewport
            viewport = new Viewport(0, 0, videoPanel1.Width, videoPanel1.Height, 0f, 1f);
        }

   
        void panel1_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);
            Control c = sender as Control;
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw skeletons
            foreach (Skeleton skeleton in this.skeletons.Values)
            {
                IReadOnlyDictionary<JointType, Kinect2SJoint> joints = skeleton.Joints;
                IDictionary<JointType, System.Drawing.Point> jointPoints = skeleton.JointPoints;

                 //Draw the bones
                foreach (var bone in this.bones)
                {
                    JointType jointtype0 = bone.Item1;
                    JointType jointtype1 = bone.Item2;
                    if (jointtype0 == null ||
                        jointtype1 == null)
                    {
                        return;
                    }
                    Kinect2SJoint joint0 = joints[jointtype0];
                    Kinect2SJoint joint1 = joints[jointtype1];
                    int joint0X = jointPoints[jointtype0].X + 50;
                    int joint0Y = jointPoints[jointtype0].Y;
                    int joint1X = jointPoints[jointtype1].X + 50;
                    int joint1Y = jointPoints[jointtype1].Y;
                    System.Drawing.Point a = new System.Drawing.Point(jointPoints[jointtype0].X, jointPoints[jointtype0].Y);
                    System.Drawing.Point b = new System.Drawing.Point(jointPoints[jointtype1].X, jointPoints[jointtype1].Y);
                    // If we can't find either of these joints, exit
                    //if (joint0.TrackingState == TrackingState.NotTracked ||
                    //    joint1.TrackingState == TrackingState.NotTracked)
                    //{
                    //    return;
                    //}

                    // We assume all drawn bones are inferred unless both joints are tracked
                    Pen drawpen = this.inferredBonePen;

                    //if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
                    //{
                        drawpen = skeleton.DrawingPen;
                    //}

                    g.DrawLine(drawpen, a, b);
                } // bones

                 //Draw the joints
                foreach (JointType jointType in joints.Keys)
                {
                    int jointX = jointPoints[jointType].X + 50;
                    int jointY = jointPoints[jointType].Y;
                    //System.Diagnostics.Debug.WriteLine("joint: " + jointType + " x: " + jointX + " y: " + jointY);
                    if (skeleton.Feedback == ProjectionMappingSample.ProjectionFeedback.LegCrossed || skeleton.Feedback == ProjectionMappingSample.ProjectionFeedback.LegStationary)
                    {
                        if (jointType == JointType.Head)
                        {
                        }
                        else if (leftLegRegion.Contains(jointType) || rightLegRegion.Contains(jointType))
                        {
                            g.FillEllipse(this.dangerousJointBrush, jointX, jointY, jointThickness, jointThickness);
                        }
                        else
                        {
                            g.FillEllipse(this.trackedJointBrush, jointX, jointY, jointThickness, jointThickness);
                        }
                    }
                    else if (skeleton.Feedback == ProjectionMappingSample.ProjectionFeedback.Slouch || skeleton.Feedback == ProjectionMappingSample.ProjectionFeedback.BodyStationary)
                    {
                        if (jointType == JointType.Head)
                        {
                        }
                        else if (trunkRegion.Contains(jointType))
                        {
                            g.FillEllipse(this.dangerousJointBrush, jointX, jointY, jointThickness, jointThickness);
                        }
                        else
                        {
                            g.FillEllipse(this.trackedJointBrush, jointX, jointY, jointThickness, jointThickness);
                        }
                    }
                    else if (skeleton.Feedback == ProjectionMappingSample.ProjectionFeedback.ArmStationary)
                    {
                        if (jointType == JointType.Head)
                        {
                        }
                        else if (rightArmRegion.Contains(jointType) || leftArmRegion.Contains(jointType))
                        {
                            g.FillEllipse(this.dangerousJointBrush, jointX, jointY, jointThickness, jointThickness);
                        }
                        else
                        {
                            g.FillEllipse(this.trackedJointBrush, jointX, jointY, jointThickness, jointThickness);
                        }
                    }
                    else
                    {
                        if (jointType == JointType.Head)
                        {
                        }
                        if (jointType == JointType.Neck)
                        {
                            g.FillEllipse(this.dangerousJointBrush, jointX, jointY, jointThickness, jointThickness);
                        }
                        else
                        {
                            g.FillEllipse(this.trackedJointBrush, jointX, jointY, jointThickness, jointThickness);
                        }
                    }
                } // joints
            } // skeletons
            //c.Invalidate();
        }

        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    base.OnPaint(e);
        //}
        public void On_BodyAmountCounted(int bodyAmount)
        {

        }

        public void On_FeedbackChanged(int bodyId, Kinect2SBody body, Tuple<ProjectionMappingSample.ProjectionFeedback, int> feedbackTuple, float headX, float headY)
        {
            PictureBox face = null;
            switch (bodyId)
            {
                case 1:
                    face = tracking_face;

                    break;
                case 2:
                    face = tracking_face2;
                    break;
            }

            int faceX = (int)(495 + 530.87 * headX);
            int faceY = (int)(533.14 - 459.46 * headY);
            face.BeginInvoke((Action)(() => face.Visible = true));
            face.BeginInvoke((Action)(() => face.Location = new System.Drawing.Point(faceX, faceY)));

            if (feedbackTuple.Item1 != ProjectionMappingSample.ProjectionFeedback.Standard)
            {
                if (feedbackTuple.Item2 < 3)
                {
                    face.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\spot_.png");
                }
                else if (feedbackTuple.Item2 >= 3 && feedbackTuple.Item2 < 5)
                {
                    face.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\lighteyes_.png");

                }
                else if (feedbackTuple.Item2 >= 5 && feedbackTuple.Item2 < 7)
                {
                    face.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\deepeyes_.png");
                }
                else
                {
                    //    float a = face.Location.X - body.Joints[JointType.Head].CameraSpacePoint.X;
                    //    float b = face.Location.Y - body.Joints[JointType.Head].CameraSpacePoint.Y;
                    if(feedbackTuple.Item1 == ProjectionMappingSample.ProjectionFeedback.LegCrossed)
                    {
                        //if (ProjectionMappingSample.bodyId==1)
                        //{
                        //    face.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\legCrossedSkeletonF.png");
                        //}
                        //else
                        //{
                            face.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\legCrossedSkeletonM.png");
                        //}                       
                    }
                    else
                    {
                        //if (ProjectionMappingSample.bodyId == 1)
                        //{
                            face.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\slouchSkeletonF.png");
                        //}
                        //else
                        //{
                        //    face.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\slouchSkeletonM.png");
                        //}
                    }


                    //Dictionary<JointType, System.Drawing.Point> jointPoints = new Dictionary<JointType, System.Drawing.Point>();
                    //foreach (JointType jointType in body.Joints.Keys)
                    //{
                    //    CameraSpacePoint position = body.Joints[jointType].CameraSpacePoint;
                    //    int x = (int)(495 + 530.87 * position.X);
                    //    int y = (int)(533.14 - 459.46 * position.Y);
                    //    jointPoints[jointType] = new System.Drawing.Point(x, y);
                    //}

                    //Pen drawPen = this.bodyColors[bodyId - 1];
                    //if (this.skeletons.ContainsKey(bodyId) && this.skeletons[bodyId] != null)
                    //{
                    //    this.skeletons[bodyId].Feedback = feedbackTuple.Item1;
                    //    this.skeletons[bodyId].Joints = body.Joints;
                    //    this.skeletons[bodyId].JointPoints = jointPoints;
                    //    this.skeletons[bodyId].DrawingPen = drawPen;
                    //}
                    //else
                    //{
                    //    this.skeletons[bodyId] = new Skeleton(feedbackTuple.Item1, body.Joints, jointPoints, drawPen);
                    //}
                    //this.videoPanel1.BeginInvoke((Action)(() => this.videoPanel1.Invalidate()));
                }
            }
            else
            {
                face.BeginInvoke((Action)(() => face.Visible = false));
            }
        }

        SharpDX.Direct3D11.Device device;
        Factory factory;
        Texture2D renderTarget, depthStencil;
        public RenderTargetView renderTargetView;
        public DepthStencilView depthStencilView;
        public Viewport viewport;
        public SwapChain swapChain;
        Object renderLock;
        ImageList imgList = new ImageList();

        //public void setTransparentBack()
        //{
        //    imgList.TransparentColor = System.Drawing.Color.Transparent;
        //    imgList.Images[0] = pictureBox1.Image;
        //}

        private void videoPanel1_SizeChanged(object sender, EventArgs e)
        {
            // TODO: look into using this as initial creation
            if (renderTargetView != null)
                lock (renderLock)
                {
                    renderTargetView.Dispose();
                    renderTarget.Dispose();

                    depthStencilView.Dispose();
                    depthStencil.Dispose();

                    swapChain.ResizeBuffers(1, videoPanel1.Width, videoPanel1.Height, Format.Unknown, SwapChainFlags.AllowModeSwitch);

                    renderTarget = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
                    renderTargetView = new RenderTargetView(device, renderTarget);

                    // depth buffer
                    var depthBufferDesc = new Texture2DDescription()
                    {
                        Width = videoPanel1.Width,
                        Height = videoPanel1.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = Format.D32_Float, // necessary?
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.DepthStencil,
                        CpuAccessFlags = CpuAccessFlags.None
                    };
                    depthStencil = new Texture2D(device, depthBufferDesc);
                    depthStencilView = new DepthStencilView(device, depthStencil);

                    // viewport
                    viewport = new Viewport(0, 0, videoPanel1.Width, videoPanel1.Height, 0f, 1f);
                }
        }

        const int Devil_Picture_Box = 1;
        const int Angel_Picture_Box = 2;
        int currentGoodToken = 0;
        int currentBadToken = 0;
        int currentWitheredFlower = 0;

        int aChangedTime = 0;
        bool[] flowerArray = new bool[5];
        private void angelTimer_Tick(object sender, EventArgs e)
        {
            char[] times = label2.Text.ToCharArray();
            int digit_sec = Convert.ToInt32(new string(times[4], 1));
            int tens_sec = Convert.ToInt32(new string(times[3], 1));
            int digit_min = Convert.ToInt32(new string(times[1], 1));
            int tens_min = Convert.ToInt32(new string(times[0], 1));
            bool tensSecChanged = false;

            if (current_good_user_count == 1)
            {
                if (digit_sec < 9)
                {
                    digit_sec++;
                }
                else
                {
                    digit_sec = 0;
                    if (tens_sec < 5)
                    {
                        tens_sec++;
                        tensSecChanged = true;
                    }
                    else
                    {
                        tens_sec = 0;
                        tensSecChanged = true;
                        if (digit_min < 9)
                        {
                            digit_min++;
                        }
                        else
                        {
                            digit_min = 0;
                            tens_min++;
                        }
                    }
                }
            }
            else if (current_good_user_count == 2)
            {
                if (digit_sec < 8)
                {
                    digit_sec = digit_sec + 2;
                }
                else
                {
                    digit_sec = digit_sec - 8;
                    if (tens_sec < 5)
                    {
                        tens_sec++;
                        tensSecChanged = true;
                    }
                    else
                    {
                        tens_sec = 0;
                        tensSecChanged = true;
                        if (digit_min < 9)
                        {
                            digit_min++;
                        }
                        else
                        {
                            digit_min = 0;
                            tens_min++;
                        }
                    }

                }
            }
            else
            {
                if (digit_sec < 7)
                {
                    digit_sec = digit_sec + 3;
                }
                else
                {
                    digit_sec = digit_sec - 7;
                    if (tens_sec < 5)
                    {
                        tens_sec++;
                        tensSecChanged = true;
                    }
                    else
                    {
                        tens_sec = 0;
                        tensSecChanged = true;
                        if (digit_min < 9)
                        {
                            digit_min++;
                        }
                        else
                        {
                            digit_min = 0;
                            tens_min++;
                        }
                    }
                }
            }

            if (tensSecChanged)
            {
                //aChangedTime++;
                if (currentBadToken > 0)
                {
                    if(currentBadToken==5)
                    {
                        bomb5.Visible = false;
                        bomb5.Refresh();
                    }
                    else if (currentBadToken == 4)
                    {
                        bomb4.Visible = false;
                        bomb4.Refresh();
                    }
                    else if (currentBadToken == 3)
                    {
                        bomb3.Visible = false;
                        bomb3.Refresh();
                    }
                    else if (currentBadToken == 2)
                    {
                        bomb2.Visible = false;
                        bomb2.Refresh();
                    }
                    else
                    {
                        bomb1.Visible = false;
                        bomb1.Refresh();
                        
                    }
                    currentBadToken--;
                }
                else if (currentWitheredFlower > 0)
                {
                    if (currentWitheredFlower == 5)
                    {
                        flower1.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign.png");
                        flower1.Invalidate();
                    }
                    else if (currentWitheredFlower == 4)
                    {
                        flower2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign.png");
                        flower2.Invalidate();
                    }
                    else if (currentWitheredFlower == 3)
                    {
                        flower3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign.png");
                        flower3.Invalidate();
                    }
                    else if (currentWitheredFlower == 2)
                    {
                        flower4.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign.png");
                        flower4.Invalidate();
                    }
                    else
                    {
                        flower5.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign.png");
                        flower5.Invalidate();
                    }
                    currentWitheredFlower--;
                }
                else if (currentGoodToken <5)
                {
                    if (currentGoodToken == 4)
                    {
                        //if (this.flower1.Image != Image.FromFile())
                        this.flower1.Visible = true;
                        this.flower1.Invalidate();
                        //flowerArray[0] = true;
                    }
                    else if (currentGoodToken == 3)
                    {
                        this.flower2.Visible = true;
                        this.flower2.Invalidate();
                        //flowerArray[4] = true;
                    }
                    else if (currentGoodToken == 2)
                    {
                        this.flower3.Visible = true;
                        this.flower3.Invalidate();
                        //flowerArray[1] = true;
                    }
                    else if (currentGoodToken == 1)
                    {
                        this.flower4.Visible = true;
                        this.flower4.Invalidate();
                        //flowerArray[3] = true;
                    }
                    else
                    {
                        this.flower5.Visible = true;
                        this.flower5.Invalidate();
                        //flowerArray[2] = true;
                    }
                    currentGoodToken++;
                }               
            }

            //System.Diagnostics.Debug.WriteLine("after if");
            string new_time = String.Format("{0}{1}:{2}{3}", tens_min, digit_min, tens_sec, digit_sec);
            //System.Diagnostics.Debug.WriteLine(new_time);
            label2.Text = new_time;
            label2.Invalidate();
            //System.Diagnostics.Debug.WriteLine("new: " + label1.Text);
        }

        int dChangedTime = 0;
        bool[] bombArray = new bool[5];
        private void devilTimer_Tick(object sender, EventArgs e)
        {
            int digit_sec;
            int tens_sec;
            int digit_min;
            int tens_min;
            char[] times = label1.Text.ToCharArray();
            digit_sec = Convert.ToInt32(new string(times[4], 1));
            tens_sec = Convert.ToInt32(new string(times[3], 1));
            digit_min = Convert.ToInt32(new string(times[1], 1));
            tens_min = Convert.ToInt32(new string(times[0], 1));
            bool tensSecChanged = false;
            if (current_bad_user_count == 1)
            {
                System.Diagnostics.Debug.WriteLine(digit_sec);
                if (digit_sec < 9)
                {
                    digit_sec++;
                }
                else
                {
                    digit_sec = 0;
                    if (tens_sec < 5)
                    {
                        tens_sec++;
                        tensSecChanged = true;
                    }
                    else
                    {
                        tens_sec = 0;
                        tensSecChanged = true;
                        if (digit_min < 9)
                        {
                            digit_min++;
                        }
                        else
                        {
                            digit_min = 0;
                            tens_min++;
                        }
                    }
                }
            }
            else if (current_bad_user_count == 2)
            {
                if (digit_sec < 8)
                {
                    digit_sec = digit_sec + 2;
                }
                else
                {
                    digit_sec = digit_sec - 8;
                    if (tens_sec < 5)
                    {
                        tens_sec++;
                        tensSecChanged = true;
                    }
                    else
                    {
                        tens_sec = 0;
                        tensSecChanged = true;
                        if (digit_min < 9)
                        {
                            digit_min++;
                        }
                        else
                        {
                            digit_min = 0;
                            tens_min++;
                        }
                    }

                }
            }
            else
            {
                if (digit_sec < 7)
                {
                    digit_sec = digit_sec + 3;
                }
                else
                {
                    digit_sec = digit_sec - 7;
                    if (tens_sec < 5)
                    {
                        tens_sec++;
                        tensSecChanged = true;
                    }
                    else
                    {
                        tens_sec = 0;
                        tensSecChanged = true;
                        if (digit_min < 9)
                        {
                            digit_min++;
                        }
                        else
                        {
                            digit_min = 0;
                            tens_min++;
                        }
                    }
                }
            }
            //System.Diagnostics.Debug.WriteLine("after if");
            string new_time = String.Format("{0}{1}:{2}{3}", tens_min, digit_min, tens_sec, digit_sec);
            //System.Diagnostics.Debug.WriteLine(new_time);
            label1.Text = new_time;
            label1.Invalidate();
            //System.Diagnostics.Debug.WriteLine("new: " + label1.Text);
            if (tensSecChanged)
            {

                if (currentGoodToken > 0 && currentGoodToken<=5)
                {
                    if (currentGoodToken == 5)
                    {
                        //if (this.flower1.Image != Image.FromFile())
                        flower1.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign_withered.png");
                        flower1.Invalidate();
                        //flowerArray[0] = true;
                    }
                    else if (currentGoodToken == 4)
                    {
                        flower2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign_withered.png");
                        flower2.Invalidate();
                        //flowerArray[4] = true;
                    }
                    else if (currentGoodToken == 3)
                    {
                        flower3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign_withered.png");
                        flower3.Invalidate();
                        //flowerArray[1] = true;
                    }
                    else if (currentGoodToken == 2)
                    {
                        flower4.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign_withered.png");
                        flower4.Invalidate();
                        //flowerArray[3] = true;
                    }
                    else
                    {
                        flower5.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\good_posture_sign_withered.png");
                        flower5.Invalidate();
                        //flowerArray[2] = true;
                    }
                    currentGoodToken--;
                    currentWitheredFlower++;
                }
                else if(currentBadToken<5)
                {
                    if (currentBadToken == 0)
                    {
                        this.bomb5.Visible = true;
                        this.bomb5.Invalidate();
                    }
                    else if (currentBadToken == 1)
                    {
                        this.bomb4.Visible = true;
                        this.bomb4.Invalidate();
                    }
                    else if (currentBadToken == 2)
                    {
                        this.bomb3.Visible = true;
                        this.bomb3.Invalidate();
                    }
                    else if (currentBadToken == 3)
                    {
                        this.bomb2.Visible = true;
                        this.bomb2.Invalidate();
                    }
                    else
                    {
                        this.bomb1.Visible = true;
                        this.bomb1.Invalidate();
                    }
                    currentBadToken++;
                }
            }
        }
    }

    public class ProjectorForm : Form1
    {
        public ProjectorForm(Factory factory, SharpDX.Direct3D11.Device device, Object renderLock, ProjectorCameraEnsemble.Projector projector)
            : base(factory, device, renderLock)
        {
            this.projector = projector;
            Text = "Projector " + projector.name;
        }

        public bool FullScreen
        {
            get { return fullScreen; }
            set
            {
                if (value)
                {
                    // switch to fullscreen
                    ShowInTaskbar = false;
                    FormBorderStyle = FormBorderStyle.None;
                    var bounds = Screen.AllScreens[projector.displayIndex].Bounds; // TODO: catch the case where the display is not available
                    StartPosition = FormStartPosition.Manual;
                    Location = new System.Drawing.Point(bounds.X, bounds.Y);
                    Size = new Size(bounds.Width, bounds.Height);
                }
                else
                {
                    // switch to windowed
                    ShowInTaskbar = true;
                    FormBorderStyle = FormBorderStyle.Sizable;
                    Location = windowedLocation;
                    Size = windowedSize;
                }
            }
        }

        bool fullScreen = false;
        System.Drawing.Point windowedLocation;
        Size windowedSize;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // cache location etc for when we come out of fullscreen
            windowedLocation = Location;
            windowedSize = Size;

            // pick up view and projection for projector
            view = new SharpDX.Matrix();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    view[i, j] = (float)projector.pose[i, j];
            view.Invert();
            view.Transpose();

            var cameraMatrix = projector.cameraMatrix;
            float fx = (float)cameraMatrix[0, 0];
            float fy = (float)cameraMatrix[1, 1];
            float cx = (float)cameraMatrix[0, 2];
            float cy = (float)cameraMatrix[1, 2];

            float near = 0.1f;
            float far = 100.0f;

            float w = projector.width;
            float h = projector.height;

            projection = ProjectionMappingSample.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
            projection.Transpose();
        }

        ProjectorCameraEnsemble.Projector projector;
        public SharpDX.Matrix view, projection;



        public void On_BodyFrameArrived(List<Body> bodies, int counter1)
        {
        }

    }
}
