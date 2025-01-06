using Padoru.Core;
using System;
using UnityEngine;

using Debug = Padoru.Diagnostics.Debug;

namespace Padoru.Audio
{
    public class PadoruAudioSource : MonoBehaviour, ITickable
    {
        [SerializeField] private string fileId;
        [SerializeField] private bool trackObject;

        private IAudioManager audioManager;
        private ITickManager tickManager;
        private AudioFile audioFile;
        private AudioSource audioSource;
        private bool isPlaying;

        private float playTime;
        private float audioDuration;
        private bool initialized;

        public event Action<PadoruAudioSource> OnAudioFinish;

        private bool CanPlay
        {
            get
            {
                if (audioFile == null)
                {
                    Debug.LogError("Null audio file");
                    return false;
                }

                if (audioFile.Clip == null)
                {
                    Debug.LogWarning("Null audio clip", gameObject);
                    return false;
                }

                if (audioFile.Disabled)
                {
                    Debug.LogWarning("You are trying to play a disabled audio", gameObject);
                    return false;
                }

                return true;
            }
        }

        private void Awake()
        {
            tickManager = Locator.Get<ITickManager>();
            tickManager.Register(this);
        }

        private void Start()
        {
            if (!initialized)
            {
                Init();
            }

            if (audioFile is { PlayOnAwake: true })
            {
                Play();
            }
        }

        private void OnEnable()
        {
            if (audioFile != null && audioFile.PlayOnAwake)
            {
                Play();
            }
        }

        /// <summary>
        /// If you want this change to take effect you need to initialize again
        /// </summary>
        public void SetAudioFileId(string fileId)
        {
            this.fileId = fileId;
        }

        public void Tick(float deltaTime)
        {
            if(audioFile != null && isPlaying && !audioFile.Loop)
            {
                if(Time.time - playTime >= audioDuration)
                {
                    FinishAudio();
                }
            }
        }

        public void Init()
        {
            audioManager = Locator.Get<IAudioManager>();

            if (audioManager == null)
            {
                Debug.LogError("Could not initialize audio source due to null audio manager");
                return;
            }

            try
            {
                audioFile = audioManager.GetAudioFile(fileId);

                initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogException("Failed to initialize Audio File", e, gameObject);
            }
        }

        public void Play()
        {
            if(!initialized)
            {
                Init();
            }

            if (!CanPlay)
            {
                return;
            }

            isPlaying = true;
            
            try
            {
                audioSource = audioManager.GetAudioSource();
            }
            catch (Exception e)
            {
                Debug.LogException($"Failed to initialize audio source", e);
            }

            if(audioSource == null)
            {
                Debug.LogError($"Audio manager failed to return an audio source");
                return;
            }

            SetupAudioSource();

            playTime = Time.time;
            audioDuration = audioFile.Clip.length;

            var pitch = 1f;

            if (audioFile.ShiftPitch)
            {
                pitch = audioFile.MinMaxPitch.GetRandomValue();
            }

            audioSource.pitch = pitch;
            audioSource.Play();
        }

        public void Stop()
        {
            if(audioSource != null)
            {
                audioSource.Stop();
            }

            FinishAudio();
        }

        private void FinishAudio()
        {
            if (audioSource != null)
            {
                try
                {
                    audioManager.ReturnAudioSource(audioSource);
                }
                catch (Exception e)
                {
                    Debug.LogException($"Failed to return audio source", e);
                }

                audioSource = null;
            }

            isPlaying = false;

            OnAudioFinish?.Invoke(this);
            
            tickManager.Unregister(this);
        }

        private void SetupAudioSource()
        {
            audioSource.playOnAwake = false;
            audioSource.outputAudioMixerGroup = audioFile.Mixer;
            audioSource.clip = audioFile.Clip;
            audioSource.volume = audioFile.Volume;
            audioSource.loop = audioFile.Loop;

            if (trackObject)
            {
                audioSource.transform.parent = transform;
            }
        }
    }
}
