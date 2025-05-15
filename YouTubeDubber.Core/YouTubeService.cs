using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;
using YouTubeDubber.Core.Services;

namespace YouTubeDubber.Core
{
    /// <summary>
    /// Implementation of the YouTube service using YoutubeExplode library
    /// </summary>
    public class YouTubeService : IYouTubeService
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly string _downloadFolder;
        private readonly AudioExtractorService _audioExtractor;
          /// <summary>
        /// Initializes a new instance of the YouTubeService class
        /// </summary>
        /// <param name="downloadFolder">Optional folder path where downloaded videos will be stored</param>
        public YouTubeService(string? downloadFolder = null)
        {
            _youtubeClient = new YoutubeClient();
            _downloadFolder = downloadFolder ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "YouTubeDubber", 
                "Downloads");
            
            // Ensure the download directory exists
            Directory.CreateDirectory(_downloadFolder);
            
            // Initialize the audio extractor service
            _audioExtractor = new AudioExtractorService();
        }
        
        /// <inheritdoc />
        public bool IsValidYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
                
            // Check if we can extract a video ID from the URL
            try
            {
                string videoId = ExtractVideoId(url);
                return !string.IsNullOrEmpty(videoId);
            }
            catch
            {
                return false;
            }
        }
        
        /// <inheritdoc />
        public string ExtractVideoId(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;
            
            // Handle standard YouTube URLs like:
            // https://www.youtube.com/watch?v=VIDEO_ID
            // https://youtu.be/VIDEO_ID
            // https://www.youtube.com/v/VIDEO_ID
            // https://www.youtube.com/embed/VIDEO_ID
            
            try
            {
                // Try to use YoutubeExplode's parser
                return VideoId.TryParse(url)?.Value;
            }
            catch
            {
                // Fallback to regex extraction if YoutubeExplode fails
                var youtubeIdRegex = new Regex(
                    @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})",
                    RegexOptions.IgnoreCase);
                
                var match = youtubeIdRegex.Match(url);
                return match.Success ? match.Groups[1].Value : null;
            }
        }
        
        /// <inheritdoc />
        public async Task<VideoInfo> GetVideoInfoAsync(string videoUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                // Extract video ID from URL
                string videoId = ExtractVideoId(videoUrl);
                if (string.IsNullOrEmpty(videoId))
                {
                    throw new ArgumentException("Invalid YouTube URL", nameof(videoUrl));
                }
                
                // Get video metadata using YoutubeExplode
                var video = await _youtubeClient.Videos.GetAsync(videoId, cancellationToken);
                
                // Create and return VideoInfo object
                return new VideoInfo
                {
                    Id = video.Id,
                    Title = video.Title,
                    Author = video.Author.ChannelTitle,
                    Duration = video.Duration ?? TimeSpan.Zero,
                    ThumbnailUrl = video.Thumbnails.FirstOrDefault()?.Url,
                    UploadDate = video.UploadDate,
                    Description = video.Description,
                    ViewCount = video.Engagement.ViewCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get video info: {ex.Message}", ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<string> DownloadVideoAsync(
            string videoUrl, 
            string? outputFilePath = null, 
            string quality = "highest",
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Extract video ID
                string videoId = ExtractVideoId(videoUrl);
                if (string.IsNullOrEmpty(videoId))
                {
                    throw new ArgumentException("Invalid YouTube URL", nameof(videoUrl));
                }
                
                // Get video details to use for the filename if not provided
                var video = await _youtubeClient.Videos.GetAsync(videoId, cancellationToken);
                
                // Generate a valid filename if not provided
                if (string.IsNullOrEmpty(outputFilePath))
                {
                    // Sanitize the title to create a valid filename
                    string sanitizedTitle = SanitizeFileName(video.Title);
                    outputFilePath = Path.Combine(_downloadFolder, $"{sanitizedTitle}.mp4");
                }
                
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                
                // Get all available streams
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId, cancellationToken);

                // Select the desired quality video stream
                IVideoStreamInfo? videoStream = null;
                
                switch (quality.ToLower())
                {
                    case "highest":
                        // Get the highest quality MP4 stream
                        videoStream = streamManifest.GetMuxedStreams()
                            .Where(s => s.Container == Container.Mp4)
                            .OrderByDescending(s => s.VideoResolution.Height)
                            .FirstOrDefault();
                        break;
                        
                    case "lowest":
                        // Get the lowest quality MP4 stream
                        videoStream = streamManifest.GetMuxedStreams()
                            .Where(s => s.Container == Container.Mp4)
                            .OrderBy(s => s.VideoResolution.Height)
                            .FirstOrDefault();
                        break;
                        
                    default: // "medium" or any other value
                        // Get a medium quality stream
                        var streams = streamManifest.GetMuxedStreams()
                            .Where(s => s.Container == Container.Mp4)
                            .OrderBy(s => s.VideoResolution.Height)
                            .ToList();
                        
                        // Get a stream from the middle of the list
                        int midIndex = streams.Count / 2;
                        videoStream = streams.Count > 0 ? streams[midIndex] : null;
                        break;
                }
                
                if (videoStream == null)
                {
                    throw new InvalidOperationException($"No suitable video stream found for quality: {quality}");
                }
                
                // Set up progress reporting
                var progress = new Progress<double>(p => progressCallback?.Report(p));
                
                // Download the stream
                await _youtubeClient.Videos.Streams.DownloadAsync(videoStream, outputFilePath, progress, cancellationToken);
                
                // Return the path to the downloaded file
                return outputFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download video: {ex.Message}", ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<string> DownloadAudioOnlyAsync(
            string videoUrl,
            string? outputFilePath = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Extract video ID
                string videoId = ExtractVideoId(videoUrl);
                if (string.IsNullOrEmpty(videoId))
                {
                    throw new ArgumentException("Invalid YouTube URL", nameof(videoUrl));
                }
                
                // Get video details
                var video = await _youtubeClient.Videos.GetAsync(videoId, cancellationToken);
                
                // Generate audio filename if not provided
                if (string.IsNullOrEmpty(outputFilePath))
                {
                    string sanitizedTitle = SanitizeFileName(video.Title);
                    outputFilePath = Path.Combine(_downloadFolder, $"{sanitizedTitle}.mp3");
                }
                
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                
                // Get all available streams
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId, cancellationToken);
                
                // Get the best audio stream
                var audioStreamInfo = streamManifest
                    .GetAudioOnlyStreams()
                    .OrderByDescending(s => s.Bitrate)
                    .FirstOrDefault();
                
                if (audioStreamInfo == null)
                {
                    throw new InvalidOperationException("No audio stream found for this video");
                }
                
                // Set up progress reporting
                var progress = new Progress<double>(p => progressCallback?.Report(p));
                
                // Download the audio stream
                await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, outputFilePath, progress, cancellationToken);
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download audio: {ex.Message}", ex);
            }
        }
          /// <inheritdoc />
        public async Task<string> ExtractAudioFromVideoAsync(
            string videoFilePath,
            string? outputFilePath = null,
            AudioExtractionOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _audioExtractor.ExtractAudioFromVideoAsync(
                    videoFilePath,
                    outputFilePath,
                    options,
                    progressCallback,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to extract audio from video: {ex.Message}", ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<string> DownloadAndExtractAudioAsync(
            string videoUrl,
            string? outputFilePath = null,
            AudioExtractionOptions? options = null,
            bool deleteVideoAfterExtraction = true,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Create a composite progress that splits between download (50%) and extraction (50%)
                IProgress<double>? combinedProgress = null;
                if (progressCallback != null)
                {
                    combinedProgress = new Progress<double>(p => 
                    {
                        // First half of progress is download, second half is extraction
                        progressCallback.Report(p / 2);
                    });
                }
                
                // Download the video first
                string videoFilePath = await DownloadVideoAsync(
                    videoUrl,
                    null, // Use default path for temporary video file
                    "highest", // Use high quality for best audio
                    combinedProgress,
                    cancellationToken);
                
                try
                {
                    // Update progress to indicate we're starting extraction
                    progressCallback?.Report(0.5);
                    
                    // Create extraction progress that maps to second half of the total progress
                    IProgress<double>? extractionProgress = null;
                    if (progressCallback != null)
                    {
                        extractionProgress = new Progress<double>(p => 
                        {
                            // Map extraction progress to second half of total progress
                            progressCallback.Report(0.5 + (p / 2));
                        });
                    }
                    
                    // Extract audio from the downloaded video
                    string audioFilePath = await ExtractAudioFromVideoAsync(
                        videoFilePath,
                        outputFilePath,
                        options,
                        extractionProgress,
                        cancellationToken);
                    
                    // Delete the video file if requested
                    if (deleteVideoAfterExtraction && File.Exists(videoFilePath))
                    {
                        File.Delete(videoFilePath);
                    }
                    
                    return audioFilePath;
                }
                catch
                {
                    // If extraction fails, still try to delete the video file if requested
                    if (deleteVideoAfterExtraction && File.Exists(videoFilePath))
                    {
                        try { File.Delete(videoFilePath); } catch { /* Ignore cleanup errors */ }
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download and extract audio: {ex.Message}", ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<string> DownloadAndExtractSpeechAudioAsync(
            string videoUrl,
            string? outputFilePath = null,
            bool deleteVideoAfterExtraction = true,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Configure optimal options for speech recognition
            var options = new AudioExtractionOptions
            {
                Format = AudioFormat.Wav,    // WAV is best for speech recognition
                SampleRate = 16000,          // 16kHz is standard for most speech recognition APIs
                Channels = 1,                // Mono is better for speech recognition
                Bitrate = 192,               // Higher quality
                NormalizeAudio = true,       // Normalize audio levels
                ApplyNoiseReduction = true   // Reduce background noise
            };
            
            return await DownloadAndExtractAudioAsync(
                videoUrl,
                outputFilePath,
                options,
                deleteVideoAfterExtraction,
                progressCallback,
                cancellationToken);
        }

        #region Helper Methods
        
        /// <summary>
        /// Sanitizes a string to be used as a valid file name
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            // Remove invalid file name characters
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string invalidReStr = $"[{Regex.Escape(invalidChars)}]";
            string sanitized = Regex.Replace(fileName, invalidReStr, "-");
            
            // Limit the length to avoid file system limitations
            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100);
            }
            
            return sanitized.Trim();
        }
        
        #endregion
    }
}
