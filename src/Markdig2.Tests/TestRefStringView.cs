// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;

namespace Markdig2.Tests;

public class RefStringViewTests
{
    [Fact]
    public void Constructor_WithFullSpan_CreatesViewCoveringEntireSpan()
    {
        Span<char> source = "Hello World".ToCharArray();
        var view = new RefStringView(source);

        Assert.Equal(0, view.Start);
        Assert.Equal(11, view.End);
        Assert.Equal(11, view.Length);
        Assert.False(view.IsEmpty);
    }

    [Fact]
    public void Constructor_WithStartAndEnd_CreatesCorrectView()
    {
        Span<char> source = "Hello World".ToCharArray();
        var view = new RefStringView(source, 6, 11);

        Assert.Equal(6, view.Start);
        Assert.Equal(11, view.End);
        Assert.Equal(5, view.Length);
        Assert.Equal("World", view.ToString());
    }

    [Fact]
    public void Indexer_ReturnsCorrectCharacter()
    {
        Span<char> source = "Hello".ToCharArray();
        var view = new RefStringView(source);

        Assert.Equal('H', view[0]);
        Assert.Equal('e', view[1]);
        Assert.Equal('o', view[4]);
    }

    [Fact]
    public void GetCharOrDefault_ReturnsCharacterWhenInBounds()
    {
        Span<char> source = "Hello".ToCharArray();
        var view = new RefStringView(source);

        Assert.Equal('H', view.GetCharOrDefault(0));
        Assert.Equal('o', view.GetCharOrDefault(4));
    }

    [Fact]
    public void GetCharOrDefault_ReturnsDefaultWhenOutOfBounds()
    {
        Span<char> source = "Hello".ToCharArray();
        var view = new RefStringView(source);

        Assert.Equal('\0', view.GetCharOrDefault(-1));
        Assert.Equal('\0', view.GetCharOrDefault(5));
        Assert.Equal('X', view.GetCharOrDefault(10, 'X'));
    }

    [Fact]
    public void Slice_WithStartAndEnd_CreatesSubview()
    {
        Span<char> source = "Hello World".ToCharArray();
        var view = new RefStringView(source);
        var subview = view.Slice(6, 11);

        Assert.Equal(5, subview.Length);
        Assert.Equal("World", subview.ToString());
    }

    [Fact]
    public void Slice_WithStartOnly_CreatesSubviewToEnd()
    {
        Span<char> source = "Hello World".ToCharArray();
        var view = new RefStringView(source);
        var subview = view.Slice(6);

        Assert.Equal(5, subview.Length);
        Assert.Equal("World", subview.ToString());
    }

    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        Span<char> source = "Hello World".ToCharArray();
        var view = new RefStringView(source, 6, 11);
        var span = view.AsSpan();

        Assert.Equal(5, span.Length);
        Assert.Equal("World", new string(span));
    }

    [Fact]
    public void ToString_ReturnsCorrectString()
    {
        Span<char> source = "Hello World".ToCharArray();
        var view = new RefStringView(source, 0, 5);

        Assert.Equal("Hello", view.ToString());
    }

    [Fact]
    public void Equals_String_ReturnsTrueForEqualContent()
    {
        Span<char> source = "Hello".ToCharArray();
        var view = new RefStringView(source);

        Assert.True(view.Equals("Hello"));
        Assert.False(view.Equals("World"));
        Assert.False(view.Equals("Hello!"));
    }

    [Fact]
    public void Equals_String_HandleNullAndEmpty()
    {
        Span<char> source = "Hello".ToCharArray();
        var view = new RefStringView(source);
        var emptyView = new RefStringView(source, 0, 0);

        Assert.False(view.Equals((string?)null));
        Assert.True(emptyView.Equals((string?)null));
        Assert.False(view.Equals(""));
    }

    [Fact]
    public void Equals_RefStringView_ReturnsTrueForEqualContent()
    {
        Span<char> source1 = "Hello".ToCharArray();
        Span<char> source2 = "Hello".ToCharArray();
        var view1 = new RefStringView(source1);
        var view2 = new RefStringView(source2);

        Assert.True(view1.Equals(view2));
    }

    [Fact]
    public void Equals_RefStringView_ReturnsFalseForDifferentContent()
    {
        Span<char> source1 = "Hello".ToCharArray();
        Span<char> source2 = "World".ToCharArray();
        var view1 = new RefStringView(source1);
        var view2 = new RefStringView(source2);

        Assert.False(view1.Equals(view2));
    }

    [Fact]
    public void IndexOf_Char_FindsCharacter()
    {
        Span<char> source = "Hello World".ToCharArray();
        var view = new RefStringView(source);

        Assert.Equal(0, view.IndexOf('H'));
        Assert.Equal(4, view.IndexOf('o'));
        Assert.Equal(6, view.IndexOf('W'));
        Assert.Equal(-1, view.IndexOf('X'));
    }

    [Fact]
    public void IndexOf_Substring_FindsSubstring()
    {
        Span<char> source = "Hello World".ToCharArray();
        var view = new RefStringView(source);

        Assert.Equal(0, view.IndexOf("Hello".AsSpan()));
        Assert.Equal(6, view.IndexOf("World".AsSpan()));
        Assert.Equal(-1, view.IndexOf("Goodbye".AsSpan()));
    }

    [Fact]
    public void TrimStart_RemovesLeadingWhitespace()
    {
        Span<char> source = "   Hello".ToCharArray();
        var view = new RefStringView(source);
        var trimmed = view.TrimStart();

        Assert.Equal("Hello", trimmed.ToString());
    }

    [Fact]
    public void TrimEnd_RemovesTrailingWhitespace()
    {
        Span<char> source = "Hello   ".ToCharArray();
        var view = new RefStringView(source);
        var trimmed = view.TrimEnd();

        Assert.Equal("Hello", trimmed.ToString());
    }

    [Fact]
    public void Trim_RemovesBothWhitespace()
    {
        Span<char> source = "   Hello   ".ToCharArray();
        var view = new RefStringView(source);
        var trimmed = view.Trim();

        Assert.Equal("Hello", trimmed.ToString());
    }

    [Fact]
    public void IsEmpty_ReturnsTrueForEmptyView()
    {
        Span<char> source = "Hello".ToCharArray();
        var emptyView = new RefStringView(source, 2, 2);

        Assert.True(emptyView.IsEmpty);
        Assert.Equal(0, emptyView.Length);
    }

    [Fact]
    public void GetHashCode_ProducesConsistentHash()
    {
        Span<char> source1 = "Hello".ToCharArray();
        Span<char> source2 = "Hello".ToCharArray();
        var view1 = new RefStringView(source1);
        var view2 = new RefStringView(source2);

        Assert.Equal(view1.GetHashCode(), view2.GetHashCode());
    }

    [Fact]
    public void ComplexSlicing_WorksCorrectly()
    {
        Span<char> source = "The quick brown fox".ToCharArray();
        var view = new RefStringView(source);
        
        // Get "quick brown"
        var subview1 = view.Slice(4, 15);
        Assert.Equal("quick brown", subview1.ToString());
        
        // Get "brown" from subview
        var subview2 = subview1.Slice(6, 11);
        Assert.Equal("brown", subview2.ToString());
    }
}
