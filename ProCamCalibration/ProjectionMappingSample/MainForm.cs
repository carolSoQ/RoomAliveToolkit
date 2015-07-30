using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.WIC;
using System.Threading;

namespace RoomAliveToolkit
{
    public partial class MainForm : RoomAliveToolkit.Form1
    {
        public event LabelTextChangedHandler LabelTextChanged;
        public delegate void LabelTextChangedHandler(string text);
        public event PictureBoxVisibilityChangedHandler VisibilityChanged;
        public delegate void PictureBoxVisibilityChangedHandler(bool b, int num);
        public event PictureBoxImageChangedHandler ImageChanged;
        public delegate void PictureBoxImageChangedHandler(string type, int num);

        public MainForm(Factory factory, SharpDX.Direct3D11.Device device, Object renderLock) : base(factory, device, renderLock)
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void badPostrureDetected_Click(object sender, EventArgs e)
        {
            this.VisibilityChanged(true, 1);
        }

        private void goodPostureDetected_Click(object sender, EventArgs e)
        {
            this.VisibilityChanged(true, 2);
        }

        private void bad1_Click(object sender, EventArgs e)
        {
            this.ImageChanged("devil", 1);
        }

        private void bad2_Click(object sender, EventArgs e)
        {
            this.ImageChanged("devil", 2);
        }

        private void bad3_Click(object sender, EventArgs e)
        {
            this.ImageChanged("devil", 3);
        }

        private void good1_Click(object sender, EventArgs e)
        {
            this.ImageChanged("angel", 1);
        }

        private void good2_Click(object sender, EventArgs e)
        {
            this.ImageChanged("angel", 2);
        }

        private void good3_Click(object sender, EventArgs e)
        {
            this.ImageChanged("angel", 3);
        }

        private void good0_Click(object sender, EventArgs e)
        {
            this.ImageChanged("angel", 0);
        }

        private void bad0_Click(object sender, EventArgs e)
        {
            this.ImageChanged("devil", 0);
        }

    }
}
