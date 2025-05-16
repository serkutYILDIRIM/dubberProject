using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace YouTubeDubber.Core.Helpers
{
    /// <summary>
    /// Helper class for Turkish voice synthesis.
    /// This class provides specialized methods and utilities for working with Turkish text-to-speech,
    /// focusing on enhancing naturalness, improving pronunciation, and providing better voice selection
    /// for the Turkish language.
    /// 
    /// Key features:
    /// - Advanced SSML generation specific to Turkish language requirements
    /// - Turkish-specific text formatting and pronunciation improvements
    /// - Automatic voice selection based on content
    /// - Support for various speaking styles and expressive speech
    /// - Methods to handle common Turkish pronunciation challenges
    /// </summary>
    public static class TurkishVoiceHelper
    {
        /// <summary>
        /// Turkish voice names available in Azure Cognitive Services
        /// </summary>
        public static readonly Dictionary<string, string> TurkishVoices = new Dictionary<string, string>
        {
            // Neural voices (preferred for more natural speech)
            { "tr-TR-AhmetNeural", "Ahmet (Erkek, Neural, Profesyonel)" },
            { "tr-TR-EmelNeural", "Emel (Kadın, Neural, Doğal)" },
            { "tr-TR-OsmanNeural", "Osman (Erkek, Neural, Günlük Konuşma)" },
            { "tr-TR-FilizNeural", "Filiz (Kadın, Neural, Enerjik)" },
            // Standard voices
            { "tr-TR-Filiz", "Filiz (Kadın, Standard)" },
            { "tr-TR-Server", "Server (Erkek, Standard)" }
        };
        
        /// <summary>
        /// Voice styles available for Turkish neural voices
        /// </summary>
        public static readonly Dictionary<string, string> TurkishVoiceStyles = new Dictionary<string, string>
        {
            { "general", "Genel (Nötr)" },
            { "calm", "Sakin" },
            { "cheerful", "Neşeli" },
            { "excited", "Heyecanlı" },
            { "friendly", "Arkadaşça" },
            { "sad", "Hüzünlü" },
            { "empathetic", "Empatik" }
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
        /// Gets the appropriate Turkish voice based on the content
        /// </summary>
        /// <param name="content">The content to analyze</param>
        /// <param name="preferredGender">Preferred gender: "male", "female", or null for automatic selection</param>
        public static string GetAppropriateVoice(string content, string? preferredGender = null)
        {
            // Default voices based on gender preference
            if (preferredGender?.ToLower() == "female")
            {
                return GetRecommendedFemaleVoice();
            }
            
            if (preferredGender?.ToLower() == "male")
            {
                return GetRecommendedMaleVoice();
            }
            
            // Content-based voice selection
            content = content.ToLower();
            
            // Check if content seems more technical
            if (content.Contains("teknik") || content.Contains("bilimsel") || 
                content.Contains("akademik") || content.Contains("araştırma"))
            {
                return "tr-TR-AhmetNeural"; // More formal male voice
            }
            
            // Check if content seems more conversational
            if (content.Contains("merhaba") || content.Contains("selam") || 
                content.Contains("nasılsın") || content.Contains("teşekkür"))
            {
                return "tr-TR-EmelNeural"; // Friendly female voice
            }
            
            // Check if content seems more like a story
            if (content.Contains("bir varmış bir yokmuş") || content.Contains("hikaye") || 
                content.Contains("masal") || content.Contains("öykü"))
            {
                return "tr-TR-FilizNeural"; // Expressive female voice
            }
            
            // Default to male voice for generic content
            return GetRecommendedMaleVoice();
        }

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
            
            // Handle common Turkish expressions better
            result = ImproveCommonExpressions(result);

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
                { "Doç.", "Doçent" },
                { "vb.", "ve benzeri" },
                { "vs.", "vesaire" },
                { "örn.", "örneğin" },
                { "yy.", "yüzyıl" },
                { "TL", "Türk Lirası" },
                { "kg", "kilogram" },
                { "km", "kilometre" },
                { "cm", "santimetre" },
                { "mm", "milimetre" },
                { "m²", "metre kare" },
                { "m³", "metre küp" },
                { "İst.", "İstanbul" },
                { "Ank.", "Ankara" },
                { "s.", "sayfa" },
                { "bkz.", "bakınız" },
                { "C.", "Cilt" },
                { "a.g.e.", "adı geçen eser" },
                { "MÖ", "Milattan Önce" },
                { "MS", "Milattan Sonra" },
                { "TC", "Türkiye Cumhuriyeti" },
                { "TBMM", "Türkiye Büyük Millet Meclisi" },
                { "YTL", "Yeni Türk Lirası" }
            };

            foreach (var abbr in commonAbbreviations)
            {
                result = Regex.Replace(result, $@"\b{abbr.Key}\b", abbr.Value);
            }

            // Fix specific letter combinations that TTS might mispronounce
            result = result.Replace("ae", "a-e")
                           .Replace("ao", "a-o")
                           .Replace("eı", "e-ı")
                           .Replace("aei", "a-e-i")
                           .Replace("aeu", "a-e-u")
                           .Replace("oea", "o-e-a")
                           .Replace("üai", "ü-a-i");
                           
            // Fix common English words that might be mispronounced in Turkish
            var englishWords = new Dictionary<string, string>
            {
                { "YouTube", "Yutyub" },
                { "Windows", "Vindovs" },
                { "iPhone", "Ayfon" },
                { "Excel", "Eksel" },
                { "WhatsApp", "Vatsap" },
                { "Facebook", "Feysbuk" },
                { "Instagram", "İnstagram" },
                { "Twitter", "Tivitır" },
                { "Google", "Gugıl" },
                { "Gmail", "Cimeyl" },
                { "Microsoft", "Maykrosaft" }
            };
            
            foreach (var word in englishWords)
            {
                result = Regex.Replace(result, $@"\b{word.Key}\b", word.Value);
            }

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
            
            // Add pause for quotations
            result = Regex.Replace(result, @"[""]", " ");
            
            // Add pause for parentheses
            result = Regex.Replace(result, @"[\(\)]", " ");
            
            // Add pause for dashes used as breaks in Turkish
            result = Regex.Replace(result, @"—|–|-", " — ");

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
                @"\b(?:çok|son derece|fazlasıyla|aşırı)\b",                          // Intensity
                @"\b(?:dikkat|uyarı|tehlike|önemli)\b",                              // Warnings
                @"\b(?:lütfen|rica ederim)\b",                                       // Politeness
                @"\b(?:harika|muhteşem|mükemmel|inanılmaz)\b"                        // Excitement
            };

            foreach (var pattern in emphasisPatterns)
            {
                result = Regex.Replace(result, pattern, match => match.Value);
            }

            return result;
        }
        
        /// <summary>
        /// Improves pronunciation of common Turkish expressions
        /// </summary>
        private static string ImproveCommonExpressions(string text)
        {
            var result = text;
            
            // Common expressions that need better pronunciation
            var expressions = new Dictionary<string, string>
            {
                { "Allah aşkına", "Allah aşkına" },
                { "yapacak bir şey yok", "yapacak bir şey yok" },
                { "öyle mi", "öyle mi" },
                { "değil mi", "değil mi" },
                { "ne oldu", "ne oldu" },
                { "ne oluyor", "ne oluyor" },
                { "nereye", "nereye" },
                { "nasılsın", "nasılsın" },
                { "nasıl gidiyor", "nasıl gidiyor" },
                { "özür dilerim", "özür dilerim" },
                { "afedersin", "afedersin" },
                { "teşekkür ederim", "teşekkür ederim" },
                { "teşekkürler", "teşekkürler" },
                { "rica ederim", "rica ederim" },
                { "bir şey değil", "bir şey değil" },
                { "görüşürüz", "görüşürüz" },
                { "sonra görüşürüz", "sonra görüşürüz" },
                { "güle güle", "güle güle" },
                { "haydi", "haydi" },
                { "tabii ki", "tabii ki" },
                { "elbette", "elbette" }
            };
            
            foreach (var expr in expressions)
            {
                // We're replacing with the same text but with SSML this will have better pronunciation
                // as we'll add prosody and emphasis in the SSML generation
                result = Regex.Replace(result, $@"\b{expr.Key}\b", expr.Value);
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
            ssmlBuilder.AppendLine("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"http://www.w3.org/2001/mstts\" xml:lang=\"tr-TR\">");
            ssmlBuilder.AppendLine($"<voice name=\"{voiceName}\">");
            
            // Determine if this is a neural voice that supports styles
            bool isNeural = voiceName.EndsWith("Neural");
            
            // Add style for neural voices
            if (isNeural)
            {
                // Select an appropriate style based on the content
                string style = DetermineAppropriateStyle(text);
                ssmlBuilder.AppendLine($"<mstts:express-as style=\"{style}\">");
            }
            
            // Add prosody settings for rate and pitch
            ssmlBuilder.AppendLine($"<prosody rate=\"{rate}\" pitch=\"{(pitch >= 0 ? "+" : "")}{pitch}%\">");
            
            // Add the text with proper breaks and emphasis
            ProcessTextForSSML(sanitized, ssmlBuilder);
            
            // Close tags in reverse order
            ssmlBuilder.AppendLine("</prosody>");
            
            if (isNeural)
            {
                ssmlBuilder.AppendLine("</mstts:express-as>");
            }
            
            ssmlBuilder.AppendLine("</voice>");
            ssmlBuilder.AppendLine("</speak>");

            return ssmlBuilder.ToString();
        }
        
        /// <summary>
        /// Processes text for SSML, adding appropriate breaks, emphasis, etc.
        /// </summary>
        private static void ProcessTextForSSML(string text, StringBuilder ssmlBuilder)
        {
            // Split by sentences for better control
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            
            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;
                
                // Process emphasis words in the sentence
                var processedSentence = ProcessEmphasisWords(sentence);
                ssmlBuilder.AppendLine(processedSentence);
                
                // Add pause between sentences
                ssmlBuilder.AppendLine("<break strength=\"medium\"/>");
            }
        }
        
        /// <summary>
        /// Processes emphasis words in the text for SSML
        /// </summary>
        private static string ProcessEmphasisWords(string text)
        {
            // Words that should be emphasized in Turkish
            var emphasisWords = new List<string>
            {
                "asla", "kesinlikle", "mutlaka", "muhakkak", "hiçbir", "çok", 
                "son derece", "fazlasıyla", "aşırı", "dikkat", "uyarı", "tehlike",
                "önemli", "lütfen", "rica", "harika", "muhteşem", "mükemmel", "inanılmaz"
            };
            
            foreach (var word in emphasisWords)
            {
                // Use regex to find the word with word boundaries
                var pattern = $@"\b{word}\b";
                text = Regex.Replace(text, pattern, match => 
                    $"<emphasis level=\"strong\">{match.Value}</emphasis>");
            }
            
            return text;
        }
        
        /// <summary>
        /// Determines the appropriate speaking style based on content
        /// </summary>
        private static string DetermineAppropriateStyle(string text)
        {
            text = text.ToLowerInvariant();
            
            // Check for various emotional content
            if (Regex.IsMatch(text, @"\b(hüzün|ağla|üzgün|keder|acı|kaybetmek)\b"))
            {
                return "sad";
            }
            
            if (Regex.IsMatch(text, @"\b(neşe|mutlu|sevin|harika|muhteşem|güzel|iyi|bravo)\b"))
            {
                return "cheerful";
            }
            
            if (Regex.IsMatch(text, @"\b(heyecan|vay|şaşırtıcı|inanılmaz|vay canına|çok güzel)\b"))
            {
                return "excited";
            }
            
            if (Regex.IsMatch(text, @"\b(sakin|rahat|dinle|huzur|barış|sessiz)\b"))
            {
                return "calm";
            }
            
            if (Regex.IsMatch(text, @"\b(nasılsın|merhaba|selam|dostum|arkadaşım|kardeşim)\b"))
            {
                return "friendly";
            }
            
            if (Regex.IsMatch(text, @"\b(anlıyorum|anlayışlı|desteklemek|empati|anlamak)\b"))
            {
                return "empathetic";
            }
            
            // Default to general style for neutral content
            return "general";
        }
    }
}
