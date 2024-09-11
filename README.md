# Blazor Hybrid Web App

This project is a Blazor Hybrid Web App that allows users to generate "Previously on" type summaries for TV series episodes. The application interacts with external services like Azure OpenAI, OpenSubtitles, and Trakt to fetch and summarize subtitle content.

## Prerequisites

- .NET SDK
- Visual Studio or Visual Studio Code
- Trakt API credentials
- OpenSubtitles API key

## Setup

1. **Clone the repository**:
    ```sh
    git clone https://github.com/your-repo/blazor-hybrid-web-app.git
    cd blazor-hybrid-web-app
    ```

2. **Create `appsettings.json` file**:

    In the root directory of the project, create a file named `appsettings.json` with the following content:

    ```json
    {
      "Trakt": {
        "ClientId": "your-trakt-client-id",
        "ClientSecret": "your-trakt-client-secret"
      },
      "OpenSubtitles": {
        "ApiKey": "your-opensubtitles-api-key"
      }
    }
    ```

    Replace the placeholders with your actual Trakt API credentials and OpenSubtitles API key.

3. **Restore dependencies**:
    ```sh
    dotnet restore
    ```

4. **Run the application**:
    ```sh
    dotnet run
    ```

## Usage

1. Open the application in your browser.
2. Enter the show name, season number, and episode number.
3. Click the "Generate Summary" button to generate a "Previously on" summary for the selected episodes.

## License

This project is licensed under the MIT License. See the LICENSE file for more details.