using SDL;
using static SDL.SDL3;
using Shard;
using System.Drawing;

namespace SpaceInvaders
{
    class Spaceship : GameObject, InputListener, CollisionHandler
    {
        bool left, right, down, up;
        float fireCounter, fireDelay;


        public override void initialize()
        {

            this.Transform.X = 100.0f;
            this.Transform.Y = 800.0f;
            this.Transform.SpritePath = Bootstrap.getAssetManager().getAssetPath("player.png");


            fireDelay = 2;
            fireCounter = fireDelay;

            Bootstrap.getInput().addListener(this);

            setPhysicsEnabled();

            MyBody.addRectCollider();

            addTag("Player");


        }

        public void fireBullet()
        {
            /*
            if (fireCounter < fireDelay)
            {
                return;
            }
            */

            Bullet b = new Bullet();

            b.setupBullet(this.Transform.Centre.X, this.Transform.Centre.Y);
            b.Dir = -1;
            b.DestroyTag = "Invader";

            fireCounter = 0;

        }

        public void handleInput(InputEvent inp, string eventType)
        {

            if (Bootstrap.getRunningGame().isRunning() == false)
            {
                return;
            }

            if (eventType == "KeyDown")
            {

                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_D)
                {
                    right = true;
                }

                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_A)
                {
                    left = true;
                }

                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_S)
                {
                     down = true;
                }
                
                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_W)
                {
                    up = true;
                }
            }
            else if (eventType == "KeyUp")
            {


                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_D)
                {
                    right = false;
                }

                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_A)
                {
                    left = false;
                }

                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_S)
                {
                    down = false;
                }

                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_W)
                {
                    up = false;
                }
            }



            if (eventType == "KeyUp")
            {
                if (inp.Key == (int)SDL_Scancode.SDL_SCANCODE_SPACE)
                {
                        fireBullet();
                }
            }
        }

        public override void update()
        {
            float amount = (float)(100 * Bootstrap.getDeltaTime());

            fireCounter += (float)Bootstrap.getDeltaTime();

            if (left)
            {
                this.Transform.translate(-10 * amount, 0);
            }

            if (right)
            {
                this.Transform.translate(10 * amount, 0);
            }

            if (up)
            {
                this.Transform.translate(0, -10 * amount);
            }

            if (down)
            {
                this.Transform.translate(0, 10 * amount);
            }
            
            fireBullet();

            Bootstrap.getDisplay().addToDraw(this);
        }

        public void onCollisionEnter(PhysicsBody x)
        {

        }

        public void onCollisionExit(PhysicsBody x)
        {

            MyBody.DebugColor = Color.Green;
        }

        public void onCollisionStay(PhysicsBody x)
        {
            MyBody.DebugColor = Color.Blue;
        }

        public override string ToString()
        {
            return "Spaceship: [" + Transform.X + ", " + Transform.Y + ", " + Transform.Wid + ", " + Transform.Ht + "]";
        }
        
        public void handleWindowEvent(WindowEvent windowEvent, string eventType)
        {
        
        }

    }
}
