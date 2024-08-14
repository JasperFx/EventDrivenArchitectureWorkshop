using Marten.Events;

namespace EventSourcingDemo;

public record IncidentLogged(
    Guid CustomerId,
    Contact Contact,
    string Description,
    Guid LoggedBy
);

public class IncidentCategorised
{
    public IncidentCategory Category { get; set; }
    public Guid UserId { get; set; }
}

public record IncidentPrioritised(IncidentPriority Priority, Guid UserId);

public record AgentAssignedToIncident(Guid AgentId);

public record AgentRespondedToIncident(        
    Guid AgentId,
    string Content,
    bool VisibleToCustomer);

public record CustomerRespondedToIncident(
    Guid UserId,
    string Content
);

public record IncidentResolved(
    ResolutionType Resolution,
    Guid ResolvedBy,
    DateTimeOffset ResolvedAt
);

public record ResolutionAcknowledgedByCustomer(
    Guid IncidentId,
    Guid AcknowledgedBy,
    DateTimeOffset AcknowledgedAt
);

public record IncidentClosed(
    Guid IncidentId,
    Guid ClosedBy,
    DateTimeOffset ClosedAt
);

public enum IncidentStatus
{
    Pending = 1,
    Resolved = 8,
    ResolutionAcknowledgedByCustomer = 16,
    Closed = 32
}

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

public enum ResolutionType
{
    Temporary,
    Permanent,
    NotAnIncident
}

public enum ContactChannel
{
    Email,
    Phone,
    InPerson,
    GeneratedBySystem
}

public record Contact(
    ContactChannel ContactChannel,
    string? FirstName = null,
    string? LastName = null,
    string? EmailAddress = null,
    string? PhoneNumber = null
);


