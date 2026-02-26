/*
*
*   SDL provides an input layer, and we're using that.  This class tracks input, anchors it to the 
*       timing of the game loop, and converts the SDL events into one that is more abstract so games 
*       can be written more interchangeably.
*   @author Michael Heron
*   @version 1.0
*   
*/

using SDL;
using static SDL.SDL3;

namespace Shard
{

    // We'll be using SDL3 here to provide our underlying input system.
    unsafe class InputFramework : InputSystem
    {

        double tick, timeInterval;
        public override void getInput()
        {

            SDL_Event ev;
            bool hasEvent;
            InputEvent ie;

            tick += Bootstrap.getDeltaTime();

            if (tick < timeInterval)
            {
                return;
            }

            while (tick >= timeInterval)
            {

                hasEvent = SDL_PollEvent(&ev);


                if (!hasEvent)
                {
                    return;
                }

                ie = new InputEvent();

                if ((SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_MOUSE_MOTION)
                {
                    SDL_MouseMotionEvent mot;

                    mot = ev.motion;

                    ie.X = (int)mot.x;
                    ie.Y = (int)mot.y;

                    informListeners(ie, "MouseMotion");
                }

                if ((SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN)
                {
                    SDL_MouseButtonEvent butt;

                    butt = ev.button;

                    ie.Button = (int)butt.button;
                    ie.X = (int)butt.x;
                    ie.Y = (int)butt.y;

                    informListeners(ie, "MouseDown");
                }

                if ((SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP)
                {
                    SDL_MouseButtonEvent butt;

                    butt = ev.button;

                    ie.Button = (int)butt.button;
                    ie.X = (int)butt.x;
                    ie.Y = (int)butt.y;

                    informListeners(ie, "MouseUp");
                }

                if ((SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_MOUSE_WHEEL)
                {
                    SDL_MouseWheelEvent wh;

                    wh = ev.wheel;

                    ie.X = (int)wh.x;
                    ie.Y = (int)wh.y;

                    informListeners(ie, "MouseWheel");
                }


                if ((SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_KEY_DOWN)
                {
                    ie.Key = (int)ev.key.scancode;
                    Debug.getInstance().log("Keydown: " + ie.Key);
                    informListeners(ie, "KeyDown");
                }

                if ((SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_KEY_UP)
                {
                    ie.Key = (int)ev.key.scancode;
                    informListeners(ie, "KeyUp");
                }

                tick -= timeInterval;

            }


        }

        public override void initialize()
        {
            tick = 0;
            timeInterval = 1.0 / 60.0;
        }

    }
}