using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using KudoRequest.Strategy;
using KudoRequest;

class Program
{
    static async Task Main()
    {
        try
        {
            Console.WriteLine($"WebJobへの実行リクエストを開始します。\n");

            // 設定ファイルの読み込み
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var webJobs = config.GetSection("KuduSettings:WebJobs").Get<List<WebJob>>();
            Console.WriteLine("実行する WebJob を選択してください:");
            foreach (var job in webJobs!)
            {
                Console.WriteLine($"{job.Id}: {job.Name}");
            }

            Console.Write("選択（番号）: ");
            var input = Console.ReadLine()!;

            if (!int.TryParse(input, out int selectedId))
            {
                Console.WriteLine("無効な入力です。終了します。");
                End();
                return;
            }

            // 選択した WebJob の `Name` を取得
            var selectedJob = webJobs.FirstOrDefault(j => j.Id == selectedId);
            if (selectedJob == null)
            {
                Console.WriteLine("選択された WebJob が見つかりません。");
                End();
                return;
            }

            var webjobStrategy = WebJobStrategyFactory.GetStrategy(selectedJob.Name, webJobs);
            var param = webjobStrategy.SetParameter();
            if (string.IsNullOrWhiteSpace(param))
            {
                Console.WriteLine("実行するパラメータが設定されていません。\nパラメータ未設定の状態で実行することはできません。");
                End();
                return;
            }

            Console.WriteLine($"実行するパラメータ: {param}");
            Console.WriteLine("このパラメーターで実行しますか？ (yes/no)");

            var confirmation = Console.ReadLine()?.Trim().ToLower();
            if (confirmation == "yes")
            {
                var kuduUrl = $"{selectedJob.WebJobUrl}?arguments={param}";
                Console.WriteLine("\nリクエストするURLに問題ないか確認してください。");
                Console.WriteLine(kuduUrl);
                Console.WriteLine("\nこの内容で WebJob を実行しますか？ (yes/no)");

                var confirmation2 = Console.ReadLine()?.Trim().ToLower();
                if (confirmation2 == "yes")
                {
                    // WebJob を実行し、ログを保存
                    await ExecuteWebJob(kuduUrl!, config["Token"]!, selectedJob.Name);
                }
                else
                {
                    Console.WriteLine("処理をキャンセルしました。");
                }
            }
            else
            {
                Console.WriteLine("処理をキャンセルしました。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー発生: {ex.Message}");
            SaveLog($"エラー発生: {ex.Message}");
        }
        End();
    }

    static void End()
    {
        Console.WriteLine("処理が完了しました。終了するには `end` と入力してください...");
        while (true)
        {
            var input = Console.ReadLine();
            if (input?.Trim().ToLower() == "end")
            {
                break;
            }
            Console.WriteLine("終了するには `end` と入力してください...");
        }
    }

    static async Task ExecuteWebJob(string kuduUrl, string token, string jobName)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = await client.PostAsync(kuduUrl, null);
            var logMessage = $"[{DateTime.Now}] WebJob: {jobName}\nURL: {kuduUrl}\n";

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"\nWebJob 実行リクエストに成功しました。: {response.StatusCode}");

                if (response.Headers.Contains("Location"))
                {
                    var runUrl = response.Headers.Location?.ToString() ?? "";
                    var runId = runUrl.Split('/').Last();

                    Console.WriteLine($"\nWebJob の実行履歴 URL: {runUrl}");
                    Console.WriteLine($"WebJob Run ID: {runId}");
                    Console.WriteLine($"Run ID を元にAzurePortalのWebJobダッシュボードで確認してください。");

                    logMessage += $"成功 (Run ID: {runId})\n履歴URL: {runUrl}\n";
                }
                else
                {
                    logMessage += "成功（実行履歴の URL なし）\n";
                }
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"WebJob 実行リクエスト失敗しました。: {response.StatusCode} - {errorMessage}");
                logMessage += $"失敗 ({response.StatusCode}) - {errorMessage}\n";
            }

            // ログをファイルに保存
            SaveLog(logMessage);
        }
    }

    static void SaveLog(string message)
    {
        var logDir = "logs";
        var logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");

        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        File.AppendAllText(logFile, message + "\n");
    }
}
