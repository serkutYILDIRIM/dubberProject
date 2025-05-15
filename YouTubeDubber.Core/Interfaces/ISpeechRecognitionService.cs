using System;
using System.Threading;
using System.Threading.Tasks;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.Core.Interfaces
{
    /// <summary>
    /// Interface for speech recognition services
    /// </summary>
    public interface ISpeechRecognitionService
    {
        /// <summary>
        /// Transcribes the audio file to text
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file to transcribe</param>
        /// <param name="options">Speech recognition configuration options</param>
        /// <param name="progressCallback">Optional callback to report transcription progress</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The transcription result with text and timing information</returns>
        Task<TranscriptionResult> TranscribeAudioFileAsync(
            string audioFilePath,
            SpeechRecognitionOptions? options = null,
            IProgress<double>? progressCallback = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if the speech service credentials are valid
        /// </summary>
        /// <param name="options">Speech recognition configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the credentials are valid, otherwise false</returns>
        Task<bool> ValidateCredentialsAsync(
            SpeechRecognitionOptions options,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Saves the transcription result to a file
        /// </summary>
        /// <param name="transcriptionResult">The transcription result to save</param>
        /// <param name="outputFilePath">Path where the transcription should be saved</param>
        /// <param name="format">Format to save the transcription (e.g., "txt", "srt", "json")</param>
        /// <returns>The path to the saved transcription file</returns>
        Task<string> SaveTranscriptionAsync(
            TranscriptionResult transcriptionResult,
            string outputFilePath,
            string format = "txt");
    }
}
