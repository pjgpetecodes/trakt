using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using TraktNet;
using TraktNet.Exceptions;
using TraktNet.Objects.Authentication;
using TraktNet.Responses;
using Trakt.NET.Examples.Authentication;
using Trakt.NET.Examples.Helper;
using System.Web;
using System.Text.Json;

namespace Trakt.NET.Examples.Authentication;

internal static class AuthenticationOAuthExample
{
    internal static async Task Main()
    {
        var configuration = BuildConfiguration();
        var (clientID, clientSecret, openSubtitlesApiKey) = GetConfigurationSettings(configuration);

        if (string.IsNullOrEmpty(clientID) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(openSubtitlesApiKey))
        {
            Console.WriteLine("Please ensure that the appsettings.json file contains the necessary credentials.");
            return;
        }

        var client = new TraktClient(clientID, clientSecret);

        try
        {
            await AuthenticateAndFetchSubtitles(client, openSubtitlesApiKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    private static (string? clientID, string? clientSecret, string? openSubtitlesApiKey) GetConfigurationSettings(IConfiguration configuration)
    {
        string? clientID = configuration["Trakt:ClientId"];
        string? clientSecret = configuration["Trakt:ClientSecret"];
        string? openSubtitlesApiKey = configuration["OpenSubtitles:ApiKey"];
        return (clientID, clientSecret, openSubtitlesApiKey);
    }

    private static async Task AuthenticateAndFetchSubtitles(TraktClient client, string openSubtitlesApiKey)
    {
        string authorizationUrl = client.Authentication.CreateAuthorizationUrl();

        if (!string.IsNullOrEmpty(authorizationUrl))
        {
            Console.WriteLine("You have to authenticate this application.");
            Console.WriteLine("Please visit the following webpage:");
            Console.WriteLine($"{authorizationUrl}\n");

            Console.WriteLine("Enter the PIN code from Trakt.tv:");
            string? code = Console.ReadLine();

            if (!string.IsNullOrEmpty(code))
            {
                var authorizationResponse = await client.Authentication.GetAuthorizationAsync(code);
                var authorization = authorizationResponse.Value;

                if (authorization.IsValid)
                {
                    Console.WriteLine("-------------- Authentication successful --------------");
                    authorization.WriteAuthorizationInformation();
                    Console.WriteLine("-------------------------------------------------------");

                    var watchedShows = await FetchWatchedShows(client);
                    if (watchedShows != null)
                    {
                        var firstShow = watchedShows.FirstOrDefault();
                        if (firstShow != null)
                        {
                            var downloadUrl = await FetchDownloadLink(firstShow.Show.Title, 1, 1, openSubtitlesApiKey);
                            if (!string.IsNullOrEmpty(downloadUrl))
                            {
                                var subtitleContent = await DownloadSubtitleFile(downloadUrl);
                                Console.WriteLine("Subtitle file contents:");
                                Console.WriteLine(subtitleContent);
                            }
                        }
                        else
                        {
                            Console.WriteLine("No shows found.");
                        }
                    }
                }
            }
        }
    }

    private static async Task<IEnumerable<TraktNet.Objects.Get.Watched.ITraktWatchedShow>> FetchWatchedShows(TraktClient client)
    {
        var watchedShowsResponse = await client.Users.GetWatchedShowsAsync("me");

        if (watchedShowsResponse.IsSuccess)
        {
            Console.WriteLine("Your watched shows:");
            foreach (var show in watchedShowsResponse.Value)
            {
                Console.WriteLine($"- {show.Show.Title}");
            }

            return watchedShowsResponse.Value;
        }
        else
        {
            Console.WriteLine("Failed to retrieve watched shows.");
            return null;
        }
    }

    private static async Task<string?> FetchDownloadLink(string showName, int seasonNumber, int episodeNumber, string openSubtitlesApiKey)
    {
        var query = HttpUtility.UrlEncode(showName);
        var requestUri = $"https://api.opensubtitles.com/api/v1/subtitles?query={query}&season_number={seasonNumber}&episode_number={episodeNumber}";

        var subtitlesResponse = await GetSubtitlesResponse(requestUri, openSubtitlesApiKey);
        var fileId = ExtractFileId(subtitlesResponse);

        if (fileId.HasValue)
        {
            return await GetDownloadUrl(fileId.Value, openSubtitlesApiKey);
        }

        return null;
    }

    private static async Task<string> GetSubtitlesResponse(string requestUri, string openSubtitlesApiKey)
    {
        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(requestUri),
            Headers =
            {
                { "User-Agent", "previouslyon v1.0.0" },
                { "Api-Key", openSubtitlesApiKey },
            },
        };

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static int? ExtractFileId(string subtitlesResponse)
    {
        var jsonDoc = JsonDocument.Parse(subtitlesResponse);
        return jsonDoc.RootElement
            .GetProperty("data")[0]
            .GetProperty("attributes")
            .GetProperty("files")[0]
            .GetProperty("file_id")
            .GetInt32();
    }

    private static async Task<string?> GetDownloadUrl(int fileId, string openSubtitlesApiKey)
    {
        using var httpClient = new HttpClient();
        var downloadRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://api.opensubtitles.com/api/v1/download"),
            Headers =
            {
                { "User-Agent", "previouslyon v1.0.0" },
                { "Accept", "application/json" },
                { "Api-Key", openSubtitlesApiKey },
            },
            Content = new StringContent($"{{\n  \"file_id\": {fileId}\n}}")
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            }
        };

        using var downloadResponse = await httpClient.SendAsync(downloadRequest);
        downloadResponse.EnsureSuccessStatusCode();
        var downloadBody = await downloadResponse.Content.ReadAsStringAsync();
        var downloadJsonDoc = JsonDocument.Parse(downloadBody);
        return downloadJsonDoc.RootElement.GetProperty("link").GetString();
    }

    private static async Task<string> DownloadSubtitleFile(string downloadUrl)
    {
        using var httpClient = new HttpClient();
        var subtitleResponse = await httpClient.GetAsync(downloadUrl);
        subtitleResponse.EnsureSuccessStatusCode();
        return await subtitleResponse.Content.ReadAsStringAsync();
    }
}