using GaneshCrawler;
using Dapper;
using HtmlAgilityPack;
using MySqlConnector;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

Console.WriteLine("Iniciando crawler...");

HtmlWeb web = new()
{
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36"
};

HtmlDocument doc = await web.LoadFromWebAsync("http://ganesh.easyorder.com.br/clientes?page=1");
Console.WriteLine("Home obtida...");

var categories = doc.DocumentNode.SelectNodes("//div[contains(@class, 'table table-striped no-border')]/tbody/tr");
Console.WriteLine("Categorias obtida...");

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