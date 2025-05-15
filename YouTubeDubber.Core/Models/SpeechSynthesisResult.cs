using System;
using System.Collections.Generic;

namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Represents the result of a text-to-speech synthesis operation
    /// </summary>
    public class SpeechSynthesisResult
    {
        /// <summary>
        /// The input text that was synthesized
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// The path to the generated audio file
        /// </summary>
        public string AudioFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// The duration of the generated audio in seconds
        /// </summary>
        public double DurationInSeconds { get; set; }
        
        /// <summary>
        /// The language code used for synthesis
        /// </summary>
        public string LanguageCode { get; set; } = string.Empty;
        
        /// <summary>
        /// The voice name used for synthesis
        /// </summary>
        public string VoiceName { get; set; } = string.Empty;
        
        /// <summary>
        /// The time when the synthesis was performed
        /// </summary>
        public DateTime SynthesisTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// For segmented synthesis, contains individual segment timing information
        /// </summary>
        public List<SpeechSynthesisSegment> Segments { get; set; } = new List<SpeechSynthesisSegment>();
    }
    
    /// <summary>
    /// Represents a segment of synthesized speech with timing information
    /// </summary>
    public class SpeechSynthesisSegment
    {
        /// <summary>
        /// The text of this segment
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// The start time of this segment in seconds
        /// </summary>
        public double StartTime { get; set; }
        
        /// <summary>
        /// The end time of this segment in seconds
        /// </summary>
        public double EndTime { get; set; }
        
        /// <summary>
        /// The audio data for this segment
        /// </summary>
        public byte[]? AudioData { get; set; }
    }
}
