namespace RoomAliveToolkit
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tracking_face = new System.Windows.Forms.PictureBox();
            this.tracking_face2 = new System.Windows.Forms.PictureBox();
            this.videoPanel1 = new RoomAliveToolkit.VideoPanel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tracking_face)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tracking_face2)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.Color.Black;
            this.pictureBox2.ImageLocation = "H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\a" +
    "ngelClock.png";
            this.pictureBox2.InitialImage = null;
            this.pictureBox2.Location = new System.Drawing.Point(765, 671);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(533, 609);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 2;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Visible = false;
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackColor = System.Drawing.Color.Black;
            this.pictureBox3.ImageLocation = "H:\\Documents\\RoomAliveToolkit\\ProCamCalibration\\ProjectionMappingSample\\Content\\d" +
    "evilClock.png";
            this.pictureBox3.Location = new System.Drawing.Point(0, 701);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(706, 710);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 3;
            this.pictureBox3.TabStop = false;
            this.pictureBox3.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Black;
            this.label1.Font = new System.Drawing.Font("Trebuchet MS", 48F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label1.Location = new System.Drawing.Point(262, 701);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(195, 81);
            this.label1.TabIndex = 4;
            this.label1.Text = "00:00";
            this.label1.Visible = false;
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Black;
            this.label2.Font = new System.Drawing.Font("Trebuchet MS", 48F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label2.Location = new System.Drawing.Point(991, 630);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(195, 81);
            this.label2.TabIndex = 5;
            this.label2.Text = "00:00";
            this.label2.Visible = false;
            // 
            // tracking_face
            // 
            this.tracking_face.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tracking_face.Image = ((System.Drawing.Image)(resources.GetObject("tracking_face.Image")));
            this.tracking_face.InitialImage = ((System.Drawing.Image)(resources.GetObject("tracking_face.InitialImage")));
            this.tracking_face.Location = new System.Drawing.Point(341, 308);
            this.tracking_face.Name = "tracking_face";
            this.tracking_face.Size = new System.Drawing.Size(100, 102);
            this.tracking_face.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.tracking_face.TabIndex = 7;
            this.tracking_face.TabStop = false;
            this.tracking_face.Visible = false;
            // 
            // tracking_face2
            // 
            this.tracking_face2.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tracking_face2.Image = ((System.Drawing.Image)(resources.GetObject("tracking_face2.Image")));
            this.tracking_face2.InitialImage = ((System.Drawing.Image)(resources.GetObject("tracking_face2.InitialImage")));
            this.tracking_face2.Location = new System.Drawing.Point(276, 341);
            this.tracking_face2.Name = "tracking_face2";
            this.tracking_face2.Size = new System.Drawing.Size(100, 102);
            this.tracking_face2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.tracking_face2.TabIndex = 8;
            this.tracking_face2.TabStop = false;
            this.tracking_face2.Visible = false;
            // 
            // videoPanel1
            // 
            this.videoPanel1.BackColor = System.Drawing.Color.Transparent;
            this.videoPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.videoPanel1.ForeColor = System.Drawing.Color.Transparent;
            this.videoPanel1.Location = new System.Drawing.Point(0, 0);
            this.videoPanel1.Name = "videoPanel1";
            this.videoPanel1.Size = new System.Drawing.Size(1384, 1011);
            this.videoPanel1.TabIndex = 0;
            this.videoPanel1.SizeChanged += new System.EventHandler(this.videoPanel1_SizeChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.ClientSize = new System.Drawing.Size(1384, 1011);
            this.Controls.Add(this.tracking_face2);
            this.Controls.Add(this.tracking_face);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox3);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.videoPanel1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tracking_face)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tracking_face2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public VideoPanel videoPanel1;
        public System.Windows.Forms.PictureBox pictureBox3;
        public System.Windows.Forms.PictureBox pictureBox2;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.PictureBox tracking_face;
        public System.Windows.Forms.PictureBox tracking_face2;

    }
}

