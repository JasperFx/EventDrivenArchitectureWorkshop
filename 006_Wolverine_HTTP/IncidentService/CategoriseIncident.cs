using System.Text.Json.Serialization;
using FluentValidation;
using Marten.Schema;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

namespace IncidentService;

public class CategoriseIncident
{
    [JsonPropertyName("Id")] [Identity] public Guid Id { get; set; }

    [JsonPropertyName("Category")] public IncidentCategory Category { get; set; }

    [JsonPropertyName("Version")]
    // This is to communicate to the server that
    // this command was issued assuming that the 
    // incident is currently at this revision
    // number
    public int Version { get; set; }

    public class Validator : AbstractValidator<CategoriseIncident>
    {
        public Validator()
        {
            RuleFor(x => x.Version).GreaterThan(0);
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}

public static class CategoriseIncidentEndpoint
{
    [WolverinePost("/api/incidents/categorise")]
    [AggregateHandler]
    // Any object in the OutgoingMessages collection will
    // be treated as a "cascading message" to be published by
    // Wolverine after the original CategoriseIncident command
    // is successfully completed
    public static (Events, OutgoingMessages) Post(
        CategoriseIncident command,
        Incident existing,
        User user)
    {
        var events = new Events();
        var messages = new OutgoingMessages();

        if (existing.Category != command.Category)
        {
            events += new IncidentCategorised
            {
                Category = command.Category,
                UserId = user.Id
            };

            // Send a command message to try to assign the priority
            messages.Add(new TryAssignPriority
            {
                IncidentId = existing.Id,
                UserId = user.Id
            });
        }

        return (events, messages);
    }
}

public static class CategoriseIncidentHandler
{
    public static readonly Guid SystemId = Guid.NewGuid();

    [AggregateHandler]
    // The object? as return value will be interpreted
    // by Wolverine as appending one or zero events
    public static async Task<object?> Handle(
        CategoriseIncident command,
        Incident existing,
        IMessageBus bus)
    {
        if (existing.Category != command.Category)
        {
            // Send the message to any and all subscribers to this message
            await bus.PublishAsync(new TryAssignPriority { IncidentId = existing.Id });
            return new IncidentCategorised
            {
                Category = command.Category,
                UserId = SystemId
            };
        }

        // Wolverine will interpret this as "do no work"
        return null;
    }
}