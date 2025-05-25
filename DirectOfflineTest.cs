using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using YouTubeDubber.Core.Services;
using YouTubeDubber.Core.Models;

// Direct test for LibreTranslateService offline mode
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("===============================================");
Console.WriteLine("    LibreTranslate Offline Mode Test");
Console.WriteLine("===============================================");

try
{
    // Create translation options for English to Turkish
    var options = new TranslationOptions
    {
        SourceLanguage = "en",
        TargetLanguage = "tr"
    };
    
    // Create offline service with invalid URL to simulate being offline
    Console.WriteLine("Creating service with invalid URL (simulating offline mode)...");
    var service = new LibreTranslateService(
        options,
        "https://nonexistent-server.invalid",
        enableOfflineMode: true,
        maxRetries: 1);
    
    // Check connectivity
    bool isOnline = await service.IsOnlineAsync();
    Console.WriteLine($"Service connectivity: {(isOnline ? "Online" : "Offline")}");
    
    Console.WriteLine("\nTest 1: Basic phrase translation");
    Console.WriteLine("--------------------------------");
    string basicPhrase = "Hello, good morning!";
    var result1 = await service.TranslateTextAsync(basicPhrase);
    Console.WriteLine($"Original: {basicPhrase}");
    Console.WriteLine($"Translated: {result1.TranslatedText}");
    
    Console.WriteLine("\nTest 2: Idiom translation");
    Console.WriteLine("-----------------------");
    string idiomPhrase = "It's raining cats and dogs outside.";
    var result2 = await service.TranslateTextWithEnhancementsAsync(idiomPhrase);
    Console.WriteLine($"Original: {idiomPhrase}");
    Console.WriteLine($"Translated: {result2.TranslatedText}");
    
    Console.WriteLine("\nTest 3: Batch translation");
    Console.WriteLine("------------------------");
    var batchTexts = new List<string> 
    {
        "First sentence to translate.",
        "It costs an arm and a leg.",
        "The weather is nice today."
    };
    
    var batchResults = await service.TranslateTextsAsync(batchTexts);
    for (int i = 0; i < batchTexts.Count; i++)
    {
        Console.WriteLine($"Original {i+1}: {batchTexts[i]}");
        Console.WriteLine($"Translated {i+1}: {batchResults[i].TranslatedText}");
    }
    
    Console.WriteLine("\nTest 4: Unknown phrase");
    Console.WriteLine("--------------------");
    string unknownPhrase = "This is a very specific sentence that shouldn't be in any predefined dictionary.";
    var result4 = await service.TranslateTextAsync(unknownPhrase);
    Console.WriteLine($"Original: {unknownPhrase}");
    Console.WriteLine($"Translated: {result4.TranslatedText}");
    
    // Show cache statistics
    var stats = service.GetCacheStatistics();
    Console.WriteLine($"\nCache statistics: {stats.CacheSize} items in cache");
    
    // Test cache persistence by clearing and reloading
    Console.WriteLine("\nTesting cache persistence...");
    Console.WriteLine("---------------------------");
    service.ClearCache();
    Console.WriteLine("Cache cleared");
    
    // Create a new service instance which should reload from disk if saved properly
    var newService = new LibreTranslateService(
        options,
        "https://nonexistent-server.invalid",
        enableOfflineMode: true);
        
    var newStats = newService.GetCacheStatistics();
    Console.WriteLine($"New service cache statistics: {newStats.CacheSize} items in cache");
}
catch (Exception ex)
{
    Console.WriteLine($"Error during testing: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
