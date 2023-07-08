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


namespace BehaveAndScanTK
{
    public partial class StimEphysOscilloscopeControl 
    {
        public Window1 senderWindow = null;
        private StimWindow stimulus1 = null;
        private Oscilloscope oscilloscope = null;
        public static switchDisps switchDisp = new switchDisps();
        public static bool FileNameEnabled = true;
        public bool triggerGo = false;

        int sampleRate = 6000;   // 60 Hz projector, 100 samples per frame // appears to be more like 30 Hz, don't know why
        public Task EphysInputTask;
        public Task EphysRunningTask;
        public AnalogMultiChannelReader EphysReader;
        public AsyncCallback EphysInputCallback;
        public bool EphysRun;

        BinaryWriter writeFileStreamEP;
        FileStream fs_ephys;

        public double displayAngle = Math.PI;
        public double moveAngle = Math.PI;    
        
        public float stripesY = 0;
        public int stim1DclosedLoopContrast = 200;
        int oscMode = 1;  // oscilloscope mode

        double[,] buffData = new double[3, 6000];
        double[,] readData;
        double[,] filtData_ = new double[3, 600 - (int)(60 / 2.0 / 10.0)]; 
        double[,] filtData = new double[3, 600 - (int)(60 / 2.0 / 10.0)];
        uint[,] histogram = new uint[3, 1500];
        int[] histMxPos = new int[3];
        int[] histMnPos = new int[3];
        double[] thresh = new double[3];
        double[] mean = new double[3];
        double[] mean_ = new double[3];
        double[] min_ = new double[3];
        int[] div = new int[3];
        uint cumHistSamps = 0;
        double[] threshScale = { 1.8, 1.8, 0.5 };
        int countInitialSamples = 0;
        int LAG = 0;
        int wind2 =60;   // window for smoothing data before taking std
        int wind = 60; 

        public bool stopEphysAll = false;

        public bool writeFile = false;
        public float dH = 0.12f / 2.2f;  
        
        long OLD_TIME;
        long NEW_TIME;
        long DT;

        int velBufferI = 0;
        int velReplayI = 0;

        public double velMultip = 1;
        public float[] vel_mults = { -50f, 50f, 0f, -15f, 15f, 0f, -10f, 10f, 0f, -5f, 5f, 0f, -2f, 2f, 0f };

        public double autoGain1 = 0;
        public double autoGain2 = 0;
        public double velMultip1 = 1;
        public double velMultip2 = 1;
        public double gainswitch = 1;
               
        public int[] shufflelist1 = { 1, 2, 3, 4, 5, 6, 7, 8 };
        public int[] shufflelist2 = { 1, 2, 3, 4, 5, 6, 7, 8 };

        public float stimParam1 = 0;
        public float stimParam2 = 0;
        public float stimParam3 = 0;
        public float stimParam4 = 0;
        public float stimParam4old = 0;
        
        public float dVel = 0.0012f / 3f;   // change in velocity per iteration when in closed loop the fish stops swimming
        public float[] velBuffer = new float[1500000];

        public int countSwims = 0;
        double t, ta;  // time
        public int tt,t1,t2;
        public double t_last = 0;
        public double[] avPow;


        public float vel = 0;
        public double rel_t;
        public float stimID = 0;
        public int stimtype_old;
        public int clmode_old;
        public bool countedSwim;

        public double[] swimPow = new double[2];
        public double spontPow;
        public double closedLoop1Dgain = 0;
        public static int recEPtime;
        public int timeSince;
                
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch stopwatchRecEP = new System.Diagnostics.Stopwatch();


        public struct StimState
        {
            public float xPosition, yPosition;
            public float orientation;
            public bool wrap;
        }

        public StimState stimState = new StimState();

        public StimEphysOscilloscopeControl()
        {
            stimulus1 = new StimWindow();
            stimulus1.Show();
        }
        

