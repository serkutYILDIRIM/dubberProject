using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YouTubeDubber.Core.Models;

namespace YouTubeDubber.UI
{
    /// <summary>
    /// Interaction logic for TurkishDubbingOptionsDialog.xaml
    /// </summary>
    public partial class TurkishDubbingOptionsDialog : Window
    {
        public TurkishDubbingOptions DubbingOptions { get; private set; }
        
        public TurkishDubbingOptionsDialog()
        {
            InitializeComponent();
            
            // Initialize with default options
            DubbingOptions = new TurkishDubbingOptions();
            
            // Set default values
            chkPreserveBackgroundSounds.IsChecked = DubbingOptions.PreserveBackgroundSounds;
            cmbMixingProfile.SelectedIndex = 0; // "Balanced" is default
            chkAddSubtitles.IsChecked = DubbingOptions.AddSubtitles;
            sliderVideoQuality.Value = DubbingOptions.VideoQuality;
            cmbOutputFormat.SelectedIndex = DubbingOptions.OutputFormat.ToLower() == "mp4" ? 0 : 1;
            
            // Set custom mixing values
            sliderBackgroundVolume.Value = DubbingOptions.CustomBackgroundVolume;
            sliderVoiceVolume.Value = DubbingOptions.CustomVoiceVolume;
            sliderDucking.Value = DubbingOptions.CustomDucking;
        }
        
        /// <summary>
        /// Handles the OK button click
        /// </summary>
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            // Save the options
            DubbingOptions.PreserveBackgroundSounds = chkPreserveBackgroundSounds.IsChecked ?? true;
            
            // Get the selected mixing profile
            var selectedItem = cmbMixingProfile.SelectedItem as ComboBoxItem;
            DubbingOptions.MixingProfile = selectedItem?.Tag.ToString() ?? "balanced";
            
            // Save custom mixing values if custom profile is selected
            if (DubbingOptions.MixingProfile == "custom")
            {
                DubbingOptions.CustomBackgroundVolume = (float)sliderBackgroundVolume.Value;
                DubbingOptions.CustomVoiceVolume = (float)sliderVoiceVolume.Value;
                DubbingOptions.CustomDucking = (float)sliderDucking.Value;
            }
            
            // Save subtitle options
            DubbingOptions.AddSubtitles = chkAddSubtitles.IsChecked ?? false;
            DubbingOptions.SubtitlesFilePath = txtSubtitlesPath.Text;
            
            // Save output options
            DubbingOptions.VideoQuality = (int)sliderVideoQuality.Value;
            DubbingOptions.OutputFormat = (cmbOutputFormat.SelectedItem as ComboBoxItem)?.Content.ToString()?.Split(' ')[0].ToLower() ?? "mp4";
            
            // Set dialog result and close
            DialogResult = true;
            Close();
        }
        
        /// <summary>
        /// Handles the Cancel button click
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Close without saving
            DialogResult = false;
            Close();
        }
        
        /// <summary>
        /// Handles the Browse button click for subtitles
        /// </summary>
        private void BtnBrowseSubtitles_Click(object sender, RoutedEventArgs e)
        {
            // Create file dialog
            var dialog = new OpenFileDialog
            {
                Title = "Select Subtitles File",
                Filter = "Subtitle Files|*.srt;*.ass;*.ssa|SRT Files|*.srt|ASS/SSA Files|*.ass;*.ssa|All Files|*.*",
                CheckFileExists = true
            };
            
            // Show dialog
            if (dialog.ShowDialog() == true)
            {
                txtSubtitlesPath.Text = dialog.FileName;
            }
        }
        
        /// <summary>
        /// Handles changes to the Add Subtitles checkbox
        /// </summary>
        private void ChkAddSubtitles_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Show/hide subtitles panel based on checkbox state
            subtitlesPanel.Visibility = chkAddSubtitles.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
        
        /// <summary>
        /// Handles changes to the selected mixing profile
        /// </summary>
        private void CmbMixingProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Show/hide custom mixing panel based on selection
            var selectedItem = cmbMixingProfile.SelectedItem as ComboBoxItem;
            customMixingPanel.Visibility = selectedItem?.Tag.ToString() == "custom" ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
