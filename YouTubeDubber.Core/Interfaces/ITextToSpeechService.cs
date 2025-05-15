using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Interfaces
{
    /// <summary>
    /// Interface for text-to-speech services
    /// </summary>
    public interface ITextToSpeechService
    {
        /// <summary>
        /// Converts text to speech and saves the audio to a file
        /// </summary>
        /// <param name="text">The text to synthesize</param>
        /// <param name="outputFilePath">Path to save the generated audio file</param>
        /// <param name="options">Text-to-speech configuration options</param>
        /// <param name="progressCallback">Optional callback to report synthesis progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the generated audio file</returns>
        Task<string> SynthesizeTextToSpeechAsync(
            string text,
            string outputFilePath,
            TextToSpeechOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Synthesizes speech from a translation result, preserving timing information
        /// </summary>
        /// <param name="translationResult">The translation result to synthesize</param>
        /// <param name="outputFilePath">Path to save the generated audio file</param>
        /// <param name="options">Text-to-speech configuration options</param>
        /// <param name="progressCallback">Optional callback to report synthesis progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the generated audio file</returns>
        Task<string> SynthesizeTranslationAsync(
            TranslationResult translationResult,
            string outputFilePath,
            TextToSpeechOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets a list of available voices for the specified language
        /// </summary>
        /// <param name="languageCode">The language code (e.g., "tr-TR" for Turkish)</param>
        /// <param name="options">Text-to-speech configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of available voice names</returns>
        Task<string[]> GetAvailableVoicesAsync(
            string languageCode,
            TextToSpeechOptions? options = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Validates the text-to-speech service API credentials
        /// </summary>
        /// <param name="options">Text-to-speech configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the credentials are valid, false otherwise</returns>
        Task<bool> ValidateCredentialsAsync(
            TextToSpeechOptions options,
            CancellationToken cancellationToken = default);
    }
}
