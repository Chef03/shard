using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shard.Bloons
{
    internal class Player
    {
        private int lives;
        private int money;
        private List<Monkey> monkeys;
        private int playerID;
        private bool isHost;
        private bool isConnected;
        private string name;
        private string IPAddress;

        public Player(int playerID, string name, bool isHost, string IPAddress)
        {
            this.playerID = playerID;
            this.name = name;
            this.isHost = isHost;
            this.IPAddress = IPAddress;
            this.lives = 100; // default starting lives
            this.money = 3000; // default starting money
            this.monkeys = new List<Monkey>();
            this.isConnected = false; // assume player is connected when created
        }

        public int getMoney()
        {
            return money;
        }

        public int getLives()
        {
            return lives;
        }

        public void addMoney(int n)
        {
            money += n;
        }
        public void removeMoney(int n)
        {
            money -= n;
        }

        public void loseLives(int n)
        {
            lives -= n;
        }

    }
}
