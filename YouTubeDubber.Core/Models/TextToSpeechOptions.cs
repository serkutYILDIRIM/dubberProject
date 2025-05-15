using System;

namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Options for configuring text-to-speech synthesis
    /// </summary>
    public class TextToSpeechOptions
    {
        /// <summary>
        /// The API key for the speech service
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// The region for the speech service (e.g., "westus", "eastus")
        /// </summary>
        public string Region { get; set; } = string.Empty;
        
        /// <summary>
        /// The language code for synthesis (e.g., "tr-TR" for Turkish)
        /// </summary>
        public string LanguageCode { get; set; } = "tr-TR";
        
        /// <summary>
        /// The voice name to use for synthesis (e.g., "tr-TR-AhmetNeural")
        /// </summary>
        public string VoiceName { get; set; } = string.Empty;
        
        /// <summary>
        /// The speaking style (e.g., "general", "cheerful", "sad")
        /// Only applicable for neural voices
        /// </summary>
        public string SpeakingStyle { get; set; } = "general";
        
        /// <summary>
        /// The speaking rate (0.5 to 2.0, 1.0 is normal speed)
        /// </summary>
        public float SpeakingRate { get; set; } = 1.0f;
        
        /// <summary>
        /// The output audio format (e.g., "wav", "mp3")
        /// </summary>
        public string OutputFormat { get; set; } = "wav";
        
        /// <summary>
        /// The audio sampling rate in Hz (e.g., 16000, 24000)
        /// </summary>
        public int SamplingRate { get; set; } = 24000;

        /// <summary>
        /// Whether to use SSML for advanced speech synthesis features
        /// </summary>
        public bool UseSSML { get; set; } = false;
        
        /// <summary>
        /// The pitch adjustment (-50% to 50%)
        /// </summary>
        public int PitchAdjustment { get; set; } = 0;
    }
}
