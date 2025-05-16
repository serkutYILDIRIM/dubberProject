using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace YouTubeDubber.Core.Helpers
{
    /// <summary>
    /// Helper class for Turkish voice synthesis
    /// </summary>
    public static class TurkishVoiceHelper
    {
        /// <summary>
        /// Turkish voice names available in Azure Cognitive Services
        /// </summary>
        public static readonly Dictionary<string, string> TurkishVoices = new Dictionary<string, string>
        {
            { "tr-TR-AhmetNeural", "Ahmet (Erkek, Neural)" },
            { "tr-TR-EmelNeural", "Emel (Kadın, Neural)" },
            { "tr-TR-OsmanNeural", "Osman (Erkek, Neural)" },
            { "tr-TR-FilizNeural", "Filiz (Kadın, Neural)" },
            // Standard voices
            { "tr-TR-Filiz", "Filiz (Kadın, Standard)" },
            { "tr-TR-Server", "Server (Erkek, Standard)" }
        };

        /// <summary>
        /// Gets the recommended male Turkish voice
        /// </summary>
        public static string GetRecommendedMaleVoice() => "tr-TR-AhmetNeural";

        /// <summary>
        /// Gets the recommended female Turkish voice
        /// </summary>
        public static string GetRecommendedFemaleVoice() => "tr-TR-EmelNeural";

        /// <summary>
        /// Formats a text for Turkish speech synthesis
        /// </summary>
        public static string FormatTextForTurkishSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Apply specialized formatting for Turkish speech
            var result = text;

            // Fix specific letter pronunciations for TTS
            result = FixTurkishTtsPronunciation(result);

            // Add pauses at commas and periods to improve naturalness
            result = AddPausesForTTS(result);

            // Add emphasis on key words (especially for dubbing)
            result = AddEmphasisForKeyWords(result);

            return result;
        }

        /// <summary>
        /// Fix Turkish specific pronunciation issues in TTS
        /// </summary>
        private static string FixTurkishTtsPronunciation(string text)
        {
            var result = text;

            // Fix number pronunciations - TTS can have issues with some formats
            result = Regex.Replace(result, @"(\d+)\.(\d+)", "$1 nokta $2"); // Convert decimal points
            result = Regex.Replace(result, @"(\d{1,3})(?=(\d{3})+(?!\d))", "$1,"); // Add thousand separators
            
            // Fix abbreviation pronunciations 
            var commonAbbreviations = new Dictionary<string, string>
            {
                { "Dr.", "Doktor" },
                { "Prof.", "Profesör" },
                { "vb.", "ve benzeri" },
                { "vs.", "vesaire" },
                { "örn.", "örneğin" },
                { "yy.", "yüzyıl" },
                { "TL", "Türk Lirası" }
            };

            foreach (var abbr in commonAbbreviations)
            {
                result = Regex.Replace(result, $@"\b{abbr.Key}\b", abbr.Value);
            }

            // Fix specific letter combinations that TTS might mispronounce
            result = result.Replace("ae", "a-e")
                           .Replace("ao", "a-o")
                           .Replace("eı", "e-ı");

            return result;
        }

        /// <summary>
        /// Add strategic pauses for more natural-sounding speech
        /// </summary>
        private static string AddPausesForTTS(string text)
        {
            // Add slight pause after commas
            var result = Regex.Replace(text, @",\s*", ", ");

            // Add medium pause after periods when not ending a sentence
            result = Regex.Replace(result, @"\.\s+(?=[a-zçğıöşüA-ZÇĞİÖŞÜ])", ". ");

            // Add longer pause after sentence endings
            result = Regex.Replace(result, @"(?<=[.!?])\s+", " ");

            return result;
        }

        /// <summary>
        /// Add emphasis on important words for dubbing
        /// </summary>
        private static string AddEmphasisForKeyWords(string text)
        {
            var result = text;

            // Find potential emphasis words (often important for meaning)
            var emphasisPatterns = new List<string>
            {
                @"\b(?:asla|kesinlikle|mutlaka|muhakkak|hiçbir zaman|her zaman)\b",  // Absolutes
                @"\b(?:şimdi|hemen|derhal|acilen)\b",                                // Urgency
                @"\b(?:çok|son derece|fazlasıyla|aşırı)\b"                           // Intensity
            };

            foreach (var pattern in emphasisPatterns)
            {
                result = Regex.Replace(result, pattern, match => match.Value);
            }

            return result;
        }

        /// <summary>
        /// Generates SSML (Speech Synthesis Markup Language) for Turkish text
        /// </summary>
        public static string GenerateTurkishSSML(string text, string voiceName, float rate = 1.0f, float pitch = 0f)
        {
            // Sanitize the input for XML
            string sanitized = text.Replace("&", "&amp;")
                                  .Replace("<", "&lt;")
                                  .Replace(">", "&gt;")
                                  .Replace("\"", "&quot;")
                                  .Replace("'", "&apos;");

            var ssmlBuilder = new StringBuilder();
            ssmlBuilder.AppendLine("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"tr-TR\">");
            ssmlBuilder.AppendLine($"<voice name=\"{voiceName}\">");
            
            // Add prosody settings for rate and pitch
            ssmlBuilder.AppendLine($"<prosody rate=\"{rate}\" pitch=\"{pitch}%\">");
            
            // Add the text with proper breaks
            var sentences = Regex.Split(sanitized, @"(?<=[.!?])\s+");
            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;
                    
                ssmlBuilder.AppendLine(sentence);
                ssmlBuilder.AppendLine("<break strength=\"medium\"/>");
            }
            
            ssmlBuilder.AppendLine("</prosody>");
            ssmlBuilder.AppendLine("</voice>");
            ssmlBuilder.AppendLine("</speak>");

            return ssmlBuilder.ToString();
        }
    }
}
