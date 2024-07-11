using Microsoft.CodeAnalysis;

namespace AlignCop.Analyzers.Internal;

/// <summary>
/// Provides helper methods for working with source file locations.
/// </summary>
internal static class LocationExtensions
{
    /// <summary>
    /// Gets the location in terms of path, line and column for a given token.
    /// </summary>
    /// <param name="token">The token to use.</param>
    /// <returns>The location in terms of path, line and column for a given token.</returns>
    internal static FileLinePositionSpan GetLineSpan(this SyntaxToken token)
    {
        if(token.SyntaxTree is null)
            throw new ArgumentException("Token is not part of a syntax tree", nameof(token));

        return token.SyntaxTree.GetLineSpan(token.Span);
    }

    /// <summary>
    /// Gets the location in terms of path, line and column for a given node.
    /// </summary>
    /// <param name="node">The node to use.</param>
    /// <returns>The location in terms of path, line and column for a given node.</returns>
    internal static FileLinePositionSpan GetLineSpan(this SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span);
    }

    /// <summary>
    /// Gets the location in terms of path, line and column for a given trivia.
    /// </summary>
    /// <param name="trivia">The trivia to use.</param>
    /// <returns>The location in terms of path, line and column for a given trivia.</returns>
    internal static FileLinePositionSpan GetLineSpan(this SyntaxTrivia trivia)
    {
        if(trivia.SyntaxTree is null)
            throw new ArgumentException("Trivia is not part of a syntax tree", nameof(trivia));

        return trivia.SyntaxTree.GetLineSpan(trivia.Span);
    }

    /// <summary>
    /// Gets the location in terms of path, line and column for a given node or token.
    /// </summary>
    /// <param name="nodeOrToken">The trivia to use.</param>
    /// <returns>The location in terms of path, line and column for a given node or token.</returns>
    internal static FileLinePositionSpan GetLineSpan(this SyntaxNodeOrToken nodeOrToken)
    {
        if(nodeOrToken.SyntaxTree is null)
            throw new ArgumentException("Node or token is not part of a syntax tree", nameof(nodeOrToken));

        return nodeOrToken.SyntaxTree.GetLineSpan(nodeOrToken.Span);
    }
}