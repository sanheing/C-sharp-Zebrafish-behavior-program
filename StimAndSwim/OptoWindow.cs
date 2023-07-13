using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BehaveAndScanGECI
{
    public partial class OptoWindow : Form
    {
        public System.Drawing.Bitmap[] patterns;
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
            patterns = new Bitmap[slmParam0.patternNum];
            for (int i = 0; i<=slmParam0.patternNum; i++)
            {
                patterns[i] = new Bitmap(slmParam0.slmPath + i.ToString() + ".png");
            }
            //pattern0 = new Bitmap(slmParam0.slmPath + "0.bmp");
            //pattern1 = new Bitmap(slmParam0.slmPath + "1.bmp");
            //pattern2 = new Bitmap(slmParam0.slmPath + "2.bmp");
            Console.WriteLine("slmParam loaded");
        }

        public void ShowSequenceNew(int seqNum)
        {
            float time_past = 0f;
            int seqState = 0;
            pictureBox1.Image = patterns[0];
            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            pictureBox1.Image = patterns[seqNum];
            seqState = seqNum;
            StimEphysOscilloscopeControl.optoOngoing = seqNum;
            while (stopwatch.ElapsedMilliseconds < slmParam0.slmSeqLen)
            {
                if (seqState == 0 & (stopwatch.ElapsedMilliseconds - time_past) > slmParam0.slmOffSeg)
                {
                    float current_time = stopwatch.ElapsedMilliseconds;
                    pictureBox1.Image = patterns[seqNum];
                    seqState = seqNum;
                    time_past = current_time;
                    StimEphysOscilloscopeControl.optoOngoing = seqNum;
                }
                else if (seqState != 0 & (stopwatch.ElapsedMilliseconds - time_past) > slmParam0.slmOnSeg)
                {
                    float current_time = stopwatch.ElapsedMilliseconds;
                    pictureBox1.Image = patterns[0];
                    seqState = 0;
                    //Console.WriteLine(current_time - time_past);
                    time_past = current_time;
                    StimEphysOscilloscopeControl.optoOngoing = 0;
                }
            }
            if (seqState != 0)
            {
                pictureBox1.Image = patterns[0];
                seqState = 0;
            }
            StimEphysOscilloscopeControl.optoOngoing = 0;
            stopwatch.Stop();
        }

        private void DrawBlack(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(System.Drawing.Color.Black);
        }
    }
}
