<Query Kind="Program">
  <Connection>
    <ID>98af8b0c-87dd-4eea-ae95-9cb5384fcc1c</ID>
    <Persist>true</Persist>
    <Server>fm-sql-01.fairmont.local\FAIRMONTSQL</Server>
    <IsProduction>true</IsProduction>
    <Database>AwareNewFairmont</Database>
  </Connection>
  <Namespace>Xunit</Namespace>
</Query>

#load "xunit"
/*
Written by Alex Hamilton-Smith on 2022-09-28.

Global assumption: pricing is always set on largest ItemPackaging per item.
If this is ever not true, would require major revision.

*/
void Main()
{
    context.db = this;
    RunTests();  // Call RunTests() or press Alt+Shift+T to initiate testing.
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
    public PricingSubmission(string SubmittedUserName, bool TestOnly=false)
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
        Assert.True(this.Item() != null);
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
    public ListPriceBreak() {}
    public ListPriceBreak(string itemcode, decimal value, DateTime start, DateTime? end, int break_qty, string project_name, string currency_code)
    {
        this.ItemCode = itemcode;
        this.Value = value;
        this.Start = start;
        this.End = end;
        this.BreakQty = break_qty;
        this.ProjectName = project_name;
        this.CurrencyCode = currency_code;
        Assert.True(this.Item() != null);
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
    public CustomerPrice() {}
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
        Assert.True(this.Item() != null);
        Assert.True(this.Customer() != null);
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
    public PriceClassPrice() {}
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
        Assert.True(this.Item() != null);
        Assert.True(this.PriceClass() != null);
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

#region private::Tests

[Fact] void ListPricesCanCreateEmpty() => Assert.True(new PricingSubmission("AlexHS").ListPrices.Count() == 0);
[Fact] void ListPriceBreaksCanCreateEmpty() => Assert.True(new PricingSubmission("Bec").ListPriceBreaks.Count() == 0);
[Fact] void CustomerPricesCanCreateEmpty() => Assert.True(new PricingSubmission("Erosha").CustomerPrices.Count() == 0);
[Theory]
[InlineData("DCC8007")]
[InlineData("LISS4122")]
[InlineData("MIX1001")]
void IsItem(string item)
{
    var basic_price = new ListPrice(itemcode: item, value: 1m, start: DateTime.Now, end: null);
    Assert.True(basic_price.Item() != null);
}
[Theory]
[InlineData("3545")]
[InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
[InlineData("")]
void IsNotItem(string item)
{
    var basic_price = new ListPrice(itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null);
    basic_price.ItemCode = item;
    Assert.Null(basic_price.Item());
}
[Theory]
[InlineData("ACT001")]
void IsCustomer(string customer)
{
    var basic_price = new CustomerPrice(customer_code: customer, itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null, break_qty: 0, project_name: null, currency_code: "AUD");
    Assert.True(basic_price.Customer() != null);
}
[Theory]
[InlineData("")]
[InlineData("xxxxxxxxxx")]
[InlineData("ACT001_")]
void IsNotCustomer(string customer)
{
    var basic_price = new CustomerPrice(customer_code: "ACT001", itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null, break_qty: 0, project_name: null, currency_code: "AUD");
    basic_price.CustomerCode = customer;
    Assert.Null(basic_price.Customer());
}
[Theory]
[InlineData("Cura Group")]
[InlineData("Ramsay Health")]
[InlineData("St John of God")]
void IsPriceClass(string price_class)
{
    var basic_price = new PriceClassPrice(price_class_name: price_class, itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null, break_qty: 0, project_name: null, currency_code: "AUD");
    Assert.True(basic_price.PriceClass() != null);
}
[Theory]
[InlineData("")]
[InlineData("xxxxxxxxxx")]
[InlineData("ACT001_")]
void IsNotPriceClass(string price_class)
{
    var basic_price = new PriceClassPrice(price_class_name: "Cura Group", itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null, break_qty: 0, project_name: null, currency_code: "AUD");
    basic_price.PriceClassName = price_class;
    Assert.Null(basic_price.PriceClass());
}
[Theory]
[InlineData("PA1221")]
[InlineData("BPO900093")]
[InlineData("CON_728")]
void IsProject(string project)
{
    var basic_price = new PriceClassPrice(price_class_name: "Cura Group", itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null, break_qty: 0, project_name: project, currency_code: "AUD");
    Assert.True(basic_price.Project() != null);
}
[Theory]
[InlineData("xxxxxxxxxxxxxxxxxxxxx")]
[InlineData("")]
[InlineData("NaN")]
void IsNotProject(string project)
{
    var basic_price = new PriceClassPrice(price_class_name: "Cura Group", itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null, break_qty: 0, project_name: "PA1221", currency_code: "AUD");
    basic_price.ProjectName = project;
    Assert.Null(basic_price.Project());
}
[Theory]
[InlineData("AlexHS")]
void IsUser(string user_name)
{
    var basic_sub = new PricingSubmission("AlexHS");
    basic_sub.SubmittedUserName = user_name;
    Assert.True(basic_sub.User() != null);
}
[Theory]
[InlineData("")]
void IsNotUser(string user_name)
{
    var basic_sub = new PricingSubmission("Richard");
    basic_sub.SubmittedUserName = user_name;
    Assert.Null(basic_sub.User());
}
[Theory]
[InlineData("Wales Health Board", "GBP")]
[InlineData("Samples UK", "GBP")]
[InlineData("Macquarie Health", "AUD")]
void PriceClassCorrectCurrency(string correct_class_name, string correct_currency_code)
{
    var basic_price = new PriceClassPrice(price_class_name: correct_class_name, itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null, break_qty: 0, project_name: null, currency_code: null);
    basic_price.CurrencyCode = basic_price.Currency().CurrencyCode;
    $"{correct_class_name} {basic_price.Currency().CurrencyCode}".Dump();
    Assert.True(basic_price.CurrencyCode == correct_currency_code);
}
[Theory]
[InlineData("BET001", "GBP")]
[InlineData("ACT001", "AUD")]
void PriceCustomerCorrectCurrency(string cust_code, string correct_currency_code)
{
    var basic_price = new CustomerPrice(customer_code: cust_code, itemcode: "MIX1001", value: 1m, start: DateTime.Now, end: null, break_qty: 0, project_name: null, currency_code: "AUD");
    Assert.True(basic_price.Customer() != null);
    basic_price.CurrencyCode = correct_currency_code;
    Assert.True(basic_price.CurrencyCode == correct_currency_code);
}
#endregion