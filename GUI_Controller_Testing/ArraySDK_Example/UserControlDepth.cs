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
    public partial class UserControlDepth : UserControl
    {
        Color ColorScale = Color.LightSteelBlue;
        const int ScaleStep = 10; // i.e. one marker for each every 10mm
        const int MarkerLength = 4;
        const int MarkerStart = 25;
        const int ScaleValueOffset = 8;
        const int ScaleCmOffset = ScaleValueOffset-5;
        public UserControlDepth()
        {
            InitializeComponent();
        }

        public void BuildDrawScale(Graphics gScale, int iDepth, ScanConverter ScanConvClass, bool bIsUpDown, float fltZoomFactor, int iOffsetScale)
        {
            if (gScale == null)
                gScale = CreateGraphics();
            // Invalid value 
            if (ScanConvClass == null)
                return;
            if (ScanConvClass.MmPerPixel == 0)
                return;
            int iYpos = iOffsetScale;
            float fltMmPerPixel = ScanConvClass.MmPerPixel/fltZoomFactor;
            int iZeroOfYScale = ScanConvClass.ZeroOfYScale;
            if (fltZoomFactor != 1.0F)
                iZeroOfYScale = 0;
            int iStartY = iZeroOfYScale + iOffsetScale;
            int iYEnd = this.Size.Height + iOffsetScale;

            // Step in pixels
            float  fltStepMarker = (float)(ScaleStep / fltMmPerPixel);

            // Draw the markers of the scale every ScaleStep
            Pen pen = new Pen(Color.White, 3.0F);
            // Create font and brush.
            Font drawFont = new Font("Lato", 10);
            drawFont = new Font(drawFont, FontStyle.Bold);
            SolidBrush drawBrush = new SolidBrush(Color.White);
            // Color background = new Color();
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            gScale.Clear(Color.Transparent);
            
            int iDir = 1; 
            if (bIsUpDown)
            {
                iStartY = iYEnd - 1 - iStartY;
                iDir = -1;
            }

            int i = 0;
            // Scale is drawn in two part : upper and low part from zero
            for (int j = 0; j < 2; j++)
            {
                do
                {
                    if (iDir > 0)
                    {
                        if (iStartY >= iYEnd)
                            break;
                    }
                    else
                    {
                        if (iStartY < iYpos)
                            break;
                    }

                    gScale.DrawLine(pen, MarkerStart, iStartY, MarkerLength + MarkerStart, iStartY);
                    gScale.DrawString(i.ToString(), drawFont, drawBrush, 0, iStartY - ScaleValueOffset);
                    if (i == 0)
                        gScale.DrawString("cm", drawFont, drawBrush, 0, iStartY + ScaleCmOffset);

                    iStartY += (int)(iDir * fltStepMarker);
                    i++;
                } while (true);

                // change dir and restart at Y0 +/- step to avoid redraw zero 
                iDir = -iDir;
                iStartY = iZeroOfYScale;
                if (bIsUpDown)
                    iStartY = iYEnd - 1 - iStartY;
                iStartY += (int)(iDir * fltStepMarker);
                i = 1;
            }
            pen.Dispose();
            
        }

        private void UserControlDepth_Load(object sender, EventArgs e)
        {

        }
    }
}
