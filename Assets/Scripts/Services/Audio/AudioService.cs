using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using ChatSystem.Services.Audio.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Models.Audio;

namespace ChatSystem.Services.Audio
{
    public class AudioService : MonoBehaviour, IAudioService
    {
        private AudioSource audioSource;
        private AudioClip microphoneClip;
        private string currentMicrophone;
        private VoiceSettings currentSettings;
        private bool isRecording;
        private bool isPlaying;
        private Coroutine recordingCoroutine;

        [SerializeField] private int recordingFrequency = 44100;
        [SerializeField] private int recordingLength = 300;

        public event Action<AudioData> OnAudioCaptured;
        public event Action OnRecordingStarted;
        public event Action OnRecordingStopped;
        public event Action<string> OnAudioError;

        public bool IsRecording => isRecording;
        public bool IsPlaying => isPlaying && audioSource != null && audioSource.isPlaying;
        public float CurrentVolume => audioSource != null ? audioSource.volume : 0f;
        public string[] AvailableMicrophones => Microphone.devices;

        private void Awake()
        {
            InitializeAudioSource();
            InitializeDefaultSettings();
        }

        private void InitializeAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            LoggingService.LogInfo("AudioService initialized with AudioSource");
        }

        private void InitializeDefaultSettings()
        {
            currentSettings = new VoiceSettings();
            if (AvailableMicrophones.Length > 0)
            {
                currentMicrophone = AvailableMicrophones[0];
                LoggingService.LogInfo($"Default microphone set to: {currentMicrophone}");
            }
            else
            {
                LoggingService.LogWarning("No microphones available");
            }
        }

        public async Task<bool> StartRecordingAsync()
        {
            if (isRecording)
            {
                LoggingService.LogWarning("Already recording");
                return false;
            }

            if (string.IsNullOrEmpty(currentMicrophone))
            {
                string errorMsg = "No microphone available for recording";
                LoggingService.LogError(errorMsg);
                OnAudioError?.Invoke(errorMsg);
                return false;
            }

            try
            {
                int frequency = currentSettings?.sampleRate ?? recordingFrequency;
                microphoneClip = Microphone.Start(currentMicrophone, true, recordingLength, frequency);
                
                if (microphoneClip == null)
                {
                    string errorMsg = "Failed to start microphone recording";
                    LoggingService.LogError(errorMsg);
                    OnAudioError?.Invoke(errorMsg);
                    return false;
                }

                isRecording = true;
                recordingCoroutine = StartCoroutine(ProcessAudioData());
                
                OnRecordingStarted?.Invoke();
                LoggingService.LogInfo($"Started recording with microphone: {currentMicrophone}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to start recording: {ex.Message}");
                OnAudioError?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task StopRecordingAsync()
        {
            if (!isRecording)
            {
                LoggingService.LogWarning("Not currently recording");
                return;
            }

            try
            {
                isRecording = false;
                
                if (recordingCoroutine != null)
                {
                    StopCoroutine(recordingCoroutine);
                    recordingCoroutine = null;
                }

                if (!string.IsNullOrEmpty(currentMicrophone))
                {
                    Microphone.End(currentMicrophone);
                }

                OnRecordingStopped?.Invoke();
                LoggingService.LogInfo("Recording stopped");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error stopping recording: {ex.Message}");
                OnAudioError?.Invoke(ex.Message);
            }
        }

        public async Task PlayAudioAsync(AudioData audioData)
        {
            if (audioSource == null)
            {
                LoggingService.LogError("AudioSource not available for playback");
                return;
            }

            try
            {
                AudioClip clip = AudioClip.Create("VoiceResponse", 
                    audioData.samples.Length, 
                    audioData.channels, 
                    audioData.sampleRate, 
                    false);
                    
                clip.SetData(audioData.samples, 0);
                
                audioSource.clip = clip;
                audioSource.volume = currentSettings?.outputVolume ?? 1.0f;
                audioSource.pitch = currentSettings?.playbackSpeed ?? 1.0f;
                
                isPlaying = true;
                audioSource.Play();
                
                LoggingService.LogInfo($"Playing audio: {audioData.duration:F2}s");
                
                StartCoroutine(WaitForPlaybackComplete(clip));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to play audio: {ex.Message}");
                OnAudioError?.Invoke(ex.Message);
            }
        }

        public async Task PlayAudioChunkAsync(byte[] audioChunk)
        {
            try
            {
                AudioData audioData = ConvertBytesToAudioData(audioChunk);
                await PlayAudioAsync(audioData);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to play audio chunk: {ex.Message}");
                OnAudioError?.Invoke(ex.Message);
            }
        }

        public void StopPlayback()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                isPlaying = false;
                LoggingService.LogInfo("Playback stopped");
            }
        }

