using System.Collections.Immutable;

namespace Infrastructure.Verticals.Context;

public static class ContextProvider
{
    private static readonly AsyncLocal<ImmutableDictionary<string, object>> _asyncLocalDict = new();

    public static void Set(string key, object value)
    {
        ImmutableDictionary<string, object> currentDict = _asyncLocalDict.Value ?? ImmutableDictionary<string, object>.Empty;
        _asyncLocalDict.Value = currentDict.SetItem(key, value);
    }

    public static object? Get(string key)
    {
        return _asyncLocalDict.Value?.TryGetValue(key, out object? value) is true ? value : null;
    }
    
    public static T Get<T>(string key) where T : class
    {
        return Get(key) as T ?? throw new Exception($"failed to get \"{key}\" from context");
    }
}
