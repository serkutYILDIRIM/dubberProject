using System;
using System.Threading.Tasks;
using YouTubeDubber.Core.Models;
using YouTubeDubber.Core.Services;

namespace SimpleLibreTranslateTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Simple LibreTranslate Test");
            Console.WriteLine("=========================");
            
            // Create options for English to Turkish
            var options = new TranslationOptions
            {
                SourceLanguage = "en",
                TargetLanguage = "tr"
            };
            
            // Initialize with invalid URL to test offline mode
            var service = new LibreTranslateService(
                options,
                "https://nonexistent-server.example",
                enableOfflineMode: true,
                maxRetries: 1);
            
            // Check if online or offline
            bool isOnline = await service.IsOnlineAsync();
            Console.WriteLine($"Service status: {(isOnline ? "Online" : "Offline")}");
            
            try
            {
                // Test a simple translation
                string text = "Hello, good morning!";
                var result = await service.TranslateTextAsync(text);
                Console.WriteLine($"Original: {text}");
                Console.WriteLine($"Translated: {result.TranslatedText}");
                
                // Test an idiom
                string idiom = "It's raining cats and dogs.";
                var idiomResult = await service.TranslateTextWithEnhancementsAsync(idiom);
                Console.WriteLine($"\nOriginal idiom: {idiom}");
                Console.WriteLine($"Translated idiom: {idiomResult.TranslatedText}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            // Show cache stats
            var stats = service.GetCacheStatistics();
            Console.WriteLine($"\nCache stats: {stats.CacheSize} items");
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
