using BovineLabs.Essence.Authoring;
using BovineLabs.HitStop.Data;
using BovineLabs.Reaction.Authoring.Conditions;
using BovineLabs.Reaction.Data.Conditions;
using BovineLabs.Reaction.Data.Core;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.HitStop.Authoring
{
    [DisallowMultipleComponent]
    public class HitStopAuthoring : MonoBehaviour
    {
        public ConditionEventObject OnHit;
        public ConditionEventObject OnEnd;
        public StatSchemaObject Intensity;
        public StatSchemaObject Duration;
        public Target Target = Target.Target;

        private class Baker : Baker<HitStopAuthoring>
        {
            public override void Bake(HitStopAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new HitStopConfig
                {
                    OnHit = authoring.OnHit != null ? authoring.OnHit.Key : ConditionKey.Null,
                    OnEnd = authoring.OnEnd != null ? authoring.OnEnd.Key : ConditionKey.Null,
                    Intensity = authoring.Intensity != null ? authoring.Intensity.Key : default,
                    Duration = authoring.Duration != null ? authoring.Duration.Key : default,
                    Target = authoring.Target
                });

                AddComponent<HitStopState>(entity);
                SetComponentEnabled<HitStopState>(entity, false);
            }
        }
    }
}