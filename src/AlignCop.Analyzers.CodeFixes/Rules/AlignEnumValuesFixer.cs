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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlignEnumValuesFixer)), Shared]
public class AlignEnumValuesFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AlignEnumValues);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic     = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var firstEnumMember = root.FindToken(diagnosticSpan.Start).Parent as EnumMemberDeclarationSyntax;
        var lastEnumMember  = root.FindToken(diagnosticSpan.End).Parent as EnumMemberDeclarationSyntax;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.AlignEnumValuesCodeFixTitle,
                createChangedDocument: cancellationToken => AlignEnumValues(context.Document, firstEnumMember, lastEnumMember, cancellationToken),
                equivalenceKey: nameof(CodeFixResources.AlignEnumValuesCodeFixTitle)),
            diagnostic);
    }

    private async Task<Document> AlignEnumValues(Document document, EnumMemberDeclarationSyntax firstEnumMember, EnumMemberDeclarationSyntax lastEnumMember, CancellationToken cancellationToken)
    {
        var enumDeclaration = firstEnumMember.Parent as EnumDeclarationSyntax;

        var startIndex = enumDeclaration.Members.IndexOf(firstEnumMember);
        var endIndex   = enumDeclaration.Members.Count;

        var maxEqualColumn = -1;

        for (var index = startIndex; index < endIndex; index++)
        {
            var member = enumDeclaration.Members[index];

            if (member.EqualsValue is { } equal)
                maxEqualColumn = Math.Max(equal.EqualsToken.GetLineSpan().StartLinePosition.Character, maxEqualColumn);

            if (member == lastEnumMember)
            {
                endIndex = index;
                break;
            }
        }

        if (maxEqualColumn < 0)
            return document;

        var text    = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<TextChange>(endIndex - startIndex);

        for (var index = startIndex; index < endIndex; index++)
        {
            var member = enumDeclaration.Members[index];
            if (member.EqualsValue is not { } equal)
                continue;

            var equalColumn = equal.EqualsToken.GetLineSpan().StartLinePosition.Character;
            if (equalColumn < maxEqualColumn)
                changes.Add(new TextChange(member.EqualsValue.EqualsToken.Span, new string(' ', maxEqualColumn - equalColumn) + member.EqualsValue.EqualsToken.Text));
        }

        if (changes.Count is 0)
            return document;

        return document.WithText(text.WithChanges(changes));
    }
}