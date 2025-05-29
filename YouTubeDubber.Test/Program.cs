using System;
using System.Threading.Tasks;
using YouTubeDubber.Core.Tests;

namespace YouTubeDubber.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("YouTube Dubber - Speech Services Test");
            Console.WriteLine("====================================");
            
            try
            {
                if (args.Length > 0)
                {
                    string command = args[0].ToLower();
                    
                    switch (command)
                    {
                        case "whisper":
                            await ServiceTests.TestWhisperServiceAsync();
                            break;
                            
                        case "silero":
                            await ServiceTests.TestSileroServiceAsync();
                            break;
                            
                        default:
                            await ServiceTests.RunAllTestsAsync();
                            break;
                    }
                }
                else
                {
                    // No args, run menu
                    await ShowMenuAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static async Task ShowMenuAsync()
        {
            while (true)
            {
                Console.WriteLine("\nChoose a test to run:");
                Console.WriteLine("1. Test Whisper Speech Recognition");
                Console.WriteLine("2. Test Silero Text-to-Speech");
                Console.WriteLine("3. Run All Tests");
                Console.WriteLine("0. Exit");
                
                Console.Write("\nYour choice: ");
                var key = Console.ReadKey();
                Console.WriteLine();
                
                switch (key.KeyChar)
                {
                    case '1':
                        await ServiceTests.TestWhisperServiceAsync();
                        break;
                        
                    case '2':
                        await ServiceTests.TestSileroServiceAsync();
                        break;
                        
                    case '3':
                        await ServiceTests.RunAllTestsAsync();
                        break;
                        
                    case '0':
                        return;
                        
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }
    }
}
