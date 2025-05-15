using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using YouTubeDubber.Core.Interfaces;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Service for merging audio and video files using FFmpeg
    /// </summary>
    public class FFmpegAudioVideoMergeService : IAudioVideoMergeService
    {
        private bool _ffmpegInitialized = false;
        
        /// <summary>
        /// Initializes a new instance of the FFmpeg Audio-Video Merge Service
        /// </summary>
        public FFmpegAudioVideoMergeService()
        {
        }
        
        /// <summary>
        /// Ensures FFmpeg is downloaded and available
        /// </summary>
        public async Task InitializeFFmpegAsync()
        {
            if (!_ffmpegInitialized)
            {
                // Download FFmpeg if it's not already installed
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
                _ffmpegInitialized = true;
            }
        }
        
        /// <summary>
        /// Merges an audio file with a video file to create a new video
        /// </summary>
        public async Task<string> MergeAudioVideoAsync(
            string videoFilePath,
            string audioFilePath,
            string outputFilePath,
            bool keepOriginalAudio = false,
            float originalAudioVolume = 0.1f,
            float newAudioVolume = 1.0f,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Validate input paths
            if (!File.Exists(videoFilePath))
                throw new FileNotFoundException("Video file not found", videoFilePath);
                
            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Audio file not found", audioFilePath);
            
            // Ensure FFmpeg is initialized
            await InitializeFFmpegAsync();
            
            try
            {
                // Report initial progress
                progressCallback?.Report(0.1);
                
                // Create output directory if needed
                string outputDir = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                // Load media info
                var mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath);
                
                // Report progress after loading media info
                progressCallback?.Report(0.2);

                // Create conversion
                IConversion conversion;
                  if (keepOriginalAudio)
                {
                    // Keep original audio and mix with new audio
                    conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-i \"{videoFilePath}\"")
                        .AddParameter($"-i \"{audioFilePath}\"")
                        .AddParameter($"-filter_complex \"[0:a]volume={originalAudioVolume}[a1];[1:a]volume={newAudioVolume}[a2];[a1][a2]amix=inputs=2:duration=longest[aout]\"")
                        .AddParameter("-map 0:v -map \"[aout]\"")
                        .AddParameter("-c:v copy")
                        .SetOutput(outputFilePath);
                }
                else
                {
                    // Replace original audio with new audio
                    conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-i \"{videoFilePath}\"")
                        .AddParameter($"-i \"{audioFilePath}\"")
                        .AddParameter("-map 0:v -map 1:a")
                        .AddParameter("-c:v copy -c:a aac")
                        .SetOutput(outputFilePath);
                }
                
                // Report progress after setting up conversion
                progressCallback?.Report(0.3);
                
                // Monitor progress
                conversion.OnProgress += (sender, args) =>
                {
                    double progress = Math.Min(0.9, 0.3 + (args.Percent * 0.6 / 100.0));
                    progressCallback?.Report(progress);
                };
                
                // Start the conversion
                await conversion.Start(cancellationToken);
                
                // Report completion
                progressCallback?.Report(1.0);
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"FFmpeg audio-video merge failed: {ex.Message}", ex);
            }
        }
    }
}
