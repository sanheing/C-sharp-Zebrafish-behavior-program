using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;  // for ZedGrap
using System.Windows.Threading;
using System.Drawing;
using System.Threading;
using System.Windows.Forms.Integration;
using System.ComponentModel;
using Application = System.Windows.Forms.Application;
using System.Drawing;
using System.Data;


namespace BehaveAndScanGEVI_optovin
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {

        public Window BaseWindow;

        public System.Windows.Forms.Panel panelProjector;
        public bool writeOnEP    = false;
        public bool triggerStart = false;

        public bool centerOnPoint = false;
        public int centerOnPointNumber = 0;
        public bool StimulusON = false;

        public bool DurationStopE = false;
        public double CommonDuration ;
        public int recStackSPS;
        public double actSPSv;
        public bool openprobe = false;
        
        public StimParams InstStimParams = new StimParams();
        public switchParams switchParamsAuto = new switchParams();
        public oscParams oscParam = new oscParams();
        public oscULValues oscULValue = new oscULValues();

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        double[] switchParamsPreset;

        public string fileNameEP;       

        public StimEphysOscilloscopeControl stimSession;

        public bool gameMode = false;
        public bool displayObstacles = true;
        public bool displayWalls = false;
        public bool displayFish = false;
        public bool StationaryObstacles = true;
        public bool fixedPosition = false;
        public static string ObsDur = "6";
        public static int FixInterOrDur = 1;
        public float fixed_pos = 2.5f;
        public float xLoc, yLoc, recorded_xLoc, recorded_yLoc = 0f;
        

        public double vObstacle = 0;
        public double vBackground = 0.3;

        public float xOffset = 0;
        public float yOffset = 0;
        public float ObsYpos = 0;

        public double leftGain = 0.05;
        public double rightGain = 0.05;

        public int leftChannel = 1;//2022
        public int rightChannel = 0;//2022

        public float horThresh = 0;
        public double JitterAmplitude = 0;
        public int trialnumber = 0; //20210819
        public int CurrObstacleDur = 0; //20230322
        public int RecordTime = 0;

        // Optogenetic component
        /*--------------------------------------*/
        public slmParams slmParam = new slmParams();
        public bool OptoStimOn = false;
        public int camChan = 2;
        public int piezoChan = 3;

        public int triggerMode = 1;
        public int imagingMode = 2;

        public int viInterval = 5;
        public int viFPS = 20;
        public int viStimDelay = 0;
        public int spDelayTime = 30;
        public int spRepeat = 100;
        public int spInterval = 2;
        public double currTrialgoingT = 0;
        public int stimTrialT = 5;
        public int block1Len, block2Len, block3Len = 20;
        static public DataTable trialInfoTable = new DataTable();
        public int addTrialLen = 20;
        public bool addOptoStim = false;
        public int addStimPattern = 1;
        public int blockNum = 3;
        /*---------------------------------------*/

        public Window1()
        {
             InitializeComponent();            
        }

        private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BaseWindow = this;
            InstStimParams.clmode = 4;
            InstStimParams.moveobject = 1;
            InstStimParams.autoTime1 = 20;
            InstStimParams.autoTime2 = 0;//2022
            InstStimParams.autoTimeSum = InstStimParams.autoTime1 + InstStimParams.autoTime2;
            InstStimParams.gwidth = 0.7;
            InstStimParams.sps = 300;
            InstStimParams.stimSequence = new float[70];

            InstStimParams.autoVel1 = 0.5;
            InstStimParams.autoVel2 = 0.5;
            InstStimParams.autoGain1 = 0.03;//2022
            InstStimParams.autoGain2 = 0.02;
            trialInfoTable.Columns.Add("blockNum", typeof(int));
            trialInfoTable.Columns.Add("trialLen", typeof(int));
            trialInfoTable.Columns.Add("optoStim", typeof(bool));
            trialInfoTable.Columns.Add("stimPattern", typeof(int));
            trialInfoTable.Columns.Add("obsdur", typeof(string));
            trialInfoTable.Columns.Add("intdur", typeof(string));
            DataRow row1 = trialInfoTable.NewRow();
            row1["blockNum"] = 1;  row1["trialLen"] = 20; row1["optoStim"] = false; row1["stimPattern"] = 0;row1["obsdur"] = "6"; row1["intdur"] = "30";
            DataRow row2 = trialInfoTable.NewRow();
            row2["blockNum"] = 2; row2["trialLen"] = 20; row2["optoStim"] = true; row2["stimPattern"] = 1; row2["obsdur"] = "6"; row2["intdur"] = "30";
            DataRow row3 = trialInfoTable.NewRow();
            row3["blockNum"] = 3; row3["trialLen"] = 20; row3["optoStim"] = true; row3["stimPattern"] = 2; row3["obsdur"] = "6"; row3["intdur"] = "30";
            trialInfoTable.Rows.Add(row1);
            trialInfoTable.Rows.Add(row2);
            trialInfoTable.Rows.Add(row3);
            trialInfo.DataContext = trialInfoTable;

            for (int k = 0; k < 70; k++)
            {
                InstStimParams.stimSequence[k] = 1;
            }


            init_struct_arrays();

            fileNameEP = Application.StartupPath;
            commonFileName.Text = fileNameEP;
 
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(33);
            dispatcherTimer.Start();
            
        }

        private void init_struct_arrays()
        {
            switchParamsAuto = new switchParams();
            switchParamsAuto.phasedur = new double[6];
            switchParamsAuto.phasedurinc = new double[6];
            switchParamsAuto.test_params = new double[3];
            switchParamsAuto.test_durinc = new double[3];
            switchParamsAuto.stopmode = 1;
            switchParamsAuto.cycles = 20;

            switchParamsPreset = new double[6];
            switchParamsPreset[0] = 30;
            switchParamsPreset[1] = 10;
            switchParamsPreset[2] = 5;
            switchParamsPreset[3] = 30;
            switchParamsPreset[4] = 10;
            switchParamsPreset[5] = 5;

            switchParamsAuto.phasedur[0] = switchParamsPreset[0];
            switchParamsAuto.phasedur[1] = switchParamsPreset[1];
            switchParamsAuto.phasedur[2] = switchParamsPreset[2];
            switchParamsAuto.phasedur[3] = switchParamsPreset[3];
            switchParamsAuto.phasedur[4] = switchParamsPreset[4];
            switchParamsAuto.phasedur[5] = switchParamsPreset[5];

            switchParamsAuto.test_params[0] = 7;
            switchParamsAuto.test_params[1] = 15;
            switchParamsAuto.test_params[2] = 30;
            switchParamsAuto.test_durinc[0] = 0;
            switchParamsAuto.test_durinc[1] = 0;
            switchParamsAuto.test_durinc[2] = 0;

            CommonDuration = (switchParamsAuto.cycles * switchParamsAuto.phasedurinc[5])+1;

            switchParamsAuto.stopmode = 1;
            
            oscParam.oscMode = 1;
            oscParam.threshScale = new double[5];
            oscParam.threshScale[0] = 2.5;
            oscParam.threshScale[1] = 2.5;
            oscParam.threshScale[2] = 2.5;
            oscParam.threshScale[3] = 2.5;
            oscParam.threshScale[4] = 2.5;
            oscParam.axisValue = 0.5;

            oscULValue.ch1UpLim = 0;
            oscULValue.ch1LoLim = 0;
            oscULValue.ch2UpLim = 0;
            oscULValue.ch2LoLim = 0;
            oscULValue.ch3UpLim = 0;
            oscULValue.ch3LoLim = 0;
            oscULValue.ch4UpLim = 0;
            oscULValue.ch4LoLim = 0;
            /*---------------------------------------*/
            oscULValue.ch5UpLim = 0;
            oscULValue.ch5LoLim = 0;
            /*---------------------------------------*/
            // Optogenetic component
            /*---------------------------------------*/
            slmParam.slmScreen = 0;
            slmParam.slmSeqLen = 500;
            slmParam.slmOnSeg = 50;
            slmParam.slmOffSeg = 50;
            slmParam.slmPath = "\\\\10.10.49.10\\d\\SHY\\OptoStimPattern\\";
            /*---------------------------------------*/

        }


        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            gain1Check.IsChecked = StimEphysOscilloscopeControl.switchDisp.gain1check;
            gain2Check.IsChecked = StimEphysOscilloscopeControl.switchDisp.gain2check;
            FrameNumDisp.Content = StimEphysOscilloscopeControl.switchDisp.frameNum.ToString("0");
            StackNumDisp.Content = StimEphysOscilloscopeControl.switchDisp.stackNum.ToString("0");
            TrialNumDisp.Content = Math.Ceiling((trialnumber+1)/2f);
            CurrTrialOngoingT.Content = currTrialgoingT;
            RecTime.Content = RecordTime;
            TrialLen.Content = CurrObstacleDur;
            CurrBlock.Content = StimEphysOscilloscopeControl.currBlock+1;
            //GainLabel.Content = StimEphysOscilloscopeControl.stimParam3;
            //TrigLabel.Content = StimEphysOscilloscopeControl.gainTriggerSwitch;
            commonFileName.IsEnabled = StimEphysOscilloscopeControl.FileNameEnabled;
            OptoOngoing.Content = StimEphysOscilloscopeControl.optoOngoing;
            //RecTime.Content = StimEphysOscilloscopeControl.recEPtime.ToString("0");
            //RecTimeSecLabel.Content = "Seconds";
            //RecSec.Content = "Seconds";


            if ( StimEphysOscilloscopeControl.triggerGo )
            {                
                actSPSv = (double)StimEphysOscilloscopeControl.switchDisp.stackNum / (double) StimEphysOscilloscopeControl.recEPtime;
                ActSPS.Content =actSPSv.ToString("0.0");
            }
            

            ch1UpLim.Content = oscULValue.ch1UpLim.ToString("N5");
            ch1LoLim.Content = oscULValue.ch1LoLim.ToString("N5");
            ch2UpLim.Content = oscULValue.ch2UpLim.ToString("N5");
            ch2LoLim.Content = oscULValue.ch2LoLim.ToString("N5");
            ch3UpLim.Content = oscULValue.ch3UpLim.ToString("N5");
            ch3LoLim.Content = oscULValue.ch3LoLim.ToString("N5");
            ch4UpLim.Content = oscULValue.ch4UpLim.ToString("N5");
            ch4LoLim.Content = oscULValue.ch4LoLim.ToString("N5");
            /*-----------------------------------------------------*/
            ch5UpLim.Content = oscULValue.ch5UpLim.ToString("N5");
            ch5LoLim.Content = oscULValue.ch5LoLim.ToString("N5");
            /*----------------------------------------------------*/
            if (InstStimParams.clmode == 10)
            {
                RightGain.Text = rightGain.ToString();
            }
            
            //if (InstStimParams.clmode != 111) { InstStimParams.recordPlayback_special = false; }

        }    



             

        private void buttonStartStimEphys_Click(object sender, RoutedEventArgs e)
        {
            if (!StimulusON)//stimulus == null)
            {
                stimSession = new StimEphysOscilloscopeControl(this);
                Thread scroll = new Thread(new ParameterizedThreadStart(stimSession.StartEphys));
                scroll.Start(this);
                
                commonFileName.IsEnabled = false;
                buttonStartStimEphys.IsEnabled = false;
                buttonStopStimEphys.IsEnabled = true;
                sps_box.IsEnabled = false;
                StimulusON = true;
                /*---------------------------------------*/
                SLMPath.IsEnabled = false;
                /*---------------------------------------*/
            }
        }

        private void buttonStopStimEphys_Click(object sender, RoutedEventArgs e)
        {
            InstStimParams.stopStimEphys = true;
            buttonStartStimEphys.IsEnabled = true;
            buttonStopStimEphys.IsEnabled = false;
            Triggered.IsEnabled = true; 
            sps_box.IsEnabled = true;
            StimulusON = false;
            /*---------------------------------------*/
            SLMPath.IsEnabled = true;
            /*---------------------------------------*/
        }

        private void writeFileBox_Changed(object sender, RoutedEventArgs e)
        {
            if (writeFileBox.IsChecked == true)
            {
                trialnumber = 0; //20210819
            }
            writeOnEP = (writeFileBox.IsChecked == true);
            Triggered.IsEnabled = !(writeFileBox.IsChecked == true);
            
        }

        private void Triggered_Click(object sender, RoutedEventArgs e)
        {
            triggerStart = (Triggered.IsChecked == true);
        }


        private void autoGain1Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(autoGain1Box.Text, out InstStimParams.autoGain1);
        }


        private void autoVel1Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(autoVel1Box.Text, out InstStimParams.autoVel1);
        }


        private void autoGain2Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(autoGain2Box.Text, out InstStimParams.autoGain2);
        }


        private void autoVel2Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(autoVel2Box.Text, out InstStimParams.autoVel2);
        }

        
        private void velZeroBox_Changed(object sender, RoutedEventArgs e)
        {
            InstStimParams.closedLoop1DzeroVel = (velZeroBox.IsChecked == true);
        }



        private void autoGainSwitchSimpleRadio_Changed(object sender, RoutedEventArgs e)
        {
            if (autoGainSwitchSimpleRadio.IsChecked == true)
            {
                Reset_Location();
                InstStimParams.clmode = 4;
            }
        }



        private void Oritune_Click(object sender, RoutedEventArgs e)
        {
            if (Oritune.IsChecked == true)
                Reset_Location();
                InstStimParams.clmode = 6;
                switchParamsAuto.stopmode = 3;
                CommonDuration = (720) + 1;
                DurationEBox.Text = CommonDuration.ToString("0");
        }



        private void HorizontalBalance_Changed(object sender, RoutedEventArgs e)
        {
            if (HorizontalBalance.IsChecked == true)
            {
                Reset_Location();
                RightGain.IsEnabled = false;
                InstStimParams.clmode = 10;
            }
            else
            {
                RightGain.IsEnabled = true;
            }
        }




        private void DurationStopECheck_Click(object sender, RoutedEventArgs e)
        {
            DurationStopE = (DurationStopECheck.IsChecked == true);
            if (DurationStopE)
            {
                DurationEBox.IsEnabled = true;
            }
            else
            {
                DurationEBox.IsEnabled = false;
            }
        }       

     

        private void DurationE_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DurationStopE)
            {
                double.TryParse(DurationEBox.Text, out CommonDuration);
            }
        }
        

        





        private void Grating_Click(object sender, RoutedEventArgs e)
        {
            InstStimParams.moveobject = 1;
        }

        private void Grid_Click(object sender, RoutedEventArgs e)
        {
            InstStimParams.moveobject = 2;
        }

        private void Texture_Click(object sender, RoutedEventArgs e)
        {
            InstStimParams.moveobject = 3;
        }

        private void Pure_Click(object sender, RoutedEventArgs e)
        {
            InstStimParams.moveobject = 4;
        }

        private void oscMode1Butt_Click(object sender, RoutedEventArgs e)
        {
            if (oscMode1Butt.IsChecked == true)
                oscParam.oscMode = 1;
        }

        private void oscMode2Butt_Click(object sender, RoutedEventArgs e)
        {
            if (oscMode2Butt.IsChecked == true)
                oscParam.oscMode = 2;
        }

        private void oscMode3Butt_Click(object sender, RoutedEventArgs e)
        {
            if (oscMode3Butt.IsChecked == true)
                oscParam.oscMode = 3;
        }

        private void oscMode4Butt_Click(object sender, RoutedEventArgs e)
        {
            if (oscMode4Butt.IsChecked == true)
                oscParam.oscMode = 4;
        }

        private void gHeight_changed(object sender, RoutedEventArgs e)
        {
            if (oscParam.axisValue != 0)
                double.TryParse(gHeight.Text, out oscParam.axisValue);
        }

        private void Ch1_L(object sender, RoutedEventArgs e)
        {
            if (Ch1LeftSelect.IsChecked == true)
                leftChannel = 0;
        }

        private void Ch2_L(object sender, RoutedEventArgs e)
        {
            if (Ch2LeftSelect.IsChecked == true)
                leftChannel = 1;
        }

        private void Ch3_L(object sender, RoutedEventArgs e)
        {
            if (Ch3LeftSelect.IsChecked == true)
                leftChannel = 2;
        }

        private void Ch4_L(object sender, RoutedEventArgs e)
        {
            if (Ch4LeftSelect.IsChecked == true)
                leftChannel = 3;
        }

        private void Ch1_R(object sender, RoutedEventArgs e)
        {
            if (Ch1RightSelect.IsChecked == true)
                rightChannel = 0;
        }

        private void Ch2_R(object sender, RoutedEventArgs e)
        {
            if (Ch2RightSelect.IsChecked == true)
                rightChannel = 1;
        }

        private void Ch3_R(object sender, RoutedEventArgs e)
        {
            if (Ch3RightSelect.IsChecked == true)
                rightChannel = 2;
        }

        private void Ch4_R(object sender, RoutedEventArgs e)
        {
            if (Ch4RightSelect.IsChecked == true)
                rightChannel = 3;
        }

        private void threshCh1Scale_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (oscParam.threshScale != null) 
                double.TryParse(threshCh1Scale.Text, out oscParam.threshScale[0]);
        }

        private void threshCh2Scale_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (oscParam.threshScale != null)
                double.TryParse(threshCh2Scale.Text, out oscParam.threshScale[1]);
        }

        private void threshCh3Scale_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (oscParam.threshScale != null)
                double.TryParse(threshCh3Scale.Text, out oscParam.threshScale[2]);
        }

        private void threshCh4Scale_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (oscParam.threshScale != null)
                double.TryParse(threshCh4Scale.Text, out oscParam.threshScale[3]);
        }

        private void commonFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            fileNameEP = commonFileName.Text;
        }

        private void horizontalThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (oscParam.threshScale != null)
                float.TryParse(horizontalThreshold.Text, out horThresh);
        }


        private void Gain1Dur_TextChanged(object sender, TextChangedEventArgs e)
        {
                double.TryParse(Gain1Dur.Text, out InstStimParams.autoTime1);
                InstStimParams.autoTimeSum = InstStimParams.autoTime1 + InstStimParams.autoTime2;
        }

        private void Gain2Dur_TextChanged(object sender, TextChangedEventArgs e)
        {
                double.TryParse(Gain2Dur.Text, out InstStimParams.autoTime2);
                InstStimParams.autoTimeSum = InstStimParams.autoTime1 + InstStimParams.autoTime2;
        }

        private void sps_box_TextChanged(object sender, TextChangedEventArgs e)
        {
                int.TryParse(sps_box.Text, out InstStimParams.sps);

        }



        private void GratingWidthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(GratingWidthBox.Text, out InstStimParams.gwidth);
        }



        private void GameMode_Click(object sender, RoutedEventArgs e)
        {
            gameMode = (bool)gameModeActivate.IsChecked;
        }

        private void Obstacle_Check(object sender, RoutedEventArgs e)
        {
            displayObstacles = true;
            Reset_Location();
        }

        private void Obstacle_Uncheck(object sender, RoutedEventArgs e)
        {
            displayObstacles = false;
            Reset_Location();
        }

        private void Fish_Check(object sender, RoutedEventArgs e)
        {
            displayFish = true;
        }

        private void Fish_Uncheck(object sender, RoutedEventArgs e)
        {
            displayFish = false;
        }


        private void Wall_Check(object sender, RoutedEventArgs e)
        {
            displayWalls = true;
            Reset_Location();
        }

        private void Wall_Uncheck(object sender, RoutedEventArgs e)
        {
            displayWalls = false;
            Reset_Location();
        }

        private void Reset_Location()
        {
            xLoc = 0;
            yLoc = 0;
        }

        private void Fixed_Click(object sender, RoutedEventArgs e)
        {
            if (fixedLocation.IsChecked == true)
            {
                fixedPosition = true;
            }
        }

        private void Random_Click(object sender, RoutedEventArgs e)
        {
            if (randomLocation.IsChecked == true)
            {
                fixedPosition = false;
            }
        }


        private void ObstacleLoc_TextChanged(object sender, TextChangedEventArgs e)
        {
            float.TryParse(ObstacleLoc.Text, out fixed_pos);
        }

        private void ObstacleVel_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(ObstacleVel.Text, out vObstacle);
        }

        private void ObstacleAmp_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(ObstacleAmp.Text, out JitterAmplitude);
        }

        private void BackgroundVel_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(BackgroundVel.Text, out vBackground);
        }

        private void XOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            float.TryParse(XOffset.Text, out xOffset);
        }

        private void FileNameTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void StatObstacle_Uncheck(object sender, RoutedEventArgs e)
        {
            StationaryObstacles = false;
            Reset_Location();
        }

        private void StatObstacle_Check(object sender, RoutedEventArgs e)
        {
            StationaryObstacles = true;
            Reset_Location();
        }

        //private void FixInter_Click(object sender, RoutedEventArgs e)
        //{
        //    FixInterOrDur = 1;
        //}

        //private void FixDur_Click(object sender, RoutedEventArgs e)
        //{
        //    FixInterOrDur = 2;
        //}

        private void ObsYpos_TextChanged(object sender, TextChangedEventArgs e)
        {
            float.TryParse(ObstacleYpos.Text, out ObsYpos);
        }

        private void YOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            float.TryParse(YOffset.Text, out yOffset);
        }      
        

        private void LeftGain_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(LeftGain.Text, out leftGain);
        }

        private void RightGain_TextChanged(object sender, TextChangedEventArgs e)
        {
            double.TryParse(RightGain.Text, out rightGain);
        }

        //private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    ObsDur = ObsDuration.Text;
        //}
        /*---------------------------------------------------------------------*/
        private void OptoStimOn_Checked(object sender, RoutedEventArgs e)
        {
            OptoStimOn = true;
        }

        private void SLMScreen_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(SLMScreen.Text, out slmParam.slmScreen);
        }

        private void CamChannel_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(CamChannel.Text, out camChan);
        }

        private void PiezoChannel_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(PiezoChannel.Text, out piezoChan);
        }

        private void SLMOnSeg_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(SLMOnSeg.Text, out slmParam.slmOnSeg);
        }

        private void SLMOffSeg_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(SLMOffSeg.Text, out slmParam.slmOffSeg);
        }

        private void SLMSeqLen_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(SLMSeqLen.Text, out slmParam.slmSeqLen);
        }

        private void SLMPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            slmParam.slmPath = SLMPath.Text;
        }

        private void PiezoTrigger_Click(object sender, RoutedEventArgs e)
        {
            triggerMode = 1;
        }

        private void AddBlock(object sender, RoutedEventArgs e)
        {
            blockNum++;
            DataRow row = trialInfoTable.NewRow();
            row["blockNum"] = blockNum; row["trialLen"] = addTrialLen; row["optoStim"] = addOptoStim; row["stimPattern"] = addStimPattern;
            row["obsdur"] = OD.Text; row["intdur"] = ID.Text;
            trialInfoTable.Rows.Add(row);
        }

        private void TL_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(TL.Text,out addTrialLen);
        }

        private void SP_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(SP.Text, out addStimPattern);
        }

        private void optoStim_Checked(object sender, RoutedEventArgs e)
        {
            addOptoStim = true;
        }

        private void optoStim_unChecked(object sender, RoutedEventArgs e)
        {
            addOptoStim = false;
        }

        private void OptoStimOn_unChecked(object sender, RoutedEventArgs e)
        {
            OptoStimOn = false;
        }

        private void DeleteBlock(object sender, RoutedEventArgs e)
        {
            DataRowView selectedItem = (DataRowView)trialInfo.SelectedItem;
            if (selectedItem!=null)
            {
                DataRow selectedRow = selectedItem.Row;
                int rowIndex = (int)selectedRow["blockNum"]-1;
                for(int i=rowIndex;i<blockNum;i++)
                {
                    DataRow row = trialInfoTable.Rows[i];
                    row["blockNum"] = (int)row["blockNum"] - 1;
                }
                DataRow selectedDataTableRow = trialInfoTable.Rows[rowIndex];
                trialInfoTable.Rows.Remove(selectedDataTableRow);
                blockNum--;
            }
        }

        private void CamTrigger_Click(object sender, RoutedEventArgs e)
        {
            triggerMode = 2;
        }

        private void StimInTrialT_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(StimInTrialT.Text, out stimTrialT);
        }

        private void VIInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(VIInterval.Text, out viInterval);
        }

        private void VIfps_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(VIfps.Text, out viFPS);
        }

        private void VIStimDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(VIStimDelay.Text, out viStimDelay);
        }
        /*---------------------------------------------------------------------*/

    }
}