        public void StartEphys(object sender)
        {

            t1 = -1; t2 = -1;
            senderWindow = (Window1)sender;
            reset_switchDisp();
            FileNameEnabled = false;

            Thread displayThread = new Thread(new ParameterizedThreadStart(renderLoopLoop));
            displayThread.Start(this);

            Thread oscilloscopeThread = new Thread(new ParameterizedThreadStart(oscilloscopeLoop));
            oscilloscopeThread.Start(this);


            countedSwim = false;
            Random rand = new Random();

            timeSince = 0;

            avPow = new double[2];

            stimState.xPosition = 0;// 1.5f;
            stimState.yPosition = 0;// 1f;
            stimState.orientation = 0;// 1.57f;
            stimState.wrap = true;

            stopEphysAll = false;
            writeFileStreamEP = null;

            if (senderWindow.fileNameEP != null)
            {
                if (senderWindow.fileNameEP.Length > 0)
                {
                    fs_ephys = new FileStream(senderWindow.fileNameEP + ".10chFlt", FileMode.OpenOrCreate);
                    writeFileStreamEP = new BinaryWriter(fs_ephys);
                }
            }         


            avPow[0] = avPow[1] = 0;


            EphysInputTask = new Task("EphysInputTask");

            EphysInputTask.AIChannels.CreateVoltageChannel("dev1/ai0:2", "", AITerminalConfiguration.Differential, -5, 5, AIVoltageUnits.Volts);
            EphysInputTask.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 100000);
            EphysInputTask.Control(TaskAction.Verify);

            EphysInputTask.Start();

            EphysRunningTask = EphysInputTask;
            EphysInputCallback = new AsyncCallback(EphysInputRead);
            EphysReader = new AnalogMultiChannelReader(EphysInputTask.Stream);
            EphysReader.SynchronizeCallbacks = false;   
            EphysRun = true;

            EphysReader.BeginReadMultiSample(120, EphysInputCallback, EphysInputTask);
            while (!stopEphysAll)
            {
                System.Threading.Thread.Sleep(100); 
            }
            stopEphysLocal();

        }


