using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;
// Use alias to avoid ambiguity with Microsoft's SpeechSynthesisResult
using AzureSpeechResult = Microsoft.CognitiveServices.Speech.SpeechSynthesisResult;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Text-to-speech service implementation using Microsoft Azure Cognitive Services
    /// </summary>
    public class AzureTextToSpeechService : ITextToSpeechService
    {
        private readonly TextToSpeechOptions _defaultOptions;
        
        /// <summary>
        /// Initializes a new instance of the Azure Text-to-Speech Service
        /// </summary>
        /// <param name="defaultOptions">Default text-to-speech options</param>
        public AzureTextToSpeechService(TextToSpeechOptions? defaultOptions = null)
        {
            _defaultOptions = defaultOptions ?? new TextToSpeechOptions();
        }
        
        /// <summary>
        /// Converts text to speech using Azure Speech Services
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
            
            // Validate API credentials
            if (string.IsNullOrEmpty(options.ApiKey) || string.IsNullOrEmpty(options.Region))
            {
                throw new ArgumentException("Azure Speech Service API key and region must be provided");
            }
            
            try
            {
                // Create speech config
                var speechConfig = CreateSpeechConfig(options);
                
                // Set up audio output configuration
                string directory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Report initial progress
                progressCallback?.Report(0.1);
                
                // Create audio output configuration
                using var audioConfig = AudioConfig.FromWavFileOutput(outputFilePath);
                
                // Create speech synthesizer
                using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
                
                // Report progress
                progressCallback?.Report(0.2);
                  // Prepare text for synthesis
                AzureSpeechResult? result;
                
                if (options.UseSSML)
                {
                    // Use SSML for advanced control over speech synthesis
                    string ssml = GenerateSSML(text, options);
                    result = await synthesizer.SpeakSsmlAsync(ssml);
                }
                else
                {
                    // Use simple text synthesis
                    result = await synthesizer.SpeakTextAsync(text);
                }
                
                // Check synthesis result
                if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    throw new Exception($"Speech synthesis canceled: {cancellation.Reason}, Error code: {cancellation.ErrorCode}, Error details: {cancellation.ErrorDetails}");
                }
                
                // Report successful completion
                progressCallback?.Report(1.0);
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Speech synthesis failed: {ex.Message}", ex);
            }
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
            if (translationResult == null || string.IsNullOrEmpty(translationResult.TranslatedText))
            {
                throw new ArgumentException("Translation result is empty or null");
            }
            
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            // Validate API credentials
            if (string.IsNullOrEmpty(options.ApiKey) || string.IsNullOrEmpty(options.Region))
            {
                throw new ArgumentException("Azure Speech Service API key and region must be provided");
            }
            
            try
            {
                // Create speech config
                var speechConfig = CreateSpeechConfig(options);
                
                // Set up output directory
                string directory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create audio output configuration
                using var audioConfig = AudioConfig.FromWavFileOutput(outputFilePath);
                
                // Create speech synthesizer
                using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
                
                // Report initial progress
                progressCallback?.Report(0.1);
                  // If the translation has segments with timing, synthesize each segment separately
                if (translationResult.TranslatedSegments != null && translationResult.TranslatedSegments.Count > 0)
                {
                    // TODO: For a complete solution, we would need to:
                    // 1. Synthesize each segment separately
                    // 2. Get timing information from each synthesis
                    // 3. Combine audio segments, adjusting for timing
                    // This complex process would require audio processing libraries
                    
                    // For now, we'll just synthesize the full text
                    var result = await synthesizer.SpeakTextAsync(translationResult.TranslatedText);
                    
                    // Check synthesis result
                    if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                        throw new Exception($"Speech synthesis canceled: {cancellation.Reason}, Error code: {cancellation.ErrorCode}, Error details: {cancellation.ErrorDetails}");
                    }
                    
                    // Report successful completion
                    progressCallback?.Report(1.0);
                }
                else
                {
                    // Synthesize the full text
                    var result = await synthesizer.SpeakTextAsync(translationResult.TranslatedText);
                    
                    // Check synthesis result
                    if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                        throw new Exception($"Speech synthesis canceled: {cancellation.Reason}, Error code: {cancellation.ErrorCode}, Error details: {cancellation.ErrorDetails}");
                    }
                    
                    // Report successful completion
                    progressCallback?.Report(1.0);
                }
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Speech synthesis failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Gets a list of available voices for the specified language
        /// </summary>
        public async Task<string[]> GetAvailableVoicesAsync(
            string languageCode,
            TextToSpeechOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            // Validate API credentials
            if (string.IsNullOrEmpty(options.ApiKey) || string.IsNullOrEmpty(options.Region))
            {
                throw new ArgumentException("Azure Speech Service API key and region must be provided");
            }
            
            try
            {
                // Create speech config
                var speechConfig = SpeechConfig.FromSubscription(options.ApiKey, options.Region);
                
                // Create speech synthesizer (without audio output)
                using var synthesizer = new SpeechSynthesizer(speechConfig, null);
                
                // Get available voices
                var voicesResult = await synthesizer.GetVoicesAsync(languageCode);
                
                if (voicesResult.Reason == ResultReason.VoicesListRetrieved)
                {
                    // Extract voice names from the result
                    return voicesResult.Voices.Select(v => v.Name).ToArray();
                }
                else
                {
                    throw new Exception($"Failed to retrieve voices: {voicesResult.Reason}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get available voices: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Validates the Azure Speech Service API credentials
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync(
            TextToSpeechOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to get voices as a credential validation
                var speechConfig = SpeechConfig.FromSubscription(options.ApiKey, options.Region);
                using var synthesizer = new SpeechSynthesizer(speechConfig, null);
                
                // A simple test call to validate credentials
                var result = await synthesizer.GetVoicesAsync();
                
                return result.Reason == ResultReason.VoicesListRetrieved;
            }
            catch
            {
                return false;
            }
        }
        
        #region Private Helper Methods
        
        /// <summary>
        /// Creates a speech configuration based on the provided options
        /// </summary>
        private SpeechConfig CreateSpeechConfig(TextToSpeechOptions options)
        {
            var config = SpeechConfig.FromSubscription(options.ApiKey, options.Region);
            
            // Set speech synthesis language
            config.SpeechSynthesisLanguage = options.LanguageCode;
            
            // Set voice name if specified
            if (!string.IsNullOrEmpty(options.VoiceName))
            {
                config.SpeechSynthesisVoiceName = options.VoiceName;
            }
            
            // Set other options
            if (options.SamplingRate > 0)
            {
                config.SetSpeechSynthesisOutputFormat(
                    GetOutputFormat(options.OutputFormat, options.SamplingRate));
            }
            
            return config;
        }
        
        /// <summary>
        /// Gets the appropriate speech synthesis output format based on format and sampling rate
        /// </summary>
        private SpeechSynthesisOutputFormat GetOutputFormat(string format, int samplingRate)
        {
            format = format.ToLowerInvariant();
            
            // Choose format based on user preference
            switch (format)
            {
                case "mp3":
                    return samplingRate switch
                    {
                        8000 => SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3,
                        16000 => SpeechSynthesisOutputFormat.Audio16Khz64KBitRateMonoMp3,
                        24000 => SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3,
                        48000 => SpeechSynthesisOutputFormat.Audio48Khz96KBitRateMonoMp3,
                        _ => SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3  // Default
                    };
                      case "ogg":
                    return samplingRate switch
                    {
                        16000 => SpeechSynthesisOutputFormat.Ogg16Khz16BitMonoOpus,
                        24000 => SpeechSynthesisOutputFormat.Ogg24Khz16BitMonoOpus,
                        48000 => SpeechSynthesisOutputFormat.Ogg48Khz16BitMonoOpus,
                        _ => SpeechSynthesisOutputFormat.Ogg24Khz16BitMonoOpus  // Default
                    };
                    
                case "wav":
                default:
                    return samplingRate switch
                    {
                        8000 => SpeechSynthesisOutputFormat.Raw8Khz16BitMonoPcm,
                        16000 => SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm,
                        24000 => SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm,
                        48000 => SpeechSynthesisOutputFormat.Raw48Khz16BitMonoPcm,
                        _ => SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm  // Default
                    };
            }
        }
        
        /// <summary>
        /// Generates SSML markup for advanced text-to-speech control
        /// </summary>
        private string GenerateSSML(string text, TextToSpeechOptions options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"" + options.LanguageCode + "\">");
            
            // Add voice element if voice name is specified
            if (!string.IsNullOrEmpty(options.VoiceName))
            {
                sb.AppendLine($"<voice name=\"{options.VoiceName}\">");
            }
            
            // Add prosody element for rate control
            if (options.SpeakingRate != 1.0f)
            {
                sb.AppendLine($"<prosody rate=\"{options.SpeakingRate}\">");
            }
            
            // Add prosody element for pitch control
            if (options.PitchAdjustment != 0)
            {
                string pitchValue = $"{(options.PitchAdjustment > 0 ? "+" : "")}{options.PitchAdjustment}%";
                sb.AppendLine($"<prosody pitch=\"{pitchValue}\">");
            }
            
            // Add mstts:express-as element for speaking style (neural voices only)
            if (!string.IsNullOrEmpty(options.SpeakingStyle) && options.SpeakingStyle != "general")
            {
                sb.AppendLine($"<mstts:express-as style=\"{options.SpeakingStyle}\">");
            }
            
            // Add the text content
            sb.AppendLine(text);
            
            // Close tags in reverse order
            if (!string.IsNullOrEmpty(options.SpeakingStyle) && options.SpeakingStyle != "general")
            {
                sb.AppendLine("</mstts:express-as>");
            }
            
            if (options.PitchAdjustment != 0)
            {
                sb.AppendLine("</prosody>");
            }
            
            if (options.SpeakingRate != 1.0f)
            {
                sb.AppendLine("</prosody>");
            }
            
            if (!string.IsNullOrEmpty(options.VoiceName))
            {
                sb.AppendLine("</voice>");
            }
            
            sb.AppendLine("</speak>");
            return sb.ToString();
        }
        
        #endregion
    }
}
