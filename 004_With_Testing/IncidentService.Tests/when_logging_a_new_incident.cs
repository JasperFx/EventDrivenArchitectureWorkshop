using System.Security.Claims;
using Alba;
using JasperFx.Core.Reflection;
using Marten.Events;
using Shouldly;

namespace IncidentService.Tests;

public class LogIncidentTests : IntegrationContext
{
    [Fact]
    public async Task should_return_400_when_the_customer_does_not_exist()
    {
        var contact = new Contact(ContactChannel.Email, "Han", "Solo");
    
        await Scenario(x =>
        {
            // Alba can help you stub claims during the request
            x.WithClaim(new Claim("user-id", Guid.NewGuid().ToString()));
    
            // This customer does not exist, so this should 400
            x.Post.Json(new LogIncident(Guid.NewGuid(), contact, "Oven won't light"))
                .ToUrl("/api/incidents");

            x.StatusCodeShouldBe(400);
            x.ContentTypeShouldBe("application/problem+json; charset=utf-8");
        });
    }

    public LogIncidentTests(AppFixture fixture) : base(fixture)
    {
    }
}

public class when_logging_a_new_incident_happy_path : IntegrationContext
{
    private Guid theUserId = Guid.NewGuid();
    private Contact theContact;
    private Guid theNewIncidentId;
    
    public when_logging_a_new_incident_happy_path(AppFixture fixture) : base(fixture)
    {
    }
    
    protected override async Task theContextIs()
    {
        theContact = new Contact(ContactChannel.Email, "Han", "Solo");
    
        var result = await Scenario(x =>
        {
            // Alba can help you stub claims during the request
            x.WithClaim(new Claim("user-id", theUserId.ToString()));
    
            x.Post.Json(new LogIncident(BaselineData.Customer1Id, theContact, "Oven won't light"))
                .ToUrl("/api/incidents");
            
            // This endpoint returns 201, empty body
            x.StatusCodeShouldBe(201);
        });
    
        theNewIncidentId = result.ReadAsJson<Guid>();
    }
    
    [Fact]
    public async Task should_have_logged_a_new_incident_created_event()
    {
        await using var session = theStore.LightweightSession();
    
        var stream = await session.Events.FetchStreamAsync(theNewIncidentId);
        var logged = stream.Single().As<IEvent<IncidentLogged>>();
            
        // Remember that .NET records build in their own equality
        // We *could* do a ShouldBe(new IncidentLogged()) here, but I think the code
        // is ugly doing that
        logged.Data.CustomerId.ShouldBe(BaselineData.Customer1Id);
        logged.Data.Contact.ShouldBe(theContact);
        logged.Data.LoggedBy.ShouldBe(theUserId);
    }
    
    
    [Fact]
    public async Task new_incident_projected_document_is_created()
    {
        await using var session = theStore.LightweightSession();
            
        var incident = await session.LoadAsync<Incident>(theNewIncidentId);
        incident.Status.ShouldBe(IncidentStatus.Pending);
        incident.CustomerId.ShouldBe(BaselineData.Customer1Id);
            
        // More assertions maybe...
    }
}



