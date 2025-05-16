using System;

namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Options for configuring Turkish video dubbing
    /// </summary>
    public class TurkishDubbingOptions
    {
        /// <summary>
        /// Whether to extract and preserve background sounds/music
        /// </summary>
        public bool PreserveBackgroundSounds { get; set; } = true;
        
        /// <summary>
        /// Whether to add Turkish subtitles to the video
        /// </summary>
        public bool AddSubtitles { get; set; } = false;
        
        /// <summary>
        /// Path to the Turkish subtitles file (SRT, ASS, or SSA)
        /// </summary>
        public string? SubtitlesFilePath { get; set; } = null;
        
        /// <summary>
        /// Audio mixing profile to use
        /// Available profiles: "background", "voice", "music", "balanced", "voice-centered"
        /// </summary>
        public string MixingProfile { get; set; } = "balanced";
        
        /// <summary>
        /// Custom background volume (0.0 to 1.0)
        /// Only used when MixingProfile is set to "custom"
        /// </summary>
        public float CustomBackgroundVolume { get; set; } = 0.3f;
        
        /// <summary>
        /// Custom voice volume (0.0 to 2.0)
        /// Only used when MixingProfile is set to "custom"
        /// </summary>
        public float CustomVoiceVolume { get; set; } = 1.0f;
        
        /// <summary>
        /// Custom ducking amount (0.5 to 1.0)
        /// Controls how much background audio is reduced when speech is present
        /// Only used when MixingProfile is set to "custom"
        /// </summary>
        public float CustomDucking { get; set; } = 0.8f;
        
        /// <summary>
        /// Output video quality (1-31, lower is better)
        /// </summary>
        public int VideoQuality { get; set; } = 18;
        
        /// <summary>
        /// Output video format
        /// </summary>
        public string OutputFormat { get; set; } = "mp4";
    }
}
