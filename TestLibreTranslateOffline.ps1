# LibreTranslate Çevrimdışı Test Script
# Doğrudan LibreTranslateService'i test eder

# C# kodunu bir .cs dosyasına yazalım
$testCode = @"
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using YouTubeDubber.Core.Models;
using YouTubeDubber.Core.Services;

namespace OfflineTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("LibreTranslate Çevrimdışı Mod Testi");
            Console.WriteLine("================================");
            
            // İngilizce-Türkçe çeviri için ayarlar
            var options = new TranslationOptions
            {
                SourceLanguage = "en",
                TargetLanguage = "tr"
            };
            
            // Geçersiz URL ile çevrimdışı modu simüle etme
            var service = new LibreTranslateService(
                options,
                "https://nonexistent-server.example",
                enableOfflineMode: true,
                maxRetries: 1);
            
            // Bağlantı durumunu kontrol
            bool isOnline = await service.IsOnlineAsync();
            Console.WriteLine($"Servis durumu: {(isOnline ? "Çevrimiçi" : "Çevrimdışı")}");
            
            try
            {
                // Basit bir çeviri testi
                string text = "Hello, good morning!";
                var result = await service.TranslateTextAsync(text);
                Console.WriteLine($"Orijinal: {text}");
                Console.WriteLine($"Çeviri: {result.TranslatedText}");
                
                // Bir deyim testi
                string idiom = "It's raining cats and dogs.";
                var idiomResult = await service.TranslateTextWithEnhancementsAsync(idiom);
                Console.WriteLine($"\nOrijinal deyim: {idiom}");
                Console.WriteLine($"Çevrilmiş deyim: {idiomResult.TranslatedText}");
                
                // Toplu çeviri testi
                Console.WriteLine("\nToplu çeviri testi:");
                var batchTexts = new List<string>
                {
                    "First sentence.",
                    "Second sentence with idiom: break a leg!",
                    "Third sentence: good afternoon."
                };
                
                var batchResults = await service.TranslateTextsAsync(batchTexts);
                for (int i = 0; i < batchTexts.Count; i++)
                {
                    Console.WriteLine($"Orijinal {i+1}: {batchTexts[i]}");
                    Console.WriteLine($"Çeviri {i+1}: {batchResults[i].TranslatedText}");
                }
                
                // Önbellek istatistiklerini görüntüle
                var stats = service.GetCacheStatistics();
                Console.WriteLine($"\nÖnbellek istatistikleri: {stats.CacheSize} öğe");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
            
            Console.WriteLine("\nTesti tamamlamak için bir tuşa basın...");
            Console.ReadKey();
        }
    }
}
"@

$testCodePath = ".\LibreTranslateOfflineTest.cs"
$testCode | Out-File -FilePath $testCodePath -Encoding utf8

# Projeye referanslarla derle ve çalıştır
Write-Host "Çevrimdışı test derleniyor ve çalıştırılıyor..." -ForegroundColor Green
dotnet run --project YouTubeDubber.Core\YouTubeDubber.Core.csproj
dotnet build YouTubeDubber.Core\YouTubeDubber.Core.csproj

$coreAssemblyPath = (Get-Item ".\YouTubeDubber.Core\bin\Debug\net8.0\YouTubeDubber.Core.dll").FullName
dotnet new console -n TempOfflineTest --no-restore
cd TempOfflineTest
Copy-Item ..\$testCodePath .
dotnet add reference $coreAssemblyPath
dotnet run

cd ..