        public AudioData GetCurrentAudioData()
        {
            if (microphoneClip == null || !isRecording)
            {
                return new AudioData();
            }

            try
            {
                int micPosition = Microphone.GetPosition(currentMicrophone);
                if (micPosition <= 0)
                {
                    return new AudioData();
                }

                float[] samples = new float[micPosition * microphoneClip.channels];
                microphoneClip.GetData(samples, 0);

                return new AudioData
                {
                    samples = samples,
                    channels = microphoneClip.channels,
                    sampleRate = microphoneClip.frequency,
                    duration = (float)samples.Length / microphoneClip.frequency / microphoneClip.channels,
                    rawData = ConvertSamplesToBytes(samples)
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get audio data: {ex.Message}");
                OnAudioError?.Invoke(ex.Message);
                return new AudioData();
            }
        }

        public void SetVoiceSettings(VoiceSettings settings)
        {
            currentSettings = settings;
            
            if (audioSource != null)
            {
                audioSource.volume = settings.outputVolume;
            }
            
            LoggingService.LogInfo("Voice settings updated");
        }

        private IEnumerator ProcessAudioData()
        {
            int lastMicPosition = 0;
            
            while (isRecording && microphoneClip != null)
            {
                int currentMicPosition = Microphone.GetPosition(currentMicrophone);
                
                if (currentMicPosition != lastMicPosition && currentMicPosition > 0)
                {
                    AudioData audioData = GetCurrentAudioData();
                    if (audioData.samples != null && audioData.samples.Length > 0)
                    {
                        OnAudioCaptured?.Invoke(audioData);
                    }
                    lastMicPosition = currentMicPosition;
                }
                
                yield return new WaitForSeconds(currentSettings?.audioChunkSizeMs / 1000f ?? 0.1f);
            }
        }

        private IEnumerator WaitForPlaybackComplete(AudioClip clip)
        {
            yield return new WaitForSeconds(clip.length);
            isPlaying = false;
            
            if (clip != null)
            {
                DestroyImmediate(clip);
            }
        }

        private AudioData ConvertBytesToAudioData(byte[] audioBytes)
        {
            int sampleCount = audioBytes.Length / 2;
            float[] samples = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)((audioBytes[i * 2 + 1] << 8) | audioBytes[i * 2]);
                samples[i] = sample / 32768f;
            }
            
            int sampleRate = currentSettings?.sampleRate ?? 24000;
            int channels = currentSettings?.channels ?? 1;
            
            return new AudioData
            {
                samples = samples,
                channels = channels,
                sampleRate = sampleRate,
                duration = (float)samples.Length / sampleRate / channels,
                rawData = audioBytes
            };
        }

        private byte[] ConvertSamplesToBytes(float[] samples)
        {
            byte[] bytes = new byte[samples.Length * 2];
            
            for (int i = 0; i < samples.Length; i++)
            {
                short sample = (short)(samples[i] * 32767f);
                bytes[i * 2] = (byte)(sample & 0xFF);
                bytes[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
            
            return bytes;
        }

        private void OnDestroy()
        {
            if (isRecording)
            {
                StopRecordingAsync();
            }
            
            StopPlayback();
            
            if (microphoneClip != null)
            {
                DestroyImmediate(microphoneClip);
            }
        }
    }
}