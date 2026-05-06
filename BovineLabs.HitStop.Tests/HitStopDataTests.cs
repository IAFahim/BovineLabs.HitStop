using BovineLabs.Essence.Data;
using BovineLabs.HitStop.Data;
using BovineLabs.Reaction.Data.Conditions;
using BovineLabs.Reaction.Data.Core;
using NUnit.Framework;
using Unity.Entities;

namespace BovineLabs.HitStop.Tests
{
    [TestFixture]
    public class HitStopConfigTests
    {
        [Test]
        public void Default_OnHit_IsNull()
        {
            var cfg = new HitStopConfig();
            Assert.AreEqual(ConditionKey.Null, cfg.OnHit);
        }

        [Test]
        public void Default_OnEnd_IsNull()
        {
            var cfg = new HitStopConfig();
            Assert.AreEqual(ConditionKey.Null, cfg.OnEnd);
        }

        [Test]
        public void Default_Intensity_IsDefault()
        {
            var cfg = new HitStopConfig();
            Assert.AreEqual(default(StatKey), cfg.Intensity);
        }

        [Test]
        public void Default_Duration_IsDefault()
        {
            var cfg = new HitStopConfig();
            Assert.AreEqual(default(StatKey), cfg.Duration);
        }

        [Test]
        public void Default_Target_IsNone()
        {
            var cfg = new HitStopConfig();
            Assert.AreEqual(Target.None, cfg.Target);
        }

        [Test]
        public void Fields_SetCorrectly()
        {
            var cfg = new HitStopConfig
            {
                OnHit = 5,
                OnEnd = 10,
                Intensity = 100,
                Duration = 200,
                Target = Target.Target
            };
            Assert.AreEqual((ConditionKey)5, cfg.OnHit);
            Assert.AreEqual((ConditionKey)10, cfg.OnEnd);
            Assert.AreEqual((StatKey)100, cfg.Intensity);
            Assert.AreEqual((StatKey)200, cfg.Duration);
            Assert.AreEqual(Target.Target, cfg.Target);
        }

        [Test]
        public void Target_CanBeSet_ToAllEnumValues()
        {
            var values = new[]
            {
                Target.None,
                Target.Target,
                Target.Owner,
                Target.Source,
                Target.Self,
                Target.Custom0,
                Target.Custom1
            };

            foreach (var target in values)
            {
                var cfg = new HitStopConfig { Target = target };
                Assert.AreEqual(target, cfg.Target);
            }
        }
    }

    [TestFixture]
    public class HitStopStateTests
    {
        [Test]
        public void Default_RemainingTime_IsZero()
        {
            var state = new HitStopState();
            Assert.AreEqual(0f, state.RemainingTime);
        }

        [Test]
        public void Default_CurrentIntensity_IsZero()
        {
            var state = new HitStopState();
            Assert.AreEqual(0f, state.CurrentIntensity);
        }

        [Test]
        public void Default_Seed_IsZero()
        {
            var state = new HitStopState();
            Assert.AreEqual(0u, state.Seed);
        }

        [Test]
        public void Default_OnEnd_IsNull()
        {
            var state = new HitStopState();
            Assert.AreEqual(ConditionKey.Null, state.OnEnd);
        }

        [Test]
        public void Default_Source_IsNullEntity()
        {
            var state = new HitStopState();
            Assert.AreEqual(Entity.Null, state.Source);
        }

        [Test]
        public void Fields_SetCorrectly()
        {
            var entity = new Entity { Index = 42, Version = 1 };
            var state = new HitStopState
            {
                RemainingTime = 0.25f,
                CurrentIntensity = 0.5f,
                Seed = 12345u,
                OnEnd = 99,
                Source = entity
            };
            Assert.AreEqual(0.25f, state.RemainingTime);
            Assert.AreEqual(0.5f, state.CurrentIntensity);
            Assert.AreEqual(12345u, state.Seed);
            Assert.AreEqual((ConditionKey)99, state.OnEnd);
            Assert.AreEqual(entity, state.Source);
        }

        [Test]
        public void Implements_IComponentData()
        {
            Assert.IsTrue(typeof(IComponentData).IsAssignableFrom(typeof(HitStopState)));
        }

        [Test]
        public void Implements_IEnableableComponent()
        {
            Assert.IsTrue(typeof(IEnableableComponent).IsAssignableFrom(typeof(HitStopState)));
        }
    }
}