using System;
using System.Threading.Tasks;
using ChatSystem.Models.Audio;

namespace ChatSystem.Services.Audio.Interfaces
{
    public interface IAudioService
    {
        event Action<AudioData> OnAudioCaptured;
        event Action OnRecordingStarted;
        event Action OnRecordingStopped;
        event Action<string> OnAudioError;
        
        Task<bool> StartRecordingAsync();
        Task StopRecordingAsync();
        Task PlayAudioAsync(AudioData audioData);
        Task PlayAudioChunkAsync(byte[] audioChunk);
        void StopPlayback();
        
        AudioData GetCurrentAudioData();
        void SetVoiceSettings(VoiceSettings settings);
        
        bool IsRecording { get; }
        bool IsPlaying { get; }
        float CurrentVolume { get; }
        string[] AvailableMicrophones { get; }
    }
}