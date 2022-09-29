<Query Kind="Program">
  <Connection>
    <ID>98af8b0c-87dd-4eea-ae95-9cb5384fcc1c</ID>
    <Server>fm-sql-01.fairmont.local\FAIRMONTSQL</Server>
    <IsProduction>true</IsProduction>
    <Database>AwareNewFairmontTraining</Database>
  </Connection>
  <Namespace>System.Text.Json</Namespace>
</Query>

#load ".\Prices"
#load ".\ProcessSubmission"
#load "xunit"

// Filename:  HttpServer.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)
// https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7

// Extended by Alex Hamilton-Smith on 2022-09-28.

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;

public static class configs
{
    public const int port = 8144;
}

// local environment may need some version of this:

//  netsh http add urlacl url=http://+:8144/ user=ts.script.user

void Main()
{
    context.db = this;
    HttpListenerExample.HttpServer.db = this;
    HttpListenerExample.HttpServer.Main();
}


namespace HttpListenerExample
{
    class HttpServer
    {
        public static UserQuery db;
        public static HttpListener listener;
        public static string url = $"http://+:{configs.port}/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>Pricing Update Utility</title>" +
            "  </head>" +
            "  <body>" +
            "    <h1>Pricing Update Utility</h1>" +
            "    <p>Page Views: {0}</p>" +
            "  </body>" +
            "</html>";


        public static async System.Threading.Tasks.Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                // Make sure we don't increment the page views counter if `favicon.ico` is requested
                if (req.Url.AbsolutePath != "/favicon.ico")
                    pageViews += 1;

                // Write the response info
                string disableSubmit = !runServer ? "disabled" : "";

                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews, disableSubmit));

                var payload = GetRequestPostData(ctx.Request);
                PricingSubmission sub = new PricingSubmission("AlexHS", TestOnly: true);
                if (payload != null)
                {
                    try
                    {	        
                        payload.Dump();
                        sub = JsonSerializer.Deserialize<PricingSubmission>(payload);
                        if (!sub.TestOnly)
                        {
                            var proc = new ProcessSubmission(sub);
                        }
                        "SUCCESS: Parsed JSON.".Dump();
                        data = Encoding.UTF8.GetBytes("SUCCESS");
                    }
                    catch (IndexOutOfRangeException)
                    {
                        data = Encoding.UTF8.GetBytes("FAIL");
                        "FAIL: Failed to parsed JSON.".Dump();
                    }
                }
                payload.Dump();
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        public static void Main()
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            System.Threading.Tasks.Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}

public static string GetRequestPostData(HttpListenerRequest request)
{
    if (!request.HasEntityBody)
    {
        return null;
    }
    using (System.IO.Stream body = request.InputStream)
    {
        using (var reader = new System.IO.StreamReader(body, request.ContentEncoding))
        {
            return reader.ReadToEnd();
        }
    }
}
