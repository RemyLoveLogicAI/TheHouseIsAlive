using System;

namespace TheHouseIsAlive
{
    public enum ScarePhase
    {
        Intro,
        Phase1Knock,
        Phase2Scurry,
        Phase3Stalk,
        Finale,
        Ended
    }

    /// <summary>
    /// Deterministic encounter clock. Default total is ~10 minutes; scale it down
    /// (e.g. timeScale 0.1) for fast simulator runs.
    /// </summary>
    [Serializable]
    public class ScareTimeline
    {
        public float introSeconds = 8f;
        public float phase1Seconds = 150f;
        public float phase2Seconds = 180f;
        public float phase3Seconds = 210f;
        public float finaleSeconds = 50f;
        public float timeScale = 1f;

        public float TotalSeconds =>
            (introSeconds + phase1Seconds + phase2Seconds + phase3Seconds + finaleSeconds) * Scale;

        private float Scale => timeScale <= 0f ? 1f : timeScale;

        public ScarePhase PhaseAt(float elapsedSeconds)
        {
            for (ScarePhase p = ScarePhase.Finale; p > ScarePhase.Intro; p--)
            {
                if (elapsedSeconds >= PhaseStart(p))
                {
                    return elapsedSeconds >= PhaseStart(ScarePhase.Ended) ? ScarePhase.Ended : p;
                }
            }
            return ScarePhase.Intro;
        }

        public float PhaseStart(ScarePhase phase)
        {
            float s = Scale;
            float edge = 0f;
            if (phase == ScarePhase.Intro) return edge;
            edge += introSeconds * s;
            if (phase == ScarePhase.Phase1Knock) return edge;
            edge += phase1Seconds * s;
            if (phase == ScarePhase.Phase2Scurry) return edge;
            edge += phase2Seconds * s;
            if (phase == ScarePhase.Phase3Stalk) return edge;
            edge += phase3Seconds * s;
            if (phase == ScarePhase.Finale) return edge;
            return edge + finaleSeconds * s;
        }
    }
}
