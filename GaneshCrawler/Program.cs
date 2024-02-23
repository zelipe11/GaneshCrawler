using GaneshCrawler;
using Dapper;
using HtmlAgilityPack;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;
using System.Net;
using System.IO;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

Console.WriteLine("Iniciando crawler...");

var web = new HtmlWeb
{
UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36"
};

// Criar um contêiner de cookies para manter a sessão
var cookieContainer = new CookieContainer();
web.PreRequest += request =>
{
if (request is HttpWebRequest httpRequest)
{
httpRequest.CookieContainer = cookieContainer;
}
return true;
};

using (var httpClient = new HttpClient(new HttpClientHandler { CookieContainer = cookieContainer }))
{

    
    // Agora, você pode realizar solicitações subsequentes usando HttpClient
    var loginUrl = "http://ganesh.easyorder.com.br/login";
    var username = "topodaniel78@gmail.com";
    var password = "Daniel123@";
    var token = "";

    var loginPageDoc = web.Load(loginUrl);
    var tokenNode = loginPageDoc.DocumentNode.SelectSingleNode("//input[@name='_token']");
    if (tokenNode != null)
    {
        token = tokenNode.GetAttributeValue("value", "");
    }

    var loginData = new Dictionary<string, string>
    {
        {"_token", token},
        {"email", username},
        {"password", password}
    };

    var loginResponse = httpClient.PostAsync(loginUrl, new FormUrlEncodedContent(loginData)).Result;


    if (loginResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("Login bem-sucedido...");

        var pageSize = 117;

        using (StreamWriter writer = new StreamWriter("output.txt"))
        {
            for (int currentPage = 1; currentPage <= pageSize; currentPage++)
            {
                var pageUrl = $"http://ganesh.easyorder.com.br/clientes?page={currentPage}";
                var doc = web.Load(pageUrl);
                Console.WriteLine($"Página {currentPage} obtida...");

                var categoryTable = doc.DocumentNode.SelectNodes("//table//tbody//tr");

                if (categoryTable != null)
                {
                    foreach (HtmlNode row in categoryTable)
                    {
                        string name = row.SelectSingleNode("td[1]").InnerText.Trim();
                        string cnpj = row.SelectSingleNode("td[2]").InnerText.Trim();
                        string email = row.SelectSingleNode("td[3]").InnerText.Trim();

                        writer.WriteLine($"{name}, {cnpj}, {email}");

                        Thread.Sleep(500);
                    }
                }

                Thread.Sleep(10000);
                Console.WriteLine($"Data for page {currentPage} appended to: {Path.GetFullPath("output.txt")}");
            }
        }
    }
    else
    {
        Console.WriteLine($"Falha no login. Código de status: {loginResponse.StatusCode}");
    }

}
