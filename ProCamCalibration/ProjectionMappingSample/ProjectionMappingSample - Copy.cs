using RoomAliveToolkit;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Kinect;
using Kinect2Serializer;
using Kinect2SimpleServer;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RoomAliveToolkit
{
    public class ProjectionMappingSample : ApplicationContext
    {
        private int framesInMemory = 10000;
        public event FeedbackChangedHandler PFeedbackChanged;
        public delegate void FeedbackChangedHandler(int bodyId, Kinect2SBody body, Tuple<ProjectionFeedback, int> pFeedbackTuple, float headX, float headY);
        public event MobileFeedbackChangedHandler MFeedbackChanged;
        public delegate void MobileFeedbackChangedHandler(int bodyId, Kinect2SBody body, Tuple<MobileFeedback, int> mFeedbackTuple, float headX, float headY);
        public event ClockChangedHandler ClockChanged;
        public delegate void ClockChangedHandler(List<int> goodUserCountRecord, List<int> badUserCountRecord);

        public enum Posture
        {
            Leg, Slouch, Height, Distance
        }

        public class PostureFrame
        {
            private HashSet<Posture> postures = new HashSet<Posture>();
            public HashSet<Posture> Postures
            {
                get
                {
                    return this.postures;
                }
            }

            private Dictionary<JointType, CameraSpacePoint> joints = new Dictionary<JointType, CameraSpacePoint>();
            public Dictionary<JointType, CameraSpacePoint> Joints
            {
                get
                {
                    return this.joints;
                }
            }
        }

        public enum ProjectionFeedback
        {
            LegCrossed, Slouch, ShortDistance, Standard, BodyStationary, HeadStationary, ArmStationary, LegStationary
        }

        public enum MobileFeedback
        {
            LegCrossed, Slouch, LowHeight, Good, BodyStationary, HeadStationary, ArmStationary, LegStationary
        }

        public class User
        {
            private List<PostureFrame> postureFrames = new List<PostureFrame>();
            public List<PostureFrame> PostureFrames
            {
                get
                {
                    return this.postureFrames;
                }
            }

            private List<ProjectionFeedback> projectionFeedbacks = new List<ProjectionFeedback>();
            public List<ProjectionFeedback> ProjectionFeedbacks
            {
                get
                {
                    return this.projectionFeedbacks;
                }
            }

            private List<MobileFeedback> mobileFeedbacks = new List<MobileFeedback>();
            public List<MobileFeedback> MobileFeedbacks
            {
                get
                {
                    return this.mobileFeedbacks;
                }
            }
        }

        //1 2 3 4 5 6 7 8
        Dictionary<ulong, User> users = new Dictionary<ulong, User>();

        Dictionary<ulong, bool[]> userIsLowHeight = new Dictionary<ulong, bool[]>();
        Dictionary<ulong, bool[]> userIsGood = new Dictionary<ulong, bool[]>();

        List<ulong> idList = new List<ulong>();
        int o = 0;
        double a;
        double b;
        double c;
        double d;
        double e;
        double f;
        double g;
        double h;
        double i;
        double j;
        double k;
        double l;
        double m;
        double n;
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
        double mCriteria = 2;
        double nCriteria = 0.04;
        private List<JointType> headRegion = new List<JointType>() { JointType.Head, JointType.Neck };
        private List<JointType> armRegion = new List<JointType>() { JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight };
        private List<JointType> trunkRegion = new List<JointType>() { JointType.ShoulderLeft, JointType.ShoulderRight, JointType.SpineShoulder, JointType.SpineBase, JointType.SpineMid };
        private List<JointType> legRegion = new List<JointType>() { JointType.KneeLeft, JointType.HipLeft, JointType.AnkleLeft, JointType.FootLeft, JointType.KneeRight, JointType.HipRight, JointType.AnkleRight, JointType.FootRight };
        public List<int> goodPostureUserRecord = new List<int>();
        public List<int> badPostureUserRecord = new List<int>();
        private Kinect2SimpleServer.Kinect2SimpleServer kinectServer;

        [STAThread]
        static void Main(string[] args)
        {
            Application.Run(new ProjectionMappingSample(args));
        }

        public ProjectionMappingSample(string[] args)
        {
            // kinect server
            this.kinectServer = new Kinect2SimpleServer.Kinect2SimpleServer(8000);
            this.kinectServer.BodyFrameReceived += kinectServer_BodyFrameReceived;
            this.kinectServer.Start();

            // load ensemble.xml
            string path = args[0];
            string directory = Path.GetDirectoryName(path);
            ensemble = RoomAliveToolkit.ProjectorCameraEnsemble.FromFile(path);

            // create d3d device
            var factory = new Factory();
            var adapter = factory.Adapters[0];
            device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.None);

            // shaders
            depthAndColorShader = new DepthAndColorShader(device);
            projectiveTexturingShader = new ProjectiveTexturingShader(device);
            passThroughShader = new PassThrough(device, userViewTextureWidth, userViewTextureHeight);
            radialWobbleShader = new RadialWobble(device, userViewTextureWidth, userViewTextureHeight);
            meshShader = new MeshShader(device);
            fromUIntPS = new FromUIntPS(device, depthImageWidth, depthImageHeight);
            bilateralFilter = new BilateralFilter(device, depthImageWidth, depthImageHeight);

            // create device objects for each camera
            foreach (var camera in ensemble.cameras)
                cameraDeviceResources[camera] = new CameraDeviceResource(device, camera, renderLock, directory);

            // one user view
            // user view render target, depth buffer, viewport for user view
            var userViewTextureDesc = new Texture2DDescription()
            {
                Width = userViewTextureWidth,
                Height = userViewTextureHeight,
                MipLevels = 1, // revisit this; we may benefit from mipmapping?
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            var userViewRenderTarget = new Texture2D(device, userViewTextureDesc);
            userViewRenderTargetView = new RenderTargetView(device, userViewRenderTarget);
            userViewSRV = new ShaderResourceView(device, userViewRenderTarget);

            var filteredUserViewRenderTarget = new Texture2D(device, userViewTextureDesc);
            filteredUserViewRenderTargetView = new RenderTargetView(device, filteredUserViewRenderTarget);
            filteredUserViewSRV = new ShaderResourceView(device, filteredUserViewRenderTarget);

            // user view depth buffer
            var userViewDpethBufferDesc = new Texture2DDescription()
            {
                Width = userViewTextureWidth,
                Height = userViewTextureHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D32_Float, // necessary?
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None
            };
            var userViewDepthStencil = new Texture2D(device, userViewDpethBufferDesc);
            userViewDepthStencilView = new DepthStencilView(device, userViewDepthStencil);

            // user view viewport
            userViewViewport = new Viewport(0, 0, userViewTextureWidth, userViewTextureHeight, 0f, 1f);


            // create a form for each projector
            foreach (var projector in ensemble.projectors)
            {
                var form = new ProjectorForm(factory, device, renderLock, projector);
                if (fullScreenEnabled)
                    form.FullScreen = fullScreenEnabled;
                form.Show();
                projectorForms.Add(form);
            }

            // example 3d object
            var mesh = Mesh.FromOBJFile("Content/d1.obj");
            meshDeviceResources = new MeshDeviceResources(device, imagingFactory, mesh);

            userViewForm = new MainForm(factory, device, renderLock);
            userViewForm.Show();
            //userViewForm.ClientSize = new System.Drawing.Size(1920, 1080);

            userViewForm.videoPanel1.MouseClick += videoPanel1_MouseClick;

            foreach (ProjectorForm form in this.projectorForms)
            {
                MainForm mainForm = userViewForm as MainForm;
                mainForm.VisibilityChanged += form.On_VisibilityChanged;
                mainForm.ImageChanged += form.On_ImageChanged;
                this.PFeedbackChanged += form.On_FeedbackChanged;
                this.ClockChanged += form.On_ClockedChanged;
                //this.kinectServer.BodyFrameArrived += form.On_BodyFrameArrived;
            }

            // connect to local camera to acquire head position
            if (localHeadTrackingEnabled)
            {
                localKinectSensor = KinectSensor.GetDefault();
                bodyFrameReader = localKinectSensor.BodyFrameSource.OpenReader();
                localKinectSensor.Open();
                System.Diagnostics.Debug.WriteLine("connected to local camera");
                new System.Threading.Thread(LocalBodyLoop).Start();
            }


            if (liveDepthEnabled)
            {
                foreach (var cameraDeviceResource in cameraDeviceResources.Values)
                    cameraDeviceResource.StartLive();
            }


            new System.Threading.Thread(RenderLoop).Start();
        }

        private void kinectServer_BodyFrameReceived(Kinect2SBodyFrame serializableBodyFrame)
        {
            if (serializableBodyFrame.Bodies.Count == 0)
            {
                // ignore
                return;
            }

            // remove users who disappear
            List<ulong> trackingIds = new List<ulong>();
            List<ulong> idsToRemove = new List<ulong>();
            foreach (Kinect2SBody body in serializableBodyFrame.Bodies)
            {
                trackingIds.Add(body.TrackingId);
            }
            foreach (ulong userId in this.users.Keys)
            {
                if (!trackingIds.Contains(userId))
                {
                    idsToRemove.Add(userId);
                }
            }
            foreach (ulong userId in idsToRemove)
            {
                this.users.Remove(userId);
            }
            int goodPostureUser = 0;
            int badPostureUser = 0;
            int bodyId = 1;
            foreach (Kinect2SBody body in serializableBodyFrame.Bodies)
            {
                ulong trackingId = body.TrackingId;

                User user;
                this.users.TryGetValue(trackingId, out user);

                // new user
                if (user == null)
                {
                    user = new User();
                    this.users.Add(trackingId, user);
                }

                // todo fix
                //this.SkeletonDrawing(body, postureFrame);

                // resize posture frames and feedbacks list
                if (user.PostureFrames.Count == this.framesInMemory)
                {
                    user.PostureFrames.RemoveRange(0, 9000);
                }
                if (user.ProjectionFeedbacks.Count == this.framesInMemory)
                {
                    user.ProjectionFeedbacks.RemoveRange(0, 9000);
                }

                // new posture frame
                PostureFrame postureFrame = new PostureFrame();
                user.PostureFrames.Add(postureFrame);

                a = Math.Abs(body.Joints[JointType.FootLeft].CameraSpacePoint.Y - body.Joints[JointType.FootRight].CameraSpacePoint.Y);
                b = body.Joints[JointType.KneeLeft].CameraSpacePoint.Y;
                c = body.Joints[JointType.FootLeft].CameraSpacePoint.Y;
                d = body.Joints[JointType.FootRight].CameraSpacePoint.Y;
                e = body.Joints[JointType.KneeRight].CameraSpacePoint.Y;
                //back 
                f = body.Joints[JointType.SpineMid].CameraSpacePoint.Z - body.Joints[JointType.SpineBase].CameraSpacePoint.Z;
                g = body.Joints[JointType.SpineShoulder].CameraSpacePoint.Y - body.Joints[JointType.SpineBase].CameraSpacePoint.Y;
                h = Math.Sqrt((body.Joints[JointType.SpineMid].CameraSpacePoint.Z - body.Joints[JointType.SpineBase].CameraSpacePoint.Z)
                         * (body.Joints[JointType.SpineMid].CameraSpacePoint.Z - body.Joints[JointType.SpineBase].CameraSpacePoint.Z)
                         + (body.Joints[JointType.SpineMid].CameraSpacePoint.X - body.Joints[JointType.SpineBase].CameraSpacePoint.X)
                         * (body.Joints[JointType.SpineMid].CameraSpacePoint.X - body.Joints[JointType.SpineBase].CameraSpacePoint.X));
                i = body.Joints[JointType.SpineShoulder].CameraSpacePoint.Y - body.Joints[JointType.SpineBase].CameraSpacePoint.Y;
                k = body.Joints[JointType.SpineShoulder].CameraSpacePoint.Z - 2 * body.Joints[JointType.SpineMid].CameraSpacePoint.Z
                         + body.Joints[JointType.SpineBase].CameraSpacePoint.Z;
                l = body.Joints[JointType.SpineShoulder].CameraSpacePoint.Y - 2 * body.Joints[JointType.SpineMid].CameraSpacePoint.Y
                         + body.Joints[JointType.SpineBase].CameraSpacePoint.Y;
                //height
                n = Math.Sqrt((body.Joints[JointType.Neck].CameraSpacePoint.Z - body.Joints[JointType.Head].CameraSpacePoint.Z)
                         * (body.Joints[JointType.Neck].CameraSpacePoint.Z - body.Joints[JointType.Head].CameraSpacePoint.Z)
                         + (body.Joints[JointType.Neck].CameraSpacePoint.X - body.Joints[JointType.Head].CameraSpacePoint.X)
                         * (body.Joints[JointType.Neck].CameraSpacePoint.X - body.Joints[JointType.Head].CameraSpacePoint.X));
                //distance
                m = body.Joints[JointType.Head].CameraSpacePoint.Z;
                float headX = body.Joints[JointType.Head].CameraSpacePoint.X;
                float headY = body.Joints[JointType.Head].CameraSpacePoint.Y;

                if (a > aCriteria || ((d > dCriteria || c > cCriteria) && !(b > bCriteria && e > eCriteria)))
                {
                    postureFrame.Postures.Add(Posture.Leg);
                }
                if (k < kkCriteria || l > lCriteria || (h > hCriteria && f > fCriteria) || (i < iCriteria && g > gCriteria))
                {
                    postureFrame.Postures.Add(Posture.Slouch);
                }
                if (!(n < nCriteria))
                {
                    postureFrame.Postures.Add(Posture.Height);
                }
                if (m < mCriteria)
                {
                    postureFrame.Postures.Add(Posture.Distance);
                }

                // find posture feedback (default: standard)
                ProjectionFeedback pfeedback = ProjectionFeedback.Standard;
                MobileFeedback mfeedback = MobileFeedback.Good;
                int postureFrameCount = user.PostureFrames.Count;
                if (postureFrameCount >= 60 && postureFrameCount % 60 == 0)
                {
                    int legCount = 0;
                    int slouchCount = 0;
                    int lowHeightCount = 0;
                    int shortDistanceCount = 0;
                    bool projectionStandard = true;
                    bool mobileGood = true;

                    for (int p = postureFrameCount - 60; p < postureFrameCount; p++)
                    {
                        PostureFrame previousFrame = user.PostureFrames[p];

                        if (previousFrame.Postures.Contains(Posture.Leg))
                        {
                            legCount++;
                        }
                        if (previousFrame.Postures.Contains(Posture.Slouch))
                        {
                            slouchCount++;
                        }
                        if (previousFrame.Postures.Contains(Posture.Distance))
                        {
                            shortDistanceCount++;
                        }
                        if (legCount > 40 || slouchCount > 40 || shortDistanceCount > 40)
                        {
                            if (legCount > 40)
                            {
                                pfeedback = ProjectionFeedback.LegCrossed;
                            }
                            else if (slouchCount > 40)
                            {
                                pfeedback = ProjectionFeedback.Slouch;
                            }
                            else
                            {
                                pfeedback = ProjectionFeedback.ShortDistance;
                            }
                        }
                        if (legCount > 40 || slouchCount > 40 || lowHeightCount > 40)
                        {
                            if (legCount > 40)
                            {
                                mfeedback = MobileFeedback.LegCrossed;
                            }
                            else if (slouchCount > 40)
                            {
                                mfeedback = MobileFeedback.Slouch;
                            }
                            else
                            {
                                mfeedback = MobileFeedback.LowHeight;
                            }
                        }
                    }

                    if (pfeedback == ProjectionFeedback.Standard)
                    {
                        if (postureFrameCount >= 30 * 30)
                        {
                            int q = 0;
                            if (findSamePosture(armRegion, trackingId) || findSamePosture(legRegion, trackingId)
                                || findSamePosture(trunkRegion, trackingId) || findSamePosture(headRegion, trackingId))
                            {
                                if (findSamePosture(armRegion, trackingId))
                                {
                                    pfeedback = ProjectionFeedback.ArmStationary;
                                    q++;
                                }
                                if (findSamePosture(legRegion, trackingId))
                                {
                                    pfeedback = ProjectionFeedback.LegStationary;
                                    q++;
                                }
                                if (findSamePosture(trunkRegion, trackingId))
                                {
                                    pfeedback = ProjectionFeedback.BodyStationary;
                                    q++;
                                }
                                if (findSamePosture(headRegion, trackingId))
                                {
                                    pfeedback = ProjectionFeedback.HeadStationary;
                                    q++;
                                }
                                if (q >= 2)
                                {
                                    pfeedback = ProjectionFeedback.BodyStationary;
                                }
                            }
                        }
                    }
                    if (mfeedback == MobileFeedback.Good)
                    {
                        if (postureFrameCount >= 30 * 30)
                        {
                            int q = 0;
                            if (findSamePosture(armRegion, trackingId) || findSamePosture(legRegion, trackingId)
                                || findSamePosture(trunkRegion, trackingId) || findSamePosture(headRegion, trackingId))
                            {
                                if (findSamePosture(armRegion, trackingId))
                                {
                                    mfeedback = MobileFeedback.ArmStationary;
                                    q++;
                                }
                                if (findSamePosture(legRegion, trackingId))
                                {
                                    mfeedback = MobileFeedback.LegStationary;
                                    q++;
                                }
                                if (findSamePosture(trunkRegion, trackingId))
                                {
                                    mfeedback = MobileFeedback.BodyStationary;
                                    q++;
                                }
                                if (findSamePosture(headRegion, trackingId))
                                {
                                    mfeedback = MobileFeedback.HeadStationary;
                                    q++;
                                }
                                if (q >= 2)
                                {
                                    mfeedback = MobileFeedback.BodyStationary;
                                }
                            }
                        }
                    }


                    user.ProjectionFeedbacks.Add(pfeedback);
                    user.MobileFeedbacks.Add(mfeedback);
                    //System.Diagnostics.Debug.WriteLine(pfeedback);

                    // find posture feedback duration
                    int pDuration = getPFeedbackDuration(user);
                    Tuple<ProjectionFeedback, int> pFeedbackTuple = new Tuple<ProjectionFeedback, int>(pfeedback, pDuration);
                    this.PFeedbackChanged(bodyId, body, pFeedbackTuple, headX, headY);
                    

                    int mDuration = getMFeedbackDuration(user);
                    Tuple<MobileFeedback, int> mFeedbackTuple = new Tuple<MobileFeedback, int>(mfeedback, mDuration);
                    this.SendPostureFeedbackToAndroid(bodyId, mFeedbackTuple);
                    if (user.ProjectionFeedbacks[user.ProjectionFeedbacks.Count - 1] == ProjectionFeedback.Standard)
                    {
                        goodPostureUser++;
                    }
                    else
                    {
                        badPostureUser++;
                    }
                } // find posture feedback end
                bodyId++;             
            } // body
            goodPostureUserRecord.Add(goodPostureUser);
            badPostureUserRecord.Add(badPostureUser);
            this.ClockChanged(goodPostureUserRecord, badPostureUserRecord);
        }

        private void SendPostureFeedbackToAndroid(int bodyId, Tuple<MobileFeedback, int> mFeedbackTuple)
        {
            try
            {
                string message = String.Format("{0}, {1}, {2}", bodyId, mFeedbackTuple.Item1, mFeedbackTuple.Item2);

                TcpClient client = new TcpClient("138.251.207.116", 8080);
                NetworkStream clientStream = client.GetStream();
                
                byte[] bytesToSend = Encoding.ASCII.GetBytes(message);
                clientStream.Write(bytesToSend, 0, bytesToSend.Length);

                clientStream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
        }


        private int getPFeedbackDuration(User user)
        {
            int feedbackDuration = 1;
            int postureFeedbacksCount = user.ProjectionFeedbacks.Count;
            ProjectionFeedback currentFeedback = user.ProjectionFeedbacks[postureFeedbacksCount - 1];
            for (int i = postureFeedbacksCount - 2; i >= 0; i--)
            {
                ProjectionFeedback previousFeedback = user.ProjectionFeedbacks[i];
                if (previousFeedback == currentFeedback)
                {
                    feedbackDuration++;
                }
                else
                {
                    break;
                }
            }
            return feedbackDuration;
        }

        private int getMFeedbackDuration(User user)
        {
            int feedbackDuration = 1;
            int postureFeedbacksCount = user.MobileFeedbacks.Count;
            MobileFeedback currentFeedback = user.MobileFeedbacks[postureFeedbacksCount - 1];
            for (int i = postureFeedbacksCount - 2; i >= 0; i--)
            {
                MobileFeedback previousFeedback = user.MobileFeedbacks[i];
                if (previousFeedback == currentFeedback)
                {
                    feedbackDuration++;
                }
                else
                {
                    break;
                }
            }
            return feedbackDuration;
        }

        private List<Dictionary<JointType, CameraSpacePoint>> jointsHistory = new List<Dictionary<JointType, CameraSpacePoint>>();
        private void getPostureDuration(List<JointType> jtList, Kinect2SBody body)
        {
            Dictionary<JointType, CameraSpacePoint> jointsPositions = new Dictionary<JointType, CameraSpacePoint>();

            foreach (JointType jt in jtList)
            {
                //Debug.WriteLine(string.Format("jointtype: {0}, position: [{1}, {2}, {3}]", jt.JointType, jt.Position.X, jt.Position.Y, jt.Position.Z));
                CameraSpacePoint coordinates = body.Joints[jt].CameraSpacePoint;
                jointsPositions.Add(jt, coordinates);
            }

            jointsHistory.Add(jointsPositions);
        }

        private bool findSamePosture(List<JointType> bodyRegion, ulong trackingId)
        {
            List<PostureFrame> userPostureFrames = this.users[trackingId].PostureFrames;
            int postureFramesCount = userPostureFrames.Count;

            PostureFrame currentFrame = userPostureFrames[postureFramesCount - 1];

            int totalJointCount = 0;
            int sameJointCount = 0;
            for (int i = postureFramesCount - 900; i < postureFramesCount - 1; i++)
            {
                PostureFrame previousFrame = userPostureFrames[i];
                foreach (JointType jtType in bodyRegion)
                {
                    if (!currentFrame.Joints.ContainsKey(jtType) || !previousFrame.Joints.ContainsKey(jtType))
                    {
                        continue;
                    }

                    totalJointCount++;
                    CameraSpacePoint currentPosition = currentFrame.Joints[jtType];
                    CameraSpacePoint previousPosition = previousFrame.Joints[jtType];

                    if (Math.Abs(previousPosition.X - currentPosition.X) <= 0.04 &&
                        Math.Abs(previousPosition.X - currentPosition.Y) <= 0.04 &&
                        Math.Abs(previousPosition.X - currentPosition.Z) <= 0.04)
                    {
                        sameJointCount++;
                    }
                }
            }

            if (sameJointCount > totalJointCount * 0.9)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        void videoPanel1_MouseClick(object sender, MouseEventArgs e)
        {
            alpha = 0;
        }


        const int userViewTextureWidth = 2000;
        const int userViewTextureHeight = 1000;
        List<ProjectorForm> projectorForms = new List<ProjectorForm>();
        DepthAndColorShader depthAndColorShader;
        ProjectiveTexturingShader projectiveTexturingShader;
        Dictionary<ProjectorCameraEnsemble.Camera, CameraDeviceResource> cameraDeviceResources = new Dictionary<ProjectorCameraEnsemble.Camera, CameraDeviceResource>();
        Object renderLock = new Object();
        RenderTargetView userViewRenderTargetView, filteredUserViewRenderTargetView;
        DepthStencilView userViewDepthStencilView;
        ShaderResourceView userViewSRV, filteredUserViewSRV;
        Viewport userViewViewport;
        SharpDX.Direct3D11.Device device;
        ProjectorCameraEnsemble ensemble;
        Form1 userViewForm;
        MeshShader meshShader;
        MeshDeviceResources meshDeviceResources;
        PassThrough passThroughShader;
        RadialWobble radialWobbleShader;
        FromUIntPS fromUIntPS;
        BilateralFilter bilateralFilter;
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        PointLight pointLight = new PointLight();


        SharpDX.WIC.ImagingFactory2 imagingFactory = new ImagingFactory2();
        float alpha = 1;

        // TODO: make so these can be changed live, put in menu
        bool threeDObjectEnabled = Properties.Settings.Default.ThreeDObjectEnabled;
        bool wobbleEffectEnabled = Properties.Settings.Default.WobbleEffectEnabled;
        bool localHeadTrackingEnabled = Properties.Settings.Default.LocalHeadTrackingEnabled;
        bool liveDepthEnabled = Properties.Settings.Default.LiveDepthEnabled;
        bool fullScreenEnabled = Properties.Settings.Default.FullScreenEnabled;


        void RenderLoop()
        {

            while (true)
            {
                lock (renderLock)
                {
                    var deviceContext = device.ImmediateContext;

                    // render user view
                    deviceContext.ClearRenderTargetView(userViewRenderTargetView, Color4.Black);
                    deviceContext.ClearDepthStencilView(userViewDepthStencilView, DepthStencilClearFlags.Depth, 1, 0);

                    SharpDX.Vector3 headPosition = new SharpDX.Vector3(0f, 1.1f, -1.4f); // may need to change this default

                    if (localHeadTrackingEnabled)
                    {
                        float distanceSquared = 0;
                        lock (headCameraSpacePointLock)
                        {
                            headPosition = new SharpDX.Vector3(headCameraSpacePoint.X, headCameraSpacePoint.Y, headCameraSpacePoint.Z);

                            float dx = handLeftCameraSpacePoint.X - handRightCameraSpacePoint.X;
                            float dy = handLeftCameraSpacePoint.Y - handRightCameraSpacePoint.Y;
                            float dz = handLeftCameraSpacePoint.Z - handRightCameraSpacePoint.Z;
                            distanceSquared = dx * dx + dy * dy + dz * dz;
                        }
                        var transform = SharpDX.Matrix.RotationY((float)Math.PI) * SharpDX.Matrix.Translation(-0.25f, 0.45f, 0);
                        headPosition = SharpDX.Vector3.TransformCoordinate(headPosition, transform);

                        if (trackingValid && (distanceSquared < 0.02f) && (alpha > 1))
                            alpha = 0;
                        //Console.WriteLine(distanceSquared);
                    }

                    var userView = LookAt(headPosition, headPosition + SharpDX.Vector3.UnitZ, SharpDX.Vector3.UnitY);
                    userView.Transpose();


                    //Console.WriteLine("headPosition = " + headPosition);


                    float aspect = (float)userViewTextureWidth / (float)userViewTextureHeight;
                    var userProjection = PerspectiveFov(55.0f / 180.0f * (float)Math.PI, aspect, 0.001f, 1000.0f);
                    userProjection.Transpose();

                    // smooth depth images
                    foreach (var camera in ensemble.cameras)
                    {
                        var cameraDeviceResource = cameraDeviceResources[camera];
                        if (cameraDeviceResource.depthImageChanged)
                        {
                            fromUIntPS.Render(deviceContext, cameraDeviceResource.depthImageTextureRV, cameraDeviceResource.floatDepthImageRenderTargetView);
                            for (int i = 0; i < 1; i++)
                            {
                                bilateralFilter.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, cameraDeviceResource.floatDepthImageRenderTargetView2);
                                bilateralFilter.Render(deviceContext, cameraDeviceResource.floatDepthImageRV2, cameraDeviceResource.floatDepthImageRenderTargetView);
                            }
                            cameraDeviceResource.depthImageChanged = false;
                        }
                    }

                    // wobble effect
                    if (wobbleEffectEnabled)
                        foreach (var camera in ensemble.cameras)
                        {
                            var cameraDeviceResource = cameraDeviceResources[camera];

                            var world = new SharpDX.Matrix();
                            for (int i = 0; i < 4; i++)
                                for (int j = 0; j < 4; j++)
                                    world[i, j] = (float)camera.pose[i, j];
                            world.Transpose();

                            // view and projection matrix are post-multiply
                            var userWorldViewProjection = world * userView * userProjection;

                            depthAndColorShader.SetConstants(deviceContext, camera.calibration, userWorldViewProjection);
                            depthAndColorShader.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, cameraDeviceResource.colorImageTextureRV, cameraDeviceResource.vertexBuffer, userViewRenderTargetView, userViewDepthStencilView, userViewViewport);
                        }

                    // 3d object
                    if (threeDObjectEnabled)
                    {
                        var world = SharpDX.Matrix.Scaling(1.0f) * SharpDX.Matrix.RotationY(90.0f / 180.0f * (float)Math.PI) *
                            SharpDX.Matrix.RotationX(-40.0f / 180.0f * (float)Math.PI) * SharpDX.Matrix.Translation(0, 0.7f, 0.0f);

                        var pointLight = new PointLight();
                        pointLight.position = new Vector3(0, 2, 0);
                        pointLight.Ia = new Vector3(0.1f, 0.1f, 0.1f);
                        meshShader.SetVertexShaderConstants(deviceContext, world, userView * userProjection, pointLight.position);
                        meshShader.Render(deviceContext, meshDeviceResources, pointLight, userViewRenderTargetView, userViewDepthStencilView, userViewViewport);
                    }

                    // wobble effect
                    if (wobbleEffectEnabled)
                    {
                        alpha += 0.05f;
                        if (alpha > 1)
                            radialWobbleShader.SetConstants(deviceContext, 0);
                        else
                            radialWobbleShader.SetConstants(deviceContext, alpha);
                        radialWobbleShader.Render(deviceContext, userViewSRV, filteredUserViewRenderTargetView);
                    }


                    // render user view to seperate form
                    passThroughShader.viewport = new Viewport(0, 0, userViewForm.Width, userViewForm.Height);
                    if (wobbleEffectEnabled)
                        passThroughShader.Render(deviceContext, filteredUserViewSRV, userViewForm.renderTargetView);
                    else
                        passThroughShader.Render(deviceContext, userViewSRV, userViewForm.renderTargetView);
                    userViewForm.swapChain.Present(0, PresentFlags.None);


                    // projection puts x and y in [-1,1]; adjust to obtain texture coordinates [0,1]
                    // TODO: put this in SetContants?
                    userProjection[0, 0] /= 2;
                    userProjection[1, 1] /= -2; // y points down
                    userProjection[2, 0] += 0.5f;
                    userProjection[2, 1] += 0.5f;

                    // projection mapping for each projector
                    foreach (var form in projectorForms)
                    {
                        deviceContext.ClearRenderTargetView(form.renderTargetView, Color4.Black);
                        deviceContext.ClearDepthStencilView(form.depthStencilView, DepthStencilClearFlags.Depth, 1, 0);

                        foreach (var camera in ensemble.cameras)
                        {
                            var cameraDeviceResource = cameraDeviceResources[camera];

                            var world = new SharpDX.Matrix();
                            for (int i = 0; i < 4; i++)
                                for (int j = 0; j < 4; j++)
                                    world[i, j] = (float)camera.pose[i, j];
                            world.Transpose();

                            var projectorWorldViewProjection = world * form.view * form.projection;
                            var userWorldViewProjection = world * userView * userProjection;

                            projectiveTexturingShader.SetConstants(deviceContext, userWorldViewProjection, projectorWorldViewProjection);
                            if (wobbleEffectEnabled)
                                projectiveTexturingShader.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, filteredUserViewSRV, cameraDeviceResource.vertexBuffer, form.renderTargetView, form.depthStencilView, form.viewport);
                            else
                                projectiveTexturingShader.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, userViewSRV, cameraDeviceResource.vertexBuffer, form.renderTargetView, form.depthStencilView, form.viewport);
                        }

                        form.swapChain.Present(1, PresentFlags.None);
                    }

                    //Console.WriteLine(stopwatch.ElapsedMilliseconds);
                    stopwatch.Restart();
                }
            }
        }

        KinectSensor localKinectSensor;
        BodyFrameReader bodyFrameReader;
        Body[] bodies = null;
        CameraSpacePoint headCameraSpacePoint, handLeftCameraSpacePoint, handRightCameraSpacePoint;
        Object headCameraSpacePointLock = new Object();
        bool trackingValid = false;

        void LocalBodyLoop()
        {
            while (true)
            {
                // find closest tracked head
                var bodyFrame = bodyFrameReader.AcquireLatestFrame();

                if (bodyFrame != null)
                {
                    if (bodies == null)
                        bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    bool foundTrackedBody = false;
                    float distanceToNearest = float.MaxValue;
                    var nearestHeadCameraSpacePoint = new CameraSpacePoint();
                    var nearestHandRightCameraSpacePoint = new CameraSpacePoint();
                    var nearestHandLeftCameraSpacePoint = new CameraSpacePoint();


                    foreach (var body in bodies)
                        if (body.IsTracked)
                        {
                            var cameraSpacePoint = body.Joints[JointType.Head].Position;
                            if (cameraSpacePoint.Z < distanceToNearest)
                            {
                                distanceToNearest = cameraSpacePoint.Z;
                                nearestHeadCameraSpacePoint = cameraSpacePoint;
                                nearestHandLeftCameraSpacePoint = body.Joints[JointType.HandLeft].Position;
                                nearestHandRightCameraSpacePoint = body.Joints[JointType.HandRight].Position;
                                foundTrackedBody = true;
                            }
                        }

                    lock (headCameraSpacePointLock)
                    {
                        if (foundTrackedBody)
                        {
                            headCameraSpacePoint = nearestHeadCameraSpacePoint;
                            handLeftCameraSpacePoint = nearestHandLeftCameraSpacePoint;
                            handRightCameraSpacePoint = nearestHandRightCameraSpacePoint;

                            trackingValid = true;
                            //Console.WriteLine("{0} {1} {2}", headCameraSpacePoint.X, headCameraSpacePoint.Y, headCameraSpacePoint.Z);
                        }
                        else
                        {
                            headCameraSpacePoint.X = 0f;
                            headCameraSpacePoint.Y = 0.3f;
                            headCameraSpacePoint.Z = 1.5f;

                            trackingValid = false;
                        }
                    }

                    bodyFrame.Dispose();
                }
                else
                    System.Threading.Thread.Sleep(5);
            }
        }




        public const int depthImageWidth = 512;
        public const int depthImageHeight = 424;
        public const int colorImageWidth = 1920;
        public const int colorImageHeight = 1080;
        class CameraDeviceResource : IDisposable
        {
            // encapsulates d3d resources for a camera
            public CameraDeviceResource(SharpDX.Direct3D11.Device device, ProjectorCameraEnsemble.Camera camera, Object renderLock, string directory)
            {
                this.device = device;
                this.camera = camera;
                this.renderLock = renderLock;

                // Kinect depth image
                var depthImageTextureDesc = new Texture2DDescription()
                {
                    Width = 512,
                    Height = 424,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.R16_UInt,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.Write,
                };
                depthImageTexture = new Texture2D(device, depthImageTextureDesc);
                depthImageTextureRV = new ShaderResourceView(device, depthImageTexture);

                var floatDepthImageTextureDesc = new Texture2DDescription()
                {
                    Width = 512,
                    Height = 424,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.R32_Float,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                };

                floatDepthImageTexture = new Texture2D(device, floatDepthImageTextureDesc);
                floatDepthImageRV = new ShaderResourceView(device, floatDepthImageTexture);
                floatDepthImageRenderTargetView = new RenderTargetView(device, floatDepthImageTexture);

                floatDepthImageTexture2 = new Texture2D(device, floatDepthImageTextureDesc);
                floatDepthImageRV2 = new ShaderResourceView(device, floatDepthImageTexture2);
                floatDepthImageRenderTargetView2 = new RenderTargetView(device, floatDepthImageTexture2);

                // Kinect color image
                var colorImageStagingTextureDesc = new Texture2DDescription()
                {
                    Width = colorImageWidth,
                    Height = colorImageHeight,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.Write
                };
                colorImageStagingTexture = new Texture2D(device, colorImageStagingTextureDesc);

                var colorImageTextureDesc = new Texture2DDescription()
                {
                    Width = colorImageWidth,
                    Height = colorImageHeight,
                    MipLevels = 0,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.GenerateMipMaps
                };
                colorImageTexture = new Texture2D(device, colorImageTextureDesc);
                colorImageTextureRV = new ShaderResourceView(device, colorImageTexture);

                // vertex buffer
                var table = camera.calibration.ComputeDepthFrameToCameraSpaceTable();
                int numVertices = 6 * (depthImageWidth - 1) * (depthImageHeight - 1);
                var vertices = new VertexPosition[numVertices];

                Int3[] quadOffsets = new Int3[]
                {
                    new Int3(0, 0, 0),  
                    new Int3(1, 0, 0),  
                    new Int3(0, 1, 0),  
                    new Int3(1, 0, 0),  
                    new Int3(1, 1, 0),  
                    new Int3(0, 1, 0),  
                };

                int vertexIndex = 0;
                for (int y = 0; y < depthImageHeight - 1; y++)
                    for (int x = 0; x < depthImageWidth - 1; x++)
                        for (int i = 0; i < 6; i++)
                        {
                            int vertexX = x + quadOffsets[i].X;
                            int vertexY = y + quadOffsets[i].Y;

                            var point = table[depthImageWidth * vertexY + vertexX];

                            var vertex = new VertexPosition();
                            vertex.position = new SharpDX.Vector4(point.X, point.Y, vertexX, vertexY);
                            vertices[vertexIndex++] = vertex;
                        }

                var stream = new DataStream(numVertices * VertexPosition.SizeInBytes, true, true);
                stream.WriteRange(vertices);
                stream.Position = 0;

                var vertexBufferDesc = new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Usage = ResourceUsage.Default,
                    SizeInBytes = numVertices * VertexPosition.SizeInBytes,
                };
                vertexBuffer = new SharpDX.Direct3D11.Buffer(device, stream, vertexBufferDesc);

                vertexBufferBinding = new VertexBufferBinding(vertexBuffer, VertexPosition.SizeInBytes, 0);

                stream.Dispose();

                var colorImage = new RoomAliveToolkit.ARGBImage(colorImageWidth, colorImageHeight);
                ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, colorImage, directory + "/camera" + camera.name + "/colorDark.tiff");

                var depthImage = new RoomAliveToolkit.ShortImage(depthImageWidth, depthImageHeight);
                ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, depthImage, directory + "/camera" + camera.name + "/mean.tiff");

                lock (renderLock) // necessary?
                {
                    UpdateColorImage(device.ImmediateContext, colorImage.DataIntPtr);
                    UpdateDepthImage(device.ImmediateContext, depthImage.DataIntPtr);
                }

                colorImage.Dispose();
                depthImage.Dispose();




            }

            struct VertexPosition
            {
                public SharpDX.Vector4 position;
                static public int SizeInBytes { get { return 4 * 4; } }
            }

            public void Dispose()
            {
                depthImageTexture.Dispose();
                depthImageTextureRV.Dispose();
                colorImageTexture.Dispose();
                colorImageTextureRV.Dispose();
                colorImageStagingTexture.Dispose();
                vertexBuffer.Dispose();
            }

            SharpDX.Direct3D11.Device device;
            public Texture2D depthImageTexture, floatDepthImageTexture, floatDepthImageTexture2;
            public ShaderResourceView depthImageTextureRV, floatDepthImageRV, floatDepthImageRV2;
            public RenderTargetView floatDepthImageRenderTargetView, floatDepthImageRenderTargetView2;
            public Texture2D colorImageTexture;
            public ShaderResourceView colorImageTextureRV;
            public Texture2D colorImageStagingTexture;
            public SharpDX.Direct3D11.Buffer vertexBuffer;
            VertexBufferBinding vertexBufferBinding;
            ProjectorCameraEnsemble.Camera camera;
            public bool renderEnabled = true;

            public void UpdateDepthImage(DeviceContext deviceContext, IntPtr depthImage)
            {
                DataStream dataStream;
                deviceContext.MapSubresource(depthImageTexture, 0,
                   MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                dataStream.WriteRange(depthImage, depthImageWidth * depthImageHeight * 2);
                deviceContext.UnmapSubresource(depthImageTexture, 0);
            }

            public void UpdateDepthImage(DeviceContext deviceContext, byte[] depthImage)
            {
                DataStream dataStream;
                deviceContext.MapSubresource(depthImageTexture, 0,
                   MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                dataStream.WriteRange<byte>(depthImage, 0, depthImageWidth * depthImageHeight * 2);
                deviceContext.UnmapSubresource(depthImageTexture, 0);
            }

            public void UpdateColorImage(DeviceContext deviceContext, IntPtr colorImage)
            {
                DataStream dataStream;
                deviceContext.MapSubresource(colorImageStagingTexture, 0,
                    MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                dataStream.WriteRange(colorImage, colorImageWidth * colorImageHeight * 4);
                deviceContext.UnmapSubresource(colorImageStagingTexture, 0);

                var resourceRegion = new ResourceRegion()
                {
                    Left = 0,
                    Top = 0,
                    Right = colorImageWidth,
                    Bottom = colorImageHeight,
                    Front = 0,
                    Back = 1,
                };
                deviceContext.CopySubresourceRegion(colorImageStagingTexture, 0, resourceRegion, colorImageTexture, 0);
                deviceContext.GenerateMips(colorImageTextureRV);
            }

            public void UpdateColorImage(DeviceContext deviceContext, byte[] colorImage)
            {
                DataStream dataStream;
                deviceContext.MapSubresource(colorImageStagingTexture, 0,
                    MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                dataStream.WriteRange<byte>(colorImage, 0, colorImageWidth * colorImageHeight * 4);
                deviceContext.UnmapSubresource(colorImageStagingTexture, 0);

                var resourceRegion = new ResourceRegion()
                {
                    Left = 0,
                    Top = 0,
                    Right = colorImageWidth,
                    Bottom = colorImageHeight,
                    Front = 0,
                    Back = 1,
                };
                deviceContext.CopySubresourceRegion(colorImageStagingTexture, 0, resourceRegion, colorImageTexture, 0);
                deviceContext.GenerateMips(colorImageTextureRV);
            }

            public void Render(DeviceContext deviceContext)
            {
                deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
                deviceContext.VertexShader.SetShaderResource(0, depthImageTextureRV);
                deviceContext.PixelShader.SetShaderResource(0, colorImageTextureRV);
                deviceContext.Draw((depthImageWidth - 1) * (depthImageHeight - 1) * 6, 0);
            }

            bool live = false;

            public void StartLive()
            {
                live = true;
                //new System.Threading.Thread(ColorCameraLoop).Start();
                new System.Threading.Thread(DepthCameraLoop).Start();
            }

            public void StopLive()
            {
                live = false;
            }


            Object renderLock;
            public bool depthImageChanged = true;

            //byte[] colorData = new byte[4 * Kinect2.Kinect2Calibration.colorImageWidth * Kinect2.Kinect2Calibration.colorImageHeight];
            byte[] nextColorData = new byte[4 * Kinect2.Kinect2Calibration.colorImageWidth * Kinect2.Kinect2Calibration.colorImageHeight];
            SharpDX.WIC.ImagingFactory2 imagingFactory = new SharpDX.WIC.ImagingFactory2();
            void ColorCameraLoop()
            {
                while (true)
                {
                    var encodedColorData = camera.Client.LatestJPEGImage();

                    // decode JPEG
                    var memoryStream = new MemoryStream(encodedColorData);
                    var stream = new WICStream(imagingFactory, memoryStream);
                    // decodes to 24 bit BGR
                    var decoder = new SharpDX.WIC.BitmapDecoder(imagingFactory, stream, SharpDX.WIC.DecodeOptions.CacheOnLoad);
                    var bitmapFrameDecode = decoder.GetFrame(0);

                    // convert to 32 bpp
                    var formatConverter = new FormatConverter(imagingFactory);
                    formatConverter.Initialize(bitmapFrameDecode, SharpDX.WIC.PixelFormat.Format32bppBGR);
                    formatConverter.CopyPixels(nextColorData, 1920 * 4); // TODO: consider copying directly to texture native memory
                    //lock (colorData)
                    //    Swap<byte[]>(ref colorData, ref nextColorData);
                    lock (renderLock) // necessary?
                    {
                        UpdateColorImage(device.ImmediateContext, nextColorData);
                    }
                    memoryStream.Close();
                    memoryStream.Dispose();
                    stream.Dispose();
                    decoder.Dispose();
                    formatConverter.Dispose();
                    bitmapFrameDecode.Dispose();
                }
            }

            //byte[] depthData = new byte[2 * Kinect2.Kinect2Calibration.depthImageWidth * Kinect2.Kinect2Calibration.depthImageHeight];
            byte[] nextDepthData;
            void DepthCameraLoop()
            {
                while (true)
                {
                    nextDepthData = camera.Client.LatestDepthImage();
                    //lock (remoteDepthData)
                    //    Swap<byte[]>(ref remoteDepthData, ref nextRemoteDepthData);
                    lock (renderLock)
                    {
                        depthImageChanged = true;
                        UpdateDepthImage(device.ImmediateContext, nextDepthData);
                    }
                }
            }

            static void Swap<T>(ref T first, ref T second)
            {
                T temp = first;
                first = second;
                second = temp;
            }
        }



        public static SharpDX.Matrix ProjectionMatrixFromCameraMatrix(float fx, float fy, float cx, float cy, float w, float h, float near, float far)
        {
            // fx, fy, cx, cy are in pixels
            // input coordinate sysem is x left, y up, z foward (right handed)
            // project to view volume where x, y in [-1, 1], z in [0, 1], x right, y up, z forward
            // pre-multiply matrix

            // -(2 * fx / w),           0,   -(2 * cx / w - 1),                           0,
            //             0,  2 * fy / h,      2 * cy / h - 1,                           0,
            //             0,           0,  far / (far - near),  -near * far / (far - near),
            //             0,           0,                   1,                           0

            return new SharpDX.Matrix(
                -(2 * fx / w), 0, -(2 * cx / w - 1), 0,
                0, 2 * fy / h, 2 * cy / h - 1, 0,
                0, 0, far / (far - near), -near * far / (far - near),
                0, 0, 1, 0
                );
        }

        public static SharpDX.Matrix PerspectiveFov(float fieldOfViewY, float aspectRatio, float near, float far)
        {
            // right handed, pre multiply, x left, y up, z forward

            float h = 1f / (float)Math.Tan(fieldOfViewY / 2f);
            float w = h / aspectRatio;

            return new SharpDX.Matrix(
                -w, 0, 0, 0,
                0, h, 0, 0,
                0, 0, far / (far - near), -near * far / (far - near),
                0, 0, 1, 0
                );
        }

        public static SharpDX.Matrix LookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            // right handed, pre multiply, x left, y up, z forward

            var zaxis = Vector3.Normalize(cameraTarget - cameraPosition);
            var xaxis = Vector3.Normalize(Vector3.Cross(cameraUpVector, zaxis));
            var yaxis = Vector3.Cross(zaxis, xaxis);

            return new SharpDX.Matrix(
                xaxis.X, xaxis.Y, xaxis.Z, -Vector3.Dot(xaxis, cameraPosition),
                yaxis.X, yaxis.Y, yaxis.Z, -Vector3.Dot(yaxis, cameraPosition),
                zaxis.X, zaxis.Y, zaxis.Z, -Vector3.Dot(zaxis, cameraPosition),
                0, 0, 0, 1
            );
        }
    }
}