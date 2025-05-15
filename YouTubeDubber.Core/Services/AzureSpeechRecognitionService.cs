using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Speech recognition service implementation using Microsoft Azure Cognitive Services
    /// </summary>
    public class AzureSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly SpeechRecognitionOptions _defaultOptions;
        
        /// <summary>
        /// Initializes a new instance of the Azure Speech Recognition Service
        /// </summary>
        /// <param name="defaultOptions">Default speech recognition options</param>
        public AzureSpeechRecognitionService(SpeechRecognitionOptions? defaultOptions = null)
        {
            _defaultOptions = defaultOptions ?? new SpeechRecognitionOptions();
        }
        
        /// <summary>
        /// Transcribes the audio file to text using Azure Speech Services
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
            
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            // Validate API credentials before proceeding
            if (string.IsNullOrEmpty(options.ApiKey) || string.IsNullOrEmpty(options.Region))
            {
                throw new ArgumentException("Azure Speech Service API key and region must be provided");
            }
            
            var transcriptionResult = new TranscriptionResult
            {
                AudioFilePath = audioFilePath,
                LanguageCode = options.LanguageCode
            };
            
            // Get audio file information to calculate progress
            var fileInfo = new FileInfo(audioFilePath);
            var fileDuration = await GetAudioDurationInSecondsAsync(audioFilePath);
            
            try
            {
                if (options.UseContinuousRecognition && fileDuration > options.MaxAudioChunkDuration)
                {
                    // For longer audio, use continuous recognition
                    await ProcessLongAudioFileAsync(
                        audioFilePath,
                        options,
                        transcriptionResult,
                        fileDuration,
                        progressCallback,
                        cancellationToken);
                }
                else
                {
                    // For shorter audio, use standard recognition
                    await ProcessShortAudioFileAsync(
                        audioFilePath,
                        options,
                        transcriptionResult,
                        progressCallback,
                        cancellationToken);
                }
                
                return transcriptionResult;
            }
            catch (Exception ex)
            {
                throw new Exception($"Speech recognition failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Validates the Azure Speech Service API credentials
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync(
            SpeechRecognitionOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var config = SpeechConfig.FromSubscription(options.ApiKey, options.Region);
                
                // Create a recognizer with the configuration to test credentials
                using var recognizer = new SpeechRecognizer(config);
                
                // Try a simple recognition operation
                var result = await recognizer.RecognizeOnceAsync();
                
                // If the status is anything but Canceled with an authentication error,
                // consider the credentials valid
                return result.Reason != ResultReason.Canceled ||
                       !CancellationDetails.FromResult(result).ErrorCode.Equals(
                           CancellationErrorCode.AuthenticationFailure);
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
                    content = JsonConvert.SerializeObject(transcriptionResult, Formatting.Indented);
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }
            
            string directoryPath = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            await File.WriteAllTextAsync(outputFilePath, content);
            return outputFilePath;
        }
        
        #region Private Helper Methods
        
        /// <summary>
        /// Processes a short audio file (less than MaxAudioChunkDuration) for transcription
        /// </summary>
        private async Task ProcessShortAudioFileAsync(
            string audioFilePath,
            SpeechRecognitionOptions options,
            TranscriptionResult transcriptionResult,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Configure the speech recognizer
            var config = CreateSpeechConfig(options);
            
            // Set up audio configuration
            using var audioInput = AudioConfig.FromWavFileInput(audioFilePath);
            
            // Create a speech recognizer
            using var recognizer = new SpeechRecognizer(config, audioInput);
            
            // Register event handlers
            double totalDuration = 0;
            double processedDuration = 0;
            int segmentIndex = 0;
            
            // Subscribe to events
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
                {
                    var segment = CreateTranscriptionSegment(e.Result, segmentIndex++);
                    lock (transcriptionResult)
                    {
                        transcriptionResult.Segments.Add(segment);
                    }
                    
                    processedDuration += segment.EndTime - segment.StartTime;
                    progressCallback?.Report(Math.Min(1.0, processedDuration / totalDuration));
                }
            };
            
            // Start recognition
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            
            // Wait for completion or cancellation
            var completionSource = new TaskCompletionSource<int>();
            using (cancellationToken.Register(() => completionSource.TrySetCanceled()))
            {
                recognizer.SessionStopped += (s, e) => completionSource.TrySetResult(0);
                
                // Wait for the recognition to complete
                await completionSource.Task.ConfigureAwait(false);
            }
            
            // Stop recognition
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            
            progressCallback?.Report(1.0); // Ensure we show 100% completion
        }
        
        /// <summary>
        /// Processes a long audio file by splitting it into chunks for transcription
        /// </summary>
        private async Task ProcessLongAudioFileAsync(
            string audioFilePath,
            SpeechRecognitionOptions options,
            TranscriptionResult transcriptionResult,
            double fileDuration,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // For simplicity, we're assuming we can process the entire file at once
            // In a production environment, you would need to split the audio file into chunks
            // and process each chunk separately
            
            // Configure the speech recognizer
            var config = CreateSpeechConfig(options);
            
            // Set up audio configuration
            using var audioInput = AudioConfig.FromWavFileInput(audioFilePath);
            
            // Create a speech recognizer
            using var recognizer = new SpeechRecognizer(config, audioInput);
            
            // Track progress
            double processedDuration = 0;
            int segmentIndex = 0;
            
            // Subscribe to recognition events
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
                {
                    var segment = CreateTranscriptionSegment(e.Result, segmentIndex++);
                    lock (transcriptionResult)
                    {
                        transcriptionResult.Segments.Add(segment);
                    }
                    
                    processedDuration += segment.EndTime - segment.StartTime;
                    progressCallback?.Report(Math.Min(1.0, processedDuration / fileDuration));
                }
            };
            
            // Start continuous recognition
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            
            // Wait for completion or cancellation
            var completionSource = new TaskCompletionSource<int>();
            using (cancellationToken.Register(() => completionSource.TrySetCanceled()))
            {
                recognizer.SessionStopped += (s, e) => completionSource.TrySetResult(0);
                
                // Wait for recognition to complete
                await completionSource.Task.ConfigureAwait(false);
            }
            
            // Stop recognition
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            
            // Ensure we show 100% completion
            progressCallback?.Report(1.0);
        }
        
        /// <summary>
        /// Creates a speech configuration based on the provided options
        /// </summary>
        private SpeechConfig CreateSpeechConfig(SpeechRecognitionOptions options)
        {
            var config = SpeechConfig.FromSubscription(options.ApiKey, options.Region);
            
            // Set the recognition language
            config.SpeechRecognitionLanguage = options.LanguageCode;
            
            // Configure for detailed output with timing information
            config.OutputFormat = OutputFormat.Detailed;
            
            // Configure additional options
            if (options.FilterProfanity)
            {
                config.SetProfanity(ProfanityOption.Masked);
            }
            
            // Enable word-level timestamps if requested
            if (options.EnableWordLevelTimestamps)
            {
                config.RequestWordLevelTimestamps();
            }
            
            return config;
        }
        
        /// <summary>
        /// Creates a transcription segment from a recognition result
        /// </summary>
        private TranscriptionSegment CreateTranscriptionSegment(SpeechRecognitionResult result, int segmentIndex)
        {
            double startTime = 0;
            double endTime = 0;
            double confidence = 0;
            
            try
            {
                // Get detailed timing information if available
                var resultJson = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    // Parse JSON to get detailed timing information
                    var jsonObj = JsonConvert.DeserializeObject<dynamic>(resultJson);
                    if (jsonObj != null)
                    {
                        var nbest = jsonObj.NBest[0];
                        startTime = Convert.ToDouble(nbest.Offset) / 10000000.0; // Convert from 100-nanosecond units to seconds
                        endTime = startTime + Convert.ToDouble(nbest.Duration) / 10000000.0;
                        confidence = Convert.ToDouble(nbest.Confidence);
                    }
                }
            }
            catch
            {
                // Fallback if we couldn't get detailed timing
                startTime = segmentIndex * 2.0; // Rough estimate if timing info not available
                endTime = startTime + 2.0;
                confidence = 0.8; // Default confidence
            }
            
            return new TranscriptionSegment
            {
                Text = result.Text,
                StartTime = startTime,
                EndTime = endTime,
                Confidence = confidence
            };
        }
        
        /// <summary>
        /// Estimates the duration of an audio file in seconds
        /// </summary>
        private async Task<double> GetAudioDurationInSecondsAsync(string audioFilePath)
        {
            // In a real implementation, you would use a library like NAudio to get the actual duration.
            // For simplicity, we'll use the file size as a very rough approximation.
            var fileInfo = new FileInfo(audioFilePath);
            
            // Rough estimate: WAV files with 16-bit PCM, 16kHz mono are about 32KB per second
            // This is a very rough approximation
            return fileInfo.Length / (32 * 1024);
        }
        
        /// <summary>
        /// Formats a transcription result as an SRT subtitle file
        /// </summary>
        private string FormatAsSrt(TranscriptionResult transcriptionResult)
        {
            var srtBuilder = new StringBuilder();
            
            for (int i = 0; i < transcriptionResult.Segments.Count; i++)
            {
                var segment = transcriptionResult.Segments[i];
                
                // Add subtitle number
                srtBuilder.AppendLine($"{i + 1}");
                
                // Add timestamp (format: 00:00:00,000 --> 00:00:00,000)
                string startTime = FormatTimeSpan(TimeSpan.FromSeconds(segment.StartTime));
                string endTime = FormatTimeSpan(TimeSpan.FromSeconds(segment.EndTime));
                srtBuilder.AppendLine($"{startTime} --> {endTime}");
                
                // Add text
                srtBuilder.AppendLine(segment.Text);
                
                // Add blank line between entries
                srtBuilder.AppendLine();
            }
            
            return srtBuilder.ToString();
        }
        
        /// <summary>
        /// Formats a TimeSpan as an SRT timestamp (00:00:00,000)
        /// </summary>
        private string FormatTimeSpan(TimeSpan time)
        {
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";
        }
        
        #endregion
    }
}
