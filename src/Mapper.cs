using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Philiprehberger.Mapper;

/// <summary>
/// Ultra-simple object-to-object mapper with convention-based property mapping
/// and fluent configuration overrides.
/// </summary>
public static class Mapper
{
    private static readonly ConcurrentDictionary<(Type Source, Type Destination), object> _configurations = new();
    private static readonly ConcurrentDictionary<(Type Source, Type Destination), PropertyMapping[]> _mappingCache = new();

    /// <summary>
    /// Maps a source object to a new instance of the destination type using
    /// convention-based property matching.
    /// </summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TDestination">The destination type to map to. Must have a parameterless constructor.</typeparam>
    /// <param name="source">The source object.</param>
    /// <returns>A new instance of <typeparamref name="TDestination"/> with mapped property values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static TDestination Map<TSource, TDestination>(TSource source)
        where TSource : class
        where TDestination : class, new()
    {
        ArgumentNullException.ThrowIfNull(source);

        var destination = new TDestination();
        return Map(source, destination);
    }

    /// <summary>
    /// Maps a source object onto an existing destination object using
    /// convention-based property matching.
    /// </summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TDestination">The destination type to map to.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="destination">The existing destination object to map onto.</param>
    /// <returns>The destination object with mapped property values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
    public static TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        where TSource : class
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var key = (typeof(TSource), typeof(TDestination));
        var mappings = _mappingCache.GetOrAdd(key, _ => BuildMappings<TSource, TDestination>());

        foreach (var mapping in mappings)
        {
            mapping.Apply(source, destination);
        }

