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
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // 実行ディレクトリを指定
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // JSON 設定を読み込む
                .Build();

            var webJobs = config.GetSection("KuduSettings:WebJobs").Get<List<WebJob>>();
            Console.WriteLine("実行する WebJob を選択してください:");
            foreach (var job in webJobs!)
            {
                Console.WriteLine($"{job.Id}: {job.Name}");
            }

            Console.Write("選択（番号）: ");
            string input = Console.ReadLine()!;

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
                Console.WriteLine($"実行するパラメータが設定されていません。\nパラメータ未設定の状態で実行することはできません。");
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
                Console.WriteLine("\nこの内容で WebJob を実行しますか？\nパラメーターURLに問題がないことを今一度ご確認ください。 (yes/no)");
                var confirmation2 = Console.ReadLine()?.Trim().ToLower();
                if (confirmation2 == "yes")
                {
                    // 3. WebJob を実行
                    await ExecuteWebJob(kuduUrl!, config["Token"]!);
                    
                }
                else
                {
                    Console.WriteLine("処理をキャンセルしました。");
                    return;
                }
            }
            else
            {
                Console.WriteLine("処理をキャンセルしました。");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー発生: {ex.Message}");
        }
        End();
    }

    static void End()
    {
        Console.WriteLine("処理が完了しました。終了するには `end` と入力してください...");
        while (true)
        {
            string? input = Console.ReadLine();
            if (input?.Trim().ToLower() == "end")
            {
                break;
            }
            Console.WriteLine("終了するには `end` と入力してください...");
        }
    }

    static async Task ExecuteWebJob(string kuduUrl, string token)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = await client.PostAsync(kuduUrl, null);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"\nWebJob 実行リクエストに成功しました。: {response.StatusCode}");
                Console.WriteLine($"\nAzurePortal上で実行ログを確認してください。\n（この機能は実行のリクエストを行うだけで、WebJobの処理が成功したわけではありません。）");
            }
            else
            {
                Console.WriteLine($"WebJob 実行リクエスト失敗しました。: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}
