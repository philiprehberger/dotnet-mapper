# Philiprehberger.Mapper

[![CI](https://github.com/philiprehberger/dotnet-mapper/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-mapper/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.Mapper.svg)](https://www.nuget.org/packages/Philiprehberger.Mapper)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-mapper)](https://github.com/philiprehberger/dotnet-mapper/commits/main)

Ultra-simple object-to-object mapper with convention-based mapping and fluent overrides.

## Installation

```bash
dotnet add package Philiprehberger.Mapper
```

## Usage

```csharp
using Philiprehberger.Mapper;

// Convention-based mapping — properties matched by name
var order = new Order { Id = 1, CustomerName = "Alice", Total = 99.99m };
var dto = Mapper.Map<Order, OrderDto>(order);
// dto.Id = 1, dto.CustomerName = "Alice", dto.Total = 99.99m
```

### Map to Existing Object

```csharp
var existing = new OrderDto { Notes = "Keep this" };
Mapper.Map(order, existing);
// existing.Id = 1, existing.Notes = "Keep this"
```

### Fluent Configuration

```csharp
Mapper.Configure<Order, OrderDto>(cfg => cfg
    .Map(src => src.Name, dest => dest.CustomerName)
    .Ignore(dest => dest.InternalCode)
    .MapFrom<string, string>(dest => dest.FullAddress, src => $"{src.Street}, {src.City}")
);

var dto = Mapper.Map<Order, OrderDto>(order);
```

### Collection Mapping

```csharp
var orders = new List<Order> { order1, order2, order3 };

// Map to List
List<OrderDto> dtos = Mapper.MapList<Order, OrderDto>(orders);

// Map to Array
OrderDto[] array = Mapper.MapArray<Order, OrderDto>(orders);

// Map to lazy IEnumerable
IEnumerable<OrderDto> enumerable = Mapper.MapEnumerable<Order, OrderDto>(orders);
```

### Nested Object Mapping

Complex properties are recursively mapped when source and destination have matching property names with different complex types:

```csharp
public class Order { public Address ShippingAddress { get; set; } }
public class OrderDto { public AddressDto ShippingAddress { get; set; } }

var dto = Mapper.Map<Order, OrderDto>(order);
// dto.ShippingAddress is mapped from order.ShippingAddress recursively
```

### Custom Value Converters

```csharp
public class StringToDateConverter : IValueConverter<string, DateTime>
{
    public DateTime Convert(string source) => DateTime.Parse(source);
}

Mapper.Configure<Order, OrderDto>(cfg => cfg
    .UseConverter<string, DateTime>(new StringToDateConverter()));

var dto = Mapper.Map<Order, OrderDto>(order);
// String properties are converted to DateTime using the registered converter
```

### Flattening

Nested properties are automatically flattened by naming convention:

```csharp
public class Order { public Address ShippingAddress { get; set; } }
public class Address { public string City { get; set; } }
public class FlatDto { public string ShippingAddressCity { get; set; } }

var dto = Mapper.Map<Order, FlatDto>(order);
// dto.ShippingAddressCity = order.ShippingAddress.City
```

### Diagnostics

```csharp
var unmapped = MapperDiagnostics.GetUnmappedProperties<Order, OrderDto>();
MapperDiagnostics.ValidateMapping<Order, OrderDto>();
```

## API

### `Mapper`

| Method | Description |
|--------|-------------|
| `Map<TSource, TDest>(source)` | Maps source to a new destination instance |
| `Map<TSource, TDest>(source, dest)` | Maps source onto an existing destination |
| `MapList<TSource, TDest>(source)` | Maps a collection to a `List<TDest>` |
| `MapArray<TSource, TDest>(source)` | Maps a collection to a `TDest[]` array |
| `MapEnumerable<TSource, TDest>(source)` | Maps a collection to a lazy `IEnumerable<TDest>` |
| `Configure<TSource, TDest>(configure)` | Registers fluent mapping overrides |
| `Reset()` | Clears all configurations and cached mappings |

### `MappingConfiguration<TSource, TDestination>`

| Method | Description |
|--------|-------------|
| `Map<T>(source, dest)` | Explicitly maps a source property to a destination property |
| `Ignore(member)` | Excludes a destination property from mapping |
| `MapFrom<TSrc, TDest>(dest, resolver)` | Maps a destination property using a custom resolver function |
| `UseConverter<TSrc, TDest>(converter)` | Registers a value converter for type pair conversion |

### `IValueConverter<TSource, TDestination>`

| Method | Description |
|--------|-------------|
| `Convert(source)` | Converts a source value to the destination type |

### `MapperDiagnostics`

| Method | Description |
|--------|-------------|
| `GetUnmappedProperties<TSource, TDest>()` | Returns unmapped destination property names |
| `ValidateMapping<TSource, TDest>()` | Throws if any destination properties are unmapped |

### `MapToAttribute`

| Property | Description |
|----------|-------------|
| `DestinationType` | The target type this class maps to |

## Development

```bash
dotnet build src/Philiprehberger.Mapper.csproj --configuration Release
```

## Support

If you find this project useful:

⭐ [Star the repo](https://github.com/philiprehberger/dotnet-mapper)

🐛 [Report issues](https://github.com/philiprehberger/dotnet-mapper/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

💡 [Suggest features](https://github.com/philiprehberger/dotnet-mapper/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)

❤️ [Sponsor development](https://github.com/sponsors/philiprehberger)

🌐 [All Open Source Projects](https://philiprehberger.com/open-source-packages)

💻 [GitHub Profile](https://github.com/philiprehberger)

🔗 [LinkedIn Profile](https://www.linkedin.com/in/philiprehberger)

## License

[MIT](LICENSE)
