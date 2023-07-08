using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.IO;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Runtime.ConstrainedExecution;

namespace BehaveAndScanGEVI_optovin
{
    public partial class StimWindow : Form
    {
        float ww = 4f;//1f;//4f;  // size of landscape in x,y
        float hh = 3f;//2f;
        float h_ = 1f;//1f*1f;         // size of visible area
        float w_ = 4f/3f;//1f*4f / 3f;
        StimEphysOscilloscopeControl stimSender = null;
        public StimCheckWindow SCWindow;


        private Microsoft.DirectX.Direct3D.Device deviceP = null; // d3d11.dll
        private Microsoft.DirectX.Direct3D.Device deviceW = null; // d3d11.dll
        private VertexBuffer vertices1 = null;       // Vertex buffer for our drawing // d3d11.dll
        private VertexBuffer vertices2 = null;       // Vertex buffer for our drawing  // d3d11.dll
        private VertexBuffer vertices3 = null;       // Vertex buffer for our drawing // d3d11.dll
        private VertexBuffer vertices4 = null;       // Vertex buffer for our drawing  // d3d11.dll
        public static int TrialLen=0;
        private int[] TimeSeq; //new int[TrialLen*2]
        private int[] ObstaclesDur = new int[0];
        private int[] IntervalLen = new int[0];
        
        //Background texture management
        private VertexBuffer backgroundV1 = null;  // d3d11.dll
        private VertexBuffer backgroundV2 = null;    // Background vertex buffer  // d3d11.dll

