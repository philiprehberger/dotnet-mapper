using Xunit;
namespace Philiprehberger.Mapper.Tests;

public class ValueConverterTests : IDisposable
{
    public void Dispose() => Mapper.Reset();

    [Fact]
    public void UseConverter_ConvertsDuringMapping()
    {
        Mapper.Configure<Order, OrderDto>(cfg => cfg
            .UseConverter<string, DateTime>(new StringToDateTimeConverter()));

        var order = new Order
        {
            Id = 1,
            Name = "Test",
            OrderDate = "2026-03-27"
        };

        var dto = Mapper.Map<Order, OrderDto>(order);

        Assert.Equal(new DateTime(2026, 3, 27), dto.OrderDate);
    }

    [Fact]
    public void UseConverter_OnlyAppliesForMatchingTypes()
    {
        Mapper.Configure<Order, OrderDto>(cfg => cfg
            .UseConverter<string, DateTime>(new StringToDateTimeConverter()));

        var order = new Order
        {
            Id = 5,
            Name = "Order Five",
            Total = 50m,
            OrderDate = "2026-01-01"
        };

        var dto = Mapper.Map<Order, OrderDto>(order);

        Assert.Equal(5, dto.Id);
        Assert.Equal("Order Five", dto.Name);
        Assert.Equal(50m, dto.Total);
    }

    [Fact]
    public void UseConverter_ThrowsOnNullConverter()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Mapper.Configure<Order, OrderDto>(cfg =>
                cfg.UseConverter<string, DateTime>(null!));
        });
    }

    [Fact]
    public void UseConverter_ChainableWithOtherConfig()
    {
        Mapper.Configure<Order, OrderDto>(cfg => cfg
            .UseConverter<string, DateTime>(new StringToDateTimeConverter())
            .Ignore(d => d.Total));

        var order = new Order
        {
            Id = 1,
            Name = "Test",
            Total = 100m,
            OrderDate = "2026-06-15"
        };

        var dto = Mapper.Map<Order, OrderDto>(order);

        Assert.Equal(new DateTime(2026, 6, 15), dto.OrderDate);
        Assert.Equal(0m, dto.Total);
    }
}
