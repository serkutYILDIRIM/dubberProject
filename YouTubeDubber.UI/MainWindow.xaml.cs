using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using YouTubeDubber.Core;
using YouTubeDubber.Core.Extensions;
using YouTubeDubber.Core.Helpers;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Models;
using YouTubeDubber.Core.Services;

namespace YouTubeDubber.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{    
    private readonly IYouTubeService _youTubeService;
    private readonly ISpeechRecognitionService _speechRecognitionService;
    private readonly ITranslationService _translationService;
    private readonly ITextToSpeechService _textToSpeechService;
    private readonly IAudioVideoMergeService _audioVideoMergeService;
    private VideoInfo? _currentVideoInfo;
    private TranscriptionResult? _currentTranscription;
    private TranslationResult? _currentTranslation;
    private string? _lastSynthesizedAudioPath;
    private string? _lastDubbedVideoPath;
    private string _downloadsFolderPath;
    private string? _lastExtractedAudioPath;
    
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
          
        // Initialize speech recognition service
        // Note: In a production app, you would load these from secure configuration
        var speechOptions = new SpeechRecognitionOptions
        {
            ApiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "",
            Region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "westus",
            LanguageCode = "en-US",
            UseContinuousRecognition = true,
            EnableWordLevelTimestamps = true
        };
        
        _speechRecognitionService = new AzureSpeechRecognitionService(speechOptions);        // Initialize translation service
        var translationOptions = new TranslationOptions
        {
            ApiKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY") ?? "",
            Region = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "global",
            SourceLanguage = "en",
            TargetLanguage = "tr"
        };
        
        // Create translation service using factory method
        _translationService = CreateTranslationService(translationOptions);
        
        // Initialize text-to-speech service
        var ttsOptions = new TextToSpeechOptions
        {
            ApiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "",
            Region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "westus",
            LanguageCode = "tr-TR",
            OutputFormat = "wav",
            SamplingRate = 24000
        };
        
        _textToSpeechService = new AzureTextToSpeechService(ttsOptions);
          // Initialize audio-video merge service
        _audioVideoMergeService = new FFmpegAudioVideoMergeService();
        
