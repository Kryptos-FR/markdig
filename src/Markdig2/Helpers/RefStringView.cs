// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace Markdig2.Helpers;

/// <summary>
/// A ref struct that provides a view into a Span of characters with specific start and end indices.
/// This is a stack-only type that does not allocate on the heap.
/// </summary>
public readonly ref struct RefStringView
{
    private readonly ReadOnlySpan<char> _source;
    private readonly int _start;
    private readonly int _end;

    /// <summary>
    /// Initializes a new instance of <see cref="RefStringView"/>.
    /// </summary>
    /// <param name="source">The source span of characters.</param>
    /// <param name="start">The start index (inclusive).</param>
    /// <param name="end">The end index (exclusive).</param>
    public RefStringView(ReadOnlySpan<char> source, int start, int end)
    {
        Debug.Assert(start >= 0 && start <= source.Length, "Start index out of range");
        Debug.Assert(end >= start && end <= source.Length, "End index out of range");

        _source = source;
        _start = start;
        _end = end;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RefStringView"/> that covers the entire source span.
    /// </summary>
    /// <param name="source">The source span of characters.</param>
    public RefStringView(ReadOnlySpan<char> source) : this(source, 0, source.Length)
    {
    }

    /// <summary>
    /// Gets the start index of this view.
    /// </summary>
    public readonly int Start => _start;

    /// <summary>
    /// Gets the end index (exclusive) of this view.
    /// </summary>
    public readonly int End => _end;

    /// <summary>
    /// Gets the length of this view.
    /// </summary>
    public readonly int Length => _end - _start;

    /// <summary>
    /// Gets a value indicating whether this view is empty.
    /// </summary>
    public readonly bool IsEmpty => _start >= _end;

    /// <summary>
    /// Gets the character at the specified index within this view.
    /// </summary>
    /// <param name="index">The index within this view (0-based).</param>
    /// <returns>The character at the specified index.</returns>
    public readonly char this[int index]
    {
        get
        {
            Debug.Assert(index >= 0 && index < Length, "Index out of range");
            return _source[_start + index];
        }
    }

    /// <summary>
    /// Gets a character at the specified position, or a default value if out of bounds.
    /// </summary>
    /// <param name="index">The index within this view (0-based).</param>
    /// <param name="defaultChar">The character to return if index is out of bounds.</param>
    /// <returns>The character at the specified index, or <paramref name="defaultChar"/> if out of bounds.</returns>
    public readonly char GetCharOrDefault(int index, char defaultChar = '\0')
    {
        if (index >= 0 && index < Length)
        {
            return _source[_start + index];
        }
        return defaultChar;
    }

    /// <summary>
    /// Creates a subview of this view.
    /// </summary>
    /// <param name="start">The start index within this view.</param>
    /// <param name="end">The end index (exclusive) within this view.</param>
    /// <returns>A new <see cref="RefStringView"/> representing the subview.</returns>
    public readonly RefStringView Slice(int start, int end)
    {
        Debug.Assert(start >= 0 && start <= Length, "Start index out of range");
        Debug.Assert(end >= start && end <= Length, "End index out of range");

        return new RefStringView(_source, _start + start, _start + end);
    }

    /// <summary>
    /// Creates a subview of this view starting at the specified index.
    /// </summary>
    /// <param name="start">The start index within this view.</param>
    /// <returns>A new <see cref="RefStringView"/> representing the subview.</returns>
    public readonly RefStringView Slice(int start)
    {
        return Slice(start, Length);
    }

    /// <summary>
    /// Gets the underlying readonly span for this view.
    /// </summary>
    /// <returns>A readonly span covering the characters in this view.</returns>
    public readonly ReadOnlySpan<char> AsReadOnlySpan()
    {
        return _source.Slice(_start, Length);
    }

    /// <summary>
    /// Returns a string representation of this view (allocates on heap).
    /// </summary>
    /// <returns>A string containing the characters in this view.</returns>
    public override readonly string ToString()
    {
        return new string(_source.Slice(_start, Length));
    }

    /// <summary>
    /// Determines whether the content of this view equals the specified string.
    /// </summary>
    /// <param name="other">The string to compare with.</param>
    /// <returns><see langword="true"/> if the views are equal; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(string? other)
    {
        if (other == null)
        {
            return IsEmpty;
        }

        if (Length != other.Length)
        {
            return false;
        }

        for (int i = 0; i < Length; i++)
        {
            if (_source[_start + i] != other[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the content of this view equals another view.
    /// </summary>
    /// <param name="other">The view to compare with.</param>
    /// <returns><see langword="true"/> if the views are equal; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(RefStringView other)
    {
        if (Length != other.Length)
        {
            return false;
        }

        for (int i = 0; i < Length; i++)
        {
            if (_source[_start + i] != other._source[other._start + i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether this view is equal to the specified object.
    /// </summary>
    public override readonly bool Equals(object? obj)
    {
        // Ref structs cannot be boxed, so this will always be false
        return false;
    }

    /// <summary>
    /// Returns the hash code for this view.
    /// </summary>
    public override readonly int GetHashCode()
    {
        var hc = new HashCode();
        var span = AsReadOnlySpan();
        foreach (char c in span)
        {
            hc.Add(c);
        }
        return hc.ToHashCode();
    }

    /// <summary>
    /// Finds the index of a character in this view.
    /// </summary>
    /// <param name="c">The character to find.</param>
    /// <returns>The index of the character in this view, or -1 if not found.</returns>
    public readonly int IndexOf(char c)
    {
        var span = AsReadOnlySpan();
        int index = span.IndexOf(c);
        return index >= 0 ? index : -1;
    }

    /// <summary>
    /// Finds the index of a substring in this view.
    /// </summary>
    /// <param name="value">The substring to find.</param>
    /// <returns>The index of the substring in this view, or -1 if not found.</returns>
    public readonly int IndexOf(ReadOnlySpan<char> value)
    {
        var span = AsReadOnlySpan();
        int index = span.IndexOf(value);
        return index >= 0 ? index : -1;
    }

    /// <summary>
    /// Trims leading whitespace from this view.
    /// </summary>
    /// <returns>A new <see cref="RefStringView"/> with leading whitespace removed.</returns>
    public readonly RefStringView TrimStart()
    {
        int start = _start;
        while (start < _end && char.IsWhiteSpace(_source[start]))
        {
            start++;
        }
        return new RefStringView(_source, start, _end);
    }

    /// <summary>
    /// Trims trailing whitespace from this view.
    /// </summary>
    /// <returns>A new <see cref="RefStringView"/> with trailing whitespace removed.</returns>
    public readonly RefStringView TrimEnd()
    {
        int end = _end;
        while (end > _start && char.IsWhiteSpace(_source[end - 1]))
        {
            end--;
        }
        return new RefStringView(_source, _start, end);
    }

    /// <summary>
    /// Trims both leading and trailing whitespace from this view.
    /// </summary>
    /// <returns>A new <see cref="RefStringView"/> with whitespace removed.</returns>
    public readonly RefStringView Trim()
    {
        return TrimStart().TrimEnd();
    }
}
