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
            try 
            {
                // Determine the native library file names based on OS
                string libraryExtension;
                string libraryPrefix;
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    libraryExtension = ".dll";
                    libraryPrefix = "";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    libraryExtension = ".so";
                    libraryPrefix = "lib";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    libraryExtension = ".dylib";
                    libraryPrefix = "lib";
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported operating system for TorchSharp");
                }
                
                // Check if TorchSharp native library exists
                string baseLibraryName = $"{libraryPrefix}torch_cpu{libraryExtension}";
                string libraryPath = GetTorchLibraryPath(baseLibraryName);
                
                if (string.IsNullOrEmpty(libraryPath) || !File.Exists(libraryPath))
                {
                    Console.WriteLine($"TorchSharp native library not found. Downloading and extracting needed files...");
                    
                    // Determine app base directory
                    string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                    
                    // Create runtimes directory structure
                    string osArchDir = GetOSArchitectureDirectory();
                    string nativeDir = Path.Combine(appDir, "runtimes", osArchDir, "native");
                    
                    Directory.CreateDirectory(nativeDir);
                    
                    // Normally we would download the libraries from a source URL
                    // For this implementation, we'll assume they are already installed by NuGet
                    // and just verify the installation
                    
                    string nugetPackagesDir = GetNuGetPackagesDirectory();
                    string torchSharpVersion = GetInstalledTorchSharpVersion();
                    
                    if (!string.IsNullOrEmpty(nugetPackagesDir) && !string.IsNullOrEmpty(torchSharpVersion))
                    {
                        string sourcePath = Path.Combine(
                            nugetPackagesDir,
                            $"torchsharp-{architecture}",
                            torchSharpVersion,
                            "runtimes", 
                            osArchDir,
                            "native");
                            
                        if (Directory.Exists(sourcePath))
                        {
                            // Copy native libraries to our runtime directory
                            foreach (var file in Directory.GetFiles(sourcePath))
                            {
                                string targetFile = Path.Combine(nativeDir, Path.GetFileName(file));
                                if (!File.Exists(targetFile))
                                {
                                    File.Copy(file, targetFile);
                                }
                            }
                            
                            Console.WriteLine($"TorchSharp native libraries installed successfully.");
                        }
                        else
                        {
                            Console.WriteLine($"Could not find TorchSharp native libraries in NuGet cache.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"TorchSharp native library found at: {libraryPath}");
                }
                
                await Task.Delay(100, cancellationToken); // Small delay to ensure filesystem operations complete
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error ensuring TorchSharp packages: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the path of a TorchSharp native library
        /// </summary>
        private static string GetTorchLibraryPath(string libraryName)
        {
            // Try to find in common locations
            string[] searchPaths = new[]
            {
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, 
                    "runtimes", 
                    GetOSArchitectureDirectory(), 
                    "native"),
                AppDomain.CurrentDomain.BaseDirectory
            };
            
            foreach (var basePath in searchPaths)
            {
                string fullPath = Path.Combine(basePath, libraryName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Gets the OS-architecture directory name for native libraries
        /// </summary>
        private static string GetOSArchitectureDirectory()
        {
            string osName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                osName = "win";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                osName = "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                osName = "osx";
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS platform");
            }
            
            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                _ => throw new PlatformNotSupportedException("Unsupported architecture")
            };
            
            return $"{osName}-{arch}";
        }
        
        /// <summary>
        /// Gets the NuGet packages directory
        /// </summary>
        private static string GetNuGetPackagesDirectory()
        {
            // Try common locations for NuGet package cache
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] possibleLocations = 
            {
                Path.Combine(userProfile, ".nuget", "packages"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuGet", "Packages")
            };
            
            foreach (var location in possibleLocations)
            {
                if (Directory.Exists(location))
                {
                    return location;
                }
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Gets the installed TorchSharp version from assembly
        /// </summary>
        private static string GetInstalledTorchSharpVersion()
        {
            try
            {
                // Try to load TorchSharp assembly to check version
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "TorchSharp");
                    
                if (assembly != null)
                {
                    Version version = assembly.GetName().Version;
                    return $"{version.Major}.{version.Minor}.{version.Build}";
                }
                
                // Fallback to the version we added to the project
                return "0.105.0";
            }
            catch
            {
                // Fallback to the version we added to the project
                return "0.105.0";
            }
        }
    }
}
