using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using NationalInstruments.DAQmx;


namespace BehaveAndScanGECI
{
    public partial class StimEphysOscilloscopeControl
    {
        public Window1 senderWindow = null;
        private StimWindow stimulus1 = null;

        /*-----------------------------*/
        public OptoWindow opto1 = null;
        /*-----------------------------*/

        private Oscilloscope oscilloscope = null;
        public static switchDisps switchDisp = new switchDisps();
        public static bool FileNameEnabled = true;
        public static bool triggerGo = false;

        int sampleRate = 6000;   // 60 Hz projector, 100 samples per frame // appears to be more like 30 Hz, don't know why
        public Task EphysInputTask, LEDTask;
        public AnalogMultiChannelReader EphysReader;
        public AsyncCallback EphysInputCallback;

        public BinaryWriter writeFileStreamEP;
        FileStream fs_ephys;

        public double displayAngle = Math.PI;
        public double moveAngle = 0;

        public float stripesX = 0;
        public float stripesY = 0;
        public int GratingsContrast = 140;
        public int ObstacleContrast = 1;
        int oscMode = 1;  // oscilloscope mode

        double[,] buffData = new double[5, 6000];
        double[,] readData = new double[4, 50];
        double[,] filtData_ = new double[4, 600 - (int)(60 / 2.0 / 10.0)];
        double[,] filtData = new double[4, 600 - (int)(60 / 2.0 / 10.0)];
        uint[,] histogram = new uint[4, 1500];
        int[] histMxPos = new int[4];
        int[] histMnPos = new int[4];
        double[] thresh = new double[4];
        double[] mean_ = new double[4];
        double[] min_ = new double[4];
        int[] div = new int[4];
        uint cumHistSamps = 0;
        double[] threshScale = { 1.8, 1.8, 1.8, 1.8, 0.5 };
        int countInitialSamples = 0;
        int wind2 = 60;   // window for smoothing data before taking std
        int wind = 60;

        public bool stopEphysAll = false;

        public bool writeFile = false;
        public float dH = 0.12f / 2.2f;
        public float dH_obst = 0.02f / 2.2f;

        long OLD_TIME;
        long NEW_TIME;
        long DT;


        public double velMultip = 1;
        public double vel2Multip = 0;

        public double autoGain1 = 0;
        public double autoGain2 = 0;
        public double velMultip1 = 1;
        public double velMultip2 = 1;
        public double gainswitch = 1;
        public int blevel = 255;
        public int boxpos = 1;


        public float dVel = 0.0012f / 3f;   // change in velocity per iteration when in closed loop the fish stops swimming
        public int countSwims = 0;
        public double t, ta;  // time
        public int tt, t1, t2, t3, t4, t5, t1_old, shift1, shift2;
        public float tf;
        public double t_last = 0;

        public float vel = 0;
        public float vel2 = 0;
        public double rel_t;
        public float stimID = 0;
        public int clmode_old;
        public bool countedSwim;
        public int cycle3 = 0;
        public int tchange1, tchange2 = 0;
        public static int tchange3;

        public static bool gainTriggerSwitch = false;

        public double[] swimPow = new double[2];
        public double[] cumSwimPow = new double[2];
        public double bufferSwimPow = 0.0001;
        public double spontPow;
        public double closedLoop1Dgain = 0;
        public static int recEPtime;
        public bool countFrame = true;

        /*-----------------------------*/
        public bool countStack = true;
        public int numFrameStack;
        public int stackFrame = 0;
        public int intervalCounter = 0;
        int[] riseFallCheck = new int[20];
        public int risefallCounter = 0;
        public int riseThresh = 14;
        public int fallThresh = 8;
        public int riseFallState = 0;
        

        public int optoStart = 0;
        public static int optoOngoing = 0;
        public int optoCount = 0;
        public double trigger_curr = 0;
        public double trigger_last = 0;
        private bool currRisingState = false;
        private bool lastRisingState = false;
        private bool risingAmp = false;
        public int[] trialLen;
        public bool[] optoStim;
        public int[] stimPattern;
        public static int currBlock;
        /*-----------------------------*/

