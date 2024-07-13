using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AlignCop.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AlignVariableAssignmentsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor rule = new
    (
        id:                 RuleIdentifiers.AlignVariableAssignments,
        title:              RuleResources.GetLocalizableString(nameof(Resources.AlignVariableAssignmentsTitle)),
        messageFormat:      RuleResources.GetLocalizableString(nameof(Resources.AlignVariableAssignmentsMessageFormat)),
        category:           RuleCategories.Readability,
        defaultSeverity:    DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description:        RuleResources.GetLocalizableString(nameof(Resources.AlignVariableAssignmentsDescription)),
        helpLinkUri:        RuleIdentifiers.GetHelpUri(RuleIdentifiers.AlignVariableAssignments)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeBlockAction, SyntaxKind.Block);
    }

    private static readonly Action<SyntaxNodeAnalysisContext> AnalyzeBlockAction = AnalyzeBlock;

    private static void AnalyzeBlock(SyntaxNodeAnalysisContext context)
    {
        var block = (BlockSyntax)context.Node;

        foreach (var unalignment in AlignmentAnalyzer.FindUnalignments(block.Statements, GetNodesToAlignSelector))
            context.ReportDiagnostic(Diagnostic.Create(rule, unalignment.Location, unalignment.AdditionalLocations));
    }

    internal static readonly Selector<StatementSyntax, SyntaxNode?, SyntaxNode?> GetNodesToAlignSelector = GetNodesToAlign;

    private static void GetNodesToAlign(StatementSyntax statementSyntax, out SyntaxNode? nodeToAlignA, out SyntaxNode? nodeToAlignB)
    {
        if (statementSyntax.RawKind is (int)SyntaxKind.LocalDeclarationStatement &&
            statementSyntax is LocalDeclarationStatementSyntax statement &&
            statement.Declaration.Variables.Count is 1)
        {
            var variable = statement.Declaration.Variables[0];

            nodeToAlignA = variable;
            nodeToAlignB = variable.Initializer;
        }
        else
        {
            nodeToAlignA = null;
            nodeToAlignB = null;
        }
    }
}