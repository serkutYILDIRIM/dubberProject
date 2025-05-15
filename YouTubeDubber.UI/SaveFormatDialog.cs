using System.Windows;
using System.Windows.Controls;

namespace YouTubeDubber.UI
{
    /// <summary>
    /// Dialog for selecting the save format for transcription files
    /// </summary>
    public class SaveFormatDialog : Window
    {
        private ComboBox _comboFormats;
        private string _selectedFormat = "txt";
        
        /// <summary>
        /// Gets the selected format from the dialog
        /// </summary>
        public string SelectedFormat => _selectedFormat;
        
        /// <summary>
        /// Initializes a new instance of the SaveFormatDialog
        /// </summary>
        public SaveFormatDialog()
        {
            // Configure window
            Title = "Select Save Format";
            Width = 350;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            // Create grid layout
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.Margin = new Thickness(20);
            
            // Add title text
            var titleText = new TextBlock
            {
                Text = "Select a format to save the transcription:",
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(titleText, 0);
            
            // Create format selection combobox
            _comboFormats = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            // Add format options
            _comboFormats.Items.Add("txt");
            _comboFormats.Items.Add("srt");
            _comboFormats.Items.Add("json");
            _comboFormats.SelectedIndex = 0;
            
            Grid.SetRow(_comboFormats, 1);
            
            // Create button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            // Create OK button
            var buttonOk = new Button
            {
                Content = "OK",
                IsDefault = true,
                MinWidth = 80,
                Margin = new Thickness(0, 0, 10, 0)
            };
            buttonOk.Click += (s, e) =>
            {
                _selectedFormat = _comboFormats.SelectedItem.ToString();
                DialogResult = true;
                Close();
            };
            
            // Create Cancel button
            var buttonCancel = new Button
            {
                Content = "Cancel",
                IsCancel = true,
                MinWidth = 80
            };
            
            // Add buttons to panel
            buttonPanel.Children.Add(buttonOk);
            buttonPanel.Children.Add(buttonCancel);
            Grid.SetRow(buttonPanel, 2);
            
            // Add elements to grid
            grid.Children.Add(titleText);
            grid.Children.Add(_comboFormats);
            grid.Children.Add(buttonPanel);
            
            // Set grid as content
            Content = grid;
        }
    }
}