        public double sps;
        public static float gain;


        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch stopwatchRecEP = new System.Diagnostics.Stopwatch();

        public static bool readFromFile = false;
        public BinaryReader ch1FileStream, ch2FileStream, ch3FileStream;
        //FileStream ch1_ephys, ch2_ephys, ch3_ephys;

        public float obst = 3.5f;
        public float preObst = 4f;
        public float postObst = 10f;
        static public float postObst_short = 3f;
        public float staPos = 6f; //20221001
        public float ySec, yLocSec, yLastSec, ySta;
        public bool enteringObsTrial = true;
        public bool enteringIntTrial = false;
        public float boundary = 8f;
        public float leftWall, rightWall, xNext, yNext, xLast, yLast, xLocNew, yLocNew;
        public float xFish = -0.7f;
        public float yFish = -0.5f;
        public float nextBlock = 0f;
        public float lastBlock = 0f;
        public float fishWidth = 0.5f;
        public float fishLength = 0.8f;

        public int num = 4;
        public int leftChannel = 2;
        public int rightChannel = 3;

        public double stimParam1;
        public double stimParam2 = 0;
        public double hitTimer = 0;//20210819
        public double StaTimer;//20221001
        public double vBackgroundture = 0.3; //20221001
        public bool isJittering = true;
        private Thread displayThread = null;
        private Thread optoThread = null;
        private Thread oscilloscopeThread = null;

        public struct StimState
        {
            public float xPosition, yPosition;
            public float orientation;
            public bool wrap;
        }

        public StimState stimState = new StimState();

        public StimEphysOscilloscopeControl(object sender)
        {
            senderWindow = (Window1)sender;
            stimulus1 = new StimWindow();
            stimulus1.Show();
            /*------------------------*/
            if (senderWindow.OptoStimOn)
            {
                opto1 = new OptoWindow();
                opto1.Show();
            }
            /*------------------------*/
        }


        public void StartEphys(object sender)
        {

            t1 = -1; t2 = -1;

            tchange1 = 0;
            tchange2 = 0;
            senderWindow = (Window1)sender;
            reset_switchDisp();
            switchDisp.frameNum = 0;
            switchDisp.stackNum = 0;
            FileNameEnabled = false;
            sps = (double)senderWindow.InstStimParams.sps;
            if (senderWindow.OptoStimOn)
            {
                opto1.LoadParams(senderWindow.slmParam);
                opto1.pictureBox1.Image = opto1.patterns[0];
            }
            displayThread = new Thread(new ParameterizedThreadStart(renderLoopLoop));
            displayThread.Start(this);

            oscilloscopeThread = new Thread(new ParameterizedThreadStart(oscilloscopeLoop));
            oscilloscopeThread.Start(this);
            trialLen = new int[senderWindow.blockNum];
            optoStim = new bool[senderWindow.blockNum];
            stimPattern = new int[senderWindow.blockNum];
            for (int i = 0;i< trialLen.Length;i++)
            {
                DataRow row = Window1.trialInfoTable.Rows[i];
                trialLen[i] = (int)row["trialLen"];
                optoStim[i] = (bool)row["optoStim"];
                stimPattern[i] = (int)row["stimPattern"];
            }

            cumSwimPow[0] = 10;
            cumSwimPow[1] = 10;

            countedSwim = false;

            stimState.xPosition = 0;// 1.5f;
            stimState.yPosition = 0;// 1f;
            stimState.orientation = 0;// 1.57f;
            stimState.wrap = true;

            stopEphysAll = false;
            writeFileStreamEP = null;

            if (senderWindow.fileNameEP != null)
            {
                bool OK;
                int fnum;
                string fileNameEP;

                if (senderWindow.fileNameEP.Length > 0)
                {
                    OK = false;
                    fnum = 1;
                    fileNameEP = senderWindow.fileNameEP;

                    while (!OK)
                    {
                        if (!File.Exists(fileNameEP + ".10chFlt"))
                        {
                            OK = true;
                        }
                        else
                        {
                            fileNameEP = senderWindow.fileNameEP + "-" + fnum.ToString();
                            fnum++;
                        }
                    }

                    fs_ephys = new FileStream(fileNameEP + ".10chFlt", FileMode.OpenOrCreate);
                    writeFileStreamEP = new BinaryWriter(fs_ephys);
                }
            }


            //ch1_ephys = new FileStream("C:/Users/fish/Desktop/BehaveAndScanGEVI_optovin/ch1.data", FileMode.Open);
            //ch1FileStream = new BinaryReader(ch1_ephys);

            //ch2_ephys = new FileStream("C:/Users/fish/Desktop/BehaveAndScanGEVI_optovin/ch2.data", FileMode.Open);
            //ch2FileStream = new BinaryReader(ch2_ephys);

            //ch3_ephys = new FileStream("C:/Users/fish/Desktop/BehaveAndScanGEVI_optovin/ch3.data", FileMode.Open);
            //ch3FileStream = new BinaryReader(ch3_ephys);


            EphysInputTask = new Task("EphysInputTask");
            string inputChannel = "Dev1/ai0,Dev1/ai1,Dev1/ai" + senderWindow.camChan + ",Dev1/ai" + senderWindow.piezoChan;
            EphysInputTask.AIChannels.CreateVoltageChannel(inputChannel, "", AITerminalConfiguration.Differential, -5, 5, AIVoltageUnits.Volts);
            EphysInputTask.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 100000);
            EphysInputTask.Control(TaskAction.Verify);


