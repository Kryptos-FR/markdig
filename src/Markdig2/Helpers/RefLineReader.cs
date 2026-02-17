// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace Markdig2.Helpers;

/// <summary>
/// A ref struct for reading lines from a Span of characters.
/// This enables stack-based, zero-copy line-by-line parsing.
/// </summary>
public ref struct RefLineReader
{
    private readonly ReadOnlySpan<char> _source;
    private int _position;
    private int _lineNumber;

    /// <summary>
    /// Initializes a new instance of <see cref="RefLineReader"/>.
    /// </summary>
    /// <param name="source">The source span of characters to read lines from.</param>
    public RefLineReader(ReadOnlySpan<char> source)
    {
        _source = source;
        _position = 0;
        _lineNumber = 0;
    }

    /// <summary>
    /// Gets the current position in the source span.
    /// </summary>
    public readonly int Position => _position;

    /// <summary>
    /// Gets the current line number (0-based).
    /// </summary>
    public readonly int LineNumber => _lineNumber;

    /// <summary>
    /// Gets the total length of the source.
    /// </summary>
    public readonly int Length => _source.Length;

    /// <summary>
    /// Gets a value indicating whether there are more characters to read.
    /// </summary>
    public readonly bool HasMore => _position < _source.Length;

    /// <summary>
    /// Reads the next line from the source.
    /// Returns a view into the source covering the line content.
    /// The returned line does NOT include the line terminator.
    /// </summary>
    /// <returns>A <see cref="RefStringView"/> representing the next line, or an empty view if at EOF.</returns>
    public RefStringView ReadLine()
    {
        if (_position >= _source.Length)
        {
            return new RefStringView(_source, _position, _position);
        }

        int lineStart = _position;
        int lineEnd = _position;

        // Find the end of the line
        while (lineEnd < _source.Length)
        {
            char c = _source[lineEnd];

            if (c == '\n')
            {
                // Move past the \n
                _position = lineEnd + 1;
                _lineNumber++;

                // Check if there's a \r before the \n and handle \r\n
                if (lineEnd > lineStart && _source[lineEnd - 1] == '\r')
                {
                    return new RefStringView(_source, lineStart, lineEnd - 1);
                }

                return new RefStringView(_source, lineStart, lineEnd);
            }
            else if (c == '\r')
            {
                // Check if followed by \n (CRLF) or standalone \r (CR)
                if (lineEnd + 1 < _source.Length && _source[lineEnd + 1] == '\n')
                {
                    _position = lineEnd + 2; // Skip both \r and \n
                }
                else
                {
                    _position = lineEnd + 1; // Skip just \r
                }

                _lineNumber++;
                return new RefStringView(_source, lineStart, lineEnd);
            }

            lineEnd++;
        }

        // End of file without line terminator
        _position = _source.Length;
        _lineNumber++;
        return new RefStringView(_source, lineStart, lineEnd);
    }

    /// <summary>
    /// Peeks at the next character without consuming it.
    /// </summary>
    /// <returns>The next character, or '\0' if at EOF.</returns>
    public readonly char Peek()
    {
        return _position < _source.Length ? _source[_position] : '\0';
    }

    /// <summary>
    /// Peeks at a character at a specific offset from the current position.
    /// </summary>
    /// <param name="offset">The offset from the current position.</param>
    /// <returns>The character at the specified offset, or '\0' if out of bounds.</returns>
    public readonly char Peek(int offset)
    {
        int index = _position + offset;
        return index >= 0 && index < _source.Length ? _source[index] : '\0';
    }

    /// <summary>
    /// Reads a single character and advances the position.
    /// </summary>
    /// <returns>The next character, or '\0' if at EOF.</returns>
    public char ReadChar()
    {
        if (_position >= _source.Length)
        {
            return '\0';
        }

        char c = _source[_position];
        _position++;

        if (c == '\n')
        {
            _lineNumber++;
        }

        return c;
    }

    /// <summary>
    /// Resets the reader to the beginning.
    /// </summary>
    public void Reset()
    {
        _position = 0;
        _lineNumber = 0;
    }

    /// <summary>
    /// Reads all remaining content as a single view.
    /// </summary>
    /// <returns>A <see cref="RefStringView"/> containing all remaining content.</returns>
    public RefStringView ReadAll()
    {
        var view = new RefStringView(_source, _position, _source.Length);
        _position = _source.Length;
        return view;
    }

    /// <summary>
    /// Reads a fixed number of characters without crossing line boundaries.
    /// </summary>
    /// <param name="count">The maximum number of characters to read.</param>
    /// <returns>A <see cref="RefStringView"/> containing up to <paramref name="count"/> characters.</returns>
    public RefStringView ReadChars(int count)
    {
        int end = _position + count;
        if (end > _source.Length)
        {
            end = _source.Length;
        }

        var view = new RefStringView(_source, _position, end);
        _position = end;

        // Count newlines to update line number
        for (int i = _position - (end - _position); i < end; i++)
        {
            if (_source[i] == '\n')
            {
                _lineNumber++;
            }
        }

        return view;
    }

    /// <summary>
    /// Advances the position by the specified number of characters.
    /// </summary>
    /// <param name="count">The number of characters to skip.</param>
    public void Skip(int count)
    {
        int newPos = _position + count;
        if (newPos > _source.Length)
        {
            newPos = _source.Length;
        }

        // Count newlines as we skip
        for (int i = _position; i < newPos; i++)
        {
            if (_source[i] == '\n')
            {
                _lineNumber++;
            }
        }

        _position = newPos;
    }

    /// <summary>
    /// Skips all whitespace characters.
    /// </summary>
    public void SkipWhitespace()
    {
        while (_position < _source.Length && char.IsWhiteSpace(_source[_position]))
        {
            if (_source[_position] == '\n')
            {
                _lineNumber++;
            }
            _position++;
        }
    }

    /// <summary>
    /// Finds the next occurrence of a character.
    /// </summary>
    /// <param name="c">The character to find.</param>
    /// <returns>The distance from the current position to the character, or -1 if not found.</returns>
    public readonly int FindNext(char c)
    {
        for (int i = _position; i < _source.Length; i++)
        {
            if (_source[i] == c)
            {
                return i - _position;
            }
        }
        return -1;
    }
}
