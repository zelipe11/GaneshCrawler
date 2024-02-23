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
    // Adicione o token XSRF diretamente ao contêiner de cookies
    cookieContainer.Add(new Uri("http://ganesh.easyorder.com.br/clientes"), new Cookie("XSRF-TOKEN", "eyJpdiI6IjJzWWt1M2VqRDg3ZklLTE5YUXRyY3c9PSIsInZhbHVlIjoiYTFLUC9acEtwTmxXSUVHT053SzVnWkhkd1NEdnkwM0g2RzRYSzA4RXhRUEpKQzNpNUFQRXQxa05tZktjd1lYeWNrb3NvcDR3OWlqQkhFd28yS1lxWGl4enNKSXBsL2tLelhjRmFqVVdUZG9BR21IeStEYVdna3BqL0VtaHpKUWYiLCJtYWMiOiIzY2FkYTY3ZGNhYjk3YjkyOTYzNDkzMjczZmNhOTJkZmY4ZmFmNzZiOGQxNzk2ZGUyNzZlYmY2ZTEyYzhmNWRlIn0%3D"));

    // Agora, você pode realizar solicitações subsequentes usando HttpClient
    var loginUrl = "http://ganesh.easyorder.com.br/login";
    var username = "topodaniel78@gmail.com";
    var password = "Daniel123@";

    var loginData = new Dictionary<string, string>
    {
        {"_token", "eyJpdiI6IjJzWWt1M2VqRDg3ZklLTE5YUXRyY3c9PSIsInZhbHVlIjoiYTFLUC9acEtwTmxXSUVHT053SzVnWkhkd1NEdnkwM0g2RzRYSzA4RXhRUEpKQzNpNUFQRXQxa05tZktjd1lYeWNrb3NvcDR3OWlqQkhFd28yS1lxWGl4enNKSXBsL2tLelhjRmFqVVdUZG9BR21IeStEYVdna3BqL0VtaHpKUWYiLCJtYWMiOiIzY2FkYTY3ZGNhYjk3YjkyOTYzNDkzMjczZmNhOTJkZmY4ZmFmNzZiOGQxNzk2ZGUyNzZlYmY2ZTEyYzhmNWRlIn0%3D"},
        {"email", username},
        {"password", password}
    };

    var loginResponse = httpClient.PostAsync(loginUrl, new FormUrlEncodedContent(loginData)).Result;


    if (loginResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Login bem-sucedido...");

                // Agora você pode prosseguir com a obtenção do documento após o login
                var doc = web.Load("http://ganesh.easyorder.com.br/clientes?page=1");
                Console.WriteLine("Home obtida...");

                var form = doc.DocumentNode.SelectSingleNode("//form[@id='form1']");
                var categoriesContainer = form?.SelectSingleNode(".//div[@id='table']//tbody");
                var categories = categoriesContainer?.SelectNodes(".//tr");

                Console.WriteLine("Categorias obtidas...");

                Console.WriteLine("Abrindo conexão com banco de dados...");

                var connection = new MySqlConnection("Server=localhost;Port=3306;DataBase=condo;Uid=root;Pwd=123456;");

                foreach (var category in categories)
                {
                    string hrefPattern = @"<a\s+(?:[^>]*?\s+)?href=(['""])(.*?)\1";
                    Match hrefMatch = Regex.Match(category.InnerHtml, hrefPattern);

                    string textPattern = @"<a[^>]*>(.*?)<\/a>";
                    Match textMatch = Regex.Match(category.InnerHtml, textPattern);

                    if (hrefMatch.Success && textMatch.Success)
                    {
                        string categoryLink = hrefMatch.Groups[2].Value;
                        string categoryName = textMatch.Groups[1].Value;

                        var categoryAlreadyExist = connection.ExecuteScalar<bool>("SELECT COUNT(1) FROM Category WHERE Name=@text", new { text = categoryName });

                        if (categoryAlreadyExist is false)
                        {
                            var sql = "INSERT INTO Category (Name, Url) VALUES (@name, @url)";
                            var newCategory = new Category { Name = categoryName, Url = categoryLink };

                            var rowsAffected = connection.Execute(sql, newCategory);

                            if (rowsAffected > 0)
                                Console.WriteLine($"{categoryName} - categoria inserida com sucesso no banco...");
                            else
                                Console.WriteLine($"erro ao inserir categoria {categoryName}...");

                            Thread.Sleep(1000);
                        }

                        Console.WriteLine($"Obtendo empresas da categoria {categoryName}...");

                        var pageSize = 117;

                        HtmlDocument categoryDoc = await web.LoadFromWebAsync($"http://ganesh.easyorder.com.br/{categoryName}?page={pageSize}", Encoding.GetEncoding("iso-8859-1"));
                        var categoryTable = categoryDoc.DocumentNode.SelectNodes("//table//tbody//tr");

                        if (categoryTable != null)
                        {
                            foreach (HtmlNode node in categoryTable)
                            {
                                string name = node.SelectSingleNode("td[1]").InnerText.Trim();
                                string cnpj = node.SelectSingleNode("td[2]").NextSibling.InnerText.Trim();
                                string email = node.SelectSingleNode("td[3]").NextSibling.InnerText.Trim();

                                var categoryId = connection.ExecuteScalar<int>("SELECT Id FROM Category WHERE Name=@text", new { text = categoryName });

                                var sql = "INSERT INTO Client (Name, CNPJ/CPF, Email) " +
                                          "VALUES (@name, @cnpj, @email)";

                                var client = new Client
                                {
                                    Name = name,
                                    Cnpjcpf = cnpj,
                                    Email = email
                                };

                                var rowsAffected = connection.Execute(sql, client);

                                if (rowsAffected > 0)
                                    Console.WriteLine($"{categoryName} - Empresa inserida com sucesso no banco...");
                                else
                                    Console.WriteLine($"erro ao inserir empresa {name}...");

                                Thread.Sleep(500);
                            }
                        }

                        Thread.Sleep(10000);
                    }
                }


    }            
    else
    {
        Console.WriteLine($"Falha no login. Código de status: {loginResponse.StatusCode}");
    }

}
