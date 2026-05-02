namespace AssistantEngineer.Api.Extensions.Collections;

public static class FilterExtensions
{
    public static IEnumerable<T> ApplyFilter<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<IEnumerable<T>, IEnumerable<T>> filter)
    {
        return condition
            ? filter(source)
            : source;
    }

    public static IEnumerable<T> ApplyValueFilter<T, TValue>(
        this IEnumerable<T> source,
        TValue? value,
        Func<T, TValue> selector)
        where TValue : struct
    {
        if (!value.HasValue)
            return source;

        var expected = value.Value;

        return source.Where(item =>
        {
            var actual = selector(item);

            return EqualityComparer<TValue>.Default.Equals(
                actual,
                expected);
        });
    }

    public static IEnumerable<T> ApplyNullableValueFilter<T, TValue>(
        this IEnumerable<T> source,
        TValue? value,
        Func<T, TValue?> selector)
        where TValue : struct
    {
        if (!value.HasValue)
            return source;

        var expected = value.Value;

        return source.Where(item =>
        {
            var actual = selector(item);

            return actual.HasValue &&
                   EqualityComparer<TValue>.Default.Equals(
                       actual.Value,
                       expected);
        });
    }

    public static IEnumerable<T> ApplyStringEqualsFilter<T>(
        this IEnumerable<T> source,
        string? value,
        Func<T, string?> selector)
    {
        var expected = Normalize(value);

        if (expected is null)
            return source;

        return source.Where(item =>
            string.Equals(
                Normalize(selector(item)),
                expected,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string? Normalize(
        string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}