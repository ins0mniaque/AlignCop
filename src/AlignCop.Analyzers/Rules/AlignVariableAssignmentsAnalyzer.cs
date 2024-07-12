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

    private static readonly Action<SyntaxNodeAnalysisContext>               AnalyzeBlockAction       = AnalyzeBlock;
    private static readonly Func<StatementSyntax, EqualsValueClauseSyntax?> GetEqualsValueClauseFunc = GetEqualsValueClause;

    private static void AnalyzeBlock(SyntaxNodeAnalysisContext context)
    {
        var block = (BlockSyntax)context.Node;

        foreach (var unalignment in AlignmentAnalyzer.FindUnalignments(block.Statements, GetEqualsValueClauseFunc))
            context.ReportDiagnostic(Diagnostic.Create(Rule, unalignment[0], unalignment.Skip(1)));
    }

    private static EqualsValueClauseSyntax? GetEqualsValueClause(StatementSyntax statementSyntax)
    {
        if (statementSyntax is LocalDeclarationStatementSyntax localDeclarationStatement && localDeclarationStatement.Declaration.Variables.Count is 1)
            return localDeclarationStatement.Declaration.Variables[0].Initializer;

        return null;
    }
}