using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using Timer = System.Timers.Timer;


namespace BCAMonitor
{
    public class Program
    {
        private static readonly IDbConnection Db = new SqlConnection(@"Data Source=.\localdb;Initial Catalog=wangningDB;Integrated Security=True");
        private static readonly Timer DelayTimer = new Timer();

        private static void Main(string[] args)
        {
            //todo: construct loop or put on jenkins
            DelayTimer.Interval = 3600000;
            DelayTimer.Elapsed += (o, e) => MonitorBca();
            MonitorBca();
            while (true)
            {

            }
        }

        public static DataTable ConvertToDataTable<T>(IList<T> data)
        {
            var properties =
               TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (var item in data)
            {
                var row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }

        public static void MonitorBca()
        {
            DelayTimer.Stop();
            DelayTimer.Start();
            var driver = new PhantomJSDriver { Url = "www.google.com" };
            try
            {
                driver.Navigate().GoToUrl("https://www.bca.gov.sg/eservice/ProcessCDNew.aspx");
                driver.FindElement(By.Id("btnSearchAgain")).Click();
                driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 1));
                var input = driver.FindElement(By.Id("txtsearch1"));
                input.Clear();
                input.SendKeys("A1656-00003-2012%");
                driver.FindElement(By.Id("btnSearch")).Click();
                driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 1));
                var allRawRows = driver.FindElement(By.Id("Table1")).FindElements(By.TagName("tr")).ToList();
                allRawRows.RemoveAt(0);
                var rows = allRawRows.Select(row => row.FindElements(By.TagName("td"))).Select(rawRowElements => new BcaRow
                {
                    PlanRefNumber = rawRowElements[0].Text, ApplicationType = rawRowElements[1].Text, Status = rawRowElements[2].Text, DateTime = DateTime.ParseExact(rawRowElements[3].Text, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), ViewDetails = rawRowElements[4].Text
                }).ToList();
                var insertedRows = Db.Query<BcaRowUpdateRecord>("[dbo].[UpsertBCAStatus]",
                    new { BcaRows = ConvertToDataTable(rows) }, commandType: CommandType.StoredProcedure);


                if (!insertedRows.Any()) return;
                var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("nwangsg@gmail.com", "wn12130124"),
                    EnableSsl = true
                };
                var message = GetMailMessageTemplate();
                client.Send(message);
                //todo: Send table or updated content
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Environment.Exit(0);
            }
            finally
            {
                driver.Quit();
            }
        }

        private static MailMessage GetMailMessageTemplate()
        {
            var message = new MailMessage {From = new MailAddress("nwangsg@gmail.com")};
            message.To.Add(new MailAddress("nwangsg@gmail.com"));
            message.Subject = "BCA Status Update";
            message.IsBodyHtml = true;
            message.Body = "New Update in BCA website!! <br /> https://www.bca.gov.sg/eservice/ProcessCDNew.aspx";
            return message;
        }
    }
}