using System;
using System.Threading.Tasks;
using YouTubeDubber.Core.Services;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Tests
{
    public class TestOfflineTranslation
    {        public static async Task Main(string[] args)
        {
            Console.WriteLine("Testing LibreTranslate Offline Mode");
            Console.WriteLine("==================================");
            
            try
            {
                // Create a translation options object
                var options = new TranslationOptions
                {
                    SourceLanguage = "en",
                    TargetLanguage = "tr"
                };
                
                // First test with a valid URL if possible
                Console.WriteLine("1. Testing with public LibreTranslate...");
                var onlineService = new LibreTranslateService(
                    options,
                    "https://libretranslate.de/", 
                    enableOfflineMode: true,
                    maxRetries: 2);
                
                bool isOnline = await onlineService.IsOnlineAsync();
                Console.WriteLine($"Service connectivity: {(isOnline ? "Online" : "Offline")}");
                
                try
                {
                    if (isOnline)
                    {
                        string testText = "Hello, this is a test.";
                        var result = await onlineService.TranslateTextAsync(testText);
                        Console.WriteLine($"Online translation: '{testText}' -> '{result.TranslatedText}'");
                        
                        // Get cache stats after online translation
                        var stats = onlineService.GetCacheStatistics();
                        Console.WriteLine($"Cache after online translation: {stats.CacheSize} items");
                    }
                    else
                    {
                        Console.WriteLine("Public LibreTranslate service appears to be offline.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during online test: {ex.Message}");
                }
                
                // Now test with an invalid URL to simulate offline mode
                Console.WriteLine("\n2. Testing with invalid URL (simulated offline mode)...");
                var offlineService = new LibreTranslateService(
                    options,
                    "https://nonexistent-server.example", 
                    enableOfflineMode: true,
                    maxRetries: 1);
                
                isOnline = await offlineService.IsOnlineAsync();
                Console.WriteLine($"Service connectivity: {(isOnline ? "Online" : "Offline")}");
                
                // Test basic translation with common phrases
                Console.WriteLine("\nTesting offline translation with common phrases...");
                string testText = "Hello, good morning. How are you today?";
                
                var result = await offlineService.TranslateTextAsync(testText);
                Console.WriteLine($"Source: {testText}");
                Console.WriteLine($"Translated: {result.TranslatedText}");
                
                // Test with an idiom
                Console.WriteLine("\nTesting idiom translation offline...");
                string idiomText = "It's raining cats and dogs outside.";
                
                var idiomResult = await offlineService.TranslateTextWithEnhancementsAsync(idiomText);
                Console.WriteLine($"Source: {idiomText}");
                Console.WriteLine($"Translated: {idiomResult.TranslatedText}");
                
                // Test batch translation in offline mode
                Console.WriteLine("\nTesting batch translation offline...");
                var batchTexts = new List<string>
                {
                    "First sentence.",
                    "It costs an arm and a leg.",
                    "The weather is nice today."
                };
                
                var batchResults = await offlineService.TranslateTextsAsync(batchTexts);
                for (int i = 0; i < batchTexts.Count; i++)
                {
                    Console.WriteLine($"Source {i+1}: {batchTexts[i]}");
                    Console.WriteLine($"Translated {i+1}: {batchResults[i].TranslatedText}");
                }
                
                // Show cache stats
                var stats = offlineService.GetCacheStatistics();
                Console.WriteLine($"\nCache statistics: {stats.CacheSize} translations cached");
                
                Console.WriteLine("\nOffline translation test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
