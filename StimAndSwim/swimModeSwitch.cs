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


namespace BehaveAndScanGEVI_optovin
{
    public partial class StimEphysOscilloscopeControl
    {

        public void swimModeSwitch()
        {

            if (triggerGo)
            {
                tt = (int)((switchDisp.stackNum - 1) / sps);
                tf = ((float)(switchDisp.stackNum - 1) / (float)sps);
            }
            else
            {
                tt = (int)(stopwatch.ElapsedMilliseconds / 1000.0);
                tf = (float)(stopwatch.ElapsedMilliseconds / 1000.0);// (int)(RecTime);
            }





            if (senderWindow.InstStimParams.clmode == 4) // simple switch  ////////////////
            {
                resetGratingParams();
                //stimParam1 = nextBlock;
                t = (int)tt % (int)senderWindow.InstStimParams.autoTimeSum;
                //stimParam5 = ((int)tt % (int)senderWindow.InstStimParams.autoTimeSum) + 1;

                if (t < senderWindow.InstStimParams.autoTime1)
                {
                    if (closedLoop1Dgain != autoGain1)
                    {
                        reset_switchDisp();
                        switchDisp.gain1check = true;
                        switchDisp.gain2check = false;
                    }
                    closedLoop1Dgain = autoGain1;
                    velMultip = velMultip1;
                }
                else
                {
                    if (closedLoop1Dgain != autoGain2)
                    {
                        reset_switchDisp();
                        switchDisp.gain1check = false;
                        switchDisp.gain2check = true;
                    }
                    closedLoop1Dgain = autoGain2;
                    velMultip = velMultip2;
                }
            }



            if (senderWindow.InstStimParams.clmode == 6) // Orientation tuning  //////////////
            {
                resetGratingParams();
                int t1 = (int)tt % 20;
                int t2 = (int)(tt % 40) / 10;
                stimParam1 = (float)t2;
                //stimParam4 = (float)t2 + 1;
                //stimParam5 = (float)(tt / 8) + 1;
                closedLoop1Dgain = autoGain1;
                if (t1 < 10)
                {
                    //if (senderWindow.switchParamsAuto.stopmode == 2 || senderWindow.switchParamsAuto.stopmode == 3)
                    //{
                    //    GratingsContrast = 0;
                    //}
                    //velMultip = 0;
                }
                else
                {
                    double an = (double)t2 / 4;
                    displayAngle = an * 2 * Math.PI;
                    moveAngle = an * 2 * Math.PI;
                }
            }


            if (senderWindow.InstStimParams.clmode == 10) // Left Right Balance ///zs 20210125
            {
                resetGratingParams();
                closedLoop1Dgain = autoGain1;
                int t0 = (int)tt % 3000;// 50min block= balance + task
                if (t0 < 300)//left-right balance 5min
                {
                    senderWindow.displayObstacles = false;//
                    if (swimPow[0] > 0 || swimPow[1] > 0)
                    {
                        cumSwimPow[0] = 0.99 * cumSwimPow[0] + swimPow[0] + bufferSwimPow;
                        cumSwimPow[1] = 0.99 * cumSwimPow[1] + swimPow[1] + bufferSwimPow;
                    }

                    senderWindow.rightGain = cumSwimPow[0] / cumSwimPow[1] * senderWindow.leftGain;
                    //Console.WriteLine(cumSwimPow[0]);


                }
                else // task
                {
                    senderWindow.displayObstacles = true;
                }



            }

        }



        public void resetGratingParams()
        {
           dH = (float)(senderWindow.InstStimParams.gwidth*0.16) / 2.2f;
           displayAngle = Math.PI;
           moveAngle = 0;
           closedLoop1Dgain = 0; 
        }

        public void reset_switchDisp()
        {
            switchDisp.gain1check = false;
            switchDisp.gain2check = false;
            switchDisp.Phase1Time = true;
            switchDisp.Phase2Time = true;
            switchDisp.Phase3Time = true;
            switchDisp.Phase4Time = true;
            switchDisp.Phase5Time = true;
            switchDisp.Phase6Time = true;
            switchDisp.velReplay = false;
        }

        public void shuffler<T>(T[] array)
        {
            Random random = new Random();
            for (int i = array.Length; i > 1; i--)
            {
                // Pick random element to swap.
                int j = random.Next(i); // 0 <= j <= i-1
                // Swap.
                T tmp = array[j];
                array[j] = array[i - 1];
                array[i - 1] = tmp;
            }
        }

    }
}
