using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AlignCop.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AlignEnumValuesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor rule = new
    (
        id:                 RuleIdentifiers.AlignEnumValues,
        title:              RuleResources.GetLocalizableString(nameof(Resources.AlignEnumValuesTitle)),
        messageFormat:      RuleResources.GetLocalizableString(nameof(Resources.AlignEnumValuesMessageFormat)),
        category:           RuleCategories.Readability,
        defaultSeverity:    DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description:        RuleResources.GetLocalizableString(nameof(Resources.AlignEnumValuesDescription)),
        helpLinkUri:        RuleIdentifiers.GetHelpUri(RuleIdentifiers.AlignEnumValues)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeEnumDeclarationAction, SyntaxKind.EnumDeclaration);
    }

    private static readonly Action<SyntaxNodeAnalysisContext> AnalyzeEnumDeclarationAction = AnalyzeEnumDeclaration;

    private static void AnalyzeEnumDeclaration(SyntaxNodeAnalysisContext context)
    {
        var enumDeclaration = (EnumDeclarationSyntax)context.Node;

        foreach (var unalignment in AlignmentAnalyzer.FindUnalignments(enumDeclaration.Members, GetNodeToAlignSelector))
            context.ReportDiagnostic(Diagnostic.Create(rule, unalignment.Location, unalignment.AdditionalLocations, enumDeclaration.Identifier.Text));
    }

    internal static readonly Selector<EnumMemberDeclarationSyntax, SyntaxNode?> GetNodeToAlignSelector = GetNodeToAlign;

    private static void GetNodeToAlign(EnumMemberDeclarationSyntax enumDeclaration, out SyntaxNode? nodeToAlign)
    {
        nodeToAlign = enumDeclaration.EqualsValue;
    }
}