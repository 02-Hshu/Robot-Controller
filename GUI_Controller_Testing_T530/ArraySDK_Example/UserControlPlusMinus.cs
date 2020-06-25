using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SDK_Example
{
    public partial class UserControlPlusMinus : UserControl
    {
        public delegate void Action();
        private Action actionPlus;
        private Action actionMinus;
        bool bInitDone = false;

        public UserControlPlusMinus()
        {
            InitializeComponent();
        }

        public void Init(string strTextLabel, Action ActPlus, Action ActMinus)
        {
            labelName.Text = strTextLabel;
            actionMinus = ActMinus;
            actionPlus = ActPlus;
            bInitDone = true;
        }

        /*
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }
         */

        private void ProtoButPlus_Click(object sender, EventArgs e)
        {
            if (actionPlus == null)
                return;
            if (bInitDone)
                actionPlus();
        }

        private void ProtoButMinus_Click(object sender, EventArgs e)
        {
            if (actionMinus == null)
                return;
            if (bInitDone)
                actionMinus();
        }

        private void ProtoButPlus_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/UserControl/uctrlPlus.png");
        }

        private void ProtoButPlus_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/UserControl/uctrlPlus.png");
        }

        private void ProtoButMinus_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/UserControl/uctrlMinus.png");
        }

        private void ProtoButMinus_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/UserControl/uctrlMinus.png");
        }

    }
}