            LEDTask = new Task();

            LEDTask.DOChannels.CreateChannel("Dev1/port0/line0", "", ChannelLineGrouping.OneChannelForEachLine);	// specify hardware
            LEDTask.Control(TaskAction.Verify);
            LEDControl(false);

            EphysInputTask.Start();
            stopwatch.Start();

            EphysReader = new AnalogMultiChannelReader(EphysInputTask.Stream);

            while (!stopEphysAll)
            {
                EphysInputRead();
            }
            stopEphysLocal();

        }

        public void LEDControl(bool IsON)
        {
            DigitalSingleChannelWriter LEDWrite = new DigitalSingleChannelWriter(LEDTask.Stream);
            LEDWrite.WriteSingleSampleSingleLine(true, IsON);
        }


        private void EphysInputRead()
        {
            if (!stopEphysAll)
            {
                Check_stimulus_type();

                OLD_TIME = NEW_TIME;
                NEW_TIME = stopwatch.ElapsedMilliseconds;
                DT = NEW_TIME - OLD_TIME;

                if (senderWindow.InstStimParams.stopStimEphys)
                {
                    stopEphysAll = true;
                }

                //This option enables user to read from binary data files.
                //Here the files are readouts from a previous recording session.
                //if (readFromFile)
                //    for (int ii = 0; ii < 50; ii++)
                //    {
                //        readData[0, ii] = ch1FileStream.ReadDouble();
                //        readData[1, ii] = ch2FileStream.ReadDouble();
                //        //readData[2, ii] = ch3FileStream.ReadDouble();
                //    }
                //else
                readData = EphysReader.ReadMultiSample(-1);



                int LRD = readData.GetLength(1);
                int LBD = buffData.GetLength(1);


                if (!triggerGo & senderWindow.triggerStart)
                {

                    for (int ttt = 0; ttt < readData.GetLength(1); ttt++)
                    {
                        if (readData[4, ttt] > (double)3.2) { triggerGo = true; }
                    }

                    if (triggerGo)
                    {
                        senderWindow.Dispatcher.Invoke((Action)(() =>
                        {
                            senderWindow.writeFileBox.IsChecked = true;
                            senderWindow.writeOnEP = true;
                            senderWindow.triggerStart = false;
                            senderWindow.Triggered.IsChecked = false;
                        }));

                    }
                }


                if (countInitialSamples <= 12000)
                    countInitialSamples += readData.GetLength(1);


                //Moves new readData to buffData.
                for (int ttt = 0; ttt < LBD - LRD; ttt++)
                {
                    for (int i = 0; i < num; i++)
                    {
                        buffData[i, ttt] = buffData[i, ttt + LRD];
                    }
                    buffData[4, ttt] = buffData[4, ttt + LRD];
                }

                for (int ttt = 0; ttt < LRD; ttt++)
                {
                    for (int i = 0; i < num; i++)
                    {
                        buffData[i, LBD - LRD + ttt] = readData[i, ttt];
                    }
                    buffData[4, LBD - LRD + ttt] = optoOngoing;
                }
                currBlock = 0;
                int trialNum = 0;
                for (int i = 0; i<trialLen.Length;i++)
                {
                    trialNum += trialLen[i];
                    if (senderWindow.trialnumber%trialLen.Sum() < trialNum * 2)
                    {
                        currBlock = i;
                        break;
                    }
                }
                /*----------------------------------------------------------------------------*/
                if (senderWindow.OptoStimOn & optoStim[currBlock] & senderWindow.trialnumber%2==0 & senderWindow.currTrialgoingT < senderWindow.stimTrialT) // 
                {
                    for (int ttt = 0; ttt < LRD; ttt++)
                    {
                        if (ttt == 0)
                        {
                            trigger_last = readData[3, ttt];
                        }
                        if (senderWindow.triggerMode == 1 & ttt % 8 == 7)
                        {
                            trigger_curr = readData[3, ttt];
                            if (trigger_curr > trigger_last)
                            {
                                riseFallCheck[risefallCounter] = 1;
                            }
                            else if (trigger_curr < trigger_last)
                            {
                                riseFallCheck[risefallCounter] = -1;
                            }
                            else
                            {
                                riseFallCheck[risefallCounter] = 0;
                            }
                            risefallCounter++;
                            if (risefallCounter >= 20)
                            {
                                risefallCounter = 0;
                            }
                            trigger_last = trigger_curr;
                        }
                        else if (senderWindow.triggerMode == 2)
                        {
                            trigger_curr = readData[2, ttt];
                            if (trigger_curr > (double)3.2 & countFrame)
                            {
                                switchDisp.frameNum++;
                                stackFrame++;
                                countFrame = false;
                            }
                            else if (trigger_curr <= (double)3.2 & !countFrame)
                            {
                                countFrame = true;
                            }

                            if (trigger_curr > (double)3.6 & countStack)
                            {
                                switchDisp.stackNum++;
                                countStack = false;
                            }
                            if (stackFrame >= senderWindow.viFPS & !countStack)
                            {
                                countStack = true;
                                stackFrame = 0;
                                intervalCounter++;
                                if (intervalCounter >= senderWindow.viInterval)
                                {
                                    optoStart = 1;
                                    intervalCounter = 0;
                                }
                            }
                        }
                        if (senderWindow.triggerMode == 1 & ttt == LRD - 1)
                        {
                            currRisingState = !riseFallCheck.Contains(-1);
                            if (lastRisingState & !currRisingState & risingAmp) //& trigger_curr>3.73
                            {
                                intervalCounter++;
                                if (intervalCounter >= senderWindow.viInterval)
                                {
                                    optoStart = 1;
                                    intervalCounter = 0; 
                                }
                            }
                            risingAmp = trigger_curr - readData[3, 0]>0.02;
                            lastRisingState = currRisingState;
                        }
                    }
                    Array.Clear(riseFallCheck,0,riseFallCheck.Length);
                    trigger_curr = 0; trigger_last = 0;
                    if (optoStart == 1)
                    {
                        optoThread = new Thread(new ParameterizedThreadStart(slmSequence));
                        optoThread.Start(stimPattern[currBlock]);
                    }
                    optoStart = 0;
                }
                /*----------------------------------------------------------------------------*/



                //Calculates standard deviation of data points and stores in filtData.
                calcPhysParams(LBD, LRD, buffData);

                oscMode = senderWindow.oscParam.oscMode;
                for (int i = 0; i < num; i++)
                {
                    threshScale[i] = senderWindow.oscParam.threshScale[i];
                }
                leftChannel = senderWindow.leftChannel;
                rightChannel = senderWindow.rightChannel;



                t = (int)(stopwatch.ElapsedMilliseconds);
                if (!stopEphysAll)
                {
                    //Detects swim when standard deviation rises above threshold and combine them to get swim power.

                    swimPow[0] = swimPow[1] = 0;
                    for (int ttt = filtData.GetLength(1) - (int)(LRD / 10.0); ttt < filtData.GetLength(1); ttt++)        // assumed 10 real data samples per filtData sample
                    {
                        if (filtData[leftChannel, ttt] > thresh[leftChannel])
                            swimPow[0] += filtData[leftChannel, ttt];
                        if (filtData[rightChannel, ttt] > thresh[rightChannel])
                            swimPow[1] += filtData[rightChannel, ttt];
                    }


                    swimPow[0] = Math.Sqrt(swimPow[0]);
                    swimPow[1] = Math.Sqrt(swimPow[1]);
                    // NOTE: IN MS WHEREAS NORMALLY IN S
                    if (t - t_last > 400 && countedSwim == false)     // 400 ms silence between swims
                    {
                        countSwims += 1;
                        countedSwim = true;
                    }
                    if (swimPow[0] > 0 || swimPow[1] > 0)    // reset t_last
                    {
                        t_last = t;
                        countedSwim = false;
                    }


                    //Uses calculated swim power to produce visual feedback.
                    swimModeSwitch();
                    swimFeedBacks();

                    if (senderWindow.InstStimParams.clmode == 4)
                        stimParam1 = nextBlock;


                    //Writes ephys data and parameters to file.
                    if (senderWindow.writeOnEP && writeFileStreamEP != null)
                    {


                        double tr = stopwatchRecEP.ElapsedMilliseconds;

                        recEPtime = (int)tr / 1000;

                        if (tr == 0)
                        {
                            stopwatch.Reset();
                            stopwatch.Start();
                            stopwatchRecEP.Reset();
                            stopwatchRecEP.Start();

                        }


                        if (triggerGo)
                        {

                            if (switchDisp.frameNum == 0)
                            {
                                stopwatch.Reset();
                                stopwatch.Start();

                                gainTriggerSwitch = false;
                                tchange1 = 0;
                                tchange2 = 0;
                            }


                            for (int ttt = 0; ttt < readData.GetLength(1); ttt++)
                            {
                                if (readData[4, ttt] > (double)3.2 & countFrame)
                                {
                                    switchDisp.stackNum++;
                                    switchDisp.frameNum++;
                                    countFrame = false;
                                }
                                else if (readData[4, ttt] <= (double)3.2 & !countFrame)
                                {
                                    countFrame = true;
                                }
                            }
                        }



                        int tstopE = (int)senderWindow.CommonDuration;

                        if (senderWindow.DurationStopE && ((triggerGo && (tstopE * sps) < switchDisp.stackNum) || (!triggerGo && tstopE < tr / 1000)))
                        {
                            stopEphysAll = true;
                            stopwatch.Reset();
                            stopwatchRecEP.Reset();
                        }
                        else
                        {
                            for (int mm = 0; mm < readData.GetLength(1); mm++)
                            {
                                writeFileStreamEP.Write((float)readData[0, mm]);
                                writeFileStreamEP.Write((float)readData[1, mm]);
                                writeFileStreamEP.Write((float)readData[2, mm]);//camera
                                writeFileStreamEP.Write((float)senderWindow.InstStimParams.clmode);
                                writeFileStreamEP.Write((float)senderWindow.trialnumber);//20210819 
                                writeFileStreamEP.Write((float)senderWindow.CurrObstacleDur);
                                writeFileStreamEP.Write((float)senderWindow.xLoc);
                                writeFileStreamEP.Write((float)senderWindow.yLoc);
                                writeFileStreamEP.Write((float)stimParam1);
                                writeFileStreamEP.Write((float)stimParam2);
                                writeFileStreamEP.Write((float)closedLoop1Dgain);
                                writeFileStreamEP.Write((float)senderWindow.leftGain);
                                writeFileStreamEP.Write((float)senderWindow.rightGain);
                                /*-----------------------------------------*/
                                writeFileStreamEP.Write((float)readData[3, mm]); // piezo
                                writeFileStreamEP.Write((float)optoOngoing);
                                writeFileStreamEP.Write((float)LRD);  // the length of each readData
                                /*-----------------------------------------*/
                            }
                        }
                    }
                }
            }
        }


