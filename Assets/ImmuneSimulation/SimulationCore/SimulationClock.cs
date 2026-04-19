namespace ImmuneSimulation.SimulationCore
{
    /// <summary>Accumulator for fixed timestep; tick index is owned by SimulationController.</summary>
    public sealed class SimulationClock
    {
        public float FixedDeltaTime { get; }
        public float Accumulator { get; private set; }

        public SimulationClock(float fixedDeltaTime)
        {
            FixedDeltaTime = fixedDeltaTime;
        }

        /// <summary>Returns number of fixed ticks to run this frame.</summary>
        public int Advance(float frameDeltaTime, int maxTicksPerFrame = 8)
        {
            Accumulator += frameDeltaTime;
            int ticks = 0;
            while (Accumulator >= FixedDeltaTime && ticks < maxTicksPerFrame)
            {
                Accumulator -= FixedDeltaTime;
                ticks++;
            }

            return ticks;
        }

        public void Reset()
        {
            Accumulator = 0f;
        }
    }
}
