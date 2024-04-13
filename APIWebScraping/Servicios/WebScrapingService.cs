using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using APIWebScraping.Models;

public class WebScrapingService
{
    private const int MaxLlamadasPorMinuto = 20;
    private static DateTime UltimaLlamada = DateTime.MinValue;
    private static int LlamadasRestantesPorMinuto = MaxLlamadasPorMinuto;
    public bool EsValidaLlamada()
    {
        var tiempoTranscurrido = DateTime.Now - UltimaLlamada;

        if (tiempoTranscurrido.TotalMinutes < 1)
        {
            if (LlamadasRestantesPorMinuto > 0)
            {
                // Restan llamadas disponibles
                LlamadasRestantesPorMinuto--;

                UltimaLlamada = DateTime.Now;
                return true;
            }
            else
            {
                // Se superó el límite de llamadas por minuto
                return false;
            }
        }
        else
        {
            // Reinicia el contador de llamadas por minuto si ha pasado más de 1 minuto
            LlamadasRestantesPorMinuto = MaxLlamadasPorMinuto;
            UltimaLlamada = DateTime.Now;
            return true;
        }
    }
public async Task<List<Dictionary<string, string>>> OFACSearchCompany(string companyName)
{
    try
    {
        if (!EsValidaLlamada())
        {
            return null;
        }

        List<Dictionary<string, string>> elementsList = new List<Dictionary<string, string>>();

        var options = new ChromeOptions();
        options.AddArgument("--headless");

        using (var driver = new ChromeDriver(options))
        {
            driver.Navigate().GoToUrl("https://sanctionssearch.ofac.treas.gov/");

            var inputSearch = driver.FindElement(By.Name("ctl00$MainContent$txtLastName"));
            inputSearch.SendKeys(companyName);

            var clickSearch = driver.FindElement(By.Name("ctl00$MainContent$btnSearch"));
            clickSearch.Submit();

            var nameCompanies = driver.FindElements(By.CssSelector("a[href^='Details.aspx?id=']"));

            var filas = driver.FindElements(By.XPath("//*[@id='gvSearchResults']/tbody/tr"));

            foreach (var fila in filas)
            {
                Dictionary<string, string> elementAttributes = new Dictionary<string, string>();

                IList<IWebElement> celdas = fila.FindElements(By.TagName("td"));

                elementAttributes["Name"] = celdas[0].Text;
                elementAttributes["Address"] = celdas[1].Text;
                elementAttributes["Type"] = celdas[2].Text;
                elementAttributes["Programs"] = celdas[3].Text;
                elementAttributes["List"] = celdas[4].Text;
                elementAttributes["Score"] = celdas[5].Text;

                elementsList.Add(elementAttributes);
            }
        }

        return await Task.FromResult(elementsList);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return null;
    }

}



    //public List<string> OffshoreSearchCompany(string companyName)
    //{
    //    try
    //    {
    //        var options = new ChromeOptions();
    //        options.AddArgument("--headless");

    //        using (var driver = new ChromeDriver(options))
    //        {
    //            driver.Navigate().GoToUrl("https://offshoreleaks.icij.org/");

    //            var checkbox = driver.FindElement(By.Id("accept"));
    //            checkbox.Click();
    //            var button = driver.FindElement(By.CssSelector("button[type='submit'].btn.btn-primary.btn-block.btn-lg"));
    //            button.Click();
    //            var searcher = driver.FindElement(By.Name("q"));
    //            searcher.SendKeys(companyName); //Se busca la compañia
    //            searcher.Submit();

    //            IList<IWebElement> links = driver.FindElements(By.CssSelector("a.font-weight-bold.text-dark")); //se guardan

    //            List<string> results = new List<string>();
    //            foreach (var item in links)
    //            {
    //                results.Add(item.Text);
    //            }


    //            return results;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error: {ex.Message}");
    //        return (new List<string>());
    //    }
    //}
    public async Task<List<Dictionary<string, string>>> BuscarEnOffshoreLeaks(string nombre)  //primera
    {
        int numberOfHits = 0;
        List<Dictionary<string, string>> elementsList = new List<Dictionary<string, string>>();
        var url = $"https://offshoreleaks.icij.org/search?utf8=%E2%9C%93&q={nombre}&c=&j=&p=";

        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

            var html = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            HtmlNode searchResultsDiv = htmlDocument.DocumentNode.SelectSingleNode("//div[@id='search_results']");

            if (searchResultsDiv != null)
            {
                HtmlNode tableResponsiveDiv = searchResultsDiv.SelectSingleNode(".//div[contains(@class, 'table-responsive')]");

                if (tableResponsiveDiv != null)
                {
                    HtmlNode searchResultsTable = tableResponsiveDiv.SelectSingleNode(".//table[contains(@class, 'table table-sm table-striped search__results__table')]");

                    if (searchResultsTable != null)
                    {
                        numberOfHits = searchResultsTable.SelectNodes(".//tr").Count - 1;  

                        foreach (HtmlNode row in searchResultsTable.SelectNodes(".//tr[position()>1]")) 
                        {
                            Dictionary<string, string> elementAttributes = new Dictionary<string, string>();

                            HtmlNodeCollection cells = row.SelectNodes(".//td");
                            if (cells != null && cells.Count == 4)
                            {
                                elementAttributes["Entity"] = cells[0].InnerText.Trim();
                                elementAttributes["Jurisdiction"] = cells[1].InnerText.Trim();
                                elementAttributes["LinkedTo"] = cells[2].InnerText.Trim();
                                elementAttributes["DataFrom"] = cells[3].InnerText.Trim();

                                elementsList.Add(elementAttributes);
                            }
                        }
                    }
                }
            }

        }

        return elementsList;
    }

    public async Task<List<Dictionary<string, string>>> ScrapeWorldBankData(string nombre) //2da
    {
        List<Dictionary<string, string>> elementsList = new List<Dictionary<string, string>>();

        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--headless"); 
        var chromeDriverService = ChromeDriverService.CreateDefaultService();
        var webDriver = new ChromeDriver(chromeDriverService, chromeOptions);

        try
        {
            var url = "https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms";
            webDriver.Navigate().GoToUrl(url);

            await Task.Delay(4000);

            string html = webDriver.PageSource;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var dataRows = doc.DocumentNode.SelectNodes("//div[@class='kendoTableContainer']//div[@class='row']//div[@class='col-lg-12 col-md-12 col-sm-12 col-xs-12']//div[@class='lp_kendo_table k-grid k-widget k-display-block']//div[@class='k-grid-content k-auto-scrollable']//table//tbody//tr");

            if (dataRows != null)
            {
                foreach (var row in dataRows)
                {
                    var rowData = new Dictionary<string, string>();
                    var cells = row.SelectNodes(".//td");

                    if (cells != null && cells.Count >= 7)
                    {
                        if (cells[0].InnerText.Trim()==nombre)
                        {
                            rowData["Firm Name"] = cells[0].InnerText.Trim();
                            rowData["Address"] = cells[2].InnerText.Trim();
                            rowData["Country"] = cells[3].InnerText.Trim();
                            rowData["From Date"] = cells[4].InnerText.Trim();
                            rowData["To Date"] = cells[5].InnerText.Trim();
                            rowData["Grounds"] = cells[6].InnerText.Trim();

                            elementsList.Add(rowData);
                        }
                    }
                }
            }

        }
        finally
        {
            webDriver.Quit(); 
        }

        return elementsList;
    }

}








