using System;
using System.Collections.Generic;
using System.Drawing;

namespace Shard.Bloons
{
    internal class BombShooter : TowerBase
    {
        private readonly double range;
        private readonly int damage;
        private readonly double blastRadius;
        private readonly double attackCooldownMs;
        private readonly double projectileSpeedPixelsPerSecond;
        private double shotCooldownRemainingMs;
        private PineappleBombProjectile activeProjectile;

        public BombShooter(
            LPoint position,
            double range = 280,
            int damage = 1,
            double blastRadius = 95,
            double attackSpeedPerSecond = 0.9,
            double projectileSpeedPixelsPerSecond = 750)
            : base(position)
        {
            this.range = range;
            this.damage = damage;
            this.blastRadius = blastRadius;
            this.attackCooldownMs = 1000.0 / attackSpeedPerSecond;
            this.projectileSpeedPixelsPerSecond = projectileSpeedPixelsPerSecond;
            this.shotCooldownRemainingMs = 0;
        }

        public override string getName()
        {
            return "Bomb Shooter";
        }

        public override void update(List<Bloon> bloons, double deltaMs, LPoint pointerWorldPosition, Player owner)
        {
            if (shotCooldownRemainingMs > 0)
            {
                shotCooldownRemainingMs -= deltaMs;
            }

            if (activeProjectile != null)
            {
                activeProjectile.update(bloons, deltaMs);
                if (!activeProjectile.getActive())
                {
                    activeProjectile = null;
                }
            }

            if (activeProjectile != null || shotCooldownRemainingMs > 0)
            {
                return;
            }

            var target = getClosestTargetInRange(position, bloons, range);
            if (target == null)
            {
                return;
            }

            activeProjectile = new PineappleBombProjectile(
                position,
                target,
                damage,
                blastRadius,
                projectileSpeedPixelsPerSecond);
            shotCooldownRemainingMs = attackCooldownMs;
        }

        public override void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f)
        {
            var screenPosition = toScreenPoint(position, worldScale, worldOffsetX, worldOffsetY);
            display.drawCircle(screenPosition.x, screenPosition.y, toScreenSize(range, worldScale), Color.FromArgb(75, 255, 235, 135));
            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(17, worldScale), Color.FromArgb(90, 110, 115));
            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(8, worldScale), Color.FromArgb(25, 25, 25));

            if (activeProjectile != null && activeProjectile.getActive())
            {
                activeProjectile.draw(display, worldScale, worldOffsetX, worldOffsetY);
            }
        }
    }

    internal class PineappleBombProjectile
    {
        private readonly Bloon target;
        private readonly int damage;
        private readonly double blastRadius;
        private readonly double speedPixelsPerSecond;
        private bool active;
        private double xPos;
        private double yPos;

        public PineappleBombProjectile(
            LPoint spawnPosition,
            Bloon target,
            int damage,
            double blastRadius,
            double speedPixelsPerSecond)
        {
            this.target = target;
            this.damage = damage;
            this.blastRadius = blastRadius;
            this.speedPixelsPerSecond = speedPixelsPerSecond;
            this.active = true;
            this.xPos = spawnPosition.x;
            this.yPos = spawnPosition.y;
        }

        public bool getActive()
        {
            return active;
        }

        public void update(List<Bloon> bloons, double deltaMs)
        {
            if (!active)
            {
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
                xPos = targetPosition.x;
                yPos = targetPosition.y;
                explode(bloons);
                active = false;
                return;
            }

            if (distance <= 0.001)
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

            var screenX = (int)MathF.Round(worldOffsetX + ((float)xPos * worldScale));
            var screenY = (int)MathF.Round(worldOffsetY + ((float)yPos * worldScale));
            var bodyRadius = Math.Max(1, (int)MathF.Round(5 * worldScale));
            var leafLength = Math.Max(1, (int)MathF.Round(6 * worldScale));

            display.drawFilledCircle(screenX, screenY, bodyRadius, Color.FromArgb(245, 210, 70));
            display.drawLine(screenX, screenY - bodyRadius, screenX, screenY - bodyRadius - leafLength, 70, 180, 70, 255);
            display.drawLine(screenX - 2, screenY - bodyRadius + 1, screenX - 4, screenY - bodyRadius - leafLength + 2, 70, 180, 70, 255);
            display.drawLine(screenX + 2, screenY - bodyRadius + 1, screenX + 4, screenY - bodyRadius - leafLength + 2, 70, 180, 70, 255);
        }

        private void explode(List<Bloon> bloons)
        {
            var blastRadiusSq = blastRadius * blastRadius;
            foreach (var bloon in bloons)
            {
                if (!bloon.getIsTargetable())
                {
                    continue;
                }

                var bloonPosition = bloon.getPosition();
                var dx = bloonPosition.x - xPos;
                var dy = bloonPosition.y - yPos;
                var distanceSq = (dx * dx) + (dy * dy);

                if (distanceSq <= blastRadiusSq)
                {
                    bloon.pop(damage);
                }
            }
        }
    }
}
