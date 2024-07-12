using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AlignCop.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AlignVariableAssignmentsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AlignVariableAssignments,
        title: new LocalizableResourceString(nameof(Resources.AlignVariableAssignmentsTitle), Resources.ResourceManager, typeof(Resources)),
        messageFormat: new LocalizableResourceString(nameof(Resources.AlignVariableAssignmentsMessageFormat), Resources.ResourceManager, typeof(Resources)),
        category: RuleCategories.Readability,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.AlignVariableAssignmentsDescription), Resources.ResourceManager, typeof(Resources)),
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AlignVariableAssignments));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeBlockAction, SyntaxKind.Block);
    }

    private  static readonly Action<SyntaxNodeAnalysisContext>                   AnalyzeBlockAction      = AnalyzeBlock;
    internal static readonly Selector<StatementSyntax, SyntaxNode?, SyntaxNode?> GetNodesToAlignSelector = GetNodesToAlign;

    private static void AnalyzeBlock(SyntaxNodeAnalysisContext context)
    {
        var block = (BlockSyntax)context.Node;

        foreach (var unalignment in AlignmentAnalyzer.FindUnalignments(block.Statements, GetNodesToAlignSelector))
            context.ReportDiagnostic(Diagnostic.Create(Rule, unalignment[0], unalignment.Skip(1)));
    }

    private static void GetNodesToAlign(StatementSyntax statementSyntax, out SyntaxNode? nodeToAlignA, out SyntaxNode? nodeToAlignB)
    {
        if (statementSyntax.RawKind is (int)SyntaxKind.LocalDeclarationStatement &&
            statementSyntax is LocalDeclarationStatementSyntax localDeclarationStatement &&
            localDeclarationStatement.Declaration.Variables.Count is 1)
        {
            var variable = localDeclarationStatement.Declaration.Variables[0];

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