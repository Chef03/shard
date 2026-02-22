/*
*
*   This class intentionally left blank.  
*   @author Michael Heron
*   @version 1.0
*   
*/

namespace Shard
{
    abstract public class Sound
    {
        abstract public void playSound(string file);
        public virtual void setVolumePercent(int volumePercent)
        {
        }

        public virtual int getVolumePercent()
        {
            return 100;
        }

    }
}