        public void stopEphysLocal()
        {
            FileNameEnabled = true;


            if (stopEphysAll)
            {
                senderWindow.Dispatcher.Invoke((Action)(() =>
                {
                    Thread.Sleep(200);
                    senderWindow.StimulusON = false;
                    senderWindow.InstStimParams.stopStimEphys = false;
                    senderWindow.buttonStartStimEphys.IsEnabled = true;
                    senderWindow.buttonStopStimEphys.IsEnabled = false;
                    senderWindow.sps_box.IsEnabled = true;
                    senderWindow.writeFileBox.IsChecked = false;
                    senderWindow.Triggered.IsEnabled = true;
                    senderWindow.trialnumber = 0;


                    senderWindow.writeOnEP = false;
                    bool optoThreadStopped = false;

                    while (displayThread.ThreadState != System.Threading.ThreadState.Stopped& !optoThreadStopped& oscilloscopeThread.ThreadState != System.Threading.ThreadState.Stopped)
                    {
                        if (optoThread == null)
                            optoThreadStopped = true;
                        else if (optoThread.ThreadState == System.Threading.ThreadState.Stopped)
                            optoThreadStopped = true;
                    }
                    stimulus1.CloseWindow();
                    //stimulus1.Dispose();
                    stimulus1.Close();
                    stimulus1 = null;
                    /*------------------------------*/
                    if(senderWindow.OptoStimOn)
                    {
                        opto1.Dispose();
                        opto1.Close();
                        opto1 = null;
                    }
                    /*------------------------------*/
                }));
                recEPtime = 0;

                EphysInputTask.Stop();
                EphysInputTask.Dispose();
                EphysInputTask = null;

                LEDControl(false);
                LEDTask.Stop();
                LEDTask.Dispose();
                LEDTask = null;

                reset_switchDisp();
                stopEphysAll = false;
                stopwatch.Reset();
                stopwatchRecEP.Reset();
                switchDisp.stackNum = 0;
                switchDisp.frameNum = 0;
                triggerGo = false;
            }

            if (writeFileStreamEP != null)
            {
                writeFileStreamEP.Close();
                writeFileStreamEP = null;
                writeFile = false; ;
            }

            for (int j1 = 0; j1 < histogram.GetLength(0); j1++)
                for (int j2 = 0; j2 < histogram.GetLength(1); j2++)
                    histogram[j1, j2] = 0;
            for (int j1 = 0; j1 < buffData.GetLength(0); j1++)
                for (int j2 = 0; j2 < buffData.GetLength(1); j2++)
                    buffData[j1, j2] = 0;
            for (int j1 = 0; j1 < filtData.GetLength(0); j1++)
                for (int j2 = 0; j2 < filtData.GetLength(1); j2++)
                    filtData[j1, j2] = 0;
        }

