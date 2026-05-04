using BovineLabs.HitStop;
using BovineLabs.HitStop.Data;
using BovineLabs.Testing;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.HitStop.Tests
{
    public class HitStopSystemTests : ECSTestsFixture
    {
        [Test]
        public void OnCreate_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                this.World.CreateSystem<HitStopSystem>();
            });
        }

        [Test]
        public void Update_WithNoEntities_DoesNotThrow()
        {
            var system = this.World.CreateSystem<HitStopSystem>();
            Assert.DoesNotThrow(() => system.Update(this.WorldUnmanaged));
        }

        [Test]
        public void HitStopState_Default_IsEnabledOnEntity()
        {
            var archetype = this.Manager.CreateArchetype(typeof(HitStopState));
            var entity = this.Manager.CreateEntity(archetype);

            Assert.IsTrue(this.Manager.IsComponentEnabled<HitStopState>(entity));
        }

        [Test]
        public void HitStopState_CanBeEnabled()
        {
            var archetype = this.Manager.CreateArchetype(typeof(HitStopState));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentEnabled<HitStopState>(entity, true);
            Assert.IsTrue(this.Manager.IsComponentEnabled<HitStopState>(entity));
        }

        [Test]
        public void HitStopState_CanBeToggled()
        {
            var archetype = this.Manager.CreateArchetype(typeof(HitStopState));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentEnabled<HitStopState>(entity, true);
            Assert.IsTrue(this.Manager.IsComponentEnabled<HitStopState>(entity));

            this.Manager.SetComponentEnabled<HitStopState>(entity, false);
            Assert.IsFalse(this.Manager.IsComponentEnabled<HitStopState>(entity));
        }

        [Test]
        public void HitStopConfig_IsAddedAsComponent()
        {
            var archetype = this.Manager.CreateArchetype(typeof(HitStopConfig));
            var entity = this.Manager.CreateEntity(archetype);

            Assert.IsTrue(this.Manager.HasComponent<HitStopConfig>(entity));
        }

        [Test]
        public void HitStopState_RemainingTime_SetAndGet()
        {
            var archetype = this.Manager.CreateArchetype(typeof(HitStopState));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new HitStopState { RemainingTime = 0.5f });
            Assert.AreEqual(0.5f, this.Manager.GetComponentData<HitStopState>(entity).RemainingTime);
        }

        [Test]
        public void HitStopState_Intensity_SetAndGet()
        {
            var archetype = this.Manager.CreateArchetype(typeof(HitStopState));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new HitStopState { CurrentIntensity = 0.3f });
            Assert.AreEqual(0.3f, this.Manager.GetComponentData<HitStopState>(entity).CurrentIntensity);
        }

        [Test]
        public void PostTransformMatrix_WithHitStopState_CanCoexist()
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(HitStopState),
                typeof(PostTransformMatrix));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new PostTransformMatrix { Value = float4x4.identity });
            Assert.IsTrue(this.Manager.HasComponent<PostTransformMatrix>(entity));
        }

        [Test]
        public void MultipleEntities_WithHitStopState_AllDisabledByDefault()
        {
            var archetype = this.Manager.CreateArchetype(typeof(HitStopState));

            using var entities = this.Manager.CreateEntity(archetype, 5, Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Assert.IsFalse(this.Manager.IsComponentEnabled<HitStopState>(entities[i]),
                    $"Entity {i} should have HitStopState disabled by default");
            }
        }

        [Test]
        public void MultipleEntities_IndependentState()
        {
            var archetype = this.Manager.CreateArchetype(typeof(HitStopState));

            using var entities = this.Manager.CreateEntity(archetype, 3, Unity.Collections.Allocator.Temp);

            this.Manager.SetComponentData(entities[0], new HitStopState { RemainingTime = 0.1f });
            this.Manager.SetComponentData(entities[1], new HitStopState { RemainingTime = 0.2f });
            this.Manager.SetComponentData(entities[2], new HitStopState { RemainingTime = 0.3f });

            Assert.AreEqual(0.1f, this.Manager.GetComponentData<HitStopState>(entities[0]).RemainingTime);
            Assert.AreEqual(0.2f, this.Manager.GetComponentData<HitStopState>(entities[1]).RemainingTime);
            Assert.AreEqual(0.3f, this.Manager.GetComponentData<HitStopState>(entities[2]).RemainingTime);
        }
    }
}
