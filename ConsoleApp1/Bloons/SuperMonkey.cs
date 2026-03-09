using System;
using System.Collections.Generic;
using System.Drawing;

namespace Shard.Bloons
{
    internal class SuperMonkey : TowerBase
    {
        private readonly double range;
        private readonly int damage;
        private readonly double attackCooldownMs;
        private double shotCooldownRemainingMs;
        private readonly List<TrackingProjectile> activeProjectiles = new List<TrackingProjectile>();

        public SuperMonkey(LPoint position, double range = 400, int damage = 1, double attackSpeedPerSecond = 10, int cost = 2500)
            : base(position, cost)
        {
            this.range = range;
            this.damage = damage;
            this.attackCooldownMs = 1000.0 / attackSpeedPerSecond;
            this.shotCooldownRemainingMs = 0;
        }

        public override string getName()
        {
            return "Dart Monkey";
        }

        public override void update(List<Bloon> bloons, double deltaMs, LPoint pointerWorldPosition, Player owner)
        {
            if (shotCooldownRemainingMs > 0)
            {
                shotCooldownRemainingMs -= deltaMs;
            }

            // Update all active projectiles, remove finished ones
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                activeProjectiles[i].update(deltaMs, owner);
                if (!activeProjectiles[i].getActive())
                    activeProjectiles.RemoveAt(i);
            }

            if (shotCooldownRemainingMs > 0)
            {
                return;
            }

            var target = getFurthestTargetInRange(position, bloons, range);
            if (target == null)
            {
                return;
            }

            activeProjectiles.Add(new TrackingProjectile(position, target, damage, speedPixelsPerSecond: 1000));
            shotCooldownRemainingMs = attackCooldownMs;
        }

        public override void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f)
        {
            var screenPosition = toScreenPoint(position, worldScale, worldOffsetX, worldOffsetY);
            display.drawCircle(screenPosition.x, screenPosition.y, toScreenSize(range, worldScale), Color.FromArgb(75, 255, 255, 255));
            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(16, worldScale), Color.FromArgb(19, 66, 194));
            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(6, worldScale), Color.FromArgb(176, 14, 9));

            foreach (var projectile in activeProjectiles)
            {
                projectile.draw(display, worldScale, worldOffsetX, worldOffsetY);
            }
        }
    }

   
}
