using System.Collections.Generic;

namespace Shard.Bloons
{
    internal interface Tower
    {
        string getName();
        void update(List<Bloon> bloons, double deltaMs, LPoint pointerWorldPosition);
        void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f);
    }
}
