using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shard.Bloons
{
    internal struct LPoint // Changed from private to internal
    {
        public int x { get; set; }
        public int y { get; set; }
    }


    internal class Map
    {

        internal struct Wave
        {
            public int n { get; set; }
            public List<Bloon> Bloons { get; set; } // each wave has a list of bloons
        }

        public Lane Lane { get; set; }

        public List<Wave> Waves { get; set; }

        public Map(Lane lane, List<Wave> waves) {
            this.Lane = lane;
            this.Waves = waves;
        }
    }

    internal class Lane
    {
        
        private List<LPoint> path;

        public Lane(List<LPoint> path) // Changed to regular constructor syntax for clarity
        {
            this.path = path;
        }
    }

    internal class Bloon
    {
        private string color;
        private int layer;
        private double speed;
        private bool camo;
        private bool regrow;
        private LPoint position;

        private const double BASE_SPEED = 1;
        private const double SPEED_INCREMENT = 0.5;

        public Bloon(string color, int layer, int speed, bool camo, bool regrow)
        {
            this.color = color;
            this.layer = layer;
            this.speed = speed;
            this.camo = camo;
            this.regrow = regrow;
        }

        public void pop(int damage)
        {

            //update color, speed, camo, regrow based on new layer
        }
        public void moveTowardsPoint(LPoint point)
        {
            //Should be able to move in any direction straight towards the point, not just up/down/left/right

        }



    }
}
