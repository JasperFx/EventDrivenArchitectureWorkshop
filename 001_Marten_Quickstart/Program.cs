// See https://aka.ms/new-console-template for more information

using JasperFx.Core;
using Marten;
using Newtonsoft.Json;

await using var store = DocumentStore
    .For("Host=localhost;Port=5432;Database=marten_testing;Username=postgres;password=postgres");

var customer = new Customer
{
    Duration = new ContractDuration(new DateOnly(2023, 12, 1), new DateOnly(2024, 12, 1)),
    Region = "West Coast",
    Priorities = new()
    {
        { IncidentCategory.Database, IncidentPriority.High }
    }
};

await using var session = store.LightweightSession();
session.Store(customer);
await session.SaveChangesAsync();

// Marten assigned an identity for us on Store(), so 
// we'll use that to load another copy of what was 
// just saved
var customer2 = await session.LoadAsync<Customer>(customer.Id);

// Just making a pretty JSON printout
Console.WriteLine(JsonConvert.SerializeObject(customer2, Formatting.Indented));
    


public class Customer
{
    public Guid Id { get; set; }

    // We'll use this later for some "logic" about how incidents
    // can be automatically prioritized
    public Dictionary<IncidentCategory, IncidentPriority> Priorities { get; set; }
        = new();
    
    public string? Region { get; set; }
    
    public ContractDuration Duration { get; set; } 
}

public record ContractDuration(DateOnly Start, DateOnly End);

public enum IncidentCategory
{
    Software,
    Hardware,
    Network,
    Database
}

public enum IncidentPriority
{
    Critical,
    High,
    Medium,
    Low
}



