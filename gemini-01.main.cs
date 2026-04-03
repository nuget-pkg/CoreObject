//css_nuget CS-Script
//css_nuget EasyObject
using CSScripting;
using CSScriptLib;
using System;
using System.Linq;
using static Global.EasyObject;
try
{
    UseAnsiConsole = true;
    // 実行したいコード (//css_nuget を含める)
    string code = @"
        //css_nuget Newtonsoft.Json
        using System;
        using Newtonsoft.Json;
        public class Script
        {
            public void Run()
            {
                var data = new { Message = ""Hello from NuGet!"", Time = DateTime.Now };
                Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
            }
        }";
    var script = CSScript.Evaluator
                     //.ReferenceAssemblyByName("System.Runtime")
                     //.ReferenceAssembliesFromCode(code)
                     ;
    CSScript.Evaluator.With(static eval =>
    {
        eval.IsCachingEnabled = false;
    });
    var assembly = script.CompileMethod(code);
    Log(assembly != null);
    ExpectTrue(assembly != null, "(assemmbly != null)");
    var classes = assembly!.GetExportedTypes()
                          .Where(t => t.IsClass);
    foreach (var type in classes)
    {
        Console.WriteLine($"Found exported class: {type.FullName}");
    }
    var scriptType = assembly.GetType("DynamicClass+Script");
    ExpectTrue(scriptType != null, "(typpe != null)");
    var wellKnownMethods = new[] { "ToString", "Equals", "GetHashCode", "GetType" };
    scriptType!.GetMethods().ForEach(m => {
        if (!wellKnownMethods.Contains(m.Name))
            Log($"User-defined method found: {m.Name}");
        });
    ExpectTrue(scriptType.GetMethod("Run") != null, "(scriptType.GetMethod(\"Run\") != null)");
    //scriptType.GetMethod("Run")!.Invoke(Activator.CreateInstance(scriptType), null);
    dynamic scriptObj = Activator.CreateInstance(scriptType);
    scriptObj.Run(); // 実行時にバインドされる
}
catch (Exception ex)
{
    Abort(ex);
}
