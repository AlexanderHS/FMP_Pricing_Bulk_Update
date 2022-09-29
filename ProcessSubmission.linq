<Query Kind="Program">
  <Connection>
    <ID>98af8b0c-87dd-4eea-ae95-9cb5384fcc1c</ID>
    <Persist>true</Persist>
    <Server>fm-sql-01.fairmont.local\FAIRMONTSQL</Server>
    <IsProduction>true</IsProduction>
    <Database>AwareNewFairmontTraining</Database>
  </Connection>
</Query>

#load ".\Prices"
#load "xunit"

void main()
{
    
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
                    insert.CreatedUserID = 227;
                    insert.LastUpdatedUserID = 227;
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
        if (project_name == null ||  project_name == "")
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
                    existing.LastUpdatedUserID = 227;
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
                cache.LastUpdatedUserID = 227;
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

