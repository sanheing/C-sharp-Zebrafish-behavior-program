using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaveAndScanGEVI_optovin
{
    public struct StimParams
    {
        //bools representing radio button state
        public int clmode;
        public int moveobject;
        public int sps;

        public double autoGain1;
        public double autoGain2;
        public double autoVel1;
        public double autoVel2;
        public double autoTime1;
        public double autoTime2;
        public double autoTimeSum;
        public double gwidth;
        public float[] stimSequence;

        public bool closedLoop1DzeroVel;
        public bool recordPlayback; // false;
        public bool recordPlayback_special; // false;
        public int replayfcycle;
    
        public bool stopStimEphys;
    }



    public struct switchParams
    {
        public double[] phasedur;
        public double[] phasedurinc;
        public double[] test_params;
        public double[] test_durinc;
        public int stopmode;
        public double cycles;
    }

    public struct switchDisps
    {
        public bool gain1check;
        public bool gain2check;
        public bool Phase1Time;
        public bool Phase2Time;
        public bool Phase3Time;
        public bool Phase4Time;
        public bool Phase5Time;
        public bool Phase6Time;
        public bool velReplay;
        public int frameNum;
        public int stackNum;
    }

    public struct oscParams
    {
        public int oscMode;
        public double[] threshScale;
        public double axisValue;
    }


    public struct oscULValues
    {
        public double ch1UpLim;
        public double ch1LoLim;
        public double ch2UpLim;
        public double ch2LoLim;
        public double ch3UpLim;
        public double ch3LoLim;
        public double ch4UpLim;
        public double ch4LoLim;
        /*------------------------------*/
        public double ch5UpLim;
        public double ch5LoLim;
        /*------------------------------*/
    }

    /*------------------------------*/
    public struct slmParams
    {
        public int slmScreen;
        public int slmSeqLen;
        public int slmOnSeg;
        public int slmOffSeg;
        public string slmPath;
    }
    /*------------------------------*/
}
