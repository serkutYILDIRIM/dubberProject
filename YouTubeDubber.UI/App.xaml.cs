using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;
using Xabe.FFmpeg.Downloader;
using Xabe.FFmpeg;
using System;
using System.IO;
using System.Windows.Controls;

namespace YouTubeDubber.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        try 
        {
            // Show a startup loading message
            SplashWindow splash = new SplashWindow("Initializing FFmpeg...");
            splash.Show();
            
            // Set FFmpeg executables path
            string ffmpegDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YouTubeDubber", "FFmpeg");
            
            Directory.CreateDirectory(ffmpegDir);
            FFmpeg.SetExecutablesPath(ffmpegDir);
            
            // Download FFmpeg if needed
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            
            // Close splash screen
            splash.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing FFmpeg: {ex.Message}\nThe application may not function correctly.", 
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Console.WriteLine($"Error initializing FFmpeg: {ex.Message}");
        }
    }
}

/// <summary>
/// Simple splash window to show during initialization
/// </summary>
public class SplashWindow : Window
{
    public SplashWindow(string message)
    {
        Width = 300;
        Height = 150;
        WindowStyle = WindowStyle.None;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        
        Grid grid = new Grid();
        TextBlock text = new TextBlock
        {
            Text = message,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16
        };
        
        grid.Children.Add(text);
        Content = grid;
    }
}

