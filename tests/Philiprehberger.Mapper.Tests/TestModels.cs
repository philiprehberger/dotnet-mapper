using Xunit;
namespace Philiprehberger.Mapper.Tests;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Total { get; set; }
    public Address ShippingAddress { get; set; } = new();
    public string OrderDate { get; set; } = "";
}

public class OrderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Total { get; set; }
    public AddressDto ShippingAddress { get; set; } = new();
    public DateTime OrderDate { get; set; }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class FlatOrderDto
{
    public int Id { get; set; }
    public string ShippingAddressCity { get; set; } = "";
}

public class Person
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
}

public class PersonDto
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
}

public class StringToDateTimeConverter : IValueConverter<string, DateTime>
{
    public DateTime Convert(string source) => string.IsNullOrEmpty(source) ? default : DateTime.Parse(source);
}
