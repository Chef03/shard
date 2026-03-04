using System;
using System.Collections.Generic;
using System.Drawing;

namespace Shard.Bloons
{
    internal class Dartling : TowerBase
    {
        private readonly int damage;
        private readonly double attackCooldownMs;
        private readonly double projectileSpeedPixelsPerSecond;
        private readonly double projectileLifetimeMs;
        private readonly List<DartlingProjectile> activeProjectiles;
        private double shotCooldownRemainingMs;
        private double aimDirectionX;
        private double aimDirectionY;

        public Dartling(
            LPoint position,
            int damage = 1,
            double attackSpeedPerSecond = 2,
            double projectileSpeedPixelsPerSecond = 1200,
            double projectileLifetimeMs = 1200)
            : base(position)
        {
            this.damage = damage;
            this.attackCooldownMs = 1000.0 / attackSpeedPerSecond;
            this.projectileSpeedPixelsPerSecond = projectileSpeedPixelsPerSecond;
            this.projectileLifetimeMs = projectileLifetimeMs;
            this.activeProjectiles = new List<DartlingProjectile>();
            this.shotCooldownRemainingMs = 0;
            this.aimDirectionX = 1;
            this.aimDirectionY = 0;
        }

        public override string getName()
        {
            return "Dartling";
        }

        public override void update(List<Bloon> bloons, double deltaMs, LPoint pointerWorldPosition, Player owner)
        {
            updateAim(pointerWorldPosition);

            if (shotCooldownRemainingMs > 0)
            {
                shotCooldownRemainingMs -= deltaMs;
            }

            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                activeProjectiles[i].update(bloons, deltaMs);
                if (!activeProjectiles[i].getActive())
                {
                    activeProjectiles.RemoveAt(i);
                }
            }

            while (shotCooldownRemainingMs <= 0)
            {
                activeProjectiles.Add(new DartlingProjectile(
                    position,
                    aimDirectionX,
                    aimDirectionY,
                    damage,
                    projectileSpeedPixelsPerSecond,
                    projectileLifetimeMs));
                shotCooldownRemainingMs += attackCooldownMs;
            }
        }

        public override void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f)
        {
            var screenPosition = toScreenPoint(position, worldScale, worldOffsetX, worldOffsetY);
            var barrelLength = toScreenSize(40, worldScale);
            var barrelEndX = screenPosition.x + (int)MathF.Round((float)(aimDirectionX * barrelLength));
            var barrelEndY = screenPosition.y + (int)MathF.Round((float)(aimDirectionY * barrelLength));

            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(16, worldScale), Color.FromArgb(55, 120, 190));
            display.drawLine(screenPosition.x, screenPosition.y, barrelEndX, barrelEndY, 230, 230, 230, 255);
            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(6, worldScale), Color.FromArgb(20, 20, 20));

            foreach (var projectile in activeProjectiles)
            {
                if (!projectile.getActive())
                {
                    continue;
                }

                projectile.draw(display, worldScale, worldOffsetX, worldOffsetY);
            }
        }

        private void updateAim(LPoint pointerWorldPosition)
        {
            var dx = pointerWorldPosition.x - position.x;
            var dy = pointerWorldPosition.y - position.y;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            if (distance <= 0.001)
            {
                return;
            }

            aimDirectionX = dx / distance;
            aimDirectionY = dy / distance;
        }
    }

    internal class DartlingProjectile
    {
        private readonly double directionX;
        private readonly double directionY;
        private readonly int damage;
        private readonly double speedPixelsPerSecond;
        private bool active;
        private double remainingLifetimeMs;
        private double xPos;
        private double yPos;

        public DartlingProjectile(
            LPoint spawnPosition,
            double directionX,
            double directionY,
            int damage,
            double speedPixelsPerSecond,
            double lifetimeMs)
        {
            this.directionX = directionX;
            this.directionY = directionY;
            this.damage = damage;
            this.speedPixelsPerSecond = speedPixelsPerSecond;
            this.remainingLifetimeMs = lifetimeMs;
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

            remainingLifetimeMs -= deltaMs;
            if (remainingLifetimeMs <= 0)
            {
                active = false;
                return;
            }

            var stepDistance = speedPixelsPerSecond * (deltaMs / 1000.0);
            xPos += directionX * stepDistance;
            yPos += directionY * stepDistance;

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
                var hitDistance = Math.Max(6, bloon.getRenderRadius());
                if (distanceSq > hitDistance * hitDistance)
                {
                    continue;
                }

                bloon.pop(damage);
                active = false;
                break;
            }
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
                Math.Max(5, (int)MathF.Round(3 * worldScale)),
                Color.FromArgb(255, 215, 75));
        }
    }
}
