using System.CommandLine;
using System.CommandLine.Parsing;

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
    Description = "Preview changes without moving files"
};
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

var parseResult = CommandLineParser.Parse(rootCmd, args);
return parseResult.Invoke(parseResult.InvocationConfiguration);
