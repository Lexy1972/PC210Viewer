using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;
using System.Collections.Generic;

namespace PC210Viewer
{
    public partial class Form1 : Form
    {
        //===========================================================================================================================================
        // Member variables
        //===========================================================================================================================================
        private bool _run = true;
        private VideoCapture _capture;
        private Mat _image;
        private Thread _cameraThread;
        private bool _fps = false;
        private bool _stop = false;
        List<CameraDevicesEnumerator.CameraDevice> _cameras;


        //===========================================================================================================================================
        // Form1
        //===========================================================================================================================================
        public Form1()
        {
            InitializeComponent();
        }


        //===========================================================================================================================================
        // Form1_FormClosing
        //===========================================================================================================================================
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _stop = true;
            _run = false;
            _cameraThread.Interrupt();

        }


        //===========================================================================================================================================
        // EnumCameras
        //  Returns index of the camera named "IR camera" or -1
        //===========================================================================================================================================
        private int EnumCameras()
        {
            int index = -1;

            lbCameras.Items.Clear();
            _cameras = CameraDevicesEnumerator.GetAllConnectedCameras();

            Log("Connected camera's:");
            foreach (var camera in _cameras)
            {
                lbCameras.Items.Add(camera.Name);
                Log("  [" + camera.OpenCvId + "]:" + camera.Name);
                if (camera.Name.Contains("ir camera", StringComparison.OrdinalIgnoreCase))
                {
                    index = camera.OpenCvId;
                }
            }

            return index;
        }


        //===========================================================================================================================================
        // Form1_Load
        //===========================================================================================================================================
        private void Form1_Load(object sender, EventArgs e)
        {
            int index = EnumCameras();

            if ( index<0)
            {
                Log("No IR camera found!!, connecting to index 0");
                index = 0;
            }
            else
            {
                Log("IR camera found on index:" + index);
            }
            _capture = new VideoCapture(index);

            _image = new Mat();
            _cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));
            _cameraThread.Start();
        }


        //===========================================================================================================================================
        // CaptureCameraCallback
        //===========================================================================================================================================
        private void CaptureCameraCallback()
        {
            while (!_stop)
            {
                if (!_run) continue;
                var startTime = DateTime.Now;

                _capture.Read(_image);
                if (_image.Empty()) return;
                var imageRes = new Mat();
                Cv2.Resize(_image, imageRes, new Size(_capture.FrameWidth, _capture.FrameHeight));
                var newImage = imageRes.Clone();
                if (cbFlip.Checked)
                {
                    Cv2.Flip(imageRes, newImage, FlipMode.X);

                }

                if (_fps)
                {
                    var diff = DateTime.Now - startTime;
                    var fpsInfo = $"FPS: Nan";
                    if (diff.Milliseconds > 0)
                    {
                        var fpsVal = 1.0 / diff.Milliseconds * 1000;
                        fpsInfo = $"FPS: {fpsVal:00}";
                    }
                    Cv2.PutText(newImage, fpsInfo, new Point(100, 300), HersheyFonts.HersheyComplexSmall, 1, Scalar.White);
                }

                var bmpEffect = BitmapConverter.ToBitmap(newImage);

                pictureBoxEffect.Image = bmpEffect;
            }

            _capture.Release();

        }


        //===========================================================================================================================================
        // buttonFPS_Click_1
        //===========================================================================================================================================
        private void buttonFPS_Click_1(object sender, EventArgs e)
        {
            _fps = !_fps;
        }


        //===========================================================================================================================================
        // radioButton1_CheckedChanged
        //===========================================================================================================================================
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbZoomed.Checked)
            {
                pictureBoxEffect.SizeMode = PictureBoxSizeMode.Zoom;
            }
            else if (rdbNormalZoom.Checked)
            {
                pictureBoxEffect.SizeMode = PictureBoxSizeMode.CenterImage;
            }
        }


        //===========================================================================================================================================
        // Log
        //===========================================================================================================================================
        public void Log(String msg)
        {
            txtLog.AppendText(msg + "\r\n");
        }


        //===========================================================================================================================================
        // btnCopy_Click
        //===========================================================================================================================================
        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetImage(pictureBoxEffect.Image);
                Log("Image is copied to clipboard!");
            }
            catch (Exception ex)
            {
                Log("Clipboard copy failed: " + ex.Message);
            }

        }


        //===========================================================================================================================================
        // btnRefresh_Click
        //===========================================================================================================================================
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            EnumCameras();
        }


        //===========================================================================================================================================
        // btnConnect_Click
        //===========================================================================================================================================
        private void btnConnect_Click(object sender, EventArgs e)
        {

            int index = lbCameras.SelectedIndex;
            if ( index >=0)
            {
                Log("Releasing current camera");
                _run = false;
                Thread.Sleep(500);
                _capture.Release();

                _capture = new VideoCapture(index);


                _run = true;

                Log("Connected to '" + lbCameras.SelectedItem.ToString() + "' index: " + index);
                Log("  W:"+ _capture.FrameWidth + " H:" + _capture.FrameHeight);
            }
            else
            {
                Log("No camera selected!");
            }
        }

    }
}
