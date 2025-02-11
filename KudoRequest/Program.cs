using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using KudoRequest.Strategy;

class Program
{
    static async Task Main()
    {
        try
        {
            Console.WriteLine($"WebJobへの実行リクエストを開始します。");
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // 実行ディレクトリを指定
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // JSON 設定を読み込む
                .Build();
           
            // 1. Azure CLI を使用して Bearer トークンを取得

            Console.WriteLine($"アクセストークンを取得します。");
            var token = await GetAzureAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("トークンの取得に失敗しました。");
                return;
            }

            Console.WriteLine($"取得したトークン: {token.Substring(0, 20)}...");

            var webJob = config["KuduSettings:WebJobType"];
            Console.WriteLine($"WebJob {webJob} を実行します。");

            var webjobStrategy = WebJobStrategyFactory.GetStrategy(webJob!);
            var param = webjobStrategy.SetParameter();
            Console.WriteLine($"\n実行するパラメータ: {param}");
            Console.WriteLine("この内容で WebJob を実行しますか？ (yes/no)");

            var confirmation = Console.ReadLine()?.Trim().ToLower();
            if (confirmation == "yes")
            {
                // 2. Kudu API の URL 設定
                var WebJobUrl = config["KuduSettings:WebJobUrl"];

                var kuduUrl = $"{WebJobUrl}?arguments={param}";
                // 3. WebJob を実行
                await ExecuteWebJob(kuduUrl!, token);
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
    }

    // Azure CLI を実行して Bearer トークンを取得
    static async Task<string> GetAzureAccessToken()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c az account get-access-token --resource https://management.azure.com --query accessToken -o tsv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"トークン取得エラー: {error}");
                    return string.Empty;
                }

                return output.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"トークン取得中にエラー発生: {ex.Message}");
            return string.Empty;
        }
    }

    // Kudu API を使用して WebJob を実行
    static async Task ExecuteWebJob(string kuduUrl, string token)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = await client.PostAsync(kuduUrl, null);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"WebJob 実行リクエストに成功しました。: {response.StatusCode}");
                Console.WriteLine($"AzurePortal上でログを確認してください。");
            }
            else
            {
                Console.WriteLine($"WebJob 実行リクエスト失敗しました。: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}