        public void renderLoopLoop(object sender)
        {
            StimEphysOscilloscopeControl senderStim = (StimEphysOscilloscopeControl)sender;
            while (!stopEphysAll)
            {
                stimulus1.Render(sender);
            }
        }


        public void oscilloscopeLoop(object sender)
        {
            StimEphysOscilloscopeControl senderStim = (StimEphysOscilloscopeControl)sender;
            oscilloscope = new Oscilloscope(senderWindow, 800, 800);
            while (!senderStim.stopEphysAll)
            {
                if (senderStim.oscMode == 1)
                    oscilloscope.RefreshGraph(senderStim.buffData);
                else if (senderStim.oscMode == 2)
                    oscilloscope.RefreshGraph(senderStim.filtData, senderStim.thresh, senderStim.mean_, senderStim.min_);
                else if (senderStim.oscMode == 3)
                    oscilloscope.RefreshGraph(senderStim.histogram);
                else if (senderStim.oscMode == 4)
                    oscilloscope.RefreshGraph(senderStim.buffData, senderWindow.oscParam.axisValue);
                Thread.Sleep(100);
            }
        }

        /*---------------------------------------*/
        public void slmSequence(object seqNum)
        {
            opto1.ShowSequenceNew((int)seqNum);
        }
        /*---------------------------------------*/

