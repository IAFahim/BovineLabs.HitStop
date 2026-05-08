using BovineLabs.Core.Model;
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
        private ComponentLookup<HitStopDuration> _durationsLookup;
        private ComponentLookup<HitStopRemainingTime> _remainingLookup;
        private ComponentLookup<HitStopActive> _activeLookup;
        private TimerEnableable<HitStopActive, HitStopRemainingTime, HitStopState, HitStopDuration> timer;
        private ConditionEventWriter.Lookup _writersLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _customsLookup = state.GetComponentLookup<TargetsCustom>(true);
            _statsLookup = state.GetBufferLookup<Stat>(true);
            _statesLookup = state.GetComponentLookup<HitStopState>(true);
            _durationsLookup = state.GetComponentLookup<HitStopDuration>(true);
            _remainingLookup = state.GetComponentLookup<HitStopRemainingTime>(true);
            _activeLookup = state.GetComponentLookup<HitStopActive>(true);
            _writersLookup.Create(ref state);

            timer.OnCreate(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _customsLookup.Update(ref state);
            _statsLookup.Update(ref state);
            _statesLookup.Update(ref state);
            _durationsLookup.Update(ref state);
            _remainingLookup.Update(ref state);
            _activeLookup.Update(ref state);
            _writersLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new TriggerJob
            {
                Customs = _customsLookup,
                Stats = _statsLookup,
                States = _statesLookup,
                Durations = _durationsLookup,
                Remaining = _remainingLookup,
                Active = _activeLookup,
                ECB = ecb.AsParallelWriter(),
                SeedOffset = (uint)(SystemAPI.Time.ElapsedTime * 10000.0)
            }.ScheduleParallel(state.Dependency);

            // TimerEnableable handles vectorized timer tick and auto-disable of HitStopActive
            timer.OnUpdate(ref state);

            // ShakeJob handles visual shake and OnEnd event firing
            state.Dependency = new ShakeJob
            {
                Writers = _writersLookup
            }.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Applies visual shake via PostTransformMatrix while active, and fires OnEnd condition event
        /// when the timer expires (remaining hits 0). TimerEnableable handles the timer tick and
        /// auto-disable; this job resets the transform and fires events on expiry.
        /// </summary>
        [BurstCompile]
        [WithAll(typeof(HitStopActive))]
        private partial struct ShakeJob : IJobEntity
        {
            public ConditionEventWriter.Lookup Writers;

            private void Execute(Entity entity, ref HitStopState state, ref PostTransformMatrix ptm,
                ref HitStopRemainingTime remaining, EnabledRefRW<HitStopActive> active)
            {
                if (remaining.Value > 0f)
                {
                    var random = Unity.Mathematics.Random.CreateFromIndex(state.Seed);
                    state.Seed = random.NextUInt();
                    ptm.Value = float4x4.Translate(random.NextFloat3Direction() * state.CurrentIntensity);
                }
                else
                {
                    ptm.Value = float4x4.identity;
                    active.ValueRW = false;

                    if (state.OnEnd != ConditionKey.Null && Writers.TryGet(state.Source, out var writer))
                    {
                        writer.Trigger(state.OnEnd, 1);
                    }
                }
            }
        }

        [BurstCompile]
        private partial struct TriggerJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<TargetsCustom> Customs;
            [ReadOnly] public BufferLookup<Stat> Stats;
            [ReadOnly] public ComponentLookup<HitStopState> States;
            [ReadOnly] public ComponentLookup<HitStopDuration> Durations;
            [ReadOnly] public ComponentLookup<HitStopRemainingTime> Remaining;
            [ReadOnly] public ComponentLookup<HitStopActive> Active;
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

                var newDuration = new HitStopDuration { Value = duration };

                if (Active.HasComponent(target))
                {
                    ECB.SetComponentEnabled<HitStopActive>(sortKey, target, true);
                    ECB.SetComponentEnabled<HitStopState>(sortKey, target, true);
                    ECB.SetComponent(sortKey, target, new HitStopState
                    {
                        CurrentIntensity = intensity,
                        Seed = SeedOffset + (uint)sortKey,
                        OnEnd = cfg.OnEnd,
                        Source = entity
                    });
                    ECB.SetComponent(sortKey, target, newDuration);
                    ECB.SetComponent(sortKey, target, new HitStopRemainingTime { Value = duration });
                }
                else
                {
                    ECB.AddComponent(sortKey, target, new HitStopState
                    {
                        CurrentIntensity = intensity,
                        Seed = SeedOffset + (uint)sortKey,
                        OnEnd = cfg.OnEnd,
                        Source = entity
                    });
                    ECB.AddComponent(sortKey, target, newDuration);
                    ECB.AddComponent(sortKey, target, new HitStopRemainingTime { Value = duration });
                    ECB.AddComponent(sortKey, target, new HitStopActive());
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
    }
}
