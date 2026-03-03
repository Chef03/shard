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
        private BloonColor color;
        private int layer;
        private double speed;
        private readonly bool camo;
        private readonly bool regrow;

        private readonly double spawnDelayMs; // ms to wait before starting to move
        private double elapsedTime = 0;
        private bool active = false; // only moves when active
        private bool end = false; //reach the end
        private bool popped = false;

        private double xPos;
        private double yPos;
        private LPoint position => new LPoint() { x = (int)xPos, y = (int)yPos };
        private int nextPointIndex; // index of the next point in the lane path that the bloon is moving towards

        public Bloon(int layer, double speed, bool camo, bool regrow, double xStartPos, double YstartPos, double spawnDelayMs)
        {
            this.layer = layer;
            this.speed = speed;
            this.camo = camo;
            this.regrow = regrow;
            this.nextPointIndex = 1;
            this.xPos = xStartPos;
            this.yPos = YstartPos;
            this.spawnDelayMs = spawnDelayMs;
        }
        

        public void pop(int damage)
        {
            
            
            if (popped || damage <= 0)
            {
                return;
            }
            
            this.popSound();
            layer -= damage;
            if (layer <= 0)
            {
                active = false;
                popped = true;
            }
        }

        private unsafe void popSound()
        {
            var track = Bootstrap.getSound().playSound ("pop.mp3", false, 10, 10);
        }

        public bool isTargetable()
        {
            return active && !end && !popped;
        }

        public LPoint getPosition()
        {
            return position;
        }

        public double getSpeed()
        {
            return speed;
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

        public int getRenderRadius()
        {
            return color == BloonColor.Red ? 30 : 10;
        }

        public Color getRenderColor()
        {
            BloonColor[] colors = [BloonColor.Red, BloonColor.Blue, BloonColor.Green, BloonColor.Yellow, BloonColor.Pink];
            BloonColor color = colors[this.layer - 1];
            return color switch
            {
                BloonColor.Red => Color.FromArgb(255, 0, 0),
                BloonColor.Blue => Color.FromArgb(0, 0, 255),
                BloonColor.Green => Color.FromArgb(0, 255, 0),
                _ => Color.FromArgb(255, 255, 255)
            };
        }

        public void moveTowardsPoint(LPoint target)
        {
            //Should be able to move in any direction straight towards the point, not just up/down/left/right
            double dx = target.x - xPos;
            double dy = target.y - yPos;

            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance == 0) return;

            xPos += (dx / distance) * speed;
            yPos += (dy / distance) * speed;
        }
        public void updateBloon(List<LPoint> path, double deltaMs)
        {
            if (popped || end)
            {
                active = false;
                return;
            }

            elapsedTime += deltaMs;
            if (elapsedTime >= spawnDelayMs)
            {
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
                if (position.x <= target.x + 1 && position.x >= target.x - 1)
                {
                    if (position.y <= target.y + 1 && position.y >= target.y - 1)
                    {
                        nextPointIndex++;
                    }
                }
            }
            //if reach end
            if (position.x <= path[path.Count - 1].x + 1 && position.x >= path[path.Count - 1].x - 1)
            {
                if (position.y <= path[path.Count - 1].y + 1 && position.y >= path[path.Count - 1].y - 1)
                {
                    active = false;
                    end = true;
                }
            }
        }

        //TODO: hitting bloons, reaching end, regrowing
    }
}
