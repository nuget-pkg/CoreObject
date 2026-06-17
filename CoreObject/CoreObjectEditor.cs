namespace Core
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using static Core.CoreObject;
    internal static class CoreObjectEditor
    {
        public static CoreObject Clone(
            CoreObject x,
            uint maxDepth = 0,
            uint maxCount = 0,
            List<string>? hideKeys = null,
            bool always = true
        )
        {
            //if (x == null) return Null;
            //Debug(new {x, maxDepth, maxCount, hideKeys }, "At beginning of Clone()");
            hideKeys = hideKeys ?? new List<string>();
            if (!always)
            {
                if (maxDepth == 0 && maxCount == 0 && hideKeys.Count == 0)
                {
                    return x;
                }
            }
            x = FromObject(x);
            Trim(x, maxDepth: maxDepth, maxCount: maxCount, hideKeys: hideKeys);
            return x; /**/
        }
        public static void Trim(
            CoreObject x,
            uint maxDepth = 0,
            uint maxCount = 0,
            List<string>? hideKeys = null
        )
        {
            hideKeys = (hideKeys ?? new List<string>());
            CoreObject y = x;
            y = TrimDepth(1, x, maxDepth: maxDepth);
            y = TrimKeys(x, hideKeys: hideKeys);
            y = TrimCount(x, maxCount: maxCount);
            x.RealData = y.RealData;
        }
        private static CoreObject TrimDepth(
            uint depth,
            CoreObject x,
            uint maxDepth
        )
        {
            if (maxDepth > 0)
            {
                if (depth >= maxDepth)
                {
                    if (x.IsArray)
                    {
                        for (int i = 0; i < x.Count; i++)
                        {
                            Clear(x.RealList![i]);
                        }
                        //return;
                    }
                    else if (x.IsObject)
                    {
                        var keys = x.Keys;
                        for (int i = 0; i < keys.Count; i++)
                        {
                            string key = keys[i];
                            Clear(x.RealDictionary![key]);
                        }
                    }
                }
                else
                {
                    if (x.IsArray)
                    {
                        for (int i = 0; i < x.Count; i++)
                        {
                            TrimDepth(depth + 1, x.RealList![i], maxDepth: maxDepth);
                        }
                    }
                    else if (x.IsObject)
                    {
                        var keys = x.Keys;
                        for (int i = 0; i < keys.Count; i++)
                        {
                            string key = keys[i];
                            TrimDepth(depth + 1, x.RealDictionary![key], maxDepth: maxDepth);
                        }
                    }
                }
            }
            return CoreObject.FromObject(x);
        }
        private static CoreObject TrimKeys(
            CoreObject x,
            List<string> hideKeys
        )
        {
            if (x.IsObject)
            {
                for (int i = 0; i < hideKeys.Count; i++)
                {
                    string key = hideKeys[i];
                    if (x.RealDictionary!.ContainsKey(key))
                    {
                        x.RealDictionary.Remove(key);
                    }
                    var keys = x.Keys;
                    for (int j = 0; j < keys.Count; j++)
                    {
                        TrimKeys(x.RealDictionary[keys[j]], hideKeys: hideKeys);
                    }
                }
            }
            else if (x.IsArray)
            {
                for (int i = 0; i < x.Count; i++)
                {
                    TrimKeys(x.RealList![i], hideKeys: hideKeys);
                }
            }
            return CoreObject.FromObject(x);
        }
        private static CoreObject TrimCount(
            CoreObject x,
            uint maxCount
        )
        {
            if (maxCount > 0)
            {
                if (x.IsArray)
                {
                    var oldList = x.RealList!.Take((int)maxCount).ToList();
                    var newList = oldList.Select(x => TrimCount(x, maxCount: maxCount)).ToList();
                    return CoreObject.FromObject(newList);
                }
                else if (x.IsObject)
                {
                    Dictionary<string, CoreObject> oldDict = x.RealDictionary!;
                    var keys = oldDict.Keys.Take((int)maxCount).ToList(); ;
                    Dictionary<string, CoreObject> newDict = new Dictionary<string, CoreObject>();
                    for (int i = 0; i < Math.Min(keys.Count, maxCount); i++)
                    {
                        newDict[keys[i]] = TrimCount(oldDict[keys[i]], maxCount: maxCount);
                    }
                    return CoreObject.FromObject(newDict);
                }
            }
            return CoreObject.FromObject(x);
        }
        public static CoreObject ShallowTake(CoreObject x, int n)
        {
            if (x.RealList != null)
            {
                var result = x.RealList!.Select(i => i).Take(n).ToList();
                return FromObject(result);
            }
            else if (x.RealDictionary != null)
            {
                var keys = x.RealDictionary.Keys.Select(i => i).Take(n).ToList();
                var result = NewObject();
                foreach (var key in keys) result[key] = x.RealDictionary[key];
                return result;
            }
            return Clone(x);
        }
        public static CoreObject DeepTake(CoreObject x, int n)
        {
            if (x.RealList != null)
            {
                var result = x.RealList!.Select(i => i).Take(n).ToList();
                result = result.Select(i => DeepTake(i, n)).ToList();
                return FromObject(result);
            }
            else if (x.RealDictionary != null)
            {
                var keys = x.RealDictionary.Keys.Select(i => i).Take(n).ToList();
                var result = NewObject();
                foreach (var key in keys) result[key] = DeepTake(x.RealDictionary[key], n);
                return result;
            }
            return Clone(x);
        }
        private static void Clear(CoreObject x)
        {
            if (x.IsArray)
            {
                x.RealList!.Clear();
            }
            else if (x.IsObject)
            {
                x.RealDictionary!.Clear();
            }
        }
        public static dynamic? ExportToExpandoObject(CoreObject x)
        {
            if (x.IsNull) return null;
            if (x.IsArray)
            {
                var result = new List<dynamic?>();
                var list = x.RealList!;
                foreach (var item in list)
                {
                    result.Add(ExportToExpandoObject(item));
                }
                return result;
            }
            else if (x.IsObject)
            {
                var result = new ExpandoObject();
                var dictionary = x.RealDictionary!;
                var keys = dictionary.Keys;
                foreach (var key in keys)
                {
                    (result as IDictionary<string, object?>)[key] = ExportToExpandoObject(dictionary[key]);
                }
                return result;
            }
            return x.RealData;
        }
    }
}