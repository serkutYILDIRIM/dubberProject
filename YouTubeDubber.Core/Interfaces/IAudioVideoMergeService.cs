using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Interfaces
{
    /// <summary>
    /// Interface for audio-video merging services
    /// </summary>
    public interface IAudioVideoMergeService
    {
        /// <summary>
        /// Merges an audio file with a video file to create a new video
        /// </summary>
        /// <param name="videoFilePath">Path to the video file</param>
        /// <param name="audioFilePath">Path to the audio file to merge with the video</param>
        /// <param name="outputFilePath">Path to save the merged video</param>
        /// <param name="keepOriginalAudio">Whether to keep and mix the original audio (true) or replace it (false)</param>
        /// <param name="originalAudioVolume">Volume level for the original audio if kept (0.0 to 1.0)</param>
        /// <param name="newAudioVolume">Volume level for the new audio (0.0 to 1.0)</param>
        /// <param name="progressCallback">Optional callback to report progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the merged video file</returns>
        Task<string> MergeAudioVideoAsync(
            string videoFilePath,
            string audioFilePath,
            string outputFilePath,
            bool keepOriginalAudio = false,
            float originalAudioVolume = 0.1f,
            float newAudioVolume = 1.0f,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Creates a dubbed video with enhanced audio mixing for Turkish dubbing
        /// </summary>
        /// <param name="videoFilePath">Path to the original video file</param>
        /// <param name="dubbingAudioPath">Path to the Turkish dubbing audio file</param>
        /// <param name="outputFilePath">Path to save the dubbed video</param>
        /// <param name="preserveBackgroundSounds">Whether to extract and preserve background sounds</param>
        /// <param name="addSubtitles">Whether to add Turkish subtitles to the video</param>
        /// <param name="subtitlesFilePath">Path to the subtitles file (if adding subtitles)</param>
        /// <param name="mixingProfile">Audio mixing profile to use (background, music, voice, balanced)</param>
        /// <param name="progressCallback">Optional callback to report progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the dubbed video file</returns>
        Task<string> CreateTurkishDubbedVideoAsync(
            string videoFilePath,
            string dubbingAudioPath,
            string outputFilePath,
            bool preserveBackgroundSounds = true,
            bool addSubtitles = false,
            string? subtitlesFilePath = null,
            string mixingProfile = "balanced",
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Creates a Turkish dubbed video with enhanced options
        /// </summary>
        /// <param name="videoFilePath">Path to the original video file</param>
        /// <param name="dubbingAudioPath">Path to the Turkish dubbing audio file</param>
        /// <param name="outputFilePath">Path to save the dubbed video</param>
        /// <param name="options">Turkish dubbing options</param>
        /// <param name="progressCallback">Optional callback to report progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the dubbed video file</returns>
        Task<string> CreateTurkishDubbedVideoAsync(
            string videoFilePath,
            string dubbingAudioPath,
            string outputFilePath,
            TurkishDubbingOptions options,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}
