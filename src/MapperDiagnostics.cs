using System.Reflection;

namespace Philiprehberger.Mapper;

/// <summary>
/// Provides diagnostic utilities for inspecting and validating property mappings
/// between source and destination types.
/// </summary>
public static class MapperDiagnostics
{
    /// <summary>
    /// Returns a list of writable destination properties that have no matching
    /// source property by name convention.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <returns>A read-only list of unmapped destination property names.</returns>
    public static IReadOnlyList<string> GetUnmappedProperties<TSource, TDestination>()
        where TSource : class
        where TDestination : class
    {
        var sourceNames = new HashSet<string>(
            typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => p.Name),
            StringComparer.OrdinalIgnoreCase);

        var destProps = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        var unmapped = new List<string>();

        foreach (var destProp in destProps)
        {
            if (!sourceNames.Contains(destProp.Name))
            {
                unmapped.Add(destProp.Name);
            }
        }

        return unmapped.AsReadOnly();
    }

    /// <summary>
    /// Validates that all writable destination properties have a matching source property.
    /// Throws an <see cref="InvalidOperationException"/> if any unmapped properties are found.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more destination properties have no matching source property.
    /// </exception>
    public static void ValidateMapping<TSource, TDestination>()
        where TSource : class
        where TDestination : class
    {
        var unmapped = GetUnmappedProperties<TSource, TDestination>();

        if (unmapped.Count > 0)
        {
            var typePair = $"{typeof(TSource).Name} -> {typeof(TDestination).Name}";
            var properties = string.Join(", ", unmapped);
            throw new InvalidOperationException(
                $"Mapping validation failed for {typePair}. " +
                $"Unmapped destination properties: {properties}");
        }
    }
}