        return destination;
    }

    /// <summary>
    /// Registers a custom mapping configuration for the specified type pair.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="configure">An action that configures the mapping using a fluent API.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    public static void Configure<TSource, TDestination>(Action<MappingConfiguration<TSource, TDestination>> configure)
        where TSource : class
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new MappingConfiguration<TSource, TDestination>();
        configure(config);

        var key = (typeof(TSource), typeof(TDestination));
        _configurations[key] = config;
        _mappingCache.TryRemove(key, out _);
    }

    /// <summary>
    /// Removes all registered configurations and clears the mapping cache.
    /// Useful for testing or reconfiguring mappings at runtime.
    /// </summary>
    public static void Reset()
    {
        _configurations.Clear();
        _mappingCache.Clear();
    }

    private static PropertyMapping[] BuildMappings<TSource, TDestination>()
        where TSource : class
        where TDestination : class
    {
        var key = (typeof(TSource), typeof(TDestination));
        var sourceProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();
        var destProps = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        MappingConfiguration<TSource, TDestination>? config = null;
        if (_configurations.TryGetValue(key, out var raw))
        {
            config = (MappingConfiguration<TSource, TDestination>)raw;
        }

        var ignoredProperties = config?.IgnoredProperties ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var customResolvers = config?.CustomResolvers ?? new Dictionary<string, Func<object, object?>>();
        var explicitMappings = config?.ExplicitMappings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var mappings = new List<PropertyMapping>();

        // Apply explicit mappings from configuration
        foreach (var (sourceName, destName) in explicitMappings)
        {
            var sourceProp = typeof(TSource).GetProperty(sourceName, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp is not null && destProps.TryGetValue(destName, out var destProp))
            {
                if (IsAssignableOrConvertible(sourceProp.PropertyType, destProp.PropertyType))
                {
                    mappings.Add(new PropertyMapping(sourceProp, destProp, null));
                }
            }
        }

        // Apply custom resolvers
        foreach (var (destName, resolver) in customResolvers)
        {
            if (destProps.TryGetValue(destName, out var destProp))
            {
                mappings.Add(new PropertyMapping(null, destProp, resolver));
            }
        }

        var handledDestProperties = new HashSet<string>(
            mappings.Select(m => m.DestinationProperty.Name),
            StringComparer.OrdinalIgnoreCase);

        // Convention-based direct name match
        foreach (var sourceProp in sourceProps)
        {
            if (destProps.TryGetValue(sourceProp.Name, out var destProp)
                && !ignoredProperties.Contains(destProp.Name)
                && !handledDestProperties.Contains(destProp.Name)
                && IsAssignableOrConvertible(sourceProp.PropertyType, destProp.PropertyType))
            {
                mappings.Add(new PropertyMapping(sourceProp, destProp, null));
                handledDestProperties.Add(destProp.Name);
            }
        }

        // Flattening convention: Customer.Address.City -> CustomerAddressCity
        foreach (var destProp in destProps.Values)
        {
            if (ignoredProperties.Contains(destProp.Name) || handledDestProperties.Contains(destProp.Name))
                continue;

            var resolver = TryBuildFlattenResolver(typeof(TSource), destProp);
            if (resolver is not null)
            {
                mappings.Add(new PropertyMapping(null, destProp, resolver));
            }
        }

        return mappings.ToArray();
    }

    private static Func<object, object?>? TryBuildFlattenResolver(Type sourceType, PropertyInfo destProp)
    {
        var destName = destProp.Name;
        var sourceProps = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        foreach (var topProp in sourceProps)
        {
            if (!destName.StartsWith(topProp.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            var remaining = destName[topProp.Name.Length..];
            if (remaining.Length == 0)
                continue;

            var chain = new List<PropertyInfo> { topProp };
            if (TryResolvePath(topProp.PropertyType, remaining, chain))
            {
                var leafType = chain[^1].PropertyType;
                if (!IsAssignableOrConvertible(leafType, destProp.PropertyType))
                    continue;

                var capturedChain = chain.ToArray();
                return source =>
                {
                    object? current = source;
                    foreach (var prop in capturedChain)
                    {
                        if (current is null) return null;
                        current = prop.GetValue(current);
                    }
                    return current;
                };
            }
        }

        return null;
    }

    private static bool TryResolvePath(Type type, string remaining, List<PropertyInfo> chain)
    {
        if (remaining.Length == 0)
            return true;

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        foreach (var prop in props)
        {
            if (!remaining.StartsWith(prop.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            chain.Add(prop);
            var next = remaining[prop.Name.Length..];

            if (next.Length == 0)
                return true;

            if (TryResolvePath(prop.PropertyType, next, chain))
                return true;

            chain.RemoveAt(chain.Count - 1);
        }

        return false;
    }

    private static bool IsAssignableOrConvertible(Type sourceType, Type destType)
    {
        if (destType.IsAssignableFrom(sourceType))
            return true;

        var sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var destUnderlying = Nullable.GetUnderlyingType(destType) ?? destType;

        if (destUnderlying.IsAssignableFrom(sourceUnderlying))
            return true;

        // Numeric widening conversions
        if (IsNumericType(sourceUnderlying) && IsNumericType(destUnderlying))
            return true;

        return false;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) ||
        type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) ||
        type == typeof(long) || type == typeof(ulong) ||
        type == typeof(float) || type == typeof(double) ||
        type == typeof(decimal);
}

internal sealed class PropertyMapping
{
    public PropertyInfo? SourceProperty { get; }
    public PropertyInfo DestinationProperty { get; }
    private readonly Func<object, object?>? _customResolver;

    public PropertyMapping(PropertyInfo? sourceProperty, PropertyInfo destinationProperty, Func<object, object?>? customResolver)
    {
        SourceProperty = sourceProperty;
        DestinationProperty = destinationProperty;
        _customResolver = customResolver;
    }

    public void Apply(object source, object destination)
    {
        object? value;

        if (_customResolver is not null)
        {
            value = _customResolver(source);
        }
        else if (SourceProperty is not null)
        {
            value = SourceProperty.GetValue(source);
        }
        else
        {
            return;
        }

        if (value is not null || IsNullable(DestinationProperty.PropertyType))
        {
            try
            {
                var destType = Nullable.GetUnderlyingType(DestinationProperty.PropertyType)
                    ?? DestinationProperty.PropertyType;

                if (value is not null && !DestinationProperty.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    value = Convert.ChangeType(value, destType);
                }

                DestinationProperty.SetValue(destination, value);
            }
            catch (InvalidCastException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
        }
    }

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
