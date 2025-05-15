namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Configuration options for speech recognition services
    /// </summary>
    public class SpeechRecognitionOptions
    {
        /// <summary>
        /// The API key for the speech service
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// The region/location of the speech service (e.g., "westus", "eastus")
        /// </summary>
        public string Region { get; set; } = string.Empty;
        
        /// <summary>
        /// The language code for speech recognition (e.g., "en-US")
        /// </summary>
        public string LanguageCode { get; set; } = "en-US";
        
        /// <summary>
        /// Whether to enable word-level timestamps
        /// </summary>
        public bool EnableWordLevelTimestamps { get; set; } = false;
        
        /// <summary>
        /// Whether to use continuous recognition for longer audio files
        /// </summary>
        public bool UseContinuousRecognition { get; set; } = true;
        
        /// <summary>
        /// Maximum duration in seconds for each audio chunk when processing large files
        /// </summary>
        public int MaxAudioChunkDuration { get; set; } = 60;
        
        /// <summary>
        /// Whether to filter out profanity in the transcription
        /// </summary>
        public bool FilterProfanity { get; set; } = false;
        
        /// <summary>
        /// Whether to include speaker identification in the transcription
        /// </summary>
        public bool IdentifySpeakers { get; set; } = false;
        
        /// <summary>
        /// Maximum number of speakers to identify (if speaker identification is enabled)
        /// </summary>
        public int MaxSpeakers { get; set; } = 2;
    }
}
