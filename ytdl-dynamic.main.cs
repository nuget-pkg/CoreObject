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
    string code = """
//css_nuget YoutubeExplode
//css_nuget YoutubeExplode.Converter
using System;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Converter;

//new Script().Run();

public class Script
{
    public void Run()
    {
        try
        {
            var youtube = new YoutubeClient();
            //var videoUrl = "https://www.youtube.com/watch?v=gkdpDAhRsDk";
            var videoUrl = "https://www.youtube.com/watch?v=wzcdhDyNmMM";
            async Task<YoutubeExplode.Videos.Video> Getter(string videoUrl)
            {
                return await youtube.Videos.GetAsync(videoUrl);
            }
            var videoAsync = Getter(videoUrl);
            videoAsync.Wait();
            // 1. まず動画の詳細情報（メタデータ）を取得
            var video = videoAsync.Result; //await youtube.Videos.GetAsync(videoUrl);
            var filePath = $"{video.Title}.mkv";
            Console.WriteLine($"filePath: {filePath}");
            // 進捗を表示するためのハンドラを作成
            var progressHandler = new Progress<double>(p =>
            {
                // p は 0.0 ～ 1.0 の値（パーセンテージ）
                Console.Write($"\rダウンロード中... {p:P1} ");
            });
            // DownloadAsync の引数に進捗ハンドラを追加
            async Task Downloader(string videoUrl, string filePath)
            {
                await youtube.Videos.DownloadAsync(videoUrl, filePath, builder => builder
                    .SetContainer("matroska")
                    .SetPreset(ConversionPreset.VeryFast),
                    progressHandler // ここに進捗オブジェクトを渡す
                );
            }
            var downloadAsync = Downloader(videoUrl, filePath);
            downloadAsync.Wait();
            Console.WriteLine("\n完了！");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
        }
    }
}
""";
    var script = CSScript.Evaluator
                     .ReferenceAssemblyByName("System.Threading.Tasks.Extensions")
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
    scriptType.GetMethod("Run")!.Invoke(Activator.CreateInstance(scriptType), null);
}
catch (Exception ex)
{
    Abort(ex);
}
