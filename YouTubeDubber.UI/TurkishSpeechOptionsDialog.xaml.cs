using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Threading;
using YouTubeDubber.Core.Models;
using YouTubeDubber.Core.Helpers;
using YouTubeDubber.Core.Interfaces;

namespace YouTubeDubber.UI
{
    /// <summary>
    /// Interaction logic for TurkishSpeechOptionsDialog.xaml
    /// </summary>
    public partial class TurkishSpeechOptionsDialog : Window
    {
        private readonly ITextToSpeechService _textToSpeechService;
        private readonly string _textContent;
        
        public TextToSpeechOptions SpeechOptions { get; private set; }
        
        public TurkishSpeechOptionsDialog(ITextToSpeechService textToSpeechService, string textContent, TextToSpeechOptions initialOptions = null)
        {
            InitializeComponent();
            
            _textToSpeechService = textToSpeechService;
            _textContent = textContent;
            
            // Initialize speech options
            SpeechOptions = initialOptions ?? new TextToSpeechOptions
            {
                ApiKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "",
                Region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "westus",
                LanguageCode = "tr-TR",
                OutputFormat = "wav",
                UseSSML = true
            };
            
            // Load voice options on window load
            Loaded += TurkishSpeechOptionsDialog_Loaded;
        }
        
        private async void TurkishSpeechOptionsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate voice dropdown
            await PopulateVoiceOptions();
            
            // Populate speaking styles
            PopulateSpeakingStyles();
            
            // Set initial values
            chkAutoVoiceSelection.IsChecked = true;
            sliderSpeechRate.Value = SpeechOptions.SpeakingRate;
            sliderPitchAdjustment.Value = SpeechOptions.PitchAdjustment;
            chkUseSSML.IsChecked = SpeechOptions.UseSSML;
            
            // Set output format
            if (SpeechOptions.OutputFormat.ToLower() == "mp3")
            {
                cmbOutputFormat.SelectedIndex = 1;
            }
            else
            {
                cmbOutputFormat.SelectedIndex = 0;
            }
        }
        
        private async Task PopulateVoiceOptions()
        {
            cmbVoiceSelection.Items.Clear();
            
            try
            {
                // Try to get available voices from the service
                string[] availableVoices = await _textToSpeechService.GetAvailableVoicesAsync(
                    "tr-TR", SpeechOptions, CancellationToken.None);
                
                // Add all available voices with their descriptions
                foreach (string voiceName in availableVoices)
                {
                    string description = TurkishVoiceHelper.TurkishVoices.TryGetValue(voiceName, out string desc) 
                        ? desc 
                        : voiceName;
                    
                    var item = new ComboBoxItem
                    {
                        Content = description,
                        Tag = voiceName
                    };
                    
                    cmbVoiceSelection.Items.Add(item);
                }
                
                // Select recommended voice
                string recommendedVoice = TurkishVoiceHelper.GetRecommendedMaleVoice();
                SelectVoiceByName(recommendedVoice);
            }
            catch
            {
                // If we can't get voices from the service, use predefined list
                foreach (var voice in TurkishVoiceHelper.TurkishVoices)
                {
                    var item = new ComboBoxItem
                    {
                        Content = voice.Value,
                        Tag = voice.Key
                    };
                    
                    cmbVoiceSelection.Items.Add(item);
                }
                
                // Select first voice
                if (cmbVoiceSelection.Items.Count > 0)
                {
                    cmbVoiceSelection.SelectedIndex = 0;
                }
            }
        }
        
        private void PopulateSpeakingStyles()
        {
            cmbSpeakingStyle.Items.Clear();
            
            // Add all available styles
            foreach (var style in TurkishVoiceHelper.TurkishVoiceStyles)
            {
                var item = new ComboBoxItem
                {
                    Content = style.Value,
                    Tag = style.Key
                };
                
                cmbSpeakingStyle.Items.Add(item);
            }
            
            // Select default style
            if (cmbSpeakingStyle.Items.Count > 0)
            {
                cmbSpeakingStyle.SelectedIndex = 0;
            }
        }
        
        private void SelectVoiceByName(string voiceName)
        {
            foreach (ComboBoxItem item in cmbVoiceSelection.Items)
            {
                if (item.Tag.ToString() == voiceName)
                {
                    cmbVoiceSelection.SelectedItem = item;
                    return;
                }
            }
            
            // If voice not found, select first item
            if (cmbVoiceSelection.Items.Count > 0 && cmbVoiceSelection.SelectedItem == null)
            {
                cmbVoiceSelection.SelectedIndex = 0;
            }
        }
        
        private void ChkAutoVoiceSelection_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Show/hide voice selection based on checkbox state
            voiceSelectionPanel.Visibility = chkAutoVoiceSelection.IsChecked == true ? 
                Visibility.Collapsed : Visibility.Visible;
            
            // If auto-selection is enabled, update the selected voice based on content
            if (chkAutoVoiceSelection.IsChecked == true && !string.IsNullOrEmpty(_textContent))
            {
                string recommendedVoice = TurkishVoiceHelper.GetAppropriateVoice(_textContent);
                SelectVoiceByName(recommendedVoice);
            }
        }
        
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            // Get voice selection
            if (chkAutoVoiceSelection.IsChecked == true)
            {
                // Use auto-selected voice
                SpeechOptions.VoiceName = TurkishVoiceHelper.GetAppropriateVoice(_textContent);
            }
            else
            {
                // Get the manually selected voice
                var selectedVoiceItem = cmbVoiceSelection.SelectedItem as ComboBoxItem;
                SpeechOptions.VoiceName = selectedVoiceItem?.Tag.ToString() ?? 
                    TurkishVoiceHelper.GetRecommendedMaleVoice();
            }
            
            // Get speaking style
            var selectedStyleItem = cmbSpeakingStyle.SelectedItem as ComboBoxItem;
            SpeechOptions.SpeakingStyle = selectedStyleItem?.Tag.ToString() ?? "general";
            
            // Get speech rate and pitch
            SpeechOptions.SpeakingRate = (float)sliderSpeechRate.Value;
            SpeechOptions.PitchAdjustment = (int)sliderPitchAdjustment.Value;
            
            // Get advanced options
            SpeechOptions.UseSSML = chkUseSSML.IsChecked ?? true;
            SpeechOptions.OutputFormat = (cmbOutputFormat.SelectedItem as ComboBoxItem)?.Content.ToString()
                ?.Split(' ')[0].ToLower() ?? "wav";
            
            // Set dialog result and close
            DialogResult = true;
            Close();
        }
        
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Close without saving
            DialogResult = false;
            Close();
        }
    }
}
