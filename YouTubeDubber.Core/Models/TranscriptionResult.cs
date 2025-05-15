using System;
using System.Collections.Generic;

namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Represents a transcription segment with text and timing information
    /// </summary>
    public class TranscriptionSegment
    {
        /// <summary>
        /// The transcribed text
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// Start time of this segment in the audio (seconds)
        /// </summary>
        public double StartTime { get; set; }
        
        /// <summary>
        /// End time of this segment in the audio (seconds)
        /// </summary>
        public double EndTime { get; set; }
        
        /// <summary>
        /// Confidence score of the transcription (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }
    }
    
    /// <summary>
    /// Represents the complete transcription result of an audio file
    /// </summary>
    public class TranscriptionResult
    {
        /// <summary>
        /// List of transcribed segments with timing information
        /// </summary>
        public List<TranscriptionSegment> Segments { get; set; } = new List<TranscriptionSegment>();
        
        /// <summary>
        /// The full transcribed text without timing information
        /// </summary>
        public string FullText 
        { 
            get
            {
                return string.Join(" ", Segments.ConvertAll(segment => segment.Text));
            }
        }
        
        /// <summary>
        /// Language code of the transcription (e.g., "en-US")
        /// </summary>
        public string LanguageCode { get; set; } = "en-US";
        
        /// <summary>
        /// Original audio file path that was transcribed
        /// </summary>
        public string AudioFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp when the transcription was completed
        /// </summary>
        public DateTime TranscriptionTime { get; set; } = DateTime.Now;
    }
}
