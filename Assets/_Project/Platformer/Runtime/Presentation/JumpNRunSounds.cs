using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    public enum JumpNRunSound
    {
        Jump,
        Land,
        Collect,
        CollectRare,
        Lantern,
        Catch,
        Bounce,
        Wind,
        Goal,
        Creak
    }

    /// <summary>
    /// Quiet procedural placeholder sounds (see <see cref="SoftSynth"/>). Clips are
    /// generated once in Awake; playing uses a small pool of AudioSources with
    /// slight pitch variation. Replace a clip by assigning one in the Inspector.
    /// </summary>
    public class JumpNRunSounds : MonoBehaviour
    {
        private const int Rate = 22050;

        [SerializeField] private AudioClip[] overrides = new AudioClip[10];
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float pitchVariation = 0.04f;

        private AudioClip[] clips;
        private AudioSource[] sources;
        private int next;

        public static JumpNRunSounds Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            sources = new AudioSource[4];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0f;
            }

            int count = System.Enum.GetValues(typeof(JumpNRunSound)).Length;
            clips = new AudioClip[count];
            for (int i = 0; i < count; i++)
            {
                clips[i] = overrides != null && i < overrides.Length && overrides[i] != null
                    ? overrides[i]
                    : Generate((JumpNRunSound)i);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void Play(JumpNRunSound sound, float volume)
        {
            if (Instance != null)
            {
                Instance.PlayInternal(sound, volume);
            }
        }

        /// <summary>Plays with a chosen pitch (e.g. the swing's creak rises with its height).</summary>
        public static void PlayPitched(JumpNRunSound sound, float volume, float pitch)
        {
            if (Instance != null)
            {
                Instance.PlayInternal(sound, volume, pitch);
            }
        }

        private void PlayInternal(JumpNRunSound sound, float volume, float pitch = -1f)
        {
            AudioSource s = sources[next];
            next = (next + 1) % sources.Length;
            s.pitch = pitch > 0f ? pitch : 1f + Random.Range(-pitchVariation, pitchVariation);
            s.PlayOneShot(clips[(int)sound], Mathf.Clamp01(volume * masterVolume));
        }

        private static AudioClip Generate(JumpNRunSound sound)
        {
            // Buffers are full scale (volume 1); loudness comes from FeedbackTuning.
            float[] b;
            switch (sound)
            {
                case JumpNRunSound.Jump:
                    b = new float[SoftSynth.Samples(Rate, 0.18f)];
                    SoftSynth.Pluck(b, Rate, 392f, 0.8f, 0.06f, 0.25f);
                    break;
                case JumpNRunSound.Land:
                    b = new float[SoftSynth.Samples(Rate, 0.16f)];
                    SoftSynth.Puff(b, Rate, 0.9f, 700f, 0.045f, 11);
                    break;
                case JumpNRunSound.Collect:
                    b = new float[SoftSynth.Samples(Rate, 0.45f)];
                    SoftSynth.Chime(b, Rate, new[] { 659f, 988f }, 0.9f, 0.06f, 0.14f);
                    break;
                case JumpNRunSound.CollectRare:
                    b = new float[SoftSynth.Samples(Rate, 0.9f)];
                    SoftSynth.Chime(b, Rate, new[] { 523f, 659f, 784f, 1047f }, 0.9f, 0.09f, 0.3f);
                    break;
                case JumpNRunSound.Lantern:
                    b = new float[SoftSynth.Samples(Rate, 0.8f)];
                    SoftSynth.Chime(b, Rate, new[] { 330f, 494f, 659f }, 0.9f, 0.05f, 0.3f);
                    break;
                case JumpNRunSound.Catch:
                    b = new float[SoftSynth.Samples(Rate, 0.7f)];
                    SoftSynth.Puff(b, Rate, 0.5f, 400f, 0.3f, 5);
                    SoftSynth.Pluck(b, Rate, 523f, 0.4f, 0.25f, -0.2f);
                    SoftSynth.Normalize(b, 0.9f);
                    break;
                case JumpNRunSound.Bounce:
                    b = new float[SoftSynth.Samples(Rate, 0.25f)];
                    SoftSynth.Pluck(b, Rate, 294f, 0.8f, 0.08f, 0.6f);
                    break;
                case JumpNRunSound.Wind:
                    b = new float[SoftSynth.Samples(Rate, 0.6f)];
                    SoftSynth.Puff(b, Rate, 0.9f, 500f, 0.25f, 23);
                    break;
                case JumpNRunSound.Creak:
                    // Soft wooden creak: a low, slightly falling tone inside a short low-passed rub.
                    b = new float[SoftSynth.Samples(Rate, 0.32f)];
                    SoftSynth.Puff(b, Rate, 0.35f, 900f, 0.12f, 41);
                    SoftSynth.Pluck(b, Rate, 170f, 0.55f, 0.14f, -0.12f);
                    SoftSynth.Normalize(b, 0.9f);
                    break;
                default:
                    b = new float[SoftSynth.Samples(Rate, 1.2f)];
                    SoftSynth.Chime(b, Rate, new[] { 392f, 494f, 587f, 784f }, 0.9f, 0.12f, 0.4f);
                    break;
            }

            var clip = AudioClip.Create("jnr_" + sound, b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }
    }
}
