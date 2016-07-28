using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Linq;
using System.Net;
using System.Net.Mail;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;

namespace BCAMonitor
{
    public class Program
    {
        private static readonly IDbConnection Db = new SqlConnection(@"Data Source=.\localdb;Initial Catalog=wangningDB;Integrated Security=True");

        private static void Main(string[] args)
        {
            //todo: construct loop or put on jenkins
            MonitorBca();
        }

        //private static void WebDriverWaitById(IWebDriver driver, string idToFind)
        //{
        //    WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 0, 5));
        //    wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.Id(idToFind)));
        //}

        public static DataTable ConvertToDataTable<T>(IList<T> data)
        {
            PropertyDescriptorCollection properties =
               TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;

        }

        public static void MonitorBca()
        {
            //var driver = new ChromeDriver { Url = "www.google.com" };
            var driver = new PhantomJSDriver { Url = "www.google.com" };
            try
            {
                driver.Navigate().GoToUrl("https://www.bca.gov.sg/eservice/ProcessCDNew.aspx");
                driver.FindElement(By.Id("btnSearchAgain")).Click();
                driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 1));
                //WebDriverWaitById(driver, "txtsearch1");
                var input = driver.FindElement(By.Id("txtsearch1"));
                input.Clear();
                input.SendKeys("A1656-00003-2012%");
                driver.FindElement(By.Id("btnSearch")).Click();
                driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 1));
                //WebDriverWaitById(driver, "table1");
                var allRawRows = driver.FindElement(By.Id("Table1")).FindElements(By.TagName("tr")).ToList();
                allRawRows.RemoveAt(0);
                var rows = allRawRows.Select(row => row.FindElements(By.TagName("td"))).Select(rawRowElements => new BcaRow
                {
                    PlanRefNumber = rawRowElements[0].Text, ApplicationType = rawRowElements[1].Text, Status = rawRowElements[2].Text, DateTime = DateTime.ParseExact(rawRowElements[3].Text, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), ViewDetails = rawRowElements[4].Text
                }).ToList();
                var insertedRows = Db.Query<BcaRowUpdateRecord>("[dbo].[UpsertBCAStatus]",
                    new { BcaRows = ConvertToDataTable(rows) }, commandType: CommandType.StoredProcedure);

                if (!insertedRows.Any()) return;

                //todo: Send table or updated content
                var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("nwangsg@gmail.com", "wn12130124"),
                    EnableSsl = true
                };
                var message = GetMailMessageTemplate();
                client.Send(message);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            finally
            {
                driver.Quit();
            }
        }

        private static MailMessage GetMailMessageTemplate()
        {
            MailMessage message = new MailMessage {From = new MailAddress("nwangsg@gmail.com")};
            message.To.Add(new MailAddress("nwangsg@gmail.com"));
            message.Subject = "BCA Status Update";
            message.IsBodyHtml = true;
            message.Body = "New Update in BCA website!! <br /> https://www.bca.gov.sg/eservice/ProcessCDNew.aspx";
            return message;
        }
    }
}