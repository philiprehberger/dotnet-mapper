using Xunit;
namespace Philiprehberger.Mapper.Tests;

public class CollectionMappingTests : IDisposable
{
    public void Dispose() => Mapper.Reset();

    [Fact]
    public void MapList_MapsEachElement()
    {
        var people = new List<Person>
        {
            new() { FirstName = "Alice", LastName = "Smith", Age = 30 },
            new() { FirstName = "Bob", LastName = "Jones", Age = 25 }
        };

        var result = Mapper.MapList<Person, PersonDto>(people);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].FirstName);
        Assert.Equal("Bob", result[1].FirstName);
    }

    [Fact]
    public void MapArray_ReturnsArray()
    {
        var people = new[]
        {
            new Person { FirstName = "Alice", Age = 30 },
            new Person { FirstName = "Bob", Age = 25 }
        };

        var result = Mapper.MapArray<Person, PersonDto>(people);

        Assert.IsType<PersonDto[]>(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(30, result[0].Age);
        Assert.Equal(25, result[1].Age);
    }

    [Fact]
    public void MapEnumerable_ReturnsLazyEnumerable()
    {
        var people = new[]
        {
            new Person { FirstName = "Alice" },
            new Person { FirstName = "Bob" },
            new Person { FirstName = "Charlie" }
        };

        var result = Mapper.MapEnumerable<Person, PersonDto>(people);

        Assert.Equal(3, result.Count());
        Assert.Equal("Charlie", result.Last().FirstName);
    }

    [Fact]
    public void MapList_EmptyCollection_ReturnsEmptyList()
    {
        var empty = Array.Empty<Person>();

        var result = Mapper.MapList<Person, PersonDto>(empty);

        Assert.Empty(result);
    }

    [Fact]
    public void MapList_ThrowsOnNullSource()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Mapper.MapList<Person, PersonDto>(null!));
    }
}
