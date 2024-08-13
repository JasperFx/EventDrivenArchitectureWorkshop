using Marten.Events;
using Marten.Events.Aggregation;

public record IncidentDetails(
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
// IncidentDetails view from the raw event data
public class IncidentDetailsProjection : SingleStreamProjection<IncidentDetails>
{
    public static IncidentDetails Create(IEvent<IncidentLogged> logged)
    {
        return new IncidentDetails(logged.StreamId, logged.Data.CustomerId, IncidentStatus.Pending,
            Array.Empty<IncidentNote>());
    }

    public IncidentDetails Apply(IncidentCategorised categorised, IncidentDetails current)
    {
        return current with { Category = categorised.Category };
    }

    public IncidentDetails Apply(IncidentPrioritised prioritised, IncidentDetails current)
    {
        return current with { Priority = prioritised.Priority };
    }

    public IncidentDetails Apply(AgentAssignedToIncident prioritised, IncidentDetails current)
    {
        return current with { AgentId = prioritised.AgentId };
    }

    public IncidentDetails Apply(IncidentResolved resolved, IncidentDetails current)
    {
        return current with { Status = IncidentStatus.Resolved };
    }

    public IncidentDetails Apply(ResolutionAcknowledgedByCustomer acknowledged, IncidentDetails current)
    {
        return current with { Status = IncidentStatus.ResolutionAcknowledgedByCustomer };
    }

    public IncidentDetails Apply(IncidentClosed closed, IncidentDetails current)
    {
        return current with { Status = IncidentStatus.Closed };
    }
}