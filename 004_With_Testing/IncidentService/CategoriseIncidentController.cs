using System.Text.Json.Serialization;
using FluentValidation;
using JasperFx.Core;
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace IncidentService;

public class CategoriseIncident
{
    [JsonPropertyName("Id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("Category")]
    public IncidentCategory Category { get; set; }
    
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

public class CategoriseIncidentController : ControllerBase
{
    [HttpPost("/api/incidents/categorise")]
    public async Task<IActionResult> Post(
        CategoriseIncident command,
        IDocumentSession session,
        IValidator<CategoriseIncident> validator)
    {
        var result = await validator.ValidateAsync(command);
        if (!result.IsValid)
        {
            return Problem(statusCode: 400, detail: result.Errors.Select(x => x.ErrorMessage).Join(", "));
        }

        var userId = currentUserId();

        // This will give us access to the existing Incident state for the event stream
        var stream = await session.Events.FetchForWriting<Incident>(command.Id, command.Version, HttpContext.RequestAborted);
        if (stream.Aggregate == null) return NotFound();
        
        if (stream.Aggregate.Category != command.Category)
        {
            stream.AppendOne(new IncidentCategorised
            {
                Category = command.Category,
                UserId = userId
            });
        }

        await session.SaveChangesAsync();

        return Ok();
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
    

}
