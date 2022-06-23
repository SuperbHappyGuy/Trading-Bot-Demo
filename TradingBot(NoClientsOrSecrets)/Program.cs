using System;
using System.Collections.Generic;
using System.IO;
using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using WebSocketSharp;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4.Data;
using System.Linq;


namespace BinanceAPI.ClientConsole
{
    class Program
    {   //Google Creds
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "Trading Bot";
        static readonly string SpreadsheetId = "SheetID";
        static readonly string sheet = "Sheet1";
        static SheetsService service;



        static void Main(string[] args)
        {
            float periodCount = 0f;
            double freeMoney = 1000;

            //Google Creds
            GoogleCredential credential;
            using (var stream = new FileStream("JSONCredentialsFile", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            //Variables
            string command;
            string localDate = DateTime.Now.ToString("dd MMMM yyyy HH;mm;ss");
            string spacerH = "-------------------------------------";
            double sma20 = 0;
            double sma50 = 0;
            double buyRate = 0.01;
            double buyLot = 0;
            double fee = 0.02;
            double tradePercentage = 0;
            int trades = 0;
            bool commandLoop = true;
            bool enterTrade = false;
            bool sma20Start = false;
            bool sma50Start = false;
            bool smaGreater = false;
            bool smaLesser = false;

            List<double> sma20Data = new List<double>();
            List<double> sma50Data = new List<double>();
            List<double> activeTradeCloses = new List<double>();
            List<double> closes = new List<double>();
            List<double> opens = new List<double>();
            List<double> sma20Averages = new List<double>();
            List<double> sma50Averages = new List<double>();

            //Gets the usd to cad conversion
            var url3 = "https://free.currconv.com/api/v7"; //base endpoint without USDCAD call and api key for demonstration

            var request3 = WebRequest.Create(url3);
            request3.Method = "GET";

            var webResponse3 = request3.GetResponse();
            var webStream3 = webResponse3.GetResponseStream();

            var reader3 = new StreamReader(webStream3);
            var data3 = reader3.ReadToEnd();

            Console.WriteLine(data3);

            Response testv3 = JsonConvert.DeserializeObject<Response>(data3);

            //Websocket Connection (Stream)
            using (var ws = new WebSocket("wss://stream.binance.com:9443/ws/btcusdt@kline_1m"))
            {
                ws.OnMessage += (sender, e) =>
                {
                    string closeV = "<<";
                    string openV = "<<";
                    string sma20V = "<<";
                    string sma50V = "<<";

                    //Console.WriteLine("Message received: " + e.Data);
                    Root oof = JsonConvert.DeserializeObject<Root>(e.Data);

                    Console.WriteLine(spacerH);
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(DateTime.Now.ToString("dd MMMM yyyy HH;mm;ss"));
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(spacerH);

                    closes.Add(oof.k.c);

                    if (closes.Count > 2)
                    {
                        closes.RemoveAt(0);
                    }

                    if (closes.Count == 2)
                    {
                        if (closes[1] > closes[0])
                        {
                            closeV = "^";
                            Console.ForegroundColor = ConsoleColor.Green;
                        }

                        if (closes[1] < closes[0])
                        {
                            closeV = "v";
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                    }

                    Console.WriteLine("Close: " + oof.k.c * testv3.USD_CAD + " (CAD) " + closeV);
                    Console.ForegroundColor = ConsoleColor.White;

                    //Counts number of candlestick periods and gets smas
                    if (oof.k.x == true)
                    {
                        sma20 = 0;
                        sma50 = 0;
                        periodCount++;
                        sma20Data.Add(oof.k.c);
                        sma50Data.Add(oof.k.c);
                        opens.Add(oof.k.o);

                        foreach (double i in sma20Data)
                        {
                            sma20 += i / sma20Data.Count;
                        }

                        foreach (double i in sma50Data)
                        {
                            sma50 += i / sma50Data.Count;
                        }

                        sma20Averages.Add(sma20);
                        sma50Averages.Add(sma50);
                    }

                    if (opens.Count > 2)
                    {
                        opens.RemoveAt(0);
                    }

                    if (opens.Count == 2)
                    {
                        if (opens[1] > opens[0])
                        {
                            openV = "^";
                            Console.ForegroundColor = ConsoleColor.Green;
                        }

                        if (opens[1] < opens[0])
                        {
                            openV = "v";
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                    }

                    Console.WriteLine("Open: " + oof.k.o * testv3.USD_CAD + " (CAD) " + openV);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Candle Start Time: " + oof.k.t);
                    Console.WriteLine("Candle Close Time: " + oof.k.T);
                    Console.WriteLine("Is Candle Closed: " + oof.k.x);
                    Console.WriteLine("Period Count: " + periodCount);
                    Console.WriteLine("Trades: " + trades);
                    //Console.WriteLine(spacerH);
                    //sma20Data.ForEach(i => Console.WriteLine(i));
                    Console.WriteLine(spacerH);

                    //Shows each SMA in CAD
                    if (sma20Averages.Count > 2)
                    {
                        sma20Averages.RemoveAt(0);
                    }

                    if (sma20Averages.Count == 2)
                    {
                        if (sma20Averages[1] > sma20Averages[0])
                        {
                            sma20V = "^";
                            Console.ForegroundColor = ConsoleColor.Green;
                        }

                        if (sma20Averages[1] < sma20Averages[0])
                        {
                            sma20V = "v";
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("SMA20: " + sma20 * testv3.USD_CAD + " (CAD) " + sma20V);

                    if (sma50Averages.Count > 2)
                    {
                        sma50Averages.RemoveAt(0);
                    }

                    if (sma50Averages.Count == 2)
                    {
                        if (sma50Averages[1] > sma50Averages[0])
                        {
                            sma50V = "^";
                            Console.ForegroundColor = ConsoleColor.Green;
                        }

                        if (sma50Averages[1] < sma50Averages[0])
                        {
                            sma50V = "v";
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("SMA50: " + sma50 * testv3.USD_CAD + " (CAD) " + sma50V);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(spacerH);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Cash: " + freeMoney);
                    Console.ForegroundColor = ConsoleColor.White;

                    //Limits each list to a length and removes the oldest one
                    if (sma20Data.Count > 20)
                    {
                        sma20Start = true;
                        sma20Data.RemoveAt(0);
                    }

                    if (sma50Data.Count > 50)
                    {
                        sma50Start = true;
                        sma50Data.RemoveAt(0);
                    }

                    if (sma20 > sma50 && oof.k.x == true)
                    {
                        if (enterTrade == true && smaLesser == true)
                        {
                            tradePercentage = (((activeTradeCloses.Last() - activeTradeCloses[0]) / activeTradeCloses[0]) * -1);
                            buyLot = (freeMoney * buyRate) * tradePercentage;
                            fee = buyLot * 0.02;
                            freeMoney += buyLot - fee;

                            activeTradeCloses.Clear();

                            enterTrade = false;
                            trades++;
                        }

                        activeTradeCloses.Add(oof.k.c);
                        smaGreater = true;

                        if (smaLesser == true && sma20Start == true && sma50Start == true)
                        {
                            enterTrade = true;
                        }
                        smaLesser = false;
                    }

                    if (sma20 < sma50 && oof.k.x == true)
                    {
                        if (enterTrade == true && smaGreater == true)
                        {
                            tradePercentage = (activeTradeCloses.Last() - activeTradeCloses[0]) / activeTradeCloses[0];
                            buyLot = (freeMoney * buyRate) * tradePercentage;
                            freeMoney += buyLot;

                            activeTradeCloses.Clear();

                            enterTrade = false;
                            trades++;
                        }

                        activeTradeCloses.Add(oof.k.c);
                        smaLesser = true;
                        if (smaGreater == true && sma20Start == true && sma50Start == true)
                        {
                            enterTrade = true;
                        }
                        smaGreater = false;
                    }
                };

                ws.OnError += (sender, e) =>
                    Console.WriteLine("Error: " + e.Message);

                ws.Connect();

                Console.ReadKey(true);
            }
            // JSON Request Data (Not Stream)
            var url2 = "https://api1.binance.com/api/v3/ticker/price?symbol=DOGEUSDT";

            var request2 = WebRequest.Create(url2);
            request2.Method = "GET";

            var webResponse = request2.GetResponse();
            var webStream = webResponse.GetResponseStream();

            var reader = new StreamReader(webStream);
            var data = reader.ReadToEnd();

            Console.WriteLine(data);

            Binance testv = JsonConvert.DeserializeObject<Binance>(data);

            int DownloadFile(String remoteFilename,
                               String localFilename)
            {
                // Function will return the number of bytes processed
                // to the caller. Initialize to 0 here.
                int bytesProcessed = 0;

                // Assign values to these objects here so that they can
                // be referenced in the finally block
                Stream remoteStream = null;
                Stream localStream = null;
                WebResponse response = null;

                // Use a try/catch/finally block as both the WebRequest and Stream
                // classes throw exceptions upon error
                try
                {
                    // Create a request for the specified remote file name
                    WebRequest request = WebRequest.Create(remoteFilename);
                    if (request != null)
                    {
                        // Send the request to the server and retrieve the
                        // WebResponse object 
                        response = request.GetResponse();
                        if (response != null)
                        {
                            // Once the WebResponse object has been retrieved,
                            // get the stream object associated with the response's data
                            remoteStream = response.GetResponseStream();

                            // Create the local file
                            localStream = File.Create(localFilename);

                            // Allocate a 1k buffer
                            byte[] buffer = new byte[1024];
                            int bytesRead;

                            // Simple do/while loop to read from stream until
                            // no bytes are returned
                            do
                            {
                                // Read data (up to 1k) from the stream
                                bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

                                // Write the data to the local file
                                localStream.Write(buffer, 0, bytesRead);

                                // Increment total bytes processed
                                bytesProcessed += bytesRead;
                            } while (bytesRead > 0);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    // Close the response and streams objects here
                    // to make sure they're closed even if an exception
                    // is thrown at some point
                    if (response != null) response.Close();
                    if (remoteStream != null) remoteStream.Close();
                    if (localStream != null) localStream.Close();
                }

                // Return total bytes processed to caller.
                return bytesProcessed;
            }

            ClientConnect();
            //Intro
            Console.WriteLine("Welcome!");
            //Initial Command
            command = Console.ReadLine();

            var range = $"{sheet}!A:F";
            var valueRange = new ValueRange();

            var objectList = new List<object>() { "1", "2", "3", "4", "5", "6" };
            valueRange.Values = new List<IList<object>> { objectList };

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var appendResponse = appendRequest.Execute();

            do
            {
                //List of Commands
                if (command == "help")
                {
                    Console.WriteLine("-ticker");
                    Console.WriteLine("-bookprice");

                    command = Console.ReadLine();
                }
                //Prints JSON Data
                else if (command == "1")
                {
                    Console.WriteLine(testv.price * testv3.USD_CAD);

                    command = Console.ReadLine();
                }
                //Test Command
                else if (command == "2")
                {
                    Console.WriteLine(testv3.USD_CAD);

                    command = Console.ReadLine();
                }

                else if (command == "0")
                {
                    localDate = DateTime.Now.ToString("dd MMMM yyyy HH;mm;ss");
                    int read = DownloadFile("https://docs.google.com/spreadsheets/d/e/2PACX-1vQ8mxlfJ4D4g7dkpHNbiJg2Tr9lKBOdbAQ99tiiHUcoQChvJO22sFVIzpCwk83RST2-KS-uGZY5LPTL/pub?output=csv",
                        @"d:\Trading Bot Sheets\.cvs");
                    Console.WriteLine("{0} bytes written", read);

                    System.IO.File.Move(@"d:\Trading Bot Sheets\.cvs", @"d:\Trading Bot Sheets\TradingBot " + localDate + ".cvs");

                    command = Console.ReadLine();
                }

                else if (command == "3")
                {
                    localDate = DateTime.Now.ToString("dd MMMM yyyy HH;mm;ss");
                    Console.WriteLine(localDate);

                    command = Console.ReadLine();
                }

                //Null Command by User
                else
                {
                    Console.WriteLine("Not A Valid Action");

                    command = Console.ReadLine();
                }
            } while (commandLoop == true);
        }

        //Connects Users API Key and Secret Key to Binance to Recieve Account Information
        static void ClientConnect()
        {
            BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials("Client", "Secret"),
                LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                LogWriters = new List<ILogger> { new ConsoleLogger() }
            });
            BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials("Client", "Secret"),
                LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                LogWriters = new List<ILogger> { new ConsoleLogger() }
            });
        }
        //Read Values From SpredSheet
        static void ReadEnteries()
        {
            var range = $"{sheet}!A1:F10";
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);

            var response = request.Execute();
            var values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    Console.WriteLine("{0} | {1} | {2} | {3}", row[5], row[4], row[3], row[1]);
                }
            }
            else
            {
                Console.WriteLine("No Data");
            }

        }
        //Create New Cells In Spreadsheet
        static void CreateEntry()
        {
            var range = $"{sheet}!A:F";
            var valueRange = new ValueRange();

            var objectList = new List<object>() { "Hello", "This", "was", "inserted", "via", "C#" };
            valueRange.Values = new List<IList<object>> { objectList };

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var appendResponse = appendRequest.Execute();
        }
        //Update Cells In Spreadsheet
        static void Update()
        {
            var range = $"{sheet}!A1";
            var valueRange = new ValueRange();

            var objectList = new List<object>() { "updated" };
            valueRange.Values = new List<IList<object>> { objectList };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var updateResponse = updateRequest.Execute();
        }
        //Delete Cells In SpreadSheet
        static void DeleteEntry()
        {
            var range = $"{sheet}!";
            var requestBody = new ClearValuesRequest();

            var deleteRequest = service.Spreadsheets.Values.Clear(requestBody, SpreadsheetId, range);
            var deleteResponse = deleteRequest.Execute();
        }
    }

    //Parses JSON Into Variables
    class Binance
    {
        public double price { get; set; }

        public string symbol { get; set; }

        //public float asks { get; set; }
    }

    public class Response
    {
        public double USD_CAD { get; set; }
    }

    public class K
    {
        public long t { get; set; }
        public long T { get; set; }
        public string s { get; set; }
        public string i { get; set; }
        public int f { get; set; }
        public int L { get; set; }
        public double o { get; set; }
        public double c { get; set; }
        public string h { get; set; }
        public string l { get; set; }
        public string v { get; set; }
        public int n { get; set; }
        public bool x { get; set; }
        public string q { get; set; }
        public string V { get; set; }
        public string Q { get; set; }
        public string B { get; set; }
    }

    public class Root
    {
        public string e { get; set; }
        public long E { get; set; }
        public string s { get; set; }
        public K k { get; set; }
    }

}