using Markdig2.Helpers;

namespace Markdig2.Tests;

public class TestRefCollection
{
    [Fact]
    public void Constructor_WithSpan_InitializesCorrectly()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        Assert.Equal(10, collection.Capacity);
        Assert.Equal(0, collection.Length);
    }

    [Fact]
    public void Constructor_WithCapacity_InitializesCorrectly()
    {
        var collection = new RefCollection<int>(10);

        Assert.True(collection.Capacity >= 10);
        Assert.Equal(0, collection.Length);
    }

    [Fact]
    public void Add_SingleItem_IncreasesLength()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        collection.Add(42);

        Assert.Equal(1, collection.Length);
        Assert.Equal(42, collection.AsReadOnlySpan()[0]);
    }

    [Fact]
    public void Add_MultipleItems_IncreasesLength()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        collection.Add(1);
        collection.Add(2);
        collection.Add(3);

        Assert.Equal(3, collection.Length);
        var span = collection.AsReadOnlySpan();
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
    }

    [Fact]
    public void Add_BeyondCapacity_GrowsBuffer()
    {
        Span<int> buffer = stackalloc int[2];
        var collection = new RefCollection<int>(buffer);

        collection.Add(1);
        collection.Add(2);
        collection.Add(3); // Should trigger growth

        Assert.Equal(3, collection.Length);
        Assert.True(collection.Capacity >= 3);
        var span = collection.AsReadOnlySpan();
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
    }

    [Fact]
    public void Add_ManyItems_GrowsMultipleTimes()
    {
        var collection = new RefCollection<int>(2);

        for (int i = 0; i < 100; i++)
        {
            collection.Add(i);
        }

        Assert.Equal(100, collection.Length);
        var span = collection.AsReadOnlySpan();
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i, span[i]);
        }
    }

    [Fact]
    public void Length_SetValid_UpdatesLength()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        collection.Add(1);
        collection.Add(2);
        collection.Add(3);

        collection.Length = 2;

        Assert.Equal(2, collection.Length);
        Assert.Equal(2, collection.AsReadOnlySpan().Length);
    }

    [Fact]
    public void Length_SetToZero_ClearsContent()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        collection.Add(1);
        collection.Add(2);

        collection.Length = 0;

        Assert.Equal(0, collection.Length);
        Assert.Equal(0, collection.AsReadOnlySpan().Length);
    }

    [Fact]
    public void Length_SetNegative_ThrowsException()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        try
        {
            collection.Length = -1;
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [Fact]
    public void Length_SetBeyondCapacity_ThrowsException()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        try
        {
            collection.Length = 11;
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [Fact]
    public void AsReadOnlySpan_ReturnsCorrectData()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        collection.Add(10);
        collection.Add(20);
        collection.Add(30);

        var span = collection.AsReadOnlySpan();

        Assert.Equal(3, span.Length);
        Assert.Equal(10, span[0]);
        Assert.Equal(20, span[1]);
        Assert.Equal(30, span[2]);
    }

    [Fact]
    public void AsReadOnlySpan_EmptyCollection_ReturnsEmptySpan()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        var span = collection.AsReadOnlySpan();

        Assert.Equal(0, span.Length);
    }

    [Fact]
    public void AsSpan_ReturnsFullBuffer()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        collection.Add(1);
        collection.Add(2);

        var span = collection.AsSpan();

        Assert.Equal(10, span.Length); // Returns full buffer capacity
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
    }

    [Fact]
    public void AsSpan_AllowsMutation()
    {
        Span<int> buffer = stackalloc int[10];
        var collection = new RefCollection<int>(buffer);

        collection.Add(1);
        collection.Add(2);

        var span = collection.AsSpan();
        span[0] = 100;

        Assert.Equal(100, collection.AsReadOnlySpan()[0]);
    }

    [Fact]
    public void WorksWithReferenceTypes()
    {
        var collection = new RefCollection<string>(10);

        collection.Add("hello");
        collection.Add("world");

        Assert.Equal(2, collection.Length);
        var span = collection.AsReadOnlySpan();
        Assert.Equal("hello", span[0]);
        Assert.Equal("world", span[1]);
    }

    [Fact]
    public void WorksWithStructs()
    {
        var collection = new RefCollection<TestStruct>(5);

        collection.Add(new TestStruct { Value = 1, Name = "One" });
        collection.Add(new TestStruct { Value = 2, Name = "Two" });

        Assert.Equal(2, collection.Length);
        var span = collection.AsReadOnlySpan();
        Assert.Equal(1, span[0].Value);
        Assert.Equal("One", span[0].Name);
        Assert.Equal(2, span[1].Value);
        Assert.Equal("Two", span[1].Name);
    }

    [Fact]
    public void Add_ToFilledBuffer_GrowsCorrectly()
    {
        Span<int> buffer = stackalloc int[4];
        var collection = new RefCollection<int>(buffer);

        // Fill the buffer exactly
        collection.Add(1);
        collection.Add(2);
        collection.Add(3);
        collection.Add(4);

        Assert.Equal(4, collection.Length);
        Assert.Equal(4, collection.Capacity);

        // Add one more to trigger growth
        collection.Add(5);

        Assert.Equal(5, collection.Length);
        Assert.True(collection.Capacity >= 5);
        var span = collection.AsReadOnlySpan();
        Assert.Equal(5, span[4]);
    }

    [Fact]
    public void Capacity_ReflectsCurrentBufferSize()
    {
        var collection = new RefCollection<int>(2);
        int initialCapacity = collection.Capacity;

        Assert.True(initialCapacity >= 2);

        // Add enough items to ensure growth (ArrayPool may return larger buffers)
        for (int i = 0; i < initialCapacity + 1; i++)
        {
            collection.Add(i);
        }

        Assert.True(collection.Capacity > initialCapacity);
    }

    private struct TestStruct
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }
}
