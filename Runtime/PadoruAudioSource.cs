using Padoru.Core;
using System;
using Padoru.Core.Utils;
using UnityEngine;

using Debug = Padoru.Diagnostics.Debug;

namespace Padoru.Audio
{
    public class PadoruAudioSource : MonoBehaviour
    {
        [SerializeField] private string fileId;
        [SerializeField] private bool trackObject;

        private IAudioManager audioManager;
        private AudioFile audioFile;
        private AudioSource currentAudioSource;

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

            AudioSource audioSource = null;
            
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

            SetupAudioSource(audioSource);

            currentAudioSource = audioSource;
            currentAudioSource.Play();

            if (!audioFile.Loop)
            {
                var countdown = new Countdown(audioFile.Clip.length);
                countdown.OnCountdownEnded += () =>
                {
                    FinishAudio(audioSource);
                };
                countdown.Start();
            }
        }

        public void Stop()
        {
            if(currentAudioSource != null)
            {
                currentAudioSource.Stop();
            }

            FinishAudio(currentAudioSource);

            currentAudioSource = null;
        }

        private void FinishAudio(AudioSource audioSource)
        {
            try
            {
                audioManager.ReturnAudioSource(audioSource);
            }
            catch (Exception e)
            {
                Debug.LogException("Failed to return audio source", e);
            }

            OnAudioFinish?.Invoke(this);
        }

        private void SetupAudioSource(AudioSource audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.outputAudioMixerGroup = audioFile.Mixer;
            audioSource.clip = audioFile.Clip;
            audioSource.volume = audioFile.Volume;
            audioSource.loop = audioFile.Loop;
            audioSource.pitch = audioFile.ShiftPitch ? audioFile.MinMaxPitch.GetRandomValue() : 1f;

            if (trackObject)
            {
                audioSource.transform.parent = transform;
            }
        }
    }
}
