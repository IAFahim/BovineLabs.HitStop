using BovineLabs.Reaction.Data.Conditions;
using Unity.Entities;

namespace BovineLabs.HitStop.Data
{
    public struct HitStopState : IComponentData, IEnableableComponent
    {
        public float CurrentIntensity;
        public uint Seed;
        public ConditionKey OnEnd;
        public Entity Source;
    }

    public struct HitStopDuration : IComponentData
    {
        public float Value;
    }

    public struct HitStopRemainingTime : IComponentData
    {
        public float Value;
    }

    public struct HitStopActive : IComponentData, IEnableableComponent
    {
    }
}
