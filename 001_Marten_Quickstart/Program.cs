// See https://aka.ms/new-console-template for more information

using JasperFx.Core;
using Marten;
using Newtonsoft.Json;

await using var store = DocumentStore
    .For("Host=localhost;Port=5432;Database=marten_testing;Username=postgres;password=postgres");

var order = new Order
{
    ShippingAddress = new Address
    {
        Address1 = "21 Cherry Tree Lane",
        City = "New London",
        StateOrProvince = "CT",
        PostalCode = "11111"
    },
    BillingAddress = new Address
    {
        Address1 = "21 Cherry Tree Lane",
        City = "New London",
        StateOrProvince = "CT",
        PostalCode = "11111"
    },
    OrderDate = DateTimeOffset.UtcNow,
    Items =
    [
        new OrderItem { LineNumber = 1, PartNumber = "10XFX", Quantity = 10 },
        new OrderItem { LineNumber = 2, PartNumber = "20XFX", Quantity = 20 },
    ]

};

await using var session = store.LightweightSession();
session.Store(order);
await session.SaveChangesAsync();

// Marten assigned an identity for us on Store(), so 
// we'll use that to load another copy of what was 
// just saved
var order2 = await session.LoadAsync<Order>(order.Id);

// Just making a pretty JSON printout
Console.WriteLine(JsonConvert.SerializeObject(order2, Formatting.Indented));
    


public class Order
{
    public Guid Id { get; set; }
    
    public Address ShippingAddress { get; set; }
    public Address BillingAddress { get; set; }
    
    public DateTimeOffset OrderDate { get; set; }
    public DateTimeOffset? ShipDate { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int LineNumber { get; set; }
    public string PartNumber { get; set; }
    public int Quantity { get; set; }
}

public class Address
{
    public Address()
    {
    }

    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string StateOrProvince { get; set; }
    public string Country { get; set; }
    public string PostalCode { get; set; }

    public bool Primary { get; set; }
    public string Street { get; set; }
    public string HouseNumber { get; set; }
}


