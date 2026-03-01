using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shard.Bloons
{
    internal class Monkey
    {
        private double range;
        private int damage;
        private bool camoDetection;
        private bool canPopLead;
        private int cost;
        private LPoint position;
        private double attackSpeed; // attacks per second
        private Projectile projectile;
        private Image sprite;
        private double size; //amount of space the monkey takes up

        public Monkey(double range, int damage, bool camoDetection, bool canPopLead, int cost, LPoint position, double attackSpeed, Image sprite, double size)
        {
            this.range = range;
            this.damage = damage;
            this.camoDetection = camoDetection;
            this.canPopLead = canPopLead;
            this.cost = cost;
            this.position = position;
            this.attackSpeed = attackSpeed;
            this.sprite = sprite;
            this.size = size;
        }
    }

    internal class Projectile
    {
        
    }
}
