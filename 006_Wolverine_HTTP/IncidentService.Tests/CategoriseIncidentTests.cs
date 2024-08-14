using IncidentService;
using Shouldly;

public class CategoriseIncidentTests
{
    [Fact]
    public void raise_categorized_event_if_changed()
    {
        var command = new CategoriseIncident
        {
            Category = IncidentCategory.Database
        };

        var details = new Incident(
            Guid.NewGuid(),
            Guid.NewGuid(),
            IncidentStatus.Closed,
            Array.Empty<IncidentNote>(),
            IncidentCategory.Hardware);

        var user = new User(Guid.NewGuid());
        var (events, messages) = CategoriseIncidentEndpoint.Post(command, details, user);

        // There should be one appended event
        var categorised = events.Single()
            .ShouldBeOfType<IncidentCategorised>();

        categorised
            .Category.ShouldBe(IncidentCategory.Database);

        categorised.UserId.ShouldBe(user.Id);

        // And there should be a single outgoing message
        var message = messages.Single()
            .ShouldBeOfType<TryAssignPriority>();

        message.IncidentId.ShouldBe(details.Id);
        message.UserId.ShouldBe(user.Id);
    }
}