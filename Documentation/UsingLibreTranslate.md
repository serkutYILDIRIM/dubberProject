# Using LibreTranslate with YouTube Dubber

This document explains how to set up and use LibreTranslate as a translation service for YouTube Dubber.

## Overview

LibreTranslate is an open-source machine translation API that can be used as an alternative to proprietary translation services. It allows you to:

- Run translations completely locally without sending data to third parties
- Access translations through a REST API
- Translate between multiple languages

## Options for Using LibreTranslate

There are three ways to use LibreTranslate with YouTube Dubber:

1. **Public Instances**: Use an existing publicly available LibreTranslate instance
2. **Local Installation**: Run a local LibreTranslate server on your own machine
3. **Self-hosted**: Deploy LibreTranslate on your own server

## Using a Public Instance

Several public LibreTranslate instances are available:

- https://libretranslate.de/ (Most stable, may have rate limits)
- https://translate.argosopentech.com/

To use a public instance in YouTube Dubber:
1. No setup is required
2. In the application, select LibreTranslate as the translation service
3. Enter the URL of the public instance
4. Note that public instances may have rate limits or be slower

## Setting Up a Local LibreTranslate Server

Running LibreTranslate locally gives you full control and avoids rate limits, but requires more resources.

### Requirements

- Docker (recommended method)
- 4GB+ RAM (depending on which models you use)
- ~2GB disk space

### Installation with Docker

1. Install Docker from [https://www.docker.com/products/docker-desktop/](https://www.docker.com/products/docker-desktop/)

2. Open PowerShell and run:

```powershell
# Pull the LibreTranslate Docker image
docker pull libretranslate/libretranslate

# Run LibreTranslate (default settings)
docker run -d -p 5000:5000 libretranslate/libretranslate
```

3. To run with specific options (like requiring API keys):

```powershell
# Run with API key requirement
docker run -d -p 5000:5000 -e LT_REQUIRE_API_KEY=true libretranslate/libretranslate

# Generate an API key
docker exec -it [container_id] ltmanage keys add --name "YouTube Dubber"
```

4. The server will be accessible at http://localhost:5000

### Installation without Docker

If you prefer not to use Docker, you can install LibreTranslate directly:

1. Install Python 3.8 or newer
2. Clone the repository and install:

```powershell
git clone https://github.com/LibreTranslate/LibreTranslate
cd LibreTranslate
pip install -e .
```

3. Run the server:

```powershell
libretranslate --host 127.0.0.1 --port 5000
```

## Configuring YouTube Dubber to Use LibreTranslate

1. In the application settings, select "LibreTranslate" as the translation service
2. Enter the URL of your LibreTranslate instance (e.g., http://localhost:5000 for local instance)
3. If required, enter your API key
4. Choose source and target languages

## Language Support

LibreTranslate supports many languages, but the specific languages available depend on the models installed on the server. The default installation includes:

- English (en)
- Arabic (ar)
- German (de)
- Spanish (es)
- French (fr)
- Italian (it)
- Dutch (nl)
- Polish (pl)
- Portuguese (pt)
- Russian (ru)
- Chinese (zh)
- Turkish (tr)

## Offline Mode

YouTube Dubber's LibreTranslate service includes offline mode capabilities to handle situations when the translation server is unavailable or you don't have an internet connection.

### How Offline Mode Works

1. **Translation Caching**: Successful translations are cached locally for future use
2. **Automatic Fallback**: If a translation server is unreachable, the service automatically falls back to:
   - Previously cached translations
   - Built-in dictionary of common phrases 
   - Idiomatic expression handling
   
### Configuring Offline Mode

Offline mode is enabled by default, but can be configured when initializing the LibreTranslate service:

```csharp
var service = new LibreTranslateService(
    defaultOptions,
    apiUrl,
    enableOfflineMode: true,  // Enable/disable offline mode
    maxRetries: 3,            // Number of retry attempts before falling back to offline
    cachePath: "custom/path"  // Optional custom path for the cache
);
```

### Managing the Translation Cache

The translation cache is stored in your application data folder by default:
- `%LocalAppData%\YouTubeDubber\TranslationCache`

You can manage the cache programmatically:

```csharp
// Get cache statistics
var stats = service.GetCacheStatistics();
Console.WriteLine($"Cache size: {stats.CacheSize} translations");

// Clear the cache when needed
service.ClearCache();
```

## Troubleshooting

- **Service Unavailable**: The service will automatically fall back to offline mode if enabled
- **Translation Fails**: Check that the language pair is supported or enable offline mode
- **Slow Performance**: Local installations may be slow on first run as they download models
- **Memory Issues**: Reduce the number of languages or use smaller models
- **Network Errors**: Check your internet connection, or rely on offline mode

## Additional Resources

- [LibreTranslate GitHub Repository](https://github.com/LibreTranslate/LibreTranslate)
- [LibreTranslate Documentation](https://github.com/LibreTranslate/LibreTranslate/blob/main/README.md)
- [Argos Translate](https://github.com/argosopentech/argos-translate) (The underlying translation library)
