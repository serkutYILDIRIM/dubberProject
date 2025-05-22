using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.ML;
using TorchSharp;
using TorchSharp.Modules;
using YouTubeDubber.Core.Helpers;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Text-to-speech service implementation using Silero TTS models
    /// </summary>
    public class SileroTextToSpeechService : ITextToSpeechService, IDisposable
    {
        private readonly TextToSpeechOptions _defaultOptions;
        private readonly string _modelsDirectory;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, Module<Tensor, Tensor>> _loadedModels;
        private bool _disposed;
        
        // URL for Silero Turkish model repository
        private const string SILERO_MODELS_REPO = "https://api.github.com/repos/snakers4/silero-models/contents/";
        private const string SILERO_RAW_CONTENT = "https://raw.githubusercontent.com/snakers4/silero-models/master/";
        
        // Available Turkish voice models
        public static class TurkishVoices
        {
            public const string MALE_VOICE_1 = "tr_male1";
            public const string MALE_VOICE_2 = "tr_male2";
            public const string FEMALE_VOICE_1 = "tr_female1";
            public const string FEMALE_VOICE_2 = "tr_female2";
        }

        /// <summary>
        /// Initializes a new instance of the Silero Text-to-Speech Service
        /// </summary>
        /// <param name="defaultOptions">Default text-to-speech options</param>
        /// <param name="modelsDirectory">Directory to store downloaded models</param>
        public SileroTextToSpeechService(TextToSpeechOptions? defaultOptions = null, string? modelsDirectory = null)
        {
            _defaultOptions = defaultOptions ?? new TextToSpeechOptions 
            { 
                VoiceName = TurkishVoices.FEMALE_VOICE_1,
                LanguageCode = "tr-TR",
                SamplingRate = 24000,
                OutputFormat = "wav"
            };
            
            _modelsDirectory = modelsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YouTubeDubber", "SileroModels");
                
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "YouTubeDubber Application");
            
            _loadedModels = new Dictionary<string, Module<Tensor, Tensor>>();
            
            // Ensure models directory exists
            Directory.CreateDirectory(_modelsDirectory);
        }

        /// <summary>
        /// Converts text to speech using Silero models
        /// </summary>
        public async Task<string> SynthesizeTextToSpeechAsync(
            string text,
            string outputFilePath,
            TextToSpeechOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Text to synthesize cannot be empty", nameof(text));
            }
            
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            try
            {
                // Report initial progress
                progressCallback?.Report(0.1);
                
                // Ensure output directory exists
                string directory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Load the model for the selected voice
                string voiceName = !string.IsNullOrEmpty(options.VoiceName) ? options.VoiceName : TurkishVoices.FEMALE_VOICE_1;
                var model = await GetOrLoadModelAsync(voiceName, progressCallback, cancellationToken);
                
                // Report progress after model loading
                progressCallback?.Report(0.4);
                
                // Process text (handle SSML if needed)
                string processedText;
                if (options.UseSSML)
                {
                    processedText = ExtractTextFromSSML(text);
                }
                else
                {
                    processedText = TurkishVoiceHelper.FormatTextForTurkishSpeech(text);
                }
                
                // Split text into sentences for better processing
                var sentences = SplitIntoSentences(processedText);
                
                // Generate audio for each sentence
                var waveformsList = new List<float[]>();
                int sentenceCount = sentences.Count;
                
                for (int i = 0; i < sentenceCount; i++)
                {
                    var sentence = sentences[i];
                    if (string.IsNullOrWhiteSpace(sentence)) continue;
                    
                    // Generate waveform for this sentence
                    var waveform = GenerateWaveform(model, sentence, options);
                    waveformsList.Add(waveform);
                    
                    // Report progress
                    double sentenceProgress = (double)(i + 1) / sentenceCount;
                    progressCallback?.Report(0.4 + (sentenceProgress * 0.5));
                }
                
                // Combine all sentence waveforms
                var combinedWaveform = CombineWaveforms(waveformsList);
                
                // Save the audio to a file
                await SaveWaveformToFileAsync(combinedWaveform, outputFilePath, options);
                
                // Report completion
                progressCallback?.Report(1.0);
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Silero speech synthesis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Specialized method for synthesizing Turkish text to speech with enhanced naturalness
        /// </summary>
        public async Task<string> SynthesizeTurkishSpeechAsync(
            string text,
            string outputFilePath,
            TextToSpeechOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Apply specific Turkish text formatting
            string formattedText = TurkishVoiceHelper.FormatTextForTurkishSpeech(text);
            
            // Set options specifically for Turkish
            options ??= _defaultOptions;
            options.LanguageCode = "tr-TR";
            
            // For enhanced Turkish, we'll add SSML markers for better prosody
            if (options.UseSSML)
            {
                formattedText = GenerateTurkishSSML(formattedText, options);
            }
            
            return await SynthesizeTextToSpeechAsync(
                formattedText,
                outputFilePath,
                options,
                progressCallback,
                cancellationToken);
        }

        /// <summary>
        /// Synthesizes speech from a translation result, preserving timing information
        /// </summary>
        public async Task<string> SynthesizeTranslationAsync(
            TranslationResult translationResult,
            string outputFilePath,
            TextToSpeechOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (translationResult == null || translationResult.Count == 0)
            {
                throw new ArgumentException("Translation result is empty or null", nameof(translationResult));
            }
            
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            try
            {
                // Report initial progress
                progressCallback?.Report(0.1);
                
                // Ensure output directory exists
                string directory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Load the model for the selected voice
                string voiceName = !string.IsNullOrEmpty(options.VoiceName) ? options.VoiceName : TurkishVoices.FEMALE_VOICE_1;
                var model = await GetOrLoadModelAsync(voiceName, progressCallback, cancellationToken);
                
                // Report progress after model loading
                progressCallback?.Report(0.3);
                
                // Create a list of audio segments
                var segments = new List<(float[] Audio, double StartTime, double EndTime)>();
                int totalSegments = translationResult.TranslatedSegments?.Count ?? 0;
                
                // Process each segment
                for (int i = 0; i < totalSegments; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    var segment = translationResult.TranslatedSegments![i];
                    string text = segment.TranslatedText;
                    
                    if (string.IsNullOrWhiteSpace(text))
                        continue;
                        
                    // Process text (handle SSML if needed)
                    if (options.UseSSML)
                    {
                        text = ExtractTextFromSSML(text);
                    }
                    
                    // Generate waveform for this segment
                    var waveform = GenerateWaveform(model, text, options);
                    
                    // Add to segments list
                    segments.Add((waveform, segment.StartTime, segment.EndTime));
                    
                    // Report progress
                    double segmentProgress = (double)(i + 1) / totalSegments;
                    progressCallback?.Report(0.3 + (segmentProgress * 0.6));
                }
                
                // Create a single audio file with all segments
                await CreateSegmentedAudioFileAsync(segments, outputFilePath, options);
                
                // Report completion
                progressCallback?.Report(1.0);
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Silero translation synthesis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a list of available Turkish voices
        /// </summary>
        public async Task<string[]> GetAvailableVoicesAsync(
            string languageCode,
            TextToSpeechOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // For Silero, we have a fixed set of Turkish voices
            if (languageCode.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    TurkishVoices.MALE_VOICE_1,
                    TurkishVoices.MALE_VOICE_2,
                    TurkishVoices.FEMALE_VOICE_1,
                    TurkishVoices.FEMALE_VOICE_2
                };
            }
            
            // Currently, we only support Turkish voices
            return Array.Empty<string>();
        }

        /// <summary>
        /// Validates service credentials (always returns true for local Silero models)
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync(
            TextToSpeechOptions options,
            CancellationToken cancellationToken = default)
        {
            // No API credentials needed for Silero as it runs locally
            // Instead, we'll check if we can download/access the models
            try
            {
                var response = await _httpClient.GetAsync(SILERO_MODELS_REPO, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        #region Private Helper Methods
        
        /// <summary>
        /// Loads a Silero TTS model for the specified voice
        /// </summary>
        private async Task<Module<Tensor, Tensor>> GetOrLoadModelAsync(
            string voiceName,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Check if model is already loaded
            if (_loadedModels.TryGetValue(voiceName, out var loadedModel))
            {
                return loadedModel;
            }
            
            // Download model if not available locally
            string modelPath = Path.Combine(_modelsDirectory, $"silero_{voiceName}.pt");
            if (!File.Exists(modelPath))
            {
                await DownloadModelAsync(voiceName, modelPath, progressCallback, cancellationToken);
            }
            
            // Load the model using TorchSharp
            try
            {
                var model = Module<Tensor, Tensor>.Load(modelPath);
                _loadedModels[voiceName] = model;
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load Silero model: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Downloads a Silero TTS model from the repository
        /// </summary>
        private async Task DownloadModelAsync(
            string voiceName,
            string destinationPath,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            progressCallback?.Report(0.1);
            
            try
            {
                // Model URL based on voice name
                string modelUrl = $"{SILERO_RAW_CONTENT}models/tr/v3_{voiceName}.pt";
                
                // Download the model
                using var response = await _httpClient.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                
                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                var bytesRead = 0;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    
                    totalBytesRead += bytesRead;
                    if (totalBytes > 0)
                    {
                        var progressPercentage = (double)totalBytesRead / totalBytes;
                        progressCallback?.Report(0.1 + (progressPercentage * 0.2)); // 10-30% progress during download
                    }
                }
                
                progressCallback?.Report(0.3); // 30% progress after download
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download Silero model: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Generates a waveform for the given text using a Silero model
        /// </summary>
        private float[] GenerateWaveform(
            Module<Tensor, Tensor> model,
            string text,
            TextToSpeechOptions options)
        {
            try
            {
                // Create input tensor with the text
                using var textTensor = torch.tensor(new[] { text });
                
                // Apply speaking rate
                float speakingRate = Math.Max(0.5f, Math.Min(2.0f, options.SpeakingRate));
                
                // Run inference
                using var waveformTensor = model.forward(textTensor);
                
                // Convert to float array
                float[] waveform = waveformTensor.data<float>().ToArray();
                
                // Apply pitch adjustment if specified
                if (options.PitchAdjustment != 0)
                {
                    waveform = AdjustPitch(waveform, options.PitchAdjustment, options.SamplingRate);
                }
                
                // Apply speaking rate adjustment
                if (Math.Abs(speakingRate - 1.0f) > 0.01f)
                {
                    waveform = AdjustSpeakingRate(waveform, speakingRate);
                }
                
                return waveform;
            }
            catch (Exception ex)
            {
                throw new Exception($"Speech synthesis failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Saves a waveform to an audio file
        /// </summary>
        private async Task SaveWaveformToFileAsync(float[] waveform, string filePath, TextToSpeechOptions options)
        {
            if (waveform == null || waveform.Length == 0)
                throw new ArgumentException("Waveform cannot be empty");
                
            int sampleRate = options.SamplingRate;
            
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fileStream);
            
            // Write WAV header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + waveform.Length * 2); // File size - 8
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            
            // Format chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // Audio format (PCM)
            writer.Write((short)1); // Number of channels
            writer.Write(sampleRate); // Sample rate
            writer.Write(sampleRate * 2); // Byte rate
            writer.Write((short)2); // Block align
            writer.Write((short)16); // Bits per sample
            
            // Data chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(waveform.Length * 2); // Chunk size
            
            // Write waveform data (convert float to Int16)
            foreach (var sample in waveform)
            {
                // Convert float from [-1,1] to Int16 range
                short pcmValue = (short)(sample * short.MaxValue);
                writer.Write(pcmValue);
            }
            
            await fileStream.FlushAsync();
        }
        
        /// <summary>
        /// Creates an audio file from segmented speech with proper timing
        /// </summary>
        private async Task CreateSegmentedAudioFileAsync(
            List<(float[] Audio, double StartTime, double EndTime)> segments,
            string filePath,
            TextToSpeechOptions options)
        {
            if (segments.Count == 0)
                return;
                
            int sampleRate = options.SamplingRate;
            double totalDuration = segments.Max(s => s.EndTime);
            int totalSamples = (int)(totalDuration * sampleRate);
            
            // Create buffer with silence
            var finalWaveform = new float[totalSamples];
            
            // Place each segment at its correct time position
            foreach (var segment in segments)
            {
                int startSample = (int)(segment.StartTime * sampleRate);
                
                // Ensure we don't go out of bounds
                int samplesToAdd = Math.Min(segment.Audio.Length, totalSamples - startSample);
                if (samplesToAdd <= 0) continue;
                
                // Copy segment to the right position
                Array.Copy(segment.Audio, 0, finalWaveform, startSample, samplesToAdd);
            }
            
            // Save the combined waveform
            await SaveWaveformToFileAsync(finalWaveform, filePath, options);
        }
        
        /// <summary>
        /// Extracts plain text from SSML
        /// </summary>
        private string ExtractTextFromSSML(string ssml)
        {
            try
            {
                // Basic parsing to extract text from SSML
                ssml = Regex.Replace(ssml, "<[^>]+>", " ");
                ssml = Regex.Replace(ssml, @"\s+", " ").Trim();
                return ssml;
            }
            catch
            {
                // If parsing fails, return the original text
                return ssml;
            }
        }
        
        /// <summary>
        /// Generates SSML for Turkish speech with enhanced expressiveness
        /// </summary>
        private string GenerateTurkishSSML(string text, TextToSpeechOptions options)
        {
            // Create basic SSML structure
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<speak version=\"1.1\" xmlns=\"http://www.w3.org/2001/10/synthesis\">");
            
            // Add global prosody settings
            float rate = Math.Max(0.5f, Math.Min(2.0f, options.SpeakingRate));
            int pitch = Math.Max(-50, Math.Min(50, options.PitchAdjustment));
            
            sb.AppendLine($"<prosody rate=\"{rate}\" pitch=\"{(pitch >= 0 ? "+" : "")}{pitch}%\">");
            
            // Process text with Turkish-specific enhancements
            var sentences = SplitIntoSentences(text);
            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;
                    
                // Add sentence with voice-specific tuning for Turkish
                sb.AppendLine($"<s>{sentence}</s>");
            }
            
            sb.AppendLine("</prosody>");
            sb.AppendLine("</speak>");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Split text into sentences for better speech synthesis
        /// </summary>
        private List<string> SplitIntoSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();
                
            // Split text at sentence boundaries
            return Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();
        }
        
        /// <summary>
        /// Combines multiple waveforms into one
        /// </summary>
        private float[] CombineWaveforms(List<float[]> waveforms)
        {
            if (waveforms.Count == 0)
                return Array.Empty<float>();
                
            if (waveforms.Count == 1)
                return waveforms[0];
                
            // Calculate total length
            int totalLength = waveforms.Sum(w => w.Length);
            var result = new float[totalLength];
            
            // Copy each waveform into the result
            int currentPosition = 0;
            foreach (var waveform in waveforms)
            {
                Array.Copy(waveform, 0, result, currentPosition, waveform.Length);
                currentPosition += waveform.Length;
            }
            
            return result;
        }
        
        /// <summary>
        /// Adjusts pitch of audio data
        /// </summary>
        private float[] AdjustPitch(float[] waveform, int pitchAdjustment, int sampleRate)
        {
            // This is a simplified implementation
            // A real implementation would use a signal processing library
            
            // For now, return the original waveform
            return waveform;
        }
        
        /// <summary>
        /// Adjusts speaking rate of audio data
        /// </summary>
        private float[] AdjustSpeakingRate(float[] waveform, float rate)
        {
            // This is a simplified implementation
            // A real implementation would use a time stretching algorithm
            
            if (Math.Abs(rate - 1.0f) < 0.01f)
                return waveform;
                
            // Simple linear interpolation for rate adjustment
            int originalLength = waveform.Length;
            int newLength = (int)(originalLength / rate);
            var result = new float[newLength];
            
            for (int i = 0; i < newLength; i++)
            {
                float position = i * rate;
                int index = (int)position;
                float fraction = position - index;
                
                if (index < originalLength - 1)
                {
                    result[i] = waveform[index] * (1 - fraction) + waveform[index + 1] * fraction;
                }
                else if (index < originalLength)
                {
                    result[i] = waveform[index];
                }
            }
            
            return result;
        }
        
        #endregion
        
        /// <summary>
        /// Disposes the service and releases resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            foreach (var model in _loadedModels.Values)
            {
                model.Dispose();
            }
            
            _loadedModels.Clear();
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
