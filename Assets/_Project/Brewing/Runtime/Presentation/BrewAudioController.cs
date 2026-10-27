using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Data;
using SteepingSpirits.Core.Audio;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Brewing sound: a loop per boil stage (crossfaded), or – where no clip is
    /// assigned – the filtered placeholder noise that follows the water
    /// continuously. Pouring rises in pitch with the fill level. Lift and bell
    /// clips are optional; the bell falls back to a soft chime.
    /// </summary>
    public class BrewAudioController : BrewView
    {
        [SerializeField] private BrewAudioSet audioSet;

        private AudioSource loopA;
        private AudioSource loopB;
        private AudioSource pourSource;
        private AudioSource oneShots;
        private FilteredNoiseSource kettleNoise;
        private FilteredNoiseSource pourNoise;
        private AudioClip chime;
        private bool aIsCurrent = true;
        private int currentStage = -1;
        private BrewSession subscribed;

        public void Configure(BrewAudioSet audioSet)
        {
            this.audioSet = audioSet;
        }

        private void Awake()
        {
            loopA = NewSource("Loop A", true);
            loopB = NewSource("Loop B", true);
            pourSource = NewSource("Pour", true);
            oneShots = NewSource("OneShots", false);
            kettleNoise = NewNoise("Kettle Noise");
            pourNoise = NewNoise("Pour Noise");
            chime = ProceduralSfx.Melody("brew_chime", new[] { 1046.5f, 1318.5f, 1568f }, 1.2f, 0.25f);

            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        private AudioSource NewSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        private FilteredNoiseSource NewNoise(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<AudioSource>();
            return go.AddComponent<FilteredNoiseSource>();
        }

        private void Update()
        {
            BrewSession s = Session;
            if (s == null || Look == null)
            {
                return;
            }

            if (subscribed != s)
            {
                if (subscribed != null) subscribed.Finished -= HandleFinished;
                s.Finished += HandleFinished;
                subscribed = s;
            }

            UpdateKettle(s);
            UpdatePour(s);
        }

        private void OnDestroy()
        {
            if (subscribed != null) subscribed.Finished -= HandleFinished;
        }

        private void UpdateKettle(BrewSession s)
        {
            float master = Look.masterVolume;
            BoilLookSample look = BoilLook.Evaluate(Look, Sim.boilStages, Sim.water.roomTemperature,
                Sim.water.boilingPoint, s.Water.Temperature);
            int stage = (int)s.Water.Stage;
            AudioClip clip = audioSet != null ? audioSet.StageLoop(stage) : null;

            if (stage != currentStage)
            {
                currentStage = stage;
                if (clip != null)
                {
                    aIsCurrent = !aIsCurrent;
                    AudioSource next = aIsCurrent ? loopA : loopB;
                    next.clip = clip;
                    next.volume = 0f;
                    next.Play();
                }
            }

            // Crossfade: current loop up, the other one down.
            float step = Time.deltaTime / Mathf.Max(0.01f, Look.audioCrossfadeSeconds);
            AudioSource current = aIsCurrent ? loopA : loopB;
            AudioSource other = aIsCurrent ? loopB : loopA;
            float clipTarget = clip != null && current.clip == clip ? master : 0f;
            current.volume = Mathf.MoveTowards(current.volume, clipTarget, step * master);
            other.volume = Mathf.MoveTowards(other.volume, 0f, step * master);

            // Placeholder noise covers every stage without a clip.
            kettleNoise.SetGlide(Look.audioCrossfadeSeconds);
            kettleNoise.SetTarget(clip == null ? look.noiseVolume * master : 0f, look.noiseCutoffHz, look.crackleRate);
            kettleNoise.SetTexture(look.highpassHz, look.swellRate, look.swellDepth);
        }

        private void UpdatePour(BrewSession s)
        {
            bool pouring = s.Phase == BrewPhase.Pour;
            float pitch = Mathf.Lerp(Look.pourPitchRange.x, Look.pourPitchRange.y, s.PhaseProgress);
            AudioClip clip = audioSet != null ? audioSet.pourLoop : null;

            if (clip != null)
            {
                if (pouring && !pourSource.isPlaying)
                {
                    pourSource.clip = clip;
                    pourSource.Play();
                }

                pourSource.pitch = pitch;
                pourSource.volume = Mathf.MoveTowards(pourSource.volume, pouring ? Look.masterVolume : 0f, Time.deltaTime * 4f);
                pourNoise.SetTarget(0f, 1000f, 0f);
            }
            else
            {
                // Pitch hook for the placeholder: brighter as the vessel fills.
                pourNoise.SetGlide(0.15f);
                pourNoise.SetTarget(pouring ? 0.18f * Look.masterVolume : 0f, 1400f * pitch, 0f);
            }
        }

        private void HandleFinished(BrewResult result)
        {
            if (audioSet != null && audioSet.liftClip != null)
            {
                oneShots.PlayOneShot(audioSet.liftClip, Look.masterVolume);
            }

            if (result.Tier == QualityTier.Perfect)
            {
                AudioClip bell = audioSet != null && audioSet.bellClip != null ? audioSet.bellClip : chime;
                oneShots.PlayOneShot(bell, Look.masterVolume);
            }
        }
    }
}
