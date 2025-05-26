using System;
using System.Collections.Generic;

namespace YouTubeDubber.Core.Helpers
{
    /// <summary>
    /// Contains offline translation dictionaries for when online translation services are unavailable
    /// </summary>
    public static class OfflineTranslations
    {
        /// <summary>
        /// Common English to Turkish translations for offline fallback
        /// </summary>
        public static readonly Dictionary<string, string> CommonEnglishTurkishPhrases = new(StringComparer.OrdinalIgnoreCase)
        {
            // Greetings and common phrases
            { "hello", "Merhaba" },
            { "hi", "Selam" },
            { "good morning", "Günaydın" },
            { "good afternoon", "İyi günler" },
            { "good evening", "İyi akşamlar" },
            { "good night", "İyi geceler" },
            { "goodbye", "Hoşça kal" },
            { "bye", "Görüşürüz" },
            { "thank you", "Teşekkür ederim" },
            { "thanks", "Teşekkürler" },
            { "please", "Lütfen" },
            { "you're welcome", "Rica ederim" },
            { "excuse me", "Affedersiniz" },
            { "sorry", "Özür dilerim" },
            { "yes", "Evet" },
            { "no", "Hayır" },
            { "maybe", "Belki" },
            
            // Common questions
            { "how are you", "Nasılsınız" },
            { "what is your name", "Adınız ne" },
            { "where are you from", "Nerelisiniz" },
            { "how old are you", "Kaç yaşındasınız" },
            { "do you speak english", "İngilizce biliyor musunuz" },
            { "do you speak turkish", "Türkçe biliyor musunuz" },
            { "where is the toilet", "Tuvalet nerede" },
            { "how much is this", "Bunun fiyatı ne kadar" },
            { "what time is it", "Saat kaç" },
            
            // Common responses
            { "i don't understand", "Anlamıyorum" },
            { "i don't know", "Bilmiyorum" },
            { "i don't speak turkish", "Türkçe bilmiyorum" },
            { "i am from", "Ben" },
            { "i am", "Ben" },
            { "my name is", "Benim adım" },
            
            // Days of the week
            { "monday", "Pazartesi" },
            { "tuesday", "Salı" },
            { "wednesday", "Çarşamba" },
            { "thursday", "Perşembe" },
            { "friday", "Cuma" },
            { "saturday", "Cumartesi" },
            { "sunday", "Pazar" },
            
            // Months
            { "january", "Ocak" },
            { "february", "Şubat" },
            { "march", "Mart" },
            { "april", "Nisan" },
            { "may", "Mayıs" },
            { "june", "Haziran" },
            { "july", "Temmuz" },
            { "august", "Ağustos" },
            { "september", "Eylül" },
            { "october", "Ekim" },
            { "november", "Kasım" },
            { "december", "Aralık" },
            
            // Numbers
            { "one", "Bir" },
            { "two", "İki" },
            { "three", "Üç" },
            { "four", "Dört" },
            { "five", "Beş" },
            { "six", "Altı" },
            { "seven", "Yedi" },
            { "eight", "Sekiz" },
            { "nine", "Dokuz" },
            { "ten", "On" },
            
            // Time-related
            { "today", "Bugün" },
            { "tomorrow", "Yarın" },
            { "yesterday", "Dün" },
            { "now", "Şimdi" },
            { "later", "Sonra" },
            { "morning", "Sabah" },
            { "afternoon", "Öğleden sonra" },
            { "evening", "Akşam" },
            { "night", "Gece" },
            
            // Places
            { "airport", "Havalimanı" },
            { "hotel", "Otel" },
            { "restaurant", "Restoran" },
            { "hospital", "Hastane" },
            { "pharmacy", "Eczane" },
            { "bank", "Banka" },
            { "shop", "Mağaza" },
            { "market", "Pazar" },
            { "supermarket", "Süpermarket" },
            { "beach", "Plaj" },
            { "city", "Şehir" },
            { "town", "Kasaba" },
            { "village", "Köy" },
            { "street", "Cadde" },
            { "road", "Yol" },
            
            // Food and drinks
            { "breakfast", "Kahvaltı" },
            { "lunch", "Öğle yemeği" },
            { "dinner", "Akşam yemeği" },
            { "water", "Su" },
            { "tea", "Çay" },
            { "coffee", "Kahve" },
            { "juice", "Meyve suyu" },
            { "beer", "Bira" },
            { "wine", "Şarap" },
            { "milk", "Süt" },
            { "bread", "Ekmek" },
            { "meat", "Et" },
            { "chicken", "Tavuk" },
            { "fish", "Balık" },
            { "fruit", "Meyve" },
            { "vegetable", "Sebze" },
            
            // Transportation
            { "bus", "Otobüs" },
            { "train", "Tren" },
            { "taxi", "Taksi" },
            { "car", "Araba" },
            { "bicycle", "Bisiklet" },
            { "boat", "Bot" },
            { "plane", "Uçak" },
            
            // Weather
            { "hot", "Sıcak" },
            { "cold", "Soğuk" },
            { "warm", "Ilık" },
            { "cool", "Serin" },
            { "sunny", "Güneşli" },
            { "rainy", "Yağmurlu" },
            { "cloudy", "Bulutlu" },
            { "windy", "Rüzgarlı" },
            { "snow", "Kar" },
            { "rain", "Yağmur" },
            
            // Special video dubbing terms
            { "video", "Video" },
            { "audio", "Ses" },
            { "translate", "Çevir" },
            { "translation", "Çeviri" },
            { "subtitle", "Altyazı" },
            { "dubbing", "Dublaj" },
            { "voice", "Ses" },
            { "record", "Kayıt" },
            { "pause", "Duraklat" },
            { "play", "Oynat" },
            { "stop", "Durdur" },
            
            // Error messages
            { "error", "Hata" },
            { "warning", "Uyarı" },
            { "not found", "Bulunamadı" },
            { "disconnected", "Bağlantı kesildi" },
            { "try again", "Tekrar deneyin" },
            { "offline", "Çevrimdışı" },
            { "online", "Çevrimiçi" },
            { "loading", "Yükleniyor" },
            { "processing", "İşleniyor" },
            { "completed", "Tamamlandı" },
            { "failed", "Başarısız" }
        };
    }
}
