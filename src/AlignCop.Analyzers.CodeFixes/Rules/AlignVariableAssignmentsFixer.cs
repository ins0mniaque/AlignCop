using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using AlignCop.Analyzers.Internal;

namespace AlignCop.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlignVariableAssignmentsFixer)), Shared]
public class AlignVariableAssignmentsFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AlignVariableAssignments);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root       = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();

        var firstSpan = diagnostic.Location.SourceSpan;
        var lastSpan  = diagnostic.AdditionalLocations.LastOrDefault()?.SourceSpan ?? firstSpan;

        var firstEnumMember = root.FindToken(firstSpan.Start).Parent.Parent.Parent.Parent as LocalDeclarationStatementSyntax;
        var lastEnumMember  = root.FindToken(lastSpan.Start).Parent.Parent.Parent.Parent as LocalDeclarationStatementSyntax;

        if (firstEnumMember is null || lastEnumMember is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.AlignEnumValuesCodeFixTitle,
                createChangedDocument: cancellationToken => AlignEnumValues(context.Document, firstEnumMember, lastEnumMember, cancellationToken),
                equivalenceKey: nameof(CodeFixResources.AlignEnumValuesCodeFixTitle)),
            diagnostic);
    }

    private async Task<Document> AlignEnumValues(Document document, LocalDeclarationStatementSyntax firstEnumMember, LocalDeclarationStatementSyntax lastEnumMember, CancellationToken cancellationToken)
    {
        var block = firstEnumMember.Parent as BlockSyntax;

        var startIndex = block.Statements.IndexOf(firstEnumMember);
        var endIndex   = block.Statements.Count;

        var maxEqualColumn = -1;

        for (var index = startIndex; index < endIndex; index++)
        {
            var member = block.Statements[index] as LocalDeclarationStatementSyntax;

            if (member?.Declaration.Variables[ 0 ].Initializer is { } equal)
                maxEqualColumn = Math.Max(equal.EqualsToken.GetLineSpan().StartLinePosition.Character, maxEqualColumn);

            if (member == lastEnumMember)
            {
                endIndex = index + 1;
                break;
            }
        }

        if (maxEqualColumn < 0)
            return document;

        var text    = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<TextChange>(endIndex - startIndex);

        for (var index = startIndex; index < endIndex; index++)
        {
            var member = block.Statements[index] as LocalDeclarationStatementSyntax;
            if (member?.Declaration.Variables[ 0 ].Initializer is not { } equal)
                continue;

            var equalColumn = equal.EqualsToken.GetLineSpan().StartLinePosition.Character;
            if (equalColumn < maxEqualColumn)
                changes.Add(new TextChange(member.Declaration.Variables[ 0 ].Initializer.EqualsToken.Span, new string(' ', maxEqualColumn - equalColumn) + member.Declaration.Variables[ 0 ].Initializer.EqualsToken.Text));
        }

        if (changes.Count is 0)
            return document;

        return document.WithText(text.WithChanges(changes));
    }
}