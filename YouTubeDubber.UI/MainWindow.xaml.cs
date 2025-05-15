using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using YouTubeDubber.Core;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{    private readonly IYouTubeService _youTubeService;
    private VideoInfo? _currentVideoInfo;
    private string _downloadsFolderPath;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Create downloads folder in Documents directory
        _downloadsFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "YouTubeDubber", 
            "Downloads");
        
        Directory.CreateDirectory(_downloadsFolderPath);
        
        // Initialize YouTube service
        _youTubeService = new YouTubeService(_downloadsFolderPath);
    }
    
    /// <summary>
    /// Handle the button click event to get video information
    /// </summary>
    private async void BtnGetVideoInfo_Click(object sender, RoutedEventArgs e)
    {
        await ProcessYouTubeUrl();
    }
    
    /// <summary>
    /// Handle enter key in the URL textbox
    /// </summary>
    private async void TxtYouTubeUrl_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await ProcessYouTubeUrl();
        }
    }
    
    /// <summary>    /// Process the YouTube URL entered by the user
    /// </summary>
    private async Task ProcessYouTubeUrl()
    {
        string? url = txtYouTubeUrl.Text?.Trim();
        
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Please enter a YouTube URL", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Validate the URL
        if (!_youTubeService.IsValidYouTubeUrl(url))
        {
            MessageBox.Show("Please enter a valid YouTube URL", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            // Show loading state
            SetLoadingState(true);
            
            // Get video information
            _currentVideoInfo = await _youTubeService.GetVideoInfoAsync(url);
            
            // Display video information
            DisplayVideoInfo(_currentVideoInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error retrieving video information: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Hide loading state
            SetLoadingState(false);
        }
    }
    
    /// <summary>
    /// Display the video information in the UI
    /// </summary>
    private void DisplayVideoInfo(VideoInfo videoInfo)
    {
        if (videoInfo == null) return;
        
        // Set video details
        txtVideoTitle.Text = videoInfo.Title;
        txtVideoAuthor.Text = videoInfo.Author;
        txtVideoDuration.Text = FormatDuration(videoInfo.Duration);
        txtVideoDescription.Text = videoInfo.Description;
        
        // Load thumbnail if available
        if (!string.IsNullOrEmpty(videoInfo.ThumbnailUrl))
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(videoInfo.ThumbnailUrl);
                bitmap.EndInit();
                imgThumbnail.Source = bitmap;
            }
            catch (Exception)
            {
                // If thumbnail loading fails, don't show any image
                imgThumbnail.Source = null;
            }
        }
        
        // Show the video info panel
        videoInfoPanel.Visibility = Visibility.Visible;
        txtNoData.Visibility = Visibility.Collapsed;
    }
    
    /// <summary>
    /// Format the duration of the video as a readable string
    /// </summary>
    private string FormatDuration(TimeSpan duration)
    {
        if (duration.Hours > 0)
        {
            return $"{duration.Hours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
        
        return $"{duration.Minutes}:{duration.Seconds:D2}";
    }
    
    /// <summary>
    /// Set the UI state for loading operations
    /// </summary>
    private void SetLoadingState(bool isLoading)
    {
        // Disable controls during loading
        txtYouTubeUrl.IsEnabled = !isLoading;
        btnGetVideoInfo.IsEnabled = !isLoading;
        
        // If download buttons are visible, disable them too
        if (videoInfoPanel.Visibility == Visibility.Visible)
        {
            btnDownloadVideo.IsEnabled = !isLoading;
            btnDownloadAudio.IsEnabled = !isLoading;
        }
        
        // Set cursor state
        Cursor = isLoading ? Cursors.Wait : null;
    }
    
    /// <summary>
    /// Handle the download video button click
    /// </summary>
    private async void BtnDownloadVideo_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideoInfo == null) return;
          try
        {
            string? url = txtYouTubeUrl.Text?.Trim();
            
            if (string.IsNullOrEmpty(url) || _currentVideoInfo == null)
            {
                MessageBox.Show("No valid video URL or video info available", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Ask user for the save location
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = $"{_currentVideoInfo.Title}.mp4",
                DefaultExt = ".mp4",
                Filter = "MP4 Files (*.mp4)|*.mp4",
                InitialDirectory = _downloadsFolderPath
            };
            
            bool? result = saveFileDialog.ShowDialog(this);
            
            if (result.HasValue && result.Value)
            {
                // Start download
                await DownloadVideo(url, saveFileDialog.FileName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error downloading video: {ex.Message}", 
                "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Handle the download audio button click
    /// </summary>
    private async void BtnDownloadAudio_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideoInfo == null) return;
          try
        {
            string? url = txtYouTubeUrl.Text?.Trim();
            
            if (string.IsNullOrEmpty(url) || _currentVideoInfo == null)
            {
                MessageBox.Show("No valid video URL or video info available", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Ask user for the save location
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = $"{_currentVideoInfo.Title}.mp3",
                DefaultExt = ".mp3",
                Filter = "MP3 Files (*.mp3)|*.mp3",
                InitialDirectory = _downloadsFolderPath
            };
            
            bool? result = saveFileDialog.ShowDialog(this);
            
            if (result.HasValue && result.Value)
            {
                // Start download
                await DownloadAudio(url, saveFileDialog.FileName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error downloading audio: {ex.Message}", 
                "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Handle the extract speech-optimized audio button click
    /// </summary>
    private async void BtnExtractSpeechAudio_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideoInfo == null) return;
        
        try
        {
            string? url = txtYouTubeUrl.Text?.Trim();
            
            if (string.IsNullOrEmpty(url) || _currentVideoInfo == null)
            {
                MessageBox.Show("No valid video URL or video info available", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Ask user for the save location
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = $"{_currentVideoInfo.Title}_speech.wav",
                DefaultExt = ".wav",
                Filter = "WAV Files (*.wav)|*.wav|All Audio Files|*.wav;*.mp3;*.flac;*.ogg",
                InitialDirectory = _downloadsFolderPath
            };
            
            bool? result = saveFileDialog.ShowDialog(this);
            
            if (result.HasValue && result.Value)
            {
                // Start download and extraction
                await ExtractSpeechAudio(url, saveFileDialog.FileName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error extracting speech audio: {ex.Message}", 
                "Extraction Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Download the video and show progress
    /// </summary>
    private async Task DownloadVideo(string url, string outputPath)
    {
        try
        {
            // Show progress UI
            downloadProgressGrid.Visibility = Visibility.Visible;
            txtDownloadStatus.Text = "Downloading video...";
            SetLoadingState(true);
            
            // Create a progress reporter
            var progress = new Progress<double>(p =>
            {
                // Update progress bar
                progressDownload.Value = p * 100;
            });
            
            // Start the download
            string downloadedFilePath = await _youTubeService.DownloadVideoAsync(
                url, 
                outputPath, 
                quality: "highest", 
                progressCallback: progress);
            
            // Download complete
            progressDownload.Value = 100;
            
            MessageBox.Show($"Video downloaded successfully to:\n{downloadedFilePath}", 
                "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Open the folder containing the downloaded file
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{downloadedFilePath}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error downloading video: {ex.Message}", 
                "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Hide progress UI
            downloadProgressGrid.Visibility = Visibility.Collapsed;
            SetLoadingState(false);
        }
    }
    
    /// <summary>
    /// Download the audio and show progress
    /// </summary>
    private async Task DownloadAudio(string url, string outputPath)
    {
        try
        {
            // Show progress UI
            downloadProgressGrid.Visibility = Visibility.Visible;
            txtDownloadStatus.Text = "Downloading audio...";
            SetLoadingState(true);
            
            // Create a progress reporter
            var progress = new Progress<double>(p =>
            {
                // Update progress bar
                progressDownload.Value = p * 100;
            });
            
            // Start the download
            string downloadedFilePath = await _youTubeService.DownloadAudioOnlyAsync(
                url, 
                outputPath, 
                progressCallback: progress);
            
            // Download complete
            progressDownload.Value = 100;
            
            MessageBox.Show($"Audio downloaded successfully to:\n{downloadedFilePath}", 
                "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Open the folder containing the downloaded file
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{downloadedFilePath}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error downloading audio: {ex.Message}", 
                "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Hide progress UI
            downloadProgressGrid.Visibility = Visibility.Collapsed;
            SetLoadingState(false);
        }
    }
    
    /// <summary>
    /// Extract speech-optimized audio from a YouTube video and show progress
    /// </summary>
    private async Task ExtractSpeechAudio(string url, string outputPath)
    {
        try
        {
            // Show progress UI
            downloadProgressGrid.Visibility = Visibility.Visible;
            txtDownloadStatus.Text = "Downloading and processing audio for speech recognition...";
            SetLoadingState(true);
            
            // Create a progress reporter
            var progress = new Progress<double>(p =>
            {
                // Update progress bar
                progressDownload.Value = p * 100;
            });
            
            // Start the download and audio extraction
            string extractedFilePath = await _youTubeService.DownloadAndExtractSpeechAudioAsync(
                url, 
                outputPath,
                deleteVideoAfterExtraction: true,
                progressCallback: progress);
            
            // Download and extraction complete
            progressDownload.Value = 100;
            
            MessageBox.Show($"Speech-optimized audio extracted successfully to:\n{extractedFilePath}", 
                "Extraction Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Open the folder containing the extracted file
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{extractedFilePath}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error extracting speech audio: {ex.Message}", 
                "Extraction Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Hide progress UI
            downloadProgressGrid.Visibility = Visibility.Collapsed;
            SetLoadingState(false);
        }
    }
}