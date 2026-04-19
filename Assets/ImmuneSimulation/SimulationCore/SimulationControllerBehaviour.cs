using UnityEngine;

namespace ImmuneSimulation.SimulationCore
{
    /// <summary>Milestone 0: fixed-timestep driver only — no agents, fields, or UI.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SimulationControllerBehaviour : MonoBehaviour
    {
        [SerializeField] float _fixedDeltaTime = 0.02f;
        [SerializeField] bool _paused;
        [SerializeField] ulong _simulationTick;

        SimulationClock _clock;

        public float FixedDeltaTime => _clock != null ? _clock.FixedDeltaTime : _fixedDeltaTime;
        public bool Paused
        {
            get => _paused;
            set => _paused = value;
        }

        public ulong SimulationTick => _simulationTick;

        void Awake()
        {
            RebuildClock();
        }

        void OnValidate()
        {
            if (_fixedDeltaTime < 0.0001f)
                _fixedDeltaTime = 0.0001f;
        }

        void RebuildClock()
        {
            _clock = new SimulationClock(_fixedDeltaTime);
        }

        void Update()
        {
            if (_paused || _clock == null)
                return;
            int ticks = _clock.Advance(Time.deltaTime);
            for (int i = 0; i < ticks; i++)
                SimulateOneFixedTick();
        }

        /// <summary>Advances exactly one fixed simulation step (for pause/step debugging).</summary>
        [ContextMenu("Step one fixed tick")]
        public void StepOnce()
        {
            if (_clock == null)
                RebuildClock();
            SimulateOneFixedTick();
        }

        void SimulateOneFixedTick()
        {
            _simulationTick++;
            // Milestone 1+: spatial queries, cells, fields, etc.
        }
    }
}