        private CustomVertex.PositionTextured[] vertex = new CustomVertex.PositionTextured[4];
        private Texture textureP,textureW,whiteP,whiteW;

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private float JitterFrequency = 3f;//3f
        //private float JitterAmplitude = 0f;//0.02f
        private float TimeElapsed;
        public StimWindow()
        {
            InitializeComponent();
            if (!InitializeDirect3D())
                return;
            
            vertices1 = new VertexBuffer(typeof(CustomVertex.PositionColored), 20, deviceP, 0, CustomVertex.PositionColored.Format, Pool.Managed);
            vertices2 = new VertexBuffer(typeof(CustomVertex.PositionColored), 20, deviceW, 0, CustomVertex.PositionColored.Format, Pool.Managed);
            vertices3 = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), 20, deviceP, 0, CustomVertex.PositionColored.Format, Pool.Managed);
            vertices4 = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), 20, deviceW, 0, CustomVertex.PositionColored.Format, Pool.Managed);
            backgroundV1 = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), 4, deviceP, 0, CustomVertex.PositionColored.Format, Pool.Managed);
            backgroundV2 = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), 4, deviceW, 0, CustomVertex.PositionColored.Format, Pool.Managed);

            //textureW = new Texture(deviceW, new Bitmap("C:/Users/mulab-10/Desktop/4c BehaveAndScanGEVI fixLength_zs/texture.bmp"), 0, Pool.Managed);
            //textureP = new Texture(deviceP, new Bitmap("C:/Users/mulab-10/Desktop/4c BehaveAndScanGEVI fixLength_zs/texture.bmp"), 0, Pool.Managed);
            //whiteW = new Texture(deviceW, new Bitmap("C:/Users/mulab-10/Desktop/4c BehaveAndScanGEVI fixLength_zs/white.jpg"), 0, Pool.Managed);
            //whiteP = new Texture(deviceP, new Bitmap("C:/Users/mulab-10/Desktop/4c BehaveAndScanGEVI fixLength_zs/white.jpg"), 0, Pool.Managed);

            textureW = new Texture(deviceW, new Bitmap("//10.10.49.10/d/zhaos/C#code/4c BehaveAndScanGEVI fixLength_LS_zs/texture.bmp"), 0, Pool.Managed);
            textureP = new Texture(deviceP, new Bitmap("//10.10.49.10/d/zhaos/C#code/4c BehaveAndScanGEVI fixLength_LS_zs//texture.bmp"), 0, Pool.Managed);
            whiteW = new Texture(deviceW, new Bitmap("//10.10.49.10/d/zhaos/C#code/4c BehaveAndScanGEVI fixLength_LS_zs//white.jpg"), 0, Pool.Managed);
            whiteP = new Texture(deviceP, new Bitmap("//10.10.49.10/d/zhaos/C#code/4c BehaveAndScanGEVI fixLength_LS_zs//white.jpg"), 0, Pool.Managed);
            TrialLen = 0;
            for(int i = 0; i<Window1.trialInfoTable.Rows.Count;i++)
            {
                DataRow row = Window1.trialInfoTable.Rows[i];
                TrialLen += (int)row["trialLen"];
                string[] ObsDurType = ((string)row["obsdur"]).Split(',');
                string[] IntDurType = ((string)row["intdur"]).Split(',');
                int[] ObsDurType_ = new int[ObsDurType.Length * 2];
                int[] IntDurType_ = new int[IntDurType.Length];
                for(int j=0;j<ObsDurType.Length;j++)
                {
                    ObsDurType_[2 * j] = int.Parse(ObsDurType[j]);
                    ObsDurType_[2 * j+1] = -int.Parse(ObsDurType[j]);
                }
                for(int j =0; j < IntDurType.Length; j++)
                {
                    IntDurType_[j] = int.Parse(IntDurType[j]);
                }
                int[] addObstaclesDur = RandomSelect(ObsDurType_, (int)row["trialLen"]);
                int[] addIntervalLen = RandomSelect(IntDurType_, (int)row["trialLen"]);
                ObstaclesDur = ObstaclesDur.Concat(addObstaclesDur).ToArray();
                IntervalLen = IntervalLen.Concat(addIntervalLen).ToArray();
            }
            int AccumulateTime = 0;
            TimeSeq = new int[TrialLen * 2];
            for (int i=0;i<TrialLen*2;i++)
            {
                if(i%2==0)
                    AccumulateTime += Math.Abs(ObstaclesDur[i / 2]);
                else
                    AccumulateTime += Math.Abs(IntervalLen[(i-1) / 2]);
                TimeSeq[i] = AccumulateTime;
            }
           


            GraphicsStream stm1 = backgroundV1.Lock(0, 0, 0);     // Lock the background vertex list
            int clr1 = System.Drawing.Color.Transparent.ToArgb();
            stm1.Write(new CustomVertex.PositionColoredTextured(-ww / 3f, -hh / 3f, 0, clr1, 0, 1));   // here the size of the background
            stm1.Write(new CustomVertex.PositionColoredTextured(-ww / 3f, hh * 2f / 3f, 0, clr1, 0, 0));    // bmp is set, also the shape
            stm1.Write(new CustomVertex.PositionColoredTextured(ww * 2f / 3f, hh * 2f / 3f, 0, clr1, 1, 0));     // so needs to match with the bitmap file
            stm1.Write(new CustomVertex.PositionColoredTextured(ww * 2f / 3f, -hh / 3f, 0, clr1, 1, 1));

            backgroundV1.Unlock();


            GraphicsStream stm2 = backgroundV2.Lock(0, 0, 0);     // Lock the background vertex list
            int clr2 = System.Drawing.Color.Transparent.ToArgb();
            stm2.Write(new CustomVertex.PositionColoredTextured(-ww / 3f, -hh / 3f, 0, clr2, 0, 1));   // here the size of the background
            stm2.Write(new CustomVertex.PositionColoredTextured(-ww / 3f, hh * 2f / 3f, 0, clr2, 0, 0));    // bmp is set, also the shape
            stm2.Write(new CustomVertex.PositionColoredTextured(ww * 2f / 3f, hh * 2f / 3f, 0, clr2, 1, 0));     // so needs to match with the bitmap file
            stm2.Write(new CustomVertex.PositionColoredTextured(ww * 2f / 3f, -hh / 3f, 0, clr2, 1, 1));

            backgroundV2.Unlock();

            stopwatch.Start();
        }

        public int[] RandomSelect(int[] array, int n)
        {
            int[] result = new int[n];
            Random random = new Random();
            List<int> indices = new List<int>();
            for (int i = 0; i < array.Length; i++)
            {
                indices.Add(i);
            }
            for (int i = 0; i < n; i++)
            {
                int index = random.Next(indices.Count);
                result[i] = array[indices[index]];
            }
            return result;
        }

        private bool InitializeDirect3D()
        {
            SCWindow = new StimCheckWindow();
            SCWindow.Show();

            try
            {                
                PresentParameters presentParams = new PresentParameters();
                presentParams.Windowed = true;
                presentParams.SwapEffect = SwapEffect.Discard;
                deviceP = new Microsoft.DirectX.Direct3D.Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing, presentParams);
                deviceW = new Microsoft.DirectX.Direct3D.Device(0, DeviceType.Hardware, SCWindow, CreateFlags.SoftwareVertexProcessing, presentParams);      
            }
            catch (DirectXException)
            {
                return false;
            }
            return true;
        }


        public void Render(object sender)
        {
            stimSender = (StimEphysOscilloscopeControl)sender;
            DisplayClear(deviceP);
            DisplayClear(deviceW);


            if (stimSender.senderWindow.InstStimParams.moveobject == 1)   // display grating - more response probably
            {
                DrawGrating((float)stimSender.displayAngle, stimSender.stripesY, (byte)stimSender.GratingsContrast, stimSender.blevel, stimSender.dH, deviceP, vertices1);
                DrawGrating((float)stimSender.displayAngle, stimSender.stripesY, (byte)stimSender.GratingsContrast, stimSender.blevel, stimSender.dH, deviceW, vertices2);
            }

            if (stimSender.senderWindow.InstStimParams.moveobject == 2)   // display grating - more response probably
            {
                DrawCrosses((float)stimSender.displayAngle, stimSender.stripesX, stimSender.stripesY, (byte)stimSender.GratingsContrast, stimSender.blevel, stimSender.dH, deviceP, vertices1);
                DrawCrosses((float)stimSender.displayAngle, stimSender.stripesX, stimSender.stripesY, (byte)stimSender.GratingsContrast, stimSender.blevel, stimSender.dH, deviceW, vertices2);
            }

            if (stimSender.senderWindow.InstStimParams.moveobject == 3)   // display grating - more response probably
            {
                //DrawBox((byte)stimSender.GratingsContrast, stimSender.blevel, stimSender.boxpos, stimSender.dH, deviceP, vertices1);
                //DrawBox((byte)stimSender.GratingsContrast, stimSender.blevel, stimSender.boxpos, stimSender.dH, deviceW, vertices2);
                DrawBackground((float)stimSender.displayAngle, stimSender.stripesX, stimSender.stripesY, (byte)stimSender.GratingsContrast, stimSender.blevel, deviceP, vertices3, textureP);
                DrawBackground((float)stimSender.displayAngle, stimSender.stripesX, stimSender.stripesY, (byte)stimSender.GratingsContrast, stimSender.blevel, deviceW, vertices4, textureW);
                deviceP.SetTexture(0, whiteP);
                deviceW.SetTexture(0, whiteW);
            }

            if (stimSender.senderWindow.InstStimParams.moveobject == 4) // display pure red background 20230322
            {
                float[] xx = { 4/3f, 0f, 0f, 4/3f };
                float[] yy = { 1f, 1f, 0f, 0f };
                int clr = Color.FromArgb(0, 0, 255).ToArgb();
                drawPoly(vertices1, deviceW, xx, yy, clr);
                drawPoly(vertices2, deviceP, xx, yy, clr);
            }
                if (stimSender.senderWindow.displayWalls)
            {
                DrawWalls((float)stimSender.displayAngle, stimSender.leftWall, stimSender.rightWall, (byte)stimSender.ObstacleContrast, stimSender.blevel, 30f, deviceP, vertices1);
                DrawWalls((float)stimSender.displayAngle, stimSender.leftWall, stimSender.rightWall, (byte)stimSender.ObstacleContrast, stimSender.blevel, 30f, deviceW, vertices2);
            }
            if(stimSender.senderWindow.displayObstacles && stimSender.senderWindow.StationaryObstacles) // display stationary obstacle on the lateral side of fish 20230322
            {
                
                int clr = Color.FromArgb(0, 0, 0).ToArgb();
                float ObstacleHalfWidth = stimSender.obst / 2 * (float)(90.0 / 1000.0);
                float [] CenterPosition = { 1.4f - 0.925f, 0.5f };
                float xoffset = stimSender.senderWindow.xOffset;
                float yoffset = stimSender.senderWindow.yOffset;
                float[] Obstaclexx = { CenterPosition[0]-ObstacleHalfWidth - xoffset, CenterPosition[0] + ObstacleHalfWidth -xoffset, CenterPosition[0] + ObstacleHalfWidth - xoffset, CenterPosition[0] - ObstacleHalfWidth - xoffset };
                float[] Obstaclexx1 = { 1.4f - CenterPosition[0] - ObstacleHalfWidth - xoffset, 1.4f - CenterPosition[0] + ObstacleHalfWidth - xoffset, 1.4f - CenterPosition[0] + ObstacleHalfWidth - xoffset, 1.4f - CenterPosition[0] - ObstacleHalfWidth - xoffset };
                float[] Obstacleyy = { CenterPosition[1] - ObstacleHalfWidth - yoffset- stimSender.senderWindow.ObsYpos, CenterPosition[1] - ObstacleHalfWidth - yoffset - stimSender.senderWindow.ObsYpos, CenterPosition[1] + ObstacleHalfWidth - yoffset - stimSender.senderWindow.ObsYpos, CenterPosition[1] + ObstacleHalfWidth - yoffset - stimSender.senderWindow.ObsYpos };
                int time_eq = stimSender.tt % TimeSeq[TimeSeq.Length-1];
                if (Math.Floor((double)stimSender.tt / TimeSeq[TimeSeq.Length - 1]) * TrialLen * 2 > stimSender.senderWindow.trialnumber)
                    stimSender.senderWindow.trialnumber++;
                if (stimSender.senderWindow.trialnumber % (TrialLen * 2) == 0)
                    stimSender.senderWindow.currTrialgoingT = time_eq;
                else
                    stimSender.senderWindow.currTrialgoingT = time_eq - TimeSeq[stimSender.senderWindow.trialnumber % (TrialLen * 2) - 1];
                if (time_eq < TimeSeq[stimSender.senderWindow.trialnumber%(TrialLen*2)])
                {
                    if(stimSender.senderWindow.trialnumber%2==0)
                    {
                        if(ObstaclesDur[stimSender.senderWindow.trialnumber% (TrialLen * 2) / 2]<0)
                        { 
                            if (!stimSender.optoStim[StimEphysOscilloscopeControl.currBlock])
                            {
                                drawPoly(vertices1, deviceW, Obstaclexx, Obstacleyy, clr);
                                drawPoly(vertices2, deviceP, Obstaclexx, Obstacleyy, clr);
                            }
                        }
                        else
                        {
                            if (!stimSender.optoStim[StimEphysOscilloscopeControl.currBlock])
                            {
                                drawPoly(vertices1, deviceW, Obstaclexx1, Obstacleyy, clr);
                                drawPoly(vertices2, deviceP, Obstaclexx1, Obstacleyy, clr);
                            }
                        }
                        stimSender.senderWindow.CurrObstacleDur = ObstaclesDur[stimSender.senderWindow.trialnumber% (TrialLen * 2) / 2];
                    }
                    else
                    {
                        stimSender.senderWindow.CurrObstacleDur = IntervalLen[stimSender.senderWindow.trialnumber% (TrialLen * 2) / 2];
                    }
                }
                else
                { stimSender.senderWindow.trialnumber++; }
                stimSender.senderWindow.RecordTime = stimSender.tt;
            }
            if (stimSender.senderWindow.displayObstacles && (!stimSender.senderWindow.StationaryObstacles))
            {
                TimeElapsed = (float)stopwatch.ElapsedMilliseconds / 1000;
                Console.WriteLine(TimeElapsed);
                if (stimSender.isJittering)
                {
                    DrawObstacles((float)stimSender.displayAngle, stimSender.xNext, (float)(stimSender.yNext + Math.Sin(TimeElapsed * JitterFrequency * 2 * Math.PI) * stimSender.senderWindow.JitterAmplitude), stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceP, vertices1);
                    DrawObstacles((float)stimSender.displayAngle, stimSender.xNext, (float)(stimSender.yNext + Math.Sin(TimeElapsed * JitterFrequency * 2 * Math.PI) * stimSender.senderWindow.JitterAmplitude), stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceW, vertices2);
                    DrawObstacles((float)stimSender.displayAngle, stimSender.xLast, (float)(stimSender.yLast + Math.Sin(TimeElapsed * JitterFrequency * 2 * Math.PI) * stimSender.senderWindow.JitterAmplitude), stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceP, vertices1);
                    DrawObstacles((float)stimSender.displayAngle, stimSender.xLast, (float)(stimSender.yLast + Math.Sin(TimeElapsed * JitterFrequency * 2 * Math.PI) * stimSender.senderWindow.JitterAmplitude), stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceW, vertices2);
                }
                else
                {
                    DrawObstacles((float)stimSender.displayAngle, stimSender.xNext, stimSender.yNext, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceP, vertices1);
                    DrawObstacles((float)stimSender.displayAngle, stimSender.xNext, stimSender.yNext, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceW, vertices2);
                    DrawObstacles((float)stimSender.displayAngle, stimSender.xLast, stimSender.yLast, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceP, vertices1);
                    DrawObstacles((float)stimSender.displayAngle, stimSender.xLast, stimSender.yLast, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceW, vertices2);
                }
                stimSender.senderWindow.CurrObstacleDur = 0;

                //DrawObstGrating((float)stimSender.displayAngle, stimSender.xNext, stimSender.yNext, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH_obst, deviceP, vertices1);
                //DrawObstGrating((float)stimSender.displayAngle, stimSender.xNext, stimSender.yNext, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH_obst, deviceW, vertices2);
                //DrawObstGrating((float)stimSender.displayAngle, stimSender.xLast, stimSender.yLast, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH_obst, deviceP, vertices1);
                //DrawObstGrating((float)stimSender.displayAngle, stimSender.xLast, stimSender.yLast, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH_obst, deviceW, vertices2);
                //DrawObstaclesTexture((float)stimSender.displayAngle, stimSender.xNext, stimSender.yNext, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceP, vertices3, textureP);
                //DrawObstaclesTexture((float)stimSender.displayAngle, stimSender.xNext, stimSender.yNext, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceW, vertices4, textureW);
                //DrawObstaclesTexture((float)stimSender.displayAngle, stimSender.xLast, stimSender.yLast, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceP, vertices3, textureP);
                //DrawObstaclesTexture((float)stimSender.displayAngle, stimSender.xLast, stimSender.yLast, stimSender.obst, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceW, vertices4, textureW);
            }
            DrawFish((float)stimSender.displayAngle, stimSender.xFish, stimSender.yFish, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceW, vertices2);   
            if (stimSender.senderWindow.displayFish)
            {
                DrawFish((float)stimSender.displayAngle, stimSender.xFish, stimSender.yFish, (byte)stimSender.ObstacleContrast, stimSender.blevel, stimSender.dH, deviceP, vertices1);
            }

            Flip(deviceP);
            Flip(deviceW);         
        }


        public void DisplayClear(Microsoft.DirectX.Direct3D.Device device)
        {
            if (device == null)
                return;

            device.Clear(ClearFlags.Target, System.Drawing.Color.Black, 1.0f, 0);
            device.RenderState.ZBufferEnable = false;   // We'll not use this feature
            device.RenderState.Lighting = false;        // Or this one...
            device.RenderState.CullMode = Cull.None;    // Or this one...

            //Begin the scene
            device.BeginScene();
        }


        public void DrawWalls(float displayAngle, float leftWall, float rightWall, byte contrast, int blevel, float dH, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice)
        {
            float W = 3f;// /1.6f since bigger screen


            if (contrast > 0)
            {
                float H = leftWall - dH;
                float[] xx = {
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*(-W) + Math.Sin(displayAngle+(Math.PI/2))*H),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*W + Math.Sin(displayAngle+(Math.PI/2))*H),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*W + Math.Sin(displayAngle+(Math.PI/2))*(H+dH)),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*(-W) + Math.Sin(displayAngle+(Math.PI/2))*(H+dH)),
                          };

                float[] yy = {
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*(-W) + Math.Cos(displayAngle+(Math.PI/2))*H),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*W + Math.Cos(displayAngle+(Math.PI/2))*H),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*W + Math.Cos(displayAngle+(Math.PI/2))*(H+dH)),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*(-W) + Math.Cos(displayAngle+(Math.PI/2))*(H+dH)),
                          };

                int clr = Color.FromArgb(contrast, contrast, contrast).ToArgb();
                drawPoly(vertice, device, xx, yy, clr);


                H = rightWall;
                float[] xx1 = {
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*(-W) + Math.Sin(displayAngle+(Math.PI/2))*H),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*W + Math.Sin(displayAngle+(Math.PI/2))*H),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*W + Math.Sin(displayAngle+(Math.PI/2))*(H+dH)),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*(-W) + Math.Sin(displayAngle+(Math.PI/2))*(H+dH)),
                          };

                float[] yy1 = {
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*(-W) + Math.Cos(displayAngle+(Math.PI/2))*H),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*W + Math.Cos(displayAngle+(Math.PI/2))*H),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*W + Math.Cos(displayAngle+(Math.PI/2))*(H+dH)),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*(-W) + Math.Cos(displayAngle+(Math.PI/2))*(H+dH)),
                          };

                clr = Color.FromArgb(contrast, contrast, contrast).ToArgb();
                drawPoly(vertice, device, xx1, yy1, clr);
            }
            else
            {
                DrawWhite(blevel, device, vertice);
            }

        }


        public void DrawObstacles(float displayAngle, float xPos, float yPos, float size, byte contrast, int blevel, float dH, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice)
        {
            float W = size / 2 * (float)(90.0 / 1000.0);// /1.6f since bigger screen


            if (contrast > 0)
            {
                float[] xx = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

                float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };

                int clr = Color.FromArgb(contrast, contrast, contrast).ToArgb();
                drawPoly(vertice, device, xx, yy, clr);
            }
            else
            {
                DrawWhite(blevel, device, vertice);
            }

        }


        public void DrawObstaclesTexture(float displayAngle, float xPos, float yPos, float size, byte contrast, int blevel, float dH, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice, Texture texture)
        {
            float W = size / 2 * (float)(90.0 / 1000.0);// /1.6f since bigger screen



                float[] xx = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

                float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };


            //vertex[0] = new CustomVertex.PositionTextured(new Vector3((float)(Math.Cos(displayAngle) * (xPos - W) + Math.Sin(displayAngle) * (xPos - W)), (float)(-Math.Sin(displayAngle) * (yPos - W) + Math.Cos(displayAngle) * (yPos - W)), 1), 0, -1);
            //vertex[1] = new CustomVertex.PositionTextured(new Vector3((float)(Math.Cos(displayAngle) * (xPos + W) + Math.Sin(displayAngle) * (xPos - W)), (float)(-Math.Sin(displayAngle) * (yPos + W) + Math.Cos(displayAngle) * (yPos - W)), 1), 1, 0);
            //vertex[2] = new CustomVertex.PositionTextured(new Vector3((float)(Math.Cos(displayAngle) * (xPos + W) + Math.Sin(displayAngle) * (xPos + W)), (float)(-Math.Sin(displayAngle) * (yPos + W) + Math.Cos(displayAngle) * (yPos + W)), 1), 0, 1);
            //vertex[3] = new CustomVertex.PositionTextured(new Vector3((float)(Math.Cos(displayAngle) * (xPos - W) + Math.Sin(displayAngle) * (xPos + W)), (float)(-Math.Sin(displayAngle) * (yPos - W) + Math.Cos(displayAngle) * (yPos + W)), 1), -1, 0);
            //drawTexture(vertex, device, texture);
            int clr = Color.FromArgb(50,50,50).ToArgb();
            drawTexture(vertice, device, xx, yy, clr, texture);

        }

        public void DrawFish(float displayAngle, float xPos, float yPos, byte contrast, int blevel, float dH, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice)
        {
            float W = stimSender.fishWidth / 2 * (float)(90.0 / 1000.0);
            float L = stimSender.fishLength / 2 * (float)(90.0 / 1000.0);


            if (contrast > 0)
            {
                float[] xx = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-L)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-L)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+L)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+L)),
                          };

                float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-L)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-L)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+L)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+L)),
                          };

                int clr = Color.FromArgb(0, contrast, contrast).ToArgb();
                drawPoly(vertice, device, xx, yy, clr);
            }
            else
            {
                DrawWhite(blevel, device, vertice);
            }
        }

        public void DrawGrating(float displayAngle, float yPos, byte contrast, int blevel, float dH, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice)
        {
            float W = 3f;// /1.6f since bigger screen

            yPos = yPos % (2 * dH);

            if (contrast > 0)
            {
                for (float H = -1.5f + yPos - 1f; H < 1.5 + yPos; H += 2 * dH)
                {
                    float[] xx = {
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+dH)),
                          };

                    float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+dH)),
                          };

                    int clr2 = Color.FromArgb(0, 0, 255).ToArgb();
                    drawPoly(vertice, device, xx, yy, clr2);


                    float[] xx1 = {
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+2*dH)),
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+2*dH)),
                          };

                    float[] yy1 = {
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+2*dH)),
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+2*dH)),
                          };

                    int clr = Color.FromArgb(0, 0, contrast).ToArgb();
                    drawPoly(vertice, device, xx1, yy1, clr);

                }
            }
            else
            {
                DrawWhite(blevel, device, vertice);
            }

        }

        public void DrawObstGrating(float displayAngle, float xPos, float yPos, float size, byte contrast, int blevel, float dH, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice)
        {
            float W = size / 2 * (float)(90.0 / 1000.0);// /1.6f since bigger screen

            if (contrast > 0)
            {
                for (float H = yPos-W; H < yPos+W; H += 3 * dH)
                {
                    float[] xx = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(H+dH)),
                          };

                    float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(xPos-W) + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*(xPos+W) + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*(xPos+W) + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*(xPos-W) + Math.Cos(displayAngle)*(H+dH)),
                          };


                    int clr2 = Color.FromArgb(contrast, contrast, contrast).ToArgb();
                    drawPoly(vertice, device, xx, yy, clr2);


                    float[] xx1 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(H+3*dH)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(H+3*dH)),
                          };

                    float[] yy1 = {
                          (float)(-Math.Sin(displayAngle)*(xPos-W) + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*(xPos+W) + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*(xPos+W) + Math.Cos(displayAngle)*(H+3*dH)),
                          (float)(-Math.Sin(displayAngle)*(xPos-W) + Math.Cos(displayAngle)*(H+3*dH)),
                          };

                    int clr = Color.FromArgb(255-contrast, 255-contrast, 255-contrast).ToArgb();
                    drawPoly(vertice, device, xx1, yy1, clr);

                }
            }
            else
            {
                DrawWhite(blevel, device, vertice);
            }

        }


        public void DrawBox(byte contrast, int blevel, int boxpos, float dH, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice)
        {

            DrawWhite(128, device, vertice);
            if (boxpos == 1)
            {
                float[] xx = { 1.0f, 1.1f, 1.1f, 1.0f }; 
                float[] yy = { 0.8f, 0.8f, 0.7f, 0.7f };
                int clr = Color.FromArgb(255, 0, 0).ToArgb();
                drawPoly(vertice, device, xx, yy, clr);
            }
            if (boxpos == 2)
            {
                float[] xx = { 1.0f, 1.1f, 1.1f, 1.0f }; 
                float[] yy = { 0.65f , 0.65f , 0.55f , 0.55f  };
                int clr = Color.FromArgb(255, 0, 0).ToArgb();
                drawPoly(vertice, device, xx, yy, clr);
            }
            if (boxpos == 3)
            {
                float[] xx = { 0.7f, 0.8f, 0.8f, 0.7f };
                float[] yy = { 0.8f, 0.8f, 0.7f, 0.7f };
                int clr = Color.FromArgb(255, 0, 0).ToArgb();
                drawPoly(vertice, device, xx, yy, clr);
            }
            if (boxpos == 4)
            {
                float[] xx = { 0.7f, 0.8f, 0.8f, 0.7f };
                float[] yy = { 0.65f, 0.65f , 0.55f , 0.55f };
                int clr = Color.FromArgb(255, 0, 0).ToArgb();
                drawPoly(vertice, device, xx, yy, clr);
            }  
        }


        public void DrawBackground(float displayAngle, float xpos, float ypos, byte contrast, int blevel, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice, Texture texture)
        {
            float xPos = xpos % 4f + 4f;
            float yPos = ypos % 4f - 3f;
            float W = 2f;
            int clr = Color.FromArgb(255,255,255).ToArgb();

            float[] xx = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx, yy, clr, texture);

            xPos = xPos - 3.98f;
            float[] xx1 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy1 = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx1, yy1, clr, texture);

            xPos = xPos - 3.98f;
            float[] xx2 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy2 = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx2, yy2, clr, texture);

            xPos = xPos + 7.96f;
            yPos = yPos + 3.98f;
            float[] xx3 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy3 = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx3, yy3, clr, texture);

            xPos = xPos - 3.98f;
            float[] xx4 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy4 = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx4, yy4, clr, texture);

            xPos = xPos - 3.98f;
            float[] xx5 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy5 = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx5, yy5, clr, texture);

            xPos = xPos + 7.96f;
            yPos = yPos + 3.98f;
            float[] xx6 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy6 = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx6, yy6, clr, texture);

            xPos = xPos - 3.98f;
            float[] xx7 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy7 = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx7, yy7, clr, texture);

            xPos = xPos - 3.98f;
            float[] xx8 = {
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos-W)),
                          (float)(Math.Cos(displayAngle)*(xPos+W) + Math.Sin(displayAngle)*(xPos+W)),
                          (float)(Math.Cos(displayAngle)*(xPos-W) + Math.Sin(displayAngle)*(xPos+W)),
                          };

            float[] yy8 = {
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos-W)),
                          (float)(-Math.Sin(displayAngle)*(yPos+W) + Math.Cos(displayAngle)*(yPos+W)),
                          (float)(-Math.Sin(displayAngle)*(yPos-W) + Math.Cos(displayAngle)*(yPos+W)),
                          };
            drawTexture(vertice, device, xx8, yy8, clr, texture);
        }


        public void DrawCrosses(float displayAngle, float xPos, float yPos, byte contrast, int blevel, float dH, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice)
        {
            float W = 3f;// /1.6f since bigger screen

            xPos = xPos % (2 * dH);
            yPos = yPos % (2 * dH);

            if (contrast > 0)
            {   
                DrawWhite(blevel, device, vertice);
                for (float H = -1.5f + yPos - 1f; H < 1.5 + yPos; H += 2 * dH)
                {
                    float[] xx = {
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*H),
                          (float)(Math.Cos(displayAngle)*W + Math.Sin(displayAngle)*(H+dH)),
                          (float)(Math.Cos(displayAngle)*(-W) + Math.Sin(displayAngle)*(H+dH)),
                          };

                    float[] yy = {
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*H),
                          (float)(-Math.Sin(displayAngle)*W + Math.Cos(displayAngle)*(H+dH)),
                          (float)(-Math.Sin(displayAngle)*(-W) + Math.Cos(displayAngle)*(H+dH)),
                          };

                    int clr = Color.FromArgb(contrast, contrast, contrast).ToArgb();
                    drawPoly(vertice, device, xx, yy, clr);
                }


                for (float H = -1.5f + xPos - 1f; H < 1.5 + xPos; H += 2 * dH)
                {
                    float[] xx1 = {
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*(-W) + Math.Sin(displayAngle+(Math.PI/2))*H),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*W + Math.Sin(displayAngle+(Math.PI/2))*H),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*W + Math.Sin(displayAngle+(Math.PI/2))*(H+dH)),
                          (float)(Math.Cos(displayAngle+(Math.PI/2))*(-W) + Math.Sin(displayAngle+(Math.PI/2))*(H+dH)),
                          };

                    float[] yy1 = {
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*(-W) + Math.Cos(displayAngle+(Math.PI/2))*H),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*W + Math.Cos(displayAngle+(Math.PI/2))*H),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*W + Math.Cos(displayAngle+(Math.PI/2))*(H+dH)),
                          (float)(-Math.Sin(displayAngle+(Math.PI/2))*(-W) + Math.Cos(displayAngle+(Math.PI/2))*(H+dH)),
                          };


                    int clr = Color.FromArgb(contrast, contrast, contrast).ToArgb();
                    drawPoly(vertice, device, xx1, yy1, clr);

                }

            }
            else
            {
                DrawWhite(blevel, device, vertice);
            }

        }

        public void DrawWhite(int blevel, Microsoft.DirectX.Direct3D.Device device, VertexBuffer vertice)
        {
            float[] xx = { -5, 5, 5, -5 };
            float[] yy = { 5, 5, -5, -5 };
            int clr = Color.FromArgb(blevel, blevel, blevel).ToArgb();
            drawPoly(vertice, device, xx, yy, clr);
        }



        public void Flip(Microsoft.DirectX.Direct3D.Device device)
        {
            device.Transform.Projection = Matrix.OrthoOffCenterLH(0, w_, 0, h_, 0, 1);
            device.EndScene();
            device.Present();
        }

        public void drawTexture(VertexBuffer vertice, Microsoft.DirectX.Direct3D.Device device, float[] x, float[] y, int clr, Texture texture)
        {
            GraphicsStream gs2 = vertice.Lock(0, 0, 0);     // Lock the vertex list

            gs2.Write(new CustomVertex.PositionColoredTextured(new Vector3(x[0], y[0], 1), clr, 1, 0));
            gs2.Write(new CustomVertex.PositionColoredTextured(new Vector3(x[1], y[1], 1), clr, 0, 0));
            gs2.Write(new CustomVertex.PositionColoredTextured(new Vector3(x[2], y[2], 1), clr, 0, 1));
            gs2.Write(new CustomVertex.PositionColoredTextured(new Vector3(x[3], y[3], 1), clr, 1, 1));

            //gs2.Write(new CustomVertex.PositionColoredTextured(new Vector3(0, 1, 1), clr, 0, 0));
            //gs2.Write(new CustomVertex.PositionColoredTextured(new Vector3(-1, -1, 1), clr, -1, 0));
            //gs2.Write(new CustomVertex.PositionColoredTextured(new Vector3(1, -1, 1), clr, 0, -1));

            vertice.Unlock();
            device.SetTexture(0, texture);
            device.SetStreamSource(0, vertice, 0);
            device.VertexFormat = CustomVertex.PositionColoredTextured.Format;
            device.DrawPrimitives(PrimitiveType.TriangleFan, 0, x.Length - 2);
        }

        //public void drawTexture(CustomVertex.PositionTextured[] vertice, Microsoft.DirectX.Direct3D.Device device, Texture texture)
        //{
        //    device.SetTexture(0, texture);
        //    device.VertexFormat = CustomVertex.PositionTextured.Format;
        //    device.DrawUserPrimitives(PrimitiveType.TriangleFan, 2, vertice);
        //}

        public void drawPoly(VertexBuffer vertice, Microsoft.DirectX.Direct3D.Device device, float[] x, float[] y, int clr)
        {
            GraphicsStream gs2 = vertice.Lock(0, 0, 0);     // Lock the vertex list

            for (int i = 0; i < x.Length; i++)
            {
                gs2.Write(new CustomVertex.PositionColored(x[i], y[i], 0, clr));
            }

            vertice.Unlock();
            device.SetStreamSource(0, vertice, 0);
            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.DrawPrimitives(PrimitiveType.TriangleFan, 0, x.Length - 2);
        }



        private void StimWindow_Load(object sender, EventArgs e)
        {

        }

        public void CloseWindow()
        {
            SCWindow.Dispose();
            SCWindow.Close();
            SCWindow = null;

        }

    }
}
