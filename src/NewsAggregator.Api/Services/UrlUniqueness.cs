namespace NewsAggregator.Api.Services;

public static class UrlUniqueness
{
    public static bool AnyDuplicateCaseInsensitive(IEnumerable<string> urls)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in urls)
        {
            if (!set.Add(u))
                return true;
        }
        return false;
    }
}
