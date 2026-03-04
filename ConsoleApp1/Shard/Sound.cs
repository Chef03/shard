/*
*
*   This class intentionally left blank.  
*   @author Michael Heron
*   @version 1.0
*   
*/

using SDL;

namespace Shard
{
    public abstract class Sound
    {
        public abstract unsafe MIX_Track* playSound(string file, bool loop = false, float left = 0, float right = 0, int volume = 1);

        public abstract unsafe void pan(MIX_Track* track, float left, float right);
        
        public virtual unsafe void setVolumePercent(MIX_Track* track, int volumePercent)
        {
        }

        public virtual int getVolumePercent()
        {
            return 100;
        }

    }
}
