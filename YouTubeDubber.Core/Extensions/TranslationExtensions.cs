using System;
using System.Threading;
using System.Threading.Tasks;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Extensions
{
    /// <summary>
    /// Extension methods for translation services
    /// </summary>
    public static class TranslationExtensions
    {
        /// <summary>
        /// Translates text with specific English to Turkish enhancements
        /// </summary>
        public static async Task<TranslationResult> TranslateEnglishToTurkish(
            this ITranslationService translationService,
            string text,
            TranslationOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Use the enhanced translation for English to Turkish
            // This method ensures that the service uses the English to Turkish specific improvements
            
            // Ensure options are set correctly for English to Turkish
            options ??= new TranslationOptions { SourceLanguage = "en", TargetLanguage = "tr" };
            options.SourceLanguage = "en";
            options.TargetLanguage = "tr";
            
            // Use the Azure-specific service if available
            if (translationService is Services.AzureTranslationService azureService)
            {
                return await azureService.TranslateTextWithEnhancementsAsync(
                    text, options, progressCallback, cancellationToken);
            }
            
            // Fallback to regular translation if the service doesn't support enhancements
            return await translationService.TranslateTextAsync(text, options, cancellationToken);
        }
        
        /// <summary>
        /// Translates transcription with specific English to Turkish enhancements
        /// </summary>
        public static async Task<TranslationResult> TranslateTranscriptionEnglishToTurkish(
            this ITranslationService translationService,
            TranscriptionResult transcriptionResult,
            TranslationOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Ensure options are set correctly
            options ??= new TranslationOptions { SourceLanguage = "en", TargetLanguage = "tr" };
            options.SourceLanguage = "en";
            options.TargetLanguage = "tr";
            
            // Use the Azure-specific service if available
            if (translationService is Services.AzureTranslationService azureService)
            {
                return await azureService.TranslateTranscriptionWithEnhancementsAsync(
                    transcriptionResult, options, progressCallback, cancellationToken);
            }
            
            // Fallback to regular translation
            return await translationService.TranslateTranscriptionAsync(
                transcriptionResult, options, progressCallback, cancellationToken);
        }
        
        /// <summary>
        /// Adds Turkish-specific configuration to translation options
        /// </summary>
        public static TranslationOptions ConfigureForTurkish(this TranslationOptions options)
        {
            // Setup custom parameters specific to Turkish
            options.TargetLanguage = "tr";
            options.CustomParameters["textType"] = "plain";
            options.CustomParameters["profanityAction"] = "marked";
            
            return options;
        }
    }
}
