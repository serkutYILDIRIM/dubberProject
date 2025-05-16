using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YouTubeDubber.Core.Helpers;
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
            CancellationToken cancellationToken = default)
        {
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
              try
            {
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
                
                // Send request
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                // Parse response
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var translations = JsonConvert.DeserializeObject<List<dynamic>>(responseJson);
                
                if (translations == null || translations.Count == 0)
                {
                    throw new InvalidOperationException("No translation result was returned");
                }
                
                // Extract translated text
                var translatedText = translations[0].translations[0].text.ToString();
                
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
        /// Translates multiple text strings in batch
        /// </summary>
        public async Task<IList<TranslationResult>> TranslateTextsAsync(
            IList<string> texts,
            TranslationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Use provided options or fall back to default options
            options ??= _defaultOptions;
            
            try
            {
                var results = new List<TranslationResult>();
                
                // Create request body for batch translation
                var requestBody = texts.Select(text => new { Text = text }).ToArray();
                
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
                
                // Parse response
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var translations = JsonConvert.DeserializeObject<List<dynamic>>(responseJson);
                
                if (translations == null || translations.Count == 0)
                {
                    throw new InvalidOperationException("No translation results were returned");
                }
                
                // Process each translation result
                for (int i = 0; i < Math.Min(texts.Count, translations.Count); i++)
                {
                    var translatedText = translations[i].translations[0].text.ToString();
                    
                    results.Add(new TranslationResult
                    {
                        SourceText = texts[i],
                        TranslatedText = translatedText,
                        SourceLanguage = options.SourceLanguage,
                        TargetLanguage = options.TargetLanguage,
                        TranslationTime = DateTime.Now
                    });
                }
                
                return results;
            }
            catch (Exception ex)
            {
                throw new Exception($"Batch translation failed: {ex.Message}", ex);
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
          /// <summary>
        /// Translates a single text string with enhanced handling for special cases and idioms
        /// </summary>
        public async Task<TranslationResult> TranslateTextWithEnhancementsAsync(
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
                
                // Pre-process the text for better translation quality
                string preprocessedText = PreprocessTextForTranslation(text);
                
                // Report preprocessing progress
                progressCallback?.Report(0.2);
                
                // Create request body
                var requestBody = new[] {
                    new { Text = preprocessedText }
                };
                
                var requestJson = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
                // Add customization options for better quality
                string targetLanguage = options.TargetLanguage;
                
                // Build request parameters for highest quality translation
                // textType: html/plain - Using 'plain' for pure text content
                // profanityAction: marked/deleted/noaction - Using 'marked' to handle but preserve profanity
                // Add suggestedFrom parameter for better detection
                // Add allowFallback if needed to allow general model fallback
                string queryParams = string.Join("&", new List<string>
                {
                    $"api-version=3.0",
                    $"from={options.SourceLanguage}",
                    $"to={targetLanguage}",
                    $"textType=plain",
                    $"profanityAction=marked"
                });
                
                // Add any custom parameters from options
                if (options.CustomParameters != null && options.CustomParameters.Count > 0)
                {
                    foreach (var param in options.CustomParameters)
                    {
                        queryParams += $"&{param.Key}={param.Value}";
                    }
                }
                
                // Setup request with quality-enhancing parameters
                var route = $"/translate?{queryParams}";
                
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
                
                // Post-process the translation for better quality
                translatedText = PostprocessTranslation(translatedText);
                
                // Report completion
                progressCallback?.Report(0.9);
                
                // Create result
                var result = new TranslationResult
                {
                    SourceText = text,
                    TranslatedText = translatedText,
                    SourceLanguage = options.SourceLanguage,
                    TargetLanguage = options.TargetLanguage,
                    TranslationTime = DateTime.Now
                };
                
                progressCallback?.Report(1.0);
                
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Enhanced translation failed: {ex.Message}", ex);
            }
        }
          /// <summary>
        /// Translates a transcription result with enhanced quality and temporal preservation
        /// </summary>
        public async Task<TranslationResult> TranslateTranscriptionWithEnhancementsAsync(
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
                
                // Preprocess segments for better translation quality
                var preprocessedSegments = segmentTexts.Select(PreprocessTextForTranslation).ToList();
                
                // Create request body with preprocessed text
                var requestBody = preprocessedSegments.Select(text => new { Text = text }).ToArray();
                
                var requestJson = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
                // Build request parameters for highest quality translation
                string queryParams = string.Join("&", new List<string>
                {
                    $"api-version=3.0",
                    $"from={options.SourceLanguage}",
                    $"to={options.TargetLanguage}",
                    $"textType=plain",  
                    $"profanityAction=marked",
                    // Prevent breaking sentence structures that need to be synchronized with timing
                    $"sentLen=false"
                });
                
                // Add any custom parameters from options
                if (options.CustomParameters != null && options.CustomParameters.Count > 0)
                {
                    foreach (var param in options.CustomParameters)
                    {
                        queryParams += $"&{param.Key}={param.Value}";
                    }
                }
                
                // Setup request with additional parameters for better quality
                var route = $"/translate?{queryParams}";
                
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
                    
                    // Post-process translated text for better quality
                    translatedText = PostprocessTranslation(translatedText);
                    
                    // Apply additional timing adjustments based on text length ratio
                    // (Turkish translations might be slightly longer/shorter than English)
                    double originalLength = originalSegment.Text.Length;
                    double translatedLength = translatedText.Length;
                    double lengthRatio = translatedLength / (originalLength > 0 ? originalLength : 1);
                    
                    // Adjust timing if the translated text is significantly different in length
                    double startTime = originalSegment.StartTime;
                    double endTime = originalSegment.EndTime;
                    
                    // Only adjust end time if translation is significantly longer
                    if (lengthRatio > 1.25 && (endTime - startTime) > 1.0)
                    {
                        // Increase end time proportionally, but not by more than 30%
                        double extension = Math.Min((endTime - startTime) * 0.3, 
                                                  (endTime - startTime) * (lengthRatio - 1.0));
                        endTime += extension;
                    }
                    
                    translatedSegments.Add(new TranslatedSegment
                    {
                        SourceText = originalSegment.Text,
                        TranslatedText = translatedText,
                        StartTime = startTime,
                        EndTime = endTime,
                        SourceConfidence = originalSegment.Confidence
                    });
                    
                    // Report segment progress
                    double segmentProgress = 0.6 + (0.4 * (i + 1) / transcriptionResult.Segments.Count);
                    progressCallback?.Report(segmentProgress);
                }
                
                // Create combined full text preserving line breaks where possible
                string translatedFull = string.Join(" ", 
                                        translatedSegments.Select(s => s.TranslatedText
                                                               .Replace("\r\n", " ")
                                                               .Replace("\n", " ")));
                
                // Create and return the translation result
                return new TranslationResult
                {
                    SourceText = transcriptionResult.FullText,
                    TranslatedText = translatedFull,
                    SourceLanguage = options.SourceLanguage,
                    TargetLanguage = options.TargetLanguage,
                    TranslationTime = DateTime.Now,
                    TranslatedSegments = translatedSegments
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Enhanced transcription translation failed: {ex.Message}", ex);
            }
        }
        
        #region Helper Methods for Enhanced Translation Quality        /// <summary>
        /// Preprocesses text for improved translation quality
        /// </summary>
        private string PreprocessTextForTranslation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;
                
            // Replace common English contractions with their expanded forms
            var contractionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "I'm", "I am" },
                { "I've", "I have" },
                { "I'll", "I will" },
                { "I'd", "I would" },
                { "don't", "do not" },
                { "doesn't", "does not" },
                { "didn't", "did not" },
                { "won't", "will not" },
                { "can't", "cannot" },
                { "shouldn't", "should not" },
                { "wouldn't", "would not" },
                { "couldn't", "could not" },
                { "isn't", "is not" },
                { "aren't", "are not" },
                { "wasn't", "was not" },
                { "weren't", "were not" },
                { "haven't", "have not" },
                { "hasn't", "has not" },
                { "hadn't", "had not" },
                { "there's", "there is" },
                { "that's", "that is" },
                { "what's", "what is" },
                { "who's", "who is" },
                { "where's", "where is" },
                { "how's", "how is" },
                { "it's", "it is" },
                { "let's", "let us" },
                { "they're", "they are" },
                { "we're", "we are" },
                { "you're", "you are" },
                { "they've", "they have" },
                { "we've", "we have" },
                { "you've", "you have" },
                { "they'll", "they will" },
                { "we'll", "we will" },
                { "you'll", "you will" },
                { "they'd", "they would" },
                { "we'd", "we would" },
                { "you'd", "you would" }
            };
            
            // Build a regex pattern for all contractions
            string pattern = string.Join("|", contractionMap.Keys.Select(k => Regex.Escape(k)));
            
            // Replace contractions with their expanded forms
            string result = Regex.Replace(text, pattern, match => contractionMap[match.Value], RegexOptions.IgnoreCase);

            // Process idioms before translation (mark them for special handling)
            result = EnglishTurkishHelper.ProcessIdioms(result);
            
            // Convert some common English expressions that translate poorly to equivalent forms
            result = Regex.Replace(result, @"\bgoing to\b", "will", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bin order to\b", "to", RegexOptions.IgnoreCase);
            
            // Add emphasis markers for proper nouns and names - helps translation engine preserve them
            // Look for capitalized words in the middle of a sentence and mark them
            result = Regex.Replace(result, @"(?<=[.!?]\s+\p{Ll}[^.!?]*\s+)(\p{Lu}\w+)", "[[NAME:$1]]");
            
            // Mark numbers with measurements for better handling
            result = Regex.Replace(result, @"(\d+)\s*(dollars|euros|pounds|inches|feet|yards|miles|kilometers|meters|cm|kg|pounds|ounces|gallons|liters)", 
                "[[MEASURE:$1 $2]]");
            
            return result;
        }
          /// <summary>
        /// Postprocesses the translated text for improved quality
        /// </summary>
        private string PostprocessTranslation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;
                
            // Process the text with various Turkish-specific enhancements
            string result = text;
            
            // Replace idiom placeholders with their correct Turkish translations
            result = EnglishTurkishHelper.ReplaceIdiomPlaceholders(result);
            
            // Fix special Turkish characters that might be incorrectly capitalized or formatted
            // The proper case conversion in Turkish:
            // i -> İ (lowercase i to uppercase dotted I)
            // I -> ı (uppercase I to lowercase undotted i)
            result = EnglishTurkishHelper.FixTurkishILetters(result);
            
            // Apply proper Turkish capitalization rules
            result = EnglishTurkishHelper.ApplyTurkishCapitalization(result);
            
            // Fix spacing for suffixes
            result = EnglishTurkishHelper.FixTurkishSuffixSpacing(result);
            
            // Replace placeholder markers
            result = Regex.Replace(result, @"\[\[NAME:(.*?)\]\]", "$1");
            result = Regex.Replace(result, @"\[\[MEASURE:(.*?)\]\]", "$1");
            
            // Handle common Turkish translation patterns and correct common translation errors
            var turkishCorrections = new Dictionary<string, string>
            {
                { "iyi günler", "İyi günler" },
                { "lütfen", "Lütfen" },
                { "teşekkür ederim", "Teşekkür ederim" },
                { "teşekkürler", "Teşekkürler" },
                { "merhaba", "Merhaba" },
                { "günaydın", "Günaydın" },
                { "iyi akşamlar", "İyi akşamlar" },
                { "özür dilerim", "Özür dilerim" },
                // Common mistranslation fixes
                { "Ben am", "Ben" },
                { "Sen are", "Sen" },
                { "O is", "O" },
                { "Biz are", "Biz" },
                { "Onlar are", "Onlar" },
                // Convert ordinal number format
                { "1 inci", "1." },
                { "2 nci", "2." },
                { "3 üncü", "3." },
                { "4 üncü", "4." },
                { "5 inci", "5." },
                { "6 ncı", "6." },
                { "7 nci", "7." },
                { "8 inci", "8." },
                { "9 uncu", "9." },
                { "10 uncu", "10." }
            };
            
            // Apply Turkish corrections when they appear at the beginning of a sentence
            foreach (var correction in turkishCorrections)
            {
                // Replace at start of text
                if (result.StartsWith(correction.Key, StringComparison.OrdinalIgnoreCase))
                {
                    result = correction.Value + result.Substring(correction.Key.Length);
                }
                
                // Replace after period, exclamation, or question mark followed by space
                result = Regex.Replace(
                    result, 
                    $@"([\.\!\?]\s+){correction.Key}",
                    m => m.Groups[1].Value + correction.Value,
                    RegexOptions.IgnoreCase
                );
            }
            
            // Fix common doubled words that may result from translation
            result = Regex.Replace(result, @"\b(\w+)\s+\1\b", "$1", RegexOptions.IgnoreCase);
            
            // Ensure proper quotation marks format for Turkish
            result = result.Replace("\"", """).Replace("\"", """);
            
            return result;
        }
        
        #endregion
    }
}
