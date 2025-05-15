using System;
using System.Threading;
using System.Threading.Tasks;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Interfaces
{
    /// <summary>
    /// Interface for services that interact with YouTube videos
    /// </summary>
    public interface IYouTubeService
    {
        /// <summary>
        /// Validates if a string is a valid YouTube URL
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <returns>True if the URL is a valid YouTube URL, otherwise false</returns>
        bool IsValidYouTubeUrl(string url);
        
        /// <summary>
        /// Extracts the video ID from a YouTube URL
        /// </summary>
        /// <param name="url">The YouTube URL</param>
        /// <returns>The video ID if found, otherwise null</returns>
        string ExtractVideoId(string url);
        
        /// <summary>
        /// Gets information about a YouTube video asynchronously
        /// </summary>
        /// <param name="videoUrl">The URL of the YouTube video</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Information about the video</returns>
        Task<VideoInfo> GetVideoInfoAsync(string videoUrl, CancellationToken cancellationToken = default);
          /// <summary>
        /// Downloads a YouTube video to a local file asynchronously
        /// </summary>
        /// <param name="videoUrl">The URL of the YouTube video</param>
        /// <param name="outputFilePath">The path where the video should be saved. If null, a default path will be used.</param>
        /// <param name="quality">The desired video quality. Default is highest.</param>
        /// <param name="progressCallback">Optional callback to report download progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The path to the downloaded file</returns>
        Task<string> DownloadVideoAsync(
            string videoUrl, 
            string? outputFilePath = null, 
            string quality = "highest",
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Downloads only the audio stream from a YouTube video
        /// </summary>
        /// <param name="videoUrl">The URL of the YouTube video</param>
        /// <param name="outputFilePath">The path where the audio should be saved. If null, a default path will be used.</param>
        /// <param name="progressCallback">Optional callback to report download progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The path to the downloaded audio file</returns>        Task<string> DownloadAudioOnlyAsync(
            string videoUrl,
            string? outputFilePath = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Extracts audio from a downloaded video with configurable options
        /// </summary>
        /// <param name="videoFilePath">Path to the downloaded video file</param>
        /// <param name="outputFilePath">The path where the audio should be saved. If null, a default path will be used.</param>
        /// <param name="options">Audio extraction options to configure quality, format, etc.</param>
        /// <param name="progressCallback">Optional callback to report extraction progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The path to the extracted audio file</returns>
        Task<string> ExtractAudioFromVideoAsync(
            string videoFilePath,
            string? outputFilePath = null,
            AudioExtractionOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Downloads a YouTube video and extracts its audio in one operation
        /// </summary>
        /// <param name="videoUrl">The URL of the YouTube video</param>
        /// <param name="outputFilePath">The path where the audio should be saved. If null, a default path will be used.</param>
        /// <param name="options">Audio extraction options to configure quality, format, etc.</param>
        /// <param name="deleteVideoAfterExtraction">Whether to delete the downloaded video after audio extraction</param>
        /// <param name="progressCallback">Optional callback to report download and extraction progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The path to the extracted audio file</returns>
        Task<string> DownloadAndExtractAudioAsync(
            string videoUrl,
            string? outputFilePath = null,
            AudioExtractionOptions? options = null,
            bool deleteVideoAfterExtraction = true,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Downloads a YouTube video and extracts audio optimized for speech recognition
        /// </summary>
        /// <param name="videoUrl">The URL of the YouTube video</param>
        /// <param name="outputFilePath">The path where the audio should be saved. If null, a default path will be used.</param>
        /// <param name="deleteVideoAfterExtraction">Whether to delete the downloaded video after audio extraction</param>
        /// <param name="progressCallback">Optional callback to report download and extraction progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The path to the extracted speech-optimized audio file</returns>
        Task<string> DownloadAndExtractSpeechAudioAsync(
            string videoUrl,
            string? outputFilePath = null,
            bool deleteVideoAfterExtraction = true,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}