using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Speech recognition service implementation using OpenAI's Whisper model locally
    /// </summary>
    public class WhisperSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly string _modelsDirectory;
        private WhisperProcessor? _whisperProcessor;
        private readonly object _lockObject = new object();
        
        /// <summary>
        /// Available Whisper model sizes with their characteristics
        /// </summary>
        public enum WhisperModelSize
        {
            /// <summary>
            /// Tiny model - fastest, least accurate (~39 MB)
            /// </summary>
            Tiny,
            /// <summary>
            /// Base model - good balance between speed and accuracy (~74 MB)
            /// </summary>
            Base,
            /// <summary>
            /// Small model - better accuracy, moderate speed (~244 MB)
            /// </summary>
            Small,
            /// <summary>
            /// Medium model - high accuracy, slower (~769 MB)
            /// </summary>
            Medium,
            /// <summary>
            /// Large model - highest accuracy, slowest (~1550 MB)
            /// </summary>
            Large
        }
        
        /// <summary>
        /// Initializes a new instance of the Whisper Speech Recognition Service
        /// </summary>
        /// <param name="modelsDirectory">Directory where Whisper models will be stored</param>
        public WhisperSpeechRecognitionService(string? modelsDirectory = null)
        {
            _modelsDirectory = modelsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YouTubeDubber", "WhisperModels");
            
            // Ensure models directory exists
            Directory.CreateDirectory(_modelsDirectory);
        }
        
        /// <summary>
        /// Downloads and initializes a Whisper model
        /// </summary>
        /// <param name="modelSize">The size of the model to download</param>
        /// <param name="progressCallback">Progress callback for download</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the downloaded model file</returns>
        public async Task<string> EnsureModelDownloadedAsync(
            WhisperModelSize modelSize = WhisperModelSize.Base,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var modelFileName = GetModelFileName(modelSize);
            var modelPath = Path.Combine(_modelsDirectory, modelFileName);
            
            if (File.Exists(modelPath))
            {
                return modelPath;
            }
            
            progressCallback?.Report(0.0);
              try
            {
                var ggmlType = GetGgmlModelType(modelSize);                // Download the model using Whisper.net's built-in downloader
                using var httpClient = new HttpClient();
                var downloader = new WhisperGgmlDownloader(httpClient);
                using var modelStream = await downloader.GetGgmlModelAsync(ggmlType);
                
                // Save the model to disk
                using var fileStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write);
                
                var buffer = new byte[8192];
                long totalBytesRead = 0;
                long? totalBytes = modelStream.CanSeek ? modelStream.Length : null;
                int bytesRead;
                
                while ((bytesRead = await modelStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytesRead += bytesRead;
                    
                    if (totalBytes.HasValue && totalBytes.Value > 0)
                    {
                        var progress = (double)totalBytesRead / totalBytes.Value;
                        progressCallback?.Report(progress);
                    }
                }
                
                progressCallback?.Report(1.0);
                return modelPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download Whisper model: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Transcribes the audio file to text using Whisper
        /// </summary>
        public async Task<TranscriptionResult> TranscribeAudioFileAsync(
            string audioFilePath,
            SpeechRecognitionOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException("Audio file not found", audioFilePath);
            }
            
            options ??= new SpeechRecognitionOptions();
            
            var transcriptionResult = new TranscriptionResult
            {
                AudioFilePath = audioFilePath,
                LanguageCode = options.LanguageCode
            };
            
            try
            {
                // Ensure Whisper processor is initialized
                await InitializeWhisperProcessorAsync(progressCallback, cancellationToken);
                
                if (_whisperProcessor == null)
                {
                    throw new InvalidOperationException("Whisper processor is not initialized");
                }
                
                progressCallback?.Report(0.1); // Model loaded
                
                // Determine language for Whisper
                var whisperLanguage = GetWhisperLanguage(options.LanguageCode);
                
                var segments = new List<TranscriptionSegment>();
                  // Process the audio file
                using var audioStream = File.OpenRead(audioFilePath);
                await foreach (var segment in _whisperProcessor.ProcessAsync(audioStream, cancellationToken))
                {
                    var transcriptionSegment = new TranscriptionSegment
                    {
                        Text = segment.Text.Trim(),
                        StartTime = segment.Start.TotalSeconds,
                        EndTime = segment.End.TotalSeconds,
                        Confidence = 1.0 // Whisper doesn't provide confidence scores, so we set it to 1.0
                    };
                    
                    if (!string.IsNullOrWhiteSpace(transcriptionSegment.Text))
                    {
                        segments.Add(transcriptionSegment);
                    }
                }
                
                transcriptionResult.Segments = segments;
                progressCallback?.Report(1.0);
                
                // Detect if the content is in Turkish
                if (IsLikelyTurkish(transcriptionResult.FullText))
                {
                    transcriptionResult.LanguageCode = "tr-TR";
                }
                
                return transcriptionResult;
            }
            catch (Exception ex)
            {
                throw new Exception($"Whisper transcription failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Validates the Whisper service (always returns true since it's local)
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync(
            SpeechRecognitionOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeWhisperProcessorAsync(null, cancellationToken);
                return _whisperProcessor != null;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Saves the transcription result to a file in the specified format
        /// </summary>
        public async Task<string> SaveTranscriptionAsync(
            TranscriptionResult transcriptionResult,
            string outputFilePath,
            string format = "txt")
        {
            if (transcriptionResult == null || transcriptionResult.Segments.Count == 0)
            {
                throw new ArgumentException("Transcription result is empty or null");
            }
            
            string content;
            
            switch (format.ToLower())
            {
                case "txt":
                    content = transcriptionResult.FullText;
                    break;
                    
                case "srt":
                    content = FormatAsSrt(transcriptionResult);
                    break;
                    
                case "json":
                    content = JsonSerializer.Serialize(transcriptionResult, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    break;
                    
                case "vtt":
                    content = FormatAsVtt(transcriptionResult);
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }
            
            // Ensure output directory exists
            var outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            
            await File.WriteAllTextAsync(outputFilePath, content, Encoding.UTF8);
            return outputFilePath;
        }
        
        /// <summary>
        /// Initializes the Whisper processor if not already initialized
        /// </summary>        private async Task InitializeWhisperProcessorAsync(
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (_whisperProcessor != null)
                return;
                
            string modelPath;
                
            lock (_lockObject)
            {
                if (_whisperProcessor != null)
                    return;
            }
                
            // Download model if needed - using await properly
            modelPath = await EnsureModelDownloadedAsync(WhisperModelSize.Base, progressCallback, cancellationToken);
              
            lock (_lockObject)
            {
                // Check again in case another thread initialized while we were downloading
                if (_whisperProcessor != null)
                    return;
                    
                // Create Whisper processor
                _whisperProcessor = WhisperFactory.FromPath(modelPath).CreateBuilder()
                    .WithLanguage("auto") // Auto-detect language
                    .Build();
            }
        }
        
        /// <summary>
        /// Gets the appropriate Whisper language code from the standard language code
        /// </summary>
        private static string GetWhisperLanguage(string languageCode)
        {
            var lowerCode = languageCode.ToLower();
            
            if (lowerCode.StartsWith("tr"))
                return "tr"; // Turkish
            if (lowerCode.StartsWith("en"))
                return "en"; // English
            if (lowerCode.StartsWith("es"))
                return "es"; // Spanish
            if (lowerCode.StartsWith("fr"))
                return "fr"; // French
            if (lowerCode.StartsWith("de"))
                return "de"; // German
            if (lowerCode.StartsWith("it"))
                return "it"; // Italian
            if (lowerCode.StartsWith("pt"))
                return "pt"; // Portuguese
            if (lowerCode.StartsWith("ru"))
                return "ru"; // Russian
            if (lowerCode.StartsWith("ja"))
                return "ja"; // Japanese
            if (lowerCode.StartsWith("ko"))
                return "ko"; // Korean
            if (lowerCode.StartsWith("zh"))
                return "zh"; // Chinese
            if (lowerCode.StartsWith("ar"))
                return "ar"; // Arabic
            if (lowerCode.StartsWith("hi"))
                return "hi"; // Hindi
                
            return "auto"; // Auto-detect for other languages
        }
        
        /// <summary>
        /// Determines if the transcribed text is likely in Turkish
        /// </summary>
        private static bool IsLikelyTurkish(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
                
            // Common Turkish characters and words
            var turkishIndicators = new[]
            {
                "ç", "ğ", "ı", "ö", "ş", "ü", // Turkish-specific characters
                " bir ", " ve ", " bu ", " o ", " ki ", " da ", " de ", " ile ", " için ", " olan ", " olarak "
            };
            
            var matches = turkishIndicators.Count(indicator => 
                text.ToLowerInvariant().Contains(indicator));
                
            // If we find at least 2 Turkish indicators, it's likely Turkish
            return matches >= 2;
        }
        
        /// <summary>
        /// Gets the model file name for the specified model size
        /// </summary>
        private static string GetModelFileName(WhisperModelSize modelSize)
        {
            return modelSize switch
            {
                WhisperModelSize.Tiny => "ggml-tiny.bin",
                WhisperModelSize.Base => "ggml-base.bin",
                WhisperModelSize.Small => "ggml-small.bin",
                WhisperModelSize.Medium => "ggml-medium.bin",
                WhisperModelSize.Large => "ggml-large-v3.bin",
                _ => "ggml-base.bin"
            };
        }
        
        /// <summary>
        /// Gets the GGML model type for the specified model size
        /// </summary>
        private static GgmlType GetGgmlModelType(WhisperModelSize modelSize)
        {
            return modelSize switch
            {
                WhisperModelSize.Tiny => GgmlType.Tiny,
                WhisperModelSize.Base => GgmlType.Base,
                WhisperModelSize.Small => GgmlType.Small,
                WhisperModelSize.Medium => GgmlType.Medium,
                WhisperModelSize.Large => GgmlType.LargeV3,
                _ => GgmlType.Base
            };
        }
        
        /// <summary>
        /// Formats the transcription result as SRT subtitle format
        /// </summary>
        private static string FormatAsSrt(TranscriptionResult transcriptionResult)
        {
            var srt = new StringBuilder();
            
            for (int i = 0; i < transcriptionResult.Segments.Count; i++)
            {
                var segment = transcriptionResult.Segments[i];
                
                srt.AppendLine((i + 1).ToString());
                srt.AppendLine($"{FormatTimeForSrt(segment.StartTime)} --> {FormatTimeForSrt(segment.EndTime)}");
                srt.AppendLine(segment.Text);
                srt.AppendLine();
            }
            
            return srt.ToString();
        }
        
        /// <summary>
        /// Formats the transcription result as WebVTT format
        /// </summary>
        private static string FormatAsVtt(TranscriptionResult transcriptionResult)
        {
            var vtt = new StringBuilder();
            vtt.AppendLine("WEBVTT");
            vtt.AppendLine();
            
            foreach (var segment in transcriptionResult.Segments)
            {
                vtt.AppendLine($"{FormatTimeForVtt(segment.StartTime)} --> {FormatTimeForVtt(segment.EndTime)}");
                vtt.AppendLine(segment.Text);
                vtt.AppendLine();
            }
            
            return vtt.ToString();
        }
        
        /// <summary>
        /// Formats time in seconds to SRT format (HH:mm:ss,fff)
        /// </summary>
        private static string FormatTimeForSrt(double timeInSeconds)
        {
            var timeSpan = TimeSpan.FromSeconds(timeInSeconds);
            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2},{timeSpan.Milliseconds:D3}";
        }
        
        /// <summary>
        /// Formats time in seconds to WebVTT format (HH:mm:ss.fff)
        /// </summary>
        private static string FormatTimeForVtt(double timeInSeconds)
        {
            var timeSpan = TimeSpan.FromSeconds(timeInSeconds);
            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}";
        }
        
        /// <summary>
        /// Disposes of the Whisper processor
        /// </summary>
        public void Dispose()
        {
            _whisperProcessor?.Dispose();
        }
    }
}
