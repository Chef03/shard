/*
*
*   This is a general, simple container for all the information someone might want to know about 
*       keyboard or mouse input.   The same object is used for both, so use your common sense 
*       to work out whether you can use the contents of, say 'x' and 'y' when registering for 
*       a key event.
*   @author Michael Heron
*   @version 1.0
*   
*/


//TODOS: CAN add mouse scroll support
namespace Shard
{
    interface IEvents
    {
        abstract string toString();
    }
    
    class InputEvent : IEvents
    {
        private int x;
        private int y;
        private int button;
        private int key;
        private string classification;

        public int X
        {
            get => x;
            set => x = value;
        }
        public int Y
        {
            get => y;
            set => y = value;
        }
        public int Button
        {
            get => button;
            set => button = value;
        }
        public string Classification
        {
            get => classification;
            set => classification = value;
        }
        public int Key
        {
            get => key;
            set => key = value;
        }

        // additions

        public string toString()
        {
            if(button != 0)
            {
                return "InputEvent: " + classification + " at (" + x + ", " + y + ") with button " + button;
            }
            else if(key != 0)
            {
                return "InputEvent: " + classification + " with key " + key;
            }
            else
            {
                return "InputEvent: " + classification;
            }
        }
    }
    
    class WindowEvent : IEvents
    {
        private int width;
        private int height;
        private bool closeRequested;
    
    
        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }
        public bool CloseRequested { get => closeRequested; set => closeRequested = value; }

        public string toString()
        {
            return "str";
        }
    }
}
