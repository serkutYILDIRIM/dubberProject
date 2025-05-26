using System;
using System.Threading.Tasks;
using YouTubeDubber.Core.Tests;

namespace OfflineTranslationTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await TestOfflineTranslation.Main(args);
        }
    }
}
