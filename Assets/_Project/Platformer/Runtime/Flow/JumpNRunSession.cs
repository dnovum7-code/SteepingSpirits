using System;
using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Runs one visit of a level: lantern checkpoints, the gentle catch when
    /// the player falls or touches brambles, and the level end. There is no
    /// death, no lives and no timer.
    /// </summary>
    public class JumpNRunSession : MonoBehaviour
    {
        [Tooltip("Falling this far below the level bottom triggers the catch")]
        [SerializeField] private float fallMargin = 3f;
        [SerializeField] private Color catchTint = new Color(0.07f, 0.09f, 0.16f, 1f);
        [SerializeField] private Color spiritColor = new Color(0.85f, 0.92f, 1f, 0.7f);

        private CheckpointTracker checkpoints;
        private readonly IngredientBag bag = new IngredientBag();
        private LanternChallenge challenge = new LanternChallenge(0);
        private CatchSequence catchSequence;
        private JumpNRunPlayer player;
        private Texture2D tintTexture;

        public CheckpointTracker Checkpoints => checkpoints;
        public IngredientBag Bag => bag;
        public LanternChallenge Challenge => challenge;
        public bool IsCatching => catchSequence != null && catchSequence.Active;
        public bool Finished { get; private set; }
        public int Catches { get; private set; }

        /// <summary>Where the bag went at the goal (inventory or pantry).</summary>
        public HandoverTarget Handover { get; private set; }

        /// <summary>Assist "fall protection": respawn at the last safe ground instead of the lantern.</summary>
        public bool FallProtection { get; set; }

        public event Action<int> LanternLit;
        public event Action Caught;
        public event Action Respawned;
        public event Action Finish;
        public event Action ChallengeComplete;

        public static JumpNRunSession Current { get; private set; }

        private void Awake()
        {
            Current = this;
            JumpNRunLevel level = GetComponent<JumpNRunLevel>();
            FeedbackParams fp = level != null && level.feedbackTuning != null ? level.feedbackTuning.feedback : new FeedbackParams();
            catchSequence = new CatchSequence(fp);
            challenge = new LanternChallenge(level != null ? level.pathLanternCount : 0);
            tintTexture = Texture2D.whiteTexture;
        }

        private void Start()
        {
            player = JumpNRunPlayer.Instance;
            Vector2 feet = player != null ? player.Feet : (Vector2)transform.position;
            checkpoints = new CheckpointTracker(new Vec2(feet.x, feet.y));
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public void LightLantern(int index, Vector2 respawnFeet)
        {
            if (checkpoints == null || !checkpoints.Light(index, new Vec2(respawnFeet.x, respawnFeet.y)))
            {
                return;
            }

            LanternLit?.Invoke(index);
        }

        /// <summary>A spirit catches the player and brings them back. Safe to call repeatedly.</summary>
        public void Catch()
        {
            // While a swing holds the player it owns them; a catch would fight over control.
            if (Finished || player == null || player.IsControlled || !catchSequence.Begin())
            {
                return;
            }

            Catches++;
            player.TakeControl(this);
            FeedbackParams p = catchSequence.Params;
            JumpNRunParticles.Burst(player.Feet + Vector2.up * 0.6f, 10, Vector2.up, 60f, 1.2f, spiritColor, 0.3f, 0.9f, -0.6f);
            JumpNRunSounds.Play(JumpNRunSound.Catch, p.catchVolume);
            Caught?.Invoke();
        }

        /// <summary>A path lantern was lit; completing all gives the bonus ingredient.</summary>
        public void LightPathLantern(int index)
        {
            if (Finished || !challenge.Light(index))
            {
                return;
            }

            bag.Add(challenge.RewardId);
            ChallengeComplete?.Invoke();
        }

        public void Collect(string ingredientId)
        {
            bag.Add(ingredientId);
        }

        public void FinishLevel()
        {
            if (Finished)
            {
                return;
            }

            Finished = true;
            Handover = IngredientHandover.Deliver(bag);
            JumpNRunLevel lvl = JumpNRunLevel.Current;
            if (lvl != null)
            {
                JumpNRunSaveStore.Current.Complete(lvl.levelId, lvl.nextLevelId);
                if (challenge.Complete) JumpNRunSaveStore.Current.CompleteChallenge(lvl.levelId);
                JumpNRunSaveStore.Save();
            }

            if (player != null)
            {
                player.TakeControl(this);
            }

            JumpNRunSounds.Play(JumpNRunSound.Goal, catchSequence.Params.lanternVolume);
            Finish?.Invoke();
        }

        private void FixedUpdate()
        {
            if (player == null || checkpoints == null || IsCatching)
            {
                return;
            }

            Vector2 feet = player.Feet;
            checkpoints.TrackGround(Time.fixedDeltaTime, player.IsGrounded, IsSafeGround(player.Ground), new Vec2(feet.x, feet.y));

            JumpNRunLevel level = JumpNRunLevel.Current;
            float bottom = level != null ? level.bounds.yMin : -20f;
            if (feet.y < bottom - fallMargin)
            {
                Catch();
            }
        }

        private void Update()
        {
            if (!IsCatching)
            {
                return;
            }

            if (catchSequence.Tick(Time.unscaledDeltaTime))
            {
                Vec2 p = checkpoints.RespawnPoint(FallProtection);
                player.Teleport(new Vector2(p.x, p.y));
                checkpoints.OnRespawned(p);
                LanternSpirit.GatherAll(player.Feet);
                var cam = FindAnyObjectByType<JumpNRunCamera>();
                if (cam != null) cam.Snap();
            }

            if (catchSequence.Phase == CatchPhase.FadeIn && player.IsControlled)
            {
                player.TakeControl(null);
                JumpNRunParticles.Burst(player.Feet + Vector2.up * 0.6f, 8, Vector2.up, 80f, 0.8f, spiritColor, 0.25f, 0.8f, -0.4f);
                Respawned?.Invoke();
            }
        }

        private static bool IsSafeGround(Collider2D ground)
        {
            if (ground == null || ground.GetComponentInParent<UnsafeGround>() != null)
            {
                return false;
            }

            Rigidbody2D rb = ground.attachedRigidbody;
            return rb == null || rb.bodyType == RigidbodyType2D.Static;
        }

        private void OnGUI()
        {
            if (catchSequence == null || catchSequence.Darkness <= 0.001f)
            {
                return;
            }

            Color old = GUI.color;
            GUI.color = new Color(catchTint.r, catchTint.g, catchTint.b, catchSequence.Darkness * 0.92f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), tintTexture);
            GUI.color = old;
        }
    }
}
