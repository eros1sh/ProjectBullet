
using System;
using System.Threading.Tasks;
using CommandLine;
using ProjectBullet.Native.Updater.Helpers;
using Spectre.Console;

namespace ProjectBullet.Native.Updater;

public static class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            await new Parser(with => { with.CaseInsensitiveEnumValues = true; }).ParseArguments<CliOptions>(args)
                .WithParsedAsync(async opts => await UpdateAsync(opts));
        }
        catch (Exception ex)
        {
            Utils.ExitWithError(ex);
        }
    }

    private static async Task UpdateAsync(CliOptions options)
    {
        // Validate the repository
        InputValidation.ValidateRepository(options.Repository);

        // If the channel was not specified, ask the user (or default to Release in silent mode)
        if (options.Silent)
        {
            options.Channel ??= BuildChannel.Release;
        }
        else
        {
            options.Channel ??= Channel.AskForChannel();
        }

        // Check if the main app is running
        await RequirementsChecker.EnsureOb2NativeNotRunningAsync();

        // Make sure the user has the required .NET runtime installed
        await RequirementsChecker.EnsureDotNetInstalledAsync(options.Silent);

        // Fetch info from remote
        using var githubClient = new GitHubClient(options.Repository, options.Channel.Value, options.Username, options.Token);
        var remoteVersionInfo = await githubClient.FetchRemoteVersionAsync();

        AnsiConsole.MarkupLineInterpolated($"[green]Remote version: {remoteVersionInfo.Version}[/]");

        // Get the current version
        var currentVersion = await FileSystemHelper.GetLocalVersionAsync();

        if (currentVersion is null)
        {
            AnsiConsole.MarkupLine("[yellow]version.txt not found, assuming this is a clean install[/]");

            if (!options.Silent)
            {
                var cleanInstall = AnsiConsole.Prompt(
                    new ConfirmationPrompt("Do you want to proceed and download the latest version?"));

                if (!cleanInstall)
                {
                    AnsiConsole.MarkupLine("[yellow]Exiting...[/]");
                    Environment.Exit(0);
                }
            }
        }
        else
        {
            if (remoteVersionInfo.Version > currentVersion)
            {
                AnsiConsole.MarkupLine("[yellow]Update available![/]");

                if (!options.Silent)
                {
                    var update = AnsiConsole.Prompt(
                        new ConfirmationPrompt("Do you want to proceed and update to the latest version?"));

                    if (!update)
                    {
                        AnsiConsole.MarkupLine("[yellow]Exiting...[/]");
                        Environment.Exit(0);
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[green]Already up to date![/]");

                if (!options.Silent)
                {
                    AnsiConsole.MarkupLine("[green]Press any key to exit...[/]");
                    Console.ReadKey();
                }

                Environment.Exit(0);
            }
        }

        // Download the new build
        await using var buildStream = await githubClient.DownloadBuildAsync(remoteVersionInfo);
        AnsiConsole.MarkupLine("[green]Download complete![/]");

        // Clean up the installation folder
        await FileSystemHelper.CleanupInstallationFolderAsync();

        // Extract the archive
        await FileSystemHelper.ExtractArchiveAsync(buildStream);

        AnsiConsole.MarkupLine("[green]The update was completed successfully.[/]");

        if (options.Silent)
        {
            // In silent mode, relaunch the main application
            try
            {
                System.Diagnostics.Process.Start("ProjectBullet.Native.exe");
            }
            catch
            {
                // If relaunch fails, that's OK — user can manually restart
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[green]You may now restart your ProjectBullet instance![/]");
            AnsiConsole.MarkupLine("[green]Press any key to exit...[/]");
            Console.ReadKey();
        }

        Environment.Exit(0);
    }
}
