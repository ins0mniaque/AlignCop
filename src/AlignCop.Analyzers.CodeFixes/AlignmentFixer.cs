using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AlignCop.Analyzers;

internal static class AlignmentFixer
{
    public static async Task<Document> FixUnalignment<T>(Document document, IReadOnlyList<T> elements, T firstElement, T lastElement, Selector<T, SyntaxNode?> getNodeToAlign, CancellationToken cancellationToken) where T : SyntaxNode
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

            getNodeToAlign(element, out var nodeToAlign);

            if (nodeToAlign is null)
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

            getNodeToAlign(element, out var nodeToAlign);

            if (nodeToAlign is null)
                continue;

            var column = columns[index];
            if (column < maxColumn)
                changes.Add(new TextChange(new TextSpan(nodeToAlign.Span.Start, 0), new string(' ', maxColumn - column)));
        }

        if (changes.Count is 0)
            return document;

        return document.WithText(text.WithChanges(changes));
    }

    public static async Task<Document> FixUnalignment<T>(Document document, IReadOnlyList<T> elements, T firstElement, T lastElement, Selector<T, SyntaxNode?, SyntaxNode?> getNodesToAlign, CancellationToken cancellationToken) where T : SyntaxNode
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

        var columnsA   = new int[length];
        var columnsB   = new int[length];
        var maxColumnA = -1;
        var maxColumnB = -1;

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];

            getNodesToAlign(element, out var nodeToAlignA, out var nodeToAlignB);

            if (nodeToAlignA is null)
            {
                columnsA[index] = -1;
                columnsB[index] = -1;
                continue;
            }

            var columnA = columnsA[index] = nodeToAlignA.GetLineSpan().StartLinePosition.Character;

            if (columnA > maxColumnA)
                maxColumnA = columnA;

            if (nodeToAlignB is null)
            {
                columnsB[index] = -1;
                continue;
            }

            var columnB = columnsB[index] = nodeToAlignB.GetLineSpan().StartLinePosition.Character;

            if (columnB > maxColumnB)
                maxColumnB = columnB;
        }

        if (maxColumnA < 0)
            return document;

        var changes = new List<TextChange>(length);

        for (var index = 0; index < length; index++)
        {
            var element = elements[startIndex + index];

            getNodesToAlign(element, out var nodeToAlignA, out var nodeToAlignB);

            if (nodeToAlignA is null)
                continue;

            var changeA = 0;
            var columnA = columnsA[index];
            if (columnA < maxColumnA)
                changes.Add(new TextChange(new TextSpan(nodeToAlignA.Span.Start, 0), new string(' ', changeA = maxColumnA - columnA)));

            if (nodeToAlignB is null)
                continue;

            var columnB = columnsB[index];
            if (columnB >= 0 && columnB + changeA < maxColumnB)
                changes.Add(new TextChange(new TextSpan(nodeToAlignB.Span.Start, 0), new string(' ', maxColumnB - columnB - changeA)));
        }

        if (changes.Count is 0)
            return document;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        return document.WithText(text.WithChanges(changes));
    }
}