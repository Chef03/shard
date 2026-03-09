using System;
using System.Collections.Generic;
using System.Drawing;

namespace Shard.Bloons
{

    public enum BloonColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Pink,
        Black,
        White,
        Lead,
        Purple,
        Zebra,
        Rainbow,
        Ceramic,
        MOAB,
        BFB,
        ZOMG,
        DDT,
        BAD
    }
    internal class Bloon
    {
        private const double baseSpeed = 1.5; // base speed for a red bloon, other bloons will be faster based on their layer
        private BloonColor color;
        private int layer;
        private double speed;
        private readonly bool camo;
        private readonly bool regrow;

        private readonly double spawnDelayMs; // ms to wait before starting to move
        private bool active = false; // only moves when active
        private bool end = false; //reach the end
        private bool popped = false;

        private double xPos;
        private double yPos;
        private LPoint position => new LPoint() { x = (int)xPos, y = (int)yPos };
        private int nextPointIndex; // index of the next point in the lane path that the bloon is moving towards
        private bool isTargetable = true;// can be targeted by towers, becomes false when in tunnels
        private double distanceTravelled = 0;
        private double totalPathLength = 0;
        
        public Bloon(int layer, bool camo, bool regrow, double xStartPos, double YstartPos, double spawnDelayMs)
        {
            this.layer = layer;
            this.speed = baseSpeed + layer * 0.4;
            this.camo = camo;
            this.regrow = regrow;
            this.nextPointIndex = 1;
            this.xPos = xStartPos;
            this.yPos = YstartPos;
            this.spawnDelayMs = spawnDelayMs;
        }

        public void updateSpeed(int layer)
        {
            if (layer == 6)
            {
                speed = baseSpeed + layer; //white fast
            }
            else
            {
                speed = baseSpeed + layer * 0.4;
            }

        }


        public void pop(int damage, Player owner)
        {
            
            
            if (popped || damage <= 0)
            {
                return;
            }
            
            this.popSound();
            layer -= damage;
            owner.addMoney(damage);
            if (layer <= 0)
            {
                active = false;
                popped = true;
            }
            updateSpeed(layer);
        }

        private unsafe void popSound()
        {
            var track = Bootstrap.getSound().playSound ("pop.mp3", false, 10, 10, 20);
        }

        //public bool isTargetable()
        //{
        //    return active && !end && !popped;
        //}

        public LPoint getPosition()
        {
            return position;
        }
        public bool getIsCamo()    => camo;
        public bool getIsRegrow()  => regrow;
        public float getProgress() => totalPathLength > 0
            ? (float)Math.Clamp(distanceTravelled / totalPathLength, 0.0, 1.0)
            : 0f;

        public bool getIsTargetable() { return isTargetable; }

        public double getSpeed()
        {
            return speed;
        }

        public bool getPopped()
        {
            return popped;
        }

        public BloonColor getColor()
        {
            return color;
        }

        public int getNextPointIndex()
        {
            return nextPointIndex;
        }
        
        public bool getActive()
        {
            return active;
        }

        public bool getEnd()
        {
            return end;
        }
        public int getLayer()
        {
            return layer;
        }

        public int getRenderRadius()
        {
            return 30 + layer * 1;
        }

        public Color getRenderColor()
        {
            BloonColor[] colors = [BloonColor.Red, BloonColor.Blue, BloonColor.Green, BloonColor.Yellow, BloonColor.Pink, BloonColor.White, BloonColor.Black];
            BloonColor color = colors[this.layer - 1];
            return color switch
            {
                BloonColor.Red    => Color.FromArgb(255, 0,   0),
                BloonColor.Blue   => Color.FromArgb(0,   0,   255),
                BloonColor.Green  => Color.FromArgb(0,   200, 0),
                BloonColor.Yellow => Color.FromArgb(255, 220, 0),
                BloonColor.Pink   => Color.FromArgb(255, 105, 180),
                BloonColor.White  => Color.FromArgb(255, 255, 255),
                BloonColor.Black  => Color.FromArgb(0,   0,   0),
                _ => Color.FromArgb(100,100,100),
            };
        }

        public void moveTowardsPoint(LPoint target)
        {
            //Should be able to move in any direction straight towards the point, not just up/down/left/right
            double dx = target.x - xPos;
            double dy = target.y - yPos;

            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance == 0) return;
            double step = Math.Min(speed, distance);
            xPos += (dx / distance) * step;
            yPos += (dy / distance) * step;
            distanceTravelled += step;
        }
        public void updateBloon(List<LPoint> path, double deltaMs, double waveElapsedTime)
        {
            if (popped || end)
            {
                active = false;
                return;
            }

            if (waveElapsedTime >= spawnDelayMs)
            {
                if (!active && totalPathLength == 0) // first activation
                {
                    for (int i = 1; i < path.Count; i++)
                    {
                        double dx = path[i].x - path[i - 1].x;
                        double dy = path[i].y - path[i - 1].y;
                        totalPathLength += Math.Sqrt(dx * dx + dy * dy);
                    }
                }
                active = true;
            }
            else
            {
                return;
            }

            int nextIndex = nextPointIndex;
            if (nextIndex <= path.Count - 1)
            {
                LPoint target = path[nextPointIndex];
                
                moveTowardsPoint(target);

                //Check bllon has reached the target point, account for rounding errors
                if (position.x <= target.x + 2 && position.x >= target.x - 2)
                {
                    if (position.y <= target.y + 2 && position.y >= target.y - 2)
                    {
                        nextPointIndex++;
                        if (target.tunnelStart)
                        {
                            isTargetable = false;

                        }
                        if (target.tunnelEnd)
                        {
                            isTargetable = true;
                        }

                    }
                }
            }
            //if reach end
            if (position.x <= path[path.Count - 1].x + 2 && position.x >= path[path.Count - 1].x - 2)
            {
                if (position.y <= path[path.Count - 1].y + 2 && position.y >= path[path.Count - 1].y - 2)
                {
                    active = false;
                    end = true;
                }
            }
        }

    }
}
