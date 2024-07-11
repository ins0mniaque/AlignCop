using System.Collections.Immutable;
using System.Composition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AlignCop.Analyzers.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlignVariableAssignmentsFixer)), Shared]
public sealed class AlignVariableAssignmentsFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AlignVariableAssignments);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root       = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();

        var firstSpan = diagnostic.Location.SourceSpan;
        var lastSpan  = diagnostic.AdditionalLocations.Count > 0 ? diagnostic.AdditionalLocations[diagnostic.AdditionalLocations.Count - 1].SourceSpan : firstSpan;

        if (root.FindToken(firstSpan.Start).Parent.Parent.Parent.Parent is not LocalDeclarationStatementSyntax firstVariableAssignment ||
            root.FindToken(lastSpan.Start).Parent.Parent.Parent.Parent is not LocalDeclarationStatementSyntax lastVariableAssignment)
            return;

        if (firstVariableAssignment.Parent is not BlockSyntax block)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.AlignVariableAssignmentsCodeFixTitle,
                createChangedDocument: cancellationToken => AlignmentFixer.FixUnalignment(context.Document, block.Statements, firstVariableAssignment, lastVariableAssignment, GetEqualsValueClauseFunc, cancellationToken),
                equivalenceKey: nameof(CodeFixResources.AlignVariableAssignmentsCodeFixTitle)),
            diagnostic);
    }

    private static readonly Func<StatementSyntax, EqualsValueClauseSyntax?> GetEqualsValueClauseFunc = GetEqualsValueClause;

    private static EqualsValueClauseSyntax? GetEqualsValueClause(StatementSyntax statementSyntax)
    {
        if (statementSyntax is LocalDeclarationStatementSyntax localDeclarationStatement && localDeclarationStatement.Declaration.Variables.Count is 1)
            return localDeclarationStatement.Declaration.Variables[0].Initializer;

        return null;
    }
}