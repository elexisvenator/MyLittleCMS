using MyLittleCMS.ApiService.DataModels;

namespace MyLittleCMS.ApiService.Models;

public static class Sanitize
{
    public static string Names(string? name) => name!.Trim();
    public static UriComponent UriComponent(string? uriComponent) => DataModels.UriComponent.From(uriComponent!.Trim());
    public static string PageContent(string? content) => content?.TrimEnd().TrimLeadingEmptyLines() ?? "";
    public static List<string> Tags(IEnumerable<string>? tags) 
        => tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToList() ?? [];

    private static string TrimLeadingEmptyLines(this string input)
    {
        return string.Join("\n", input.Split('\n').SkipWhile(string.IsNullOrWhiteSpace));
    }
}