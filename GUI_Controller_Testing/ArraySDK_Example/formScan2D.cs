using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Management;

using IntersonTools.Graphic;
using IntersonArray;
using IntersonArray.Controls;
using IntersonArray.Imaging;
using ArraySDK_Example.Properties;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using ArraySDK_Example;

namespace SDK_Example
{
    /// <summary>
    /// represents the Main Form to display the US image.
    /// </summary>
    public partial class formScan2D : Form
    {
        #region ATTRIBUTES
        /// <summary>
        /// TO BE DONE
        /// Image extension Should be adapted to the new array probe since it's very dark as is.
        /// </summary>
        /// UScanGuide

        Boolean bImgExt = false; // Use ImageExtension if true

        const int INTENSITY_MIN = 0;
        const int INTENSITY_MAX = 256;
        const int CONTRAST_MIN = 0;
        const int CONTRAST_MAX = 512;
        //ImageControl imgControl; 
        int sbytBrightnessValue = 127;
        int sbytContrastValue = 255;


        string strVersion = Version + " - " + IntersonArray.IntersonClass.Version + " - " + IntersonTools.IntersonTools.Version;
        const float fltRFVersion = 2.05F;

        public static formScan2D MyFormScan2D = null;


        /// <summary>
        /// bytRawImage will be passed by reference to the Dll to receive the US image
        /// </summary>
        Bitmap bmpUSImage;

        Bitmap bmpOut;
        uint uiNbOfLines = 0;
        int iCfmSamples = 0;

   

        /// <summary>
        /// pass by reference to the dll
        /// </summary>
        Byte[,] bytRawImageRef;

        /// <summary>
        /// Current data
        /// </summary>
        Byte[,] bytRawImage;

        /// <summary>
        /// to keep track of the previous data, to use it when needs
        /// </summary>
        Byte[,] bytRawImagePrevious;

        /// <summary>
        /// RF Current Data
        /// </summary>
        ushort[,] aushRawRF;

        /// <summary>
        /// RF Curve
        /// </summary>
        Point[] aptRFdata = new Point[ScanConverter.MAX_RFSAMPLES];
        const int RF_SWING = 65535; //16 bits (ushort)
        int iRFNumLine = 64;//line to be displayed
        int ifactorRF = 1;

        //---- Copy of RFData when "capture" or saved RFData when "idle"
        bool bSoftRFData = false;
        //---- Copy of CFMData when "capture" or saved CFMData when "idle"
        bool bSoftCFMData = false;

        int iCFMNumLine = 0;//line to be displayed
        int ifactorCFM = 1;

        bool bGetRawCfm = false;//Set to true to display the Raw RF CFM data

        /// <summary>
        /// CFM Current Data
        /// </summary>
        byte[] abytRawCFM;

        /// <summary>
        /// CFM: RF raw data
        /// </summary>
        ushort[] aushRawCFM;


        /// <summary>
        /// Cineloop
        /// </summary>
        /// 

        // UScanGuide - Christine changed maxCine from 40 to be big enough for a full 360 robot scan
        // This MUST be the same as the TrackBarCine Maximum in Properties

        int iCineCounter = 0; /// current index of the loop
        int MaxCine = 512;     /// Number of Images saved in the loop
        bool bCineOn = false; /// Play/Stop
        List<Byte[,]> ByteArrayList = new List<Byte[,]>(); /// Container of the Cineloop

        List<ushort[,]> ushortArrayList = new List<ushort[,]>();/// Container of the cineloop for RF Data
        List<Byte[]> ByteUniArrayList = new List<Byte[]>();/// Container of the cineloop for CFM Data
        List<ushort[]> ushortUniArrayList = new List<ushort[]>();/// Container of the cineloop for CFM Data (RF)


        Thread trdCineShow; /// Thread when play the cineloop
        Stopwatch stopWatch = new Stopwatch(); /// Timer for the FrameRate

        /// probes connected
        public string strNotConnected = "Not Connected";

        /// intermediate Bitmap used to Save/Load to file
        Bitmap bmpLoad;
        /// Probe Id of the saved/loaded image 
        short shIdleId;

        /// Flag to indentify Idle mode
        bool bIdle = false;

        /// "Idle" Scan Converter 
        int iIdleWidth;
        int iIdleHeight;
        int iIdleIndexSC;
        int iIdleDepth;
        uint uiIdleNbOfLinesPerArray;
        bool bIdleDoubler;
        bool bIdleCompound;
        int iIdleCompoundAngle;
        int iIdleSteering;
        int iIdleDepthCfmBox = 0;

        /// <summary>
        ///  Define the array of Depths for each probe. Array is not mandatory. 
        /// </summary>
        int iMaxDepthIndex = 0;
        int iDepthIndex = 0;
        int iDepth = 0;
        int[] aiDepth1 = { 40, 80, 120, 160, 200 };
        int[] aiDepth2 = { 20, 40, 60, 80, 100 };
        int[] aiDepth3 = { 15, 20, 25, 30, 35, 40 };
        int[] aiDepth4 = { 20, 40, 60, 80, 100, 130, 160 };
        int[] aiDepth5 = { 10, 20, 30, 40 };
        int[] aiDepth;
        int iOffsetScale = 0;

        /// <summary>
        /// Define the array of Frequencies - mandatory
        /// </summary>
        int[] aiFreq;
        byte bytFreqIndex = 1;

        /// <summary>
        /// Define the array of Focus - mandatory
        /// </summary>
        int[] aiFocus;
        byte bytFocusIndex = 1;

        //---- Steering 
        int iSteering = 0;

        /// <summary>
        /// Define the array of HighVoltage, values are a percentage of the highest voltage. Array is not mandatory.
        /// </summary>
        byte[] abytHighVoltage = { 20, 30, 40, 50 };
        byte[] abytHighVoltageCFM = { 10, 15, 20, 25 };

        int iHighIndex = 0;

        //---- PRF
        int[] aiPRF;
        byte bytPrfIndex = 1;
        //---- box
        int iDepthCfmBox = 0;

        //---- Cfm gain
        byte bytCfmGain;


        /// <summary>
        ///  Software TGC 
        /// </summary>
        const int DIG_GAIN_MIN = -127;
        const int DIG_GAIN_INIT = 0;
        const int DIG_GAIN_MAX = 127;

        const int TGC_LOGDYNAMIC = 55;  // Hardware setting
        const int TGC_DYNAMIC = 15;// Maximum value is TGC_LOGDYNAMIC/2
        const int GRAYSCALEMAX = 255;    // Maximum of the Gray  Scale

        const float fltTgcScaling = ((float)(TGC_DYNAMIC) / TGC_LOGDYNAMIC) * ((float)(GRAYSCALEMAX) / DIG_GAIN_MAX);

        const int TGC_SEG_NB = 3;
        float[] afltTgcInc = new float[TGC_SEG_NB];
        int[] aiTgcSeg = { 340, 340, 344 };// length of each segment (i.e. number of samples for each segment) !!!! aiTgcSeg[0] + aiTgcSeg[1] + aiTgcSeg[2] = 1024 = MAX_SAMPLES !!!!
        float[] afltTgcCurve = new float[ScanConverter.MAX_SAMPLES];
        sbyte[] asbytTgcCurve = new sbyte[ScanConverter.MAX_SAMPLES];


        sbyte sbytMainGainValue = DIG_GAIN_INIT;
        sbyte[] asbytTgcValue = { DIG_GAIN_INIT, DIG_GAIN_INIT, DIG_GAIN_INIT };

        byte bytTgcStep = 8;

        /// <summary>
        /// Dynamic and Anti-Log
        /// </summary>
        int iDynamicIndex = 7;
        byte[] abytDynamicValue = { 30, 35, 40, 45, 50, 55, 60, 65 };

        // labels
        string strMM = " mm";
        string strHz = " Hz";
        string strVolt = " V";
        string strdB = " dB";

        string strDepth = "";
        string strFrequency = "";
        string strFocus = "";
        string strHighVolt = "";
        string strZoom = "";
        string strMainGain = "Brightness:  ";
        string strCfmGain = "CFM Gain:  ";
        string strDynamic = "Contrast:  ";
        string strImagesPer = "Images per TBD:  ";
        string strFR = "FR:  ";
        string strSteering = "Steering:  ";
        string strCompound = "Compound:  ";
        string strPRF = "PRF: ";

        // store the date & time image is returned from probe
        // this to get image timestamp to use for image file labeling 
        // and the time difference between image returns in milliseconds
        // store local time
        DateTime curImageTime;
        DateTime[] cineImageTimes;
        //to do: change image labelling system to degree based

        // store also the step count when the image was taken
        int curStepPos;
        int prevStepPos = 0;
        int[] cineStepCount;
        //for the homing function, counting maximum steps
        bool homeCountSteps = false;
        int maxSteps = 0;

        List<Byte> responseCodes = new List<Byte>(1024);

        byte[] robotData = new byte[1];

        // reset to 0 in ready to scan state
        int posStepCount = 0;
        int negStepCount = 0;

        //Rita- boolean for whether or not to take pic (every x steps)
        //todo: make this feature functional
        bool takePicture;
        int stepInterval;

        /// <summary>
        /// this for communicating with SerialPort
        /// </summary>
        SerialPort comPort = null;

        // this for receiving data from the Serial Port
        byte[] ReceivedBytes = new Byte[1];


        enum RobotStateEnum
        {
            disConnected,
            unInitialized,
            homing,
            readyToScan, // green
            scanning, // blue
            endOfTravel, // amber, need to reverse
            emergencyStopped, // red
            rewinding // gray
        };

        RobotStateEnum RobotState = RobotStateEnum.disConnected;


        enum ControlEnum
        {
            buttonScan,
            buttonCine,
            TrackBar,
            //three needed for robot
            TrackBarCounter,
            buttonProbe1,
            buttonProbe2,
            Probe1Enabled,
            Probe2Enabled,
            RFModeEnabled,
            CfmModeEnabled,
            UpdateTrackBar
        };
        delegate void MarshalToForm(ControlEnum eCtrl, String textToAdd);

        /// <summary>
        /// Define the choices of the sizes of the Scan Converter
        /// </summary>
        string[] astrSC = { "Image 512 * 512", "Image 640 * 480", "Image 768 * 768", "Image 800 * 512", "Image 1000 * 800" };
        int[] aiWidth = { 512, 640, 768, 800, 1000 };
        int[] aiHeigth = { 512, 480, 768, 512, 800 };
        int iIndexSC = 1;


        /// <summary>
        /// Define the 2 classes for the ScanConverter: Scan2DClass and ScanConverter
        /// </summary>
        bool bInitDone = false;
        IntersonArray.Imaging.Capture Scan2D;
        ImageBuilding ImageBuilding;

        public ScanConverter ScanConv;
        bool bIsUpDown = false;
        bool bIsLeftRight = false;



        /// <summary>
        /// get an Instance of HWControls
        /// </summary>
        HWControls MyHwControls;

        /// <summary>
        /// define the Class for the zoom. ZoomBoxClass is included in the ToolsLibrary
        /// </summary>
        ZoomBox ZoomBox;
        int iZoomX = 0;
        int iZoomY = 0;
        public const float fltZoomInc = 0.5F;
        float fltZoomFactor = ZoomBox.ZOOM_MIN;

        /// <summary>
        ///  Color
        /// </summary>
        Color colBox = Color.LightSteelBlue;
        Color colEnabled = Color.OrangeRed;
        Color colDisabled = Color.LightSteelBlue;

        //---- Selected probe according to the 2 buttons so 0 or 1
        static public int iSelectedProbe = 0;

        // Checks if the textRadius textbox has been clicked
        bool hasBeenClicked = false;
        #endregion

        #region FORMSCAN
        public formScan2D()
        {
            InitializeComponent();
            toolStripStatusLabel.Text = "CFM....................... " + strVersion;
            for (int i = 0; i < astrSC.Length; i++)
            {
                // cboxSCSize.Items.Add(astrSC[i]);
            }
            // double-buffered
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        /// <summary>
        /// To load the main form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void formScan2D_Load(object sender, EventArgs e)
        {
            // Sets the focus on labelTgc so that textRadius isn't focused
            this.ActiveControl = labelTgc;

            DeviceNotificationsStart();//start notification NEW SDK 2.08

            StringCollection mycolProbesName = new StringCollection();

            try
            {
                HWControls controls = new HWControls();
                controls.FindAllProbes(ref mycolProbesName);
                controls.FindMyProbe(0);

                SetSelectedProbe();

                // Starts scanning immediately after probe has been selected
                StartScan();

                // Sets Doulber to on immediately after scanner starts scanning
                if ((ScanConv.Compound) == true)
                    return;
                ScanConverter.Doubler = !ScanConverter.Doubler;
                SetDoubler();
                RebuildAll();
            } catch (ArgumentOutOfRangeException error) {
            }

        }


        /// <summary>
        /// File Version
        /// </summary>
        static public string Version
        {
            get
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location);
                return asm.ManifestModule.Name + String.Format(" : {0}", fvi.FileVersion);
            }
        }

