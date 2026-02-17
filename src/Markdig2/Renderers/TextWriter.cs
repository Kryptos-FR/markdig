// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Text;

namespace Markdig2.Renderers;

/// <summary>
/// A struct wrapper around StringBuilder for buffered text output.
/// Used by renderers to write markdown output efficiently.
/// </summary>
public struct TextWriter
{
    private readonly StringBuilder _builder;
    private int _column;
    private bool _previousWasNewLine;

    /// <summary>
    /// Initializes a new instance of <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="builder">The StringBuilder to write to.</param>
    public TextWriter(StringBuilder builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _column = 0;
        _previousWasNewLine = true;
    }

    /// <summary>
    /// Gets the current column position (0-based).
    /// </summary>
    public readonly int Column => _column;

    /// <summary>
    /// Gets whether the previous write was a newline.
    /// </summary>
    public readonly bool PreviousWasNewLine => _previousWasNewLine;

    /// <summary>
    /// Gets the length of the output written so far.
    /// </summary>
    public readonly int Length => _builder.Length;

    /// <summary>
    /// Writes a single character.
    /// </summary>
    public void Write(char c)
    {
        _builder.Append(c);
        if (c == '\n')
        {
            _column = 0;
            _previousWasNewLine = true;
        }
        else
        {
            _column++;
            _previousWasNewLine = false;
        }
    }

    /// <summary>
    /// Writes a string.
    /// </summary>
    public void Write(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        _builder.Append(text);
        UpdatePosition(text);
    }

    /// <summary>
    /// Writes a span of characters.
    /// </summary>
    public void Write(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
            return;

        _builder.Append(text);
        UpdatePosition(text);
    }

    /// <summary>
    /// Writes a newline character.
    /// </summary>
    public void WriteLine()
    {
        Write('\n');
    }

    /// <summary>
    /// Writes a string followed by a newline.
    /// </summary>
    public void WriteLine(string? text)
    {
        Write(text);
        WriteLine();
    }

    /// <summary>
    /// Writes a span of characters followed by a newline.
    /// </summary>
    public void WriteLine(ReadOnlySpan<char> text)
    {
        Write(text);
        WriteLine();
    }

    /// <summary>
    /// Gets the final output as a string.
    /// </summary>
    public readonly override string ToString() => _builder.ToString();

    /// <summary>
    /// Clears all output.
    /// </summary>
    public void Clear()
    {
        _builder.Clear();
        _column = 0;
        _previousWasNewLine = true;
    }

    private void UpdatePosition(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                _column = 0;
                _previousWasNewLine = true;
            }
            else
            {
                _column++;
                _previousWasNewLine = false;
            }
        }
    }
}
