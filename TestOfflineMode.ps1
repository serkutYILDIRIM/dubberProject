# Test LibreTranslate Offline Mode
# This script creates a simple C# program to test the offline translation capability

$testCode = @"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using YouTubeDubber.Core.Services;
using YouTubeDubber.Core.Models;

namespace SimpleOfflineTranslationTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during testing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
"@

# Create a temporary directory for our test
$testDir = ".\TestLibreOfflineMode"
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
Set-Location $testDir

# Create the Program.cs file
$testCode | Out-File -FilePath "Program.cs" -Encoding utf8

# Create project file and add reference
dotnet new console
dotnet add reference ..\YouTubeDubber.Core\YouTubeDubber.Core.csproj

Write-Host "`nBuilding the test project..." -ForegroundColor Yellow
dotnet build

Write-Host "`nRunning the LibreTranslate offline mode test..." -ForegroundColor Green
dotnet run

# Go back to original directory
Set-Location ..
