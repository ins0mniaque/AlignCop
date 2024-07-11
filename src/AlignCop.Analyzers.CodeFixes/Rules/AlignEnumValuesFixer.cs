using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AlignCop.Analyzers.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlignEnumValuesFixer)), Shared]
public class AlignEnumValuesFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AlignEnumValues);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root       = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();

        var firstSpan = diagnostic.Location.SourceSpan;
        var lastSpan  = diagnostic.AdditionalLocations.LastOrDefault()?.SourceSpan ?? firstSpan;

        var firstEnumMember = root.FindToken(firstSpan.Start).Parent.Parent as EnumMemberDeclarationSyntax;
        var lastEnumMember  = root.FindToken(lastSpan.Start).Parent.Parent as EnumMemberDeclarationSyntax;

        if (firstEnumMember is null || lastEnumMember is null)
            return;

        var enumDeclaration = firstEnumMember.Parent as EnumDeclarationSyntax;
        if (enumDeclaration is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.AlignEnumValuesCodeFixTitle,
                createChangedDocument: cancellationToken => AlignmentFixer.FixUnalignment(context.Document, enumDeclaration.Members, firstEnumMember, lastEnumMember, GetEqualsValueClauseFunc, cancellationToken),
                equivalenceKey: nameof(CodeFixResources.AlignEnumValuesCodeFixTitle)),
            diagnostic);
    }

    private static readonly Func<EnumMemberDeclarationSyntax, EqualsValueClauseSyntax?> GetEqualsValueClauseFunc = GetEqualsValueClause;

    private static EqualsValueClauseSyntax? GetEqualsValueClause(EnumMemberDeclarationSyntax enumDeclaration)
    {
        return enumDeclaration.EqualsValue;
    }
}