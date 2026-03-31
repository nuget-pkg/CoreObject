using System.Collections.Generic;
using System.Dynamic;
namespace Core;

using static Core.CoreObject;
internal static class CoreObjectEditor {
    public static CoreObject Clone(
        CoreObject x,
        uint maxDepth = 0,
        List<string>? hideKeys = null,
        bool always = true
    ) {
        if (x == null) return Null;
        hideKeys = hideKeys ?? new List<string>();
        if (!always)
            if (maxDepth == 0 && hideKeys.Count == 0)
                return x;
        x = FromObject(x);
        Trim(x, maxDepth, hideKeys);
        return x;
    }
    public static void Trim(
        CoreObject x,
        uint maxDepth = 0,
        List<string>? hideKeys = null
    ) {
        if (x == null) return;
        hideKeys = hideKeys ?? new List<string>();
        TrimHelper(1, x, maxDepth, hideKeys);
    }
    private static void TrimHelper(
        uint depth,
        CoreObject x,
        uint maxDepth,
        List<string> hideKeys
    ) {
        if (x == null) return;
        if (maxDepth > 0)
            if (depth >= maxDepth) {
                if (x.IsArray) {
                    for (var i = 0; i < x.Count; i++) Clear(x.RealList![i]);
                    //return;
                }
                else if (x.IsObject) {
                    var keys = x.Keys;
                    for (var i = 0; i < keys.Count; i++) {
                        var key = keys[i];
                        Clear(x.RealDictionary![key]);
                    }
                    //return;
                }
            }
        if (x.IsArray) {
            for (var i = 0; i < x.Count; i++) TrimHelper(depth + 1, x.RealList![i], maxDepth, hideKeys);
        }
        else if (x.IsObject) {
            var keys = x.Keys;
            for (var i = 0; i < keys.Count; i++) {
                var key = keys[i];
                if (hideKeys.Contains(key)) {
                    x.RealDictionary!.Remove(key);
                    continue;
                }
                TrimHelper(depth + 1, x.RealDictionary![key], maxDepth, hideKeys);
            }
        }
    }
    private static void Clear(CoreObject x) {
        if (x == null) return;
        if (x.IsArray)
            x.RealList!.Clear();
        else if (x.IsObject) x.RealDictionary!.Clear();
    }
    public static dynamic? ExportToExpandoObject(CoreObject x) {
        if (x.IsNull) return null;
        if (x.IsArray) {
            var result = new List<dynamic?>();
            var list = x.RealList!;
            foreach (var item in list) result.Add(ExportToExpandoObject(item));
            return result;
        }
        if (x.IsObject) {
            var result = new ExpandoObject();
            var dictionary = x.RealDictionary!;
            var keys = dictionary.Keys!;
            foreach (var key in keys)
                (result as IDictionary<string, object?>)[key] = ExportToExpandoObject(dictionary[key]);
            return result;
        }
        return x.RealData;
    }
}