using System.Globalization;

namespace AlignCop.Analyzers;

internal static class RuleIdentifiers
{
    public const string AlignVariableAssignments = "AA1000";
    public const string AlignEnumValues          = "AA1002";

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://github.com/ins0mniaque/AlignCop/blob/main/docs/Rules/{0}.md", identifier);
    }
}