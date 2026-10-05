using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SteepingSpirits.Platformer.Hooks;

namespace SteepingSpirits.Platformer.Tests
{
    /// <summary>
    /// PlayMode tests on the built level scenes (run SteepingSpirits → JumpNRun →
    /// Build Levels first; the scenes must be in the build list, which the builder does).
    /// They talk to the level only through <see cref="JumpNRunHooks"/>.
    /// </summary>
    public class JumpNRunPlayModeTests
    {
        private sealed class ScriptedInput : IPlayerInputSource
        {
            public PlayerInputFrame Next;

            public PlayerInputFrame Read()
            {
                PlayerInputFrame f = Next;
                Next.jumpPressed = false;
                return f;
            }
        }

        private static IEnumerator Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Assert.Ignore($"Scene {scene} not built – run SteepingSpirits → JumpNRun → Build Levels.");
            }

            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            float t = 0f;
            while ((JumpNRunHooks.Probe == null || !JumpNRunHooks.Probe.HasPlayer) && t < 5f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(JumpNRunHooks.Probe, "level probe missing");
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            JumpNRunHooks.InputOverride = null;
        }

        [UnityTest]
        public IEnumerator Spawn_PlayerStandsAtTheStart()
        {
            yield return Load("JumpNRun_Level1");
            IJumpNRunProbe p = JumpNRunHooks.Probe;
            Assert.IsTrue(p.Grounded, "player should stand on the ground after spawning");
            Assert.AreEqual(0, p.Catches);
            Assert.AreEqual(-1, p.CurrentLantern);
            Assert.Less(Mathf.Abs(p.PlayerVelocity.y), 0.1f);
        }

        [UnityTest]
        public IEnumerator Lantern_LightsWhenTouched()
        {
            yield return Load("JumpNRun_Level1");
            IJumpNRunProbe p = JumpNRunHooks.Probe;
            Assert.Greater(p.LanternCount, 0);
            p.Teleport(p.LanternFeet(0) + Vector2.left * 1.5f);
            var input = new ScriptedInput { Next = { move = Vector2.right } };
            JumpNRunHooks.InputOverride = input;
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0, p.CurrentLantern);
        }

        [UnityTest]
        public IEnumerator Fall_IsCaughtAndBroughtBackGently()
        {
            yield return Load("JumpNRun_Level1");
            IJumpNRunProbe p = JumpNRunHooks.Probe;
            Vector2 start = p.PlayerFeet;
            p.Teleport(new Vector2(start.x, -6f));
            float t = 0f;
            while (p.Catches == 0 && t < 2f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(1, p.Catches, "a spirit should catch the fall");
            t = 0f;
            while (p.IsCatching && t < 3f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsFalse(p.IsCatching);
            Assert.Less(Vector2.Distance(p.PlayerFeet, start), 1.5f, "back at the start (no lantern lit yet)");
        }

        [UnityTest]
        public IEnumerator Swing_PumpAndJumpOffFlies()
        {
            yield return Load("JumpNRun_Level2");
            IJumpNRunProbe p = JumpNRunHooks.Probe;
            Assert.Greater(p.SwingCount, 0, "Level 2 has a swing");
            var input = new ScriptedInput();
            JumpNRunHooks.InputOverride = input;
            p.Teleport(p.SwingSeat(0) + Vector2.up * 0.3f);

            float t = 0f;
            while (!p.Riding && t < 2f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(p.Riding, "landing on the seat should sit down");

            // Pump in rhythm for a while.
            t = 0f;
            while (t < 5f)
            {
                var o = p.Observe();
                input.Next.move = new Vector2(Mathf.Abs(o.swingAngularVelocity) < 0.15f ? 1f : Mathf.Sign(o.swingAngularVelocity), 0f);
                t += Time.deltaTime;
                yield return null;
            }

            Assert.Greater(p.Observe().swingAmplitude, 0.5f, "pumping in rhythm builds up the swing");
            input.Next = new PlayerInputFrame { jumpPressed = true, jumpHeld = true };
            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(p.Riding, "jump lets go");
            Assert.Greater(p.PlayerVelocity.magnitude, 2f, "and the player flies off with the swing's speed");
        }
    }
}
