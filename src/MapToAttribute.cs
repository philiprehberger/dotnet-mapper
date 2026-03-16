namespace Philiprehberger.Mapper;

/// <summary>
/// Marker attribute that indicates which type a class should be mapped to.
/// Used for documentation and discovery purposes. Does not affect mapping behavior in v0.1.0.
/// </summary>
/// <example>
/// <code>
/// [MapTo(typeof(OrderDto))]
/// public class Order
/// {
///     public int Id { get; set; }
///     public string Name { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MapToAttribute : Attribute
{
    /// <summary>
    /// Gets the destination type that this class maps to.
    /// </summary>
    public Type DestinationType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapToAttribute"/> class.
    /// </summary>
    /// <param name="destinationType">The type this class should be mapped to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destinationType"/> is null.</exception>
    public MapToAttribute(Type destinationType)
    {
        DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
    }
}
