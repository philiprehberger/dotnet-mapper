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
    private static readonly ConcurrentDictionary<(Type Source, Type Destination), Delegate> _compiledDelegates = new();

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

        // Try compiled delegate first (no custom resolvers, converters, or nested mapping)
        if (!HasCustomConfiguration(key))
        {
            var compiled = _compiledDelegates.GetOrAdd(key, _ => CompileMapping<TSource, TDestination>());
            ((Action<TSource, TDestination>)compiled)(source, destination);
            return destination;
        }

        var mappings = _mappingCache.GetOrAdd(key, _ => BuildMappings<TSource, TDestination>());

        foreach (var mapping in mappings)
        {
            mapping.Apply(source, destination);
        }

        return destination;
    }

    /// <summary>
    /// Maps each element in a source collection to a new list of destination objects.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TDestination">The destination element type. Must have a parameterless constructor.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <returns>A list of mapped destination objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
        where TSource : class
        where TDestination : class, new()
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = new List<TDestination>();
        foreach (var item in source)
        {
            result.Add(Map<TSource, TDestination>(item));
        }
        return result;
    }

    /// <summary>
    /// Maps each element in a source collection to an array of destination objects.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TDestination">The destination element type. Must have a parameterless constructor.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <returns>An array of mapped destination objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static TDestination[] MapArray<TSource, TDestination>(IEnumerable<TSource> source)
        where TSource : class
        where TDestination : class, new()
    {
        ArgumentNullException.ThrowIfNull(source);
        return MapList<TSource, TDestination>(source).ToArray();
    }

    /// <summary>
    /// Maps each element in a source collection to an enumerable of destination objects.
    /// Elements are mapped lazily as the result is enumerated.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TDestination">The destination element type. Must have a parameterless constructor.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <returns>An enumerable of mapped destination objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static IEnumerable<TDestination> MapEnumerable<TSource, TDestination>(IEnumerable<TSource> source)
        where TSource : class
        where TDestination : class, new()
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            yield return Map<TSource, TDestination>(item);
        }
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
        _compiledDelegates.TryRemove(key, out _);
    }

    /// <summary>
    /// Removes all registered configurations and clears the mapping cache.
    /// Useful for testing or reconfiguring mappings at runtime.
    /// </summary>
    public static void Reset()
    {
        _configurations.Clear();
        _mappingCache.Clear();
        _compiledDelegates.Clear();
    }

    private static bool HasCustomConfiguration((Type Source, Type Destination) key)
    {
        return _configurations.ContainsKey(key);
    }

    private static Delegate CompileMapping<TSource, TDestination>()
        where TSource : class
        where TDestination : class
    {
        var sourceParam = Expression.Parameter(typeof(TSource), "source");
        var destParam = Expression.Parameter(typeof(TDestination), "dest");

        var sourceProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();
        var destProps = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var assignments = new List<Expression>();

        foreach (var sourceProp in sourceProps)
        {
            if (!destProps.TryGetValue(sourceProp.Name, out var destProp))
                continue;

            if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
            {
                var readSource = Expression.Property(sourceParam, sourceProp);
                var writeDest = Expression.Property(destParam, destProp);
                assignments.Add(Expression.Assign(writeDest, readSource));
            }
            else if (IsComplexType(sourceProp.PropertyType) && IsComplexType(destProp.PropertyType))
            {
                // Nested object mapping via reflection call
                var readSource = Expression.Property(sourceParam, sourceProp);
                var nestedMapMethod = typeof(Mapper)
                    .GetMethod(nameof(MapNested), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(sourceProp.PropertyType, destProp.PropertyType);

                var callNested = Expression.Call(nestedMapMethod, readSource);
                var writeDest = Expression.Property(destParam, destProp);
                var nullCheck = Expression.IfThen(
                    Expression.NotEqual(readSource, Expression.Constant(null, sourceProp.PropertyType)),
                    Expression.Assign(writeDest, callNested));
                assignments.Add(nullCheck);
            }
            else if (IsNumericType(Nullable.GetUnderlyingType(sourceProp.PropertyType) ?? sourceProp.PropertyType)
                     && IsNumericType(Nullable.GetUnderlyingType(destProp.PropertyType) ?? destProp.PropertyType))
            {
                var readSource = Expression.Property(sourceParam, sourceProp);
                var converted = Expression.Convert(readSource, destProp.PropertyType);
                var writeDest = Expression.Property(destParam, destProp);
                assignments.Add(Expression.Assign(writeDest, converted));
            }
        }

        var body = Expression.Block(assignments.Count > 0
            ? assignments
            : new List<Expression> { Expression.Empty() });

        var lambda = Expression.Lambda<Action<TSource, TDestination>>(body, sourceParam, destParam);
        return lambda.Compile();
    }

    private static TDestination MapNested<TSource, TDestination>(TSource source)
        where TSource : class
        where TDestination : class, new()
    {
        return Map<TSource, TDestination>(source);
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
        var valueConverters = config?.ValueConverters ?? new Dictionary<(Type Source, Type Dest), object>();

        var mappings = new List<PropertyMapping>();

        // Apply explicit mappings from configuration
        foreach (var (sourceName, destName) in explicitMappings)
        {
            var sourceProp = typeof(TSource).GetProperty(sourceName, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp is not null && destProps.TryGetValue(destName, out var destProp))
            {
                var converter = FindConverter(valueConverters, sourceProp.PropertyType, destProp.PropertyType);
                if (converter is not null)
                {
                    mappings.Add(new PropertyMapping(sourceProp, destProp, null, converter));
                }
                else if (IsAssignableOrConvertible(sourceProp.PropertyType, destProp.PropertyType))
                {
                    mappings.Add(new PropertyMapping(sourceProp, destProp, null, null));
                }
            }
        }

        // Apply custom resolvers
        foreach (var (destName, resolver) in customResolvers)
        {
            if (destProps.TryGetValue(destName, out var destProp))
            {
                mappings.Add(new PropertyMapping(null, destProp, resolver, null));
            }
        }

        var handledDestProperties = new HashSet<string>(
            mappings.Select(m => m.DestinationProperty.Name),
            StringComparer.OrdinalIgnoreCase);

        // Convention-based direct name match
        foreach (var sourceProp in sourceProps)
        {
            if (!destProps.TryGetValue(sourceProp.Name, out var destProp)
                || ignoredProperties.Contains(destProp.Name)
                || handledDestProperties.Contains(destProp.Name))
                continue;

            // Check for value converter
            var converter = FindConverter(valueConverters, sourceProp.PropertyType, destProp.PropertyType);
            if (converter is not null)
            {
                mappings.Add(new PropertyMapping(sourceProp, destProp, null, converter));
                handledDestProperties.Add(destProp.Name);
                continue;
            }

            if (IsAssignableOrConvertible(sourceProp.PropertyType, destProp.PropertyType))
            {
                mappings.Add(new PropertyMapping(sourceProp, destProp, null, null));
                handledDestProperties.Add(destProp.Name);
            }
            else if (IsComplexType(sourceProp.PropertyType) && IsComplexType(destProp.PropertyType))
            {
                // Nested object mapping
                var nestedResolver = BuildNestedResolver(sourceProp, destProp);
                mappings.Add(new PropertyMapping(null, destProp, nestedResolver, null));
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
                mappings.Add(new PropertyMapping(null, destProp, resolver, null));
            }
        }

        return mappings.ToArray();
    }

    private static Func<object, object?>? FindConverter(
        Dictionary<(Type Source, Type Dest), object> converters,
        Type sourceType,
        Type destType)
    {
        if (converters.Count == 0)
            return null;

        var converterKey = (sourceType, destType);
        if (!converters.TryGetValue(converterKey, out var converter))
            return null;

        var converterType = typeof(IValueConverter<,>).MakeGenericType(sourceType, destType);
        var convertMethod = converterType.GetMethod("Convert")!;

        return source =>
        {
            var value = source is PropertyInfo ? source : source;
            return convertMethod.Invoke(converter, new[] { value });
        };
    }

    private static Func<object, object?> BuildNestedResolver(PropertyInfo sourceProp, PropertyInfo destProp)
    {
        var mapMethod = typeof(Mapper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "Map"
                        && m.GetParameters().Length == 1
                        && m.GetGenericArguments().Length == 2)
            .MakeGenericMethod(sourceProp.PropertyType, destProp.PropertyType);

        return source =>
        {
            var nestedSource = sourceProp.GetValue(source);
            if (nestedSource is null)
                return null;
            return mapMethod.Invoke(null, new[] { nestedSource });
        };
    }

    private static bool IsComplexType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return !underlying.IsPrimitive
               && !underlying.IsEnum
               && underlying != typeof(string)
               && underlying != typeof(decimal)
               && underlying != typeof(DateTime)
               && underlying != typeof(DateTimeOffset)
               && underlying != typeof(TimeSpan)
               && underlying != typeof(Guid)
               && !underlying.IsArray;
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
    private readonly Func<object, object?>? _valueConverter;

    public PropertyMapping(
        PropertyInfo? sourceProperty,
        PropertyInfo destinationProperty,
        Func<object, object?>? customResolver,
        Func<object, object?>? valueConverter)
    {
        SourceProperty = sourceProperty;
        DestinationProperty = destinationProperty;
        _customResolver = customResolver;
        _valueConverter = valueConverter;
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
            var rawValue = SourceProperty.GetValue(source);

            if (_valueConverter is not null && rawValue is not null)
            {
                value = _valueConverter(rawValue);
            }
            else
            {
                value = rawValue;
            }
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
