using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    }
}
