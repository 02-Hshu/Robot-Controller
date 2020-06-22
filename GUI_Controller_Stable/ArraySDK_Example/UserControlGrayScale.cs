using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using IntersonArray.Controls;
using IntersonArray.Imaging;

namespace SDK_Example
{
    public partial class UserControlGrayScale : UserControl
    {
        public UserControlGrayScale()
        {
            InitializeComponent();
        }


        public void DrawGrayScale()
        {
            Graphics g = this.CreateGraphics();
            Pen pen;
            int k=0;

            for (int j = 0; j < ImageProcessing.Color8Bits.Length; j++, k += 2)
            {
                for (int i = 0; i < this.Width; i++)
                {
                    pen = new Pen((Color)ImageProcessing.Color8Bits[j], 2.0F);
                    g.DrawLine(pen, 0, k, this.Width - 1, k);
                }
            }
        }
    }
}
