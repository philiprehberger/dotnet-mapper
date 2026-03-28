namespace Philiprehberger.Mapper;

/// <summary>
/// Defines a converter that transforms a value from one type to another
/// during property mapping.
/// </summary>
/// <typeparam name="TSource">The source value type.</typeparam>
/// <typeparam name="TDestination">The destination value type.</typeparam>
public interface IValueConverter<in TSource, out TDestination>
{
    /// <summary>
    /// Converts a source value to the destination type.
    /// </summary>
    /// <param name="source">The source value to convert.</param>
    /// <returns>The converted destination value.</returns>
    TDestination Convert(TSource source);
}