        private void Check_stimulus_type()
        {
            autoGain1 = senderWindow.InstStimParams.autoGain1;
            autoGain2 = senderWindow.InstStimParams.autoGain2;
            velMultip1 = senderWindow.InstStimParams.autoVel1;
            velMultip2 = senderWindow.InstStimParams.autoVel2;

            if (clmode_old != senderWindow.InstStimParams.clmode)
            {
                stopwatch.Reset();
                stopwatch.Start();
                switchDisp.frameNum = 0;
                switchDisp.stackNum = 0;
                clmode_old = senderWindow.InstStimParams.clmode;
                t1 = -1; t2 = -1;
            }
        }




        private void calcPhysParams(int LBD, int LRD, double[,] Data)
        {

            // GENERATE FILTERED DATA

            //Array.Clear(filtData, 0, filtData.Length);
            Array.Clear(filtData_, 0, filtData_.Length);

            int k = 0;
            for (int ttt = 0; ttt < LBD - (int)(wind / 2.0); ttt += 10)
            {
                int count__ = 0;
                for (int i = 0; i < num; i++)
                {
                    filtData[i, k] = 0;
                }


                for (int q = Math.Max(-(int)(wind2 / 2.0), -ttt); q < (int)(wind2 / 2.0); q++)
                {
                    for (int i = 0; i < num; i++)
                    {
                        filtData_[i, k] += Data[i, ttt + q];
                    }
                    count__++;
                }

                for (int i = 0; i < num; i++)
                {
                    filtData_[i, k] /= ((double)count__);
                }


                for (int q = Math.Max(-(int)(wind / 2.0), -ttt); q < (int)(wind / 2.0); q++)
                {
                    for (int i = 0; i < num; i++)
                    {
                        filtData[i, k] += Math.Pow((Data[i, ttt + q] - filtData_[i, k]), 2);
                    }
                }

                for (int i = 0; i < num; i++)
                {
                    filtData[i, k] = filtData[i, k] / ((double)count__) * 60.0;
                    if (filtData[i, k] == 0)
                        filtData[i, k] = mean_[i];
                }


                k++;
            }

            // COMPUTE HISTOGRAM

            if (countInitialSamples >= 12000)
            {
                for (int ttt = 0; ttt < filtData.GetLength(1); ttt++)
                {
                    for (int i = 0; i < num; i++)
                    {
                        div[i] = (int)(filtData[i, ttt] * 3000);
                        if (div[i] > 1499) { div[i] = 1499; }
                        if (div[i] < 0) { div[i] = 0; }
                        histogram[i, div[i]] += 1;
                    }
                }
            }

            cumHistSamps += (uint)LRD;
            if (cumHistSamps > 6000 * 15)
            {
                cumHistSamps = 0;
                for (int ttt = 0; ttt < histogram.GetLength(1); ttt++)
                {
                    for (int i = 0; i < num; i++)
                    {
                        histogram[i, ttt] /= 2;
                    }
                }
            }


            // DECIDE MIN AND MAX 

            Array.Clear(histMxPos, 0, histMxPos.Length);
            Array.Clear(histMnPos, 0, histMnPos.Length);

            // DECIDE MAX

            for (int i = 0; i < num; i++)
            {
                histMxPos[i] = 1;
                for (int q = 1; q < histogram.GetLength(1); q++)
                {
                    if (histogram[i, q] > histogram[i, histMxPos[i]])
                        histMxPos[i] = q;
                }
            }

            // DECIDE MIN

            for (int i = 0; i < num; i++)
            {
                histMnPos[i] = 0;
                for (int q = 1; q < histMxPos[i]; q++)
                    if (histogram[i, q] < (double)histogram[i, histMxPos[i]] / 400)
                        histMnPos[i] = q;
            }

            // DECIDE THRESHOLD

            for (int i = 0; i < num; i++)
            {
                thresh[i] = (histMxPos[i] + threshScale[i] * (histMxPos[i] - histMnPos[i])) / 3000.0;
                if (thresh[i] == 0)
                    thresh[i] = 10;
                mean_[i] = (histMxPos[i]) / 3000.0;
                min_[i] = (histMnPos[i]) / 3000.0;
            }

        }

    }
}