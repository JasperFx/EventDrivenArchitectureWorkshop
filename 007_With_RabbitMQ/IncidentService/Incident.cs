using Marten.Events;
using Marten.Events.Aggregation;

public record Incident(
    Guid Id,
    Guid CustomerId,
    IncidentStatus Status,
    IncidentNote[] Notes,
    IncidentCategory? Category = null,
    IncidentPriority? Priority = null,
    Guid? AgentId = null,
    
    // This is meant to be the revision number
    // of the event stream for this incident
    int Version = 1
);

public record IncidentNote(
    IncidentNoteType Type,
    Guid From,
    string Content,
    bool VisibleToCustomer
);

public enum IncidentNoteType
{
    FromAgent,
    FromCustomer
}

// This class contains the directions for Marten about how to create the
// Incident view from the raw event data
public class IncidentProjection: SingleStreamProjection<Incident>
{
    public static Incident Create(IEvent<IncidentLogged> logged) =>
        new(logged.StreamId, logged.Data.CustomerId, IncidentStatus.Pending, Array.Empty<IncidentNote>());

    public Incident Apply(IncidentCategorised categorised, Incident current) =>
        current with { Category = categorised.Category };

    public Incident Apply(IncidentPrioritised prioritised, Incident current) =>
        current with { Priority = prioritised.Priority };

    public Incident Apply(AgentAssignedToIncident prioritised, Incident current) =>
        current with { AgentId = prioritised.AgentId };

    public Incident Apply(IncidentResolved resolved, Incident current) =>
        current with { Status = IncidentStatus.Resolved };

    public Incident Apply(ResolutionAcknowledgedByCustomer acknowledged, Incident current) =>
        current with { Status = IncidentStatus.ResolutionAcknowledgedByCustomer };

    public Incident Apply(IncidentClosed closed, Incident current) =>
        current with { Status = IncidentStatus.Closed };
}
