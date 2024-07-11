using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using AlignCop.Analyzers.Internal;

namespace AlignCop.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AlignEnumValuesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AlignEnumValues,
        title: new LocalizableResourceString(nameof(Resources.AlignEnumValuesTitle), Resources.ResourceManager, typeof(Resources)),
        messageFormat: new LocalizableResourceString(nameof(Resources.AlignEnumValuesMessageFormat), Resources.ResourceManager, typeof(Resources)),
        category: RuleCategories.Readability,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.AlignEnumValuesDescription), Resources.ResourceManager, typeof(Resources)),
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AlignEnumValues));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeEnumDeclarationAction, SyntaxKind.EnumDeclaration);
    }

    private static readonly Action<SyntaxNodeAnalysisContext>                           AnalyzeEnumDeclarationAction = AnalyzeEnumDeclaration;
    private static readonly Func<EnumMemberDeclarationSyntax, EqualsValueClauseSyntax?> GetEqualsValueClauseFunc     = GetEqualsValueClause;

    private static void AnalyzeEnumDeclaration(SyntaxNodeAnalysisContext context)
    {
        var enumDeclaration = (EnumDeclarationSyntax)context.Node;

        foreach (var unalignment in AlignmentAnalyzer.FindUnalignments(context, enumDeclaration.Members, GetEqualsValueClauseFunc))
            context.ReportDiagnostic(Diagnostic.Create(Rule, unalignment[0], unalignment.Skip(1), enumDeclaration.Identifier.Text));
    }

    private static EqualsValueClauseSyntax? GetEqualsValueClause(EnumMemberDeclarationSyntax enumDeclaration)
    {
        return enumDeclaration.EqualsValue;
    }
}