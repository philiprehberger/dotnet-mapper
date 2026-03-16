# Philiprehberger.Mapper

Ultra-simple object-to-object mapper with convention-based mapping and fluent overrides.

## Install

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

### Flattening

Nested properties are automatically flattened by naming convention:

```csharp
public class Order
{
    public Address ShippingAddress { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class OrderDto
{
    public string ShippingAddressCity { get; set; } // Auto-mapped from Order.ShippingAddress.City
}

var dto = Mapper.Map<Order, OrderDto>(order);
```

### MapTo Attribute

Mark source classes for documentation and discovery:

```csharp
[MapTo(typeof(OrderDto))]
public class Order { /* ... */ }
```

### Diagnostics

```csharp
// Find unmapped properties
var unmapped = MapperDiagnostics.GetUnmappedProperties<Order, OrderDto>();

// Throw if any destination properties are unmapped
MapperDiagnostics.ValidateMapping<Order, OrderDto>();
```

## API

### `Mapper`

| Method | Description |
|--------|-------------|
| `Map<TSource, TDest>(source)` | Maps source to a new destination instance |
| `Map<TSource, TDest>(source, dest)` | Maps source onto an existing destination |
| `Configure<TSource, TDest>(configure)` | Registers fluent mapping overrides |
| `Reset()` | Clears all configurations and cached mappings |

### `MappingConfiguration<TSource, TDestination>`

| Method | Description |
|--------|-------------|
| `Map<T>(source, dest)` | Explicitly maps a source property to a destination property |
| `Ignore(member)` | Excludes a destination property from mapping |
| `MapFrom<TSrc, TDest>(dest, resolver)` | Maps a destination property using a custom resolver function |

### `MapperDiagnostics`

| Method | Description |
|--------|-------------|
| `GetUnmappedProperties<TSource, TDest>()` | Returns unmapped destination property names |
| `ValidateMapping<TSource, TDest>()` | Throws if any destination properties are unmapped |

### `MapToAttribute`

| Property | Description |
|----------|-------------|
| `DestinationType` | The target type this class maps to |

## License

MIT