        // Initialize FFmpeg in the background
        InitializeFFmpegAsync();
    }
    
    /// <summary>
    /// Creates a translation service with the specified options
    /// </summary>
    private ITranslationService CreateTranslationService(TranslationOptions options)
    {
        // Create an instance of AzureTranslationService without directly referencing the type
        // Use reflection to create the instance
        var type = Type.GetType("YouTubeDubber.Core.Services.AzureTranslationService, YouTubeDubber.Core");
        if (type == null)
        {
            throw new InvalidOperationException("AzureTranslationService type not found");
        }
        
        // Create an instance using the constructor that takes TranslationOptions
        return (ITranslationService)Activator.CreateInstance(type, options);
    }
    
    /// <summary>
    /// Initialize FFmpeg in the background
    /// </summary>
    private async void InitializeFFmpegAsync()
    {
        try
        {
            // Cast to specific implementation to access the initialization method
            if (_audioVideoMergeService is FFmpegAudioVideoMergeService ffmpegService)
            {
                await ffmpegService.InitializeFFmpegAsync();
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't show to user yet - this is a background initialization
            Console.WriteLine($"FFmpeg initialization error: {ex.Message}");
        }
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
            
            // Store the path to the extracted audio file
            _lastExtractedAudioPath = extractedFilePath;
            
            // Download and extraction complete
            progressDownload.Value = 100;
            
            MessageBox.Show($"Speech-optimized audio extracted successfully to:\n{extractedFilePath}", 
                "Extraction Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Open the folder containing the extracted file
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{extractedFilePath}\"");
            
            // Ask if the user wants to transcribe the audio now
            var result = MessageBox.Show(
                "Would you like to transcribe this audio to text now?",
                "Transcribe Audio",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                await TranscribeAudio(_lastExtractedAudioPath);
            }
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
    
    /// <summary>
    /// Handle the transcribe audio button click
    /// </summary>
    private async void BtnTranscribeAudio_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_lastExtractedAudioPath) || !File.Exists(_lastExtractedAudioPath))
            {
                // If no audio has been extracted, prompt the user to select an audio file
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*",
                    Title = "Select Audio File to Transcribe",
                    InitialDirectory = _downloadsFolderPath
                };
                
                if (openFileDialog.ShowDialog() != true)
                {
                    return;
                }
                
                _lastExtractedAudioPath = openFileDialog.FileName;
            }
            
            // Check if we have Azure Speech credentials
            var apiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
            var region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");
            
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
            {
                // Prompt for API key and region if not set
                var result = MessageBox.Show(
                    "Azure Speech Service credentials are required for transcription.\n\n" +
                    "Would you like to enter your credentials now?",
                    "Credentials Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Show a dialog to get credentials (you would implement this dialog)
                    if (!await ShowCredentialsDialog())
                    {
                        return;
                    }
                    
                    // Update the credentials in the service
                    apiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
                    region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");
                    
                    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
                    {
                        MessageBox.Show(
                            "Valid credentials are required to continue.",
                            "Transcription Canceled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            
            // Now transcribe the audio
            await TranscribeAudio(_lastExtractedAudioPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error transcribing audio: {ex.Message}",
                "Transcription Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Transcribe the audio file and display the result
    /// </summary>
    private async Task TranscribeAudio(string audioFilePath)
    {
        try
        {
            // Show progress UI
            downloadProgressGrid.Visibility = Visibility.Visible;
            txtDownloadStatus.Text = "Transcribing audio...";
            SetLoadingState(true);
            
            // Create a progress reporter
            var progress = new Progress<double>(p =>
            {
                // Update progress bar
                progressDownload.Value = p * 100;
            });
            
            // Configure speech recognition options
            var options = new SpeechRecognitionOptions
            {
                ApiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "",
                Region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "westus",
                LanguageCode = "en-US",
                UseContinuousRecognition = true,
                EnableWordLevelTimestamps = true
            };
            
            // Start the transcription
            _currentTranscription = await _speechRecognitionService.TranscribeAudioFileAsync(
                audioFilePath,
                options,
                progressCallback: progress);
            
            // Transcription complete
            progressDownload.Value = 100;
            
            // Display the transcription result
            DisplayTranscriptionResult(_currentTranscription);
            
            // Show success message
            MessageBox.Show("Audio transcription completed successfully!",
                "Transcription Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error transcribing audio: {ex.Message}",
                "Transcription Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Hide progress UI
            downloadProgressGrid.Visibility = Visibility.Collapsed;
            SetLoadingState(false);
        }
    }
    
    /// <summary>
    /// Display the transcription result in the UI
    /// </summary>
    private void DisplayTranscriptionResult(TranscriptionResult transcription)
    {
        if (transcription == null || transcription.Segments.Count == 0)
        {
            MessageBox.Show("No transcription result available", "Empty Transcription", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Show the transcription panel
        transcriptionPanel.Visibility = Visibility.Visible;
        
        // Display the full text in the text box
        txtTranscriptionResult.Text = transcription.FullText;
    }
    
    /// <summary>
    /// Handle the save transcription button click
    /// </summary>
    private async void BtnSaveTranscription_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTranscription == null)
        {
            MessageBox.Show("No transcription available to save", "Save Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            // Ask user for the save format
            var formatWindow = new SaveFormatDialog();
            if (formatWindow.ShowDialog() != true)
            {
                return;
            }
            
            string format = formatWindow.SelectedFormat;
            string defaultExt = format.ToLower();
            string filter = $"{format.ToUpper()} Files (*.{defaultExt})|*.{defaultExt}|All Files (*.*)|*.*";
            
            // Ask user for the save location
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = $"Transcription_{DateTime.Now:yyyyMMdd_HHmmss}.{defaultExt}",
                DefaultExt = defaultExt,
                Filter = filter,
                InitialDirectory = _downloadsFolderPath
            };
            
            bool? result = saveFileDialog.ShowDialog(this);
            
            if (result.HasValue && result.Value)
            {
                // Show loading state
                SetLoadingState(true);
                
                // Save the transcription
                string savedPath = await _speechRecognitionService.SaveTranscriptionAsync(
                    _currentTranscription,
                    saveFileDialog.FileName,
                    format.ToLower());
                
                MessageBox.Show($"Transcription saved successfully to:\n{savedPath}", 
                    "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Open the folder containing the saved file
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{savedPath}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving transcription: {ex.Message}", 
                "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetLoadingState(false);
        }
    }
      /// <summary>
    /// Handle the translate text button click
    /// </summary>
    private async void BtnTranslateText_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentTranscription == null || _currentTranscription.Segments.Count == 0)
            {
                MessageBox.Show("No transcription available to translate", "Translation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Check if we have Azure Translator credentials
            var apiKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");
            var region = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION");
            
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
            {
                // Prompt for API key and region if not set
                var result = MessageBox.Show(
                    "Azure Translator Service credentials are required for translation.\n\n" +
                    "Would you like to enter your credentials now?",
                    "Credentials Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Show a dialog to get credentials
                    if (!await ShowTranslatorCredentialsDialog())
                    {
                        return;
                    }
                    
                    // Update the credentials in the service
                    apiKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");
                    region = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION");
                    
                    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
                    {
                        MessageBox.Show(
                            "Valid credentials are required to continue.",
                            "Translation Canceled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            
            // Now translate the transcription
            await TranslateTranscription(_currentTranscription);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error translating text: {ex.Message}",
                "Translation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Translates the transcription and displays the result
    /// </summary>
    private async Task TranslateTranscription(TranscriptionResult transcription)
    {
        try
        {
            // Show progress UI
            downloadProgressGrid.Visibility = Visibility.Visible;
            txtDownloadStatus.Text = "Translating text...";
            SetLoadingState(true);
            
            // Create a progress reporter
            var progress = new Progress<double>(p =>
            {
                // Update progress bar
                progressDownload.Value = p * 100;
            });
              // Configure translation options
            var options = new TranslationOptions
            {
                ApiKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY") ?? "",
                Region = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "global",
                SourceLanguage = "en",
                TargetLanguage = "tr",
                PreserveFormatting = true
            };
            
            // Configure additional Turkish-specific options
            options.ConfigureForTurkish();
            
            // Start the enhanced translation for English to Turkish
            _currentTranslation = await _translationService.TranslateTranscriptionEnglishToTurkish(
                transcription,
                options,
                progressCallback: progress,
                cancellationToken: CancellationToken.None);
            
            // Translation complete
            progressDownload.Value = 100;
            
            // Display the translation result
            DisplayTranslationResult(_currentTranslation);
            
            // Show success message
            MessageBox.Show("Text translation completed successfully!",
                "Translation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Ask if the user wants to save the translation
            var saveResult = MessageBox.Show(
                "Would you like to save the translated text?",
                "Save Translation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (saveResult == MessageBoxResult.Yes)
            {
                await SaveTranslation();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error translating text: {ex.Message}",
                "Translation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Hide progress UI
            downloadProgressGrid.Visibility = Visibility.Collapsed;
            SetLoadingState(false);
        }
    }
      /// <summary>
    /// Display the translation result in the UI
    /// </summary>
    private void DisplayTranslationResult(TranslationResult translation)
    {
        if (translation == null || string.IsNullOrEmpty(translation.TranslatedText))
        {
            MessageBox.Show("No translation result available", "Empty Translation", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Display the translated text in the translation tab
        txtTranslationResult.Text = translation.TranslatedText;
        
        // Update the bilingual view
        txtBilingualSource.Text = translation.SourceText;
        txtBilingualTarget.Text = translation.TranslatedText;
        
        // Switch to the translation tab
        tabTranslation.IsSelected = true;
          // Show the save translation button
        btnSaveTranslation.Visibility = Visibility.Visible;
        
        // Show the enhance Turkish button
        btnEnhanceTranslation.Visibility = Visibility.Visible;
        
        // Show the generate speech button
        btnGenerateSpeech.Visibility = Visibility.Visible;
    }
    
    /// <summary>
    /// Save the current translation to a file
    /// </summary>
    private async Task SaveTranslation()
    {
        if (_currentTranslation == null)
        {
            MessageBox.Show("No translation available to save", "Save Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            // Ask user for the save format
            var formatWindow = new SaveFormatDialog();
            if (formatWindow.ShowDialog() != true)
            {
                return;
            }
            
            string format = formatWindow.SelectedFormat;
            string defaultExt = format.ToLower();
            string filter = $"{format.ToUpper()} Files (*.{defaultExt})|*.{defaultExt}|All Files (*.*)|*.*";
            
            // Ask user for the save location
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = $"Translation_TR_{DateTime.Now:yyyyMMdd_HHmmss}.{defaultExt}",
                DefaultExt = defaultExt,
                Filter = filter,
                InitialDirectory = _downloadsFolderPath
            };
            
            bool? result = saveFileDialog.ShowDialog(this);
            
            if (result.HasValue && result.Value)
            {
                // Show loading state
                SetLoadingState(true);
                
                // Save the translation
                string savedPath = await _translationService.SaveTranslationAsync(
                    _currentTranslation,
                    saveFileDialog.FileName,
                    format.ToLower());
                
                MessageBox.Show($"Translation saved successfully to:\n{savedPath}", 
                    "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Open the folder containing the saved file
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{savedPath}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving translation: {ex.Message}", 
                "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetLoadingState(false);
        }
    }
    
    /// <summary>
    /// Handle the save translation button click
    /// </summary>
    private async void BtnSaveTranslation_Click(object sender, RoutedEventArgs e)
    {
        await SaveTranslation();
    }
    
    /// <summary>
    /// Shows a dialog to collect Azure Translator Service credentials
    /// </summary>
    private Task<bool> ShowTranslatorCredentialsDialog()
    {
        // For simplicity, we'll use a message box to get the credentials
        // In a real app, you would create a proper dialog with input fields
        
        // Get API Key
        string apiKey = Interaction.InputBox(
            "Enter your Azure Translator API Key:",
            "Azure Translator Service Credentials",
            Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY") ?? "");
            
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(false);
        }
        
        // Get Region
        string region = Interaction.InputBox(
            "Enter your Azure Translator Region (e.g., global, westus):",
            "Azure Translator Service Credentials",
            Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "global");
            
        if (string.IsNullOrWhiteSpace(region))
        {
            return Task.FromResult(false);
        }
        
        // Store the credentials as environment variables
        Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", apiKey);
        Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_REGION", region);
        
        return Task.FromResult(true);
    }
      /// <summary>
    /// Shows a dialog to collect Azure Speech Service credentials
    /// </summary>
    private Task<bool> ShowCredentialsDialog()
    {
        // For simplicity, we'll use a message box to get the credentials
        // In a real app, you would create a proper dialog with input fields
        
        // Get API Key
        string apiKey = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter your Azure Speech Service API Key:",
            "Azure Speech Service Credentials",
            Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "");
            
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(false);
        }
        
        // Get Region
        string region = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter your Azure Speech Service Region (e.g., westus, eastus):",
            "Azure Speech Service Credentials",
            Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "westus");
            
        if (string.IsNullOrWhiteSpace(region))
        {
            return Task.FromResult(false);
        }
        
        // Store the credentials as environment variables
        Environment.SetEnvironmentVariable("AZURE_SPEECH_KEY", apiKey);
        Environment.SetEnvironmentVariable("AZURE_SPEECH_REGION", region);
        
        return Task.FromResult(true);
    }
    
    /// <summary>
    /// Handle the exit menu item click
    /// </summary>
    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    /// <summary>
    /// Handle the configure speech credentials menu item click
    /// </summary>
    private async void BtnConfigureSpeechCredentials_Click(object sender, RoutedEventArgs e)
    {
        await ShowCredentialsDialog();
    }

    /// <summary>
    /// Handle the configure translation credentials menu item click
    /// </summary>
    private async void BtnConfigureTranslationCredentials_Click(object sender, RoutedEventArgs e)
    {
        await ShowTranslatorCredentialsDialog();
    }

    /// <summary>
    /// Handle the reset credentials menu item click
    /// </summary>
    private void BtnResetCredentials_Click(object sender, RoutedEventArgs e)
    {
        // Clear all credentials from environment variables
        Environment.SetEnvironmentVariable("AZURE_SPEECH_KEY", null);
        Environment.SetEnvironmentVariable("AZURE_SPEECH_REGION", null);
        Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", null);
        Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_REGION", null);
        
        MessageBox.Show(
            "All Azure credentials have been reset. You will be prompted for credentials when needed.",
            "Credentials Reset",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Handle the about menu item click
    /// </summary>
    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "YouTube Video Dubber\n\n" +
            "A tool for downloading YouTube videos, extracting audio, transcribing, translating to Turkish, " +
            "generating synthesized speech, and creating dubbed videos.\n\n" +
            "Version: 1.0\n" +
            "© 2025 - All rights reserved",
            "About YouTube Video Dubber",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    
    /// <summary>
    /// Handle the generate speech button click
    /// </summary>
    private async void BtnGenerateSpeech_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentTranslation == null || string.IsNullOrEmpty(_currentTranslation.TranslatedText))
            {
                MessageBox.Show("No translation available to synthesize speech", "Speech Synthesis Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Check if we have Azure Speech credentials
            var apiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
            var region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");
            
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
            {
                // Prompt for API key and region if not set
                var result = MessageBox.Show(
                    "Azure Speech Service credentials are required for text-to-speech synthesis.\n\n" +
                    "Would you like to enter your credentials now?",
                    "Credentials Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Show a dialog to get credentials
                    if (!await ShowCredentialsDialog())
                    {
                        return;
                    }
                    
                    // Update the credentials in the service
                    apiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
                    region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");
                    
                    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
                    {
                        MessageBox.Show(
                            "Valid credentials are required to continue.",
                            "Speech Synthesis Canceled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            
            // Configure synthesis options
            var options = await ShowSpeechSynthesisOptionsDialog();
            if (options == null)
            {
                return; // User canceled
            }
            
            // Update options with credentials
            options.ApiKey = apiKey;
            options.Region = region;
              // Apply Turkish-specific text formatting
            _currentTranslation.TranslatedText = TurkishVoiceHelper.FormatTextForTurkishSpeech(_currentTranslation.TranslatedText);
            
            // Enable SSML for better Turkish pronunciation
            options.UseSSML = true;
            
            // The SSML text will be generated by the text-to-speech service internally
            // We provide the voice name and other options
            
            // Show progress UI
            downloadProgressGrid.Visibility = Visibility.Visible;
            txtDownloadStatus.Text = "Generating speech...";
            SetLoadingState(true);
            
            // Create a progress reporter
            var progress = new Progress<double>(p =>
            {
                // Update progress bar
                progressDownload.Value = p * 100;
            });
            
            // Output file path
            string fileName = _currentVideoInfo?.Title ?? "speech";
            string outputPath = Path.Combine(
                _downloadsFolderPath,
                "Synthesized",
                $"{fileName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
            
            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
            
            // Generate speech
            _lastSynthesizedAudioPath = await _textToSpeechService.SynthesizeTranslationAsync(
                _currentTranslation, 
                outputPath,
                options,
                progress,
                CancellationToken.None);
            
            // Enable the dubbing button
            btnCreateDubbedVideo.Visibility = Visibility.Visible;
            
            // Show success message
            MessageBox.Show(
                $"Speech generated successfully and saved to:\n{_lastSynthesizedAudioPath}",
                "Speech Generation Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating speech: {ex.Message}",
                "Speech Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Hide progress UI
            downloadProgressGrid.Visibility = Visibility.Collapsed;
            SetLoadingState(false);
        }
    }
    
    /// <summary>
    /// Handle the create dubbed video button click
    /// </summary>
    private async void BtnCreateDubbedVideo_Click(object sender, RoutedEventArgs e)
    {
        try
        {            if (_currentVideoInfo == null || string.IsNullOrEmpty(_currentVideoInfo.LocalFilePath))
            {
                MessageBox.Show("No video available for dubbing", "Dubbing Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(_lastSynthesizedAudioPath) || !File.Exists(_lastSynthesizedAudioPath))
            {
                MessageBox.Show("No synthesized audio available for dubbing", "Dubbing Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Show prompt for keeping original audio
            var keepOriginalAudio = MessageBox.Show(
                "Would you like to keep the original audio as background?\n\n" +
                "If you select Yes, the original audio will be mixed with the synthesized speech at a lower volume.\n" +
                "If you select No, only the synthesized speech will be used.",
                "Keep Original Audio?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
            
            // Show progress UI
            downloadProgressGrid.Visibility = Visibility.Visible;
            txtDownloadStatus.Text = "Creating dubbed video...";
            SetLoadingState(true);
            
            // Create a progress reporter
            var progress = new Progress<double>(p =>
            {
                // Update progress bar
                progressDownload.Value = p * 100;
            });
            
            // Output file path
            string fileName = _currentVideoInfo.Title ?? "dubbed_video";
            string outputPath = Path.Combine(
                _downloadsFolderPath,
                "Dubbed",
                $"{fileName.Replace(" ", "_")}_dubbed_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            
            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
              // Merge audio and video
            _lastDubbedVideoPath = await _audioVideoMergeService.MergeAudioVideoAsync(
                _currentVideoInfo.LocalFilePath,
                _lastSynthesizedAudioPath,
                outputPath,
                keepOriginalAudio,
                keepOriginalAudio ? 0.2f : 0.0f, // Original audio volume if kept
                1.0f, // New audio volume
                progress,
                CancellationToken.None);
            
            // Show success message with option to play the video
            var playResult = MessageBox.Show(
                $"Dubbed video created successfully and saved to:\n{_lastDubbedVideoPath}\n\nWould you like to play it now?",
                "Video Dubbing Complete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
                
            if (playResult == MessageBoxResult.Yes)
            {
                // Open the video using the default player
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _lastDubbedVideoPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating dubbed video: {ex.Message}",
                "Dubbing Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Hide progress UI
            downloadProgressGrid.Visibility = Visibility.Collapsed;
            SetLoadingState(false);
        }
    }
    
    /// <summary>
    /// Shows a dialog to collect speech synthesis options
    /// </summary>
    private async Task<TextToSpeechOptions?> ShowSpeechSynthesisOptionsDialog()
    {
        try
        {
            // Create a new options object with default values
            var options = new TextToSpeechOptions
            {
                ApiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "",
                Region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "westus",
                LanguageCode = "tr-TR",
                OutputFormat = "wav"
            };
              // Try to get available voices
            string[] availableVoices;
            try
            {
                availableVoices = await _textToSpeechService.GetAvailableVoicesAsync(
                    options.LanguageCode, options, CancellationToken.None);
                
                if (availableVoices.Length > 0)
                {
                    // Default to the recommended Turkish voice from TurkishVoiceHelper
                    options.VoiceName = TurkishVoiceHelper.GetRecommendedMaleVoice();
                }
            }
            catch
            {
                // If we can't get voices, use Turkish voices from the helper
                availableVoices = TurkishVoiceHelper.TurkishVoices.Keys.ToArray();
                options.VoiceName = TurkishVoiceHelper.GetRecommendedMaleVoice();
            }
            
            // Use a series of input boxes for simplicity
            // In a real app, you would create a proper dialog with dropdown menus and sliders            // Select voice
            int voiceIndex = 0;
            
            // Create a more descriptive voice list using TurkishVoiceHelper
            var voiceOptions = new List<string>();
            var voiceDescriptions = new Dictionary<int, string>();
            
            for (int i = 0; i < availableVoices.Length; i++)
            {
                string voiceName = availableVoices[i];
                string description = TurkishVoiceHelper.TurkishVoices.TryGetValue(voiceName, out string desc) 
                    ? desc 
                    : voiceName;
                voiceOptions.Add($"{i + 1}. {description}");
                voiceDescriptions[i] = voiceName;
            }
            
            string voiceList = string.Join(Environment.NewLine, voiceOptions);
            string voiceSelection = Microsoft.VisualBasic.Interaction.InputBox(
                $"Available voices for Turkish (enter the number):\n\n{voiceList}",
                "Select Voice",
                "1");
            
            if (string.IsNullOrWhiteSpace(voiceSelection))
            {
                return null; // User canceled
            }
            
            if (int.TryParse(voiceSelection, out int selectedVoice) && selectedVoice >= 1 && selectedVoice <= availableVoices.Length)
            {
                voiceIndex = selectedVoice - 1;
                options.VoiceName = availableVoices[voiceIndex];
            }
              // Speaking rate
            string rateSelection = Microsoft.VisualBasic.Interaction.InputBox(
                "Speaking rate (0.5 to 2.0, where 1.0 is normal speed):",
                "Speaking Rate",
                "1.0");
                
            if (string.IsNullOrWhiteSpace(rateSelection))
            {
                return null; // User canceled
            }
            
            if (float.TryParse(rateSelection, out float rate) && rate >= 0.5f && rate <= 2.0f)
            {
                options.SpeakingRate = rate;
            }
            
            // Set other Turkish-specific TTS options
            options.UseSSML = true;
            
            return options;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error in speech synthesis options dialog: {ex.Message}",
                "Dialog Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }
    
    /// <summary>
    /// Enhances existing Turkish translation using the special Turkish helpers
    /// </summary>
    private void BtnEnhanceTranslation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentTranslation == null || string.IsNullOrEmpty(_currentTranslation.TranslatedText))
            {
                MessageBox.Show("No translation available to enhance", "Enhancement Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Apply the Turkish-specific enhancements
            string enhancedText = _currentTranslation.TranslatedText;
            
            // Apply all Turkish enhancements
            enhancedText = EnglishTurkishHelper.ReplaceIdiomPlaceholders(enhancedText);
            enhancedText = EnglishTurkishHelper.ApplyTurkishCapitalization(enhancedText);
            enhancedText = EnglishTurkishHelper.FixTurkishILetters(enhancedText);
            enhancedText = EnglishTurkishHelper.FixTurkishSuffixSpacing(enhancedText);
            
            // Update the translation
            _currentTranslation.TranslatedText = enhancedText;
            
            // Update the UI
            DisplayTranslationResult(_currentTranslation);
            
            // Show success message
            MessageBox.Show("Turkish translation has been enhanced with specialized formatting and grammar corrections.",
                "Enhancement Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error enhancing translation: {ex.Message}",
                "Enhancement Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}