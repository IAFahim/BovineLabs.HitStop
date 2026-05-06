using BovineLabs.Essence.Data;
using BovineLabs.HitStop.Data;
using BovineLabs.Reaction.Conditions;
using BovineLabs.Reaction.Data.Conditions;
using BovineLabs.Reaction.Data.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.HitStop
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct HitStopSystem : ISystem
    {
        private ComponentLookup<TargetsCustom> _customsLookup;
        private BufferLookup<Stat> _statsLookup;
        private ComponentLookup<HitStopState> _statesLookup;
        private ConditionEventWriter.Lookup _writersLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _customsLookup = state.GetComponentLookup<TargetsCustom>(true);
            _statsLookup = state.GetBufferLookup<Stat>(true);
            _statesLookup = state.GetComponentLookup<HitStopState>(true);
            _writersLookup.Create(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _customsLookup.Update(ref state);
            _statsLookup.Update(ref state);
            _statesLookup.Update(ref state);
            _writersLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new TriggerJob
            {
                Customs = _customsLookup,
                Stats = _statsLookup,
                States = _statesLookup,
                ECB = ecb.AsParallelWriter(),
                SeedOffset = (uint)(SystemAPI.Time.ElapsedTime * 10000.0)
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new UpdateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Writers = _writersLookup
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct TriggerJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<TargetsCustom> Customs;
            [ReadOnly] public BufferLookup<Stat> Stats;
            [ReadOnly] public ComponentLookup<HitStopState> States;
            public EntityCommandBuffer.ParallelWriter ECB;
            public uint SeedOffset;

            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in HitStopConfig cfg,
                in DynamicBuffer<ConditionEvent> events, in Targets targets)
            {
                if (cfg.OnHit == ConditionKey.Null || !HasEvent(events, cfg.OnHit)) return;

                if (!TryResolveTarget(cfg.Target, entity, targets, Customs, out var target)) return;

                var duration = 0f;
                var intensity = 0f;

                if (Stats.TryGetBuffer(target, out var targetStats))
                {
                    var map = targetStats.AsMap();
                    duration = map.GetValueFloat(cfg.Duration);
                    intensity = map.GetValueFloat(cfg.Intensity);
                }
                else if (Stats.TryGetBuffer(entity, out var selfStats))
                {
                    var map = selfStats.AsMap();
                    duration = map.GetValueFloat(cfg.Duration);
                    intensity = map.GetValueFloat(cfg.Intensity);
                }

                if (duration <= 0f) return;

                var newState = new HitStopState
                {
                    RemainingTime = duration,
                    CurrentIntensity = intensity,
                    Seed = SeedOffset + (uint)sortKey,
                    OnEnd = cfg.OnEnd,
                    Source = entity
                };

                if (States.HasComponent(target))
                {
                    ECB.SetComponentEnabled<HitStopState>(sortKey, target, true);
                    ECB.SetComponent(sortKey, target, newState);
                }
                else
                {
                    ECB.AddComponent(sortKey, target, newState);
                    ECB.AddComponent(sortKey, target, new PostTransformMatrix { Value = float4x4.identity });
                }
            }

            private static bool HasEvent(in DynamicBuffer<ConditionEvent> events, ConditionKey key)
            {
                foreach (var kvp in events.AsMap())
                    if (kvp.Key == key)
                        return true;
                return false;
            }

            private static bool TryResolveTarget(Target target, Entity self, in Targets targets,
                in ComponentLookup<TargetsCustom> customs, out Entity resolved)
            {
                resolved = target switch
                {
                    Target.Owner => targets.Owner,
                    Target.Source => targets.Source,
                    Target.Target => targets.Target,
                    Target.Self => self,
                    Target.Custom0 => customs.TryGetComponent(self, out var c) ? c.Target0 : Entity.Null,
                    Target.Custom1 => customs.TryGetComponent(self, out var c) ? c.Target1 : Entity.Null,
                    _ => Entity.Null
                };

                return resolved != Entity.Null;
            }
        }

        [BurstCompile]
        private partial struct UpdateJob : IJobEntity
        {
            public float DeltaTime;
            public ConditionEventWriter.Lookup Writers;

            private void Execute(Entity entity, ref HitStopState state, ref PostTransformMatrix ptm,
                EnabledRefRW<HitStopState> enabled)
            {
                state.RemainingTime -= DeltaTime;

                if (state.RemainingTime > 0f)
                {
                    var random = Random.CreateFromIndex(state.Seed);
                    state.Seed = random.NextUInt();
                    ptm.Value = float4x4.Translate(random.NextFloat3Direction() * state.CurrentIntensity);
                }
                else
                {
                    ptm.Value = float4x4.identity;
                    enabled.ValueRW = false;

                    if (state.OnEnd != ConditionKey.Null && Writers.TryGet(state.Source, out var writer))
                        writer.Trigger(state.OnEnd, 1);
                }
            }
        }
    }
}