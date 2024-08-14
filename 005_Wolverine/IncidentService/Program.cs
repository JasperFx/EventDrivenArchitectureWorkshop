using FluentValidation;
using IncidentService;
using Marten;
using Marten.Events.Projections;
using Wolverine;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarten(opts =>
    {
        // You always have to tell Marten what the connection string to the underlying
        // PostgreSQL database is, but this is the only mandatory piece of 
        // configuration
        var connectionString = builder.Configuration.GetConnectionString("postgres");
        opts.Connection(connectionString);

        // We have to tell Marten about the projection we built in the previous post
        // so that Marten will "know" how to project events to the IncidentDetails
        // projected view
        opts.Projections.Add<IncidentProjection>(ProjectionLifecycle.Inline);
    })
    // This is a mild optimization
    .UseLightweightSessions()
    
    // This adds middleware support for Marten as well as the 
    // transactional middleware support we'll introduce in a little bit...
    .IntegrateWithWolverine()
    
    // Push any IncidentCategorised events to Wolverine command handling as TryAssignPriority
    .EventForwardingToWolverine(opts =>
    {
        // Setting up a little transformation of an event with event metadata to an internal command message
        opts.SubscribeToEvent<IncidentCategorised>().TransformedTo(e => new TryAssignPriority
        {
            IncidentId = e.StreamId,
            UserId = e.Data.UserId
        });
    });;

builder.Host.UseWolverine(opts =>
{
    opts.Policies.AutoApplyTransactions();
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddSingleton<IValidator<CategoriseIncident>, CategoriseIncident.Validator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();


// This is necessary for the Alba specification we'll
// do shortly
public partial class Program
{
}