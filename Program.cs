using Italbytz.Common.Abstractions;
using Italbytz.Music.Abstractions;
using Italbytz.Music.ITunes.Client;

return await MusicCoverArtCli.RunAsync(args);

internal static class MusicCoverArtCli
{
    public static async Task<int> RunAsync(string[] args)
    {
        CliOptions? options;

        try
        {
            options = CliOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine();
            PrintUsage();
            return 1;
        }

        if (options is null)
        {
            PrintUsage();
            return 0;
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("music-cover-art/0.1");

        var searchEngine = new ITunesSearchClient(httpClient);
        Result<List<MusicSearchResult>> result;

        try
        {
            result = await searchEngine.Execute(new MusicSearchQuery(options.Term, options.Limit));
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"iTunes search failed: {exception.Message}");
            return 1;
        }

        var tracks = result.Value;
        if (tracks.Count == 0)
        {
            Console.WriteLine($"No tracks found for '{options.Term}'.");
            return 0;
        }

        PrintTracks(tracks);

        if (options.DownloadIndex is null)
        {
            return 0;
        }

        if (options.DownloadIndex.Value < 0 || options.DownloadIndex.Value >= tracks.Count)
        {
            Console.Error.WriteLine($"Download index {options.DownloadIndex.Value} is out of range for {tracks.Count} search result(s).");
            return 1;
        }

        var selectedTrack = tracks[options.DownloadIndex.Value];

        try
        {
            var savedFile = await CoverArtDownloader.DownloadAsync(httpClient, selectedTrack, options.OutputPath, options.Size);
            Console.WriteLine();
            Console.WriteLine($"Saved cover art to {savedFile}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Cover download failed: {exception.Message}");
            return 1;
        }
    }

    private static void PrintTracks(IReadOnlyList<MusicSearchResult> tracks)
    {
        for (var index = 0; index < tracks.Count; index++)
        {
            var track = tracks[index];
            Console.WriteLine($"[{index}] {track.ArtistName} - {track.TrackName} ({track.CollectionName})");
            Console.WriteLine($"    Artwork: {track.CoverArt.MediumUri}");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  music-cover-art search <term> [--limit <n>] [--download-first | --download-index <n>] [--output <path>] [--size <small|medium|large>]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  music-cover-art search Daft Punk");
        Console.WriteLine("  music-cover-art search \"Daft Punk\" --download-first --output ./covers --size large");
        Console.WriteLine("  music-cover-art search \"Daft Punk\" --download-index 1 --output ./covers --size medium");
    }
}

internal sealed class CliOptions
{
    private CliOptions(string term, int limit, int? downloadIndex, string outputPath, CoverArtSize size)
    {
        Term = term;
        Limit = limit;
        DownloadIndex = downloadIndex;
        OutputPath = outputPath;
        Size = size;
    }

    public string Term { get; }

    public int Limit { get; }

    public int? DownloadIndex { get; }

    public string OutputPath { get; }

    public CoverArtSize Size { get; }

    public static CliOptions? Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return null;
        }

        if (!string.Equals(args[0], "search", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only the 'search' command is supported in the first slice.");
        }

        var termParts = new List<string>();
        var limit = 5;
        int? downloadIndex = null;
        var outputPath = "./covers";
        var size = CoverArtSize.Medium;

        for (var index = 1; index < args.Count; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                termParts.Add(argument);
                continue;
            }

            switch (argument)
            {
                case "--download-first":
                    EnsureDownloadModeNotSet(downloadIndex, argument);
                    downloadIndex = 0;
                    break;
                case "--download-index":
                    EnsureDownloadModeNotSet(downloadIndex, argument);
                    var rawIndex = ReadOptionValue(args, ++index, argument);
                    if (!int.TryParse(rawIndex, out var parsedIndex) || parsedIndex < 0)
                    {
                        throw new ArgumentException("--download-index must be a non-negative integer.");
                    }

                    downloadIndex = parsedIndex;
                    break;
                case "--output":
                    outputPath = ReadOptionValue(args, ++index, argument);
                    break;
                case "--size":
                    size = ParseSize(ReadOptionValue(args, ++index, argument));
                    break;
                case "--limit":
                    var rawLimit = ReadOptionValue(args, ++index, argument);
                    if (!int.TryParse(rawLimit, out limit) || limit < 1 || limit > 200)
                    {
                        throw new ArgumentException("--limit must be an integer between 1 and 200.");
                    }

                    break;
                default:
                    throw new ArgumentException($"Unknown option '{argument}'.");
            }
        }

        if (termParts.Count == 0)
        {
            throw new ArgumentException("The search command requires a search term.");
        }

        return new CliOptions(string.Join(" ", termParts), limit, downloadIndex, outputPath, size);
    }

    private static void EnsureDownloadModeNotSet(int? downloadIndex, string option)
    {
        if (downloadIndex is not null)
        {
            throw new ArgumentException($"Option '{option}' cannot be combined with another download mode.");
        }
    }

    private static string ReadOptionValue(IReadOnlyList<string> args, int index, string option)
    {
        if (index >= args.Count || args[index].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Option '{option}' requires a value.");
        }

        return args[index];
    }

    private static CoverArtSize ParseSize(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "small" => CoverArtSize.Small,
            "medium" => CoverArtSize.Medium,
            "large" => CoverArtSize.Large,
            _ => throw new ArgumentException("--size must be one of: small, medium, large."),
        };
    }
}

internal static class CoverArtDownloader
{
    public static async Task<string> DownloadAsync(HttpClient httpClient, MusicSearchResult track, string outputPath, CoverArtSize size)
    {
        Directory.CreateDirectory(outputPath);

        var artworkUri = track.CoverArt.GetUri(size);
        using var response = await httpClient.GetAsync(artworkUri);
        response.EnsureSuccessStatusCode();

        var extension = Path.GetExtension(artworkUri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var fileName = $"{Sanitize(track.ArtistName)}-{Sanitize(track.TrackName)}-{track.Id}{extension}";
        var filePath = Path.GetFullPath(Path.Combine(outputPath, fileName));

        await using var fileStream = File.Create(filePath);
        await response.Content.CopyToAsync(fileStream);

        return filePath;
    }
    private static string Sanitize(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var buffer = value
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray();

        return string.Join(string.Empty, new string(buffer)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Trim('-');
    }
}