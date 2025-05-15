using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Service for extracting and processing audio from video files
    /// </summary>
    public class AudioExtractorService
    {
        private bool _ffmpegInitialized = false;
        
        /// <summary>
        /// Initializes a new instance of the AudioExtractorService class
        /// </summary>
        public AudioExtractorService()
        {
        }
          /// <summary>
        /// Ensures FFmpeg is downloaded and available
        /// </summary>
        public async Task InitializeFFmpegAsync(CancellationToken cancellationToken = default)
        {
            if (!_ffmpegInitialized)
            {
                // Download FFmpeg if it's not already installed
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
                _ffmpegInitialized = true;
            }
        }
        
        /// <summary>
        /// Extracts audio from a video file with optimized settings for speech recognition
        /// </summary>
        /// <param name="videoFilePath">Path to the video file</param>
        /// <param name="outputFilePath">Output audio file path (optional, will generate if not provided)</param>
        /// <param name="options">Audio extraction options</param>
        /// <param name="progressCallback">Progress reporting callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the extracted audio file</returns>
        public async Task<string> ExtractAudioFromVideoAsync(
            string videoFilePath,
            string? outputFilePath = null,
            AudioExtractionOptions? options = null,
            IProgress<double>? progressCallback = null, 
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(videoFilePath))
            {
                throw new FileNotFoundException("Video file not found", videoFilePath);
            }
            
            // Initialize FFmpeg if needed
            await InitializeFFmpegAsync(cancellationToken);
            
            // Use default options if none provided
            options ??= new AudioExtractionOptions();
            
            // Generate output path if not provided
            if (string.IsNullOrEmpty(outputFilePath))
            {
                string directory = Path.GetDirectoryName(videoFilePath) ?? Path.GetTempPath();
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFilePath);
                string extension = GetFileExtension(options.Format);
                outputFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_audio{extension}");
            }
            
            // Ensure the output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
            
            // Get media info from the video
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath, cancellationToken);
            
            // Configure conversion based on options
            var conversion = FFmpeg.Conversions.New();
            
            // Configure input file and start/duration if needed
            if (options.StartTime > 0 || options.Duration > 0)
            {
                var startTime = TimeSpan.FromSeconds(options.StartTime);
                
                if (options.Duration > 0)
                {
                    var duration = TimeSpan.FromSeconds(options.Duration);
                    conversion.AddParameter($"-ss {startTime} -t {duration}");
                }
                else
                {
                    conversion.AddParameter($"-ss {startTime}");
                }
            }
            
            conversion.AddParameter($"-i \"{videoFilePath}\"");
            
            // If we want mono audio (better for speech recognition)
            if (options.Channels == 1)
            {
                conversion.AddParameter("-ac 1");
            }
            
            // Set sample rate (16kHz is often best for speech recognition)
            conversion.AddParameter($"-ar {options.SampleRate}");
            
            // Set bitrate
            conversion.AddParameter($"-b:a {options.Bitrate}k");
            
            // Apply audio normalization if requested
            if (options.NormalizeAudio)
            {
                conversion.AddParameter("-filter:a loudnorm");
            }
            
            // Apply noise reduction if requested
            if (options.ApplyNoiseReduction)
            {
                conversion.AddParameter("-af afftdn=nf=-25");
            }
            
            // Set format-specific options
            switch (options.Format)
            {
                case AudioFormat.Wav:
                    conversion.AddParameter("-acodec pcm_s16le");
                    break;
                case AudioFormat.Mp3:
                    conversion.AddParameter("-codec:a libmp3lame -qscale:a 2");
                    break;
                case AudioFormat.Flac:
                    conversion.AddParameter("-codec:a flac");
                    break;
                case AudioFormat.Ogg:
                    conversion.AddParameter("-codec:a libvorbis -q:a 4");
                    break;
            }
            
            // Disable video and subtitles
            conversion.AddParameter("-vn -sn");
            
            // Set output file
            conversion.SetOutput(outputFilePath);
            
            // Set up progress reporting if needed
            if (progressCallback != null)
            {
                conversion.OnProgress += (sender, args) => 
                {
                    progressCallback.Report(args.Percent / 100.0);
                };
            }
            
            // Start the conversion
            await conversion.Start(cancellationToken);
            
            return outputFilePath;
        }
        
        /// <summary>
        /// Extracts high-quality audio optimized for speech recognition from a video
        /// </summary>
        /// <param name="videoFilePath">Path to the video file</param>
        /// <param name="outputFilePath">Output audio file path (optional)</param>
        /// <param name="progressCallback">Progress reporting callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the extracted audio file</returns>
        public async Task<string> ExtractSpeechOptimizedAudioAsync(
            string videoFilePath,
            string? outputFilePath = null,
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
            
            return await ExtractAudioFromVideoAsync(
                videoFilePath,
                outputFilePath,
                options,
                progressCallback,
                cancellationToken
            );
        }
        
        /// <summary>
        /// Gets the file extension for the specified audio format
        /// </summary>
        private string GetFileExtension(AudioFormat format)
        {
            return format switch
            {
                AudioFormat.Wav => ".wav",
                AudioFormat.Mp3 => ".mp3",
                AudioFormat.Flac => ".flac",
                AudioFormat.Ogg => ".ogg",
                _ => ".wav"
            };
        }
    }
}
