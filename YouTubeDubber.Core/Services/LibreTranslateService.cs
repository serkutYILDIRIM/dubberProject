using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Collections.Concurrent;
using YouTubeDubber.Core.Helpers;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Services
{
    /// <summary>
    /// Translation service implementation using LibreTranslate API with offline capability
    /// </summary>
    public class LibreTranslateService : ITranslationService
    {
        private readonly TranslationOptions _defaultOptions;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _cacheDirectory;
        private readonly ConcurrentDictionary<string, string> _translationCache;
        private readonly int _maxRetries;
        private readonly bool _enableOfflineMode;        /// <summary>
        /// Initializes a new instance of the LibreTranslate Translation Service
        /// </summary>
        /// <param name="defaultOptions">Default translation options</param>
        /// <param name="apiBaseUrl">Base URL for LibreTranslate API. Defaults to a public instance.</param>
        /// <param name="enableOfflineMode">Whether to enable offline mode with caching</param>
        /// <param name="maxRetries">Maximum number of retries for network operations</param>
        /// <param name="cachePath">Path to store translation cache. If null, uses default location.</param>
        public LibreTranslateService(
            TranslationOptions? defaultOptions = null, 
            string? apiBaseUrl = null, 
            bool enableOfflineMode = true,
            int maxRetries = 3,
            string? cachePath = null)
        {
            _defaultOptions = defaultOptions ?? new TranslationOptions();
            _apiBaseUrl = apiBaseUrl ?? "https://libretranslate.de/";
            _enableOfflineMode = enableOfflineMode;
            _maxRetries = maxRetries;
            
            // Set up HTTP client
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Shorter timeout to fail faster when offline
            
            // Initialize cache
            _translationCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            // Set up cache directory
            _cacheDirectory = cachePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YouTubeDubber", 
                "TranslationCache");
                
            // Create cache directory if it doesn't exist
            if (_enableOfflineMode && !Directory.Exists(_cacheDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not create translation cache directory: {ex.Message}");
                }
            }
            
            // Load existing cache if available
            if (_enableOfflineMode)
            {
                LoadCacheFromDisk();
            }
        }
        
        /// <summary>
        /// Loads the translation cache from disk
        /// </summary>
        private void LoadCacheFromDisk()
        {
            try
            {
                string cacheFilePath = Path.Combine(_cacheDirectory, "translation_cache.json");
                if (File.Exists(cacheFilePath))
                {
                    string cacheJson = File.ReadAllText(cacheFilePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var loadedCache = JsonSerializer.Deserialize<Dictionary<string, string>>(cacheJson, options);
                    
                    if (loadedCache != null)
                    {
                        foreach (var pair in loadedCache)
                        {
                            _translationCache[pair.Key] = pair.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load translation cache: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves the translation cache to disk
        /// </summary>
        private void SaveCacheToDisk()
        {
            try
            {
                string cacheFilePath = Path.Combine(_cacheDirectory, "translation_cache.json");
                string cacheJson = JsonSerializer.Serialize(_translationCache);
                File.WriteAllText(cacheFilePath, cacheJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to save translation cache: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if the service is online and available
        /// </summary>
        /// <returns>True if online and available, otherwise false</returns>
        public async Task<bool> IsOnlineAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync("languages", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the supported languages from LibreTranslate API
        /// </summary>
        /// <returns>A list of supported languages</returns>
        public async Task<List<LanguageInfo>> GetSupportedLanguagesAsync()
        {            var response = await _httpClient.GetAsync("languages");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<LanguageInfo>>(content, options) ?? new List<LanguageInfo>();
        }        /// <summary>
        /// Translates a single text string with fallback to offline mode
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
                // Check for English->Turkish specific enhancements
                if (options.SourceLanguage == "en" && options.TargetLanguage == "tr")
                {
                    // Apply English idiom translations
                    text = EnglishTurkishHelper.ReplaceIdioms(text);
                }
                
                // Generate cache key
                string cacheKey = GenerateCacheKey(text, options.SourceLanguage, options.TargetLanguage);
                
                // Check cache first if offline mode is enabled
                if (_enableOfflineMode && _translationCache.TryGetValue(cacheKey, out string? cachedTranslation))
                {
                    return CreateTranslationResult(text, cachedTranslation, options.SourceLanguage, options.TargetLanguage);
                }
                
                // Try online translation with retries
                for (int attempt = 0; attempt < _maxRetries; attempt++)
                {
                    try
                    {
                        // Create request body
                        var requestBody = new
                        {
                            q = text,
                            source = ConvertToLibreTranslateLanguageCode(options.SourceLanguage),
                            target = ConvertToLibreTranslateLanguageCode(options.TargetLanguage),
                            format = "text",
                            api_key = string.IsNullOrEmpty(options.ApiKey) ? string.Empty : options.ApiKey
                        };
                        
                        // Serialize request
                        var jsonContent = JsonSerializer.Serialize(requestBody);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        
                        // Create a combined token that respects both cancellation and timeout
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        
                        // Send request
                        var response = await _httpClient.PostAsync("translate", content, combinedCts.Token);
                        response.EnsureSuccessStatusCode();
                        
                        // Parse response
                        var responseContent = await response.Content.ReadAsStringAsync(combinedCts.Token);
                        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var translationResponse = JsonSerializer.Deserialize<LibreTranslateResponse>(responseContent, jsonOptions);
                        
                        if (translationResponse == null || string.IsNullOrEmpty(translationResponse.TranslatedText))
                        {
                            throw new Exception("Failed to parse translation response");
                        }
                        
                        // Apply post-processing for Turkish translations if needed
                        string translatedText = translationResponse.TranslatedText;
                        if (options.SourceLanguage == "en" && options.TargetLanguage == "tr")
                        {
                            translatedText = EnglishTurkishHelper.PostProcessTurkishTranslation(translatedText);
                        }
                        
                        // Cache the successful translation
                        if (_enableOfflineMode)
                        {
                            _translationCache[cacheKey] = translatedText;
                            
                            // Periodically save to disk (every 10 translations)
                            if (_translationCache.Count % 10 == 0)
                            {
                                Task.Run(() => SaveCacheToDisk());
                            }
                        }
                        
                        // Create result
                        var result = CreateTranslationResult(text, translatedText, options.SourceLanguage, options.TargetLanguage);
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        // If the user's cancellation token was triggered, propagate the cancellation
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        
                        // Otherwise it was our timeout - try again or fall back to offline
                        if (attempt == _maxRetries - 1)
                        {
                            return await FallbackToOfflineTranslationAsync(text, options);
                        }
                    }
                    catch (Exception ex) when (IsNetworkError(ex))
                    {
                        // Network error - try again or fall back to offline
                        if (attempt == _maxRetries - 1)
                        {
                            return await FallbackToOfflineTranslationAsync(text, options);
                        }
                        
                        // Wait a moment before retrying
                        await Task.Delay(500 * (attempt + 1), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // For other exceptions, try offline mode or rethrow
                        return await FallbackToOfflineTranslationAsync(text, options, ex);
                    }
                }
                
                // This should not be reached due to the retry mechanism
                return await FallbackToOfflineTranslationAsync(text, options);
            }
            catch (Exception ex)
            {
                throw new Exception($"LibreTranslate translation failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Creates a translation result object
        /// </summary>
        private TranslationResult CreateTranslationResult(
            string sourceText, 
            string translatedText, 
            string sourceLanguage, 
            string targetLanguage)
        {
            return new TranslationResult
            {
                SourceText = sourceText,
                TranslatedText = translatedText,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                TranslationTime = DateTime.Now
            };
        }
        
        /// <summary>
        /// Checks if an exception is related to network connectivity
        /// </summary>
        private bool IsNetworkError(Exception ex)
        {
            return ex is HttpRequestException 
                || ex is WebException 
                || ex is OperationCanceledException
                || (ex.InnerException != null && IsNetworkError(ex.InnerException));
        }
        
        /// <summary>
        /// Generates a cache key for a translation
        /// </summary>
        private string GenerateCacheKey(string text, string sourceLanguage, string targetLanguage)
        {
            // Use the first 100 chars of text to avoid overly long keys
            string truncatedText = text.Length > 100 ? text.Substring(0, 100) : text;
            return $"{sourceLanguage}:{targetLanguage}:{truncatedText.GetHashCode()}";
        }
        
        /// <summary>
        /// Fallback to offline translation when online translation fails
        /// </summary>
        private async Task<TranslationResult> FallbackToOfflineTranslationAsync(
            string text, 
            TranslationOptions options, 
            Exception? originalException = null)
        {
            if (!_enableOfflineMode)
            {
                if (originalException != null)
                {
                    throw new Exception($"Translation failed and offline mode is disabled: {originalException.Message}", originalException);
                }
                else
                {
                    throw new Exception("Translation failed and offline mode is disabled");
                }
            }
            
            // Try to use the cache first
            string cacheKey = GenerateCacheKey(text, options.SourceLanguage, options.TargetLanguage);
            if (_translationCache.TryGetValue(cacheKey, out string? cachedTranslation))
            {
                return CreateTranslationResult(text, cachedTranslation, options.SourceLanguage, options.TargetLanguage);
            }
            
            // Basic fallback with dictionary lookup for common phrases
            if (options.SourceLanguage == "en" && options.TargetLanguage == "tr")
            {
                string fallbackTranslation = GetFallbackTranslation(text);
                return CreateTranslationResult(text, fallbackTranslation, options.SourceLanguage, options.TargetLanguage);
            }
            
            // Last resort: use the original text with a note about offline mode
            string notAvailableMessage = options.TargetLanguage == "tr" 
                ? "[Çeviri kullanılamıyor - çevrimdışı mod]" 
                : "[Translation not available - offline mode]";
                
            return CreateTranslationResult(
                text, 
                $"{text} {notAvailableMessage}", 
                options.SourceLanguage, 
                options.TargetLanguage);
        }
        
        /// <summary>
        /// Get a basic fallback translation for common phrases
        /// </summary>
        private string GetFallbackTranslation(string text)
        {
            // For English to Turkish, we can provide basic translations for common phrases
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
                
            // Try to find exact matches in our dictionary
            if (OfflineTranslations.CommonEnglishTurkishPhrases.TryGetValue(text.Trim().ToLowerInvariant(), out string? translation))
            {
                return translation;
            }
            
            // Try fallback translations using idioms
            string processedText = EnglishTurkishHelper.ReplaceIdioms(text);
            
            // If no changes were made by the idiom replacement, just return the original
            return processedText != text 
                ? processedText 
                : $"{text} [Çevrimdışı çeviri]";
        }
          /// <summary>
        /// Translates multiple text strings in batch with fallback to offline mode
        /// </summary>
        public async Task<IList<TranslationResult>> TranslateTextsAsync(
            IList<string> texts,
            TranslationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= _defaultOptions;
            
            var results = new List<TranslationResult>();
            bool isOffline = !await IsOnlineAsync();
            
            // First check if we need to go to offline mode immediately
            if (isOffline && _enableOfflineMode)
            {
                // We're offline and offline mode is enabled, use offline mode for all translations
                foreach (var text in texts)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    var result = await FallbackToOfflineTranslationAsync(text, options);
                    results.Add(result);
                }
                
                return results;
            }
            
            // Otherwise try online translation for each item, with individual fallbacks if needed
            foreach (var text in texts)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                try
                {
                    var result = await TranslateTextAsync(text, options, cancellationToken);
                    results.Add(result);
                }
                catch (Exception)
                {
                    // If a single translation fails, try offline mode for just that one
                    if (_enableOfflineMode)
                    {
                        var fallbackResult = await FallbackToOfflineTranslationAsync(text, options);
                        results.Add(fallbackResult);
                    }
                    else
                    {
                        // Re-throw if offline mode isn't enabled
                        throw;
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Translates a transcription result, preserving timing information with offline fallback
        /// </summary>
        public async Task<TranslationResult> TranslateTranscriptionAsync(
            TranscriptionResult transcriptionResult,
            TranslationOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            options ??= _defaultOptions;
            
            // Prepare result
            var result = new TranslationResult
            {
                SourceText = transcriptionResult.FullText,
                SourceLanguage = options.SourceLanguage,
                TargetLanguage = options.TargetLanguage,
                TranslationTime = DateTime.Now,
                TranslatedSegments = new List<TranslatedSegment>()
            };
            
            // Check connectivity once before starting segment translations
            bool isOffline = !await IsOnlineAsync();
            
            // Translate each segment individually to preserve timing
            int totalSegments = transcriptionResult.Segments.Count;
            int completedSegments = 0;
            
            foreach (var segment in transcriptionResult.Segments)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                // Translate segment text
                TranslationResult segmentTranslation;
                
                if (isOffline && _enableOfflineMode)
                {
                    // Use offline translation if we already know we're offline
                    segmentTranslation = await FallbackToOfflineTranslationAsync(segment.Text, options);
                }
                else
                {
                    try
                    {
                        // Try online translation first
                        segmentTranslation = await TranslateTextAsync(segment.Text, options, cancellationToken);
                    }
                    catch (Exception)
                    {
                        if (_enableOfflineMode)
                        {
                            // Fall back to offline mode for this segment
                            segmentTranslation = await FallbackToOfflineTranslationAsync(segment.Text, options);
                            // Update offline status for future segments
                            isOffline = true;
                        }
                        else
                        {
                            // Re-throw if offline mode is disabled
                            throw;
                        }
                    }
                }
                
                // Add translated segment with preserved timing
                result.TranslatedSegments.Add(new TranslatedSegment
                {
                    SourceText = segment.Text,
                    TranslatedText = segmentTranslation.TranslatedText,
                    StartTime = segment.StartTime,
                    EndTime = segment.EndTime
                });
                
                // Report progress
                completedSegments++;
                progressCallback?.Report((double)completedSegments / totalSegments);
            }
            
            // Combine all translated segments into the main translated text
            result.TranslatedText = string.Join(" ", 
                result.TranslatedSegments.Select(s => s.TranslatedText));
            
            return result;
        }
          /// <summary>
        /// Enhanced translation method for English to Turkish with specialized improvements and offline support
        /// </summary>
        public async Task<TranslationResult> TranslateTextWithEnhancementsAsync(
            string text,
            TranslationOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Apply pre-translation enhancements for English->Turkish
            string processedText = EnglishTurkishHelper.PreProcessEnglishText(text);
            
            // Report preprocessing progress
            progressCallback?.Report(0.25);
            
            // Do the actual translation, with error handling already built in
            var result = await TranslateTextAsync(processedText, options, cancellationToken);
            
            // Report translation progress
            progressCallback?.Report(0.75);
            
            // Apply post-translation enhancements regardless of whether we got the result
            // from online or offline sources
            result.TranslatedText = EnglishTurkishHelper.PostProcessTurkishTranslation(result.TranslatedText);
            
            // Report completion
            progressCallback?.Report(1.0);
            return result;
        }
        
        /// <summary>
        /// Get information about the cache size and hit rate
        /// </summary>
        public (int CacheSize, int CachedLanguagePairs) GetCacheStatistics()
        {
            if (!_enableOfflineMode)
                return (0, 0);
                
            var languagePairs = _translationCache.Keys
                .Select(k => k.Split(':', 2)[0] + ":" + k.Split(':', 2)[1])
                .Distinct()
                .Count();
                
            return (_translationCache.Count, languagePairs);
        }
        
        /// <summary>
        /// Clears the translation cache
        /// </summary>
        public void ClearCache()
        {
            if (!_enableOfflineMode)
                return;
                
            _translationCache.Clear();
            
            try
            {
                string cacheFilePath = Path.Combine(_cacheDirectory, "translation_cache.json");
                if (File.Exists(cacheFilePath))
                {
                    File.Delete(cacheFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to delete cache file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Converts standard language codes to LibreTranslate compatible codes
        /// </summary>
        private string ConvertToLibreTranslateLanguageCode(string languageCode)
        {
            // LibreTranslate uses two-letter codes, so we need to convert some common ones
            return languageCode.ToLower() switch
            {
                "en-us" => "en",
                "en-gb" => "en", 
                "tr-tr" => "tr",
                _ => languageCode.Split('-')[0].ToLower() // Default to first part of language code
            };
        }
        
        /// <summary>
        /// Response structure from LibreTranslate API
        /// </summary>
        private class LibreTranslateResponse
        {
            public string TranslatedText { get; set; } = string.Empty;
        }
        
        /// <summary>
        /// Language information from LibreTranslate API
        /// </summary>
        public class LanguageInfo
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}
