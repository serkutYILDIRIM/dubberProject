using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Translation service implementation using Azure Translator
    /// </summary>
    public class AzureTranslationService : ITranslationService
    {
        private readonly TranslationOptions _defaultOptions;
        private readonly HttpClient _httpClient;
        
        /// <summary>
        /// Initializes a new instance of the Azure Translation Service
        /// </summary>
        /// <param name="defaultOptions">Default translation options</param>
        public AzureTranslationService(TranslationOptions? defaultOptions = null)
        {
            _defaultOptions = defaultOptions ?? new TranslationOptions();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        /// <summary>
        /// Translates a single text string
        /// </summary>
        public async Task<TranslationResult> TranslateTextAsync(
            string text,
            TranslationOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            try
            {
                // Report initial progress
                progressCallback?.Report(0.1);
                
                // Create request body
                var requestBody = new[] {
                    new { Text = text }
                };
                
                var requestJson = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
                // Setup request
                var route = $"/translate?api-version=3.0&from={options.SourceLanguage}&to={options.TargetLanguage}";
                
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, route);
                requestMessage.Content = content;
                requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
                requestMessage.Headers.Add("Ocp-Apim-Subscription-Region", options.Region);
                
                // Report progress
                progressCallback?.Report(0.3);
                
                // Send request
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                // Report progress
                progressCallback?.Report(0.7);
                
                // Parse response
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var translations = JsonConvert.DeserializeObject<List<dynamic>>(responseJson);
                
                if (translations == null || translations.Count == 0)
                {
                    throw new InvalidOperationException("No translation result was returned");
                }
                
                // Extract translated text
                var translatedText = translations[0].translations[0].text.ToString();
                
                // Report completion
                progressCallback?.Report(1.0);
                
                // Create result
                return new TranslationResult
                {
                    SourceText = text,
                    TranslatedText = translatedText,
                    SourceLanguage = options.SourceLanguage,
                    TargetLanguage = options.TargetLanguage,
                    TranslationTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Translation failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Translates a transcription result, preserving timing information
        /// </summary>
        public async Task<TranslationResult> TranslateTranscriptionAsync(
            TranscriptionResult transcriptionResult,
            TranslationOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            try
            {
                // Extract all segments
                var segmentTexts = transcriptionResult.Segments.Select(s => s.Text).ToList();
                
                // Report initial progress
                progressCallback?.Report(0.1);
                
                // Create request body
                var requestBody = segmentTexts.Select(text => new { Text = text }).ToArray();
                
                var requestJson = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
                // Setup request
                var route = $"/translate?api-version=3.0&from={options.SourceLanguage}&to={options.TargetLanguage}";
                
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, route);
                requestMessage.Content = content;
                requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
                requestMessage.Headers.Add("Ocp-Apim-Subscription-Region", options.Region);
                
                // Report progress
                progressCallback?.Report(0.3);
                
                // Send request
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                // Report progress
                progressCallback?.Report(0.6);
                
                // Parse response
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var translations = JsonConvert.DeserializeObject<List<dynamic>>(responseJson);
                
                if (translations == null || translations.Count == 0)
                {
                    throw new InvalidOperationException("No translation result was returned");
                }
                
                // Create translated segments with preserved timing information
                var translatedSegments = new List<TranslatedSegment>();
                
                for (int i = 0; i < Math.Min(transcriptionResult.Segments.Count, translations.Count); i++)
                {
                    var originalSegment = transcriptionResult.Segments[i];
                    var translatedText = translations[i].translations[0].text.ToString();
                    
                    translatedSegments.Add(new TranslatedSegment
                    {
                        SourceText = originalSegment.Text,
                        TranslatedText = translatedText,
                        StartTime = originalSegment.StartTime,
                        EndTime = originalSegment.EndTime,
                        SourceConfidence = originalSegment.Confidence
                    });
                    
                    // Report segment progress
                    double segmentProgress = 0.6 + (0.4 * (i + 1) / transcriptionResult.Segments.Count);
                    progressCallback?.Report(segmentProgress);
                }
                
                // Create and return the translation result
                return new TranslationResult
                {
                    SourceText = transcriptionResult.FullText,
                    TranslatedText = string.Join(" ", translatedSegments.Select(s => s.TranslatedText)),
                    SourceLanguage = options.SourceLanguage,
                    TargetLanguage = options.TargetLanguage,
                    TranslationTime = DateTime.Now,
                    TranslatedSegments = translatedSegments
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Transcription translation failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Validates the Azure Translator API credentials
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync(
            TranslationOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try a simple translation to validate credentials
                var requestBody = new[] { new { Text = "Hello" } };
                
                var requestJson = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
                // Setup request
                var route = $"/translate?api-version=3.0&from={options.SourceLanguage}&to={options.TargetLanguage}";
                
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, route);
                requestMessage.Content = content;
                requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
                requestMessage.Headers.Add("Ocp-Apim-Subscription-Region", options.Region);
                
                // Send request
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                // If we get here without exception, credentials are valid
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets available language pairs for translation
        /// </summary>
        public async Task<Dictionary<string, string>> GetAvailableLanguagesAsync(
            TranslationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            try
            {
                // Setup request
                var route = "/languages?api-version=3.0&scope=translation";
                
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, route);
                requestMessage.Headers.Add("Accept-Language", "en");
                
                // Send request
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                // Parse response
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObj = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(responseJson);
                
                if (responseObj == null || !responseObj.ContainsKey("translation"))
                {
                    throw new InvalidOperationException("Invalid languages response");
                }
                
                // Extract language information
                var languages = responseObj["translation"];
                var result = new Dictionary<string, string>();
                
                foreach (var language in languages)
                {
                    if (language.Value.ContainsKey("name"))
                    {
                        result.Add(language.Key, language.Value["name"]);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get available languages: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Saves the translation result to a file in the specified format
        /// </summary>
        public async Task<string> SaveTranslationAsync(
            TranslationResult translationResult,
            string outputFilePath,
            string format = "txt")
        {
            if (translationResult == null || string.IsNullOrEmpty(translationResult.TranslatedText))
            {
                throw new ArgumentException("Translation result is empty or null");
            }
            
            string content;
            
            switch (format.ToLower())
            {
                case "txt":
                    content = translationResult.TranslatedText;
                    break;
                    
                case "srt":
                    content = FormatAsSrt(translationResult);
                    break;
                    
                case "json":
                    content = JsonConvert.SerializeObject(translationResult, Newtonsoft.Json.Formatting.Indented);
                    break;
                    
                case "bilingual":
                    content = FormatBilingual(translationResult);
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }
            
            // Create directory if needed
            string directoryPath = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            // Write the file
            await File.WriteAllTextAsync(outputFilePath, content, cancellationToken: CancellationToken.None);
            
            return outputFilePath;
        }
        
        #region Private Helper Methods
        
        /// <summary>
        /// Formats the translation result as an SRT subtitle file
        /// </summary>
        private string FormatAsSrt(TranslationResult translationResult)
        {
            if (translationResult.TranslatedSegments == null || translationResult.TranslatedSegments.Count == 0)
            {
                return translationResult.TranslatedText;
            }
            
            var srtBuilder = new StringBuilder();
            
            for (int i = 0; i < translationResult.TranslatedSegments.Count; i++)
            {
                var segment = translationResult.TranslatedSegments[i];
                
                // Add subtitle number
                srtBuilder.AppendLine($"{i + 1}");
                
                // Add timestamp (format: 00:00:00,000 --> 00:00:00,000)
                string startTime = FormatTimeSpan(TimeSpan.FromSeconds(segment.StartTime));
                string endTime = FormatTimeSpan(TimeSpan.FromSeconds(segment.EndTime));
                srtBuilder.AppendLine($"{startTime} --> {endTime}");
                
                // Add text
                srtBuilder.AppendLine(segment.TranslatedText);
                
                // Add blank line between entries
                srtBuilder.AppendLine();
            }
            
            return srtBuilder.ToString();
        }
        
        /// <summary>
        /// Formats the translation result as a bilingual text file with both source and target languages
        /// </summary>
        private string FormatBilingual(TranslationResult translationResult)
        {
            if (translationResult.TranslatedSegments == null || translationResult.TranslatedSegments.Count == 0)
            {
                return $"{translationResult.SourceText}\n\n{translationResult.TranslatedText}";
            }
            
            var bilingualBuilder = new StringBuilder();
            
            bilingualBuilder.AppendLine($"Source Language: {translationResult.SourceLanguage}");
            bilingualBuilder.AppendLine($"Target Language: {translationResult.TargetLanguage}");
            bilingualBuilder.AppendLine($"Translation Time: {translationResult.TranslationTime}");
            bilingualBuilder.AppendLine();
            
            for (int i = 0; i < translationResult.TranslatedSegments.Count; i++)
            {
                var segment = translationResult.TranslatedSegments[i];
                
                bilingualBuilder.AppendLine($"[{TimeSpan.FromSeconds(segment.StartTime):hh\\:mm\\:ss} - {TimeSpan.FromSeconds(segment.EndTime):hh\\:mm\\:ss}]");
                bilingualBuilder.AppendLine($"SOURCE: {segment.SourceText}");
                bilingualBuilder.AppendLine($"TARGET: {segment.TranslatedText}");
                bilingualBuilder.AppendLine();
            }
            
            return bilingualBuilder.ToString();
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
