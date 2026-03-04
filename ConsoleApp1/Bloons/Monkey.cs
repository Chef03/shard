using System;
using System.Collections.Generic;
using System.Drawing;

namespace Shard.Bloons
{
    internal class Monkey
    {
        private readonly LPoint position;
        private readonly double range;
        private readonly int damage;
        private readonly double attackCooldownMs;
        private double shotCooldownRemainingMs;
        private Projectile activeProjectile;

        public Monkey(LPoint position, double range = 230, int damage = 1, double attackSpeedPerSecond = 1.7)
        {
            this.position = position;
            this.range = range;
            this.damage = damage;
            this.attackCooldownMs = 1000.0 / attackSpeedPerSecond;
            this.shotCooldownRemainingMs = 0;
        }

        public void update(List<Bloon> bloons, double deltaMs, Player owner)
        {
            if (shotCooldownRemainingMs > 0)
            {
                shotCooldownRemainingMs -= deltaMs;
            }

            if (activeProjectile != null)
            {
                activeProjectile.update(deltaMs, owner);
                if (!activeProjectile.getActive())
                {
                    activeProjectile = null;
                }
            }

            if (activeProjectile != null || shotCooldownRemainingMs > 0)
            {
                return;
            }

            var target = getClosestTargetInRange(bloons);
            if (target == null)
            {
                return;
            }

            activeProjectile = new Projectile(position, target, damage, speedPixelsPerSecond: 850);

            shotCooldownRemainingMs = attackCooldownMs;
        }

        public void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f)
        {
            var screenPosition = toScreenPoint(position, worldScale, worldOffsetX, worldOffsetY);
            display.drawCircle(screenPosition.x, screenPosition.y, toScreenSize(range, worldScale), Color.FromArgb(75, 255, 255, 255));
            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(16, worldScale), Color.FromArgb(160, 90, 45));
            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(6, worldScale), Color.FromArgb(40, 40, 40));

            if (activeProjectile != null && activeProjectile.getActive())
            {
                activeProjectile.draw(display, worldScale, worldOffsetX, worldOffsetY);
            }
        }

        private static int toScreenSize(double value, float worldScale)
        {
            return Math.Max(1, (int)MathF.Round((float)value * worldScale));
        }

        private static LPoint toScreenPoint(LPoint worldPoint, float worldScale, float worldOffsetX, float worldOffsetY)
        {
            return new LPoint()
            {
                x = (int)MathF.Round(worldOffsetX + (worldPoint.x * worldScale)),
                y = (int)MathF.Round(worldOffsetY + (worldPoint.y * worldScale))
            };
        }

        private Bloon getClosestTargetInRange(List<Bloon> bloons)
        {
            Bloon closest = null;
            double closestDistanceSq = double.MaxValue;
            var rangeSq = range * range;

            foreach (var bloon in bloons)
            {
                if (!bloon.getIsTargetable())
                {
                    continue;
                }

                var targetPosition = bloon.getPosition();
                var dx = targetPosition.x - position.x;
                var dy = targetPosition.y - position.y;
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
    }

    internal class Projectile
    {
        private readonly Bloon target;
        private readonly int damage;
        private readonly double speedPixelsPerSecond;
        private bool active;
        private double xPos;
        private double yPos;

        public Projectile(LPoint spawnPosition, Bloon target, int damage, double speedPixelsPerSecond)
        {
            this.target = target;
            this.damage = damage;
            this.speedPixelsPerSecond = speedPixelsPerSecond;
            this.active = true;
            this.xPos = spawnPosition.x;
            this.yPos = spawnPosition.y;
        }

        public bool getActive()
        {
            return active;
        }

        public void update(double deltaMs, Player owner)
        {
            if (!active)
            {
                return;
            }

            if (!target.getIsTargetable())
            {
                active = false;
                return;
            }

            var targetPosition = target.getPosition();
            var dx = targetPosition.x - xPos;
            var dy = targetPosition.y - yPos;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            var stepDistance = speedPixelsPerSecond * (deltaMs / 1000.0);
            var hitDistance = Math.Max(6, target.getRenderRadius());

            if (distance <= hitDistance || distance <= stepDistance)
            {
                target.pop(damage);
                owner.addMoney(damage);
                active = false;
                return;
            }

            if (distance == 0)
            {
                return;
            }

            xPos += (dx / distance) * stepDistance;
            yPos += (dy / distance) * stepDistance;
        }

        public void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f)
        {
            if (!active)
            {
                return;
            }

            display.drawFilledCircle(
                (int)MathF.Round(worldOffsetX + ((float)xPos * worldScale)),
                (int)MathF.Round(worldOffsetY + ((float)yPos * worldScale)),
                Math.Max(1, (int)MathF.Round(4 * worldScale)),
                Color.FromArgb(255, 40, 40));
        }
    }
}
