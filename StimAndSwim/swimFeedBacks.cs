using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace BehaveAndScanGECI
{
    public partial class StimEphysOscilloscopeControl
    {

        public void swimFeedBacks()
        {
            if ((swimPow[0] > 0 || swimPow[1] > 0) && (senderWindow.InstStimParams.closedLoop1DzeroVel == false))
            {
                vel = 0.3f * vel + 0.6f * (float)(-0.0025 / 3 * velMultip + closedLoop1Dgain * (swimPow[0] + swimPow[1]) / 2)*1;  // /2 since bigger screen
                if (vel > 0.0025 / 3f * 10)//0.0025 * 10)
                    vel = (float)(0.0025 / 3f * 10);
                vel2 = 0.3f * vel2 + 0.6f * (float)(senderWindow.rightGain * swimPow[1] - senderWindow.leftGain * swimPow[0]);
                if (vel2 < senderWindow.horThresh && vel2 > -senderWindow.horThresh)
                    vel2 = 0;
                else if (vel2 > 0.0025 / 3f * 10)//0.0025 * 10) 
                    vel2 = (float)(0.0025 / 3f * 10);
                else if (vel2 < -0.0025 / 3f * 10)//0.0025 * 10)              
                    vel2 = (float)(-0.0025 / 3f * 10);
            }
            else    // no swimming
            {
                vel = vel - dVel / 3f;
                if (vel < -0.0025f / 3f * (float)senderWindow.vObstacle)
                    vel = -0.0025f / 3f * (float)senderWindow.vObstacle;
                if (vel2 > dVel / 3f)
                    vel2 = vel2 - dVel / 3f;
                else if (vel2 < -dVel / 3f)
                    vel2 = vel2 + dVel / 3f;
                else
                    vel2 = 0f;

                if (senderWindow.InstStimParams.closedLoop1DzeroVel)
                {
                    vel = 0f;
                    vel2 = 0f;
                }
            }



            //This option enables keyboard control by pressing the direction keys.
            if (senderWindow.gameMode)
            {
                vel = -0.0025f / 3f * (float)senderWindow.vObstacle;
                vel2 = 0;
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (Keyboard.IsKeyDown(Key.Up))
                        vel += 0.01f;
                    if (Keyboard.IsKeyDown(Key.Down))
                        vel -= 0.01f;
                    if (Keyboard.IsKeyDown(Key.Left))
                        vel2 -= 0.01f;
                    if (Keyboard.IsKeyDown(Key.Right))
                        vel2 += 0.01f;
                });

            }



            //This option toggles on the display of walls and obstacles.

                
            xLocNew = senderWindow.xLoc + DT * vel2;
            yLocNew = senderWindow.yLoc + DT * vel;
            yLocSec = yLocNew - yLastSec;
                
            if(senderWindow.displayWalls)
            {
                if (xLocNew - fishWidth / 2 < -boundary)
                {
                    vel2 = (-boundary - senderWindow.xLoc + fishWidth / 2) / DT;
                }
                else if (xLocNew + fishWidth / 2 > boundary)
                {
                    vel2 = (boundary - senderWindow.xLoc - fishWidth / 2) / DT;
                }
            }



            if (senderWindow.displayObstacles && senderWindow.StationaryObstacles && senderWindow.timedInt)
                vBackgroundture = senderWindow.vBackground;
            if (senderWindow.displayObstacles && (!senderWindow.StationaryObstacles) && !senderWindow.timedInt)
            {
                ySec = preObst + obst + postObst;

                //20221001
                ySta = preObst + obst + staPos;

                if ((yLocSec > ySta) && (yLocSec < ySec)) // move forward to the stationary phase 20221001
                {
                    StaTimer -= DT;
                    if (StaTimer < 3001 && StaTimer > 1)
                    { vBackgroundture = 0; }
                    else
                    { vBackgroundture = senderWindow.vBackground; }

                }
                if (yLocSec > ySec) // move forward to the next section
                {
                    lastBlock = nextBlock;
                    yLastSec += ySec;
                    yLocSec = yLocNew - yLastSec;
                    senderWindow.trialnumber++; //20210819
                    hitTimer = 0;//20210819
                    StaTimer = 3001; //20221001
                    vBackgroundture = senderWindow.vBackground;//20221001

                    if (senderWindow.fixedPosition)
                    {
                        nextBlock = xLocNew + senderWindow.fixed_pos;
                        //senderWindow.fixed_pos = senderWindow.fixed_pos * (-1);//alternate obstacle position
                    }
                    else
                    {
                        Random rand = new Random();
                        //nextBlock = xLocNew + (float)Math.Round(((rand.NextDouble() - 0.5) * 3));
                        if (rand.NextDouble() <= 0.5)
                        {
                            nextBlock = xLocNew + 2.5f;
                            stimParam2 = 2.5;
                            postObst = 10f;
                        }
                        else
                        {
                            nextBlock = xLocNew - 2.5f;
                            stimParam2 = -2.5;
                            postObst = 10f;
                        }
                    }
                }

                else if (yLocSec < 0) // return to the last section
                {
                    nextBlock = lastBlock;
                    yLastSec -= ySec;
                    yLocSec = yLocNew - yLastSec;
                    senderWindow.trialnumber++; //20210819

                    if (senderWindow.fixedPosition)
                    {
                        lastBlock = xLocNew + senderWindow.fixed_pos;
                        senderWindow.fixed_pos = senderWindow.fixed_pos * (-1);//alternate obstacle position
                    }
                    else
                    {
                        Random rand = new Random();
                        //lastBlock = xLocNew + (float)Math.Round(((rand.NextDouble() - 0.5) * 3));
                        if (rand.NextDouble() <= 0.5)
                        {
                            nextBlock = xLocNew + 2.5f;
                            stimParam2 = 2.5;
                            postObst = 10f;
                        }
                        else
                        {
                            nextBlock = xLocNew - 2.5f;
                            stimParam2 = -2.5;
                            postObst = 10f;
                        }

                    }

                }
                else if ((xLocNew - fishWidth / 2 < nextBlock + obst / 2) && (xLocNew + fishWidth / 2 > nextBlock - obst / 2) && ((yLocSec + fishLength / 2) > preObst) && ((yLocSec - fishLength / 2) < (preObst + obst)))
                {
                    if (hitTimer < 1000) //20210828
                    {
                        if (((yLocSec + fishLength / 2) > preObst) && ((senderWindow.yLoc + fishLength / 2 - yLastSec) <= preObst + 0.001f))
                        {
                            hitTimer += DT;
                            vel = (preObst - senderWindow.yLoc + yLastSec - fishLength / 2) / DT;
                        }
                        else if ((xLocNew - fishWidth / 2 <= nextBlock + obst / 2) && (senderWindow.xLoc + fishWidth / 2 >= nextBlock + obst / 2))
                        {
                            vel2 = ((nextBlock + obst / 2) - senderWindow.xLoc + fishWidth / 2) / DT + 0.001f;
                        }
                        else if ((xLocNew + fishWidth / 2 >= nextBlock - obst / 2) && (senderWindow.xLoc - fishWidth / 2 <= nextBlock - obst / 2))
                        {
                            vel2 = ((nextBlock - obst / 2) - senderWindow.xLoc - fishWidth / 2) / DT - 0.001f;
                        }
                        else
                        {
                            vel = (preObst + obst - senderWindow.yLoc + fishLength / 2 + yLastSec) / DT + 0.001f;
                        }
                    }
                }
            }
            else if (senderWindow.displayObstacles && !senderWindow.StationaryObstacles && senderWindow.timedInt && senderWindow.trialnumber % 2 == 0)
            {
                ySec = preObst + obst + postObst_short;
                vBackgroundture = senderWindow.vBackground;
                if (enteringObsTrial)
                {
                    yLastSec = yLocNew;
                    enteringObsTrial = false;
                    if (senderWindow.fixedPosition)
                    {
                        nextBlock = xLocNew + senderWindow.fixed_pos;
                    }
                    else
                    {
                        Random rand = new Random();
                        if (rand.NextDouble() <= 0.5)
                        {
                            nextBlock = xLocNew + 2.5f;
                            stimParam2 = 2.5;
                        }
                        else
                        {
                            nextBlock = xLocNew - 2.5f;
                            stimParam2 = -2.5;
                        }
                    }
                }
                if (yLocNew- yLastSec > ySec)
                {
                    senderWindow.trialnumber++;
                    enteringIntTrial = true;
                }
                else if ((xLocNew - fishWidth / 2 < nextBlock + obst / 2) && (xLocNew + fishWidth / 2 > nextBlock - obst / 2) && ((yLocSec + fishLength / 2) > preObst) && ((yLocSec - fishLength / 2) < (preObst + obst)))
                {
                    if (hitTimer < 1000) //20210828
                    {
                        if (((yLocSec + fishLength / 2) > preObst) && ((senderWindow.yLoc + fishLength / 2 - yLastSec) <= preObst + 0.001f))
                        {
                            hitTimer += DT;
                            vel = (preObst - senderWindow.yLoc + yLastSec - fishLength / 2) / DT;
                        }
                        else if ((xLocNew - fishWidth / 2 <= nextBlock + obst / 2) && (senderWindow.xLoc + fishWidth / 2 >= nextBlock + obst / 2))
                        {
                            vel2 = ((nextBlock + obst / 2) - senderWindow.xLoc + fishWidth / 2) / DT + 0.001f;
                        }
                        else if ((xLocNew + fishWidth / 2 >= nextBlock - obst / 2) && (senderWindow.xLoc - fishWidth / 2 <= nextBlock - obst / 2))
                        {
                            vel2 = ((nextBlock - obst / 2) - senderWindow.xLoc - fishWidth / 2) / DT - 0.001f;
                        }
                        else
                        {
                            vel = (preObst + obst - senderWindow.yLoc + fishLength / 2 + yLastSec) / DT + 0.001f;
                        }
                    }
                }
            }
            else if (senderWindow.displayObstacles && !senderWindow.StationaryObstacles && senderWindow.timedInt && senderWindow.trialnumber % 2 != 0)
            {
                vBackgroundture = 0;
            }
            senderWindow.xLoc += DT * vel2;
            senderWindow.yLoc += DT * vel;




            if (vel > 0 && gainTriggerSwitch)
            {
                gainTriggerSwitch = false;
            }
            if (vel < 0 && gainTriggerSwitch == false)
            {
                gainTriggerSwitch = true;
            }




            //Updates grating location using velocity and time passed.
            if (senderWindow.InstStimParams.moveobject == 1)
                stripesY += 0.0025f / 3f * (float)(senderWindow.vObstacle - vBackgroundture) * (float)(DT * (90.0 / 1000.0))
                        + (float)Math.Cos(moveAngle) * vel * (float)(DT * (90.0 / 1000.0))
                        + (float)Math.Sin(moveAngle) * vel2 * (float)(DT * (90.0 / 1000.0));
            else
            {
                stripesX += (float)Math.Cos(-moveAngle) * vel2 * (float)(DT * (90.0 / 1000.0))
                        + (float)Math.Sin(moveAngle) * vel * (float)(DT * (90.0 / 1000.0));
                stripesY += 0.0025f / 3f * (float)(senderWindow.vObstacle - vBackgroundture) * (float)(DT * (90.0 / 1000.0))
                        + (float)Math.Cos(moveAngle) * vel * (float)(DT * (90.0 / 1000.0))
                        + (float)Math.Sin(-moveAngle) * vel2 * (float)(DT * (90.0 / 1000.0));
            }
            //stimState.yPosition -= (float)vel * (float)(DT * ( 90.0 / 1000.0));

            xFish = -0.7f + senderWindow.xOffset;
            yFish = -0.5f + senderWindow.yOffset;
            //Updates wall and obstacle location using velocity and time passed.
            if(senderWindow.displayWalls)
            {
                leftWall = (-boundary + senderWindow.xLoc) * (float)(90.0 / 1000.0) + xFish;
                rightWall = (boundary + senderWindow.xLoc) * (float)(90.0 / 1000.0) + xFish;
            }    
            if(senderWindow.displayObstacles && (!senderWindow.StationaryObstacles))
            {
                xNext = (senderWindow.xLoc - nextBlock) * (float)(90.0 / 1000.0) + xFish;
                xLast = (senderWindow.xLoc - lastBlock) * (float)(90.0 / 1000.0) + xFish;
                yNext = (senderWindow.yLoc - fishLength / 2 - yLastSec + fishLength / 2 - obst/2 - preObst) * (float)(90.0 / 1000.0) + yFish;
                yLast = (senderWindow.yLoc - fishLength / 2 - yLastSec + fishLength / 2 + obst/2 + postObst) * (float)(90.0 / 1000.0) + yFish;
            }
        }
    }
}
