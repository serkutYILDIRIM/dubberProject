using System;
using System.Threading.Tasks;
using YouTubeDubber.Core.Services;
using YouTubeDubber.Core.Models;

namespace SimpleOfflineTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("LibreTranslate Offline Mode Simple Test");
            Console.WriteLine("=======================================");
            
            try
            {
                // Create translation options
                var options = new TranslationOptions
                {
                    SourceLanguage = "en",
                    TargetLanguage = "tr"
                };
                
                // Create service with invalid URL to simulate offline mode
                Console.WriteLine("Creating translation service with invalid URL (offline mode)...");
                var service = new LibreTranslateService(
                    options,
                    "https://nonexistent-url.example", 
                    enableOfflineMode: true,
                    maxRetries: 1);
                
                // Check if service is online (should be false)
                bool isOnline = await service.IsOnlineAsync();
                Console.WriteLine($"Service connectivity: {(isOnline ? "Online" : "Offline")}");
                
                // Test basic translation
                Console.WriteLine("\nTesting basic translation...");
                string simpleText = "Hello, good morning.";
                var result = await service.TranslateTextAsync(simpleText);
                Console.WriteLine($"Original: '{simpleText}'");
                Console.WriteLine($"Translated: '{result.TranslatedText}'");
                
                // Test idiom translation
                Console.WriteLine("\nTesting idiom translation...");
                string idiomText = "It's raining cats and dogs.";
                var idiomResult = await service.TranslateTextWithEnhancementsAsync(idiomText);
                Console.WriteLine($"Original: '{idiomText}'");
                Console.WriteLine($"Translated: '{idiomResult.TranslatedText}'");
                
                // Test with non-cached phrase
                Console.WriteLine("\nTesting non-cached phrase...");
                string randomText = "This is a completely random sentence that shouldn't be in any cache.";
                var randomResult = await service.TranslateTextAsync(randomText);
                Console.WriteLine($"Original: '{randomText}'");
                Console.WriteLine($"Translated: '{randomResult.TranslatedText}'");
                
                // Get cache statistics
                var stats = service.GetCacheStatistics();
                Console.WriteLine($"\nCache statistics: {stats.CacheSize} items cached");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during testing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
