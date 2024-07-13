using Microsoft.CodeAnalysis;

namespace AlignCop.Analyzers;

internal static class RuleResources
{
    public static LocalizableResourceString GetLocalizableString(string nameOfLocalizableResource)
    {
        return new LocalizableResourceString(nameOfLocalizableResource, Resources.ResourceManager, typeof(Resources));
    }
}