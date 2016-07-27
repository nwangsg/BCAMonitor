using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace BCAMonitor
{
    public class Program
    {
        private static readonly IDbConnection Db = new SqlConnection(@"Data Source=.\localdb;Initial Catalog=wangningDB;Integrated Security=True");

        private static void Main(string[] args)
        {
            var driver = new ChromeDriver { Url = "www.google.com" };
            try
            {
                driver.Navigate().GoToUrl("https://www.bca.gov.sg/eservice/ProcessCDNew.aspx");
                driver.FindElement(By.Id("btnSearchAgain")).Click();

                WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 0, 5));
                wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.Id("txtsearch1")));
                var input = driver.FindElement(By.Id("txtsearch1"));
                input.Clear();
                input.SendKeys("A1656-00003-2012%");
                driver.FindElement(By.Id("btnSearch")).Click();
                
                wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.Id("table1")));

                var allRawRows = driver.FindElement(By.Id("Table1")).FindElements(By.TagName("tr")).ToList();
                allRawRows.RemoveAt(0);
                List<BcaRow> rows = new List<BcaRow>();
                foreach (var row in allRawRows)
                {
                    var rawRowElements = row.FindElements(By.TagName("td"));
                    var newBcaRow = new BcaRow
                    {
                        PlanRefNumber = rawRowElements[0].Text,
                        ApplicationType = rawRowElements[1].Text,
                        Status = rawRowElements[2].Text,
                        DateTime = DateTime.ParseExact(rawRowElements[3].Text, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                        ViewDetails = rawRowElements[4].Text
                    };
                    rows.Add(newBcaRow);
                }
                Db.Execute(
                    "insert into BCAStatus values(@PlanRefNumber,@ApplicationType,@Status,@DateTime,@ViewDetails)", rows);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                driver.Quit();
                Console.Read();
            }
        }
    }
}