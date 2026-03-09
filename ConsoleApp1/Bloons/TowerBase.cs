using System;
using System.Collections.Generic;

namespace Shard.Bloons
{
    internal abstract class TowerBase : Tower
    {
        protected readonly LPoint position;
        private int cost;

        protected TowerBase(LPoint position, int cost)
        {
            this.position = position;
            this.cost = cost;
        }
        public int getCost()
        {
            return cost;
        }

        public LPoint getPosition()
        {
            return position;
        }

        public abstract string getName();
        public virtual List<ProjectileSnapshot> getProjectileSnapshots()
        {
            return new List<ProjectileSnapshot>();
        }

        public virtual TowerSnapshot createSnapshot(int ownerId)
        {
            return new TowerSnapshot
            {
                TowerType = GetType().Name,
                X = position.x,
                Y = position.y,
                OwnerId = ownerId,
                AimDirectionX = 0,
                AimDirectionY = 0,
            };
        }

        public virtual void applySnapshot(TowerSnapshot snapshot)
        {
        }

        public abstract void update(List<Bloon> bloons, double deltaMs, LPoint pointerWorldPosition, Player owner);
        public abstract void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f);

        protected static int toScreenSize(double value, float worldScale)
        {
            return Math.Max(1, (int)MathF.Round((float)value * worldScale));
        }

        protected static LPoint toScreenPoint(LPoint worldPoint, float worldScale, float worldOffsetX, float worldOffsetY)
        {
            return new LPoint()
            {
                x = (int)MathF.Round(worldOffsetX + (worldPoint.x * worldScale)),
                y = (int)MathF.Round(worldOffsetY + (worldPoint.y * worldScale))
            };
        }

        protected static Bloon getClosestTargetInRange(LPoint sourcePosition, List<Bloon> bloons, double range)
        {
            Bloon closest = null;
            var closestDistanceSq = double.MaxValue;
            var rangeSq = range * range;

            foreach (var bloon in bloons)
            {
                if (!bloon.getIsTargetable())
                {
                    continue;
                }

                var targetPosition = bloon.getPosition();
                var dx = targetPosition.x - sourcePosition.x;
                var dy = targetPosition.y - sourcePosition.y;
                var distanceSq = (dx * dx) + (dy * dy);

                if (distanceSq > rangeSq || distanceSq >= closestDistanceSq)
                {
                    continue;
                }

                closestDistanceSq = distanceSq;
                closest = bloon;
            }

            return closest;
        }
        protected static Bloon getFurthestTargetInRange(LPoint sourcePosition, List<Bloon> bloons, double range)
        {
            Bloon furthest = null;
            var furthestProgress = -1f;
            var rangeSq = range * range;

            foreach (var bloon in bloons)
            {
                if (!bloon.getIsTargetable())
                    continue;

                var targetPosition = bloon.getPosition();
                var dx = targetPosition.x - sourcePosition.x;
                var dy = targetPosition.y - sourcePosition.y;
                var distanceSq = (dx * dx) + (dy * dy);

                if (distanceSq > rangeSq)
                    continue;

                // Compare by path index first, then fractional progress as tiebreaker
                var bloonProgress = (bloon.getNextPointIndex() * 1000) + (bloon.getProgress() * 1000);
                if (bloonProgress <= furthestProgress)
                    continue;

                furthestProgress = bloonProgress;
                furthest = bloon;
            }

            return furthest;
        }
    }
}
