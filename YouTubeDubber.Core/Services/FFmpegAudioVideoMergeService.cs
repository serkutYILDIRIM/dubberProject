using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Service for merging audio and video files using FFmpeg
    /// </summary>
    public class FFmpegAudioVideoMergeService : IAudioVideoMergeService
    {
        private bool _ffmpegInitialized = false;
        
        // Audio mixing profiles
        private readonly Dictionary<string, (float backgroundVolume, float voiceVolume, float ducking)> _mixingProfiles = 
            new Dictionary<string, (float, float, float)>
            {
                { "background", (0.5f, 1.0f, 0.8f) },       // Emphasizes background sounds
                { "voice", (0.15f, 1.2f, 0.9f) },           // Emphasizes voice/dubbing
                { "music", (0.7f, 0.9f, 0.7f) },            // Preserves music well
                { "balanced", (0.3f, 1.0f, 0.85f) },        // Balanced mix
                { "voice-centered", (0.1f, 1.2f, 0.95f) }   // Maximum voice clarity
            };
        
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
        
        /// <summary>
        /// Creates a dubbed video with enhanced audio mixing for Turkish dubbing
        /// </summary>
        public async Task<string> CreateTurkishDubbedVideoAsync(
            string videoFilePath,
            string dubbingAudioPath,
            string outputFilePath,
            bool preserveBackgroundSounds = true,
            bool addSubtitles = false,
            string? subtitlesFilePath = null,
            string mixingProfile = "balanced",
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Validate input paths
            if (!File.Exists(videoFilePath))
                throw new FileNotFoundException("Video file not found", videoFilePath);
                
            if (!File.Exists(dubbingAudioPath))
                throw new FileNotFoundException("Dubbing audio file not found", dubbingAudioPath);
            
            if (addSubtitles && (string.IsNullOrEmpty(subtitlesFilePath) || !File.Exists(subtitlesFilePath)))
                throw new FileNotFoundException("Subtitles file not found", subtitlesFilePath ?? "");
            
            // Ensure FFmpeg is initialized
            await InitializeFFmpegAsync();
            
            try
            {
                // Report initial progress
                progressCallback?.Report(0.05);
                
                // Create output directory if needed
                string outputDir = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                // Load media info
                var mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath);
                
                // Report progress after loading media info
                progressCallback?.Report(0.1);
                
                // Temporary files for audio processing
                string tempDir = Path.Combine(Path.GetTempPath(), "YTDubber_" + Guid.NewGuid().ToString().Substring(0, 8));
                Directory.CreateDirectory(tempDir);
                
                string extractedOriginalAudioPath = Path.Combine(tempDir, "original_audio.wav");
                string processedBackgroundPath = string.Empty;
                string normalizedDubbingPath = Path.Combine(tempDir, "normalized_dubbing.wav");
                
                try
                {
                    // Get mixing profile params (or use balanced as default)
                    var profileParams = _mixingProfiles.ContainsKey(mixingProfile) 
                        ? _mixingProfiles[mixingProfile] 
                        : _mixingProfiles["balanced"];
                    
                    float backgroundVolume = profileParams.backgroundVolume;
                    float voiceVolume = profileParams.voiceVolume;
                    float ducking = profileParams.ducking;
                    
                    progressCallback?.Report(0.15);
                    
                    // Step 1: Extract original audio
                    await ExtractAudioFromVideo(videoFilePath, extractedOriginalAudioPath);
                    
                    progressCallback?.Report(0.25);
                    
                    // Step 2: Apply audio processing if preserving background sounds
                    if (preserveBackgroundSounds)
                    {
                        progressCallback?.Report(0.3);
                        
                        // Process background sounds (remove/reduce vocals from original)
                        processedBackgroundPath = Path.Combine(tempDir, "background_sounds.wav");
                        await ExtractBackgroundSounds(extractedOriginalAudioPath, processedBackgroundPath);
                        
                        progressCallback?.Report(0.4);
                        
                        // Normalize dubbing audio
                        await NormalizeAudio(dubbingAudioPath, normalizedDubbingPath);
                        
                        progressCallback?.Report(0.5);
                    }
                    
                    // Create conversion
                    IConversion conversion;
                    
                    if (preserveBackgroundSounds)
                    {
                        // Advanced audio mixing with background preservation
                        conversion = FFmpeg.Conversions.New()
                            .AddParameter($"-i \"{videoFilePath}\"")
                            .AddParameter($"-i \"{normalizedDubbingPath}\"")
                            .AddParameter($"-i \"{processedBackgroundPath}\"");
                            
                        // Create complex audio filter for ducking (background gets quieter when voice is present)
                        string filterComplex = $"-filter_complex \"" +
                            $"[1:a]volume={voiceVolume},apad[voice];" +
                            $"[2:a]volume={backgroundVolume}[bg];" +
                            $"[bg][voice]sidechaincompress=threshold=0.03:ratio={ducking}:release=300[final]\"" +
                            $" -map 0:v -map \"[final]\"";
                            
                        conversion.AddParameter(filterComplex);
                    }
                    else
                    {
                        // Simple replacement of audio
                        conversion = FFmpeg.Conversions.New()
                            .AddParameter($"-i \"{videoFilePath}\"")
                            .AddParameter($"-i \"{dubbingAudioPath}\"")
                            .AddParameter($"-map 0:v -map 1:a")
                            .AddParameter($"-c:a aac -b:a 192k");
                    }
                    
                    // If adding subtitles
                    if (addSubtitles && !string.IsNullOrEmpty(subtitlesFilePath))
                    {
                        string subtitleExt = Path.GetExtension(subtitlesFilePath).ToLower();
                        
                        if (subtitleExt == ".srt")
                        {
                            // Use subtitles filter for SRT files
                            conversion.AddParameter($"-vf subtitles='{subtitlesFilePath.Replace("\\", "\\\\")}'");
                        }
                        else if (subtitleExt == ".ass" || subtitleExt == ".ssa")
                        {
                            // Use ASS filter for ASS/SSA files
                            conversion.AddParameter($"-vf ass='{subtitlesFilePath.Replace("\\", "\\\\")}'");
                        }
                        else
                        {
                            // For other formats, just try generic subtitles filter
                            conversion.AddParameter($"-vf subtitles='{subtitlesFilePath.Replace("\\", "\\\\")}'");
                        }
                    }
                    
                    // Set other output parameters
                    conversion.AddParameter("-c:v copy")
                        .SetOutput(outputFilePath);
                    
                    // Report progress after setting up conversion
                    progressCallback?.Report(0.6);
                    
                    // Monitor progress
                    conversion.OnProgress += (sender, args) =>
                    {
                        double progress = Math.Min(0.95, 0.6 + (args.Percent * 0.35 / 100.0));
                        progressCallback?.Report(progress);
                    };
                    
                    // Start the conversion
                    await conversion.Start(cancellationToken);
                    
                    // Report completion
                    progressCallback?.Report(1.0);
                    
                    return outputFilePath;
                }
                finally
                {
                    // Clean up temporary files
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch
                    {
                        // Ignore errors in cleanup
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"FFmpeg Turkish dubbing failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Creates a Turkish dubbed video with enhanced options
        /// </summary>
        public async Task<string> CreateTurkishDubbedVideoAsync(
            string videoFilePath,
            string dubbingAudioPath,
            string outputFilePath,
            TurkishDubbingOptions options,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Validate input paths
            if (!File.Exists(videoFilePath))
                throw new FileNotFoundException("Video file not found", videoFilePath);
                
            if (!File.Exists(dubbingAudioPath))
                throw new FileNotFoundException("Dubbing audio file not found", dubbingAudioPath);
            
            if (options.AddSubtitles && (string.IsNullOrEmpty(options.SubtitlesFilePath) || !File.Exists(options.SubtitlesFilePath)))
                throw new FileNotFoundException("Subtitles file not found", options.SubtitlesFilePath ?? "");
            
            // Ensure FFmpeg is initialized
            await InitializeFFmpegAsync();
            
            try
            {
                // Report initial progress
                progressCallback?.Report(0.05);
                
                // Create output directory if needed
                string outputDir = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                // Load media info
                var mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath);
                
                // Report progress after loading media info
                progressCallback?.Report(0.1);
                
                // Temporary files for audio processing
                string tempDir = Path.Combine(Path.GetTempPath(), "YTDubber_" + Guid.NewGuid().ToString().Substring(0, 8));
                Directory.CreateDirectory(tempDir);
                
                string extractedOriginalAudioPath = Path.Combine(tempDir, "original_audio.wav");
                string processedBackgroundPath = string.Empty;
                string normalizedDubbingPath = Path.Combine(tempDir, "normalized_dubbing.wav");
                
                try
                {
                    // Get mixing profile params
                    var profileParams = GetMixingProfileParams(options);
                    
                    float backgroundVolume = profileParams.backgroundVolume;
                    float voiceVolume = profileParams.voiceVolume;
                    float ducking = profileParams.ducking;
                    
                    progressCallback?.Report(0.15);
                    
                    // Step 1: Extract original audio
                    await ExtractAudioFromVideo(videoFilePath, extractedOriginalAudioPath);
                    
                    progressCallback?.Report(0.25);
                    
                    // Step 2: Apply audio processing if preserving background sounds
                    if (options.PreserveBackgroundSounds)
                    {
                        progressCallback?.Report(0.3);
                        
                        // Process background sounds (remove/reduce vocals from original)
                        processedBackgroundPath = Path.Combine(tempDir, "background_sounds.wav");
                        await ExtractBackgroundSounds(extractedOriginalAudioPath, processedBackgroundPath);
                        
                        progressCallback?.Report(0.4);
                        
                        // Normalize dubbing audio
                        await NormalizeAudio(dubbingAudioPath, normalizedDubbingPath);
                        
                        progressCallback?.Report(0.5);
                    }
                    
                    // Create conversion
                    IConversion conversion;
                    
                    if (options.PreserveBackgroundSounds)
                    {
                        // Advanced audio mixing with background preservation
                        string dubbingPath = File.Exists(normalizedDubbingPath) ? normalizedDubbingPath : dubbingAudioPath;
                        
                        conversion = FFmpeg.Conversions.New()
                            .AddParameter($"-i \"{videoFilePath}\"")
                            .AddParameter($"-i \"{dubbingPath}\"")
                            .AddParameter($"-i \"{processedBackgroundPath}\"");
                            
                        // Create complex audio filter for ducking (background gets quieter when voice is present)
                        string filterComplex = $"-filter_complex \"" +
                            $"[1:a]volume={voiceVolume},apad[voice];" +
                            $"[2:a]volume={backgroundVolume}[bg];" +
                            $"[bg][voice]sidechaincompress=threshold=0.03:ratio={ducking}:release=300[final]\"" +
                            $" -map 0:v -map \"[final]\"";
                            
                        conversion.AddParameter(filterComplex);
                    }
                    else
                    {
                        // Simple replacement of audio
                        conversion = FFmpeg.Conversions.New()
                            .AddParameter($"-i \"{videoFilePath}\"")
                            .AddParameter($"-i \"{dubbingAudioPath}\"")
                            .AddParameter($"-map 0:v -map 1:a")
                            .AddParameter($"-c:a aac -b:a 192k");
                    }
                    
                    // If adding subtitles
                    if (options.AddSubtitles && !string.IsNullOrEmpty(options.SubtitlesFilePath))
                    {
                        string subtitleExt = Path.GetExtension(options.SubtitlesFilePath).ToLower();
                        
                        if (subtitleExt == ".srt")
                        {
                            // Use subtitles filter for SRT files
                            conversion.AddParameter($"-vf subtitles='{options.SubtitlesFilePath.Replace("\\", "\\\\")}'");
                        }
                        else if (subtitleExt == ".ass" || subtitleExt == ".ssa")
                        {
                            // Use ASS filter for ASS/SSA files
                            conversion.AddParameter($"-vf ass='{options.SubtitlesFilePath.Replace("\\", "\\\\")}'");
                        }
                        else
                        {
                            // For other formats, just try generic subtitles filter
                            conversion.AddParameter($"-vf subtitles='{options.SubtitlesFilePath.Replace("\\", "\\\\")}'");
                        }
                    }
                    
                    // Apply video quality options
                    string outputFormat = options.OutputFormat.ToLower();
                    
                    // For MP4, we can use H.264 with CRF for quality
                    if (outputFormat == "mp4")
                    {
                        conversion.AddParameter($"-c:v libx264 -crf {options.VideoQuality} -preset medium");
                    }
                    // For other formats, just copy the video stream
                    else
                    {
                        conversion.AddParameter("-c:v copy");
                    }
                    
                    // Ensure output file has the correct extension
                    string outputExtension = Path.GetExtension(outputFilePath).TrimStart('.');
                    if (string.IsNullOrEmpty(outputExtension) || outputExtension.ToLower() != outputFormat)
                    {
                        outputFilePath = Path.ChangeExtension(outputFilePath, outputFormat);
                    }
                    
                    conversion.SetOutput(outputFilePath);
                    
                    // Report progress after setting up conversion
                    progressCallback?.Report(0.6);
                    
                    // Monitor progress
                    conversion.OnProgress += (sender, args) =>
                    {
                        double progress = Math.Min(0.95, 0.6 + (args.Percent * 0.35 / 100.0));
                        progressCallback?.Report(progress);
                    };
                    
                    // Start the conversion
                    await conversion.Start(cancellationToken);
                    
                    // Report completion
                    progressCallback?.Report(1.0);
                    
                    return outputFilePath;
                }
                finally
                {
                    // Clean up temporary files
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch
                    {
                        // Ignore errors in cleanup
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"FFmpeg Turkish dubbing failed: {ex.Message}", ex);
            }
        }
        
        #region Private Helper Methods
        
        /// <summary>
        /// Gets the mixing profile parameters based on the options
        /// </summary>
        private (float backgroundVolume, float voiceVolume, float ducking) GetMixingProfileParams(TurkishDubbingOptions options)
        {
            // If using custom profile, return the custom values
            if (options.MixingProfile.ToLower() == "custom")
            {
                return (options.CustomBackgroundVolume, options.CustomVoiceVolume, options.CustomDucking);
            }
            
            // Otherwise, get the named profile (or use balanced as default)
            return _mixingProfiles.ContainsKey(options.MixingProfile) 
                ? _mixingProfiles[options.MixingProfile] 
                : _mixingProfiles["balanced"];
        }
        
        /// <summary>
        /// Extracts audio from a video file
        /// </summary>
        private async Task ExtractAudioFromVideo(string videoPath, string outputAudioPath)
        {
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{videoPath}\"")
                .AddParameter("-vn -acodec pcm_s16le -ar 44100 -ac 2")
                .SetOutput(outputAudioPath);
                
            await conversion.Start();
        }
        
        /// <summary>
        /// Process the audio to extract background sounds by reducing vocals
        /// </summary>
        private async Task ExtractBackgroundSounds(string audioPath, string outputPath)
        {
            // This filter uses center channel extraction to reduce vocals
            // Most vocals are centered in the stereo field
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{audioPath}\"")
                .AddParameter("-af \"pan=stereo|c0=c0-0.5*c1|c1=c1-0.5*c0,loudnorm\"")
                .SetOutput(outputPath);
                
            await conversion.Start();
        }
        
        /// <summary>
        /// Normalizes audio volume levels
        /// </summary>
        private async Task NormalizeAudio(string audioPath, string outputPath)
        {
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{audioPath}\"")
                .AddParameter("-af \"loudnorm=I=-16:LRA=11:TP=-1.5\"")
                .SetOutput(outputPath);
                
            await conversion.Start();
        }
        
        #endregion
    }
}
