/*
*
*   Any game object interested in listening for input events will need to register itself 
*       with this manager.   It handles the informing of all listener objects when an 
*       event is raised.
*   @author Michael Heron
*   @version 1.0
*   
*/

using System.Collections.Generic;

namespace Shard
{

    abstract class InputSystem
    {
        private List<InputListener> myListeners;

        public virtual void initialize()
        {
        }

        public InputSystem()
        {
            myListeners = new List<InputListener>();
        }

        public void addListener(InputListener il)
        {
            if (myListeners.Contains(il) == false)
            {
                myListeners.Add(il);
            }
        }

        public void removeListener(InputListener il)
        {
            myListeners.Remove(il);
        }

        public void informListeners(IEvents e, string eventType)
        {
            string type = null;
            InputListener il;
            if (e is InputEvent)
            {
                type = "InputEvent";
            }else if (e is WindowEvent)
            {
                type = "WindowEvent";
            }
            
            for (int i = 0; i < myListeners.Count; i++)
            {
                il = myListeners[i];

                if (il == null)
                {
                    continue;
                }

                //handle different event types
                switch (type)
                {
                    case "InputEvent":
                        il.handleInput((InputEvent)e, eventType);
                        break;
                    case "WindowEvent":
                        il.handleWindowEvent((WindowEvent)e, eventType);
                        break;
                    default:
                        Debug.Log("Unknown event type: " + type);
                        break;
                }
            }
        }
        public abstract void getInput();
    }
}
