using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace YouTubeDubber.Core.Helpers
{
    /// <summary>
    /// Specialized helper class for English to Turkish translation improvements
    /// </summary>
    public static class EnglishTurkishHelper
    {
        /// <summary>
        /// English idioms and their Turkish equivalents
        /// </summary>
        private static readonly Dictionary<string, string> IdiomDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common English idioms that need special translation in Turkish
            { "break a leg", "başarılar dilerim" },
            { "it's raining cats and dogs", "bardaktan boşanırcasına yağmur yağıyor" },
            { "piece of cake", "çocuk oyuncağı" },
            { "cost an arm and a leg", "göz kadar pahalı" },
            { "once in a blue moon", "yılda yılan ayda bir" },
            { "under the weather", "keyifsiz" },
            { "speak of the devil", "iti an çomağı hazırla" },
            { "hit the road", "yola koyulmak" },
            { "break the ice", "buzları eritmek" },
            { "cut to the chase", "sadede gelmek" },
            { "beat around the bush", "lafı dolandırmak" },
            { "the best of both worlds", "hem nalına hem mıhına" },
            { "get your act together", "kendini toparlamak" },
            { "hang in there", "dayanmak" },
            { "go the extra mile", "fazladan çaba göstermek" },
            { "off the hook", "yırtmak" },
            { "on the ball", "işinin ehli" },
            { "rule of thumb", "altın kural" },
            { "no pain no gain", "emek olmadan yemek olmaz" },
            { "kill two birds with one stone", "bir taşla iki kuş vurmak" },
            { "in the same boat", "aynı gemide olmak" },
            { "bite the bullet", "acıya katlanmak" },
            { "a blessing in disguise", "hayırlısı olmuş" },
            { "hit the nail on the head", "tam üstüne basmak" },
            { "in hot water", "başı belada olmak" },
            { "give someone the cold shoulder", "yüz vermemek" },
            { "actions speak louder than words", "laf değil icraat önemli" },
            { "all ears", "kulak kesilmek" },
            { "barking up the wrong tree", "yanlış kapı çalmak" },
            { "by the skin of your teeth", "kıl payı" },
            { "get a taste of your own medicine", "kendi silahıyla vurulmak" },
            { "go down in flames", "çuvallamak" },
            { "pull yourself together", "kendine gelmek" },
            { "take a rain check", "başka bir zamana ertelemek" },
            { "the last straw", "bardağı taşıran son damla" },
            { "wild goose chase", "boş yere peşinden koşmak" },
            { "cross that bridge when you come to it", "o günün derdini o gün çekmek" },
            { "jump the gun", "erken davranmak" },
            { "out of the blue", "pat diye" }
        };

        /// <summary>
        /// Turkish pronouns that should be aligned with verb placement
        /// </summary>
        private static readonly Dictionary<string, string> EnglishTurkishPronouns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "I ", "Ben " },
            { "you ", "Sen " },
            { "he ", "O " },
            { "she ", "O " },
            { "it ", "O " },
            { "we ", "Biz " },
            { "they ", "Onlar " },
            { "my ", "Benim " },
            { "your ", "Senin " },
            { "his ", "Onun " },
            { "her ", "Onun " },
            { "its ", "Onun " },
            { "our ", "Bizim " },
            { "their ", "Onların " }
        };

        /// <summary>
        /// Words that should be capitalized in Turkish (mostly place and proper names)
        /// </summary>
        private static readonly HashSet<string> TurkishCapitalizationExceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "türkiye", "türk", "istanbul", "ankara", "izmir", "antalya", "amerika", "avrupa", 
            "asya", "afrika", "ingiltere", "almanya", "fransa", "japonya", "çin", "rusya", 
            "pazartesi", "salı", "çarşamba", "perşembe", "cuma", "cumartesi", "pazar",
            "ocak", "şubat", "mart", "nisan", "mayıs", "haziran", "temmuz", "ağustos", "eylül", "ekim", "kasım", "aralık"
        };

        /// <summary>
        /// List of common Turkish suffixes that need to be properly attached (without space)
        /// </summary>
        private static readonly List<string> TurkishSuffixes = new List<string>
        {
            "de", "da", "te", "ta", "den", "dan", "ten", "tan", "ye", "ya", "e", "a", "i", "ı", "u", "ü", "ler", "lar", "ki"
        };

        /// <summary>
        /// Process text for idiomatic expressions before translation
        /// </summary>
        public static string ProcessIdioms(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Look for known idioms in the text
            foreach (var idiom in IdiomDictionary)
            {
                // Use regex to match whole idiom expressions (not partial matches)
                string pattern = $@"\b{Regex.Escape(idiom.Key)}\b";
                
                // Mark the idiom with special tags to preserve it through translation
                text = Regex.Replace(text, pattern, $"[[IDIOM:{idiom.Key}]]", RegexOptions.IgnoreCase);
            }

            return text;
        }

        /// <summary>
        /// Replace any idiom placeholders with their Turkish translations
        /// </summary>
        public static string ReplaceIdiomPlaceholders(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Find all idiom placeholders and replace them with Turkish equivalents
            string pattern = @"\[\[IDIOM:(.*?)\]\]";
            return Regex.Replace(text, pattern, match => 
            {
                string idiom = match.Groups[1].Value;
                return IdiomDictionary.TryGetValue(idiom, out string translation) 
                    ? translation 
                    : idiom; // fallback to original if not found
            });
        }

        /// <summary>
        /// Apply Turkish capitalization rules
        /// </summary>
        public static string ApplyTurkishCapitalization(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Split into sentences
            string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            
            for (int i = 0; i < sentences.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(sentences[i]))
                    continue;

                // Capitalize first character of each sentence
                if (sentences[i].Length > 0)
                {
                    sentences[i] = char.ToUpper(sentences[i][0]) + sentences[i].Substring(1);
                }

                // Check for proper nouns and place names that should be capitalized
                foreach (var word in TurkishCapitalizationExceptions)
                {
                    string pattern = $@"\b{Regex.Escape(word)}\b";
                    sentences[i] = Regex.Replace(sentences[i], pattern, m => 
                        char.ToUpper(m.Value[0]) + m.Value.Substring(1), 
                        RegexOptions.IgnoreCase);
                }
            }

            return string.Join(" ", sentences);
        }

        /// <summary>
        /// Fix Turkish i/İ letter handling - Turkish has two types of "i" letters
        /// </summary>
        public static string FixTurkishILetters(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Turkish specific letter conversions: i->İ and I->ı
            var result = text;
            
            // This is a complex issue - we need to handle it word by word
            var words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
                    continue;

                // Check if the word is in all caps
                bool isAllCaps = word.ToUpper() == word && word.Length > 1;

                // If it's all caps, make sure I/İ are correct
                if (isAllCaps)
                {
                    // In all-caps Turkish words, "I" should be used instead of "İ"
                    words[i] = word.Replace("İ", "I");
                }
                else
                {
                    // In normal words, lowercase "i" gets a dot in Turkish, but uppercase is "İ"
                    // Only convert the first letter if it's at the beginning of a word
                    if (word[0] == 'i')
                    {
                        words[i] = 'İ' + word.Substring(1);
                    }
                    else if (word[0] == 'I')
                    {
                        words[i] = 'ı' + word.Substring(1);
                    }
                }
            }

            return string.Join(" ", words);
        }

        /// <summary>
        /// Fix spacing for Turkish suffixes
        /// </summary>
        public static string FixTurkishSuffixSpacing(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string result = text;
            
            // For suffixes that must attach to the word without a space
            foreach (var suffix in TurkishSuffixes)
            {
                result = Regex.Replace(result, $@"\s+({suffix})\b", "$1");
            }

            return result;
        }

        /// <summary>
        /// Transform word order for better Turkish expression where subject-object-verb order is common
        /// </summary>
        public static string OptimizeWordOrder(string text)
        {
            // This is a complex NLP task that would require a full parser
            // As a simplified approach, we'll handle common pronoun-verb structures
            
            // For now, we'll leave this as a placeholder for future enhancement
            return text;
        }
    }
}
