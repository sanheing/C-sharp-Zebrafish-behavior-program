using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using NationalInstruments.DAQmx;


namespace BehaveAndScanTK
{
    public class Ephys
    {
        int sampleRate = 6000;   // 60 Hz projector, 100 samples per frame // appears to be more like 30 Hz, don't know why
        Task inputTask;
        Task runningTask;
        AnalogMultiChannelReader reader;
        AsyncCallback inputCallback;


        public Ephys()           // constructor initalizes the NI board channels.
        {           
            inputTask = new Task("inputTask2");
   
            inputTask.AIChannels.CreateVoltageChannel("dev1/ai0:2", "", AITerminalConfiguration.Differential, -5, 5, AIVoltageUnits.Volts);
            inputTask.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 100000);
            inputTask.Control(TaskAction.Verify);

            inputTask.Start();

            runningTask = inputTask;
            reader = new AnalogMultiChannelReader(inputTask.Stream);
            reader.SynchronizeCallbacks = true;   
        }

        public double[,] ReadInput(IAsyncResult ar)
        {
            double[,] readData = reader.EndReadMultiSample(ar);// EndReadMultiSample(ar);
            reader.BeginReadMultiSample(146, inputCallback, inputTask);
            return readData;
        }

        public void StopEphys()
        {
            inputTask.Stop();
            inputTask.Dispose();
            inputTask = null;            
        }
    }
}

