// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;

namespace Markdig2.Tests;

public class RefLineReaderTests
{
    [Fact]
    public void ReadLine_UnixLineEndings_ReadsLinesCorrectly()
    {
        Span<char> source = "Line 1\nLine 2\nLine 3".ToCharArray();
        var reader = new RefLineReader(source);

        var line1 = reader.ReadLine();
        Assert.Equal("Line 1", line1.ToString());
        Assert.Equal(1, reader.LineNumber);

        var line2 = reader.ReadLine();
        Assert.Equal("Line 2", line2.ToString());
        Assert.Equal(2, reader.LineNumber);

        var line3 = reader.ReadLine();
        Assert.Equal("Line 3", line3.ToString());
        Assert.Equal(3, reader.LineNumber);

        Assert.False(reader.HasMore);
    }

    [Fact]
    public void ReadLine_WindowsLineEndings_ReadsLinesCorrectly()
    {
        Span<char> source = "Line 1\r\nLine 2\r\nLine 3".ToCharArray();
        var reader = new RefLineReader(source);

        var line1 = reader.ReadLine();
        Assert.Equal("Line 1", line1.ToString());

        var line2 = reader.ReadLine();
        Assert.Equal("Line 2", line2.ToString());

        var line3 = reader.ReadLine();
        Assert.Equal("Line 3", line3.ToString());

        Assert.False(reader.HasMore);
    }

    [Fact]
    public void ReadLine_MacLineEndings_ReadsLinesCorrectly()
    {
        Span<char> source = "Line 1\rLine 2\rLine 3".ToCharArray();
        var reader = new RefLineReader(source);

        var line1 = reader.ReadLine();
        Assert.Equal("Line 1", line1.ToString());

        var line2 = reader.ReadLine();
        Assert.Equal("Line 2", line2.ToString());

        var line3 = reader.ReadLine();
        Assert.Equal("Line 3", line3.ToString());

        Assert.False(reader.HasMore);
    }

    [Fact]
    public void ReadLine_MixedLineEndings_ReadsLinesCorrectly()
    {
        Span<char> source = "Line 1\nLine 2\r\nLine 3\rLine 4".ToCharArray();
        var reader = new RefLineReader(source);

        Assert.Equal("Line 1", reader.ReadLine().ToString());
        Assert.Equal("Line 2", reader.ReadLine().ToString());
        Assert.Equal("Line 3", reader.ReadLine().ToString());
        Assert.Equal("Line 4", reader.ReadLine().ToString());
    }

    [Fact]
    public void ReadLine_EmptyLines_HandledCorrectly()
    {
        Span<char> source = "Line 1\n\nLine 3".ToCharArray();
        var reader = new RefLineReader(source);

        var line1 = reader.ReadLine();
        Assert.Equal("Line 1", line1.ToString());

        var line2 = reader.ReadLine();
        Assert.True(line2.IsEmpty);
        Assert.Equal("", line2.ToString());

        var line3 = reader.ReadLine();
        Assert.Equal("Line 3", line3.ToString());
    }

    [Fact]
    public void ReadLine_NoTrailingNewline_ReadsLastLine()
    {
        Span<char> source = "Line 1\nLine 2".ToCharArray();
        var reader = new RefLineReader(source);

        reader.ReadLine();
        var line2 = reader.ReadLine();
        
        Assert.Equal("Line 2", line2.ToString());
        Assert.False(reader.HasMore);
    }

    [Fact]
    public void ReadLine_EmptySource_ReturnsEmptyView()
    {
        Span<char> source = Array.Empty<char>();
        var reader = new RefLineReader(source);

        var line = reader.ReadLine();
        Assert.True(line.IsEmpty);
        Assert.False(reader.HasMore);
    }

    [Fact]
    public void Peek_ReturnsNextCharWithoutConsuming()
    {
        Span<char> source = "Hello".ToCharArray();
        var reader = new RefLineReader(source);

        Assert.Equal('H', reader.Peek());
        Assert.Equal('H', reader.Peek()); // Still 'H'
        Assert.Equal(0, reader.Position);
    }

    [Fact]
    public void Peek_WithOffset_ReturnsCharacterAtOffset()
    {
        Span<char> source = "Hello".ToCharArray();
        var reader = new RefLineReader(source);

        Assert.Equal('H', reader.Peek(0));
        Assert.Equal('e', reader.Peek(1));
        Assert.Equal('l', reader.Peek(2));
        Assert.Equal('\0', reader.Peek(10)); // Out of bounds
    }

    [Fact]
    public void ReadChar_ConsumesCharacter()
    {
        Span<char> source = "Hello".ToCharArray();
        var reader = new RefLineReader(source);

        Assert.Equal('H', reader.ReadChar());
        Assert.Equal(1, reader.Position);
        Assert.Equal('e', reader.ReadChar());
        Assert.Equal(2, reader.Position);
    }

