using System.Collections.Generic;

namespace Shard.Bloons
{
    internal interface Tower
    {
        string getName();
        int getCost();
        LPoint getPosition();
        List<ProjectileSnapshot> getProjectileSnapshots();
        TowerSnapshot createSnapshot(int ownerId);
        void applySnapshot(TowerSnapshot snapshot);
        
        void update(List<Bloon> bloons, double deltaMs, LPoint pointerWorldPosition, Player owner);
        void draw(Display display, float worldScale = 1.0f, float worldOffsetX = 0.0f, float worldOffsetY = 0.0f);
    }
}
