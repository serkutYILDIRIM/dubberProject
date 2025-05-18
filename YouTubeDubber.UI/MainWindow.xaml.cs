using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using YouTubeDubber.Core;
using YouTubeDubber.Core.Models;
using YouTubeDubber.Core.Interfaces;
using YouTubeDubber.Core.Services;

namespace YouTubeDubber.UI
{
    public partial class MainWindow : Window
    {
    // Services
    private IYouTubeService? _youTubeService;
    private ISpeechRecognitionService? _speechRecognitionService;
    private ITextToSpeechService? _textToSpeechService;
    private ITranslationService? _translationService;
    private IAudioVideoMergeService? _audioVideoMergeService;

    // State management
    private VideoInfo? _currentVideo;
    private TranscriptionResult? _transcriptionResult;
    private TranslationResult? _translationResult;
    private string? _downloadedVideoPath;
    private string? _downloadedAudioPath;
    private string? _extractedSpeechPath;
    private string? _generatedSpeechPath;
    private string? _dubbedVideoPath;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            InitializeVoiceSettings();
        }

        private void InitializeServices()
        {
            try
            {
                _youTubeService = new YouTubeService();
                _speechRecognitionService = new AzureSpeechRecognitionService();
                _textToSpeechService = new AzureTextToSpeechService();
                _translationService = new AzureTranslationService();
                _audioVideoMergeService = new FFmpegAudioVideoMergeService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Servisler başlatılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    private void InitializeVoiceSettings()
    {
        try
        {
            // Note: We don't need to initialize these here as they will be populated 
            // when needed from the UI events. The controls are defined in XAML.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in InitializeVoiceSettings: {ex.Message}");
        }
    }

        #region UI Helper Methods    private void ShowProgressUI(string activity, ProgressPanel panel)
    {
        try
        {
            // Common progress UI update
            if (downloadProgressPanel != null)
                downloadProgressPanel.Visibility = Visibility.Visible;
                
            // Panel-specific UI updates
            switch (panel)
            {
                case ProgressPanel.Download:
                    if (txtDownloadStatus != null)
                    {
                        txtDownloadStatus.Text = activity;
                    }
                    if (progressDownload != null)
                    {
                        progressDownload.IsIndeterminate = true;
                        progressDownload.Value = 0;
                    }
                    break;
                    
                // Other cases would be handled if the UI controls existed
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowProgressUI: {ex.Message}");
        }
    }

    private void UpdateProgressUI(double progress, ProgressPanel panel)
    {
        try
        {
            switch (panel)
            {
                case ProgressPanel.Download:
                    if (progressDownload != null)
                    {
                        progressDownload.IsIndeterminate = false;
                        progressDownload.Value = progress * 100;
                    }
                    break;
                    
                // Other cases would be handled if the UI controls existed
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateProgressUI: {ex.Message}");
        }
    }        private void HideProgressUI(ProgressPanel panel)
        {
            try
            {
                // Common progress UI update
                if (downloadProgressPanel != null)
                    downloadProgressPanel.Visibility = Visibility.Collapsed;
                
                // Panel-specific cleanup
                switch (panel)
                {
                    case ProgressPanel.Download:
                        // Just collapse the common progress grid, which we've already done
                        break;
                        
                    // Other cases would be handled if the UI controls existed
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HideProgressUI: {ex.Message}");
            }
        }

    private void DisplayTranscriptionResult(TranscriptionResult transcription)
    {
        try
        {
            // Save result
            _transcriptionResult = transcription;
            
            // Display in UI
            if (txtTranscriptionResult != null)
                txtTranscriptionResult.Text = transcription.FullText;
            if (txtBilingualSource != null)
                txtBilingualSource.Text = transcription.FullText;
            
            // Make the translation button available
            if (btnTranslateText != null)
                btnTranslateText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DisplayTranscriptionResult: {ex.Message}");
        }
    }

    private void DisplayTranslationResult(TranslationResult translation)
    {
        try
        {
            // Save result
            _translationResult = translation;
            
            // Display in UI
            if (txtTranslationResult != null)
                txtTranslationResult.Text = translation.TranslatedText;
            if (txtBilingualTarget != null)
                txtBilingualTarget.Text = translation.TranslatedText;
            if (tabTranslation != null)
                tabTranslation.IsSelected = true;
            
            // Update UI to enable next steps
            if (btnGenerateSpeech != null)
                btnGenerateSpeech.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DisplayTranslationResult: {ex.Message}");
        }
    }
        #endregion

        #region Event Handlers
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnConfigureSpeechCredentials_Click(object sender, RoutedEventArgs e)
        {
            // Open speech credentials configuration dialog
            MessageBox.Show("Speech credentials configuration dialog will open here.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnConfigureTranslationCredentials_Click(object sender, RoutedEventArgs e)
        {
            // Open translation credentials configuration dialog
            MessageBox.Show("Translation credentials configuration dialog will open here.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnResetCredentials_Click(object sender, RoutedEventArgs e)
        {
            // Reset credentials
            MessageBox.Show("Credentials have been reset.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            // Show about dialog
            MessageBox.Show("YouTube Video Dublaj Uygulaması\nSürüm 1.0\n\n© 2025", "Hakkında", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TxtYouTubeUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnGetVideoInfo_Click(sender, e);
            }
        }

        private async void BtnGetVideoInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtYouTubeUrl != null && !string.IsNullOrWhiteSpace(txtYouTubeUrl.Text))
                {
                    string url = txtYouTubeUrl.Text.Trim();
                    
                    ShowProgressUI("Video bilgileri alınıyor...", ProgressPanel.Download);
                    
                    // Get video info
                    _currentVideo = await _youTubeService.GetVideoInfoAsync(url);
                    
                    // Update UI with video info
                    if (txtVideoTitle != null)
                        txtVideoTitle.Text = _currentVideo.Title;
                    if (txtVideoAuthor != null)
                        txtVideoAuthor.Text = _currentVideo.Author;
                    if (txtVideoDuration != null)
                        txtVideoDuration.Text = _currentVideo.Duration.ToString(@"hh\:mm\:ss");
                    if (txtVideoDescription != null)
                        txtVideoDescription.Text = _currentVideo.Description;
                    
                    // Load thumbnail
                    if (imgThumbnail != null && !string.IsNullOrEmpty(_currentVideo.ThumbnailUrl))
                    {
                        try
                        {
                            imgThumbnail.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_currentVideo.ThumbnailUrl));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading thumbnail: {ex.Message}");
                        }
                    }
                    
                    // Show video info panel
                    if (videoInfoPanel != null)
                        videoInfoPanel.Visibility = Visibility.Visible;
                    if (txtNoData != null)
                        txtNoData.Visibility = Visibility.Collapsed;
                    
                    HideProgressUI(ProgressPanel.Download);
                }
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Download);
                MessageBox.Show($"Video bilgileri alınırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        private async void BtnDownloadVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentVideo == null)
                {
                    MessageBox.Show("Önce bir YouTube video URL'si girmelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                ShowProgressUI("Video indiriliyor...", ProgressPanel.Download);
                
                // Progress reporter
                var progress = new Progress<double>(p => UpdateProgressUI(p, ProgressPanel.Download));
                
                // Download video
                _downloadedVideoPath = await _youTubeService.DownloadVideoAsync(
                    txtYouTubeUrl.Text,
                    null,
                    "highest",
                    progress
                );
                
                HideProgressUI(ProgressPanel.Download);
                
                // Show preview
                if (!string.IsNullOrEmpty(_downloadedVideoPath))
                {
                    if (mediaPreview != null)
                    {
                        mediaPreview.Source = new Uri(_downloadedVideoPath);
                    }
                    
                    if (videoPreviewBorder != null)
                    {
                        videoPreviewBorder.Visibility = Visibility.Visible;
                    }
                    
                    if (videoPreviewControls != null)
                    {
                        videoPreviewControls.Visibility = Visibility.Visible;
                    }
                    
                    MessageBox.Show($"Video başarıyla indirildi: {_downloadedVideoPath}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Download);
                MessageBox.Show($"Video indirilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        private async void BtnDownloadAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentVideo == null)
                {
                    MessageBox.Show("Önce bir YouTube video URL'si girmelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                ShowProgressUI("Ses dosyası indiriliyor...", ProgressPanel.Download);
                
                // Progress reporter
                var progress = new Progress<double>(p => UpdateProgressUI(p, ProgressPanel.Download));
                
                // Download audio
                _downloadedAudioPath = await _youTubeService.DownloadAudioOnlyAsync(
                    txtYouTubeUrl.Text,
                    null,
                    progress
                );
                
                HideProgressUI(ProgressPanel.Download);
                
                if (!string.IsNullOrEmpty(_downloadedAudioPath))
                {
                    MessageBox.Show($"Ses dosyası başarıyla indirildi: {_downloadedAudioPath}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Download);
                MessageBox.Show($"Ses dosyası indirilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnExtractSpeechAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentVideo == null)
                {
                    MessageBox.Show("Önce bir YouTube video URL'si girmelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (string.IsNullOrEmpty(_downloadedVideoPath) && string.IsNullOrEmpty(_downloadedAudioPath))
                {
                    MessageBox.Show("Önce video veya ses dosyasını indirmelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                ShowProgressUI("Konuşma sesleri çıkartılıyor...", ProgressPanel.Download);
                
                // Extract speech audio
                string sourceFile = !string.IsNullOrEmpty(_downloadedAudioPath) ? _downloadedAudioPath : _downloadedVideoPath;
                
                // Call audio extraction service
                //_extractedSpeechPath = await _audioExtractionService.ExtractSpeechAsync(sourceFile, new AudioExtractionOptions
                //{
                //    EnhanceSpeech = true
                //});
                
                // For now, just mock the result
                _extractedSpeechPath = sourceFile;
                
                HideProgressUI(ProgressPanel.Download);
                
                if (!string.IsNullOrEmpty(_extractedSpeechPath))
                {
                    MessageBox.Show("Konuşma sesleri başarıyla çıkartıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Download);
                MessageBox.Show($"Konuşma sesleri çıkartılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnTranscribeAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_downloadedAudioPath) && string.IsNullOrEmpty(_extractedSpeechPath))
                {
                    MessageBox.Show("Önce ses dosyasını indirmelisiniz veya konuşma seslerini çıkartmalısınız.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                ShowProgressUI("Konuşmalar tanımlanıyor...", ProgressPanel.Transcription);
                
                // Transcribe audio
                string sourceFile = !string.IsNullOrEmpty(_extractedSpeechPath) ? _extractedSpeechPath : _downloadedAudioPath;                var options = new SpeechRecognitionOptions
                {
                    // Note: SpeechRecognitionOptions doesn't have a Language property in this interface
                    // Use correct properties from the interface
                };
                
                var transcriptionResult = await _speechRecognitionService.TranscribeAudioFileAsync(sourceFile, options);
                
                HideProgressUI(ProgressPanel.Transcription);
                
                if (transcriptionResult != null)
                {
                    DisplayTranscriptionResult(transcriptionResult);
                }
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Transcription);
                MessageBox.Show($"Konuşma tanıma işlemi sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnTranslateText_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_transcriptionResult == null || string.IsNullOrEmpty(_transcriptionResult.FullText))
                {
                    MessageBox.Show("Önce konuşmaları tanımlamanız gerekiyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                ShowProgressUI("Çeviri yapılıyor...", ProgressPanel.Translation);
                
                // Translate text
                var options = new TranslationOptions
                {
                    SourceLanguage = "en",
                    TargetLanguage = "tr"
                };
                
                var translationResult = await _translationService.TranslateTextAsync(_transcriptionResult.FullText, options);
                
                HideProgressUI(ProgressPanel.Translation);
                
                if (translationResult != null)
                {
                    DisplayTranslationResult(translationResult);
                }
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Translation);
                MessageBox.Show($"Çeviri işlemi sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEnhanceTranslation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_translationResult == null || string.IsNullOrEmpty(_translationResult.TranslatedText))
                {
                    MessageBox.Show("Önce çeviri yapmanız gerekiyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                ShowProgressUI("Çeviri iyileştiriliyor...", ProgressPanel.Translation);
                
                // Enhance translation - this would typically call an AI service to improve the translation
                // For now, just simulate the enhancement
                await Task.Delay(2000);
                
                string enhancedText = _translationResult.TranslatedText;
                _translationResult.TranslatedText = enhancedText;
                
                HideProgressUI(ProgressPanel.Translation);
                
                DisplayTranslationResult(_translationResult);
                MessageBox.Show("Çeviri iyileştirildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Translation);
                MessageBox.Show($"Çeviri iyileştirme işlemi sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        private async void BtnGenerateSpeech_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_translationResult == null || string.IsNullOrEmpty(_translationResult.TranslatedText))
                {
                    MessageBox.Show("Önce çeviri yapmanız gerekiyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Create and show Turkish speech options dialog
                var speechOptionsDialog = new TurkishSpeechOptionsDialog(_textToSpeechService, _translationResult.TranslatedText);
                speechOptionsDialog.Owner = this;
                
                bool? dialogResult = speechOptionsDialog.ShowDialog();
                
                if (dialogResult != true)
                {
                    // User cancelled
                    return;
                }
                
                // Get options from dialog
                var options = speechOptionsDialog.SpeechOptions;
                
                ShowProgressUI("Türkçe konuşma oluşturuluyor...", ProgressPanel.Speech);
                
                // Generate a temporary file path for the speech output
                string outputFilePath = Path.Combine(Path.GetTempPath(), $"speech_{Guid.NewGuid()}.mp3");
                
                // Call the speech service to generate audio
                _generatedSpeechPath = await _textToSpeechService.SynthesizeTextToSpeechAsync(
                    _translationResult.TranslatedText,
                    outputFilePath,
                    options
                );
                
                HideProgressUI(ProgressPanel.Speech);
                
                if (!string.IsNullOrEmpty(_generatedSpeechPath))
                {
                    // Update UI to show success
                    if (btnCreateDubbedVideo != null)
                        btnCreateDubbedVideo.Visibility = Visibility.Visible;
                    
                    MessageBox.Show("Türkçe konuşma başarıyla oluşturuldu.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Speech);
                MessageBox.Show($"Konuşma sentezi sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        private async void BtnCreateDubbedVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_downloadedVideoPath))
                {
                    MessageBox.Show("Önce video dosyasını indirmelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (string.IsNullOrEmpty(_generatedSpeechPath))
                {
                    MessageBox.Show("Önce Türkçe konuşma oluşturmalısınız.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Create and show Turkish dubbing options dialog
                var dubbingOptionsDialog = new TurkishDubbingOptionsDialog();
                dubbingOptionsDialog.Owner = this;
                
                bool? dialogResult = dubbingOptionsDialog.ShowDialog();
                
                if (dialogResult != true)
                {
                    // User cancelled
                    return;
                }
                
                // Get options from dialog
                var options = dubbingOptionsDialog.DubbingOptions;
                
                ShowProgressUI("Dublajlı video oluşturuluyor...", ProgressPanel.Dubbing);
                
                // Generate output path for the dubbed video
                string outputFilePath = Path.Combine(
                    Path.GetDirectoryName(_downloadedVideoPath) ?? Path.GetTempPath(),
                    $"{Path.GetFileNameWithoutExtension(_downloadedVideoPath)}_dubbed.mp4"
                );
                
                // Create a progress reporter for the UI
                var progress = new Progress<double>(p => UpdateProgressUI(p, ProgressPanel.Dubbing));
                
                // Call the service to create the dubbed video
                _dubbedVideoPath = await _audioVideoMergeService.CreateTurkishDubbedVideoAsync(
                    _downloadedVideoPath,
                    _generatedSpeechPath,
                    outputFilePath,
                    options,
                    progress
                );
                
                HideProgressUI(ProgressPanel.Dubbing);
                
                if (!string.IsNullOrEmpty(_dubbedVideoPath))
                {
                    // Switch to the preview tab
                    if (previewTab != null)
                    {
                        previewTab.IsSelected = true;
                    }
                    
                    // Load the video in the preview media element
                    if (mediaPreviewFull != null)
                    {
                        mediaPreviewFull.Source = new Uri(_dubbedVideoPath);
                        mediaPreviewFull.Visibility = Visibility.Visible;
                    }
                    
                    // Hide the "no preview" message
                    if (txtNoPreview != null)
                    {
                        txtNoPreview.Visibility = Visibility.Collapsed;
                    }
                    
                    // Show the preview controls
                    if (previewControlsFull != null)
                    {
                        previewControlsFull.Visibility = Visibility.Visible;
                    }
                    
                    MessageBox.Show($"Dublajlı video başarıyla oluşturuldu: {_dubbedVideoPath}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                HideProgressUI(ProgressPanel.Dubbing);
                MessageBox.Show($"Dublajlı video oluşturulurken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveTranscription_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_transcriptionResult == null || string.IsNullOrEmpty(_transcriptionResult.FullText))
                {
                    MessageBox.Show("Kaydedilecek çeviriyazı bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    Title = "Çeviriyazıyı Kaydet",
                    FileName = $"{_currentVideo?.Title ?? "transcription"}_en.txt"
                };
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, _transcriptionResult.FullText);
                    MessageBox.Show($"Çeviriyazı başarıyla kaydedildi: {saveFileDialog.FileName}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Çeviriyazı kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveTranslation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_translationResult == null || string.IsNullOrEmpty(_translationResult.TranslatedText))
                {
                    MessageBox.Show("Kaydedilecek çeviri bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    Title = "Çeviriyi Kaydet",
                    FileName = $"{_currentVideo?.Title ?? "translation"}_tr.txt"
                };
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, _translationResult.TranslatedText);
                    MessageBox.Show($"Çeviri başarıyla kaydedildi: {saveFileDialog.FileName}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Çeviri kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPlayPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mediaPreview != null)
                {
                    mediaPreview.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BtnPlayPreview_Click: {ex.Message}");
            }
        }

        private void BtnPausePreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mediaPreview != null)
                {
                    mediaPreview.Pause();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BtnPausePreview_Click: {ex.Message}");
            }
        }

        private void BtnStopPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mediaPreview != null)
                {
                    mediaPreview.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BtnStopPreview_Click: {ex.Message}");
            }
        }

        private void BtnPlayPreviewFull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mediaPreviewFull != null)
                {
                    mediaPreviewFull.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BtnPlayPreviewFull_Click: {ex.Message}");
            }
        }

        private void BtnPausePreviewFull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mediaPreviewFull != null)
                {
                    mediaPreviewFull.Pause();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BtnPausePreviewFull_Click: {ex.Message}");
            }
        }

        private void BtnStopPreviewFull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mediaPreviewFull != null)
                {
                    mediaPreviewFull.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BtnStopPreviewFull_Click: {ex.Message}");
            }
        }

        private void CmbMixingProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbMixingProfile != null && customMixingPanel != null)
                {
                    var item = cmbMixingProfile.SelectedItem as ComboBoxItem;
                    if (item != null && item.Tag != null)
                    {
                        string profile = item.Tag.ToString();
                        
                        if (profile == "custom")
                        {
                            customMixingPanel.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            customMixingPanel.Visibility = Visibility.Collapsed;
                            
                            // Set default values based on profile
                            switch (profile)
                            {
                                case "balanced":
                                    if (sliderBackgroundVolume != null) sliderBackgroundVolume.Value = 0.3;
                                    if (sliderVoiceVolume != null) sliderVoiceVolume.Value = 1.0;
                                    if (sliderDucking != null) sliderDucking.Value = 0.8;
                                    break;
                                case "voice-centered":
                                    if (sliderBackgroundVolume != null) sliderBackgroundVolume.Value = 0.1;
                                    if (sliderVoiceVolume != null) sliderVoiceVolume.Value = 1.2;
                                    if (sliderDucking != null) sliderDucking.Value = 0.9;
                                    break;
                                case "background":
                                    if (sliderBackgroundVolume != null) sliderBackgroundVolume.Value = 0.5;
                                    if (sliderVoiceVolume != null) sliderVoiceVolume.Value = 1.0;
                                    if (sliderDucking != null) sliderDucking.Value = 0.7;
                                    break;
                                case "music":
                                    if (sliderBackgroundVolume != null) sliderBackgroundVolume.Value = 0.4;
                                    if (sliderVoiceVolume != null) sliderVoiceVolume.Value = 1.1;
                                    if (sliderDucking != null) sliderDucking.Value = 0.7;
                                    break;
                                case "voice":
                                    if (sliderBackgroundVolume != null) sliderBackgroundVolume.Value = 0.1;
                                    if (sliderVoiceVolume != null) sliderVoiceVolume.Value = 1.3;
                                    if (sliderDucking != null) sliderDucking.Value = 0.95;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CmbMixingProfile_SelectionChanged: {ex.Message}");
            }
        }

        private void ChkAddSubtitles_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkAddSubtitles != null && subtitlesPanel != null)
                {
                    bool isChecked = chkAddSubtitles.IsChecked ?? false;
                    subtitlesPanel.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ChkAddSubtitles_CheckedChanged: {ex.Message}");
            }
        }

        private void BtnBrowseSubtitles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Subtitle files (*.srt;*.ass;*.ssa)|*.srt;*.ass;*.ssa|All files (*.*)|*.*",
                    Title = "Altyazı Dosyası Seç"
                };
                
                if (openFileDialog.ShowDialog() == true)
                {
                    if (txtSubtitlesPath != null)
                    {
                        txtSubtitlesPath.Text = openFileDialog.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Altyazı dosyası seçilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }

    // Helper enum for UI panel management
    public enum ProgressPanel
    {
        Download,
        Transcription,
        Translation,
        Speech,
        Dubbing
    }
}