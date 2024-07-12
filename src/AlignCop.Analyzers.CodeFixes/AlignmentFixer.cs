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
            var element = elements[index];

            if (startIndex < 0)
            {
                if (element == firstElement)
                    startIndex = index;
                else
                    continue;
            }

            if (element == lastElement)
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
            var element = elements[startIndex + index];

            if (getNodeToAlign(element) is not { } nodeToAlign)
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

    public static async Task<Document> FixUnalignment<T>(Document document, IReadOnlyList<T> elements, T firstElement, T lastElement, Func<T, SyntaxNode?> getLeftNodeToAlign, Func<T, SyntaxNode?> getRightNodeToAlign, CancellationToken cancellationToken) where T : SyntaxNode
    {
        var startIndex = -1;
        var length     = -1;

        for (var index = 0; index < elements.Count; index++)
        {
            var element = elements[index];

            if (startIndex < 0)
            {
                if (element == firstElement)
                    startIndex = index;
                else
                    continue;
            }

            if (element == lastElement)
            {
                length = index + 1 - startIndex;
                break;
            }
        }

        if (length < 0)
            return document;

        var leftColumns    = new int[length];
        var rightColumns   = new int[length];
        var maxLeftColumn  = -1;
        var maxRightColumn = -1;

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];

            if (getLeftNodeToAlign(element) is not { } leftNodeToAlign)
            {
                leftColumns[index]  = -1;
                rightColumns[index] = -1;
                continue;
            }

            var leftColumn = leftColumns[index] = leftNodeToAlign.GetLineSpan().StartLinePosition.Character;

            if (leftColumn > maxLeftColumn)
                maxLeftColumn = leftColumn;

            if (getRightNodeToAlign(element) is not { } rightNodeToAlign)
            {
                rightColumns[index] = -1;
                continue;
            }

            var rightColumn = rightColumns[index] = rightNodeToAlign.GetLineSpan().StartLinePosition.Character;

            if (rightColumn > maxRightColumn)
                maxRightColumn = rightColumn;
        }

        if (maxLeftColumn < 0)
            return document;

        var text    = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<TextChange>(length);

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];
            if (getLeftNodeToAlign(element) is not { } leftNodeToAlign)
                continue;

            var leftChange = 0;
            var leftColumn = leftColumns[index];
            if (leftColumn < maxLeftColumn)
                changes.Add(new TextChange(new TextSpan(leftNodeToAlign.Span.Start, 0), new string(' ', leftChange = maxLeftColumn - leftColumn)));

            if (getRightNodeToAlign(element) is not { } rightNodeToAlign)
                continue;

            var rightColumn = rightColumns[index];
            if (rightColumn >= 0 && rightColumn + leftChange < maxRightColumn)
                changes.Add(new TextChange(new TextSpan(rightNodeToAlign.Span.Start, 0), new string(' ', maxRightColumn - rightColumn - leftChange)));
        }

        if (changes.Count is 0)
            return document;

        return document.WithText(text.WithChanges(changes));
    }
}