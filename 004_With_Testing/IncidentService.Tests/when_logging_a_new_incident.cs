using System.Security.Claims;
using Alba;
using Shouldly;

namespace IncidentService.Tests;

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

            x.Post.Json(new LogIncident(BaselineData.Customer1Id, theContact, "Oven won't light"));
            x.StatusCodeShouldBeOk();
        });

        theNewIncidentId = result.ReadAsJson<Guid>();
    }

    [Fact]
    public async Task should_have_logged_a_new_incident_created_event()
    {
        await using var session = theStore.LightweightSession();

        var stream = await session.Events.FetchStreamAsync(theNewIncidentId);
        var logged = stream.Single().ShouldBeOfType<IncidentLogged>();
        
        // Remember that .NET records build in their own equality
        // We *could* do a ShouldBe(new IncidentLogged()) here, but I think the code
        // is ugly doing that
        logged.CustomerId.ShouldBe(BaselineData.Customer1Id);
        logged.Contact.ShouldBe(theContact);
        logged.LoggedBy.ShouldBe(theUserId);
    }


    [Fact]
    public async Task new_incident_projected_document_is_created()
    {
        await using var session = theStore.LightweightSession();
        
        var incident = await session.LoadAsync<IncidentDetails>(theNewIncidentId);
        incident.Status.ShouldBe(IncidentStatus.Pending);
        incident.CustomerId.ShouldBe(BaselineData.Customer1Id);
        
        // More assertions maybe...
    }
}