using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AlignCop.Analyzers;

internal static class AlignmentFixer
{
    public static async Task<Document> FixUnalignment<T>(Document document, IReadOnlyList<T> nodes, T firstNode, T lastNode, Selector<T, SyntaxNode?> getNodeToAlign, CancellationToken cancellationToken) where T : SyntaxNode
    {
        IndexOfStartAndLength(nodes, firstNode, lastNode, out var startIndex, out var length);

        if (length < 0)
            return document;

        var columns   = new int[length];
        var maxColumn = -1;

        for (var index = 0; index < length; index++)
        {
            getNodeToAlign(nodes[startIndex + index], out var nodeToAlign);

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

        var changes = new List<TextChange>(length);

        for (var index = 0; index < length; index++)
        {
            getNodeToAlign(nodes[startIndex + index], out var nodeToAlign);

            if (nodeToAlign is null)
                continue;

            var column = columns[index];
            if (column < maxColumn)
                changes.Add(new TextChange(new TextSpan(nodeToAlign.Span.Start, 0), new string(' ', maxColumn - column)));
        }

        if (changes.Count is 0)
            return document;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        return document.WithText(text.WithChanges(changes));
    }

    public static async Task<Document> FixUnalignment<T>(Document document, IReadOnlyList<T> nodes, T firstNode, T lastNode, Selector<T, SyntaxNode?, SyntaxNode?> getNodesToAlign, CancellationToken cancellationToken) where T : SyntaxNode
    {
        IndexOfStartAndLength(nodes, firstNode, lastNode, out var startIndex, out var length);

        if (length < 0)
            return document;

        var columnsA   = new int[length];
        var columnsB   = new int[length];
        var maxColumnA = -1;
        var maxColumnB = -1;

        for (var index = 0; index < length; index++)
        {
            getNodesToAlign(nodes[startIndex + index], out var nodeToAlignA, out var nodeToAlignB);

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
            getNodesToAlign(nodes[startIndex + index], out var nodeToAlignA, out var nodeToAlignB);

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

    private static void IndexOfStartAndLength<T>(IReadOnlyList<T> nodes, T firstNode, T lastNode, out int startIndex, out int length) where T : SyntaxNode
    {
        startIndex = -1;
        length     = -1;

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];

            if (startIndex < 0)
            {
                if (node == firstNode)
                    startIndex = index;
                else
                    continue;
            }

            if (node == lastNode)
            {
                length = index + 1 - startIndex;
                break;
            }
        }
    }
}