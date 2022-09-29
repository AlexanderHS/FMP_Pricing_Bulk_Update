<Query Kind="Program">
  <Connection>
    <ID>98af8b0c-87dd-4eea-ae95-9cb5384fcc1c</ID>
    <Persist>true</Persist>
    <Server>fm-sql-01.fairmont.local\FAIRMONTSQL</Server>
    <IsProduction>true</IsProduction>
    <Database>AwareNewFairmontTraining</Database>
  </Connection>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>Xunit</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#load "xunit"
#load ".\Prices"

static class WebConfig
{
    public const int port = 8144;
    public const string ip = "192.168.0.14";
 }

void Main()
{
 
    context.db = this;
    RunTests();
    return;

    var pricing = new PricingSubmission("Juliet");
    pricing.ListPrices.Add(new ListPrice(
            itemcode: "LISS4122",
            value: 129.2m,
            start: DateTime.Now,
            end: null
        ));
    //
    //    pricing.ListPrices.Add(new ListPrice(
    //          itemcode: "LISS4121",
    //          value: 12.97m,
    //          start: DateTime.Now,
    //          end: null
    //        ));
    //post(pricing).Result.Dump();
    //return;

    //pricing.CustomerPrices.Add(new CustomerPrice(
    //        customer_code: "ACT001",
    //        itemcode: "MIX1001",
    //        value: 420m,
    //        start: DateTime.Today.AddDays(-40),
    //        end: null,
    //        break_qty: 0,
    //        project_name: null,
    //        currency_code: "AUD"
    //      ));

    //pricing.ListPriceBreaks.Add(new ListPriceBreak(
    //        itemcode: "N-8140-002",
    //        value: 126.47m,
    //        start: DateTime.Today.AddDays(-40),
    //        end: null,
    //        break_qty: 10,
    //        project_name: "ABC123",
    //        currency_code: "AUD"
    //      ));


    post(pricing).Result.Dump();
}

async Task<string> post(PricingSubmission pricingSubmission)
{
    string myJson = JsonSerializer.Serialize(pricingSubmission);
    using (var client = new HttpClient())
    {
        var response = await client.PostAsync(
            $"http://{WebConfig.ip}:{WebConfig.port}",
             new StringContent(myJson, Encoding.UTF8, "application/json"));
        var answer = await response.Content.ReadAsStringAsync();
        return answer;
    }
}

#region private::Tests
[Fact]
void PostReturnsSuccessList()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    pricing.ListPrices.Add(new ListPrice(
            itemcode: "MIX1001",
            value: 55m,
            start: DateTime.Now,
            end: null
          ));
    Assert.True(post(pricing).Result == "SUCCESS");
}
[Fact]
void PostReturnsSuccessListBreak()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    pricing.ListPriceBreaks.Add(new ListPriceBreak(
            itemcode: "MIX1001",
            value: 55m,
            start: DateTime.Now,
            end: null,
            break_qty: 1,
            project_name: null,
            currency_code: null
          ));
    Assert.True(post(pricing).Result == "SUCCESS");
}
[Fact]
void PostReturnsSuccessCustomerPrice()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    pricing.CustomerPrices.Add(new CustomerPrice(
            customer_code: "ACT001",
            itemcode: "MIX1001",
            value: 55m,
            start: DateTime.Now,
            end: null,
            break_qty: 1,
            project_name: null,
            currency_code: null
          ));
    Assert.True(post(pricing).Result == "SUCCESS");
}
[Fact]
void PostReturnsSuccessPriceClassPrice()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    pricing.PriceClassPrices.Add(new PriceClassPrice(
            price_class_name: "Cura Group",
            itemcode: "MIX1001",
            value: 55m,
            start: DateTime.Now,
            end: null,
            break_qty: 1,
            project_name: null,
            currency_code: null
          ));
    Assert.True(post(pricing).Result == "SUCCESS");
}

[Fact]
void UserValid()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    pricing.PriceClassPrices.Add(new PriceClassPrice(
            price_class_name: "Cura Group",
            itemcode: "MIX1001",
            value: 55m,
            start: DateTime.Now,
            end: null,
            break_qty: 1,
            project_name: null,
            currency_code: null
          ));
    Assert.True(pricing.User() != null);
}
[Fact]
void CanSerialize()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    string myJson = JsonSerializer.Serialize(pricing);
    var reconstructed = JsonSerializer.Deserialize<PricingSubmission>(myJson);
    Assert.Equal(pricing.SubmittedUserName, reconstructed.SubmittedUserName);
    Assert.Equal(pricing.ListPriceBreaks.Count(), reconstructed.ListPriceBreaks.Count());
}
[Fact]
void CanSerializeWithList()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    pricing.ListPrices.Add(new ListPrice(
        itemcode: "MIX1001",
        value: 55m,
        start: DateTime.Now,
        end: null
      ));
    string myJson = JsonSerializer.Serialize(pricing);
    var reconstructed = JsonSerializer.Deserialize<PricingSubmission>(myJson);
    Assert.Equal(pricing.SubmittedUserName, reconstructed.SubmittedUserName);
    Assert.Equal(pricing.ListPrices.Count(), reconstructed.ListPrices.Count());
}

[Fact]
void CanSerializeWithListBreak()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    pricing.ListPriceBreaks.Add(new ListPriceBreak(
        itemcode: "MIX1001",
        value: 55m,
        start: DateTime.Now,
        end: null,
        break_qty: 1,
        project_name: null,
        currency_code: "AUD"
      ));
    string myJson = JsonSerializer.Serialize(pricing);
    var reconstructed = JsonSerializer.Deserialize<PricingSubmission>(myJson);
    Assert.Equal(pricing.SubmittedUserName, reconstructed.SubmittedUserName);
    Assert.Equal(pricing.ListPriceBreaks.Count(), reconstructed.ListPriceBreaks.Count());
    Assert.Equal(pricing.ListPriceBreaks.First().Value, reconstructed.ListPriceBreaks.First().Value);
}

[Fact]
void CanSerializeWithCustomerPrice()
{
    var pricing = new PricingSubmission("AlexHS", TestOnly: true);
    pricing.CustomerPrices.Add(new CustomerPrice(
        customer_code: "ACT001",
        itemcode: "MIX1001",
        value: 55m,
        start: DateTime.Now,
        end: null,
        break_qty: 1,
        project_name: null,
        currency_code: "AUD"
      ));
    string myJson = JsonSerializer.Serialize(pricing);
    var reconstructed = JsonSerializer.Deserialize<PricingSubmission>(myJson);
    Assert.Equal(pricing.SubmittedUserName, reconstructed.SubmittedUserName);
    Assert.Equal(pricing.CustomerPrices.Count(), reconstructed.CustomerPrices.Count());
}

#endregion