        private void EphysInputRead(IAsyncResult ar)
        {
            if (!stopEphysAll)
            {
                check_stimulus_type();
                stimID = senderWindow.InstStimParams.stimtype;

                OLD_TIME = NEW_TIME;
                NEW_TIME = stopwatch.ElapsedMilliseconds;
                DT = NEW_TIME - OLD_TIME;

                if (senderWindow.InstStimParams.stopStimEphys)
                {
                    stopEphysAll = true;
                }

                readData = EphysReader.EndReadMultiSample(ar);
                EphysReader.BeginReadMultiSample(120, EphysInputCallback, EphysInputTask);


                int LRD = readData.GetLength(1);
                int LBD = buffData.GetLength(1);


                if (senderWindow.triggerStart)
                {

                    for (int ttt = 0; ttt < readData.GetLength(1); ttt++)
                    {
                        if (readData[2, ttt] > (double)3) { triggerGo = true; }
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


                if (countInitialSamples < 12000)
                    countInitialSamples += readData.GetLength(1);

                double[] lowPassData = new double[readData.GetLength(1)];       // DODGY !!!!!!

                for (int ttt = 0; ttt < readData.GetLength(1); ttt++)
                {
                    lowPassData[ttt] = 0;
                    int cnt = 0;
                    for (int kk = 0; kk <= 1; kk++)
                    {
                        for (int g = -50; g <= 50; g++)
                        {
                            if ((ttt + g >= 0 && ttt + g < readData.GetLength(1)))
                            {
                                lowPassData[ttt] = lowPassData[ttt] + readData[kk, ttt + g];
                                cnt++;
                            }
                        }
                    }
                    lowPassData[ttt] = lowPassData[ttt] / (double)cnt;
                }


                for (int ttt = 0; ttt < LBD - LRD; ttt++)
                {
                    buffData[0, ttt] = buffData[0, ttt + LRD];
                    buffData[1, ttt] = buffData[1, ttt + LRD];
                    buffData[2, ttt] = buffData[2, ttt + LRD];
                }

                for (int ttt = 0; ttt < LRD; ttt++)
                {
                    buffData[0, LBD - LRD + ttt] = readData[0, ttt];
                    buffData[1, LBD - LRD + ttt] = readData[1, ttt];
                    buffData[2, LBD - LRD + ttt] = lowPassData[ttt];
                }




                calcPhysParams(LBD, LRD);

                oscMode = senderWindow.oscParam.oscMode;
                threshScale[0] = senderWindow.oscParam.threshScale[0];
                threshScale[1] = senderWindow.oscParam.threshScale[1];
                threshScale[2] = senderWindow.oscParam.threshScale[2];

                if (senderWindow.writeOnEP && writeFileStreamEP != null)
                {
                    long tr = stopwatchRecEP.ElapsedMilliseconds;
                    if (tr == 0)
                    {
                        stopwatch.Reset();
                        stopwatch.Start();
                        stopwatchRecEP.Reset();
                        stopwatchRecEP.Start();
                    }

                    recEPtime = (int)tr / 1000;

                    long tstopE = (long)senderWindow.CommonDuration * 1000;
                    if (tstopE < tr && senderWindow.DurationStopE)
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
                            writeFileStreamEP.Write((float)readData[1, mm]);  ///...1
                            writeFileStreamEP.Write((float)readData[2, mm]);
                            writeFileStreamEP.Write((float)stimParam4);
                            writeFileStreamEP.Write((float)0);   // new 12 may 11
                            writeFileStreamEP.Write((float)lowPassData[mm]);   // new 12 may 11
                            writeFileStreamEP.Write((float)stimParam3);
                            writeFileStreamEP.Write((float)stimID);
                            writeFileStreamEP.Write((float)stimParam1);
                            writeFileStreamEP.Write((float)stimParam2);
                        }
                    }
                }

                if (!stopEphysAll)

                {
                    // DETECTING SWIMS//////////////////////////////////////////////////////////////////////////////////

                    swimPow[0] = swimPow[1] = 0;
                    for (int ttt = filtData.GetLength(1) - (int)(LRD / 10.0); ttt < filtData.GetLength(1); ttt++)        // assumed 10 real data samples per filtData sample
                        for (int j = 0; j < 2; j++)
                            if (filtData[j, ttt] > thresh[j])
                                swimPow[j] += filtData[j, ttt];

                    swimPow[0] = Math.Sqrt(swimPow[0]);
                    swimPow[1] = Math.Sqrt(swimPow[1]);

                    t = (int)(stopwatch.ElapsedMilliseconds);   // NOTE: IN MS WHEREAS NORMALLY IN S
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

                    timeSince = (int)(t - t_last);


                    // SPONTANEOUS POWER --- THE LOW-PASSED CHANNELS 1 AND 2, THAT GIVE THESE SPONTANEOUS BLIPS...

                    spontPow = 0;
                    for (int ttt = filtData.GetLength(1) - (int)(LRD / 10.0); ttt < filtData.GetLength(1); ttt++)
                    {
                        if (filtData[2, ttt] > thresh[2])
                        {
                            spontPow += (filtData[2, ttt] - thresh[2]);
                        }
                    }


                    // DETECTING SWIMS//////////////////////////////////////////////////////////////////////////////////

                    swimModeSwitch();
                    swimFeedBacks();
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
                    recEPtime = 0;
                    senderWindow.writeFileBox.IsChecked = false;

                    senderWindow.writeOnEP = false;

                    stimulus1.CloseWindow();
                    stimulus1.Dispose();
                    stimulus1.Close();
                    stimulus1 = null;
                }));

                EphysInputTask.Stop();
                EphysInputTask.Dispose();
                EphysInputTask = null;

