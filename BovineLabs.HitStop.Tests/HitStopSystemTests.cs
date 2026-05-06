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
        public void HitStopState_RemainingTime_SetAndGet()
        {
            var archetype = Manager.CreateArchetype(typeof(HitStopState));
            var entity = Manager.CreateEntity(archetype);

            Manager.SetComponentData(entity, new HitStopState { RemainingTime = 0.5f });
            Assert.AreEqual(0.5f, Manager.GetComponentData<HitStopState>(entity).RemainingTime);
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
            var archetype = Manager.CreateArchetype(typeof(HitStopState));

            using var entities = Manager.CreateEntity(archetype, 3, Allocator.Temp);

            Manager.SetComponentData(entities[0], new HitStopState { RemainingTime = 0.1f });
            Manager.SetComponentData(entities[1], new HitStopState { RemainingTime = 0.2f });
            Manager.SetComponentData(entities[2], new HitStopState { RemainingTime = 0.3f });

            Assert.AreEqual(0.1f, Manager.GetComponentData<HitStopState>(entities[0]).RemainingTime);
            Assert.AreEqual(0.2f, Manager.GetComponentData<HitStopState>(entities[1]).RemainingTime);
            Assert.AreEqual(0.3f, Manager.GetComponentData<HitStopState>(entities[2]).RemainingTime);
        }
    }
}