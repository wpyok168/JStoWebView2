using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        WebView2 wv=new WebView2();
        private async void Form1_Load(object sender, EventArgs e)
        {
            //https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.web.webview2.core.corewebview2.addhostobjecttoscript?view=webview2-dotnet-1.0.1418.22

            this.Controls.Add(wv);
            this.wv.Dock = DockStyle.Fill;
            await this.wv.EnsureCoreWebView2Async();

            //注册winning脚本c#互操作  注意注册的名称winning要和前端window.chrome.webview.hostObjects.winning;保持一致
            this.wv.CoreWebView2.AddHostObjectToScript("winning", new CustomWebView2HostObject());
            //注册全局变量winning
            //await this.wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("var hostobject =window.chrome.webview.hostObjects.winning;");
            this.wv.CoreWebView2.AddHostObjectToScript("bridge", new Bridge());


            this.wv.CoreWebView2.Navigate("https://visualsupport.microsoft.com/s5lTRSI/activate/result");
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

        }
    }
}