        /// <summary>
        /// File Version
        /// </summary>
        static public float ShortVersion
        {
            get
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location);
                return (float)(fvi.FileMinorPart + fvi.FileMajorPart * 100) / 100;
            }
        }

        /// <summary>
        /// Enables idle mode
        /// </summary>
        void EnableIdle()
        {
            /*
            gBoxImaging.Enabled = false;
            gboxGain.Enabled = false;
            gboxImage.Enabled = false;
            gBoxCineloop.Enabled = false;
            gBoxTgc.Enabled = false;
            */
            bIdle = true;
        }

        /// <summary>
        /// Disables idle mode
        /// </summary>
        void DisableIdle()
        {
            /*
            gBoxImaging.Enabled = true;
            gboxGain.Enabled = true;
            gboxImage.Enabled = true;
            gBoxCineloop.Enabled = true;
            gBoxTgc.Enabled = true;
            */
            bIdle = false;
        }

        /// <summary>
        /// Sets up the probe that has been connected to the computer. 
        /// </summary>
        void SetSelectedProbe()
        {
            // buttonProbe1.Enabled = buttonProbe2.Enabled = butRFMode.Enabled = true;

            /// Initialize Capture, ImageBuilding and ScanConverter classes
            ScanConv = new ScanConverter();
            Scan2D = new IntersonArray.Imaging.Capture();
            ImageBuilding = new ImageBuilding();

            /// NewImageTick Event handler; event raised when image ready to be displayed

            Scan2D.NewImageTick += new IntersonArray.Imaging.Capture.NewImageHandler(ImageRefresh);
            // Our own custom timer instead of Interson's timer
            // System.Windows.Forms.Timer timerImage = null;
            // timerImage = new System.Windows.Forms.Timer();
            // timerImage.Tick += new EventHandler(ImageRefresh);
            // timerImage.Interval = 1;
            // timerImage.Enabled = true;
            // timerImage.Start();

            Scan2D.FrameAvg = true; //Enable/Disable Frame Averaging. Default is true.

            ///Robot initialization
            RobotState = RobotStateEnum.disConnected;
            SetButtonForRobotState(RobotState);
            EnableRobotConnectTimer();

            /// HWControls
            MyHwControls = new HWControls();

            MyHwControls.EnableHardButton();
            MyHwControls.HWButtonTick += new HWControls.HWButtonHandler(WatchHWButton); //Hardware Button was pressed

            String version = HWControls.ReadFPGAVersion();
            toolStripStatusLabelFpga.Text = "- FPGA Version: " + version.ToString();

            bSoftRFData = Scan2D.RFData;

            bSoftCFMData = Scan2D.CFMData;

            /// Zoom
            ZoomBox = new ZoomBox(uctrlScan, colBox);

            //Initialize all the arrays according Number of lines
            uiNbOfLines = MyHwControls.GetLinesPerArray();
            //imgControl = new ImageControl(((int)uiNbOfLines), ScanConverter.MAX_SAMPLES);

            //Initialize all the arrays according Number of Samples
            iCfmSamples = MyHwControls.GetMaxCfmSamples();


            bytRawImageRef = new byte[uiNbOfLines, ScanConverter.MAX_SAMPLES];
            bytRawImage = new byte[uiNbOfLines, ScanConverter.MAX_SAMPLES];
            bytRawImagePrevious = new byte[uiNbOfLines, ScanConverter.MAX_SAMPLES];
            aushRawRF = new ushort[uiNbOfLines, ScanConverter.MAX_RFSAMPLES];
            abytRawCFM = new byte[ScanConverter.MAX_CFMLINE * iCfmSamples];
            if (bGetRawCfm == true)
                aushRawCFM = new ushort[ScanConverter.MAX_CFM_LINES * ScanConverter.MAX_RFSAMPLES];


            /// Cineloop
            ByteArrayList.Clear();
            ByteArrayList.Capacity = MaxCine;
            ushortArrayList.Clear();
            ushortArrayList.Capacity = MaxCine;
            ByteUniArrayList.Clear();
            ByteUniArrayList.Capacity = MaxCine;
            ushortUniArrayList.Clear();
            ushortUniArrayList.Capacity = MaxCine;
            //for UScanGuide Labeling? 
            cineImageTimes = new DateTime[MaxCine];
            cineStepCount = new int[MaxCine];
            //TODO: Make labelling related to step/position/angle

            /// Initialise the rest 
            iOffsetScale = this.uctrlScan.Location.Y - this.uctrlDepth.Location.Y;
            InitControlPlusMinus();
            // cboxSCSize.Text = astrSC[iIndexSC];

            if (ScanConv != null)
            {

                /// Initialize Depth Array according to the probe selected
                short shId = HWControls.GetProbeID();



                if (shId == HWControls.ID_A_7_5MHz)
                {
                    aiDepth = aiDepth2;
                    bytCfmGain = HWControls.CFMGAIN_DEFAULTLIN;
                }
                else if (shId == HWControls.ID_CA_5_0MHz)
                {
                    aiDepth = aiDepth1;
                    bytCfmGain = HWControls.CFMGAIN_DEFAULTCVX;
                }
                else if ((shId == HWControls.ID_CA_OP_10MHz) || (shId == HWControls.ID_A_OP_10MHz) || (shId == HWControls.ID_A_OP_12MHz))
                {
                    aiDepth = aiDepth3;
                    bytCfmGain = HWControls.CFMGAIN_DEFAULTCVX;
                }
                else
                {
                    aiDepth = aiDepth1;
                    bytCfmGain = HWControls.CFMGAIN_DEFAULTCVX;
                }


                iMaxDepthIndex = aiDepth.Length;
                for (int i = 0; i < iMaxDepthIndex; i++)
                {
                    /// Check if the Depth is valid for that probe, i.e. if too deep, return MaxDepth 
                    aiDepth[i] = MyHwControls.ValidDepth(aiDepth[i]);
                }

                iDepthIndex = 2;
                iDepth = aiDepth[iDepthIndex];

                ///Get the array of frequencies and focus
                aiFreq = new int[HWControls.MAX_FREQ];
                aiFreq = MyHwControls.GetFrequency();
                bytFreqIndex = 1;

                aiFocus = new int[HWControls.MAX_FOCUS];
                aiFocus = MyHwControls.GetFocus(); ;
                bytFocusIndex = 1;

                aiPRF = MyHwControls.GetPRF();
                bytPrfIndex = 1;

                ///Set High Voltage index
                iHighIndex = abytHighVoltage.Length - 1;

                getLastSettings();

                UpdateLabels();
                ShowDisplayControls();
                TgcCurve();
                bInitDone = false;
            }
        }

        /// <summary>
        /// To enable or disable Cineloop buttons
        /// </summary>
        /// <param name="enable"></param>
        void UpdateCineloopGroup(bool enable)
        {
            if (enable == true)
            {
                buttonCine.Enabled = true;
                buttonPrevious.Enabled = true;
                buttonNext.Enabled = true;
            }
            else
            {
                buttonCine.Enabled = false;
                buttonPrevious.Enabled = false;
                buttonNext.Enabled = false;

            }
        }

        /// <summary>
        /// Updates all the buttons.
        /// </summary>
        void UpdateButtons()
        {
            /*
            if (bSoftRFData)
                // butRFMode.BackColor = Color.OrangeRed;
            else
                // butRFMode.BackColor = Color.LightSteelBlue;
            */
            if (bSoftCFMData == true)
            {
                butCfmMode.BackColor = Color.OrangeRed;
                protoUCtrlPMFrequency.Init("PRF", FreqPlus, FreqMinus);
            }
            else
            {
                butCfmMode.BackColor = Color.LightSteelBlue;
                protoUCtrlPMFrequency.Init("Freq", FreqPlus, FreqMinus);
            }
        }

        /// <summary>
        /// To update all the labels
        /// </summary>
        private void UpdateLabels()
        {
            labelDepth.Text = strDepth + iDepth.ToString() + strMM;
            if (aiFreq != null)
            {
                if (bSoftCFMData)
                {
                    labelFrequency.Text = strPRF + aiPRF[bytPrfIndex].ToString() + strHz;
                }
                else
                {
                    double kiloHertz = Convert.ToDouble(aiFreq[bytFreqIndex]) / 1000;
                    labelFrequency.Text = strFrequency + kiloHertz.ToString() + " kHz";
                    // labelFrequency.Text = strFrequency + aiFreq[bytFreqIndex].ToString() + strHz;
                }
            }
            if (aiFocus != null)
                labelFocus.Text = strFocus + aiFocus[bytFocusIndex].ToString() + strMM;
            labelHighVolt.Text = strHighVolt + abytHighVoltage[iHighIndex].ToString() + strVolt;

            strMainGain = "";
            strCfmGain = "CFM Gain:  ";
            strDynamic = "";

            double dblMainGain = sbytMainGainValue / 6.4;//---- +128 -> 20dB => 6.4 -> 1dB
            if (bSoftCFMData) {
                labelMainGain.Text = strCfmGain + bytCfmGain.ToString();
            } else {
                labelMainGain.Text = strMainGain + dblMainGain.ToString("0.00") + strdB;
            }
            labelDynamic.Text = strDynamic + abytDynamicValue[iDynamicIndex].ToString() + strdB;


            // labelZoom.Text = strZoom + fltZoomFactor.ToString("0.00");
            if (MyHwControls != null)
            {
                // labelFR.Text = strFR + MyHwControls.GetProbeFrameRate().ToString();
                // if (bIdle == false) //---- cannot get the property if there is no probe
                    // labelCompound.Text = strCompound + MyHwControls.CompoundAngle.ToString();
            }

            // labelSteering.Text = strSteering + iSteering.ToString();
            //UScanGuide: need to update imagesper and position
        }

        /// <summary>
        /// To reset all the labels, no value displayed, just the title
        /// </summary>
        private void ResetLabels()
        {
            labelDepth.Text = strDepth;
            labelFrequency.Text = strFrequency;
            labelFocus.Text = strFocus;
            labelHighVolt.Text = strHighVolt;
            // labelZoom.Text = strZoom;
            labelMainGain.Text = strMainGain;
            labelDynamic.Text = strDynamic;
            // labelFR.Text = strFR;
            // labelSteering.Text = strSteering;
            // labelCompound.Text = strCompound;
            // labelImagesPer.Text = strImagesPer;

        }

        /// <summary>
        /// Updates the Track bars
        /// </summary>
        void UpdateTrackBar()
        {
            if (bSoftRFData)
            {
                protoTrackBarCine.Maximum = ushortArrayList.Count() - 1;
                protoTrackBarCine.Value = ushortArrayList.Count() - 1;
                iCineCounter = ushortArrayList.Count() - 1;
            }
            else if (Scan2D.CFMData)
            {

                if (bGetRawCfm == true)
                {
                    protoTrackBarCine.Maximum = ushortUniArrayList.Count() - 1;
                    protoTrackBarCine.Value = ushortUniArrayList.Count() - 1;
                    iCineCounter = ushortUniArrayList.Count() - 1;
                }
                else
                {
                    //#else
                    //#else
                    protoTrackBarCine.Maximum = ByteUniArrayList.Count() - 1;
                    protoTrackBarCine.Value = ByteUniArrayList.Count() - 1;
                    iCineCounter = ByteUniArrayList.Count() - 1;
                }
            }
            else
            {
                protoTrackBarCine.Maximum = ByteArrayList.Count() - 1;
                protoTrackBarCine.Value = ByteArrayList.Count() - 1;
                iCineCounter = ByteArrayList.Count() - 1;
            }

        }
       
        /// <summary>
        /// To initialize the PlusMinus controls
        /// </summary>
        private void InitControlPlusMinus()
        {
            //---- Imaging
            protoUCtrlPMDepth.Init("Depth", DepthPlus, DepthMinus);
            protoUCtrlPMFrequency.Init("Freq", FreqPlus, FreqMinus);
            protoUCtrlPMFocus.Init("Focus", FocusPlus, FocusMinus);
            protoUCtrlPMHighVoltage.Init("Volt", HighPlus, HighMinus);
            protoUCtrlPMGalGain.Init("Main Gain", MainGainPlus, MainGainMinus);
            protoUCtrlPMDynamic.Init("Dyn Gain", DynamicPlus, DynamicMinus);
            // uctrlPMSteering.Init("Steering", SteeringPlus, SteeringMinus);
            // uctrlImagesPer.Init("ImagesPer", ImagPlus, ImagMinus);

        }

        /// <summary>
        /// To close the main form 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void formScan2D_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisableConnect(); //UscanGuide
            StopThreadScan();

            //Uscanguide
            DisableRobotListener();
            DisableRobotConnectTimer();

            try
            {
                if ((comPort != null) && (comPort.IsOpen))
                    comPort.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            buttonRobotScan.Enabled = false;
            buttonSaveCine.Enabled = false;

            StopCineloop();
            try { 
                Scan2D.NewImageTick -= new IntersonArray.Imaging.Capture.NewImageHandler(ImageRefresh); //Image Ready to be displayed
                MyHwControls.DisableHardButton();
                MyHwControls.HWButtonTick -= new HWControls.HWButtonHandler(WatchHWButton);
            }
            catch (Exception ex) {

            }
            DeviceNotificationsStop();//NEW SDK 2.08

            Thread.Sleep(200); //wait to release ressources

            /// Stop High Voltage
            HWControls.DisableHighVoltage();

            if (Scan2D != null)
                Scan2D.StopReadScan();

        }

        #endregion

        #region SCAN
        /// <summary>
        /// To initialize a new scan, called when Scan Starts
        /// </summary>
        private void InitScan()
        {
            if (Scan2D.ScanOn == true)
                return;

            bIdle = false;

            /// Initialize bmpUSImage
            bmpUSImage = new Bitmap(aiWidth[iIndexSC], aiHeigth[iIndexSC], System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            bmpOut = new Bitmap(bmpUSImage.Width, bmpUSImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);//ici au lieu de refresh

            ///Initialize Doubler 
            SetDoubler();
            /// initialize Compound
            SetCompound();
            ///Send frequency to HW
            HWControls.SetFrequencyAndFocus(bytFreqIndex, bytFocusIndex, iSteering);
            ///Send High Voltage value to HW
            HWControls.SendHighVoltage(abytHighVoltage[iHighIndex], abytHighVoltageCFM[iHighIndex]);
            ///Enable High Voltage
            HWControls.EnableHighVoltage();
            //Send the dynamic value to HW
            HWControls.SendDynamic(abytDynamicValue[iDynamicIndex]);


            ///Calculate the scanconverter according to the characteristics of the image
            iDepth = aiDepth[iDepthIndex];

            if (bSoftCFMData == true)
                //To set the new PRF and to update the Frame Rate consequently
                HWControls.UpdateCfmPrf(iDepth, aiPRF[bytPrfIndex], uiNbOfLines);
            ScanConverter.ScanConverterError error = ScanConv.HardInitScanConverter(iDepth, aiWidth[iIndexSC], aiHeigth[iIndexSC], iSteering, iDepthCfmBox, ref Scan2D, ref ImageBuilding); /// Calculation of ScanConverter

            if ((error == IntersonArray.Imaging.ScanConverter.ScanConverterError.OVER_LIMITS) ||
                (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.UNDER_LIMITS) ||
                (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.PROBE_NOT_INITIALIZED) ||
                (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.ERROR) ||
                (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.WRONG_FORMAT) ||
                (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.UNKNOWN_PROBE)
                )
            {
                FormMessage formMessage = new FormMessage();
                formMessage.ShowMessage((object)MessageClass.ScanConverterMessage);
                formMessage.Dispose();
                bInitDone = false;
            }
            else
            {

                ///Adapt the size of the form
                ResizeForm(iIndexSC);

                //Abort the Previous Scan TODO: check if necessary
                //Scan2D.AbortScan();

                ///Draw the depth Scale
                uctrlDepth.BuildDrawScale(null, iDepth, this.ScanConv, bIsUpDown, fltZoomFactor, iOffsetScale);

                ///Done
                bInitDone = true;
            }
        }

        /// <summary>
        /// To recalculate the ScanConverter when a parameter has changed, ex: Depth, Zoom
        /// </summary>
        private void RebuildScanConverter()
        {
            bool bRestart = false;
            if (Scan2D.ScanOn == true)// Stop Scan if running
            {
                StopThreadScan();
                bRestart = true;
            }
            bmpUSImage = new Bitmap(aiWidth[iIndexSC], aiHeigth[iIndexSC], System.Drawing.Imaging.PixelFormat.Format8bppIndexed);//to erase the previous Bitmap
            bmpOut = new Bitmap(bmpUSImage.Width, bmpUSImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);//ici au lieu de refresh

            iDepth = aiDepth[iDepthIndex];
            ScanConv.HardInitScanConverter(iDepth, aiWidth[iIndexSC], aiHeigth[iIndexSC], iSteering, iDepthCfmBox, ref Scan2D, ref ImageBuilding); /// Calculation of ScanConverter
            uctrlDepth.BuildDrawScale(null, iDepth, this.ScanConv, bIsUpDown, fltZoomFactor, iOffsetScale); ///DepthScale
            UpdateLabels();
            if (bRestart == true)
                StartThreadScan();// Start Scan
        }

        /// <summary>
        /// idem RebuildScanConverter + SetFrequencyAndFocus 
        /// </summary>
        private void RebuildAll()
        {
            bool bRestart = false;
            if (Scan2D.ScanOn == true)// Stop Scan if running
            {
                StopThreadScan();
                bRestart = true;
            }
            bmpUSImage = new Bitmap(aiWidth[iIndexSC], aiHeigth[iIndexSC], System.Drawing.Imaging.PixelFormat.Format8bppIndexed);//to erase the previous Bitmap
            bmpOut = new Bitmap(bmpUSImage.Width, bmpUSImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);//ici au lieu de refresh

            iDepth = aiDepth[iDepthIndex];
            ScanConv.HardInitScanConverter(iDepth, aiWidth[iIndexSC], aiHeigth[iIndexSC], iSteering, iDepthCfmBox, ref Scan2D, ref ImageBuilding); /// Calculation of ScanConverter
            uctrlDepth.BuildDrawScale(null, iDepth, this.ScanConv, bIsUpDown, fltZoomFactor, iOffsetScale); ///DepthScale
            HWControls.SetFrequencyAndFocus(bytFreqIndex, bytFocusIndex, iSteering);
            if (bSoftCFMData == true)
                HWControls.UpdateCfmPrf(iDepth, aiPRF[bytPrfIndex], uiNbOfLines);
            UpdateLabels();
            if (bRestart == true)
                StartThreadScan();// Start Scan
        }

        /// <summary>
        /// to Start/Stop the Acquisition and scan
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonScan_Click(object sender, EventArgs e)
        {
            if (bCineOn == true)
                return;
            UpdateCineloopGroup(true);//--- when come back from "load"
            bSoftRFData = Scan2D.RFData;
            bSoftCFMData = Scan2D.CFMData;

            if (Scan2D.ScanOn == true)// Already started then stop
            {
                StopScan();
            }
            else //Start Scanning
            {
                StartScan();
            }
        }

        /// <summary>
        /// Call to start scan
        /// </summary>
        void StartScan()
        {
            if (Scan2D.ScanOn == true)
                return;

            DisableConnect(); //UScanGuide

            if (bInitDone == false)
            {
                InitScan();
                UpdateLabels();
            }

            if (bInitDone == true)
            {
                // buttonProbe1.Enabled = buttonProbe2.Enabled = butRFMode.Enabled = false;

                ShowDisplayControls();
                MyMarshalToForm(ControlEnum.buttonScan, "Freeze");
                StartThreadScan();// Start the scan
            }
        }

        /// <summary>
        /// Call to stop scan
        /// </summary>
        void StopScan()
        {
            //if (Scan2D.ScanOn == false)
            //    return;
            StopThreadScan();// Stop the scan

            MyMarshalToForm(ControlEnum.Probe1Enabled, "");
            MyMarshalToForm(ControlEnum.Probe2Enabled, "");
            MyMarshalToForm(ControlEnum.RFModeEnabled, "");
            MyMarshalToForm(ControlEnum.CfmModeEnabled, "");
            MyMarshalToForm(ControlEnum.buttonScan, "Scan"); //UscanGuide
            if (bSoftRFData)
            {
                protoTrackBarCine.Maximum = ushortArrayList.Count() - 1;
                protoTrackBarCine.Value = ushortArrayList.Count() - 1;
                iCineCounter = ushortArrayList.Count() - 1;
            }
            else
            {
                protoTrackBarCine.Maximum = ByteArrayList.Count() - 1;
                protoTrackBarCine.Value = ByteArrayList.Count() - 1;
                iCineCounter = ByteArrayList.Count() - 1;
            }
            // christine this to keep the tick frequency more or less constant
            // not that the possible size of the maximum can be very large
            // (to accomodate the large number of images in a robot scan)
            protoTrackBarCine.TickFrequency = Math.Max((protoTrackBarCine.Maximum + 39) / 40, 1);
            MyMarshalToForm(ControlEnum.UpdateTrackBar, "");

        }

        /// <summary>
        /// Start the Acquisition and scan
        /// </summary>
        private void StartThreadScan()
        {

            labelFileName.Visible = false;

            bIsBusy = false;

            if (bSoftRFData == true)
            {
                ushortArrayList.Clear();//new RF cineloop
                Scan2D.StartRFReadScan(ref aushRawRF);
                System.Threading.Thread.Sleep(100);//let's time to start
                HWControls.StartRFmode();//start acquisition
            }
            else if (Scan2D.CFMData == true)
            {
                ByteArrayList.Clear();// B cineloop
                if (bGetRawCfm == true)
                {
                    ushortUniArrayList.Clear();//CFM RF
                    Scan2D.StartCFMReadScan(ref bytRawImageRef, ref aushRawCFM);
                }
                else
                {
                    ByteUniArrayList.Clear();//CFM color
                    Scan2D.StartCFMReadScan(ref bytRawImageRef, ref abytRawCFM);
                }
                System.Threading.Thread.Sleep(100);
                HWControls.StartCFMmode();

            }
            else
            {
                ByteArrayList.Clear();// New B cineloop
                Scan2D.StartReadScan(ref bytRawImageRef);
                System.Threading.Thread.Sleep(100);//let's time to start
                HWControls.StartBmode(); //start acquisition
            }


        }

        /// <summary>
        /// to Stop the Acquisition and scan
        /// </summary>
        private void StopThreadScan()
        {
            HWControls.StopAcquisition();//stop acquisition
            bIsBusy = false;
            if (Scan2D != null)
                Scan2D.StopReadScan();// stop scan
        }

        #endregion

        #region SOFTWARE TGC AND CFM GAIN
        void UpdateCfmGain()
        {
            bool bRestart = false;
            if (Scan2D.ScanOn == true) //must stop to update the gain
            {
                StopThreadScan();
                bRestart = true;
            }

            if (bSoftCFMData == true)
                MyHwControls.UpdateCfmGain(bytCfmGain);
            UpdateLabels();
            if (bRestart == true)
                StartThreadScan();// Start Scan
        }

        private void MainGainPlus()
        {
            if (bSoftCFMData)
            {

                if (bytCfmGain >= HWControls.CFMGAIN_MAX)
                    return;

                bytCfmGain++;
                UpdateCfmGain();
            }
            else
            {

                if (sbytMainGainValue > DIG_GAIN_MAX - bytTgcStep)
                    return;
                sbytMainGainValue += (sbyte)bytTgcStep;
                TgcCurve();
            }
        }

        private void MainGainMinus()
        {
            if (bSoftCFMData)
            {
                if (bytCfmGain <= 0)
                    return;

                bytCfmGain--;
                UpdateCfmGain();
            }
            else
            {

                if (sbytMainGainValue < DIG_GAIN_MIN + bytTgcStep)
                    return;
                sbytMainGainValue -= (sbyte)bytTgcStep;
                TgcCurve();
            }
        }

        /// <summary>
        /// Defines the Time Gain Compensation.
        /// </summary>
        private void TgcCurve()
        {
            afltTgcInc[0] = ((float)(asbytTgcValue[1] - asbytTgcValue[0]) / aiTgcSeg[0]) * fltTgcScaling;
            afltTgcInc[1] = ((float)(asbytTgcValue[2] - asbytTgcValue[1]) / aiTgcSeg[1]) * fltTgcScaling;
            afltTgcInc[2] = 0;

            afltTgcCurve[0] = (sbytMainGainValue + asbytTgcValue[0]) * fltTgcScaling;
            for (int i = 1; i < ScanConverter.MAX_SAMPLES; i++)
            {
                if (i <= aiTgcSeg[0])
                    afltTgcCurve[i] = afltTgcCurve[i - 1] + afltTgcInc[0];
                else if (i <= aiTgcSeg[0] + aiTgcSeg[1])
                    afltTgcCurve[i] = afltTgcCurve[i - 1] + afltTgcInc[1];
                else
                    afltTgcCurve[i] = afltTgcCurve[i - 1] + afltTgcInc[2];
            }
            for (int i = 1; i < ScanConverter.MAX_SAMPLES; i++)
            {
                if (afltTgcCurve[i] > Math.Floor(DIG_GAIN_MAX * fltTgcScaling))
                    asbytTgcCurve[i] = (sbyte)Math.Floor(DIG_GAIN_MAX * fltTgcScaling);
                else if (afltTgcCurve[i] < -Math.Floor(DIG_GAIN_MAX * fltTgcScaling))
                    asbytTgcCurve[i] = (sbyte)(-Math.Floor(DIG_GAIN_MAX * fltTgcScaling));
                else
                    asbytTgcCurve[i] = (sbyte)Math.Floor(afltTgcCurve[i]);
            }
            UpdateLabels();
            if ((ByteArrayList.Count != 0) && (bCineOn == false) && (Scan2D.ScanOn == false))/// i.e. Frozen image no scan no cine are running, so refresh the tgc 
                DrawOut(iCineCounter);

        }

        /// <summary>
        /// Applies Time Gain Compensation for the probe.
        /// </summary>
        /// <param name="arrayByte"></param>
        private void ApplyTgc(byte[,] arrayByte)
        {
            int iMax = (int)uiNbOfLines;

            for (int j = 0; j < iMax; j++)
            {
                for (int i = 0; i < ScanConverter.MAX_SAMPLES; i++)
                {
                    short shTmp = (short)arrayByte[j, i];
                    shTmp += asbytTgcCurve[i];
                    if (shTmp < 0)
                        shTmp = 0;
                    else if (shTmp > 255)
                        shTmp = 255;
                    arrayByte[j, i] = (byte)shTmp;
                }
            }
        }

        #endregion

        #region DYNAMIC
        private void DynamicMinus()
        {
            if (iDynamicIndex == 0)
                return;
            iDynamicIndex--;
            HWControls.SendDynamic(abytDynamicValue[iDynamicIndex]);
            UpdateLabels();
        }

        private void DynamicPlus()
        {
            if (iDynamicIndex >= abytDynamicValue.Length - 1)
                return;
            iDynamicIndex++;
            HWControls.SendDynamic(abytDynamicValue[iDynamicIndex]);
            UpdateLabels();
        }
        #endregion
        #region DEPTH
        /// <summary>
        /// To increase the Depth
        /// </summary>
        private void DepthPlus()
        {
            if (iDepthIndex >= iMaxDepthIndex - 1)
                return;
            iDepthIndex++;
            RebuildScanConverter();
        }

        /// <summary>
        /// To decrease the Depth
        /// </summary>
        private void DepthMinus()
        {
            if (iDepthIndex == 0)
                return;
            iDepthIndex--;
            RebuildScanConverter();
        }
        #endregion
        #region FREQUENCY and FOCUS
        void UpdateFrequencyAndFocus()
        {
            StopThreadScan();
            HWControls.SetFrequencyAndFocus(bytFreqIndex, bytFocusIndex, iSteering);
            StartThreadScan();
            UpdateLabels();
        }

        void UpdatePRF()
        {
            StopThreadScan();
            if (bSoftCFMData == true)
                HWControls.UpdateCfmPrf(iDepth, aiPRF[bytPrfIndex], uiNbOfLines);
            StartThreadScan();
            UpdateLabels();
        }

        /// <summary>
        /// To increase the frequency
        /// </summary>

        private void FreqPlus()
        {
            if (bSoftCFMData)
            {
                if (bytPrfIndex >= HWControls.MAX_FREQ - 1)
                    return;

                bytPrfIndex++;
                UpdatePRF();
            }
            else
            {
                if (bytFreqIndex >= HWControls.MAX_FREQ - 1)
                    return;
                bytFreqIndex++;
                UpdateFrequencyAndFocus();
            }
        }

        private void FreqMinus()
        {
            if (bSoftCFMData)
            {
                if (bytPrfIndex == 0)
                    return;

                bytPrfIndex--;
                UpdatePRF();
            }
            else
            {

                if (bytFreqIndex == 0)
                    return;
                bytFreqIndex--;
                UpdateFrequencyAndFocus();
            }
        }

        private void FocusPlus()
        {
            if (bytFocusIndex >= HWControls.MAX_FOCUS - 1)
                return;
            bytFocusIndex++;
            UpdateFrequencyAndFocus();
        }

        private void FocusMinus()
        {
            if (bytFocusIndex == 0)
                return;
            bytFocusIndex--;
            UpdateFrequencyAndFocus();
        }

        #endregion
        #region HIGHVOLTAGE
        /// <summary>
        /// To increase the High Voltage
        /// </summary>
        private void HighPlus()
        {
            if (iHighIndex >= abytHighVoltage.Length - 1)
                return;
            iHighIndex++;
            HWControls.SendHighVoltage(abytHighVoltage[iHighIndex], abytHighVoltageCFM[iHighIndex]);
            UpdateLabels();

        }

        /// <summary>
        /// To decrease the High Voltage
        /// </summary>
        private void HighMinus()
        {
            if (iHighIndex == 0)
                return;
            iHighIndex--;
            HWControls.SendHighVoltage(abytHighVoltage[iHighIndex], abytHighVoltageCFM[iHighIndex]);

            UpdateLabels();
        }

        #endregion
        #region IMAGESPER
        /// <summary>
        /// To increase the High Voltage
        /// </summary>
        private void ImagPlus()
        {
            //TODO: Put code here to adjust number of images per radian or degree or whatever
            //if (iHighIndex >= abytHighVoltage.Length - 1)
            //    return;
            //iHighIndex++;
            //HWControls.SendHighVoltage(abytHighVoltage[iHighIndex], abytHighVoltageCFM[iHighIndex]);
            //UpdateLabels();

        }
        private void ImagMinus()
        {
            //TODO: Put code here to adjust number of images per radian or degree or whatever
            //if (iHighIndex == 0)
            //    return;
            //iHighIndex--;
            //HWControls.SendHighVoltage(abytHighVoltage[iHighIndex], abytHighVoltageCFM[iHighIndex]);
            //UpdateLabels();
        }
        #endregion
        #region STEERING
        private void SteeringPlus()
        {
            if (iSteering >= HWControls.MAX_STEERING_ANGLE)
                return;
            iSteering++;
            RebuildAll();
        }

        private void SteeringMinus()
        {
            if (iSteering == -HWControls.MAX_STEERING_ANGLE)
                return;
            iSteering--;
            RebuildAll();
        }

        #endregion
        #region DOUBLER
        private void butDoubler_Click(object sender, EventArgs e)
        {

            if ((ScanConv.Compound) == true)
                return;
            ScanConverter.Doubler = !ScanConverter.Doubler;
            SetDoubler();
            RebuildAll();
        }

        void SetDoubler()
        {
            if (ScanConverter.Doubler)
            {
                // butDoubler.BackColor = Color.OrangeRed;
                HWControls.EnableDoubler();
            }
            else
            {
                // butDoubler.BackColor = Color.LightSteelBlue;
                HWControls.DisableDoubler();

            }
        }
        #endregion
        #region COMPOUND
        private void butCompound_Click(object sender, EventArgs e)
        {
            if (ScanConverter.Doubler == true)
                return;

            (ScanConv.Compound) = !(ScanConv.Compound);

            SetCompound();
            RebuildAll();
        }

        void SetCompound()
        {
            if (ScanConv.Compound == true)
            {
                // butCompound.BackColor = Color.OrangeRed;
                HWControls.EnableCompound();
                // labelCompound.Visible = true;
            }
            else
            {
                // butCompound.BackColor = Color.LightSteelBlue;
                HWControls.DisableCompound();
                // labelCompound.Visible = false;
            }

        }
        #endregion

        #region REFRESH 
        //TODO: fix image marker to be position or angle
        static bool bIsBusy = false;
        static readonly object locker = new object();

        /// <summary>
        /// NewImageHandler (First Version)
        /// Only takes a picture when interson array says that a new scan is ready (too slow).
        /// </summary>
        /// <param name="e"></param>
        /*
        void firstImageRefresh(IntersonArray.Imaging.Capture scan, EventArgs e)
        {
            // Remove later
            if (RobotState == RobotStateEnum.scanning) {
                imageRefreshCount++;
            }
            if (bIsBusy == false)
            {
                Thread trdRefresh = new Thread(new ThreadStart(StartRefresh));
                trdRefresh.Start();
                while (trdRefresh.IsAlive == false) ;
            }
            
        }
         */

        /// <summary>
        /// Starts a new thread when a new thread is available
        /// </summary>
        void ImageRefresh(Object sender, EventArgs e)
        {
            if (bIsBusy == false)
            {
                Thread trdRefresh = new Thread(new ThreadStart(StartRefresh));
                trdRefresh.Start();
                while (trdRefresh.IsAlive == false) ;
            }
        }

        /// <summary>
        /// Capturing the current image.
        /// Calls DoRefresh to draw the graphics.
        /// Calls AddToCine to save the images into ByteArrayList
        /// </summary>
        void StartRefresh()
        {
            curImageTime = DateTime.Now;
            lock (locker)
            {
                bIsBusy = true;
                if (bSoftRFData == true)
                {
                    AddToCineRF(aushRawRF);
                    BuildRFData(aushRawRF, ifactorRF);
                }
                else
                {
                    Array.Copy(bytRawImage, bytRawImagePrevious, uiNbOfLines * ScanConverter.MAX_SAMPLES);
                    Array.Copy(bytRawImageRef, bytRawImage, uiNbOfLines * ScanConverter.MAX_SAMPLES);
                    // Console.WriteLine("previous step position: " + prevStepPos);
                    if (prevStepPos < curStepPos) {

                        if (curStepPos % 2 == 0) { // Take a scan every 2 steps
                            AddToCine(bytRawImage); //B
                        }
                        prevStepPos++;
                    }
                    ApplyTgc(bytRawImage);      //B
                                                //if (bImgExt == true)
                                                //{
                                                //    imgControl.ApplyLUT(bytRawImage);   
                                                //}

                    if (Scan2D.CFMData == true)
                    {
                        if (bGetRawCfm == false)
                        {
                            AddToCineCFM(abytRawCFM);
                            ImageBuilding.Build2D(ref bmpUSImage, bytRawImage, abytRawCFM, ScanConv);
                        }
                        else
                        {
                            AddToCineCFM(aushRawCFM);
                            BuildCFMData(aushRawCFM, ifactorCFM);
                        }
                    }
                    else
                    {
                        ImageBuilding.Build2D(ref bmpUSImage, bytRawImage, bytRawImagePrevious, ScanConv);
                    }

                }

                // Only refresh every 16 ticks while scanning to save processing
                if (RobotState != RobotStateEnum.scanning || curStepPos % 16 == 0) {
                    DoRefresh();
                }

                bIsBusy = false;
            }
            // Thread.Sleep(1);

        }

        /// <summary>
        /// To refresh uctrlscan, container of the US image.
        /// Drawing the graphics.
        /// </summary>
        void DoRefresh()
        {
            Graphics g = uctrlScan.CreateGraphics();
            if (bmpLoad != null)
            {
                Graphics gl = CreateGraphics();
                gl.DrawImage(bmpLoad, uctrlDepth.Location.X, uctrlDepth.Location.Y);
                gl.Dispose();
                return;
            }

            if (bSoftRFData)//RF
                DrawCurve(0, 0, ifactorRF);
            else if ((bGetRawCfm == true) && (bSoftCFMData == true))//RF CFM
                DrawCurve(0, 0, ifactorRF);
            else//2D B / Color 
            {

                if (bmpUSImage == null)
                    return;

                if (Scan2D.ScanOn == false)
                {
                    if (bmpOut != null)//so already scanned
                    {
                        CineloopSlide();
                    }
                }
                else
                {
                    if (bmpUSImage == null)
                        return;

                    Rectangle destRect;
                    Rectangle srcRect;

                    // Original size of the image
                    destRect = new Rectangle(0, 0, bmpUSImage.Width, bmpUSImage.Height);
                    // Zoom
                    srcRect = new Rectangle(iZoomX, iZoomY, (int)(destRect.Width / fltZoomFactor), (int)(destRect.Height / fltZoomFactor));

                    using (Graphics gr = Graphics.FromImage(bmpOut))
                    {
                        gr.DrawImage(bmpUSImage, new Rectangle(0, 0, bmpOut.Width, bmpOut.Height));
                    }

                    if (bSoftCFMData)
                    {
                        ImageBuilding.DrawCFMBox(ref bmpOut, ScanConv, iDepthCfmBox, colBox);
                    }

                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    g.DrawImage(bmpOut, destRect, srcRect, GraphicsUnit.Pixel);
                }
            }
            if (ZoomBox.Visible)
                ZoomBox.Draw(g);
            g.Dispose();
        }

        /// <summary>
        /// to Draw the RF curve
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="factor"></param>
        void DrawCurve(int x, int y, int factor)
        {
            Graphics g = uctrlScan.CreateGraphics();
            //Create a Bitmap object with the size of the panel
            Bitmap curBmp = new Bitmap(uctrlScan.Width, uctrlScan.Height / factor);
            //Create a temporary Graphics object from the bitmap
            Graphics gCur = Graphics.FromImage(curBmp);
            // gCur.Clear(this.BackColor);
            //RF
            Pen penCurrent = new Pen(Color.Red);
            gCur.DrawCurve(penCurrent, aptRFdata);
            //Call DrawImage of Graphics and draw bitmap
            g.DrawImage(curBmp, x, y);

            //Dispose of objects
            gCur.Dispose();
            curBmp.Dispose();


        }

        /// <summary>
        /// to calculate the points of the curve, Display one line of the RF data
        /// </summary>
        /// <param name="aushBuild"></param>
        /// <param name="factor"></param>
        void BuildRFData(ushort[,] aushBuild, int factor)
        {
            int start = 0;
            for (int j = 0; j < ScanConverter.MAX_RFSAMPLES; j++)
            {
                int rounddata = (int)((double)((short)(aushBuild[iRFNumLine, start]) * this.uctrlScan.Height / factor) / RF_SWING);
                aptRFdata[j] = new Point((int)((double)(j * this.uctrlScan.Width) / ScanConverter.MAX_RFSAMPLES), System.Math.Abs(rounddata - this.uctrlScan.Height / (2 * factor)));
                start++;
            }
        }
        /// <summary>
        /// to calculate the points of the curve. Display one line of the CFM data (raw data then RF)
        /// </summary>
        /// <param name="aushBuild"></param>
        /// <param name="factor"></param>
        void BuildCFMData(ushort[] aushBuild, int factor) //352*2048 => line=0 of 352
        {
            int start = 0;
            for (int j = 0; j < ScanConverter.MAX_RFSAMPLES; j++)
            {
                int rounddata = (int)((double)((short)(aushBuild[iCFMNumLine * ScanConverter.MAX_RFSAMPLES + start]) * this.uctrlScan.Height / factor) / RF_SWING);
                aptRFdata[j] = new Point((int)((double)(j * this.uctrlScan.Width) / ScanConverter.MAX_RFSAMPLES), System.Math.Abs(rounddata - this.uctrlScan.Height / (2 * factor)));
                start++;
            }
        }


        #endregion

        #region DISPLAY
        /// <summary>
        ///  Enables accessing a form from another thread
        /// </summary>
        /// <param name="eCtrl">A enum that points to the control to perform the action</param>
        /// <param name="textToDisplay">Text that the form displays or uses for 
        ///  another purpose. Actions that don't use text ignore this parameter.</param>
        private void MyMarshalToForm(ControlEnum eCtrl, String textToDisplay)
        {

            object[] args = { eCtrl, textToDisplay };
            MarshalToForm MarshalToFormDelegate = null;

            try
            {
                //  The AccessForm routine contains the code that accesses the form.
                MarshalToFormDelegate = new MarshalToForm(AccessForm);

                //  Execute AccessForm, passing the parameters in args.
                base.Invoke(MarshalToFormDelegate, args);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// MarshalToForm
        /// </summary>
        /// <param name="eCtrl"></param>
        /// <param name="formText"></param>
        private void AccessForm(ControlEnum eCtrl, String formText)
        {
            switch (eCtrl)
            {
                case ControlEnum.buttonScan:
                    // buttonScan.Text = formText;
                    break;
                case ControlEnum.buttonCine:
                    buttonCine.Text = formText;
                    break;
                case ControlEnum.TrackBarCounter:
                    protoTrackBarCine.Value = iCineCounter;
                    break;
                case ControlEnum.buttonProbe1:
                    // buttonProbe1.Text = formText;
                    break;
                case ControlEnum.buttonProbe2:
                    buttonProbe2.Text = formText;
                    break;
                case ControlEnum.Probe1Enabled:
                    // buttonProbe1.Enabled = true;
                    break;
                case ControlEnum.Probe2Enabled:
                    buttonProbe2.Enabled = true;
                    break;
                case ControlEnum.RFModeEnabled:
                    // butRFMode.Enabled = true;
                    break;
                case ControlEnum.CfmModeEnabled:
                    butCfmMode.Enabled = true;
                    break;
                case ControlEnum.UpdateTrackBar:
                    UpdateTrackBar();
                    break;

            }
        }

        /// <summary>
        /// To display GrayScale.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uctrlGrayScale_Paint(object sender, PaintEventArgs e)
        {
            uctrlGrayScale.DrawGrayScale();
        }

        /// <summary>
        /// paint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void formScan2D_Paint(object sender, PaintEventArgs e)
        {
            if (bmpLoad != null)
            {
                DoRefresh();
            }

        }

        private void uctrlScan_Paint(object sender, PaintEventArgs e)
        {

        }


        #endregion

        #region ZOOM
        /// //////////
        /// To be used with ZoomBoxClass (ToolsLibrary.dll)
        /// //////////


        /// <summary>
        /// To reset the Zoom, back to ZOOM_MIN
        /// </summary>
        private void ResetZoom()
        {
            fltZoomFactor = ZoomBox.ZOOM_MIN;
            ZoomBox.Visible = false;
            iZoomX = iZoomY = 0;
            UpdateLabels();
            DoRefresh();
        }

        /// <summary>
        /// Command to reset Zoom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonZoomBox_Click(object sender, EventArgs e)
        {
            ResetZoom();
        }

        /// <summary>
        /// Resize the Zoom Box according the mouse coordinates
        /// </summary>
        /// <param name="iX"></param>
        /// <param name="iY"></param>
        public void MouseResizeBox(int iX, int iY)
        {
            if (ZoomBox.Visible)
            {
                ZoomBox.Drag(iX, iY);// resize the box
                if (Scan2D.ScanOn == false)
                    DoRefresh();
            }
        }

        /// <summary>
        /// Command to resize the zoom box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uctrlScan_MouseMove(object sender, MouseEventArgs e)
        {
            if (ZoomBox.Visible == true)
                MouseResizeBox(e.X, e.Y);
        }

        /// <summary>
        /// To Display the Zoom Box
        /// </summary>
        /// <param name="iX"></param>
        /// <param name="iY"></param>
        public void InitZoom(int iX, int iY)
        {
            ResetZoom();
            ZoomBox.Visible = true;

            ZoomBox.InitZoomBox(new Point(iX, iY));
            ZoomBox.Drag(iX, iY);
            DoRefresh();
        }

        /// <summary>
        /// Command to display the zoom box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uctrlScan_MouseClick(object sender, MouseEventArgs e)
        {
            if (ZoomBox.Visible == false)
            {
                InitZoom(e.X, e.Y);
            }

        }

        /// <summary>
        /// To Validate the ZoomFactor
        /// </summary>
        private void ValidateZoomFactor()
        {
            if (ZoomBox.ZoomFactor < ZoomBox.ZOOM_MIN || ZoomBox.ZoomFactor > ZoomBox.ZOOM_MAX)
            {
                ZoomBox.ZoomFactor = ZoomBox.ZOOM_MIN;
                ZoomBox.Visible = false;
                iZoomX = iZoomY = 0;
            }
            UpdateLabels();
        }

        /// <summary>
        /// set Zoom parameters
        /// </summary>
        public void SetZoom()
        {
            if (ZoomBox.Visible == true)
            {
                ZoomBox.Visible = false;
                Rectangle region = ZoomBox.RectangleZoom;
                iZoomX = region.Location.X;
                iZoomY = region.Location.Y;
                fltZoomFactor = ZoomBox.ZoomFactor;
            }
        }

        /// <summary>
        /// Command to validate the Zoom box and set Zoom parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uctrlScan_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (ZoomBox.Visible == true)
            {
                ValidateZoomFactor();
                SetZoom();
                UpdateLabels();
                DoRefresh();
            }
        }
        #endregion

        #region CINE
        //TODO: Mark images by their position or angle
        /// <summary>
        /// When scanning, to Add the last Raw data to the List( i.e. cineloop)
        /// Adds all of the images taken during the scan to ByteArrayList
        /// </summary>
        /// <param name="arrayByte"></param>
        void AddToCine(byte[,] arrayByte)
        {
            Byte[,] bytRawCine;

            bytRawCine = new byte[(int)uiNbOfLines, ScanConverter.MAX_SAMPLES];
            Array.Copy(arrayByte, bytRawCine, (int)uiNbOfLines * ScanConverter.MAX_SAMPLES);

            if (Scan2D.ScanOn == true)
            {
                if (ByteArrayList.Count == ByteArrayList.Capacity)
                {
                    ByteArrayList.RemoveAt(0);

                    for (int i = 1; i < ByteArrayList.Count; ++i)
                    {
                        cineImageTimes[i - 1] = cineImageTimes[i];
                        cineStepCount[i - 1] = cineStepCount[i];
                    }
                }
                ByteArrayList.Add(bytRawCine);
                cineImageTimes[ByteArrayList.Count - 1] = curImageTime;
                cineStepCount[ByteArrayList.Count - 1] = curStepPos;
            }
        }

        /// <summary>
        /// When scanning, to Add the last Raw RF data to the List( i.e. cineloop)
        /// </summary>
        /// <param name="arrayUshort"></param>
        void AddToCineRF(ushort[,] arrayUshort)
        {
            ushort[,] aushRawCine;

            //RF Mode
            aushRawCine = new ushort[(int)uiNbOfLines, ScanConverter.MAX_RFSAMPLES];
            Array.Copy(arrayUshort, aushRawCine, (int)uiNbOfLines * ScanConverter.MAX_RFSAMPLES);

            if (Scan2D.ScanOn == true)
            {
                if (ushortArrayList.Count == ushortArrayList.Capacity)
                {
                    ushortArrayList.RemoveAt(0);
                }
                ushortArrayList.Add(aushRawCine);
            }
        }

        /// <summary>
        /// When scanning, to Add the last Raw CFM data to the List( i.e. cineloop)
        /// </summary>
        /// <param name="arrayByte"></param>
        void AddToCineCFM(byte[] arrayByte)
        {
            //CFM mode
            byte[] abytRawCine = new byte[iCfmSamples * ScanConverter.MAX_CFMLINE]; ;
            Array.Copy(arrayByte, abytRawCine, iCfmSamples * ScanConverter.MAX_CFMLINE);

            if (Scan2D.ScanOn == true)
            {
                if (ByteUniArrayList.Count == ByteUniArrayList.Capacity)
                {
                    ByteUniArrayList.RemoveAt(0);
                }
                ByteUniArrayList.Add(abytRawCine);
            }
        }

        /// <summary>
        /// Ushort version
        /// </summary>
        /// <param name="arrayUshort"></param>
        void AddToCineCFM(ushort[] arrayUshort)
        {
            //CFM mode
            ushort[] aushRawCine = new ushort[32 * 11 * ScanConverter.MAX_RFSAMPLES]; ;
            Array.Copy(arrayUshort, aushRawCine, 32 * 11 * ScanConverter.MAX_RFSAMPLES);

            if (Scan2D.ScanOn == true)
            {
                if (ushortUniArrayList.Count == ushortUniArrayList.Capacity)
                {
                    ushortUniArrayList.RemoveAt(0);
                }
                ushortUniArrayList.Add(aushRawCine);
            }
        }

        /// <summary>
        /// To start the cineloop.
        /// </summary>
        private void StartCineloop()
        {
            if (bCineOn == false)
            {
                bCineOn = true;
                trdCineShow = new Thread(new ParameterizedThreadStart(CinePlay));// create the thread
                short shFrameRate = (short)(1000 / MyHwControls.GetProbeFrameRate());

                trdCineShow.Start(shFrameRate);// then start
                trdCineShow.IsBackground = true;
                while (trdCineShow.IsAlive == false) ;
            }
        }

        /// <summary>
        /// To stop the cineloop.
        /// </summary>
        private void StopCineloop()
        {
            if (bCineOn == true)
            {
                bCineOn = false;
                trdCineShow.Join(1000);//just added 1000 to be sure not hanged
                trdCineShow = null;
            }

        }

        /// <summary>
        /// Displays the slide "iCineCounter" of the cineloop.
        /// </summary>
        private void CineloopSlide()
        {
            if (bSoftRFData)
            {
                if (ushortArrayList.Count() == 0)
                    return;
                if (iCineCounter >= ushortArrayList.Count())
                    iCineCounter = 0;
                if (iCineCounter < 0)
                    iCineCounter = ushortArrayList.Count() - 1;
            }
            else
            {

                if (ByteArrayList.Count() == 0)
                    return;
                if (iCineCounter >= ByteArrayList.Count())
                    iCineCounter = 0;
                if (iCineCounter < 0)
                    iCineCounter = ByteArrayList.Count() - 1;
            }
            DrawOut(iCineCounter);
        }

        /// <summary>
        /// Draws the graphics for playing back the cine. 
        /// </summary>
        /// <param name="slide"></param>
        void DrawOut(int slide)
        {
            if (bmpOut == null) // So Idle mode, so use iIdleIndexSC
                bmpOut = new Bitmap(aiWidth[iIdleIndexSC], aiHeigth[iIdleIndexSC], System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            Graphics g = uctrlScan.CreateGraphics();
            Rectangle destRect = uctrlScan.ClientRectangle;
            Rectangle srcRect = new Rectangle(iZoomX, iZoomY, (int)(destRect.Width / fltZoomFactor), (int)(destRect.Height / fltZoomFactor));
            int iCfmBoxDepth = 0;
            int inbData = 0;
            int inbDataCfm = ScanConverter.MAX_CFMLINE * iCfmSamples;
            int imax = 0;

            if (bSoftRFData)
                imax = ScanConverter.MAX_RFSAMPLES;
            else
                imax = ScanConverter.MAX_SAMPLES;


            if (bIdle)
            {
                inbData = (int)uiIdleNbOfLinesPerArray * imax;
                iCfmBoxDepth = iIdleDepthCfmBox;
            }
            else
            {
                inbData = (int)uiNbOfLines * imax;
                iCfmBoxDepth = iDepthCfmBox;
            }


            if (bSoftRFData)
            {
                Array.Copy(ushortArrayList.ElementAt(slide), aushRawRF, inbData);
                BuildRFData(aushRawRF, ifactorRF);
                DrawCurve(0, 0, ifactorRF);

            }
            else
            {

                Array.Copy(ByteArrayList.ElementAt(slide), bytRawImage, inbData);
                if (((ScanConv.Compound) && (bIdle == false))
                    || ((bIdleCompound == true) && (bIdle == true)))
                {
                    if (slide == 0)
                    {
                        Array.Copy(ByteArrayList.ElementAt(ByteArrayList.Count - 1), bytRawImagePrevious, inbData);

                    }
                    else
                    {
                        Array.Copy(ByteArrayList.ElementAt(slide - 1), bytRawImagePrevious, inbData);
                    }
                }


                // if used then to do it
                ApplyTgc(bytRawImage);
                ApplyTgc(bytRawImagePrevious);

                //if (bImgExt == true)
                //{
                //    imgControl.ApplyLUT(bytRawImage);    
                //    imgControl.ApplyLUT(bytRawImagePrevious);    
                //}

                if (bSoftCFMData == false)
                    ImageBuilding.Build2D(ref bmpOut, bytRawImage, bytRawImagePrevious, ScanConv);// build 
                else
                {
                    //                    if (ByteUniArrayList.Count != 0)
                    if (ByteUniArrayList.Count == ByteArrayList.Count)
                        Array.Copy(ByteUniArrayList.ElementAt(slide), abytRawCFM, inbDataCfm);

                    ImageBuilding.Build2D(ref bmpOut, bytRawImage, abytRawCFM, ScanConv);
                    ImageBuilding.DrawCFMBox(ref bmpOut, ScanConv, iDepthCfmBox, colBox);

                }

                {
                    destRect = new Rectangle(0, 0, bmpOut.Width, bmpOut.Height);
                    srcRect = new Rectangle(iZoomX, iZoomY, (int)(destRect.Width / fltZoomFactor), (int)(destRect.Height / fltZoomFactor));
                }


                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;

                g.DrawImage(bmpOut, destRect, srcRect, GraphicsUnit.Pixel);
                g.Dispose();
            }

        }

        /// <summary>
        /// Plays the cineloop.
        /// </summary>
        /// <param name="FrameRateMilliSeconds"> frame Rate of the probe</param>
        void CinePlay(object FrameRateMilliSeconds)
        {
            short shFrameRate = (short)(FrameRateMilliSeconds);
            while (bCineOn)
            {
                stopWatch.Reset();
                stopWatch.Start();

                CineloopSlide();
                MyMarshalToForm(ControlEnum.TrackBarCounter, "");

                while (stopWatch.ElapsedMilliseconds <= shFrameRate) { }
                stopWatch.Stop();
                iCineCounter++;

                Debug.WriteLine("ElapsedMilliseconds: " + stopWatch.ElapsedMilliseconds.ToString());
            }
        }

        /// <summary>
        /// Outside of the thread of the cineloop, to display the next image
        /// </summary>
        void NextImage()
        {
            StopCineloop();
            iCineCounter++;
            CineloopSlide();
        }

        /// <summary>
        /// Outside of the thread of the cineloop, to display the previous image.
        /// </summary>
        void PreviousImage()
        {
            StopCineloop();
            iCineCounter--;
            CineloopSlide();
        }
              
        #endregion

        #region INTERFACE
        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkboxUpDown_CheckedChanged(object sender, EventArgs e)
        {
            bIsUpDown = !bIsUpDown;
            RebuildScanConverter();
        }

        private void chkboxLR_CheckedChanged(object sender, EventArgs e)
        {
            bIsLeftRight = !bIsLeftRight;
            RebuildScanConverter();
        }

        private void butRFMode_Click(object sender, EventArgs e)
        {
            Scan2D.CFMData = false;
            bSoftCFMData = Scan2D.CFMData;
            butCfmMode.BackColor = Color.LightSteelBlue;

            StopScan();
            Scan2D.RFData = !Scan2D.RFData;// this is the reference.
            bSoftRFData = Scan2D.RFData;

            RebuildScanConverter();

            UpdateButtons();
        }

        private void butCfmMode_Click(object sender, EventArgs e)
        {

            //StopScan();

            Scan2D.RFData = false;
            bSoftRFData = Scan2D.RFData;

            Scan2D.FrameAvg = false;
            ScanConv.Compound = false;
            ScanConverter.Doubler = false;
            SetCompound();
            SetDoubler();


            Scan2D.CFMData = !Scan2D.CFMData;
            bSoftCFMData = Scan2D.CFMData;

            RebuildAll();
            SetUItoCFM(bSoftCFMData);

            // butRFMode.BackColor = Color.LightSteelBlue;

        }

        /// <summary>
        /// Sets UI into CFM mode.
        /// </summary>
        /// <param name="bcfm"></param>
        void SetUItoCFM(bool bcfm)
        {
            if (bcfm == true)
            {
                butCfmMode.BackColor = Color.OrangeRed;
                protoUCtrlPMFrequency.Init("PRF", FreqPlus, FreqMinus);
                protoUCtrlPMGalGain.Init("CFM", MainGainPlus, MainGainMinus);

            }
            else
            {
                butCfmMode.BackColor = Color.LightSteelBlue;
                protoUCtrlPMFrequency.Init("Freq", FreqPlus, FreqMinus);
                protoUCtrlPMGalGain.Init("Main", MainGainPlus, MainGainMinus);

            }
            Graphics g = uctrlScan.CreateGraphics();
            // g.Clear(this.BackColor);
            g.Dispose();

        }

        /// <summary>
        /// Returns a bitmap to save.
        /// </summary>
        /// <returns></returns>
        Bitmap DoSaveScan()
        {
            Bitmap bmpSave = new Bitmap(aiWidth[iIndexSC] + uctrlDepth.Width, aiHeigth[iIndexSC]);
            // Copy Depth scale 
            uctrlDepth.DrawToBitmap(bmpSave, new Rectangle(0, 0, bmpSave.Width, bmpSave.Height));

            Graphics g = Graphics.FromImage(bmpSave);

            if (bmpOut != null)
            {
                Bitmap bmpImage = new Bitmap(bmpUSImage.Width, bmpUSImage.Height);//---- US Image
                Rectangle destRect = new Rectangle(0, 0, bmpUSImage.Width, bmpUSImage.Height);
                //Resized according to the Zoom factor
                Rectangle srcRect = new Rectangle(iZoomX, iZoomY, (int)(destRect.Width / fltZoomFactor), (int)(destRect.Height / fltZoomFactor));
                Graphics gI = Graphics.FromImage(bmpImage);
                gI.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                gI.DrawImage(bmpOut, destRect, srcRect, GraphicsUnit.Pixel);
                g.DrawImage(bmpImage, new Point(uctrlDepth.Width, 0));
                bmpImage.Dispose();
            }

            g.Dispose();
            return bmpSave;
        }

        /// <summary>
        /// Save the raw data: 
        /// 2 files ImageName.raw with the Raw Data, ImageName.dat with the minimum charateristics to retrieve 
        /// the image: depth, up/down, left/right, size of ScanConverter, (Index of ScanConverter), ProbeID
        /// </summary>
        /// <param name="fileName"></param>
        void DoSaveRaw(string fileName)
        {
            StreamWriter file = null;

            BinaryWriter bwrtFile = null;
            FileStream writer = null;
            int l = 0;
            string RawRFFile = fileName.Replace(".raw", "RF.raw");
            string TextRFFile = fileName.Replace(".raw", "RF.txt");
            string RawCfmFile = fileName.Replace(".raw", "Cfm.raw");//CFM raw

            int max = (int)uiNbOfLines;

            try
            {
                if (bSoftRFData)
                {
                    writer = new FileStream(RawRFFile, FileMode.OpenOrCreate);
                    bwrtFile = new BinaryWriter(writer);
                    file = new System.IO.StreamWriter(TextRFFile);

                    int i, j;
                    for (j = 0; j < ScanConverter.MAX_RFSAMPLES; j++)
                    {
                        for (i = 0; i < max; i++)
                        {
                            bwrtFile.Write(aushRawRF[i, j]);
                        }
                    }

                    //text file in RF
                    for (j = 0; j < ScanConverter.MAX_RFSAMPLES; j++)
                    {
                        for (i = 0; i < max; i++)
                        {
                            short shtmp = (short)aushRawRF[i, j];
                            file.Write(shtmp.ToString() + ",");
                        }
                        file.WriteLine("");
                    }

                }
                else
                {


                    //---- erase old File
                    writer = new FileStream(fileName, FileMode.OpenOrCreate);
                    bwrtFile = new BinaryWriter(writer);

                    int i, j;

                    for (j = 0; j < ScanConverter.MAX_SAMPLES; j++)
                    {
                        for (i = 0; i < max; i++)
                        {
                            bwrtFile.Write(bytRawImage[i, j]);
                            l++;
                        }
                    }


                    if (ScanConv.Compound)
                    {
                        for (j = 0; j < ScanConverter.MAX_SAMPLES; j++)
                        {
                            for (i = 0; i < max; i++)
                            {
                                bwrtFile.Write(bytRawImagePrevious[i, j]);
                                l++;
                            }
                        }
                    }
                }

            }
            catch (Exception eFile)
            {
                System.Windows.Forms.MessageBox.Show(eFile.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
                if (file != null)
                {
                    file.Close();
                }

            }


            writer = new FileStream(RawCfmFile, FileMode.OpenOrCreate);
            bwrtFile = new BinaryWriter(writer);
            int maxcfm = ScanConverter.MAX_CFMLINE * iCfmSamples;
            try
            {
                int i;
                if (bSoftCFMData == true)
                {
                    if (bGetRawCfm == false)
                    {
                        //Image CFM
                        for (i = 0; i < maxcfm; i++)
                        {
                            bwrtFile.Write(abytRawCFM[i]);
                        }
                    }
                    else
                    {
                        //Image CFM: raw RF data, TO DO: Choose the format of the saved data depending of the application(binary, text..)
                        maxcfm = ScanConverter.MAX_CFM_LINES * ScanConverter.MAX_RFSAMPLES;
                        for (i = 0; i < maxcfm; i++)
                        {
                            bwrtFile.Write(aushRawCFM[i]);
                        }
                    }
                }
            }

            catch (Exception eFile)
            {
                System.Windows.Forms.MessageBox.Show(eFile.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }

            }


            string strtmp = fileName.Replace(".raw", ".dat");
            if (bSoftRFData)
                strtmp = fileName.Replace(".raw", "RF.dat");

            try
            {

                writer = new FileStream(strtmp, FileMode.OpenOrCreate);
                bwrtFile = new BinaryWriter(writer);
                WriteData(bwrtFile, writer);
            }
            catch (Exception eFile)
            {
                System.Windows.Forms.MessageBox.Show(eFile.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }
        }

        /// <summary>
        /// Write a binary file with the values from Depth, Frequency, etc
        /// </summary>
        /// <param name="bwrtFile"></param>
        /// <param name="writer"></param>
        void WriteData(BinaryWriter bwrtFile, FileStream writer)
        {
            bwrtFile.Write(HWControls.GetProbeID());

            bwrtFile.Write(aiWidth[iIndexSC]);
            bwrtFile.Write(aiHeigth[iIndexSC]);
            bwrtFile.Write(iIndexSC);

            bwrtFile.Write(iDepth);

            bwrtFile.Write(uiNbOfLines);

            bwrtFile.Write(ScanConverter.Doubler);

            bwrtFile.Write(ScanConv.Compound);
            bwrtFile.Write(MyHwControls.CompoundAngle);
            bwrtFile.Write(iSteering);

            if (ShortVersion >= fltRFVersion)
            {
                bwrtFile.Write(Scan2D.RFData);
                bwrtFile.Write(Scan2D.CFMData);
                bwrtFile.Write(iDepthCfmBox);

            }

        }

        /// <summary>
        /// Load the raw data.
        /// </summary>
        /// <param name="fileName"></param>
        void DoLoadRaw(string fileName)
        {
            BinaryReader brdFile = null;
            FileStream reader = null;
            string strData = fileName.Replace(".raw", ".dat");
            uint max = 0;

            try
            {
                reader = new FileStream(strData, FileMode.Open);
                brdFile = new BinaryReader(reader);

                ReadData(brdFile, reader);
            }
            catch (Exception eFile)
            {
                System.Windows.Forms.MessageBox.Show(eFile.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            max = uiIdleNbOfLinesPerArray;
            bytRawImage = new byte[max, ScanConverter.MAX_SAMPLES];
            bytRawImagePrevious = new byte[max, ScanConverter.MAX_SAMPLES];
            aushRawRF = new ushort[max, ScanConverter.MAX_RFSAMPLES];

            if (bSoftRFData == true)
            {
                try
                {
                    reader = new FileStream(fileName, FileMode.Open);
                    brdFile = new BinaryReader(reader);
                    for (int j = 0; j < ScanConverter.MAX_RFSAMPLES; j++)
                    {
                        for (int i = 0; i < max; i++)
                        {
                            aushRawRF[i, j] = (ushort)brdFile.ReadUInt16();
                        }
                    }
                }
                catch (Exception eFile)
                {
                    System.Windows.Forms.MessageBox.Show(eFile.Message);
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                }

            }
            else
            {
                try
                {
                    reader = new FileStream(fileName, FileMode.Open);
                    brdFile = new BinaryReader(reader);
                    for (int j = 0; j < ScanConverter.MAX_SAMPLES; j++)
                    {
                        for (int i = 0; i < max; i++)
                        {
                            bytRawImage[i, j] = (byte)brdFile.ReadByte();
                        }
                    }
                    if (bIdleCompound == true)
                    {
                        for (int j = 0; j < ScanConverter.MAX_SAMPLES; j++)
                        {
                            for (int i = 0; i < max; i++)
                            {
                                bytRawImagePrevious[i, j] = (byte)brdFile.ReadByte();
                            }
                        }

                    }
                }
                catch (Exception eFile)
                {
                    System.Windows.Forms.MessageBox.Show(eFile.Message);
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                }


                if (bSoftCFMData == true)
                {
                    try
                    {
                        abytRawCFM = new byte[ScanConverter.MAX_CFMLINE * iCfmSamples];

                        int maxcfm = ScanConverter.MAX_CFMLINE * iCfmSamples;
                        string strtmp = fileName.Replace(".raw", "Cfm.raw");

                        reader = new FileStream(strtmp, FileMode.Open);
                        brdFile = new BinaryReader(reader);
                        for (int i = 0; i < maxcfm; i++)
                        {
                            abytRawCFM[i] = (byte)brdFile.ReadByte();
                        }
                    }
                    catch (Exception eFile)
                    {
                        System.Windows.Forms.MessageBox.Show(eFile.Message);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Reads data.txt file so redraw the depth scale
        /// </summary>
        /// <param name="brdFile"></param>
        /// <param name="reader"></param>
        void ReadData(BinaryReader brdFile, FileStream reader)
        {
            shIdleId = brdFile.ReadInt16();

            iIdleWidth = brdFile.ReadInt32();
            iIdleHeight = brdFile.ReadInt32();
            iIdleIndexSC = brdFile.ReadInt32();

            iIdleDepth = brdFile.ReadInt32();

            uiIdleNbOfLinesPerArray = brdFile.ReadUInt32();

            bIdleDoubler = brdFile.ReadBoolean();

            bIdleCompound = brdFile.ReadBoolean();
            iIdleCompoundAngle = brdFile.ReadInt32();
            iIdleSteering = brdFile.ReadInt32();
            //RF data
            if (ShortVersion >= fltRFVersion)
            {
                bSoftRFData = false;
                bSoftCFMData = false;
                bSoftRFData = brdFile.ReadBoolean();
                bSoftCFMData = brdFile.ReadBoolean();
                if (brdFile.BaseStream.Position != brdFile.BaseStream.Length)
                    iIdleDepthCfmBox = brdFile.ReadInt32();
                else
                    iIdleDepthCfmBox = iDepthCfmBox;

            }

        }

        /// <summary>
        /// Displays the controls when not in idle mode
        /// </summary>
        void ShowDisplayControls()
        {
            uctrlDepth.Visible = true;
            uctrlScan.Visible = true;
            labelFileName.Visible = false;
            if (bmpLoad != null)
            {
                bmpLoad.Dispose();
                bmpLoad = null;
            }
            DoClear();
        }

        /// <summary>
        /// Hides display controls in idle mode
        /// </summary>
        void HideDisplayControls()
        {
            uctrlDepth.Visible = false;
            uctrlGrayScale.Visible = false;
            uctrlScan.Visible = false;
            labelFileName.Visible = true;
            DoClear();
        }

        /// <summary>
        /// Sets the graphics object to a new instance
        /// </summary>
        void DoClear()
        {
            Graphics g = CreateGraphics();
            g.Dispose();
        }

        /// <summary>
        /// Draws the Depth scale.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uctrlDepth_Paint(object sender, PaintEventArgs e)
        {
            uctrlDepth.BuildDrawScale(e.Graphics, iDepth, this.ScanConv, bIsUpDown, fltZoomFactor, iOffsetScale);
        }

        /// <summary>
        /// To watch the Hardware Button, Event is raised when Button is released
        /// </summary>
        /// <param name="HwCtrl"></param>
        /// <param name="e"></param>
        void WatchHWButton(HWControls HwCtrl, EventArgs e)
        {
            buttonScan_Click((object)HwCtrl, e);
        }

        /// <summary>
        /// Resizes the form.
        /// However, currently set up so that the form can't resize.
        /// </summary>
        /// <param name="i"></param>
        void ResizeForm(int i)
        {
            //Added boxes for UScanGuide here
            uctrlGrayScale.Visible = false;
            this.uctrlScan.Size = new Size(aiWidth[i], aiHeigth[i]);
            this.uctrlDepth.Size = new Size(30, aiHeigth[i]);
            // this.panelLabel.Location = new Point(aiWidth[i] + 140, 47);
            // this.panelSwitch.Location = new Point(aiWidth[i] + 140, 275);
            /*
            this.gboxSave.Location = new Point(aiWidth[i] + 293, 35);
            this.gBoxCineloop.Location = new Point(aiWidth[i] + 293, 140);
            this.gBoxTgc.Location = new Point(aiWidth[i] + 293, 290);
            this.gboxManual.Location = new Point(aiWidth[i] + 293, 520);
            this.cboxSCSize.Location = new Point(aiWidth[i] + 120, 500);
            this.gBoxImaging.Location = new Point(25, aiHeigth[i] + 57);
            this.gboxImage.Location = new Point(360, aiHeigth[i] + 57);
            this.gboxGain.Location = new Point(490, aiHeigth[i] + 57);
            this.gboxAngle.Location = new Point(630, aiHeigth[i] + 57);
            this.gboxImagesPer.Location = new Point(720, aiHeigth[i] + 57);
            */
            // this.Size = new Size(aiWidth[i] + 508, aiHeigth[i] + 256);
            // this.Size = new Size(1182, 788);
        }

        #endregion

        #region Probe De/Connect
        /// <summary>
        /// Watch the USB connections:
        /// The timer is disabled when scanning, then enabled when idle.
        /// The interval of the timer is 500ms
        /// This example has two buttons to reflect the connections of the USB connectors. 
        /// The name of each probe is printed inside the button. Push the button to switch probes.
        /// </summary>

        static Boolean bTimerConnect = false;
        static System.Windows.Forms.Timer timerConnect = null;

        /// <summary>
        /// To Start watching the USB connections
        /// </summary>
        public void EnableConnect()
        {
            if (bTimerConnect == false)
            {
                timerConnect = new System.Windows.Forms.Timer();
                timerConnect.Tick += new EventHandler(WatchConnect);
                timerConnect.Interval = 500;
                timerConnect.Enabled = true;
                timerConnect.Start();

                bTimerConnect = true;
            }
        }

        /// <summary>
        /// To Stop watching the USB connections
        /// </summary>
        public void DisableConnect()
        {
            bTimerConnect = false;
            if (timerConnect != null)
            {
                timerConnect.Tick -= new EventHandler(WatchConnect);
                timerConnect.Stop();
                timerConnect.Dispose();
            }
        }

        /// <summary>
        /// Initializes/clears HWControls
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WatchConnect(Object sender, EventArgs e)
        {
            HWControls MyHwControls = new HWControls();
        }

        private void buttonProbe1_Click(object sender, EventArgs e)
        {
            /*
            if (buttonProbe1.Text != strNotConnected)
                SwitchProbe(0);
             */
        }

        private void buttonProbe2_Click(object sender, EventArgs e)
        {
            if (buttonProbe2.Text != strNotConnected)
                SwitchProbe(1);
        }

        /// <summary>
        /// Switches from one probe to another if there are multiple probes attached 
        /// </summary>
        /// <param name="index"></param>
        void SwitchProbe(int index)
        {
            HWControls localHwControls = new HWControls();
            localHwControls.FindMyProbe(index);
            iSelectedProbe = index;
            //---- Get the probe's characteristics
            SetSelectedProbe();
            DisableIdle();
        }

        /// <summary>
        /// Does nothing?
        /// </summary>
        void UpdateProbesButtons()
        {
        }
        #endregion

        #region ADD/REMOVE DEVICE
        /// <summary>
        /// Another way to check the connections. this is the one used
        /// </summary>
        private ManagementEventWatcher _deviceRemovedWatcher;
        private ManagementEventWatcher _deviceArrivedWatcher;
        static bool bIsIntersonProbeDetected = false;


        ///  <summary>
        ///  Add a handler to detect arrival of devices.
        ///  </summary>
        void AddDeviceArrivedHandler()
        {
            const Int32 pollingIntervalSeconds = 1;
            var scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;

            try
            {
                var q = new WqlEventQuery();
                q.EventClassName = "__InstanceCreationEvent";
                q.WithinInterval = new TimeSpan(0, 0, pollingIntervalSeconds);
                q.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'";
                _deviceArrivedWatcher = new ManagementEventWatcher(scope, q);
                _deviceArrivedWatcher.EventArrived += DeviceAdded;

                _deviceArrivedWatcher.Start();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                if (_deviceArrivedWatcher != null)
                    _deviceArrivedWatcher.Stop();
            }
        }


        ///  <summary>
        ///  Add a handler to detect removal of devices.
        ///  </summary>
        void AddDeviceRemovedHandler()
        {
            const Int32 pollingIntervalSeconds = 1;
            var scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;

            try
            {
                var q = new WqlEventQuery();
                q.EventClassName = "__InstanceDeletionEvent";
                q.WithinInterval = new TimeSpan(0, 0, pollingIntervalSeconds);
                q.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'";
                _deviceRemovedWatcher = new ManagementEventWatcher(scope, q);
                _deviceRemovedWatcher.EventArrived += DeviceRemoved;
                _deviceRemovedWatcher.Start();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                if (_deviceRemovedWatcher != null)
                    _deviceRemovedWatcher.Stop();
            }
        }

        ///  <summary>
        ///  Called on arrival of any device.
        ///  Calls a routine that searches to see if the desired device is present.
        ///  </summary>
        private void DeviceAdded(object sender, EventArrivedEventArgs e)
        {
            try
            {
                Debug.WriteLine("A USB device has been inserted");

                HWControls MyHwControls = new HWControls();
                bIsIntersonProbeDetected = MyHwControls.FindIntersonDevice();
                if (bIsIntersonProbeDetected)
                {
                    CheckProbes();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeviceAdded error");
                //DisplayException(Name, ex);
                throw;
            }
        }

        ///  <summary>
        ///  Add handlers to detect device arrival and removal.
        ///  </summary>
        internal void DeviceNotificationsStart()
        {
            AddDeviceArrivedHandler();
            AddDeviceRemovedHandler();
        }

        ///  <summary>
        ///  Stop receiving notifications about device arrival and removal
        ///  </summary>
        internal void DeviceNotificationsStop()
        {
            try
            {
                if (_deviceArrivedWatcher != null)
                    _deviceArrivedWatcher.Stop();
                if (_deviceRemovedWatcher != null)
                    _deviceRemovedWatcher.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeviceNotificationsStop error");

                //DisplayException(Name, ex);
                throw;
            }
        }

        ///  <summary>
        ///  Called on removal of any device.
        ///  Calls a routine that searches to see if the desired device is still present.
        ///  </summary>
        /// 
        private void DeviceRemoved(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("A USB device has been removed");
                HWControls MyHwControls = new HWControls();
                bIsIntersonProbeDetected = MyHwControls.FindIntersonDevice();
                CheckProbes();
                if (bIsIntersonProbeDetected == false)
                {
                    StopScan();//anyway there is no probe connected
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeviceRemoved error");
                //                DisplayException(Name, ex);
                 throw;
            }
        }

        /// <summary>
        /// Checks if the probe is currently connected to the computer
        /// </summary>
        void CheckProbes()
        {
            HWControls localHwControls = new HWControls();
            bool bconnected = false;

            bool bRestart = false;
            if ((Scan2D != null) && (Scan2D.ScanOn == true))// Stop Scan if running
            {
                StopThreadScan();
                bRestart = true;
            }

            if ((bRestart == true) && (bconnected == true))
                StartThreadScan();// Start Scan if the current is still connected
            else
            {
                StopScan();//the current probe has been disconnected so complete the stop
            }

            UpdateProbesButtons();
        }

        #endregion

        #region Robot De/Connect
        /// <summary>
        /// Watch the comPort motor connections:
        /// The timer is disabled when the motor is connected.
        /// The interval of the timer is 500ms
        /// Modelled after the USB probe watch timer
        /// A different timer runs when the motor is connected to listen for communications with the motor
        /// We use two different timers to allow for different time intervals
        /// </summary>

        static Boolean bRobotTimerConnect = false;
        static System.Windows.Forms.Timer robotTimerConnect = null;

        static int comPortQueried = -1;
        int numTicksSinceQuery = 0;
        static int MAX_TICKS_SINCE_QUERY = 10;

        List<String> comPortsList = new List<String>(4);

        /// <summary>
        /// To Start watching the SerialPort connections
        /// </summary>
        public void EnableRobotConnectTimer()
        {
            if (bRobotTimerConnect == false)
            {
                portTest();
                comPortQueried = -1;
                comPortsList.Clear();

                robotTimerConnect = new System.Windows.Forms.Timer();
                robotTimerConnect.Tick += new EventHandler(WatchRobotConnect);
                robotTimerConnect.Interval = 500;
                robotTimerConnect.Enabled = true;
                robotTimerConnect.Start();

                bRobotTimerConnect = true;
            }
        }

        /// <summary>
        /// To Stop watching the Serial Port connections
        /// </summary>
        public void DisableRobotConnectTimer()
        {
            bRobotTimerConnect = false;
            if (robotTimerConnect != null)
            {
                robotTimerConnect.Tick -= new EventHandler(WatchRobotConnect);
                robotTimerConnect.Stop();
                robotTimerConnect.Dispose();
                robotTimerConnect = null;
            }
        }

        /// <summary>
        /// Calls getComPortList to generate a list of comports
        /// </summary>
        void portTest()
        {
            if (!getComPortList())
            {
                return;
            }

            //foreach (string port in comPortsList)
            //{
            //    openComPort(port);
            //}
            return;
        }

        /// <summary>
        /// Watches for when the robot connects
        /// When the robot does connect, it enables the robot listener and disables the robot connect timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WatchRobotConnect(Object sender, EventArgs e)
        {


            if ((comPortQueried < 0) || (comPortQueried >= comPortsList.Count()))
            {
                // we aren't waiting for a response, so create a new list
                if (!getComPortList())
                    return;
            }
            else
            {
                // check for response 
                if (checkForQueryResponse())
                {
                    Debug.WriteLine("Got it!");

                    // we have it!
                    EnableRobotListener();
                    DisableRobotConnectTimer();
                    return;
                }

                ++numTicksSinceQuery;
                Debug.Write(' ' + numTicksSinceQuery);

                if (numTicksSinceQuery < MAX_TICKS_SINCE_QUERY)
                    return; // stick around a bit longer
            }

            // move on to next port 
            // proceed down the list until we find one that opens
            if (++comPortQueried < comPortsList.Count)
            {
                if (openComPort(comPortsList[comPortQueried]))
                {
                    RobotState = RobotStateEnum.unInitialized;
                    numTicksSinceQuery = 0;
                    Debug.WriteLine("Querying " + comPortsList[comPortQueried]);
                    Thread.Sleep(2000);
                    queryRobotState();
                    return;
                }
            }
        }

        /// <summary>
        /// Copy of robotListenerTimer_Tick but just accepts responses to the initial query
        /// </summary>
        /// <returns></returns>
        Boolean checkForQueryResponse()
        {
            Debug.WriteLine("\r\nQuerying");

            byte b = 0;
            try
            {
                int bytesRead = comPort.Read(ReceivedBytes, 0, 1);
                if (bytesRead == 0)
                    return false;

                b = ReceivedBytes[0];

                Debug.WriteLine("\n0  " + (char)b);

                if ((b & 0xF0) != 0x40)
                    return false;

                responseCodes.Add(b);
            }
            catch (Exception e)
            {
                return false;
            }

            responseCodes.Clear();
            responseCodes.Add(b);

            negStepCount = 0;
            posStepCount = 0;

            // ready to scan
            if (((b & 2) == 0))
            {
                RobotState = RobotStateEnum.homing;
            }
            else
            {
                RobotState = RobotStateEnum.emergencyStopped;
            }

            SetButtonForRobotState(RobotState);

            return true;
        }

        /// <summary>
        /// getComPortList
        /// hard-coding of 4, 5 and 7 as preferred comPorts
        /// workaround to prevent getting stuck on a virtual comPort
        /// this shouldn't be necessary now that we are querying the comport
        /// before accepting it as the correct on
        /// But no harm in keeping it in?
        /// </summary>
        static string[] preferredNames = {"COM1", "COM2", "COM3", "COM4", "COM5", "COM7", "COM8", "COM9", "COM10", "COM11", "COM12" };

        /// <summary>
        /// Adds strings of comport names to comPortsList
        /// Always returns true
        /// </summary>
        /// <returns></returns>
        Boolean getComPortList()
        {
            comPortsList.Clear();


            string[] ComPortsNames = System.IO.Ports.SerialPort.GetPortNames();

            for (int i = 0; i < preferredNames.Length; ++i)
            {
                string name = preferredNames[i];

                if (ComPortsNames.Contains(name))
                {
                    comPortsList.Add(name);
                }

            }

            for (int i = 0; i < ComPortsNames.Length; ++i)
            {
                string name = ComPortsNames[i];

                if (!comPortsList.Contains(name))
                {
                    comPortsList.Add(name);
                }
            }

            return true;
        }

        /// <summary>
        /// Opens a comport
        /// </summary>
        /// <param name="comPortName"></param>
        /// <returns></returns>
        Boolean openComPort(string comPortName)
        {
            if (comPort == null)
            {
                comPort = new SerialPort(comPortName, 38400, Parity.None, 8);
                comPort.ReadTimeout = 0;
            }

            try
            {
                comPort.PortName = comPortName;
                comPort.Open();

                Debug.WriteLine(comPortName);

                return true;
            }
            catch
            {
                // need to specifically ignore readTimeOut exception
            }

            return false;
        }
        #endregion

        #region ROBOT LISTENER
        /// <summary>
        /// Timer that listens for communications from the motor
        /// The timer is disabled when the motor is disconnected.
        /// The interval of the timer is 100ms
        /// A different timer runs when the motor is disconnected to listen for motor reconnection
        /// We use two different timers to allow for different time intervals
        /// </summary>

        static Boolean bRobotListenerTimer = false;
        static System.Windows.Forms.Timer robotListenerTimer = null;

        /// <summary>
        /// To Listen for communications from the robot
        /// </summary>
        public void EnableRobotListener()
        {
            if (bRobotListenerTimer == false)
            {
                robotListenerTimer = new System.Windows.Forms.Timer();
                robotListenerTimer.Tick += new EventHandler(RobotListenerTimer_Tick);
                robotListenerTimer.Interval = 5; //20
                robotListenerTimer.Enabled = true;
                robotListenerTimer.Start();
                bRobotListenerTimer = true;
            }
        }

        /// <summary>
        /// Stops Listening for communications from the robot
        /// </summary>
        public void DisableRobotListener()
        {
            bRobotListenerTimer = false;
            if (robotListenerTimer != null)
            {
                robotListenerTimer.Tick -= new EventHandler(RobotListenerTimer_Tick);
                robotListenerTimer.Stop();
                robotListenerTimer.Dispose();
                robotListenerTimer = null;
            }
        }

        /// <summary>
        /// Tells the scanner and robot what to do according to what the Arduino sends to the computer
        /// MaxSteps and CurSteps are counted here
        /// MaxCine is later set to MaxSteps, size of the ArrayList holding all of the images in a scan is set to MaxCine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RobotListenerTimer_Tick(Object sender, EventArgs e)
        {
            byte b = 0;

            try
            {
                int bytesRead = comPort.Read(ReceivedBytes, 0, 1);

                if (bytesRead != 0)
                {
                    b = ReceivedBytes[0];

                    if (b == 0x50)
                    {
                        if (RobotState == RobotStateEnum.homing && homeCountSteps)
                        {
                            ++maxSteps;
                        }
                        else if (RobotState == RobotStateEnum.scanning)
                        {
                            ++posStepCount;
                        }
                        else if (RobotState == RobotStateEnum.rewinding)
                        {
                            ++negStepCount;
                        }
                        curStepPos = posStepCount - negStepCount;
                        labelPosition.Text = curStepPos.ToString();
                        return;
                    }

                    if ((b & 0xF0) == 0x40)
                    {
                        responseCodes.Add(b);
                        // Debug.Write(" " + (char)b);
                    }
                    else
                    {
                        // Debug.Write("Don't Understand");
                        return;
                    }
                }
            }
            catch (TimeoutException)
            {
                // not an error
                return;
            }
            catch (InvalidOperationException ex)
            {
                // this means the port has been disconnected
                robotConnectionLost();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            //SerialPortReceiveTimerElapsedTime += SerialPortReceiveTimer.Interval;

            // ready to scan, recieved 0x4D
            if (b == 0x4D) //(b&2)
            {
                // ignore if the state isn't right
                if ((RobotState == RobotStateEnum.unInitialized) ||
                    (RobotState == RobotStateEnum.rewinding))
                {

                    responseCodes.Clear();
                    responseCodes.Add(b);
                    posStepCount = 0;
                    negStepCount = 0;
                    // Change back later 
                    labelPosition.Text = posStepCount.ToString();
                    RobotState = RobotStateEnum.readyToScan;
                    SetButtonForRobotState(RobotState);
                }
            }
            // completed scan, recieved 0x4B
            else if (b == 0x4B) // (b&4)
            {
                responseCodes.Add(b);

                writeResponseCodes();

                StopScan();

                RobotState = RobotStateEnum.endOfTravel;

                SetButtonForRobotState(RobotState);
            }
            // emergency stop, recieved 0x47
            else if (b == 0x47) //(b&8)
            {
                responseCodes.Add(b);

                writeResponseCodes();

                // StopScan();

                RobotState = RobotStateEnum.emergencyStopped;

                SetButtonForRobotState(RobotState);
            }
            //Homing pt 1, start counting the steps, received 0x42
            else if (b == 0x42)
            {
                homeCountSteps = true;
            }
            //Homing pt 2, stop counting the steps and store max steps, received 0x41
            else if (b == 0x41)
            {
                homeCountSteps = false;
                // Debug.Write(" max steps: " + maxSteps);
                RobotState = RobotStateEnum.rewinding;
                SetButtonForRobotState(RobotState);

                MaxCine = maxSteps;
                /// Cineloop
                ByteArrayList.Clear();
                ByteArrayList.Capacity = MaxCine;
                ushortArrayList.Clear();
                ushortArrayList.Capacity = MaxCine;
                ByteUniArrayList.Clear();
                ByteUniArrayList.Capacity = MaxCine;
                ushortUniArrayList.Clear();
                ushortUniArrayList.Capacity = MaxCine;
                //for UScanGuide Labeling? 
                cineImageTimes = new DateTime[MaxCine];
                cineStepCount = new int[MaxCine];

            }

            else if (RobotState == RobotStateEnum.unInitialized)
            {
                responseCodes.Add(b);

                RobotState = RobotStateEnum.emergencyStopped;

                SetButtonForRobotState(RobotState);
            }
        }

        /// <summary>
        /// Saves a log file of what the Arduino sends back to the computer
        /// </summary>
        private void writeResponseCodes()
        {
            FileStream writer = null;
            BinaryWriter bwrtFile;

            try
            {

                writer = new FileStream("ResponseCodes.txt", FileMode.OpenOrCreate);
                bwrtFile = new BinaryWriter(writer);

                for (int i = 0; i < responseCodes.Count; ++i)
                {
                    bwrtFile.Write(responseCodes[i]);
                }
            }
            catch (Exception eFile)
            {
                System.Windows.Forms.MessageBox.Show(eFile.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }
        }
        #endregion
                    
        #region ROBOT CONTROL COMMANDS

        /// <summary>
        /// Changes the circle indicator on the button "Robot Scan" when the state of the robot changes
        /// </summary>
        /// <param name="robotState"></param>
        void SetButtonForRobotState(RobotStateEnum robotState)
        {
            switch (robotState)
            {
                case RobotStateEnum.disConnected:
                    buttonRobotScan.Enabled = false;
                    labelRobotState.Text = "No Robot";
                    break;
                case RobotStateEnum.unInitialized:
                    labelRobotState.Text = "Uninitialized";
                    break;
                case RobotStateEnum.readyToScan:
                    buttonRobotScan.Enabled = true;
                    labelRobotState.Text = "Ready to Scan";
                    robotStateIndicator.Load("Images/readyToScan.png");
                    break;
                case RobotStateEnum.homing:
                    buttonRobotScan.Enabled = true;
                    labelRobotState.Text = "Homing";
                    robotStateIndicator.Load("Images/homing.png");
                    break;
                case RobotStateEnum.scanning:
                    buttonRobotScan.Enabled = true;
                    labelRobotState.Text = "Scanning";
                    robotStateIndicator.Load("Images/scanning.png");
                    break;
                case RobotStateEnum.endOfTravel:
                    buttonRobotScan.Enabled = true;
                    labelRobotState.Text = "End of Travel";
                    robotStateIndicator.Load("Images/endOfTravel.png");
                    break;
                case RobotStateEnum.emergencyStopped:
                    buttonRobotScan.Enabled = true;
                    labelRobotState.Text = "Emergency Stopped";
                    robotStateIndicator.Load("Images/emergencyStopped.png");
                    break;
                case RobotStateEnum.rewinding:
                    StartScan();
                    buttonRobotScan.Enabled = true;
                    labelRobotState.Text = "Rewinding";
                    robotStateIndicator.Load("Images/rewinding.png");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Sends a "l" to the Arduino to stop the robot
        /// </summary>
        private void StopRobot()
        {

            try
            {
                // stop the robot
                comPort.Write("l");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                robotConnectionLost();
                return;
            }

            Debug.WriteLine(" ");
        }

        /// <summary>
        /// Sends a "D" to the Arduino to check the robot's state
        /// </summary>
        private void queryRobotState()
        {
            try
            {
                comPort.Write("D");
            }
            catch (Exception ex)
            {
                // ignore or we will get the same error message every time this port is queried 
            }
        }

        /// <summary>
        /// Sends an "m" to the Arduino to start the robot
        /// </summary>
        private void StartRobot()
        {

            try
            {
                comPort.Write("m");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                robotConnectionLost();
                return;
            }
        }

        /// <summary>
        /// Rewinds the robot
        /// </summary>
        private void rewindRobot()
        {
            Debug.Write("Rewinding...");

            try
            {
                comPort.Write("n");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                robotConnectionLost();
                return;
            }
        }

        /// <summary>
        /// All the things we have to do when the robot disconnects
        /// But we are not shutting down the robot
        /// 
        /// Closes the comports
        /// Sets the robot state to disconnected
        /// Disables the robot listener
        /// </summary>
        private void robotConnectionLost()
        {

            if (RobotState == RobotStateEnum.disConnected)
                return;

            try
            {
                comPort.Dispose();
                comPort.Close();
            }
            catch (Exception ex)
            {

            }

            RobotState = RobotStateEnum.disConnected;

            MessageBox.Show("Robot Connection Lost");

            // disconnect and then reconnect of robot fails
            // possibly the comport needs flushing
            // will this effectively flush the comPort?
            comPort = null;

            DisableRobotListener();
            EnableRobotConnectTimer();

            SetButtonForRobotState(RobotState);
        }

        /// <summary>
        ///  Set motor speed to a percentage of maximum
        ///  Max speed is 70 rpm while scanning
        ///  RPM = 2 * Robot_Speed + 20 rpm
        /// </summary>
        private void setRobotSpeed(int percent)
        {
            // track bar reads speed as a percentage
            // motor expects a value 
            // Set Speed		0x80 .. 0xbf (bit 7 is set, bit 6 is clear, bits 5:0 represent the speed value)

            const int MAX_ROBOT_SPEED = 25;
            const int SET_BIT_7 = 128;

            int speed = (percent * MAX_ROBOT_SPEED) / 100;

            string binary = Convert.ToString(speed + SET_BIT_7, 2);
            Debug.Write(binary);

            try
            {
                // start the motor
                robotData[0] = (byte)(speed + SET_BIT_7);

                comPort.Write(robotData, 0, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                robotConnectionLost();
                return;
            }
        }
        #endregion

        #region SAVE ROBOT SCAN
        /// </summary>
        /// DoSave3DScan
        /// Saves all the images generated in a 3D scan as raw files
        /// Currently these are stored in the same array as the cine loop
        /// Ultimately may want to use a separate array?
        /// // Need to understanding processing of raw data....
        /// </summary>
        string baseFileName = "Scan_3d_";

        private void DoSaveRobotScan()
        {
            if (ByteArrayList.Count == 0)
                return;

            // prompt user for folder but create folder beneath with unique name
            // to make sure pre-existing stuff doesnt get overwritten

            // BUT scan cannot be saved twice to same folder
            // If we need this feature either append _a, _b, _c etc to folder name to make it unique
            // or use current time stamp (DateTime.Now) instead of time that scan was taken

            string timestamp = cineImageTimes[0].ToString("yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture);

            string folderName = baseFileName + timestamp;

            String datFilePath = null;
            String bmpPath = null;
            String jpgPath = null;
            String rawPath = null;

            // prompt user for file folder
            using (var fbd = new FolderBrowserDialog())
            {
                string savedPath = Settings.Default.SaveRobotScanFolder;
                if (!String.IsNullOrEmpty(savedPath))
                {
                    fbd.SelectedPath = savedPath;
                }

                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string path = fbd.SelectedPath + Path.DirectorySeparatorChar + folderName;

                    Directory.CreateDirectory(path);

                    datFilePath = path + Path.DirectorySeparatorChar + baseFileName + "_data.txt";

                    Settings.Default.SaveRobotScanFolder = fbd.SelectedPath;
                    Settings.Default.Save();

                    // check if scan has been saved to this folder
                    // Shouldnt happen now that we are appending timestamp to subfolder
                    if (File.Exists(datFilePath))
                    {
                        // what we need here is an OK / Cancel dialog box
                        // on OK we overwrite the folder, on Cancel we return
                        // better to just go back to the browser dialog
                        MessageBox.Show(" This scan has already been saved to this folder! Please choose another");
                        return;
                    }

                    bmpPath = path + Path.DirectorySeparatorChar + baseFileName + "_bmp";
                    jpgPath = path + Path.DirectorySeparatorChar + baseFileName + "_jpg";
                    rawPath = path + Path.DirectorySeparatorChar + baseFileName + "_raw";

                }
                else
                    return;
            }

            try
            {
                // create subfolders for *.bmp, *.jpeg, *.raw
                Directory.CreateDirectory(bmpPath);
                // Directory.CreateDirectory(jpgPath);
                // Directory.CreateDirectory(rawPath);

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            DoSaveRobotScanDataAsText(datFilePath);
            //Rita, need to see what the file names are.. check cineStepCount/cineImageTimes variables
            for (int i = 0; i < ByteArrayList.Count; ++i)
            {

                TimeSpan span = cineImageTimes[i] - cineImageTimes[0];

                string fileName = "Image_" + Convert.ToString(i) + '_' +
                                  Convert.ToString(cineStepCount[i]) + '_' +
                                  ((int)span.TotalMilliseconds).ToString() + ".raw";

                DoSaveRobotScanRaw(rawPath + '/' + fileName,
                                bmpPath + '/' + fileName,
                                jpgPath + '/' + fileName,
                                i);

            }
        }


        /// <summary>
        /// Saves a .txt file that has the values used for Depth, Frequency, High Voltage, Main Gain, Dynamic, Etc
        /// </summary>
        /// <param name="filePath"></param>
        void DoSaveRobotScanDataAsText(string filePath)
        {
            // Example #3: Write only some strings in an array to a file.
            // The using statement automatically flushes AND CLOSES the stream and calls 
            // IDisposable.Dispose on the stream object.
            // NOTE: do not use FileStream for text files because it writes bytes, but StreamWriter
            // encodes the output as text.
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(filePath))
            {
                file.WriteLine("Index " + iIndexSC);
                file.WriteLine("Depth " + iDepth);

                // Main button settings 
                String freq = Regex.Replace(labelFrequency.Text, "[^0-9.+-]", "");
                String highVolt = Regex.Replace(labelHighVolt.Text, "[^0-9.+-]", "");
                String focus = Regex.Replace(labelFocus.Text, "[^0-9.+-]", "");
                String mainGain = Regex.Replace(labelMainGain.Text, "[^0-9.+-]", "");
                String dynamic = Regex.Replace(labelDynamic.Text, "[^0-9.+-]", "");

                file.WriteLine("Frequency: " + freq);
                file.WriteLine("High_Voltage: " + highVolt);
                file.WriteLine("Focus: " + focus);
                file.WriteLine("Main_Gain: " + mainGain);
                file.WriteLine("Dynamic: " + dynamic);

                // TGC values
                file.WriteLine("TGC_Near: " + asbytTgcValue[0]);
                file.WriteLine("TGC_Middle: " + asbytTgcValue[1]);
                file.WriteLine("TGC_Far: " + asbytTgcValue[2]);

                // Misc settings
                file.WriteLine("Doubler: " + ScanConverter.Doubler);
                file.WriteLine("Compound: " + ScanConv.Compound);
                file.WriteLine("Compound_Angle: " + MyHwControls.CompoundAngle);
                file.WriteLine("Steering: " + iSteering);
                file.WriteLine("Number_of_Lines: " + uiNbOfLines);

                // Image description
                file.WriteLine("Probe_ID: " + HWControls.GetProbeID());
                file.WriteLine("Width: " + aiWidth[iIndexSC]);
                file.WriteLine("Height: " + aiHeigth[iIndexSC]);
                file.WriteLine("Number_Images: " + ByteArrayList.Count);

                // Sweep angle
                int stepsPerTick = 5;
                double degreesPerStep = 180.0 / 3102;
                double sweepAngle = maxSteps * stepsPerTick * degreesPerStep;
                int sweepAngleRounded = (int) Math.Round(sweepAngle);

                file.WriteLine("Sweep_Angle: " + sweepAngleRounded);

                // Radius in cm
                file.WriteLine("Radius: " + textRadius.Text);

            }

        }

        /// <summary>
        /// Save the raw data for one image of the 3D scan: 
        /// essentially copied from DoSaveRaw but with the .dat part pulled out
        /// and reads the image from the saved BytArray
        /// 
        /// Only used to call SaveBmpFromRaw
        /// </summary>
        /// <param name="fileName"></param>
        void DoSaveRobotScanRaw(string rawFileName, string bmpFileName, string jpgFileName, int imageIndex)
        {
            StreamWriter file = null;

            BinaryWriter bwrtFile = null;
            FileStream writer = null;
            int l = 0;

            try
            {

                //---- erase old File
                /*
                writer = new FileStream(rawFileName, FileMode.OpenOrCreate);
                bwrtFile = new BinaryWriter(writer);
                int max = (int)uiNbOfLines;

                int i, j;

                byte[,] rawImage = ByteArrayList[imageIndex];

                for (j = 0; j < ScanConverter.MAX_SAMPLES; j++)
                {
                    for (i = 0; i < max; i++)
                    {
                        bwrtFile.Write(rawImage[i, j]);
                        l++;
                    }
                }
                */

                SaveBmpFromRaw(imageIndex,
                               Path.ChangeExtension(bmpFileName, ".bmp"),
                               Path.ChangeExtension(jpgFileName, ".jpg"));

                // Compound is not used for the array probe
                /*
                if (ScanConv.Compound)
                {
                    for (j = 0; j < ScanConverter.MAX_SAMPLES; j++)
                    {
                        for (i = 0; i < max; i++)
                        {
                            bwrtFile.Write(bytRawImagePrevious[i, j]);
                            l++;
                        }
                    }
                }*/

            }
            catch (Exception eFile)
            {
                System.Windows.Forms.MessageBox.Show(eFile.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
                if (file != null)
                {
                    file.Close();
                }

            }
        }
        /// <summary>
        ///  copied from DrawOut to create the bmp from a raw file without changing the graphics
        /// </summary>
        /// <param name="slide"></param>
        /// <param name="fileName"></param>
        void SaveBmpFromRaw(int slide, String bmpFileName, String jpgFileName)
        {
            Graphics g = uctrlScan.CreateGraphics();
            Rectangle destRect = uctrlScan.ClientRectangle;
            Rectangle srcRect = new Rectangle(iZoomX, iZoomY, (int)(destRect.Width / fltZoomFactor), (int)(destRect.Height / fltZoomFactor));
            Bitmap bmpOut1 = new Bitmap(aiWidth[iIndexSC], aiHeigth[iIndexSC], System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            int inbData = 0;
            int imax = 0;

            if (bSoftRFData)
                imax = ScanConverter.MAX_RFSAMPLES;
            else
                imax = ScanConverter.MAX_SAMPLES;

            if (bIdle)
            {
                bmpOut1 = new Bitmap(aiWidth[iIdleIndexSC], aiHeigth[iIdleIndexSC], System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                inbData = (int)uiIdleNbOfLinesPerArray * imax;
            }
            else
                inbData = (int)uiNbOfLines * imax;

            if (bSoftRFData)
            {
                Array.Copy(ushortArrayList.ElementAt(slide), aushRawRF, inbData);
                BuildRFData(aushRawRF, ifactorRF);

                // TODO....
                // figure out how to modify this to draw the bitmap to a file....
                // draws to the screen in two different steps
                // are we going to be using this???
                // DrawCurve(0, 0, ifactorRF);
                /*
                Graphics g = uctrlScan.CreateGraphics();
                //Create a Bitmap object with the size of the panel
                Bitmap curBmp = new Bitmap(uctrlScan.Width, uctrlScan.Height / factor);
                //Create a temporary Graphics object from the bitmap
                Graphics gCur = Graphics.FromImage(curBmp);
                gCur.Clear(this.BackColor);
                //RF
                Pen penCurrent = new Pen(Color.Red);
                gCur.DrawCurve(penCurrent, aptRFdata);
                //Call DrawImage of Graphics and draw bitmap
                g.DrawImage(curBmp, x, y);

                //Dispose of objects
                gCur.Dispose();
                curBmp.Dispose();*/
            }
            else
            {

                Array.Copy(ByteArrayList.ElementAt(slide), bytRawImage, inbData);
                if (((ScanConv.Compound) && (bIdle == false))
                    || ((bIdleCompound == true) && (bIdle == true)))
                {
                    if (slide == 0)
                    {
                        Array.Copy(ByteArrayList.ElementAt(ByteArrayList.Count - 1), bytRawImagePrevious, inbData);

                    }
                    else
                    {
                        Array.Copy(ByteArrayList.ElementAt(slide - 1), bytRawImagePrevious, inbData);
                    }
                }


                // if used then to do it
                ApplyTgc(bytRawImage);
                ApplyTgc(bytRawImagePrevious);

                //if (bImgExt == true)
                //{
                //    imgControl.ApplyLUT(bytRawImage);    
                //    imgControl.ApplyLUT(bytRawImagePrevious);    
                //}
                ImageBuilding.Build2D(ref bmpOut1, bytRawImage, bytRawImagePrevious, ScanConv);// build 

                // christine should we be using previous image??
                // ImageBuilding.Build2D(ref bmpOut1, bytRawImage, null, ScanConv);// build

                // bmpOut1.Save(jpgFileName, ImageFormat.Jpeg);

                bmpOut1.Save(bmpFileName, ImageFormat.Bmp);
            }
        }
        #endregion

        #region ROBOT MANUAL
        //need code to update position

        private void butManFwd_MouseDown(object sender, MouseEventArgs e)
        {
            //Send byte to robot telling to go in reverse 
            //update button to manual
            StartRobot();
            RobotState = RobotStateEnum.scanning;
            SetButtonForRobotState(RobotState);

        }

        private void butManFwd_MouseUp(object sender, MouseEventArgs e)
        {

            //send byte to robot telling it to stop
            StopRobot();
            RobotState = RobotStateEnum.emergencyStopped;
            SetButtonForRobotState(RobotState);
            //update button to red rewind
        }
        private void butManRev_MouseDown(object sender, MouseEventArgs e)
        {
            rewindRobot();
            RobotState = RobotStateEnum.rewinding;
            SetButtonForRobotState(RobotState);
            //Send byte to robot telling to go in reverse 
            //update button to manual
        }

        private void butManRev_MouseUp(object sender, MouseEventArgs e)
        {
            StopRobot();
            RobotState = RobotStateEnum.emergencyStopped;
            SetButtonForRobotState(RobotState);
            //send byte to robot telling it to stop
            //update button to red rewind
        }


        #endregion

        #region SETTINGS
        private void getLastSettings()
        {
            string filename = Application.StartupPath + "/settings.txt";
            if (!File.Exists(filename)){
                return;
            }
            StreamReader reader = File.OpenText(filename);
            string line;
            try
            {
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split(' ');
                    int myInteger = int.Parse(items[1]); // Here's your integer.

                    switch (items[0])
                    {
                        case "Depth":
                            int last_depth = int.Parse(items[1]);
                            iDepth = last_depth;
                            RebuildScanConverter();
                            break;
                        case "Frequency":
                            byte last_ifreq = byte.Parse(items[1]);
                            bytFreqIndex = last_ifreq;
                            UpdateFrequencyAndFocus();
                            break;
                        case "High_Volt":
                            int last_iHV = int.Parse(items[1]);
                            iHighIndex = last_iHV;
                            HWControls.SendHighVoltage(abytHighVoltage[iHighIndex], abytHighVoltageCFM[iHighIndex]);
                            break;
                        case "Focus":
                            byte last_ifocus = byte.Parse(items[1]);
                            bytFocusIndex = last_ifocus;
                            UpdateFrequencyAndFocus();
                            break;
                        case "Main_Gain":
                            double last_dblMainGain = double.Parse(items[1]);
                            sbyte last_sbytMainGain = Convert.ToSByte(last_dblMainGain * 6.4);
                            if (last_sbytMainGain > DIG_GAIN_MAX - bytTgcStep)
                                return;
                            sbytMainGainValue = last_sbytMainGain;
                            TgcCurve();
                            break;
                        case "Dynamic_Gain":
                            int last_idyn = int.Parse(items[1]);
                            iDynamicIndex = last_idyn;
                            HWControls.SendDynamic(abytDynamicValue[iDynamicIndex]);
                            break;
                        case "Doubler":
                            bool last_doubler = bool.Parse(items[1]);
                            ScanConverter.Doubler = last_doubler;
                            SetDoubler();
                            break;
                        case "Compound":
                            bool last_compound = bool.Parse(items[1]);
                            ScanConv.Compound = last_compound;
                            SetCompound();
                            break;
                        case "Steering":
                            int last_isteer = int.Parse(items[1]);
                            iSteering = last_isteer;
                            RebuildAll();
                            break;
                        default: break;
                    }
                }
                UpdateLabels();
                return;
            }
            catch(Exception e)
            {
                return;
            }
        }


        private void formScan2D_FormClosing(object sender, FormClosingEventArgs e)
        { //save the settings to a file in same folder as the executable
            string filename = Application.StartupPath + "/settings.txt";
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
                {
                    file.WriteLine(" Depth " + iDepth.ToString());
                    file.WriteLine(" Frequency " + bytFreqIndex.ToString());
                    file.WriteLine(" High_Volt" + iHighIndex.ToString());
                    file.WriteLine(" Focus " + bytFocusIndex.ToString());
                    //gain
                    double dblMainGain = sbytMainGainValue / 6.4;
                    file.WriteLine(" Main_Gain " + dblMainGain.ToString("0.00"));
                    file.WriteLine(" Dynamic_Gain " + iDynamicIndex.ToString());

                    file.WriteLine(" Doubler " + ScanConverter.Doubler); //bool
                    file.WriteLine(" Compound " + ScanConv.Compound);
                    file.WriteLine(" Steering " + iSteering);
                }
            }
            catch(Exception a)
            {
                return;
            }
            
        } // end partial class formScan2D : Form
        #endregion

        #region EVENT HANDLERS

        private void TextRadius_MouseClick(object sender, MouseEventArgs e)
        {
            if (!hasBeenClicked)
            {
                TextBox box = sender as TextBox;
                box.Text = String.Empty;
                hasBeenClicked = true;
            }
        }

        private void TextRadius_Leave(object sender, EventArgs e)
        {
            String[] characters = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "." }; 

            TextBox box = sender as TextBox;
            String text = box.Text;

            if (!double.TryParse(text, out double n)) {
                box.Text = "Enter the Radius";
            }
        }

        private void MenuSave_Click(object sender, EventArgs e)
        {
            DoSaveRobotScan();
            StartScan();
        }

        private void MenuSaveScreenshot_Click(object sender, EventArgs e)
        {
            bool bRestart = false;
            if (Scan2D.ScanOn == true)
            {
                StopThreadScan();
                bRestart = true;
            }

            if (bCineOn == true)
                StopCineloop();


            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "BMP Files(*.bmp)|*.bmp|Jpeg Files(*.jpg)|*.jpg|Raw Files(*.raw)|*.raw";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                if (fileDialog.FileName.EndsWith("raw"))
                {
                    DoSaveRaw(fileDialog.FileName);
                }
                else
                {
                    Bitmap bmpSaveScan = DoSaveScan();
                    if (fileDialog.FileName.EndsWith("jpg"))
                        bmpSaveScan.Save(fileDialog.FileName, ImageFormat.Jpeg);
                    else if (fileDialog.FileName.EndsWith("bmp"))
                        bmpSaveScan.Save(fileDialog.FileName, ImageFormat.Bmp);
                    else
                        bmpSaveScan.Save(fileDialog.FileName);
                }
            }

            if (bRestart == true)
                StartThreadScan();
        }

        private void MenuLoad_Click(object sender, EventArgs e)
        {
            if (Scan2D.ScanOn == true)
            {
                StopThreadScan();
                MyMarshalToForm(ControlEnum.buttonScan, "Scan");
                bInitDone = false;

            }

            HideDisplayControls();

            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "BMP Files(*.bmp)|*.bmp|Jpeg Files(*.jpg)|*.jpg|Raw Files(*.raw)|*.raw";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                if (fileDialog.FileName.EndsWith("raw"))
                {
                    DoLoadRaw(fileDialog.FileName);
                    UpdateCineloopGroup(false);
                    Bitmap bmpImage = new Bitmap(aiWidth[iIdleIndexSC], aiHeigth[iIdleIndexSC]);
                    IntersonArray.Imaging.ScanConverter.ScanConverterError error = ScanConv.IdleInitScanConverter(iIdleDepth, aiWidth[iIdleIndexSC], aiHeigth[iIdleIndexSC], shIdleId, iIdleSteering, iIdleDepthCfmBox, bIdleDoubler, bIdleCompound, iIdleCompoundAngle, bSoftCFMData, ref ImageBuilding);

                    if ((error == IntersonArray.Imaging.ScanConverter.ScanConverterError.OVER_LIMITS) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.UNDER_LIMITS) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.PROBE_NOT_INITIALIZED) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.UNKNOWN_PROBE) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.WRONG_FORMAT) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.ERROR)
                        )
                    {
                        FormMessage formMessage = new FormMessage();
                        formMessage.ShowMessage((object)MessageClass.ScanConverterMessage);
                        formMessage.Dispose();
                    }
                    else if (bSoftRFData)
                    {
                        uctrlScan.Visible = true;
                        BuildRFData(aushRawRF, ifactorRF);
                        DoRefresh();
                    }
                    else
                    {

                        if (bSoftCFMData == false)
                            ImageBuilding.Build2D(ref bmpImage, bytRawImage, bytRawImagePrevious, ScanConv);// build 
                        else
                        {
                            ImageBuilding.Build2D(ref bmpImage, bytRawImage, abytRawCFM, ScanConv);
                            ImageBuilding.DrawCFMBox(ref bmpImage, ScanConv, iIdleDepthCfmBox, colBox);
                        }

                        bmpLoad = new Bitmap(aiWidth[iIdleIndexSC] + uctrlDepth.Width, aiHeigth[iIdleIndexSC]);
                        uctrlDepth.BuildDrawScale(null, iDepth, this.ScanConv, bIsUpDown, fltZoomFactor, iOffsetScale);
                        uctrlDepth.DrawToBitmap(bmpLoad, new Rectangle(0, 0, bmpLoad.Width, bmpLoad.Height));
                        uctrlDepth.Visible = true;
                        bIdle = true;
                        ResizeForm(iIdleIndexSC);

                        Graphics g = Graphics.FromImage(bmpLoad);
                        g.DrawImage(bmpImage, new Point(uctrlDepth.Width, iOffsetScale));
                        g.Dispose();
                        DoRefresh();

                    }
                    bmpImage.Dispose();

                }
                else
                {
                    bmpLoad = new Bitmap(aiWidth[iIndexSC] + uctrlDepth.Width, aiHeigth[iIndexSC]);
                    bmpLoad = (Bitmap)Bitmap.FromFile(fileDialog.FileName, false);
                }
                ResetLabels();
                labelDepth.Text = strDepth + iIdleDepth.ToString() + strMM;
                uctrlDepth.Visible = true;
                labelFileName.Text = fileDialog.FileName;
                DoRefresh();

            }
        }

        private void MenuDoubler_Click(object sender, EventArgs e)
        {
            if ((ScanConv.Compound) == true)
                return;
            ScanConverter.Doubler = !ScanConverter.Doubler;
            SetDoubler();
            RebuildAll();

            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item.Checked == true)
            {
                item.Checked = false;
            }
            else {
                item.Checked = true;
            }
        }

        private void MenuCompound_Click(object sender, EventArgs e)
        {
            if (ScanConverter.Doubler == true)
                return;

            (ScanConv.Compound) = !(ScanConv.Compound);

            SetCompound();
            RebuildAll();
        }

        private void PictureBoxIcon_Click(object sender, EventArgs e)
        {

        }

        private void MenuWebsite_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo("http://cccs-site.herokuapp.com/");
            Process.Start(sInfo);
        }

        private void LabelImaging_Click(object sender, EventArgs e)
        {

        }

        private void LabelFileName_Click(object sender, EventArgs e)
        {

        }

        private void LabelRobotCtrls_Click(object sender, EventArgs e)
        {

        }

        private void LabelSaveReview_Click(object sender, EventArgs e)
        {

        }

        private void LabelDynamic_Click(object sender, EventArgs e)
        {

        }

        private void LabelPosition_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Button Robot Scan
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void ProtoButtonRobotScan_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox) sender;
            image.Load("Images/Pressed/buttonRobotScan.png");
        }

        private void ProtoButtonRobotScan_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox) sender;
            image.Load("Images/Unpressed/buttonRobotScan.png");
        }

        private void ProtoButtonRobotScan_Click(object sender, EventArgs e)
        {
            switch (RobotState)
            {
                case RobotStateEnum.readyToScan:

                    RobotState = RobotStateEnum.scanning;
                    SetButtonForRobotState(RobotState);

                    // clear out old scans
                    ByteArrayList.Clear();

                    // Resets prevStepPos to the current position
                    prevStepPos = curStepPos;

                    StartRobot();
                    break;
                case RobotStateEnum.homing:
                    StopRobot();
                    RobotState = RobotStateEnum.emergencyStopped;
                    SetButtonForRobotState(RobotState);
                    break;

                case RobotStateEnum.endOfTravel:
                case RobotStateEnum.emergencyStopped:

                    RobotState = RobotStateEnum.rewinding;
                    SetButtonForRobotState(RobotState);

                    rewindRobot();
                    break;

                case RobotStateEnum.scanning:

                    RobotState = RobotStateEnum.emergencyStopped;

                    SetButtonForRobotState(RobotState);

                    StopRobot();
                    // StopScan();
                    break;
                case RobotStateEnum.rewinding:

                    RobotState = RobotStateEnum.emergencyStopped;
                    SetButtonForRobotState(RobotState);

                    // StopRobot();

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Button Reverse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProtoButManRev_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/butManRev.png");
            rewindRobot();
            RobotState = RobotStateEnum.rewinding;
            SetButtonForRobotState(RobotState);
        }

        private void ProtoButManRev_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/butManRev.png");
            StopRobot();
            RobotState = RobotStateEnum.emergencyStopped;
            SetButtonForRobotState(RobotState);
        }

        /// <summary>
        /// Button Forward
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProtoButManFwd_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/butManFwd.png");
            StartRobot();
            RobotState = RobotStateEnum.scanning;
            SetButtonForRobotState(RobotState);
        }

        private void ProtoButManFwd_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/butManFwd.png");
            StopRobot();
            RobotState = RobotStateEnum.emergencyStopped;
            SetButtonForRobotState(RobotState);
        }

        /// <summary>
        /// Button Load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProtoButtonLoad_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/buttonLoad.png");
        }

        private void ProtoButtonLoad_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/buttonLoad.png");
        }

        private void ProtoButtonLoad_Click(object sender, EventArgs e)
        {
            if (Scan2D.ScanOn == true)
            {
                StopThreadScan();
                MyMarshalToForm(ControlEnum.buttonScan, "Scan");
                bInitDone = false;

            }

            HideDisplayControls();

            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "BMP Files(*.bmp)|*.bmp|Jpeg Files(*.jpg)|*.jpg|Raw Files(*.raw)|*.raw";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                if (fileDialog.FileName.EndsWith("raw"))
                {
                    DoLoadRaw(fileDialog.FileName);
                    UpdateCineloopGroup(false);
                    Bitmap bmpImage = new Bitmap(aiWidth[iIdleIndexSC], aiHeigth[iIdleIndexSC]);
                    IntersonArray.Imaging.ScanConverter.ScanConverterError error = ScanConv.IdleInitScanConverter(iIdleDepth, aiWidth[iIdleIndexSC], aiHeigth[iIdleIndexSC], shIdleId, iIdleSteering, iIdleDepthCfmBox, bIdleDoubler, bIdleCompound, iIdleCompoundAngle, bSoftCFMData, ref ImageBuilding);

                    if ((error == IntersonArray.Imaging.ScanConverter.ScanConverterError.OVER_LIMITS) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.UNDER_LIMITS) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.PROBE_NOT_INITIALIZED) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.UNKNOWN_PROBE) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.WRONG_FORMAT) ||
                        (error == IntersonArray.Imaging.ScanConverter.ScanConverterError.ERROR)
                        )
                    {
                        FormMessage formMessage = new FormMessage();
                        formMessage.ShowMessage((object)MessageClass.ScanConverterMessage);
                        formMessage.Dispose();
                    }
                    else if (bSoftRFData)
                    {
                        uctrlScan.Visible = true;
                        BuildRFData(aushRawRF, ifactorRF);
                        DoRefresh();
                    }
                    else
                    {

                        if (bSoftCFMData == false)
                            ImageBuilding.Build2D(ref bmpImage, bytRawImage, bytRawImagePrevious, ScanConv);// build 
                        else
                        {
                            ImageBuilding.Build2D(ref bmpImage, bytRawImage, abytRawCFM, ScanConv);
                            ImageBuilding.DrawCFMBox(ref bmpImage, ScanConv, iIdleDepthCfmBox, colBox);
                        }

                        bmpLoad = new Bitmap(aiWidth[iIdleIndexSC] + uctrlDepth.Width, aiHeigth[iIdleIndexSC]);
                        uctrlDepth.BuildDrawScale(null, iDepth, this.ScanConv, bIsUpDown, fltZoomFactor, iOffsetScale);
                        uctrlDepth.DrawToBitmap(bmpLoad, new Rectangle(0, 0, bmpLoad.Width, bmpLoad.Height));
                        uctrlDepth.Visible = true;
                        bIdle = true;
                        ResizeForm(iIdleIndexSC);

                        Graphics g = Graphics.FromImage(bmpLoad);
                        g.DrawImage(bmpImage, new Point(uctrlDepth.Width, iOffsetScale));
                        g.Dispose();
                        DoRefresh();

                    }
                    bmpImage.Dispose();

                }
                else
                {
                    bmpLoad = new Bitmap(aiWidth[iIndexSC] + uctrlDepth.Width, aiHeigth[iIndexSC]);
                    bmpLoad = (Bitmap)Bitmap.FromFile(fileDialog.FileName, false);
                }
                ResetLabels();
                labelDepth.Text = strDepth + iIdleDepth.ToString() + strMM;
                uctrlDepth.Visible = true;
                labelFileName.Text = fileDialog.FileName;
                DoRefresh();

            }
        }

        /// <summary>
        /// Button Save Cine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void ProtoButtonSaveCine_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/buttonSaveCine.png");
        }

        private void ProtoButtonSaveCine_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/buttonSaveCine.png");
        }

        private void ProtoButtonSaveCine_Click(object sender, EventArgs e)
        {
            DoSaveRobotScan();
            StartScan();
        }

        /// <summary>
        /// Button Previous
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProtoButtonPrevious_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/buttonPrev.png");
        }

        private void ProtoButtonPrevious_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/buttonPrev.png");
        }

        private void ProtoButtonPrevious_Click(object sender, EventArgs e)
        {
            PreviousImage();
            protoTrackBarCine.Value = iCineCounter;
        }

        /// <summary>
        /// Button Cine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProtoButtonCine_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/buttonCine.png");
        }

        private void ProtoButtonCine_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/buttonCine.png");
        }

        private void ProtoButtonCine_Click(object sender, EventArgs e)
        {
            if (Scan2D.ScanOn == true)
                return;

            if (bCineOn == false)
            {
                ShowDisplayControls();
                StartCineloop();
                MyMarshalToForm(ControlEnum.buttonCine, "Pause");
            }
            else
            {
                StopCineloop();
                MyMarshalToForm(ControlEnum.buttonCine, "Play");
            }
        }

        private void ProtoButtonNext_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Pressed/buttonNext.png");
        }

        private void ProtoButtonNext_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox image = (PictureBox)sender;
            image.Load("Images/Unpressed/buttonNext.png");
        }

        private void ProtoButtonNext_Click(object sender, EventArgs e)
        {
            NextImage();
            protoTrackBarCine.Value = iCineCounter;
        }

        private void AnkleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            formFoot referenceFoot = new formFoot();
            referenceFoot.Show();
        }

        private void ProtoTBarTgc1_Scroll_1(object sender, EventArgs e)
        {
            if ((asbytTgcValue[0] >= DIG_GAIN_MAX)
               || (asbytTgcValue[0] <= DIG_GAIN_MIN))
                return;
            asbytTgcValue[0] = (sbyte)(protoTBarTgc1.Value * bytTgcStep);
            TgcCurve();
        }

        private void ProtoTBarTgc2_Scroll_1(object sender, EventArgs e)
        {
            if ((asbytTgcValue[1] >= DIG_GAIN_MAX)
                          || (asbytTgcValue[1] <= DIG_GAIN_MIN))
                return;
            asbytTgcValue[1] = (sbyte)(protoTBarTgc2.Value * bytTgcStep);
            TgcCurve();
        }

        private void ProtoTBarTgc3_Scroll_1(object sender, EventArgs e)
        {
            if ((asbytTgcValue[2] >= DIG_GAIN_MAX)
                           || (asbytTgcValue[2] <= DIG_GAIN_MIN))
                return;
            asbytTgcValue[2] = (sbyte)(protoTBarTgc3.Value * bytTgcStep);
            TgcCurve();
        }

        private void ProtoTrackBarRobotSpeed_Scroll(object sender, EventArgs e)
        {
            double speed = protoTrackBarRobotSpeed.Value;
            setRobotSpeed((int)speed);
        }

        private void ProtoTrackBarCine_Scroll(object sender, EventArgs e)
        {
            StopCineloop();
            iCineCounter = protoTrackBarCine.Value;
            CineloopSlide();
        }
    }
    #endregion
}///namespace SDK_EXAMPLE