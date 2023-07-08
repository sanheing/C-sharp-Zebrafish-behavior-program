using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BehaveAndScanGEVI_optovin
{
    public partial class OptoWindow : Form
    {
        public System.Drawing.Bitmap pattern0, pattern1, pattern2, nextPattern;
        public System.Diagnostics.Stopwatch stopwatch;
        public slmParams slmParam0;
        public OptoWindow()
        {
            InitializeComponent();
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.DrawBlack);
            double resolution = 1E9 / System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine("The minimum measurable time on this system is: {0} nanoseconds", resolution);
        }
        public void LoadParams(slmParams slmParam1)
        {
            slmParam0 = slmParam1;
            this.Location = System.Windows.Forms.Screen.AllScreens[slmParam0.slmScreen].WorkingArea.Location;
            pattern0 = new Bitmap(slmParam0.slmPath + "0.png");
            pattern1 = new Bitmap(slmParam0.slmPath + "1.png");
            pattern2 = new Bitmap(slmParam0.slmPath + "2.png");
            //pattern0 = new Bitmap(slmParam0.slmPath + "0.bmp");
            //pattern1 = new Bitmap(slmParam0.slmPath + "1.bmp");
            //pattern2 = new Bitmap(slmParam0.slmPath + "2.bmp");
            Console.WriteLine("slmParam loaded");
        }

        public void ShowSequenceNew(int seqNum)
        {
            float time_past = 0f;
            int seqState = 0;
            pictureBox1.Image = pattern0;
            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            if (seqNum == 1)
            {
                pictureBox1.Image = pattern1;
                seqState = 1;
                BehaveAndScanGEVI_optovin.StimEphysOscilloscopeControl.optoOngoing = 1;
                while (stopwatch.ElapsedMilliseconds < slmParam0.slmSeqLen)
                {
                    if (seqState == 0 & (stopwatch.ElapsedMilliseconds - time_past) > slmParam0.slmOffSeg)
                    {
                        float current_time = stopwatch.ElapsedMilliseconds;
                        pictureBox1.Image = pattern1;
                        seqState = 1;
                        //Console.WriteLine(current_time - time_past);
                        time_past = current_time;
                        BehaveAndScanGEVI_optovin.StimEphysOscilloscopeControl.optoOngoing = 1;
                    }
                    else if (seqState == 1 & (stopwatch.ElapsedMilliseconds - time_past) > slmParam0.slmOnSeg)
                    {
                        float current_time = stopwatch.ElapsedMilliseconds;
                        pictureBox1.Image = pattern0;
                        seqState = 0;
                        //Console.WriteLine(current_time - time_past);
                        time_past = current_time;
                        BehaveAndScanGEVI_optovin.StimEphysOscilloscopeControl.optoOngoing = 0;
                    }
                }
            }
            else if (seqNum == 2)
            {
                pictureBox1.Image = pattern2;
                seqState = 1;
                BehaveAndScanGEVI_optovin.StimEphysOscilloscopeControl.optoOngoing = 2;
                while (stopwatch.ElapsedMilliseconds < slmParam0.slmSeqLen)
                {
                    if (seqState == 0 & stopwatch.ElapsedMilliseconds < slmParam0.slmOffSeg)
                    {
                        float current_time = stopwatch.ElapsedMilliseconds;
                        pictureBox1.Image = pattern2;
                        seqState = 1;
                        //Console.WriteLine(current_time - time_past);
                        time_past = current_time;
                        BehaveAndScanGEVI_optovin.StimEphysOscilloscopeControl.optoOngoing = 2;
                    }
                    if (seqState == 0 & (stopwatch.ElapsedMilliseconds - time_past) > slmParam0.slmOffSeg)
                    {
                        float current_time = stopwatch.ElapsedMilliseconds;
                        pictureBox1.Image = pattern2;
                        seqState = 1;
                        //Console.WriteLine(current_time - time_past);
                        time_past = current_time;
                        BehaveAndScanGEVI_optovin.StimEphysOscilloscopeControl.optoOngoing = 2;
                    }
                    else if (seqState == 1 & (stopwatch.ElapsedMilliseconds - time_past) > slmParam0.slmOnSeg)
                    {
                        float current_time = stopwatch.ElapsedMilliseconds;
                        pictureBox1.Image = pattern0;
                        seqState = 0;
                        //Console.WriteLine(current_time - time_past);
                        time_past = current_time;
                        BehaveAndScanGEVI_optovin.StimEphysOscilloscopeControl.optoOngoing = 0;
                    }
                }
            }
            if (pictureBox1.Image != pattern0)
            {
                pictureBox1.Image = pattern0;
            }
            BehaveAndScanGEVI_optovin.StimEphysOscilloscopeControl.optoOngoing = 0;
            stopwatch.Stop();
        }

        private void DrawBlack(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(System.Drawing.Color.Black);
        }
    }
}
