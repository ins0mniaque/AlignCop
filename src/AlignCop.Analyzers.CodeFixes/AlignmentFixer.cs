using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using AlignCop.Analyzers.Internal;

namespace AlignCop.Analyzers;

internal static class AlignmentFixer
{
    public static async Task<Document> FixUnalignment<T>(Document document, IReadOnlyList<T> elements, T firstElement, T lastElement, Func<T, EqualsValueClauseSyntax?> getEqualsValueClause, CancellationToken cancellationToken) where T : SyntaxNode
    {
        var startIndex = -1;
        var endIndex   = -1;

        for (var index = 0; index < elements.Count; index++)
        {
            var member = elements[index];

            if (startIndex < 0)
            {
                if (member == firstElement)
                    startIndex = index;
                else
                    continue;
            }

            if (member == lastElement)
            {
                endIndex = index + 1;
                break;
            }
        }

        if (endIndex < 0)
            return document;

        var equalColumns   = new int[endIndex - startIndex];
        var maxEqualColumn = -1;

        for (var index = startIndex; index < endIndex; index++)
        {
            if (getEqualsValueClause(elements[index]) is not { } equal)
            {
                equalColumns[index] = -1;
                continue;
            }

            var equalColumn = equalColumns[index - startIndex] = equal.EqualsToken.GetLineSpan().StartLinePosition.Character;
            if (equalColumn > maxEqualColumn)
                maxEqualColumn = equalColumn;
        }

        if (maxEqualColumn < 0)
            return document;

        var text    = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<TextChange>(endIndex - startIndex);

        for (var index = startIndex; index < endIndex; index++)
        {
            var member = elements[index];
            if (getEqualsValueClause(member) is not { } equal)
                continue;

            var equalColumn = equalColumns[index - startIndex];
            if (equalColumn < maxEqualColumn)
                changes.Add(new TextChange(new TextSpan(equal.EqualsToken.Span.Start, 0), new string(' ', maxEqualColumn - equalColumn)));
        }

        if (changes.Count is 0)
            return document;

        return document.WithText(text.WithChanges(changes));
    }
}