    [Fact]
    public void ReadChar_UpdatesLineNumber()
    {
        Span<char> source = "Line1\nLine2".ToCharArray();
        var reader = new RefLineReader(source);

        for (int i = 0; i < 5; i++) reader.ReadChar(); // Read "Line1"
        Assert.Equal(0, reader.LineNumber);

        reader.ReadChar(); // Read '\n'
        Assert.Equal(1, reader.LineNumber);
    }

    [Fact]
    public void Reset_ResetsReaderToBeginning()
    {
        Span<char> source = "Hello\nWorld".ToCharArray();
        var reader = new RefLineReader(source);

        reader.ReadLine();
        Assert.Equal(1, reader.LineNumber);

        reader.Reset();
        Assert.Equal(0, reader.Position);
        Assert.Equal(0, reader.LineNumber);
        Assert.Equal("Hello", reader.ReadLine().ToString());
    }

    [Fact]
    public void ReadAll_ReturnsRemainingContent()
    {
        Span<char> source = "Hello\nWorld".ToCharArray();
        var reader = new RefLineReader(source);

        reader.ReadLine(); // Read "Hello"
        
        var remaining = reader.ReadAll();
        Assert.Equal("World", remaining.ToString());
        Assert.False(reader.HasMore);
    }

    [Fact]
    public void ReadChars_ReadsFixedNumberOfCharacters()
    {
        Span<char> source = "Hello World".ToCharArray();
        var reader = new RefLineReader(source);

        var chars = reader.ReadChars(5);
        Assert.Equal("Hello", chars.ToString());
        Assert.Equal(5, reader.Position);
    }

    [Fact]
    public void ReadChars_DoesNotExceedLength()
    {
        Span<char> source = "Hello".ToCharArray();
        var reader = new RefLineReader(source);

        var chars = reader.ReadChars(100);
        Assert.Equal("Hello", chars.ToString());
        Assert.Equal(5, reader.Position);
    }

    [Fact]
    public void Skip_AdvancesPosition()
    {
        Span<char> source = "Hello World".ToCharArray();
        var reader = new RefLineReader(source);

        reader.Skip(6);
        Assert.Equal(6, reader.Position);
        Assert.Equal('W', reader.Peek());
    }

    [Fact]
    public void Skip_UpdatesLineNumber()
    {
        Span<char> source = "Line1\nLine2\nLine3".ToCharArray();
        var reader = new RefLineReader(source);

        reader.Skip(12); // Skip to after second newline
        Assert.Equal(2, reader.LineNumber);
    }

    [Fact]
    public void SkipWhitespace_SkipsSpacesAndTabs()
    {
        Span<char> source = "   \t  Hello".ToCharArray();
        var reader = new RefLineReader(source);

        reader.SkipWhitespace();
        Assert.Equal('H', reader.Peek());
    }

    [Fact]
    public void SkipWhitespace_SkipsNewlines()
    {
        Span<char> source = "  \n  \n  Hello".ToCharArray();
        var reader = new RefLineReader(source);

        reader.SkipWhitespace();
        Assert.Equal('H', reader.Peek());
        Assert.Equal(2, reader.LineNumber);
    }

    [Fact]
    public void FindNext_FindsCharacter()
    {
        Span<char> source = "Hello World".ToCharArray();
        var reader = new RefLineReader(source);

        Assert.Equal(0, reader.FindNext('H'));
        Assert.Equal(4, reader.FindNext('o'));
        Assert.Equal(6, reader.FindNext('W'));
        Assert.Equal(-1, reader.FindNext('X'));
    }

    [Fact]
    public void HasMore_ReflectsRemainingContent()
    {
        Span<char> source = "AB".ToCharArray();
        var reader = new RefLineReader(source);

        Assert.True(reader.HasMore);
        reader.ReadChar();
        Assert.True(reader.HasMore);
        reader.ReadChar();
        Assert.False(reader.HasMore);
    }

    [Fact]
    public void Length_ReturnsSourceLength()
    {
        Span<char> source = "Hello World".ToCharArray();
        var reader = new RefLineReader(source);

        Assert.Equal(11, reader.Length);
    }

    [Fact]
    public void ComplexScenario_ReadingMarkdownLikeContent()
    {
        Span<char> source = "# Heading\n\nParagraph 1\nParagraph 2\n\n- Item 1\n- Item 2".ToCharArray();
        var reader = new RefLineReader(source);

        Assert.Equal("# Heading", reader.ReadLine().ToString());
        Assert.Equal("", reader.ReadLine().ToString());
        Assert.Equal("Paragraph 1", reader.ReadLine().ToString());
        Assert.Equal("Paragraph 2", reader.ReadLine().ToString());
        Assert.Equal("", reader.ReadLine().ToString());
        Assert.Equal("- Item 1", reader.ReadLine().ToString());
        Assert.Equal("- Item 2", reader.ReadLine().ToString());
        Assert.False(reader.HasMore);
    }
}
