using System;
using System.Collections.Generic;
using System.Drawing;

namespace Shard.Bloons
{
    internal class TackShooter : TowerBase
    {
        private readonly int damage;
        private readonly double range;
        private readonly double attackCooldownMs;
        private readonly double projectileSpeedPixelsPerSecond;
        private readonly double projectileLifetimeMs;
        private readonly List<TackProjectile> activeProjectiles;
        private double shotCooldownRemainingMs;

        public TackShooter(
            LPoint position,
            double range = 150,
            int damage = 1,
            int cost = 260,
            double attackSpeedPerSecond = 1.5,
            double projectileSpeedPixelsPerSecond = 980,
            double projectileLifetimeMs = 200)
            : base(position, cost)
        {
            this.damage = damage;
            this.range = range;
            this.attackCooldownMs = 1000.0 / attackSpeedPerSecond;
            this.projectileSpeedPixelsPerSecond = projectileSpeedPixelsPerSecond;
            this.projectileLifetimeMs = projectileLifetimeMs;
            this.activeProjectiles = new List<TackProjectile>();
            this.shotCooldownRemainingMs = 0;
        }

        public override string getName()
        {
            return "Tack Shooter";
        }

        public override List<ProjectileSnapshot> getProjectileSnapshots()
        {
            var snapshots = new List<ProjectileSnapshot>(activeProjectiles.Count);
            foreach (var projectile in activeProjectiles)
            {
                if (!projectile.getActive())
                {
                    continue;
                }

                snapshots.Add(projectile.toSnapshot());
            }

            return snapshots;
        }

        public override void update(List<Bloon> bloons, double deltaMs, LPoint pointerWorldPosition, Player owner)
        {
            if (shotCooldownRemainingMs > 0)
            {
                shotCooldownRemainingMs -= deltaMs;
            }

            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                activeProjectiles[i].update(bloons, deltaMs, owner);
                if (!activeProjectiles[i].getActive())
                {
                    activeProjectiles.RemoveAt(i);
                }
            }

            while (shotCooldownRemainingMs <= 0 && getClosestTargetInRange(position, bloons, range) != null)
            {
                spawnRadialTacks();
                shotCooldownRemainingMs += attackCooldownMs;
            }
        }

        public override void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f)
        {
            var screenPosition = toScreenPoint(position, worldScale, worldOffsetX, worldOffsetY);
            var bodyRadius = toScreenSize(18, worldScale);
            var barrelLength = toScreenSize(28, worldScale);

            foreach (var direction in getEightWayDirections())
            {
                var barrelEndX = screenPosition.x + (int)MathF.Round((float)(direction.x * barrelLength));
                var barrelEndY = screenPosition.y + (int)MathF.Round((float)(direction.y * barrelLength));
                display.drawLine(screenPosition.x, screenPosition.y, barrelEndX, barrelEndY, 248, 185, 221, 255);
            }

            display.drawFilledCircle(screenPosition.x, screenPosition.y, bodyRadius, Color.FromArgb(255, 231, 100, 180));

            // Geometric crossed two-tack emblem.
            var emblemArm = toScreenSize(8, worldScale);
            display.drawLine(
                screenPosition.x - emblemArm,
                screenPosition.y - emblemArm,
                screenPosition.x + emblemArm,
                screenPosition.y + emblemArm,
                20,
                20,
                20,
                255);
            display.drawLine(
                screenPosition.x - emblemArm,
                screenPosition.y + emblemArm,
                screenPosition.x + emblemArm,
                screenPosition.y - emblemArm,
                20,
                20,
                20,
                255);
            display.drawFilledCircle(screenPosition.x, screenPosition.y, toScreenSize(2, worldScale), Color.FromArgb(25, 25, 25));

            foreach (var projectile in activeProjectiles)
            {
                if (!projectile.getActive())
                {
                    continue;
                }

                projectile.draw(display, worldScale, worldOffsetX, worldOffsetY);
            }
        }

        private void spawnRadialTacks()
        {
            foreach (var direction in getEightWayDirections())
            {
                activeProjectiles.Add(new TackProjectile(
                    position,
                    direction.x,
                    direction.y,
                    damage,
                    projectileSpeedPixelsPerSecond,
                    projectileLifetimeMs));
            }
        }

        private List<(double x, double y)> getEightWayDirections()
        {
            var diagonal = Math.Sqrt(0.5);
            return new List<(double x, double y)>()
            {
                (1, 0),
                (diagonal, diagonal),
                (0, 1),
                (-diagonal, diagonal),
                (-1, 0),
                (-diagonal, -diagonal),
                (0, -1),
                (diagonal, -diagonal)
            };
        }
    }

    internal class TackProjectile
    {
        private readonly double directionX;
        private readonly double directionY;
        private readonly int damage;
        private readonly double speedPixelsPerSecond;
        private bool active;
        private double remainingLifetimeMs;
        private double xPos;
        private double yPos;

        public TackProjectile(
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

        public void update(List<Bloon> bloons, double deltaMs, Player owner)
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

                bloon.pop(damage, owner);
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

            var screenX = (int)MathF.Round(worldOffsetX + ((float)xPos * worldScale));
            var screenY = (int)MathF.Round(worldOffsetY + ((float)yPos * worldScale));
            var tipLength = Math.Max(1, (int)MathF.Round(5 * worldScale));

            display.drawLine(screenX - tipLength, screenY, screenX + tipLength, screenY, 25, 25, 25, 255);
            display.drawLine(screenX, screenY - tipLength, screenX, screenY + tipLength, 25, 25, 25, 255);
        }

        public ProjectileSnapshot toSnapshot()
        {
            return new ProjectileSnapshot
            {
                X = (float)xPos,
                Y = (float)yPos,
                RenderType = ProjectileRenderType.Cross,
                Size = 5,
                R = 25,
                G = 25,
                B = 25,
                A = 255,
            };
        }
    }
}
