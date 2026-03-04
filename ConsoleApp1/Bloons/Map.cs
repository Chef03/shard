using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shard.Bloons
{
    internal struct LPoint // Changed from private to internal
    {
        public LPoint()
        {
        }

        public int x { get; set; }
        public int y { get; set; }
        
        public bool tunnelStart { get; set; } = false;
        public bool tunnelEnd { get; set; } = false;
    }


    internal class Map
    {

        internal struct Wave
        {
            public List<Bloon> Bloons { get; set; } // each wave has a list of bloons
            public int spawnIntervalMs { get; set; } // time in milliseconds between each bloon spawn in this wave
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

        public List<LPoint> getPath()
        {
            return this.path;
        }

    }

    
}
