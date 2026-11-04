namespace TheHouseIsAlive
{
    public enum EntityState
    {
        Dormant,
        Hiding,
        Scurrying,
        Peeking,
        Stalking,
        Frozen
    }

    /// <summary>
    /// Pure "Don't Look Away" transition rules. Evaluated every frame from raw gaze,
    /// so the creature freezes on the same frame it enters the direct cone.
    /// </summary>
    public static class CreatureBrain
    {
        public static EntityState Next(EntityState current, bool directlyObserved)
        {
            if (directlyObserved)
            {
                switch (current)
                {
                    case EntityState.Stalking: return EntityState.Frozen;
                    case EntityState.Peeking:
                    case EntityState.Scurrying: return EntityState.Hiding;
                    default: return current;
                }
            }

            switch (current)
            {
                case EntityState.Frozen: return EntityState.Stalking;
                default: return current;
            }
        }

        /// <summary>Only these states may translate the creature.</summary>
        public static bool CanMove(EntityState state, bool directlyObserved)
        {
            if (directlyObserved) return false;
            return state == EntityState.Stalking || state == EntityState.Scurrying;
        }
    }
}
