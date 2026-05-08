using BovineLabs.HitStop.Data;
using BovineLabs.Testing;
using NUnit.Framework;
using Unity.Collections;
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
            Assert.DoesNotThrow(() => { World.CreateSystem<HitStopSystem>(); });
        }

        [Test]
        public void Update_WithNoEntities_DoesNotThrow()
        {
            var system = World.CreateSystem<HitStopSystem>();
            Assert.DoesNotThrow(() => system.Update(WorldUnmanaged));
        }

        [Test]
        public void HitStopState_Default_IsEnabledOnEntity()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopState));
            var entity = Manager.CreateEntity(archetype);

            Assert.IsTrue(Manager.IsComponentEnabled<HitStopState>(entity));
        }

        [Test]
        public void HitStopState_CanBeEnabled()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopState));
            var entity = Manager.CreateEntity(archetype);

            Manager.SetComponentEnabled<HitStopState>(entity, true);
            Assert.IsTrue(Manager.IsComponentEnabled<HitStopState>(entity));
        }

        [Test]
        public void HitStopState_CanBeToggled()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopState));
            var entity = Manager.CreateEntity(archetype);

            Manager.SetComponentEnabled<HitStopState>(entity, true);
            Assert.IsTrue(Manager.IsComponentEnabled<HitStopState>(entity));

            Manager.SetComponentEnabled<HitStopState>(entity, false);
            Assert.IsFalse(Manager.IsComponentEnabled<HitStopState>(entity));
        }

        [Test]
        public void HitStopConfig_IsAddedAsComponent()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopConfig));
            var entity = Manager.CreateEntity(archetype);

            Assert.IsTrue(Manager.HasComponent<HitStopConfig>(entity));
        }

        [Test]
        public void HitStopRemainingTime_SetAndGet()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopRemainingTime));
            var entity = Manager.CreateEntity(archetype);

            Manager.SetComponentData(entity, new HitStopRemainingTime { Value = 0.5f });
            Assert.AreEqual(0.5f, Manager.GetComponentData<HitStopRemainingTime>(entity).Value);
        }

        [Test]
        public void HitStopState_Intensity_SetAndGet()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopState));
            var entity = Manager.CreateEntity(archetype);

            Manager.SetComponentData(entity, new HitStopState { CurrentIntensity = 0.3f });
            Assert.AreEqual(0.3f, Manager.GetComponentData<HitStopState>(entity).CurrentIntensity);
        }

        [Test]
        public void PostTransformMatrix_WithHitStopState_CanCoexist()
        {
            var archetype = Manager.CreateArchetype(
                typeof(HitStopState),
                typeof(PostTransformMatrix));
            var entity = Manager.CreateEntity(archetype);

            Manager.SetComponentData(entity, new PostTransformMatrix { Value = float4x4.identity });
            Assert.IsTrue(Manager.HasComponent<PostTransformMatrix>(entity));
        }

        [Test]
        public void MultipleEntities_WithHitStopState_AllEnabledByDefault()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopState));

            using var entities = Manager.CreateEntity(archetype, 5, Allocator.Temp);

            for (var i = 0; i < entities.Length; i++)
                Assert.IsTrue(Manager.IsComponentEnabled<HitStopState>(entities[i]),
                    $"Entity {i} should have HitStopState enabled by default");
        }

        [Test]
        public void MultipleEntities_IndependentState()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopState), typeof(HitStopRemainingTime));

            using var entities = Manager.CreateEntity(archetype, 3, Allocator.Temp);

            Manager.SetComponentData(entities[0], new HitStopRemainingTime { Value = 0.1f });
            Manager.SetComponentData(entities[1], new HitStopRemainingTime { Value = 0.2f });
            Manager.SetComponentData(entities[2], new HitStopRemainingTime { Value = 0.3f });

            Assert.AreEqual(0.1f, Manager.GetComponentData<HitStopRemainingTime>(entities[0]).Value);
            Assert.AreEqual(0.2f, Manager.GetComponentData<HitStopRemainingTime>(entities[1]).Value);
            Assert.AreEqual(0.3f, Manager.GetComponentData<HitStopRemainingTime>(entities[2]).Value);
        }
    }
}
