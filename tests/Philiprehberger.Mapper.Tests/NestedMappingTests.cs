using Xunit;
namespace Philiprehberger.Mapper.Tests;

public class NestedMappingTests : IDisposable
{
    public void Dispose() => Mapper.Reset();

    [Fact]
    public void Map_NestedObject_MapsRecursively()
    {
        var order = new Order
        {
            Id = 1,
            Name = "Test Order",
            ShippingAddress = new Address { Street = "123 Main St", City = "Springfield", Zip = "62701" }
        };

        // Use Configure to trigger the reflection-based path (which handles nested mapping)
        Mapper.Configure<Order, OrderDto>(cfg => cfg.Ignore(d => d.OrderDate));

        var dto = Mapper.Map<Order, OrderDto>(order);

        Assert.Equal("123 Main St", dto.ShippingAddress.Street);
        Assert.Equal("Springfield", dto.ShippingAddress.City);
        Assert.Equal("62701", dto.ShippingAddress.Zip);
    }

    [Fact]
    public void Map_NestedObject_NullSource_DoesNotThrow()
    {
        var order = new Order
        {
            Id = 1,
            Name = "Test",
            ShippingAddress = null!
        };

        Mapper.Configure<Order, OrderDto>(cfg => cfg.Ignore(d => d.OrderDate));

        var dto = Mapper.Map<Order, OrderDto>(order);

        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public void Map_NestedObject_PreservesTopLevelProperties()
    {
        var order = new Order
        {
            Id = 42,
            Name = "Important Order",
            Total = 199.99m,
            ShippingAddress = new Address { Street = "456 Oak Ave", City = "Portland", Zip = "97201" }
        };

        var dto = Mapper.Map<Order, OrderDto>(order);

        Assert.Equal(42, dto.Id);
        Assert.Equal("Important Order", dto.Name);
        Assert.Equal(199.99m, dto.Total);
        Assert.Equal("Portland", dto.ShippingAddress.City);
    }
}
