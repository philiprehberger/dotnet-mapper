using System.Linq.Expressions;

namespace Philiprehberger.Mapper;

/// <summary>
/// Fluent configuration for customizing property mappings between a source
/// and destination type pair.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TDestination">The destination type.</typeparam>
public sealed class MappingConfiguration<TSource, TDestination>
    where TSource : class
    where TDestination : class
{
    internal HashSet<string> IgnoredProperties { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, Func<object, object?>> CustomResolvers { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, string> ExplicitMappings { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<(Type Source, Type Dest), object> ValueConverters { get; } = new();

    /// <summary>
    /// Maps a source property to a destination property with the same type,
    /// overriding the convention-based name matching.
    /// </summary>
    /// <typeparam name="TMember">The property type.</typeparam>
    /// <param name="source">An expression selecting the source property.</param>
    /// <param name="destination">An expression selecting the destination property.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public MappingConfiguration<TSource, TDestination> Map<TMember>(
        Expression<Func<TSource, TMember>> source,
        Expression<Func<TDestination, TMember>> destination)
    {
        var sourceName = GetMemberName(source);
        var destName = GetMemberName(destination);
        ExplicitMappings[sourceName] = destName;
        return this;
    }

    /// <summary>
    /// Ignores a destination property, preventing it from being mapped.
    /// </summary>
    /// <param name="member">An expression selecting the destination property to ignore.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public MappingConfiguration<TSource, TDestination> Ignore(
        Expression<Func<TDestination, object?>> member)
    {
        var name = GetMemberName(member);
        IgnoredProperties.Add(name);
        return this;
    }

    /// <summary>
    /// Maps a destination property using a custom resolver function that
    /// receives the source object and returns the value.
    /// </summary>
    /// <typeparam name="TSourceMember">The source expression type (unused, for API clarity).</typeparam>
    /// <typeparam name="TDestMember">The destination property type.</typeparam>
    /// <param name="destination">An expression selecting the destination property.</param>
    /// <param name="resolver">A function that receives the source object and returns the destination value.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public MappingConfiguration<TSource, TDestination> MapFrom<TSourceMember, TDestMember>(
        Expression<Func<TDestination, TDestMember>> destination,
        Func<TSource, TDestMember> resolver)
    {
        var destName = GetMemberName(destination);
        CustomResolvers[destName] = source => resolver((TSource)source);
        return this;
    }

    /// <summary>
    /// Registers a custom value converter for mapping between two property types.
    /// When a source property of type <typeparamref name="TSrc"/> is mapped to a
    /// destination property of type <typeparamref name="TDest"/>, the converter is
    /// used instead of the default assignment.
    /// </summary>
    /// <typeparam name="TSrc">The source property type.</typeparam>
    /// <typeparam name="TDest">The destination property type.</typeparam>
    /// <param name="converter">The value converter instance.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converter"/> is null.</exception>
    public MappingConfiguration<TSource, TDestination> UseConverter<TSrc, TDest>(IValueConverter<TSrc, TDest> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ValueConverters[(typeof(TSrc), typeof(TDest))] = converter;
        return this;
    }

    internal static string GetMemberName<T, TMember>(Expression<Func<T, TMember>> expression)
    {
        return expression.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression member } => member.Member.Name,
            _ => throw new ArgumentException(
                $"Expression must be a property access expression, got: {expression.Body.NodeType}",
                nameof(expression))
        };
    }
}
