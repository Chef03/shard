using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private bool camo;
        private bool regrow;

        private double spawnDelayMs; // ms to wait before starting to move
        private double elapsedTime = 0;
        private bool active = false; // only moves when active
        private bool end = false; //reach the end

        private double xPos;
        private double yPos;
        private LPoint position => new LPoint() { x = (int)xPos, y = (int)yPos };
        private int nextPointIndex; // index of the next point in the lane path that the bloon is moving towards


        private const double BASE_SPEED = 1;
        private const double SPEED_INCREMENT = 0.5;

        public Bloon(BloonColor color, int layer, double speed, bool camo, bool regrow, double xStartPos, double YstartPos, double spawnDelayMs)
        {
            this.color = color;
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

            //update color, speed, camo, regrow based on new layer
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
        public int getNextPointIndex() { 
            return nextPointIndex;
        }
        public bool getActive()
        {
            return active;
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
            if (!end)
            {
                elapsedTime += deltaMs;
                Debug.Log($"Bloon at ({position.x}, {position.y}) with speed {speed} and elapsed time {elapsedTime} ms. Is active: {active}");
                if (elapsedTime >= spawnDelayMs)
                {
                    active = true;
                }
                else { return; }

                int nextIndex = nextPointIndex;
                if (nextIndex <= path.Count - 1)
                {
                    LPoint target = path[nextPointIndex];
                    //Debug.Log($"Bloon at ({position.x}, {position.y}) moving towards ({target.x}, {target.y}) with speed {speed}");
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

        }

        //TODO: hitting bloons, reaching end, regrowing
    }
}
