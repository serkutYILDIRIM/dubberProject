 using System;
using System.Collections.Generic;

namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Represents the result of a text translation
    /// </summary>
    public class TranslationResult
    {
        /// <summary>
        /// The original text in the source language
        /// </summary>
        public string SourceText { get; set; } = string.Empty;
        
        /// <summary>
        /// The translated text in the target language
        /// </summary>
        public string TranslatedText { get; set; } = string.Empty;
        
        /// <summary>
        /// The source language code (e.g., "en" for English)
        /// </summary>
        public string SourceLanguage { get; set; } = "en";
        
        /// <summary>
        /// The target language code (e.g., "tr" for Turkish)
        /// </summary>
        public string TargetLanguage { get; set; } = "tr";
        
        /// <summary>
        /// Timestamp when the translation was completed
        /// </summary>
        public DateTime TranslationTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Translated version of transcription segments with timing information preserved
        /// </summary>
        public List<TranslatedSegment>? TranslatedSegments { get; set; }
        
        /// <summary>
        /// Count of segments for easier property access
        /// </summary>
        public int Count => TranslatedSegments?.Count ?? 0;
    }
    
    /// <summary>
    /// Represents a translated segment with preserved timing information
    /// </summary>
    public class TranslatedSegment
    {
        /// <summary>
        /// The original text in the source language
        /// </summary>
        public string SourceText { get; set; } = string.Empty;
        
        /// <summary>
        /// The translated text in the target language
        /// </summary>
        public string TranslatedText { get; set; } = string.Empty;
        
        /// <summary>
        /// Start time of this segment in the audio (seconds)
        /// </summary>
        public double StartTime { get; set; }
        
        /// <summary>
        /// End time of this segment in the audio (seconds)
        /// </summary>
        public double EndTime { get; set; }
        
        /// <summary>
        /// Confidence score of the original recognition (0.0 to 1.0)
        /// </summary>
        public double SourceConfidence { get; set; }
    }
}
