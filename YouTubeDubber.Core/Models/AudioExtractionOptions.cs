using System;

namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Options for audio extraction from video
    /// </summary>
    public class AudioExtractionOptions
    {
        /// <summary>
        /// The format of the extracted audio file
        /// </summary>
        public AudioFormat Format { get; set; } = AudioFormat.Wav;
        
        /// <summary>
        /// The audio bitrate in kbps (e.g., 128, 192, 256)
        /// </summary>
        public int Bitrate { get; set; } = 128;
        
        /// <summary>
        /// The sample rate in Hz (e.g., 16000, 22050, 44100, 48000)
        /// </summary>
        public int SampleRate { get; set; } = 16000;
        
        /// <summary>
        /// The number of audio channels (1 for mono, 2 for stereo)
        /// </summary>
        public int Channels { get; set; } = 1;
        
        /// <summary>
        /// Apply noise reduction to improve speech recognition quality
        /// </summary>
        public bool ApplyNoiseReduction { get; set; } = false;
        
        /// <summary>
        /// Normalize audio levels to optimize for speech recognition
        /// </summary>
        public bool NormalizeAudio { get; set; } = true;
        
        /// <summary>
        /// Timestamp offset in seconds to start extracting audio (0 for beginning)
        /// </summary>
        public double StartTime { get; set; } = 0;
        
        /// <summary>
        /// Duration in seconds to extract (0 for entire file)
        /// </summary>
        public double Duration { get; set; } = 0;
    }
    
    /// <summary>
    /// Audio format options for extraction
    /// </summary>
    public enum AudioFormat
    {
        /// <summary>
        /// WAV format - Uncompressed, best quality for speech processing
        /// </summary>
        Wav,
        
        /// <summary>
        /// MP3 format - Compressed, smaller file size
        /// </summary>
        Mp3,
        
        /// <summary>
        /// FLAC format - Lossless compression, good quality with smaller size
        /// </summary>
        Flac,
        
        /// <summary>
        /// OGG format - Compressed format with good speech quality
        /// </summary>
        Ogg
    }
}
