using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AlignCop.Analyzers;

internal static class LocationExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Location CombineWith(this Location location, Location otherLocation)
    {
        return Location.Create(location.SourceTree ?? throw new ArgumentException("Location must be in a syntax tree", nameof(location)),
                               TextSpan.FromBounds(location     .SourceSpan.Start,
                                                   otherLocation.SourceSpan.End));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Location GetLastLocation(this Diagnostic diagnostic)
    {
        if (diagnostic.AdditionalLocations.Count is 0)
            return diagnostic.Location;

        return diagnostic.AdditionalLocations[diagnostic.AdditionalLocations.Count - 1];
    }

    /// <summary>
    /// Gets the location in terms of path, line and column for a given node.
    /// </summary>
    /// <param name="node">The node to use.</param>
    /// <returns>The location in terms of path, line and column for a given node.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FileLinePositionSpan GetLineSpan(this SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOnNextLineAlignedTo(this FileLinePositionSpan lineSpan, FileLinePositionSpan previousLineSpan)
    {
        return lineSpan.StartLinePosition.Character == previousLineSpan.StartLinePosition.Character &&
               lineSpan.StartLinePosition.Line      == previousLineSpan.StartLinePosition.Line + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SpansMultipleLines(this FileLinePositionSpan lineSpan)
    {
        return lineSpan.StartLinePosition.Line != lineSpan.EndLinePosition.Line;
    }
}