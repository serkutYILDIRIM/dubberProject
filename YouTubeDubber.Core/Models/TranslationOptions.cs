using System.Collections.Generic;

namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Configuration options for translation services
    /// </summary>
    public class TranslationOptions
    {
        /// <summary>
        /// The API key for the translation service
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// The region/location of the translation service (e.g., "global", "westus")
        /// </summary>
        public string Region { get; set; } = "global";
        
        /// <summary>
        /// The source language code (e.g., "en" for English)
        /// </summary>
        public string SourceLanguage { get; set; } = "en";
        
        /// <summary>
        /// The target language code (e.g., "tr" for Turkish)
        /// </summary>
        public string TargetLanguage { get; set; } = "tr";
        
        /// <summary>
        /// Whether to preserve the original formatting as much as possible
        /// </summary>
        public bool PreserveFormatting { get; set; } = true;
        
        /// <summary>
        /// Whether to include document structure information if available
        /// </summary>
        public bool IncludeDocumentStructure { get; set; } = false;
        
        /// <summary>
        /// Whether to include alignment information for source and target texts
        /// </summary>
        public bool IncludeAlignment { get; set; } = false;
        
        /// <summary>
        /// Dictionary of custom parameters for the translation service
        /// </summary>
        public Dictionary<string, string> CustomParameters { get; set; } = new Dictionary<string, string>();
    }
}
