using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using NationalInstruments.DAQmx;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace BehaveAndScanGECI
{
    public class Oscilloscope
    {
        WriteableBitmap oscillIm1;
        WriteableBitmap oscillIm2;
        WriteableBitmap oscillIm3;
        WriteableBitmap oscillIm4;
        /*-----------------------*/
        WriteableBitmap oscillIm5;
        /*-----------------------*/
        byte[, ,] imageArrays;
        byte[,] imageArray1;  
        int width, height;
        Window1 senderWindow;
        double[,] displayData;
        int nRecSamples = 0;
        int cnt = 0;

        int num = 5;   

        public Oscilloscope(Window1 sender, int width_, int height_)
        {
            senderWindow = (Window1)sender;
            width = width_;
            height = height_;

            imageArray1 = new byte[width, height];
            imageArrays = new byte[num, width, height];
            displayData = new double[num, 290];//400];

            senderWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                oscillIm1 = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
                senderWindow.oscilloscopeImage1.Source = oscillIm1;
                oscillIm2 = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
                senderWindow.oscilloscopeImage2.Source = oscillIm2;
                oscillIm3 = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
                senderWindow.oscilloscopeImage3.Source = oscillIm3;
                oscillIm4 = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
                senderWindow.oscilloscopeImage4.Source = oscillIm4;
                /*-----------------------------------------------------------------------------------------------------*/
                oscillIm5 = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
                senderWindow.oscilloscopeImage5.Source = oscillIm5;
                /*-----------------------------------------------------------------------------------------------------*/
            }));
        }


        public void RefreshGraph(uint[,] data)
        {
            double[,] data2 = new double[num, data.GetLength(1)];

            for (int i = 0; i < data.GetLength(1); i++)
                for (int j = 0; j < num; j++)
                    data2[j, i] = (double)data[j, i];
            RefreshGraph(data2);
        }

        public void RefreshGraph(double[,] data)
        {
            displayData = data;

            int l = data.GetLength(1);
            int L = l;

            nRecSamples += l;

            double[] mx = new double[num];
            double[] mn = new double[num];
            for (int i = 0; i < num; i++)
            {
                mx[i] = data[i, 1];
                mn[i] = data[i, 1];
            }

            //find max and min in each channel
            for (int i = 0; i < L; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    if (data[j, i] > mx[j])
                        mx[j] = data[j, i];

                    if (data[j, i] < mn[j])
                        mn[j] = data[j, i];
                }
            }


            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < num; k++)
                    {
                        imageArrays[k, i, j] = 0;
                    }
                }
            }

            int y = 0; 

            int[] x = new int[num];
            int[] x_prev = new int[num];
            for (int i = 0; i < num; i++)
            {
                x[i] = 0;
                x_prev[i] = 0;
            }

            for (int i = 0; i < L; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    x_prev[j] = x[j];
                    x[j] = (int)(2.0 + (data[j, i] - mn[j]) / (mx[j] - mn[j]) * (double)(width - 4)); //normalize
                }
                y = (int)(2.0 + (double)i / (double)data.GetLength(1) * (double)(height - 4));

                if (i > 0)
                {
                    for (int k = 0; k < num; k++)
                    {

                        if (x_prev[k] < x[k])
                            for (int j = x_prev[k]; j <= x[k]; j++)
                            {
                                if (width - 1 - j < width && width - 1 - j >= 0)
                                    imageArrays[k, width - 1 - j, y] = 100;
                            }
                        else if (x_prev[k] > x[k])
                            for (int j = x_prev[k]; j >= x[k]; j--)
                            {
                                if (width - 1 - j < width && width - 1 - j >= 0)
                                    imageArrays[k, width - 1 - j, y] = 255;
                            }
                        else
                            if (width - 1 - x[k] < width && width - 1 - x[k] >= 0)
                                imageArrays[k, width - 1 - x[k], y] = 255;
                            if (width - 1 - x[k]-1 < width && width - 1 - x[k]-1 >= 0)
                                imageArrays[k, width - 1 - x[k] - 1, y] = 255;
                            if (width - 1 - x[k] + 1 < width && width - 1 - x[k] + 1 >= 0)
                                imageArrays[k, width - 1 - x[k] + 1, y] = 255;
                    }
                }
            }


            cnt += l;
            if (cnt > L)
            {
                cnt -= L;

                senderWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    for (int k = 0; k < num; k++)
                    {
                        for (int i = 0; i < imageArrays.GetLength(1); i++)
                            for (int j = 0; j < imageArrays.GetLength(2); j++)
                                imageArray1[i, j] = imageArrays[k, i, j];

                        if (k == 0)
                            oscillIm1.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 1)
                            oscillIm2.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 2)
                            oscillIm3.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 3)
                            oscillIm4.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        /*----------------------------------------------------------------------------------*/
                        else if (k == 4)
                            oscillIm5.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        /*----------------------------------------------------------------------------------*/
                    }

                }));

                senderWindow.oscULValue.ch1UpLim = mx[0];
                senderWindow.oscULValue.ch1LoLim = mn[0];
                senderWindow.oscULValue.ch2UpLim = mx[1];
                senderWindow.oscULValue.ch2LoLim = mn[1];
                senderWindow.oscULValue.ch3UpLim = mx[2];
                senderWindow.oscULValue.ch3LoLim = mn[2];
                senderWindow.oscULValue.ch4UpLim = mx[3];
                senderWindow.oscULValue.ch4LoLim = mn[3];
                senderWindow.oscULValue.ch5UpLim = mx[4];
                senderWindow.oscULValue.ch5LoLim = mn[4];

            }
        }


        public void RefreshGraph(double[,] data, double gheight)
        {
            displayData = data;

            int l = data.GetLength(1);
            int L = l;

            nRecSamples += l;

            double[] mx = new double[num];
            double[] mn = new double[num];
           

            for (int j = 0; j < num; j++)
            {

                mx[j] = gheight;
                mn[j] = -gheight;
            }



            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < num; k++)
                    {
                        imageArrays[k, i, j] = 0;
                    }
                }
            }

            int y = 0;

            int[] x = new int[num];
            int[] x_prev = new int[num];
            for (int i = 0; i < num; i++)
            {
                x[i] = 0;
                x_prev[i] = 0;
            }

            for (int i = 0; i < L; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    x_prev[j] = x[j];
                    x[j] = (int)(2.0 + (data[j, i] - mn[j]) / (mx[j] - mn[j]) * (double)(width - 4));
                    y = (int)(2.0 + (double)i / (double)data.GetLength(1) * (double)(height - 4));
                }

                if (i > 0)
                {
                    for (int k = 0; k < num; k++)
                    {

                        if (x_prev[k] < x[k])
                            for (int j = x_prev[k]; j <= x[k]; j++)
                            {
                                if (width - 1 - j < width && width - 1 - j >= 0)
                                    imageArrays[k, width - 1 - j, y] = 255;
                            }
                        else if (x_prev[k] > x[k])
                            for (int j = x_prev[k]; j >= x[k]; j--)
                            {
                                if (width - 1 - j < width && width - 1 - j >= 0)
                                    imageArrays[k, width - 1 - j, y] = 255;
                            }
                        else
                            if (width - 1 - x[k] < width && width - 1 - x[k] >= 0)
                                imageArrays[k, width - 1 - x[k], y] = 255;
                        /*
                            if (width - 1 - x[k] - 1 < width && width - 1 - x[k] - 1 >= 0)
                                imageArrays[k, width - 1 - x[k] - 1, y] = 255;
                            if (width - 1 - x[k] + 1 < width && width - 1 - x[k] + 1 >= 0)
                                imageArrays[k, width - 1 - x[k] + 1, y] = 255;
                         */
                    }
                }
            }


            cnt += l;
            if (cnt > L)
            {
                cnt -= L;

                senderWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    for (int k = 0; k < num; k++)
                    {
                        for (int i = 0; i < imageArrays.GetLength(1); i++)
                            for (int j = 0; j < imageArrays.GetLength(2); j++)
                                imageArray1[i, j] = imageArrays[k, i, j];

                        if (k == 0)
                            oscillIm1.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 1)
                            oscillIm2.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 2)
                            oscillIm3.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 3)
                            oscillIm4.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        /*----------------------------------------------------------------------------------*/
                        else if (k == 4)
                            oscillIm5.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        /*----------------------------------------------------------------------------------*/
                    }

                }));

                senderWindow.oscULValue.ch1UpLim = gheight;
                senderWindow.oscULValue.ch1LoLim = -gheight;
                senderWindow.oscULValue.ch2UpLim = gheight;
                senderWindow.oscULValue.ch2LoLim = -gheight;
                senderWindow.oscULValue.ch3UpLim = gheight;
                senderWindow.oscULValue.ch3LoLim = -gheight;
                senderWindow.oscULValue.ch4UpLim = gheight;
                senderWindow.oscULValue.ch4LoLim = -gheight;
                /*----------------------------------------------*/
                senderWindow.oscULValue.ch5UpLim = gheight;
                senderWindow.oscULValue.ch5LoLim = -gheight;
                /*----------------------------------------------*/
            }
        }


        public void RefreshGraph(double[,] data, double[] thresh, double[] mean_, double[] min_)  // overloaded method for also displaying thresholds
        {
            displayData = data;
            int l = data.GetLength(1);
            int L = l;
            nRecSamples += l;

            double[] mx = new double[num];
            double[] mn = new double[num];

            for (int k = 0; k < num; k++)
            {
                mx[k] = thresh[k];
                mn[k] = min_[k];
            }

            for (int i = 0; i < L; i++)
            {
                for (int k = 0; k < num; k++)
                {
                    if (displayData[k, i] > mx[k])
                        mx[k] = displayData[k, i];
                    if (displayData[k, i] < mn[k])
                        mn[k] = displayData[k, i];
                }
            }


            for (int k = 0; k < num; k++)
            {
                if (mx[k] > 0)
                    mx[k] = mx[k] + (mx[k] - mn[k]) * .2;
            }

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    for (int kk = 0; kk < num; kk++)
                        imageArrays[kk, i, j] = 0;

            int y = 0;

            int[] x = new int[num];
            int[] x_prev = new int[num];

            for (int i = 0; i < num; i++)
            {
                x[i] = 0;
                x_prev[i] = 0;
            }

            int[] th = new int[num];
            int[] mean = new int[num];
            int[] min = new int[num];

            for (int i = 0; i < num; i++)
            {
                th[i] = (int)(2 + (thresh[i] - mn[i]) / (mx[i] - mn[i]) * (double)(width - 4));
                mean[i] = (int)(2 + (mean_[i] - mn[i]) / (mx[i] - mn[i]) * (double)(width - 4));
                min[i] = (int)(2 + (min_[i] - mn[i]) / (mx[i] - mn[i]) * (double)(width - 4));
            }

            //Console.WriteLine(displayData.GetLength(1));
            for (int i = 0; i < L; i++)
            {
                for (int k = 0; k < num; k++)
                {
                    x_prev[k] = x[k];
                    x[k] = (int)(2 + (displayData[k, i] - mn[k]) / (mx[k] - mn[k]) * (double)(width - 4));
                }
                y = (int)(2 + (double)i / (double)displayData.GetLength(1) * (height - 4));


                if (i > 0)
                {
                    for (int k = 0; k < num; k++)
                    {
                        if (x_prev[k] < x[k])
                            for (int j = x_prev[k]; j < x[k]; j++)
                            {
                                if (width - 1 - j < 800 && width - 1 - j >= 0)
                                    imageArrays[k, width - 1 - j, y] = 255;
                            }
                        else if (x_prev[k] > x[k])
                            for (int j = x_prev[k]; j > x[k]; j--)
                            {
                                if (width - 1 - j < 800 && width - 1 - j >= 0)
                                    imageArrays[k, width - 1 - j, y] = 255;
                            }
                        else
                            if (width - 1 - x[k] < 800 && width - 1 - x[k] >= 0)
                            imageArrays[k, width - 1 - x[k], y] = 255;
                    }
                }


                for (int k = 0; k < num; k++)
                {
                    if (width - 1 - x[k] < 800 && width - 1 - x[k] >= 0)
                        imageArrays[k, width - 1 - x[k], y] = 255;

                    if (width - 1 - th[k] < 800 && width - 1 - th[k] >= 0)
                        imageArrays[k, width - 1 - th[k], y] = 255;
                    if (width - 1 - th[k] + 1 < 800 && width - 1 - th[k] + 1 >= 0)
                        imageArrays[k, width - 1 - th[k] + 1, y] = 255;
                    if (width - 1 - th[k] + 2 < 800 && width - 1 - th[k] + 2 >= 0)
                        imageArrays[k, width - 1 - th[k] + 2, y] = 255;
                    if (width - 1 - th[k] + 3 < 800 && width - 1 - th[k] + 3 >= 0)
                        imageArrays[k, width - 1 - th[k] + 3, y] = 255;

                    if (width - 1 - mean[k] < 800 && width - 1 - mean[k] >= 0)
                        imageArrays[k, width - 1 - mean[k], y] = 255;
                    if (width - 1 - mean[k] + 1 < 800 && width - 1 - mean[k] + 1 >= 0)
                        imageArrays[k, width - 1 - mean[k] + 1, y] = 255;
                    if (width - 1 - mean[k] + 2 < 800 && width - 1 - mean[k] + 2 >= 0)
                        imageArrays[k, width - 1 - mean[k] + 2, y] = 255;
                    if (width - 1 - mean[k] + 3 < 800 && width - 1 - mean[k] + 3 >= 0)
                        imageArrays[k, width - 1 - mean[k] + 3, y] = 255;

                    if (width - 1 - min[k] < 800 && width - 1 - min[k] >= 0)
                        imageArrays[k, width - 1 - min[k], y] = 255;
                }

                for (int k = num; k < num; k++)
                {
                    if (width - 1 - x[k] < 800 && width - 1 - x[k] >= 0)
                        imageArrays[k, width - 1 - x[k], y] = 255;
                    if (width - 1 - min[k] < 800 && width - 1 - min[k] >= 0)
                        imageArrays[k, width - 1 - min[k], y] = 255;
                }
            }

            cnt += l;
            if (cnt > L)
            {
                cnt -= L;

                senderWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    for (int k = 0; k < num; k++)
                    {
                        for (int i = 0; i < imageArrays.GetLength(1); i++)
                            for (int j = 0; j < imageArrays.GetLength(2); j++)
                                imageArray1[i, j] = imageArrays[k, i, j];

                        if (k == 0)
                            oscillIm1.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 1)
                            oscillIm2.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 2)
                            oscillIm3.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        else if (k == 3)
                            oscillIm4.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        /*--------------------------------------------------------------------------------*/
                        else if (k == 4)
                            oscillIm5.WritePixels(new Int32Rect(0, 0, width, height), imageArray1, width, 0);
                        /*--------------------------------------------------------------------------------*/
                    }

                }));

                senderWindow.oscULValue.ch1UpLim = mx[0];
                senderWindow.oscULValue.ch1LoLim = mn[0];
                senderWindow.oscULValue.ch2UpLim = mx[1];
                senderWindow.oscULValue.ch2LoLim = mn[1];
                senderWindow.oscULValue.ch3UpLim = mx[2];
                senderWindow.oscULValue.ch3LoLim = mn[2];
                senderWindow.oscULValue.ch4UpLim = mx[3];
                senderWindow.oscULValue.ch4LoLim = mn[3];
                /*--------------------------------------*/
                senderWindow.oscULValue.ch5UpLim = mx[4];
                senderWindow.oscULValue.ch5LoLim = mn[4];
                /*--------------------------------------*/
            }
        }
    }
}
