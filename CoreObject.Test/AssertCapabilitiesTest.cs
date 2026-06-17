using System;
using TO = Global.EasyObject;
using CO = Core.CoreObject;
using static Core.CoreObject;
// ReSharper disable CheckNamespace
namespace Core.CoreObjectTest;
internal class AssertCapabilitiesTest {
    [NUnit.Framework.SetUp]
    public void Setup() {
        TO.ClearSettings();
        TO.UseAnsiConsole = true;
        TO.Echo("abc", "def");
        TO.Log(TO.FullName(this));
    }
    [NUnit.Framework.Test]
    public void Test901()
    {
        TO.Pass();
        ShowDetail = true;
        CO eo = "abc";
        TO.Echo(eo, "eo");
        string s = eo.Dynamic;
        TO.Echo(s, "s");
        TO.AssertIdentical(s, "abc");
        eo.Dynamic.A = "AAA";
        TO.Echo(eo, "eo");
        TO.AssertIdentical(eo.TypeValue, @object);
        Console.WriteLine(eo);
        foreach (var e in eo.Dynamic)
        {
            TO.Echo(e, "e");
            TO.AssertIdentical(e.Key, "A");
            TO.AssertIdentical(e.Value.Cast<string>(), "AAA");
            string ss = e.Value.Dynamic;
            TO.AssertIdentical(ss, "AAA");
            TO.AssertIdentical((string)(e.Value.Dynamic), "AAA");
        }
        var list0 = TO.NewArray("A", "B", "C");
        var list1 = list0.AsStringArray;
        var list2 = list0.AsStringList;
        TO.AssertIdentical(list1, list2);
        TO.AssertEquivalent(list0, list1);
        TO.AssertIdentical(list1, new object[] { "A", "B", "C" });
        var dict0 = TO.NewObject("A", 11, "B", 22, "C", null);
        var dict1 = dict0.ToObject(asDynamicObject: false);
        var dict2 = dict0.ToObject(asDynamicObject: true);
        TO.AssertIdentical(dict1, dict2);
        TO.AssertEquivalent(dict0, dict1);
        TO.Log("pass-01");
        TO.AssertEquivalent(dict1, new { A = 11, B = 22, C = Null });
        TO.Log("pass-02");
        // /*⁅FAILS⁆*/ TO.AssertIdentical(dict1, new { A = 11, B = 22, C = Null });
        //Log("pass-03");
        TO.Pass();
    }
    [NUnit.Framework.Test]
    public void Test902()
    {
        TO.Pass();
        ShowDetail = true;
        DebugOutput = true;
        TO.Pass();
        CO eo = CO.NewArray(CO.NewArray(CO.NewObject("a", CO.NewObject("b", 222, "d", 111), "b", 20, "c", 30), 11, 22, 33), "a", "b", "c");
        TO.Pass();
        TO.Echo(eo, maxCount: 2, hideKeys: ["b"], title: "1");
        eo.Trim(maxCount: 2, hideKeys: ["b"], maxDepth: 3);
        TO.Pass();
        TO.AssertIdentical(actual: eo.ToJson(), expected: """[[{"a":{},"c":30},11],"a"]""");
        TO.Echo(eo);
        eo.Trim(maxCount: 2, hideKeys: ["b"], maxDepth: 2);
        TO.Pass();
        TO.AssertIdentical(actual: eo.ToJson(), expected: """[[{},11],"a"]""");
        eo.Trim(maxCount: 2, hideKeys: ["b"], maxDepth: 1);
        TO.Echo(eo);
        TO.Pass();
        TO.AssertIdentical(actual: eo.ToJson(), expected: """[[],"a"]""");
    }
}