using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SDK_Example
{
    /// <summary>
    /// Represents a message form. Displays message defined by MessageClass
    /// </summary>
    public partial class FormMessage : Form
    {
        public FormMessage()
        {
            InitializeComponent();
        }

        public void ShowMessage(object stMessage)
        {
            structMessage stMess = (structMessage) stMessage;
            labelMessage.Text = stMess.strMessage;

            this.buttonCancel.Visible =  stMess.bCancel;
            this.buttonOK.Visible = stMess.bOK;

            if ((stMess.bCancel == true) && (stMess.bOK == false))
                this.buttonCancel.Location = new Point(120, 160);
            else if ((stMess.bCancel == false) && (stMess.bOK == true))
                this.buttonOK.Location = new Point(120, 160);
            else
            {
                //default
            }
            this.ShowDialog();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
