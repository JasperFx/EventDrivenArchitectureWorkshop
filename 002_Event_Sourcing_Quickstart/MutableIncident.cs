using Marten.Events;

namespace EventSourcingDemo;

public class MutableIncident
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public IncidentStatus Status { get; set; }
    public IncidentNote[] Notes { get; set; }
    public IncidentCategory? Category { get; set; }
    public IncidentPriority? Priority { get; set; }
    public Guid? AgentId { get; set; }

    public int Version { get; set; }

    // Make your life easier and do this for easy serialization
    public MutableIncident()
    {
    }

    public MutableIncident(IEvent<IncidentLogged> logged)
    {
        Id = logged.StreamId; // this isn't strictly necessary
        CustomerId = logged.Data.CustomerId;
        Status = IncidentStatus.Pending;
        Notes = Array.Empty<IncidentNote>();
    }

    public void Apply(IncidentCategorised categorised) 
        => Category = categorised.Category;

    public void Apply(IncidentPrioritised prioritised) 
        => Priority = prioritised.Priority;

    public void Apply(AgentAssignedToIncident prioritised) =>
        AgentId = prioritised.AgentId;

    public void Apply(IncidentResolved resolved) 
        => Status = IncidentStatus.Resolved;

    public void Apply(ResolutionAcknowledgedByCustomer acknowledged) 
        => Status = IncidentStatus.ResolutionAcknowledgedByCustomer;

    public void Apply(IncidentClosed closed) 
        => Status = IncidentStatus.Closed;
}