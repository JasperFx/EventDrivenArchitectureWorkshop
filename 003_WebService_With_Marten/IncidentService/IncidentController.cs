using Marten;
using Marten.AspNetCore;
using Microsoft.AspNetCore.Mvc;

public record LogIncident(
    Guid CustomerId,
    Contact Contact,
    string Description
);

public class IncidentController : ControllerBase
{
    private readonly IDocumentSession _session;

    public IncidentController(IDocumentSession session)
    {
        _session = session;
    }

    [HttpPost("/api/incidents")]
    public async Task<IResult> Log(
        [FromBody] LogIncident command
        )
    {
        // Let's come back to this one in a bit...
        var userId = Guid.NewGuid();
        
        var logged = new IncidentLogged(command.CustomerId, command.Contact, command.Description, userId);

        var incidentId = _session.Events.StartStream(logged).Id;
        await _session.SaveChangesAsync(HttpContext.RequestAborted);

        return Results.Created("/incidents/" + incidentId, incidentId);
    }

    [HttpGet("/api/incidents/{incidentId}")]
    public Task<IncidentDetails> Get(Guid incidentId)
    {
        return _session.LoadAsync<IncidentDetails>(incidentId);
    }

    [HttpGet("/api/incidents/pending")]
    public Task<IReadOnlyList<IncidentDetails>> GetOpenIncidents()
    {
        return _session
            .Query<IncidentDetails>()
            .Where(x => x.Status == IncidentStatus.Pending)
            .ToListAsync();
    }

}