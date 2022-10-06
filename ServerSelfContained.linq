<Query Kind="Program">
  <Connection>
    <ID>98af8b0c-87dd-4eea-ae95-9cb5384fcc1c</ID>
    <Persist>true</Persist>
    <Server>fm-sql-01.fairmont.local\FAIRMONTSQL</Server>
    <IsProduction>true</IsProduction>
    <Database>AwareNewFairmontTraining</Database>
  </Connection>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>Xunit</Namespace>
</Query>

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

                string payload = GetRequestPostData(ctx.Request);
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
                    catch (Exception e)
                    {
                        e.Dump();
                        
                        data = Encoding.UTF8.GetBytes($"Failed to parsed JSON. {e.Message}");
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


public class context
{
    public static UserQuery db;
}

public class PricingSubmission
{
    public string SubmittedUserName { get; set; }
    public bool TestOnly { get; set; }
    public HashSet<ListPrice> ListPrices { get; set; }
    public HashSet<ListPriceBreak> ListPriceBreaks { get; set; }
    public HashSet<CustomerPrice> CustomerPrices { get; set; }
    public HashSet<PriceClassPrice> PriceClassPrices { get; set; }
    public PricingSubmission(string SubmittedUserName, bool TestOnly = false)
    {
        this.SubmittedUserName = SubmittedUserName;
        this.TestOnly = TestOnly;
        this.ListPrices = new HashSet<UserQuery.ListPrice>();
        this.ListPriceBreaks = new HashSet<UserQuery.ListPriceBreak>();
        this.CustomerPrices = new HashSet<UserQuery.CustomerPrice>();
        this.PriceClassPrices = new HashSet<UserQuery.PriceClassPrice>();
        Assert.True(User() != null, $"Cannot match on user {SubmittedUserName}"!);
    }
    public User User()
    {
        return context.db.Users.FirstOrDefault(u => u.UserName == this.SubmittedUserName);
    }
}

public class PricingBase
{
    public string ItemCode { get; set; }
    public decimal Value { get; set; }
    public string CurrencyCode { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public Item Item()
    {
        return context.db.Items.FirstOrDefault(i => i.ItemCode == this.ItemCode);
    }
    public Currency Currency()
    {
        return context.db.Currencies.First(c => c.CurrencyCode == this.CurrencyCode);
    }
}

public class PricingBaseWithProject : PricingBase
{
    public string ProjectName { get; set; }
    public Project Project()
    {
        return context.db.Projects.FirstOrDefault(i => i.ProjectName == this.ProjectName);
    }
}

public class ListPrice : PricingBase
{
    public int MinOrder { get; set; }
    public ListPrice(string itemcode, decimal value, DateTime start, DateTime? end)
    {
        this.ItemCode = itemcode;
        this.Value = value;
        this.Start = start;
        this.End = end;
        this.MinOrder = 1;
        this.CurrencyCode = "AUD";
        if (this.Item() == null) throw new DataException($"Cannot update for item '{itemcode}' as does not exist! Check spelling or remove.");
    }
    new public Currency Currency()
    {
        if (this.CurrencyCode == null) return context.db.Currencies.First(c => c.CurrencyCode == "AUD");
        return context.db.Currencies.First(c => c.CurrencyCode == this.CurrencyCode);
    }
}

public class ListPriceBreak : PricingBaseWithProject
{
    public int BreakQty { get; set; }
    public ListPriceBreak() { }
    public ListPriceBreak(string itemcode, decimal value, DateTime start, DateTime? end, int break_qty, string project_name, string currency_code)
    {
        this.ItemCode = itemcode;
        this.Value = value;
        this.Start = start;
        this.End = end;
        this.BreakQty = break_qty;
        this.ProjectName = project_name;
        this.CurrencyCode = currency_code;
        if (this.Item() == null) throw new DataException($"Cannot update for item '{itemcode}' as does not exist! Check spelling or remove.");
    }
    new public Currency Currency()
    {
        if (this.CurrencyCode == null) return context.db.Currencies.First(c => c.CurrencyCode == "AUD");
        return context.db.Currencies.First(c => c.CurrencyCode == this.CurrencyCode);
    }
}

public class CustomerPrice : PricingBaseWithProject
{
    public string CustomerCode { get; set; }
    public int BreakQty { get; set; }
    public CustomerPrice() { }
    public CustomerPrice(string customer_code, string itemcode, decimal value, DateTime start, DateTime? end, int break_qty, string project_name, string currency_code)
    {
        this.CustomerCode = customer_code;
        this.ItemCode = itemcode;
        this.Value = value;
        this.Start = start;
        this.End = end;
        this.BreakQty = break_qty;
        this.ProjectName = project_name;
        this.CurrencyCode = currency_code;
        if (this.Item() == null) throw new DataException($"Cannot update for item '{itemcode}' as does not exist! Check spelling or remove.");
        if (this.Customer() == null) throw new DataException($"Cannot update for Customer '{customer_code}' as does not exist! Check spelling or remove.");
    }
    public Customer Customer()
    {
        return context.db.Customers.FirstOrDefault(i => i.CustomerCode == this.CustomerCode);
    }
    new public Currency Currency()
    {
        if (this.CurrencyCode == null)
        {
            var cust_div_config = this.Customer().CustomerDivisions.First().CustomerDivisionConfigurationValues.FirstOrDefault(d => d.EntityTypeConfiguration.ConfigurationName == "DefaultTransactionCurrency");
            if (cust_div_config != null)
            {
                var id = cust_div_config.ConfigurationValue;
                return context.db.Currencies.First(c => c.CurrencyID.ToString() == id);
            }
            var div_id = this.Customer().CustomerDivisions.First().Division.Trading.TradingCurrencyID;
            return context.db.Currencies.First(c => c.CurrencyID == div_id);
        }
        return context.db.Currencies.First(c => c.CurrencyCode == this.CurrencyCode);
    }
}

public class PriceClassPrice : PricingBaseWithProject
{
    public string PriceClassName { get; set; }
    public int BreakQty { get; set; }
    public PriceClassPrice() { }
    public PriceClassPrice(string price_class_name, string itemcode, decimal value, DateTime start, DateTime? end, int break_qty, string project_name, string currency_code)
    {
        this.PriceClassName = price_class_name;
        this.ItemCode = itemcode;
        this.Value = value;
        this.Start = start;
        this.End = end;
        this.BreakQty = break_qty;
        this.ProjectName = project_name;
        this.CurrencyCode = currency_code;
        if (this.Item() == null) throw new DataException($"Cannot update for item '{itemcode}' as does not exist! Check spelling or remove.");
        if (this.PriceClass() == null) throw new DataException($"Cannot update for Customer '{price_class_name}' as does not exist! Check spelling or remove.");
    }
    public EntityClassificationCategory PriceClass()
    {
        return context.db.EntityClassificationCategories.FirstOrDefault(c => c.EntityClassificationCategoryDisplayName == PriceClassName && c.EntityClassificationCategoryGroupID == 12);
    }
    new public Currency Currency()
    {
        if (this.CurrencyCode == null)
        {
            var customers = context.db.Customers.Where(c => c.CustomerConfigurationValues.Any(s =>
                    s.EntityTypeConfiguration.ConfigurationDisplayName == "Customer Price Class"
                    &&
                    s.ConfigurationValue != null
                    &&
                    context.db.EntityClassificationCategories.FirstOrDefault(e =>
                            e.EntityClassificationCategoryID.ToString() == s.ConfigurationValue).EntityClassificationCategoryDisplayName == this.PriceClass().EntityClassificationCategoryDisplayCode
                ));
            string.Format("Holding election for single currency with {0} customers.", customers.Count()).Dump();
            customers.Select(c => c.CustomerCode).Dump();
            Dictionary<Currency, int> votes = new Dictionary<Currency, int>();
            foreach (var customer in customers)
            {
                $"{customer.CustomerCode}! - {customer.CustomerDivisions.First().Division.Company.BaseCurrencyID.ToString()}".Dump();
                var cust_div_config = customer.CustomerDivisions.First().CustomerDivisionConfigurationValues.FirstOrDefault(d => d.EntityTypeConfiguration.ConfigurationName == "DefaultTransactionCurrency");
                string id = "";
                if (cust_div_config != null) id = cust_div_config.ConfigurationValue;
                if (cust_div_config == null) id = customer.CustomerDivisions.First().Division.Trading.TradingCurrencyID.ToString();
                var currency = context.db.Currencies.First(c => c.CurrencyID.ToString() == id);
                if (!votes.ContainsKey(currency)) votes[currency] = 1;
                votes[currency] += 1;
                String.Format("Adding one vote for {0}.", votes.First(v => v.Value == votes.Max(m => m.Value)).Key.CurrencyName).Dump();
            }
            if (votes.Count() > 0)
            {
                String.Format("Tally votes and we have a winner! It's {0}.", votes.First(v => v.Value == votes.Max(m => m.Value)).Key.CurrencyName).Dump();
                return votes.First(v => v.Value == votes.Max(m => m.Value)).Key;
            }
            return null;
        }
        return context.db.Currencies.First(c => c.CurrencyCode == this.CurrencyCode);
    }
}


static class helpers
{
    static public HashSet<string> none_variants = new HashSet<string>()
    {
        null,
        "None",
        "NONE",
        "",
        "-",
        "NULL",
        "Null",
    };
}

public class ProcessSubmission
{
    bool Changes = false;
    PricingSubmission PricingSubmission;
    public ProcessSubmission(PricingSubmission PricingSubmission_)
    {
        this.PricingSubmission = PricingSubmission_;
        if (this.PricingSubmission.ListPrices.Count() > 0) this.process(this.PricingSubmission.ListPrices);
        if (this.PricingSubmission.ListPriceBreaks.Count() > 0) this.process(this.PricingSubmission.ListPriceBreaks);
        if (this.PricingSubmission.CustomerPrices.Count() > 0) this.process(this.PricingSubmission.CustomerPrices);
        if (this.PricingSubmission.PriceClassPrices.Count() > 0) this.process(this.PricingSubmission.PriceClassPrices);
        this.apply();
    }

    void process(HashSet<PriceClassPrice> priceClassPrices)
    {
        foreach (var price in priceClassPrices)
        {
            var price_class = price.PriceClass();
            var project = price.Project();
            if (!helpers.none_variants.Contains(price.ProjectName))
            {
                if (project == null) project = get_project(price.ProjectName, get_div(price.PriceClass()), price.Start, price.End);
                Debug.Assert(project != null, String.Format("No project!!!"));
            }
            Currency currency = price.Currency();
            Debug.Assert(currency != null, String.Format("No currency!!!"));

            var item = price.Item();
            if (item == null)
            {
                $"        !!      WARNING: Item '{price.ItemCode}' does not exist.".Dump();
            }
            else
            {
                var item_packaging = item.ItemPackagings.First(ip => ip.ConversionUnits == item.ItemPackagings.Max(s => s.ConversionUnits));
                var existing_price = context.db.PriceDiscountCustomerPriceClassItems.FirstOrDefault(p =>
                        p.Item == item
                        &&
                        p.CustomerPrice == price_class
                        &&
                        p.DiscountPriceTransactionCurrency == price.Value
                        &&
                        p.StartDate == price.Start
                        &&
                        p.Currency == currency
                        &&
                        p.ItemPackaging == item_packaging
                    );
                bool exists_already = existing_price != null;

                if (exists_already)
                {
                    if (existing_price.EndDate != price.End)
                    {
                        string.Format("Correcting EndDate for {0}.", price.ItemCode).Dump();
                        existing_price.EndDate = price.End;
                        existing_price.LastUpdatedDate = DateTime.Now;
                        Changes = true;
                    }
                    if (existing_price.Project != project)
                    {
                        string.Format("Correcting Project for {0}.", price.ItemCode).Dump();
                        existing_price.Project = project;
                        existing_price.LastUpdatedDate = DateTime.Now;
                        Changes = true;
                    }
                }
                else
                {
                    var prices_to_expire = context.db.PriceDiscountCustomerPriceClassItems.Where(p =>
                        p.Item == item
                        &&
                        p.CustomerPrice == price_class
                        &&
                        p.QtyMinimum == price.BreakQty
                        &&
                        p.Currency == currency
                        &&
                        p.StartDate <= DateTime.Today
                        &&
                        (p.EndDate == null || p.EndDate >= DateTime.Today)
                    );
                    foreach (var p in prices_to_expire)
                    {
                        p.EndDate = DateTime.Today.AddDays(-1);
                        Changes = true;
                        string.Format("Expiring pricing for {0}.", price.ItemCode).Dump();
                    }

                    string.Format("Creating new price record for {0}.", price.ItemCode).Dump();
                    var insert = new PriceDiscountCustomerPriceClassItem();
                    insert.Company = get_div(price.PriceClass()).Company;
                    insert.Division = get_div(price.PriceClass());
                    insert.Currency = currency;
                    insert.Item = item;
                    insert.CustomerPrice = price_class;
                    insert.Project = project;
                    insert.ItemPackaging = item_packaging;
                    insert.StartDate = price.Start;
                    insert.EndDate = price.End;
                    insert.QtyMinimum = price.BreakQty;
                    insert.AlwaysUseThisDiscount = true;
                    insert.AllowPriceOverride = false;
                    insert.Active = true;
                    insert.CreatedUserID = PricingSubmission.User().UserID;
                    insert.LastUpdatedUserID = PricingSubmission.User().UserID;
                    insert.CreatedDate = DateTime.Now;
                    insert.LastUpdatedDate = DateTime.Now;
                    insert.DiscountPriceTransactionCurrency = price.Value;
                    context.db.PriceDiscountCustomerPriceClassItems.InsertOnSubmit(insert);
                    Changes = true;
                }
            }
        }
    }


    void process(HashSet<CustomerPrice> customerPrices)
    {
        foreach (var price in customerPrices)
        {
            Division div = get_div(price.Customer());
            DateTime project_start = price.Start;
            DateTime? project_end = price.End;
            Project project = get_project(price.ProjectName, div, project_start, project_end);
            var customer = price.Customer();
            var currency = price.Currency();
            var item = price.Item();
            var item_packaging = item.ItemPackagings.First(ip => ip.ConversionUnits == item.ItemPackagings.Max(s => s.ConversionUnits));
            var existing_price = context.db.PriceDiscountCustomerItems.FirstOrDefault(p =>
                    p.Item == item
                    &&
                    p.Customer == customer
                    &&
                    p.DiscountPriceTransactionCurrency == price.Value
                    &&
                    p.StartDate == price.Start
                    &&
                    p.Currency == currency
                    &&
                    p.ItemPackaging == item_packaging
                    &&
                    p.QtyMinimum == price.BreakQty
                );
            bool exists_already = existing_price != null;

            if (exists_already)
            {
                if (existing_price.EndDate != price.End)
                {
                    string.Format("Correcting EndDate for {0}.", price.ItemCode).Dump();
                    existing_price.EndDate = price.End;
                    existing_price.LastUpdatedDate = DateTime.Now;
                    Changes = true;
                }
            }
            else
            {
                var existing_to_expire = context.db.PriceDiscountCustomerItems.Where(p =>
                        p.Customer == customer
                        &&
                        p.Item == item
                        &&
                        (
                            p.QtyMinimum == price.BreakQty
                            ||
                            (p.QtyMinimum == 0 && price.BreakQty == 1)
                            ||
                            (p.QtyMinimum == 1 && price.BreakQty == 0)
                        )
                        &&
                        p.Currency == currency
                        &&
                        p.StartDate <= DateTime.Today
                        &&
                        (p.EndDate == null || p.EndDate >= DateTime.Today)
                    );
                foreach (var e in existing_to_expire)
                {
                    e.EndDate = DateTime.Today.AddDays(-1);
                    string.Format("Expiring price record for {0}.", price.ItemCode).Dump();
                    Changes = true;
                }
                string.Format("Creating new price record for {0}.", price.ItemCode).Dump();
                var insert = new PriceDiscountCustomerItem();
                insert.Company = customer.Company;
                insert.Division = customer.CustomerDivisions.First().Division;
                insert.Currency = currency;
                insert.Item = item;
                insert.Customer = customer;
                insert.ItemPackaging = item_packaging;
                insert.StartDate = price.Start;
                insert.EndDate = price.End;
                insert.QtyMinimum = price.BreakQty;
                insert.AlwaysUseThisDiscount = false;
                insert.AllowPriceOverride = true;
                insert.Active = true;
                insert.CreatedUserID = PricingSubmission.User().UserID;
                insert.LastUpdatedUserID = PricingSubmission.User().UserID;
                insert.CreatedDate = DateTime.Now;
                insert.LastUpdatedDate = DateTime.Now;
                insert.DiscountPriceTransactionCurrency = price.Value;
                insert.Project = project;
                context.db.PriceDiscountCustomerItems.InsertOnSubmit(insert);
                Changes = true;
            }
        }
    }

    Division get_div(EntityClassificationCategory price_class)
    {
        return get_customers(price_class: price_class).First().CustomerDivisions.First().Division;
    }

    IQueryable<Customer> get_customers(EntityClassificationCategory price_class)
    {
        return context.db.Customers.Where(c => c.CustomerConfigurationValues.Any(s =>
            s.EntityTypeConfiguration.ConfigurationDisplayName == "Customer Price Class"
            &&
            s.ConfigurationValue != null
            &&
            context.db.EntityClassificationCategories.FirstOrDefault(e =>
                    e.EntityClassificationCategoryID.ToString() == s.ConfigurationValue).EntityClassificationCategoryDisplayName == price_class.EntityClassificationCategoryDisplayCode
                )
            );
    }

    Division get_div(Customer customer)
    {
        return customer.CustomerDivisions.First().Division;
    }

    Project get_project(string project_name, Division div, DateTime project_start, DateTime? project_end)
    {
        if (project_name == null || project_name == "")
            return null;
        Project existing_project = context.db.Projects.FirstOrDefault(p => p.ProjectName == project_name);
        if (existing_project != null)
            return existing_project;
        var insert = new Project();
        insert.Company = div.Company;
        insert.Division = div;
        insert.ProjectName = project_name;
        insert.ProjectReference = null;
        insert.ProjectDescription = null;
        insert.EntityClassificationTypeID = 10115;
        insert.EntityClassificationCategoryID = 12322;
        insert.EntityTypeTransactionStatusID = 1012;
        insert.ScheduledStartDate = project_start;
        insert.ScheduledEndDate = project_end;
        insert.CreatedUserID = this.PricingSubmission.User().UserID;
        insert.LastUpdatedUserID = this.PricingSubmission.User().UserID;
        insert.CreatedDate = DateTime.Now;
        insert.LastUpdatedDate = DateTime.Now;
        context.db.Projects.InsertOnSubmit(insert);
        Changes = true;
        apply();
        return insert;
    }

    void process(HashSet<ListPriceBreak> listPriceBreaks)
    {
        foreach (var price in listPriceBreaks)
        {
            Division div = context.db.Divisions.First(f => f.DivisionID == 1);
            DateTime project_start = price.Start;
            DateTime? project_end = price.End;
            Project project = get_project(price.ProjectName, div, project_start, project_end);
            var currency = price.Currency();
            var item = price.Item();
            var item_packaging = item.ItemPackagings.First(ip => ip.ConversionUnits == item.ItemPackagings.Max(s => s.ConversionUnits));
            var existing_price = context.db.PriceDiscountItems.FirstOrDefault(p =>
                    p.Item == item
                    &&
                    p.DiscountPriceTransactionCurrency == price.Value
                    &&
                    p.StartDate == price.Start
                    &&
                    p.Currency == currency
                    &&
                    p.ItemPackaging == item_packaging
                    &&
                    p.QtyMinimum == price.BreakQty
                );
            bool exists_already = existing_price != null;

            if (exists_already)
            {
                if (existing_price.EndDate != price.End)
                {
                    string.Format("Correcting EndDate for {0}.", price.ItemCode).Dump();
                    existing_price.EndDate = price.End;
                    existing_price.LastUpdatedDate = DateTime.Now;
                    Changes = true;
                }
            }
            else
            {
                var existing_to_expire = context.db.PriceDiscountItems.Where(p =>
                        p.Item == item
                        &&
                        (
                            p.QtyMinimum == price.BreakQty
                            ||
                            (p.QtyMinimum == 0 && price.BreakQty == 1)
                            ||
                            (p.QtyMinimum == 1 && price.BreakQty == 0)
                        )
                        &&
                        p.Currency == currency
                        &&
                        p.StartDate <= DateTime.Today
                        &&
                        (p.EndDate == null || p.EndDate >= DateTime.Today)
                    );
                foreach (var e in existing_to_expire)
                {
                    e.EndDate = DateTime.Today.AddDays(-1);
                    e.LastUpdatedDate = DateTime.Now;
                    e.LastUpdatedUserID = PricingSubmission.User().UserID;
                    string.Format("Expiring price record for {0}.", price.ItemCode).Dump();
                    Changes = true;
                }
                string.Format("Creating new price record for {0}.", price.ItemCode).Dump();
                var insert = new PriceDiscountItem();
                insert.CompanyID = 1;
                insert.DivisionID = 1;
                insert.Currency = currency;
                insert.Item = item;
                insert.ItemPackaging = item_packaging;
                insert.StartDate = price.Start;
                insert.EndDate = price.End;
                insert.QtyMinimum = price.BreakQty;
                insert.AlwaysUseThisDiscount = false;
                insert.AllowPriceOverride = true;
                insert.Active = true;
                insert.CreatedUserID = PricingSubmission.User().UserID;
                insert.LastUpdatedUserID = PricingSubmission.User().UserID;
                insert.CreatedDate = DateTime.Now;
                insert.LastUpdatedDate = DateTime.Now;
                insert.DiscountPriceTransactionCurrency = price.Value;
                insert.Project = project;
                context.db.PriceDiscountItems.InsertOnSubmit(insert);
                Changes = true;
            }
        }
    }

    void process(HashSet<ListPrice> listPrices)
    {
        foreach (var price in listPrices)
        {
            var existing = context.db.PriceItems.FirstOrDefault(p =>
            p.CompanyID == 1
            &&
            p.DivisionID == 1
            &&
            p.Item == price.Item()
            &&
            p.CurrencyID == 1
            &&
            p.Active
            &&
            p.SellPriceTransactionCurrency == price.Value
            &&
            p.StartDate == price.Start
            &&
            p.EndDate == price.End
        );
            var incorrect_records = context.db.PriceItems.Where(p =>
                    p.CompanyID == 1
                    &&
                    p.DivisionID == 1
                    &&
                    p.Item == price.Item()
                    &&
                    p.CurrencyID == 1
                    &&
                    p.Active
                    &&
                    p.StartDate <= DateTime.Today
                    &&
                    (p.EndDate == null || p.EndDate >= DateTime.Today)
                );
            foreach (var incorrect_record in incorrect_records) if (existing != incorrect_record)
                {
                    incorrect_record.EndDate = price.Start.AddDays(-1);
                    Changes = true;
                    $"Marking as expiring/expired on {incorrect_record.Item.ItemCode}.".Dump();
                }
            if (existing == null)
            {
                var insert = new PriceItem();
                insert.CompanyID = 1;
                insert.Item = price.Item();
                insert.DivisionID = 1;
                insert.CurrencyID = 1;
                insert.ItemPackaging = insert.Item.ItemPackagings.OrderByDescending(i => i.ConversionUnits).First();
                insert.StartDate = price.Start;
                insert.EndDate = price.End;
                insert.PriceBasisEntityClassificationTypeID = null;
                insert.SellPriceTransactionCurrency = price.Value;
                insert.MarkUpPrice = false;
                insert.MinimumMargin = null;
                insert.MaterialMarkup = null;
                insert.LabourMarkup = null;
                insert.ServiceMarkup = null;
                insert.MinimumOrderQty = price.MinOrder;
                insert.AllowPriceOverride = true;
                insert.CreatedDate = DateTime.Now;
                insert.LastUpdatedDate = DateTime.Now;
                insert.CreatedUserID = this.PricingSubmission.User().UserID;
                insert.LastUpdatedUserID = this.PricingSubmission.User().UserID;
                insert.Active = true;
                context.db.PriceItems.InsertOnSubmit(insert);
                Changes = true;
                string.Format("Added List Price for {0}.", price.ItemCode).Dump();
            }
            else
            {
                if (existing.SellPriceTransactionCurrency != price.Value || existing.Active == false)
                {
                    existing.SellPriceTransactionCurrency = price.Value;
                    existing.LastUpdatedDate = DateTime.Now;
                    existing.LastUpdatedUserID = PricingSubmission.User().UserID;
                    existing.Active = true;
                    Changes = true;
                    string.Format("Adjusted List Price for {0}.", price.ItemCode).Dump();
                }
            }
        }
    }
    void apply()
    {
        if (this.Changes)
        {
            var caches = context.db.EntityTypeCaches.Where(etc => etc.EntityType.EntityTypeFullName.Contains("Pricing") || etc.EntityType.EntityTypeFullName.Contains("Project"));
            foreach (var cache in caches)
            {
                cache.LastEntityStoreDeleteTime = DateTime.Now;
                cache.LastUpdatedDate = DateTime.Now;
                cache.LastUpdatedUserID = PricingSubmission.User().UserID;
            }
            "Applying changes...".Dump();
            context.db.SubmitChanges();
            "Done.".Dump();
        }
        else
        {
            "No change.".Dump();
        }
    }
}
