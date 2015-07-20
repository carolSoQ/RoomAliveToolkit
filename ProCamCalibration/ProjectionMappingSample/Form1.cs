using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.WIC;
using RoomAliveToolkit;
using System.Diagnostics;


namespace RoomAliveToolkit
{

    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();
        }

        public Form1(Factory factory, SharpDX.Direct3D11.Device device, Object renderLock)
        {
            InitializeComponent();
            this.factory = factory;
            this.device = device;
            this.renderLock = renderLock;
            //pictureBox3.Controls.Add(label1);
            //label1.Location = new System.Drawing.Point(0, 0);
            //label1.BackColor = System.Drawing.Color.Transparent;
            //setTransparentBack();
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


        //private void button6_Click(object sender, EventArgs e)
        //{
        //    this.label1.Text = "xxxxxxxxxxxxx";
        //    this.label1.Invalidate();
        //}
    }

    public class ProjectorForm : Form1
    {
        public ProjectorForm(Factory factory, SharpDX.Direct3D11.Device device, Object renderLock, ProjectorCameraEnsemble.Projector projector) : base(factory, device, renderLock)
        {
            this.projector = projector;
            Text = "Projector " + projector.name;
        }

        int bad_user_count = 1;
        int good_user_count = 1;

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
                        else if(num==1)
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
                        bad_user_count = num;
                        System.Diagnostics.Debug.WriteLine(bad_user_count);
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
                        else if (num== 1)
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
                        good_user_count = num;
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
                        angelRiseTimer.Interval = 170; // 30 milliseconds
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
            if (good_user_count==1)
            {
                //System.Diagnostics.Debug.WriteLine(digit_sec);
                if(digit_sec<9)
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
                        if(digit_min<9)
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
            else if(good_user_count==2)
            {
                if (digit_sec < 8)
                {
                    digit_sec = digit_sec+2;
                }
                else
                {
                    digit_sec = digit_sec-8;
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
            int digit_sec = Convert.ToInt32(new string(times[4], 1));
            int tens_sec = Convert.ToInt32(new string(times[3], 1));
            int digit_min = Convert.ToInt32(new string(times[1], 1));
            int tens_min = Convert.ToInt32(new string(times[0], 1));
            if (bad_user_count == 1)
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
            else if (bad_user_count == 2)
            {
                if (digit_sec < 8)
                {
                    digit_sec = digit_sec + 2;
                }
                else
                {
                    digit_sec = digit_sec-8;
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
                    digit_sec = digit_sec-7;
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

    }


}
 