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
        private double JointThickness = 3;
        private List<Tuple<JointType, JointType>> bones;
        private List<System.Windows.Media.Pen> bodyColors;

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
            //pictureBox3.Controls.Add(label1);
            //label1.Location = new System.Drawing.Point(0, 0);
            //label1.BackColor = System.Drawing.Color.Transparent;
            //setTransparentBack();
        }

        public void createSkeleton()
        {
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
            this.bodyColors = new List<System.Windows.Media.Pen>();
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.PaleGreen, 6));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.PaleTurquoise, 6));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.PaleGoldenrod, 6));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.LightCoral, 6));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Plum, 6));
            this.bodyColors.Add(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Pink, 6));
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            System.Drawing.Bitmap image = new System.Drawing.Bitmap("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\spot_deep_eyes.png", false);
            e.Graphics.DrawImage(image, 300, 300);
        }

        public void On_BodyAmountCounted(int bodyAmount)
        {
        }

        public void On_FeedbackChanged(int feedbackType, double headX, double headY)
        {
            if (feedbackType != 4)
            {
                //System.Drawing.
                //PaintEventArgs e 
                //drawFace(image, e, (515+730*headX), (120-750*headY));
                //System.Drawing.Graphics graphics = this.CreateGraphics();
                //graphics.DrawEllipse(System.Drawing.Pens.White, (int)(515 + 730 * headX), (int)(120 - 750 * headY), 100, 100);
                //graphics.DrawEllipse(Pens.White, 500, 500, 300, 300);
            }
        }

        public void On_SkeletonDrawing(Kinect2SBody body, ProjectionMappingSample.PostureFrame postureFrame)
        {
            System.Windows.Media.Pen drawPen = this.bodyColors[0];
            foreach (Kinect2SJoint joint in body.Joints.Values)
            {
                //this.DrawBody(joints, jointPoints, dc, drawPen);
            }
        }

        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, System.Drawing.Point> jointPoints, Pen drawingPen)
        {
            //// Draw the bones
            //foreach (var bone in this.bones)
            //{
            //    this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            //}
            //// Draw the joints
            //foreach (JointType jointType in joints.Keys)
            //{
            //    Brush drawBrush = null;
            //    if (badPosture)
            //    {
            //        if (crossedLegsDetected)
            //        {
            //            if (leftLegRegion.Contains(jointType) || rightLegRegion.Contains(jointType))
            //            {
            //                drawBrush = this.dangerousJointBrush;
            //                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], 3 * JointThickness, 3 * JointThickness);
            //            }
            //            else
            //            {
            //                drawBrush = this.trackedJointBrush;
            //                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
            //            }
            //        }
            //        else if (slouchDetected)
            //        {
            //            if (trunkRegion.Contains(jointType))
            //            {
            //                drawBrush = this.dangerousJointBrush;
            //                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], 3 * JointThickness, 3 * JointThickness);
            //            }
            //            else
            //            {
            //                drawBrush = this.trackedJointBrush;
            //                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
            //            }
            //        }
            //        else if (lowViewingHeightDetected)
            //        {
            //            if (headRegion.Contains(jointType))
            //            {
            //                drawBrush = this.dangerousJointBrush;
            //                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], 3 * JointThickness, 3 * JointThickness);
            //            }
            //            else
            //            {
            //                drawBrush = this.trackedJointBrush;
            //                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
            //            }
            //        }
            //        else
            //        {
            //            if (jointType == JointType.Head)
            //            {
            //                drawBrush = this.dangerousJointBrush;
            //                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], 3 * JointThickness, 3 * JointThickness);
            //            }
            //            else
            //            {
            //                drawBrush = this.trackedJointBrush;
            //                drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
            //            }
            //        }
            //    }

            //    else
            //    {
            //        drawBrush = this.trackedJointBrush;
            //        drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
            //    }

                //TrackingState trackingState = joints[jointType].TrackingState;

                //if (trackingState == TrackingState.Tracked)
                //{
                //drawBrush = this.trackedJointBrush;
                //}
                //else if (trackingState == TrackingState.Inferred)
                //{
                //    drawBrush = this.inferredJointBrush;
                //}

                //if (drawBrush != null)
                //{
                //    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                //}
            //}

        }

        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, System.Drawing.Point> jointPoints, JointType jointType0, JointType jointType1, Pen drawingPen)
        {
            //Joint joint0 = joints[jointType0];
            //Joint joint1 = joints[jointType1];

            //// If we can't find either of these joints, exit
            //if (joint0.TrackingState == TrackingState.NotTracked ||
            //    joint1.TrackingState == TrackingState.NotTracked)
            //{
            //    return;
            //}

            //// We assume all drawn bones are inferred unless BOTH joints are tracked
            //Pen drawPen = this.inferredBonePen;
            //if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            //{
            //    drawPen = drawingPen;
            //}

            //drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

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

        int current_bad_user_count = 1;
        int current_good_user_count = 1;
        int goodUserCount;
        int badUserCount;

        public void On_PostureChanged(ProjectionMappingSample.PostureFeedback feedback)
        {
            //eowrkesoprk
        }

        public void On_ImageChanged(string type, int num)
        {
            switch (type)
            {
                case "devil":
                    {
                        if (num == 3)
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock3.png");
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }
                        else if (num == 2)
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock2.png");
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }
                        else if (num == 1)
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock1.png");
                            devilTimer.Start();
                            devilTimerWatch.Start();
                        }
                        else
                        {
                            pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock.png");
                            devilTimer.Stop();
                            devilTimerWatch.Stop();
                        }
                        current_bad_user_count = num;
                        if (current_bad_user_count > badUserCount)
                        {
                            int pb3Height = pictureBox3.Size.Height;
                            int pb3Width = pictureBox3.Size.Width;
                            pictureBox3.Height = (int)(pb3Height * 1.15);
                            pictureBox3.Width = (int)(pb3Width * 1.15);

                        }
                        else if (current_bad_user_count < badUserCount)
                        {
                            pictureBox3.Height = (int)(pictureBox3.Size.Height / 1.15);
                            pictureBox3.Width = (int)(pictureBox3.Size.Width / 1.15);

                        }
                        System.Diagnostics.Debug.WriteLine(current_bad_user_count);
                        break;
                    }
                case "angel":
                    {
                        if (num == 3)
                        {
                            pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock3.png");
                            angelTimer.Start();
                            angelTimerWatch.Start();
                        }
                        else if (num == 2)
                        {
                            pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock2.png");
                            angelTimer.Start();
                            angelTimerWatch.Start();
                        }
                        else if (num == 1)
                        {
                            pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock1.png");
                            angelTimer.Start();
                            angelTimerWatch.Start();
                        }
                        else
                        {
                            pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock.png");
                            angelTimer.Stop();
                            angelTimerWatch.Stop();
                        }
                        current_good_user_count = num;
                        if ((pictureBox2.Image.Equals(Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock1_flower.png"))
                            || pictureBox2.Image.Equals(Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock2_flower.png"))
                            || pictureBox2.Image.Equals(Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock3_flower.png")))
                            && (current_good_user_count < goodUserCount))
                        {
                            if (current_good_user_count == 2)
                            {
                                pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock2_witheredFlower.png");
                            }
                            else if (current_good_user_count == 1)
                            {
                                pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock1_witheredFlower.png");
                            }
                            else
                            {
                                pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock_witheredFlower.png");
                            }
                        }
                        goodUserCount = current_good_user_count;
                        break;
                    }
                default:
                    break;
            }
        }

        const int Devil_Picture_Box = 1;
        const int Angel_Picture_Box = 2;

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


        public void On_VisibilityChanged(bool b, int id)
        {
            switch (id)
            {
                case Devil_Picture_Box:
                    {
                        this.pictureBox3.Visible = b;
                        this.pictureBox3.Invalidate();
                        devilRiseTimer = new Timer();
                        devilRiseTimer.Tick += devilRiseTimer_Tick;
                        devilRiseTimer.Interval = 170; // milliseconds
                        devilRiseWatch = new Stopwatch();
                        devilRiseTimer.Start();
                        devilRiseWatch.Start();
                        break;
                    }
                case Angel_Picture_Box:
                    {
                        this.pictureBox2.Visible = b;
                        this.pictureBox2.Invalidate();
                        angelRiseTimer = new Timer();
                        angelRiseTimer.Tick += angelRiseTimer_Tick;
                        angelRiseTimer.Interval = 170; // milliseconds
                        angelRiseWatch = new Stopwatch();
                        angelRiseTimer.Start();
                        angelRiseWatch.Start();
                        break;
                    }
                default:
                    break;
            }
        }

        private void devilRiseTimer_Tick(object sender, EventArgs e)
        {
            if (devilRiseWatch.ElapsedMilliseconds >= 10000)
            {
                devilRiseTimer.Stop();
                devilRiseWatch.Stop();
                label1.Visible = true;
                label1.Refresh();
                pictureBox3.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\devilClock1.png");
                pictureBox3.Refresh();
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
            pictureBox3.Location = new System.Drawing.Point(pictureBox3.Location.X, pictureBox3.Location.Y - 12);
        }

        private void angelRiseTimer_Tick(object sender, EventArgs e)
        {
            if (angelRiseWatch.ElapsedMilliseconds >= 10000)
            {
                angelRiseTimer.Stop();
                angelRiseWatch.Stop();
                label2.Visible = true;
                label2.Refresh();
                pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock1.png");
                pictureBox3.Refresh();
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
            pictureBox2.Location = new System.Drawing.Point(pictureBox2.Location.X, pictureBox2.Location.Y - 12);
        }


        private void angelTimer_Tick(object sender, EventArgs e)
        {
            char[] times = label2.Text.ToCharArray();
            int digit_sec = Convert.ToInt32(new string(times[4], 1));
            int tens_sec = Convert.ToInt32(new string(times[3], 1));
            int digit_min = Convert.ToInt32(new string(times[1], 1));
            int tens_min = Convert.ToInt32(new string(times[0], 1));
            if (current_bad_user_count == 0)
            {
                if (current_good_user_count == 1)
                {
                    pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock1_flower.png");
                }
                else if (current_good_user_count == 2)
                {
                    pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock2_flower.png");
                }
                else if (current_good_user_count == 3)
                {
                    pictureBox2.Image = Image.FromFile("H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\angelClock3_flower.png");
                }
            }
            if (current_good_user_count == 1)
            {
                //System.Diagnostics.Debug.WriteLine(digit_sec);
                if (digit_sec < 9)
                {
                    //System.Diagnostics.Debug.WriteLine("here");
                    digit_sec++;
                }
                else
                {
                    digit_sec = 0;
                    if (tens_sec < 5)
                    {
                        tens_sec++;
                    }
                    else
                    {
                        tens_sec = 0;
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
                    }
                    else
                    {
                        tens_sec = 0;
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
                    }
                    else
                    {
                        tens_sec = 0;
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
            label2.Text = new_time;
            label2.Invalidate();
            //System.Diagnostics.Debug.WriteLine("new: " + label1.Text);
        }

        private void devilTimer_Tick(object sender, EventArgs e)
        {
            char[] times = label1.Text.ToCharArray();
            badUserCount = current_bad_user_count;
            int digit_sec = Convert.ToInt32(new string(times[4], 1));
            int tens_sec = Convert.ToInt32(new string(times[3], 1));
            int digit_min = Convert.ToInt32(new string(times[1], 1));
            int tens_min = Convert.ToInt32(new string(times[0], 1));
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
                    }
                    else
                    {
                        tens_sec = 0;
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
                    }
                    else
                    {
                        tens_sec = 0;
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
                    }
                    else
                    {
                        tens_sec = 0;
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

        double aCriteria = 0.075;
        double bCriteria = 0;
        double cCriteria = -0.5;
        double dCriteria = -0.5;
        double eCriteria = 0;
        double fCriteria = 0.05;
        double gCriteria = 0.2;
        double hCriteria = 0.131;
        double iCriteria = 0.331;
        double kkCriteria = -0.052;
        double lCriteria = -0.052;
        double mCriteria = 0.13;
        double nCriteria = 0.04;

        public void On_BodyFrameArrived(List<Body> bodies, int counter1)
        {
            //foreach(Body body in bodies)
            //{
            //    for(int o=60; o<counter1; o+=60)
            //    {
            //        int legCrossedCount =0;
            //        int slouchCount =0;
            //        int standartCount =0;
            //        int standardViewingHeightCount =0;
            //        for(int p=o-60; p<counter1; p++)
            //        {
            //            if()
            //        }
            //    }

            //}

        }

    }
}
