using BovineLabs.Essence.Data;
using BovineLabs.Reaction.Data.Conditions;
using BovineLabs.Reaction.Data.Core;
using Unity.Entities;

namespace BovineLabs.HitStop.Data
{
    public struct HitStopConfig : IComponentData
    {
        public ConditionKey OnHit;
        public ConditionKey OnEnd;
        public StatKey Intensity;
        public StatKey Duration;
        public Target Target;
    }
}
