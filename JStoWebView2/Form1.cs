using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using JStoWebView2.Properties;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace JStoWebView2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        WebView2 wv = new WebView2();
        private async void Form1_Load(object sender, EventArgs e)
        {
            //https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.web.webview2.core.corewebview2.addhostobjecttoscript?view=webview2-dotnet-1.0.1418.22
            this.richTextBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            this.Controls.Add(wv);
            this.wv.Dock = DockStyle.Fill;

            //微软的cookie存储到本地
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string userDataFolder = Path.Combine(baseDir, "WebView2Profile", "MSLogin");
            //多账号登录存储，一账号一目录
            //string profile = Path.Combine(baseDir, "WebView2Profile",$"MSLogin_{accountId}");

            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: userDataFolder
            );
            await this.wv.EnsureCoreWebView2Async(env);

            //await this.wv.EnsureCoreWebView2Async();
            this.wv.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All); //监控每个请求

            //注册winning脚本c#互操作  注意注册的名称winning要和前端window.chrome.webview.hostObjects.winning;保持一致
            this.wv.CoreWebView2.AddHostObjectToScript("winning", new CustomWebView2HostObject());
            //注册全局变量winning
            //await this.wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("var hostobject =window.chrome.webview.hostObjects.winning;");
            this.wv.CoreWebView2.AddHostObjectToScript("bridge", new Bridge());


            this.wv.CoreWebView2.Navigate("https://visualsupport.microsoft.com/");//https://visualsupport.microsoft.com/s5lTRSI/activate/result
            //this.wv.Source = new Uri("https://www.baidu.com");

            /* 前端控制台调用——注意使用异步调用
               async function jscallcshart() {
                  var winning =window.chrome.webview.hostObjects.winning;
                  var result = await winning.TestCalcAddByCsharpMethod(10, 13, "加法计算");
                  console.log(result);
                }
                jscallcshart();
             *
             *
             */
            this.wv.NavigationCompleted += Wv_NavigationCompleted;
            this.wv.NavigationStarting += Wv_NavigationStarting;
            this.wv.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            this.wv.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            this.wv.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        }

        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            string requestUrl = e.Request.Uri;
            Console.WriteLine("监控到的每一个请求：" + requestUrl); //https://login.microsoftonline.com/common/oauth2/v2.0/token
            if (requestUrl.Contains("/token"))
            {
                //没用，应该用WebResourceResponseReceived事件
                if (e.Response != null)
                {
                    flag = true;
                    // 获取响应的内容
                    var contentStream = e.Response.Content;

                    // 这里可以对 contentStream 进行读取操作，转换成字符串或其他格式
                    StreamReader reader = new StreamReader(contentStream);
                    string responseBody = reader.ReadToEnd();

                    // 输出响应数据
                    Console.WriteLine(responseBody);
                }
                else
                {
                    flag = false;
                }
            }
        }
        private bool flag = false;
        private bool flag1 = false;

        private async void Wv_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Console.WriteLine("NavigationStarting:   " + e.Uri.ToString());
            if (e.Uri.ToString().Equals("https://login.microsoftonline.com/common/GetCredentialType?"))
            {
                //await this.wv.ExecuteScriptAsync(Resources.GetToken);
                List<CoreWebView2Cookie> cw2c = await this.wv.CoreWebView2.CookieManager.GetCookiesAsync("https://login.microsoftonline.com/");
                if (cw2c != null)
                {
                    string cookiestr1 = string.Join(";", cw2c.Select(c => $"{c.Name}={c.Value}"));
                }
            }
            else if (e.Uri.ToString().Contains("token"))
            {

            }
            if (e.Uri.StartsWith("https://visualsupport.microsoft.com/", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(e.Uri);

                var query = System.Web.HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#'));

                string code = query["code"];
                string state = query["state"];

                if (!string.IsNullOrEmpty(code) && flag1 )
                {
                    e.Cancel = true; // 阻止继续跳转
                                     // 👉 保存 code，准备换 token


                    //var result = await ExchangeCodeForTokenAsync(code, codeVerifier);

                    await ExchangeCodeByJsFetch(code);
                    flag1 = false;
                }
            }
        }

        
        string cookiestr1 = string.Empty;
        private async void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            if (e.Request.Uri.Contains("https://login.microsoftonline.com/"))//https://login.microsoftonline.com/common/GetCredentialType?mkt=zh-CN  https://login.microsoftonline.com/common/oauth2/v2.0/authorize?
            {
                List<CoreWebView2Cookie> cw2c = await this.wv.CoreWebView2.CookieManager.GetCookiesAsync("https://login.microsoftonline.com/");
                if (cw2c != null)
                {
                    cookiestr1 = string.Join(";", cw2c.Select(c => $"{c.Name}={c.Value}"));
                    Console.WriteLine(cookiestr1);
                }
            }
            if (e.Request.Uri.ToString().Contains("/token"))
            {
                if (e.Response.StatusCode == 200)
                {
                    try
                    {
                        var result = await e.Response.GetContentAsync();
                        if (result == null) return;
                        StreamReader reader = new StreamReader(result);
                        string responseBody = reader.ReadToEnd();

                        // 输出响应数据
                        Console.WriteLine(responseBody);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }

        }

        private async void Wv_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            List<CoreWebView2Cookie> cookiestr = await this.wv.CoreWebView2.CookieManager.GetCookiesAsync("https://visualsupport.microsoft.com");
            Console.WriteLine(this.wv.CoreWebView2.Source);
            string url = this.wv.CoreWebView2.Source;
            if (url.Contains("https://login.microsoftonline.com/common/GetCredentialType?"))//https://login.microsoftonline.com/common/oauth2/v2.0/authorize?
            {
                List<CoreWebView2Cookie> cw2c = await this.wv.CoreWebView2.CookieManager.GetCookiesAsync("https://login.microsoftonline.com/");
                if (cw2c != null)
                {
                    string cookiestr1 = string.Join(";", cw2c.Select(c => $"{c.Name}={c.Value}"));
                }
            }

        }
        string codeVerifier = string.Empty;
        string codeChallenge = string.Empty;
        private void button1_Click(object sender, EventArgs e)
        {
            //await this.wv.ExecuteScriptAsync(this.richTextBox1.Text);
            codeVerifier = string.Empty;
            codeChallenge = string.Empty;
            CreateChallengeNcodeVerifie(out codeVerifier, out codeChallenge);
            string accouttid = "petterwang@gad483.onmicrosoft.com";
            flag1 = true;
            string authorizeUrl =
                    "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
                    "client_id=2b217cec-607d-4eb6-887e-c928520a14f6" +
                    "&response_type=code" +
                    "&response_mode=fragment" +
                    "&scope=openid%20profile%20offline_access" +
                    "&redirect_uri=https%3A%2F%2Fvisualsupport.microsoft.com%2F" +
                    "&code_challenge=" + codeChallenge +
                    "&code_challenge_method=S256" +
                    "&login_hint=" + accouttid;
            this.wv.CoreWebView2.Navigate(authorizeUrl);
        }

        async Task<string> ExchangeCodeForTokenAsync(string code, string codeVerifier )
        {
            var client = new HttpClient();

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = "2b217cec-607d-4eb6-887e-c928520a14f6",
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = "https://visualsupport.microsoft.com/",
                ["code_verifier"] = codeVerifier
            });

            var response = await client.PostAsync(
                "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                content
            );

            string json = await response.Content.ReadAsStringAsync();

            return json; // 里面就是 access_token / refresh_token
        }


        private async System.Threading.Tasks.Task ExchangeCodeByJsFetch(string code)
        {
                        string js = $@"
            async function refrestoken () {{
                const data = new URLSearchParams({{
                    client_id: '2b217cec-607d-4eb6-887e-c928520a14f6',
                    grant_type: 'authorization_code',
                    code: '{code}',
                    redirect_uri: 'https://visualsupport.microsoft.com/',
                    code_verifier: '{codeVerifier}'
                }});

                const resp = await fetch(
                    'https://login.microsoftonline.com/common/oauth2/v2.0/token',
                    {{
                        method: 'POST',
                        headers: {{
                            'Content-Type': 'application/x-www-form-urlencoded'
                        }},
                        body: data.toString(),
                        //credentials: 'include'
                    }}
                );

                const json = await resp.json();
                console.log(JSON.stringify(json));
                
                window.chrome.webview.postMessage(JSON.stringify(json));

                return JSON.stringify(json);
            }};
            refrestoken();
            ";

            await this.wv.CoreWebView2.ExecuteScriptAsync(js);
        }

        #region

        private void CreateChallengeNcodeVerifie(out string codeVerifier, out string codeChallenge)
        {
            codeVerifier = GenerateCodeVerifier();
            codeChallenge = GenerateCodeChallenge(codeVerifier);
        }
        static string GenerateCodeVerifier()
        {
            //var bytes = RandomNumberGenerator.GetBytes(32);
            byte[] bytes = new byte[32];
            //RandomNumberGenerator.Fill(bytes);
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Base64UrlEncode(bytes);
        }
        static string GenerateCodeChallenge(string verifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
                return Base64UrlEncode(bytes);
            }
        }
        static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }


        #endregion

        private async void button2_Click(object sender, EventArgs e)
        {
            string script = @"
    async function getcid() {
        async function jscallcshart() {
            var result = 10 + 3;
            console.log(result);
            return result;
        }
        
        var cid = await jscallcshart();
        // 发送回 C#
        window.chrome.webview.postMessage(cid.toString());
        return cid;
    }
    
    getcid();
";

            var result = await this.wv.CoreWebView2.ExecuteScriptAsync(script);
            var res = await this.wv.ExecuteScriptAsync(this.richTextBox1.Text);
            //WebMessageReceived 接收返回值
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            //var json = JsonDocument.Parse(e.WebMessageAsJson);
            //OnTokenReceived?.Invoke(json.RootElement);
            //var result = e.WebMessageAsJson;
            var message = e.TryGetWebMessageAsString();
            Console.WriteLine($"Received from JavaScript: {message}");
        }
    }
}
