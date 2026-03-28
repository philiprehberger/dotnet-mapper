using Xunit;
namespace Philiprehberger.Mapper.Tests;

public class ExpressionCompilationTests : IDisposable
{
    public void Dispose() => Mapper.Reset();

    [Fact]
    public void Map_WithoutConfig_UsesCompiledDelegate()
    {
        var person = new Person { FirstName = "Alice", LastName = "Smith", Age = 30 };

        var dto = Mapper.Map<Person, PersonDto>(person);

        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
        Assert.Equal(30, dto.Age);
    }

    [Fact]
    public void Map_SecondCall_UsesCachedDelegate()
    {
        var p1 = new Person { FirstName = "Alice", Age = 30 };
        var p2 = new Person { FirstName = "Bob", Age = 25 };

        var dto1 = Mapper.Map<Person, PersonDto>(p1);
        var dto2 = Mapper.Map<Person, PersonDto>(p2);

        Assert.Equal("Alice", dto1.FirstName);
        Assert.Equal("Bob", dto2.FirstName);
    }

    [Fact]
    public void Reset_ClearsCompiledDelegates()
    {
        var person = new Person { FirstName = "Alice", Age = 30 };
        Mapper.Map<Person, PersonDto>(person);

        Mapper.Reset();

        var dto = Mapper.Map<Person, PersonDto>(person);
        Assert.Equal("Alice", dto.FirstName);
    }

    [Fact]
    public void Configure_InvalidatesCompiledDelegate()
    {
        var person = new Person { FirstName = "Alice", LastName = "Smith", Age = 30 };

        // First call uses compiled delegate
        var dto1 = Mapper.Map<Person, PersonDto>(person);
        Assert.Equal("Smith", dto1.LastName);

        // Configure switches to reflection-based path
        Mapper.Configure<Person, PersonDto>(cfg => cfg.Ignore(d => d.LastName));
        var dto2 = Mapper.Map<Person, PersonDto>(person);
        Assert.Equal("", dto2.LastName);
    }
}
