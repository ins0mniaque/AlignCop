using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using AlignCop.Analyzers.Internal;

namespace AlignCop.Analyzers;

internal static class AlignmentFixer
{
    public static async Task<Document> FixUnalignment<T>(Document document, IReadOnlyList<T> elements, T firstElement, T lastElement, Func<T, SyntaxNode?> getNodeToAlign, CancellationToken cancellationToken) where T : SyntaxNode
    {
        var startIndex = -1;
        var length     = -1;

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
                length = index + 1 - startIndex;
                break;
            }
        }

        if (length < 0)
            return document;

        var columns   = new int[length];
        var maxColumn = -1;

        for (var index = 0; index < length; index++)
        {
            if (getNodeToAlign(elements[startIndex + index]) is not { } nodeToAlign)
            {
                columns[index] = -1;
                continue;
            }

            var column = columns[index] = nodeToAlign.GetLineSpan().StartLinePosition.Character;
            if (column > maxColumn)
                maxColumn = column;
        }

        if (maxColumn < 0)
            return document;

        var text    = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<TextChange>(length);

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];
            if (getNodeToAlign(element) is not { } nodeToAlign)
                continue;

            var column = columns[index];
            if (column < maxColumn)
                changes.Add(new TextChange(new TextSpan(nodeToAlign.Span.Start, 0), new string(' ', maxColumn - column)));
        }

        if (changes.Count is 0)
            return document;

        return document.WithText(text.WithChanges(changes));
    }
}