using BovineLabs.Reaction.Data.Conditions;
using Unity.Entities;

namespace BovineLabs.HitStop.Data
{
    public struct HitStopState : IComponentData, IEnableableComponent
    {
        public float RemainingTime;
        public float CurrentIntensity;
        public uint Seed;
        public ConditionKey OnEnd;
        public Entity Source;
    }
}