                reset_switchDisp();
                stimParam3 = 0;
                stimParam4 = 0;
                stimParam4old = 0 ;
                stopEphysAll = false;
                stopwatch.Reset();
                stopwatchRecEP.Reset();
                triggerGo = false;
            }

            if (writeFileStreamEP != null)
            {
                writeFileStreamEP.Close();
                writeFileStreamEP = null;
                writeFile = false;;
                senderWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    senderWindow.textMessageBox.Text = "closed the e-phys file";
                }));
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
                Thread.Sleep(200);
            }
        }

        private void check_stimulus_type()
        {
            autoGain1       = senderWindow.InstStimParams.autoGain1;
            autoGain2       = senderWindow.InstStimParams.autoGain2;
            velMultip1      = senderWindow.InstStimParams.autoVel1;
            velMultip2      = senderWindow.InstStimParams.autoVel2;
            
            if (senderWindow.InstStimParams.recordPlayback)
            {
                switchDisp.velReplay = (velReplayI > 0);
            }

            if (stimtype_old != senderWindow.InstStimParams.stimtype)
            {
                stopwatch.Reset();
                stopwatch.Start();
                stimtype_old = senderWindow.InstStimParams.stimtype;
                t1 = -1; t2 = -1;
            }

            if (clmode_old != senderWindow.InstStimParams.clmode)
            {
                stopwatch.Reset();
                stopwatch.Start();
                clmode_old = senderWindow.InstStimParams.clmode;
                t1 = -1; t2 = -1;
            }

        }


        private void calcPhysParams(int LBD, int LRD)
        {

            for (int kk = 0; kk <3; kk++)
            {
                mean[kk] = 0;
            }

            for (int ttt = 0; ttt < LBD; ttt++)
            {
                for (int kk = 0; kk < 3; kk++)
                {
                    mean[kk] = mean[kk] + buffData[kk, ttt];
                }

            }

            for (int kk = 0; kk < 3; kk++)
            {
                mean[kk] = mean[kk] / LBD;
            }




            for (int kk = 0; kk < 3; kk++)
            {
                int k = 0;
                for (int ttt = -LAG; ttt < LBD - (int)(wind / 2.0) - LAG; ttt += 10)
                {
                    filtData_[kk, k] = 0;
                    for (int q = Math.Max(-(int)(wind2 / 2.0), -ttt); q < (int)(wind2 / 2.0); q++)
                        filtData_[kk, k] += buffData[kk, ttt + q];

                    filtData_[kk, k] = filtData_[kk, k] / (0.000000000001 + (double)((int)(wind2 / 2.0) - Math.Max(-(int)(wind2 / 2.0), -ttt)));

                    filtData[kk, k] = 0; //filtData[1, k] = 0;
                    int count__ = 0;
                    for (int q = Math.Max(-(int)(wind / 2.0), -ttt); q < (int)(wind / 2.0); q++)
                    {
                        count__++;
                        if (kk < 2)
                            filtData[kk, k] += (buffData[kk, ttt + q] - filtData_[kk, k]) * (buffData[kk, ttt + q] - filtData_[kk, k]);
                        else
                            filtData[kk, k] += (0.1 + buffData[kk, ttt + q]) / 100;
                    }

                    filtData[kk, k] = filtData[kk, k] / ((double)count__) * 60.0;

                    if (filtData[kk, k] == 0)    // due to LAG
                        filtData[kk, k] = mean_[kk];
                    k++;
                }
            }

            
            for (int ttt = (int)(LAG / 10); ttt < filtData.GetLength(1); ttt++)
            {
                for (int kk = 0; kk < 3; kk++)
                {
                    if (countInitialSamples > 12000)
                    {
                        div[kk] = (int)(filtData[kk, ttt] * 3000);
                        if (div[kk] > 1499)
                            div[kk] = 1499;
                        if (div[kk] < 0)
                            div[kk] = 0;

                        histogram[kk, div[kk]] += 1;
                    }
                }
            }

            cumHistSamps += (uint)LRD;  
            if (cumHistSamps > 6000 * 15)
            {
                cumHistSamps = 0;
                for (int kk = 0; kk < 3; kk++)
                {
                    for (int ttt = 0; ttt < histogram.GetLength(1); ttt++)
                    {
                        histogram[kk, ttt] = histogram[kk, ttt] / 2;
                    }
                }
            }

            for (int kk = 0; kk < 3; kk++)
            {
                histMxPos[kk] = histMnPos[kk] = 0;
            }


            for (int kk = 0; kk < 3; kk++)
            {
                histMxPos[kk] = 1;
                for (int q = 1; q < histogram.GetLength(1); q++)
                {
                    if (histogram[kk, q] > histogram[kk, histMxPos[kk]])
                        histMxPos[kk] = q;
                }
            }

            for (int kk = 0; kk < 3; kk++)
            {
                histMnPos[kk] = 0;
                for (int q = 1; q < histMxPos[kk]; q++)
                    if (histogram[kk, q] < (double)histogram[kk, histMxPos[kk]] / 400)
                        histMnPos[kk] = q;
            }


            for (int kk = 0; kk < 3; kk++)
            {
                thresh[kk] = (histMxPos[kk] + threshScale[kk] * (histMxPos[kk] - histMnPos[kk])) / 3000.0;

                if (thresh[kk] == 0)
                    thresh[kk] = 10;

                mean_[kk] = (histMxPos[kk]) / 3000.0;
                min_[kk] = (histMnPos[kk]) / 3000.0;

            }
        }



        
    }
}