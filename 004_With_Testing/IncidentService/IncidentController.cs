using Marten;
using Marten.AspNetCore;
using Microsoft.AspNetCore.Authorization;
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
    [ProducesResponseType<Guid>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [Authorize]
    public async Task<IActionResult> Log(
        [FromBody] LogIncident command
        )
    {
        var userId = currentUserId();

        var customer = await _session.LoadAsync<Customer>(command.CustomerId);
        
        if (customer == null) return Problem("Invalid customer", statusCode:400);
        
        var logged = new IncidentLogged(command.CustomerId, command.Contact, command.Description, userId);

        var incidentId = _session.Events.StartStream(logged).Id;
        await _session.SaveChangesAsync(HttpContext.RequestAborted);

        return Created("/incidents/" + incidentId, incidentId);
    }

    private Guid currentUserId()
    {
        // let's say that we do something here that "finds" the
        // user id as a Guid from the ClaimsPrincipal
        var userIdClaim = User.FindFirst("user-id");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var id))
        {
            return id;
        }

        throw new UnauthorizedAccessException("No user");
    }
    
    [HttpGet("/api/incidents/{incidentId}")]
    public Task<Incident> Get(Guid incidentId)
    {
        return _session.LoadAsync<Incident>(incidentId);
    }

    [HttpGet("/api/incidents/pending")]
    public Task<IReadOnlyList<Incident>> GetOpenIncidents()
    {
        return _session
            .Query<Incident>()
            .Where(x => x.Status == IncidentStatus.Pending)
            .ToListAsync();
    }

}