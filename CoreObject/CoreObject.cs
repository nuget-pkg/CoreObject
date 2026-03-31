namespace Core;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable once CheckNamespace
using Global;
public enum CoreObjectType {
    // ReSharper disable once InconsistentNaming
    @string,
    // ReSharper disable once InconsistentNaming
    number,
    // ReSharper disable once InconsistentNaming
    boolean,
    // ReSharper disable once InconsistentNaming
    @object,
    // ReSharper disable once InconsistentNaming
    array,
    // ReSharper disable once InconsistentNaming
    @null
}
internal class CoreObjectConverter : IConvertParsedResult {
    public object? ConvertParsedResult(object? x, string origTypeName) {
        if (x is Dictionary<string, object>) {
            var dict = x as Dictionary<string, object>;
            var keys = dict!.Keys;
            var result = new Dictionary<string, CoreObject>();
            foreach (var key in keys) {
                var eo = new CoreObject();
                eo.RealData = dict[key];
                result[key] = eo;
            }
            return result;
        }
        if (x is List<object>) {
            var list = x as List<object>;
            var result = new List<CoreObject>();
            foreach (var e in list!) {
                var eo = new CoreObject();
                eo.RealData = e;
                result.Add(eo);
            }
            return result;
        }
        return x;
    }
}
public class CoreObject :
    DynamicObject,
    IExposeInternalObject,
    IExportToPlainObject,
    IImportFromPlainObject,
    IExportToCommonJson,
    IImportFromCommonJson {
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly IParseJson DefaultJsonParser = new Core.JsoncHandler(numberAsDecimal: true);
    public static IParseJson? JsonParser /*= null*/;
    // ReSharper disable once MemberCanBePrivate.Global
    public static bool DebugOutput /*= false*/;
    public static bool ShowDetail /*= false*/;
    // ReSharper disable once MemberCanBePrivate.Global
    public static bool ForceAscii /*= false*/;
    public object? RealData /*= null*/;
    static CoreObject() {
        ClearSettings();
    }
    public CoreObject() {
        RealData = null;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public CoreObject(object? x) {
        RealData = new ObjectConverter(JsonParser, false, new CoreObjectConverter()).Parse(x, true);
    }
    public dynamic Dynamic => this;
    public static CoreObject Null => new();
    public static CoreObject EmptyArray => new(new List<CoreObject>());
    public static CoreObject EmptyObject => new(new Dictionary<string, CoreObject>());

    // ReSharper disable once InconsistentNaming
    public static CoreObjectType @string => CoreObjectType.@string;
    // ReSharper disable once InconsistentNaming
    public static CoreObjectType boolean => CoreObjectType.boolean;
    // ReSharper disable once InconsistentNaming
    public static CoreObjectType @object => CoreObjectType.@object;
    // ReSharper disable once InconsistentNaming
    public static CoreObjectType array => CoreObjectType.array;
    // ReSharper disable once InconsistentNaming
    public static CoreObjectType @null => CoreObjectType.@null;
    public bool IsString => TypeValue == CoreObjectType.@string;
    public bool IsNumber => TypeValue == CoreObjectType.number;
    public bool IsBoolean => TypeValue == CoreObjectType.boolean;
    public bool IsObject => TypeValue == CoreObjectType.@object;
    public bool IsArray => TypeValue == CoreObjectType.array;
    public bool IsNull => TypeValue == CoreObjectType.@null;
    public CoreObjectType TypeValue {
        get {
            var obj = ExposeInternalObjectHelper(this);
            if (obj == null) return CoreObjectType.@null;
            switch (Type.GetTypeCode(obj.GetType())) {
                case TypeCode.Boolean:
                    return CoreObjectType.boolean;
                case TypeCode.String:
                case TypeCode.Char:
                case TypeCode.DateTime:
                    return CoreObjectType.@string;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return CoreObjectType.number;
                case TypeCode.Object:
                    return obj is List<CoreObject> ? CoreObjectType.array : CoreObjectType.@object;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                default:
                    if (obj is TimeSpan || obj is Guid) return @string;
                    return CoreObjectType.@null;
            }
        }
    }
    public string TypeName => TypeValue.ToString();

    // ReSharper disable once InconsistentNaming
    public List<CoreObject>? RealList => RealData as List<CoreObject>;

    // ReSharper disable once InconsistentNaming
    public Dictionary<string, CoreObject>? RealDictionary => RealData as Dictionary<string, CoreObject>;
    public int Count {
        get {
            if (RealList != null) return RealList.Count;
            if (RealDictionary != null) return RealDictionary.Count;
            return 0;
        }
    }
    public List<string> Keys {
        get {
            var keys = new List<string>();
            if (RealDictionary == null) return keys;
            foreach (var key in RealDictionary.Keys) keys.Add(key);
            return keys;
        }
    }
    public CoreObject this[string name] {
        get {
            if (RealList != null) return TryAssoc(name);
            if (RealDictionary == null) return Null;
            CoreObject? eo;
            RealDictionary.TryGetValue(name, out eo);
            if (eo == null) return Null;
            return eo;
        }
        set {
            if (RealDictionary == null) RealData = new Dictionary<string, CoreObject>();
            RealDictionary![name] = value;
        }
    }
    public CoreObject this[int pos] {
        get {
            if (RealList == null) return WrapInternal(null);
            if (RealList.Count < pos + 1) return WrapInternal(null);
            return WrapInternal(RealList[pos]);
        }
        set {
            if (pos < 0) throw new ArgumentException("index below 0");
            if (RealList == null) RealData = new List<CoreObject>();
            while (RealList!.Count < pos + 1) RealList.Add(Null);
            RealList[pos] = value;
        }
    }
    public List<CoreObject>? AsList {
        get {
            // ReSharper disable once ArrangeAccessorOwnerBody
            return RealList;
        }
    }
    public Dictionary<string, CoreObject>? AsDictionary {
        get {
            // ReSharper disable once ArrangeAccessorOwnerBody
            return RealDictionary;
        }
    }
    public string[] AsStringArray {
        get {
            if (RealList != null)
                return
                    RealList!
                        .Select(i =>
                            i.IsString ? i.Cast<string>() : i.ToJson(keyAsSymbol: true, indent: false))
                        .ToArray();
            if (RealDictionary != null) return RealDictionary.Keys.Select(i => i).ToArray();
            return [];
        }
    }
    public List<string> AsStringList => AsStringArray.ToList();
    public string ExportToCommonJson() {
        return ToJson(
            true
        );
    }
    public object? ExportToPlainObject() {
        return new ObjectConverter(null, ForceAscii).Parse(RealData);
    }
    public object? ExposeInternalObject() {
        return ExposeInternalObjectHelper(this);
    }
    public void ImportFromCommonJson(string x) {
        var eo = FromJson(x);
        RealData = eo.RealData;
    }
    public void ImportFromPlainObject(object? x) {
        var eo = FromObject(x);
        RealData = eo.RealData;
    }
    public static void ClearSettings() {
        JsonParser = DefaultJsonParser;
        DebugOutput = false;
        ShowDetail = false;
        ForceAscii = false;
    }
    public static void SetupConsoleEncoding(Encoding? encoding = null) {
        if (encoding == null) encoding = Encoding.UTF8;
        try {
            Console.OutputEncoding = encoding;
            Console.InputEncoding = encoding;
            Console.SetError(
                new StreamWriter(
                    Console.OpenStandardError(), encoding) {
                    AutoFlush = true
                });
        }
        catch (Exception) {
            // Ignore exceptions related to console encoding
        }
    }
    public override string ToString() {
        return ToPrintable();
    }
    public string ToPrintable(bool compact = false) {
        return ToPrintable(this, compact: compact);
    }
    public static CoreObject NewArray(params object[] args) {
        var result = EmptyArray;
        for (var i = 0; i < args.Length; i++) result.Add(FromObject(args[i]));
        return result;
    }
    public static CoreObject NewObject(params object[] args) {
        if (args.Length % 2 != 0)
            throw new ArgumentException("EasyObjectClassic.NewObject() requires even number arguments");
        var result = EmptyObject;
        for (var i = 0; i < args.Length; i += 2) result.Add(args[i].ToString()!, FromObject(args[i + 1]));
        return result;
    }
    private static object? ExposeInternalObjectHelper(object? x) {
        while (x is CoreObject) x = ((CoreObject)x).RealData;
        return x;
    }
    private static CoreObject WrapInternal(object? x) {
        if (x is CoreObject) return (x as CoreObject)!;
        return new CoreObject(x);
    }
    public bool ContainsKey(string name) {
        if (RealDictionary == null) return false;
        return RealDictionary.ContainsKey(name);
    }
    public CoreObject Add(object x) {
        if (RealList == null) RealData = new List<CoreObject>();
        var eo = x is CoreObject ? (x as CoreObject)! : new CoreObject(x);
        RealList!.Add(eo);
        return this;
    }
    public CoreObject Add(string key, object? x) {
        if (RealDictionary == null) RealData = new Dictionary<string, CoreObject>();
        var eo = x is CoreObject ? (x as CoreObject)! : new CoreObject(x);
        RealDictionary!.Add(key, eo);
        return this;
    }
    public override bool TryGetMember(
        GetMemberBinder binder, out object result) {
        result = Null;
        var name = binder.Name;
        if (RealList != null) {
            var assoc = TryAssoc(name);
            result = assoc;
        }
        if (RealDictionary == null) return true;
        CoreObject? eo;
        RealDictionary.TryGetValue(name, out eo);
        if (eo == null) eo = Null;
        result = eo;
        return true;
    }
    public override bool TrySetMember(
        SetMemberBinder binder, object? value) {
        value = ExposeInternalObjectHelper(value);
        if (RealDictionary == null) RealData = new Dictionary<string, CoreObject>();
        var name = binder.Name;
        RealDictionary![name] = WrapInternal(value);
        return true;
    }
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
        result = Null;
        var idx = indexes[0];
        if (idx is int) {
            var pos = (int)indexes[0];
            if (RealList == null) {
                result = WrapInternal(null);
                return true;
            }
            if (RealList.Count < pos + 1) {
                result = WrapInternal(null);
                return true;
            }
            result = WrapInternal(RealList[pos]);
            return true;
        }
        if (RealList != null) {
            var assoc = TryAssoc((string)idx);
            result = assoc;
        }
        if (RealDictionary == null) {
            result = Null;
            return true;
        }
        CoreObject? eo /*= Null*/;
        RealDictionary.TryGetValue((string)idx, out eo);
        if (eo == null) eo = Null;
        result = eo;
        return true;
    }
    public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value) {
        if (value is CoreObject) value = ((CoreObject)value).RealData;
        var idx = indexes[0];
        if (idx is int) {
            var pos = (int)indexes[0];
            if (pos < 0) throw new ArgumentException("index is below 0");
            if (RealList == null) RealData = new List<CoreObject>();
            while (RealList!.Count < pos + 1) RealList.Add(Null);
            RealList[pos] = WrapInternal(value);
            return true;
        }
        if (RealDictionary == null) RealData = new Dictionary<string, CoreObject>();
        var name = (string)indexes[0];
        RealDictionary![name] = WrapInternal(value);
        return true;
    }
    public override bool TryConvert(ConvertBinder binder, out object? result) {
        if (binder.Type == typeof(IEnumerable)) {
            if (RealList != null) {
                var ie1 = RealList.Select(x => x);
                result = ie1;
                return true;
            }
            if (RealDictionary != null) {
                var ie2 = RealDictionary.Select(x => x);
                result = ie2;
                return true;
            }
            result = new List<CoreObject>().Select(x => x);
            return true;
        }
        result = Convert.ChangeType(RealData, binder.Type);
        return true;
    }
    private static string[] TextToLines(string text) {
        var lines = new List<string>();
        using (var sr = new StringReader(text)) {
            string? line;
            while ((line = sr.ReadLine()) != null) lines.Add(line);
        }
        return lines.ToArray();
    }
    public static CoreObject FromObject(object? obj, bool ignoreErrors = false) {
        if (!ignoreErrors) return new CoreObject(obj);
        try {
            return new CoreObject(obj);
        }
        catch (Exception) {
            return new CoreObject(null);
        }
    }
    public static CoreObject FromJson(string? json, bool ignoreErrors = false) {
        if (json == null) return Null;
        if (json.StartsWith("#!")) {
            var lines = TextToLines(json);
            lines = lines.Skip(1).ToArray();
            json = string.Join("\n", lines);
        }
        if (!ignoreErrors) return new CoreObject(JsonParser!.ParseJson(json));
        try {
            return new CoreObject(JsonParser!.ParseJson(json));
        }
        catch (Exception) {
            return new CoreObject(null);
        }
    }
    public dynamic? ToObject(bool asDynamicObject = false) {
        if (asDynamicObject) return ExportToDynamicObject();
        return ExportToPlainObject();
    }
    public string ToJson(bool indent = false, bool sortKeys = false, bool keyAsSymbol = false) {
        var poc = new ObjectConverter(JsonParser, ForceAscii);
        return poc.Stringify(RealData, indent, sortKeys, keyAsSymbol);
    }
    public static string ToPrintable(object? x, string? title = null, bool compact = false) {
        var poc = new ObjectConverter(JsonParser, ForceAscii);
        return poc.ToPrintable(ShowDetail, x, title, compact);
    }
    public static void Echo(
        object? x,
        string? title = null,
        bool compact = false,
        uint maxDepth = 0,
        List<string>? hideKeys = null
    ) {
        hideKeys ??= new List<string>();
        if (maxDepth > 0 || hideKeys.Count > 0) {
            var eo = FromObject(x);
            x = eo.Clone(
                maxDepth,
                hideKeys,
                false);
        }
        var s = ToPrintable(x, title, compact);
        Console.WriteLine(s);
        System.Diagnostics.Debug.WriteLine(s);
    }
    public static void Log(
        object? x,
        string? title = null,
        bool compact = false,
        uint maxDepth = 0,
        List<string>? hideKeys = null
    ) {
        hideKeys ??= new List<string>();
        if (maxDepth > 0 || hideKeys.Count > 0) {
            var eo = FromObject(x);
            x = eo.Clone(
                maxDepth,
                hideKeys,
                false);
        }
        var s = ToPrintable(x, title, compact);
        Console.Error.WriteLine("[Log] " + s);
        System.Diagnostics.Debug.WriteLine("[Log] " + s);
    }
    public static void Debug(
        object? x,
        string? title = null,
        bool compact = false,
        uint maxDepth = 0,
        List<string>? hideKeys = null
    ) {
        if (!DebugOutput) return;
        hideKeys ??= new List<string>();
        if (maxDepth > 0 || hideKeys.Count > 0) {
            var eo = FromObject(x);
            x = eo.Clone(
                maxDepth,
                hideKeys,
                false);
        }
        var s = ToPrintable(x, title, compact);
        Console.Error.WriteLine("[Debug] " + s);
        System.Diagnostics.Debug.WriteLine("[Debug] " + s);
    }
    public static void Message(
        object? x,
        string? title = null,
        bool compact = false,
        uint maxDepth = 0,
        List<string>? hideKeys = null
    ) {
        if (title == null) title = "Message";
        var s = ToPrintable(x, title, compact);
        NativeMethods.MessageBoxW(IntPtr.Zero, s, title, 0);
    }
    private CoreObject TryAssoc(string name) {
        try {
            if (RealList == null) return Null;
            for (var i = 0; i < RealList.Count; i++) {
                var pair = RealList[i].AsList!;
                if (pair[0].Cast<string>() == name) return pair[1];
            }
            return Null;
        }
        catch (Exception /*e*/) {
            return Null;
        }
    }
    public T Cast<T>() {
        if (RealData is DateTime dt) {
            string? s = null;
            switch (dt.Kind) {
                case DateTimeKind.Local:
                    s = dt.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
                    break;
                case DateTimeKind.Utc:
                    s = dt.ToString("o");
                    break;
                case DateTimeKind.Unspecified:
                    s = dt.ToString("o").Replace("Z", "");
                    break;
            }
            return (T)Convert.ChangeType(s, typeof(T))!;
        }
        return (T)Convert.ChangeType(RealData, typeof(T))!;
    }
    public static string FullName(dynamic? x) {
        if (x is null) return "null";
        var fullName = ((object)x).GetType().FullName!;
        if (fullName.StartsWith("<>f__AnonymousType")) return "AnonymousType";
        return fullName.Split('`')[0];
    }
    public static implicit operator CoreObject(bool x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(string x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(char x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(short x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(int x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(long x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(ushort x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(uint x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(ulong x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(float x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(double x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(decimal x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(sbyte x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(byte x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(DateTime x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(TimeSpan x) {
        return new CoreObject(x);
    }
    public static implicit operator CoreObject(Guid x) {
        return new CoreObject(x);
    }
    public void Nullify() {
        RealData = null;
    }
    public void Trim(
        uint maxDepth = 0,
        List<string>? hideKeys = null
    ) {
        CoreObjectEditor.Trim(this, maxDepth, hideKeys);
    }
    public CoreObject Clone(
        uint maxDepth = 0,
        List<string>? hideKeys = null,
        bool always = true
    ) {
        return CoreObjectEditor.Clone(this, maxDepth, hideKeys, always);
    }
    public CoreObject? Shift() {
        if (RealList == null) return null;
        if (RealList.Count == 0) return null;
        var result = RealList[0];
        RealList.RemoveAt(0);
        return result;
    }
    public dynamic? ExportToDynamicObject() {
        return CoreObjectEditor.ExportToExpandoObject(this);
    }
    public static string ObjectToJson(object? x, bool indent = false) {
        return FromObject(x).ToJson(indent);
    }
    public static object? ObjectToObject(object? x, bool asDynamicObject = false) {
        return FromObject(x).ToObject(asDynamicObject);
    }
    private static class NativeMethods {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int MessageBoxW(
            IntPtr hWnd, string lpText, string lpCaption, uint uType);
    }
}