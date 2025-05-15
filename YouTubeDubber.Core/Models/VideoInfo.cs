using System;

namespace YouTubeDubber.Core.Models
{
    /// <summary>
    /// Represents information about a YouTube video
    /// </summary>
    public class VideoInfo
    {        /// <summary>
        /// The unique identifier of the YouTube video
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The title of the YouTube video
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The author/channel name of the video
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// The duration of the video
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The URL of the thumbnail image
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// The local file path where the video is downloaded
        /// </summary>
        public string? LocalFilePath { get; set; }
        
        /// <summary>
        /// The date when the video was published
        /// </summary>
        public DateTimeOffset UploadDate { get; set; }

        /// <summary>
        /// The description of the video
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// The view count of the video
        /// </summary>
        public long? ViewCount { get; set; }
    }
}