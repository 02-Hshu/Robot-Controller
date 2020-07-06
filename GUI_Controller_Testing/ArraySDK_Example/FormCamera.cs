using DarrenLee.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArraySDK_Example
{
    public partial class FormCamera : Form
    {
        int count = 0;
        Camera myCamera = new Camera();

        public FormCamera()
        {
            InitializeComponent();

            GetInfo();
            myCamera.OnFrameArrived += MyCamera_OnFrameArrived;
        }

        private void GetInfo()
        {
            var cameraDevices = myCamera.GetCameraSources();
            var cameraResolutions = myCamera.GetSupportedResolutions();

            foreach (var d in cameraDevices)
            {
                cmbCameraDevices.Items.Add(d);
            }

            foreach (var r in cameraResolutions)
            {
                cmbCameraResolutions.Items.Add(r);
            }

            cmbCameraResolutions.SelectedIndex = 0;
            cmbCameraDevices.SelectedIndex = 0;
        }

        private void MyCamera_OnFrameArrived(object source, FrameArrivedEventArgs e)
        {
            Image img = e.GetFrame();
            picCamera.Image = img;
        }

        private void CmbCameraDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            myCamera.ChangeCamera(cmbCameraDevices.SelectedIndex);
        }

        private void CmbCameraResolutions_SelectedIndexChanged(object sender, EventArgs e)
        {
            myCamera.Start(cmbCameraResolutions.SelectedIndex);
        }

        private void FormCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            myCamera.Stop();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            String filename = Application.StartupPath + @"\" + "Image" + count.ToString();
            myCamera.Capture(filename);
            count++;
        }
    }
}
