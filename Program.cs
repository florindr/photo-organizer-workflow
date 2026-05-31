using System.CommandLine;
using System.CommandLine.Parsing;

// ── shared options ────────────────────────────────────────────────────────────

var sourceArg = new Argument<DirectoryInfo>("source")
{
    Description = "Source directory containing media files"
};
var outputArg = new Argument<DirectoryInfo>("output")
{
    Description = "Destination root directory"
};
var formatOpt = new Option<string>("--format", "-f")
{
    Description = "Destination folder structure using C# date tokens (MM=month, dd=day, yyyy=year)",
    DefaultValueFactory = _ => "yyyy/yyyy-MM/yyyy-MM-dd"
};
var dryRunOpt = new Option<bool>("--dry-run", "-n")
{
    Description = "Preview changes without modifying files"
};

// ── organize command (default / root) ─────────────────────────────────────────

var extensionsOpt = new Option<string[]>("--extensions", "-e")
{
    Description = "File extensions to process (default: .mp4 .mov .avi .mkv .3gp)",
    DefaultValueFactory = _ => [".mp4", ".mov", ".avi", ".mkv", ".3gp"],
    AllowMultipleArgumentsPerToken = true
};
var copyOpt = new Option<bool>("--copy", "-c")
{
    Description = "Copy files instead of moving them"
};

var rootCmd = new RootCommand("Organizes media files into a date-based folder structure");
rootCmd.Add(sourceArg);
rootCmd.Add(outputArg);
rootCmd.Add(formatOpt);
rootCmd.Add(dryRunOpt);
rootCmd.Add(extensionsOpt);
rootCmd.Add(copyOpt);

rootCmd.SetAction(result =>
{
    new FileOrganizer(
        result.GetValue(sourceArg)!,
        result.GetValue(outputArg)!,
        result.GetValue(formatOpt)!,
        result.GetValue(dryRunOpt),
        result.GetValue(extensionsOpt)!,
        result.GetValue(copyOpt)
    ).Run();
});

// ── sync subcommand ───────────────────────────────────────────────────────────

var syncSourceArg = new Argument<DirectoryInfo>("source")
{
    Description = "Source directory to sync from"
};
var syncOutputArg = new Argument<DirectoryInfo>("output")
{
    Description = "Destination root directory (organized structure)"
};
var syncFormatOpt = new Option<string>("--format", "-f")
{
    Description = "Folder structure using C# date tokens",
    DefaultValueFactory = _ => "yyyy/yyyy-MM/yyyy-MM-dd"
};
var syncDryRunOpt = new Option<bool>("--dry-run", "-n")
{
    Description = "Preview what would be synced without modifying files"
};
var syncDeleteSyncedOpt = new Option<bool>("--delete-synced", "-d")
{
    Description = "After syncing, prompt to delete source files confirmed at destination"
};

var syncCmd = new Command("sync", "Sync all media from source into the organized destination; lists suspected junk separately");
syncCmd.Add(syncSourceArg);
syncCmd.Add(syncOutputArg);
syncCmd.Add(syncFormatOpt);
syncCmd.Add(syncDryRunOpt);
syncCmd.Add(syncDeleteSyncedOpt);

syncCmd.SetAction(result =>
{
    new Syncer(
        result.GetValue(syncSourceArg)!,
        result.GetValue(syncOutputArg)!,
        result.GetValue(syncFormatOpt)!,
        result.GetValue(syncDryRunOpt),
        result.GetValue(syncDeleteSyncedOpt)
    ).Run();
});

rootCmd.Add(syncCmd);

// ── quarantine subcommand ─────────────────────────────────────────────────────

var quarantineSourceArg = new Argument<DirectoryInfo>("source")
{
    Description = "Organized destination folder to scan for junk"
};
var quarantineDirArg = new Argument<DirectoryInfo>("quarantine-dir")
{
    Description = "Folder to move suspected junk into (preserves subfolder structure)"
};
var quarantineDryRunOpt = new Option<bool>("--dry-run", "-n")
{
    Description = "Preview suspected junk without moving anything"
};
var quarantineFromYearOpt = new Option<int>("--from-year")
{
    Description = "Only scan year folders >= this value",
    DefaultValueFactory = _ => 1900
};
var quarantineToYearOpt = new Option<int>("--to-year")
{
    Description = "Only scan year folders <= this value",
    DefaultValueFactory = _ => 9999
};

var quarantineCmd = new Command("quarantine", "Find suspected junk inside an organized folder and move it to a quarantine directory for inspection");
quarantineCmd.Add(quarantineSourceArg);
quarantineCmd.Add(quarantineDirArg);
quarantineCmd.Add(quarantineDryRunOpt);
quarantineCmd.Add(quarantineFromYearOpt);
quarantineCmd.Add(quarantineToYearOpt);

quarantineCmd.SetAction(result =>
{
    new Quarantiner(
        result.GetValue(quarantineSourceArg)!,
        result.GetValue(quarantineDirArg)!,
        result.GetValue(quarantineDryRunOpt),
        result.GetValue(quarantineFromYearOpt),
        result.GetValue(quarantineToYearOpt)
    ).Run();
});

rootCmd.Add(quarantineCmd);

// ── dispatch ──────────────────────────────────────────────────────────────────

var parseResult = CommandLineParser.Parse(rootCmd, args);
return parseResult.Invoke(parseResult.InvocationConfiguration);
