using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Interfaces
{
    /// <summary>
    /// Interface for translation services
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// Translates a single text string
        /// </summary>
        /// <param name="text">The text to translate</param>
        /// <param name="options">Translation configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The translation result</returns>
        Task<TranslationResult> TranslateTextAsync(
            string text,
            TranslationOptions? options = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Translates multiple text strings in batch
        /// </summary>
        /// <param name="texts">The texts to translate</param>
        /// <param name="options">Translation configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The translation results</returns>
        Task<IList<TranslationResult>> TranslateTextsAsync(
            IList<string> texts,
            TranslationOptions? options = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Translates a transcription result, preserving timing information
        /// </summary>
        /// <param name="transcriptionResult">The transcription result to translate</param>
        /// <param name="options">Translation configuration options</param>
        /// <param name="progressCallback">Optional callback to report translation progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The translation result with preserved timing information</returns>
        Task<TranslationResult> TranslateTranscriptionAsync(
            TranscriptionResult transcriptionResult,
            TranslationOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validates the translation service API credentials
        /// </summary>
        /// <param name="options">Translation configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the credentials are valid, otherwise false</returns>
        Task<bool> ValidateCredentialsAsync(
            TranslationOptions options,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets available language pairs for translation
        /// </summary>
        /// <param name="options">Translation configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of language codes with their corresponding display names</returns>
        Task<Dictionary<string, string>> GetAvailableLanguagesAsync(
            TranslationOptions? options = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Saves the translation result to a file
        /// </summary>
        /// <param name="translationResult">The translation result to save</param>
        /// <param name="outputFilePath">Path where the translation should be saved</param>
        /// <param name="format">Format to save the translation (e.g., "txt", "srt", "json")</param>
        /// <returns>The path to the saved translation file</returns>
        Task<string> SaveTranslationAsync(
            TranslationResult translationResult,
            string outputFilePath,
            string format = "txt");
    }
}
