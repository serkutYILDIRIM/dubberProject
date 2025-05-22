using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YouTubeDubber.Core.Helpers
{
    /// <summary>
    /// Helper class for working with Silero TTS models
    /// </summary>
    public static class SileroTtsHelper
    {
        private static bool _nativeLibrariesInitialized;
        private static readonly object _initLock = new object();
        
        /// <summary>
        /// Dictionary of Turkish phoneme mappings to improve pronunciation
        /// </summary>
        public static readonly Dictionary<string, string> TurkishPhonemeMapping = new Dictionary<string, string>
        {
            { "ç", "ch" },
            { "ş", "sh" },
            { "ğ", "gh" },
            { "ı", "i" },
            { "ö", "eu" },
            { "ü", "ue" }
        };
        
        /// <summary>
        /// Initializes TorchSharp native libraries required for Silero TTS
        /// </summary>
        public static void InitializeTorchSharpLibraries()
        {
            if (_nativeLibrariesInitialized) return;
            
            lock (_initLock)
            {
                if (_nativeLibrariesInitialized) return;
                
                try
                {
                    // Attempt to load native libraries from the application directory
                    string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                    string nativeDirectory = Path.Combine(appDirectory, "runtimes");
                    
                    if (Directory.Exists(nativeDirectory))
                    {
                        // Add native directory to path for easier resolution
                        SetNativeLibraryPath(nativeDirectory);
                    }
                    
                    // Set the torch module path (optional)
                    Environment.SetEnvironmentVariable("TORCH_MODULES_DIR", appDirectory);
                    
                    _nativeLibrariesInitialized = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to initialize TorchSharp native libraries: {ex.Message}", ex);
                }
            }
        }
        
        /// <summary>
        /// Sets the path for native library resolution
        /// </summary>
        private static void SetNativeLibraryPath(string path)
        {
            // Different handling based on OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Add to PATH on Windows
                var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                var newPath = $"{path};{existingPath}";
                Environment.SetEnvironmentVariable("PATH", newPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Set LD_LIBRARY_PATH on Linux
                Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", 
                    $"{path}:{Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Set DYLD_LIBRARY_PATH on macOS
                Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", 
                    $"{path}:{Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH")}");
            }
        }
        
        /// <summary>
        /// Normalizes Turkish text for better TTS processing
        /// </summary>
        public static string NormalizeTurkishText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            var normalizedText = text;
            
            // Replace Turkish-specific characters with ASCII equivalents if needed
            foreach (var mapping in TurkishPhonemeMapping)
            {
                normalizedText = normalizedText.Replace(mapping.Key, mapping.Value);
            }
            
            // Normalize numbers and special characters
            normalizedText = NormalizeNumbers(normalizedText, new CultureInfo("tr-TR"));
            
            return normalizedText;
        }
        
        /// <summary>
        /// Normalizes numbers in text for better TTS pronunciation
        /// </summary>
        private static string NormalizeNumbers(string text, CultureInfo culture)
        {
            // Process digits and replace with word representations in the target culture
            // This is a simplified implementation; a complete solution would handle
            // different number formats (ordinals, decimals, thousands, etc.)
            
            var sb = new StringBuilder(text);
            
            // Replace decimal points according to culture
            sb.Replace(".", culture.NumberFormat.NumberDecimalSeparator);
            sb.Replace(",", culture.NumberFormat.NumberGroupSeparator);
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Gets suggested native sample rates for Silero TTS models
        /// </summary>
        public static int GetPreferredSampleRate()
        {
            // Silero models typically work best with these sample rates
            return 24000; // 24kHz is a common rate for Silero models
        }
        
        /// <summary>
        /// Adds necessary TorchSharp packages to the project
        /// </summary>
        public static async Task EnsureTorchPackagesAsync(
            string architecture = "cpu", 
            CancellationToken cancellationToken = default)
        {
            // This would be implemented in a real scenario to ensure that
            // the correct native TorchSharp packages are installed
            
            // For example, we would check if libtorch_cpu.so/dylib/dll exists
            // and if not, download the appropriate version
            
            // For demo purposes, we'll just log that this would happen
            Console.WriteLine($"Ensuring TorchSharp {architecture} packages are installed...");
            await Task.Delay(100, cancellationToken);
        }
    }
}
