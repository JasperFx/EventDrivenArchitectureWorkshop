using FluentValidation;
using IncidentService;
using JasperFx.Core;
using Marten;
using Marten.Events.Projections;
using Oakton;
using Oakton.Resources;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

// Adds in some command line diagnostics
builder.Host.ApplyOaktonExtensions();

builder.Services.AddAuthentication("Test");
builder.Services.AddAuthorization();

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
    
    // Option #1: Publish the events to Wolverine in strict order
    .PublishEventsToWolverine("PriorityAssignments", r =>
    {
        r.PublishEvent<IncidentCategorised>();
    })
    
    // Option #2: Push any IncidentCategorised events to Wolverine command handling as TryAssignPriority
    // immediately with no ordering guarantees
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
    
    // Applies a transactional inbox/outbox on 
    // local queues
    opts.Policies.UseDurableLocalQueues();
    
    // Apply the validation middleware *and* discover and register
    // Fluent Validation validators
    opts.UseFluentValidation();
    
    opts.LocalQueueFor<TryAssignPriority>()
        // By default, local queues allow for parallel processing with a maximum
        // parallel count equal to the number of processors on the executing
        // machine, but you can override the queue to be sequential and single file
        .Sequential()

        // Or add more to the maximum parallel count!
        .MaximumParallelMessages(10)

        // Pause processing on this local queue for 1 minute if there's
        // more than 20% failures for a period of 2 minutes
        .CircuitBreaker(cb =>
        {
            cb.PauseTime = 1.Minutes();
            cb.SamplingPeriod = 2.Minutes();
            cb.FailurePercentageThreshold = 20;
            
            // Definitely worry about this type of exception
            cb.Include<TimeoutException>();
            
            // Don't worry about this type of exception
            cb.Exclude<InvalidInputThatCouldNeverBeProcessedException>();
        });
});

// Depending on your DevOps setup and policies,
// you may or may not actually want this enabled
// in production installations, but some folks do
if (builder.Environment.IsDevelopment())
{
    // This will direct our application to set up
    // all known "stateful resources" at application bootstrapping
    // time
    builder.Services.AddResourceSetupOnStartup();
}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapWolverineEndpoints(opts =>
{
    // Direct Wolverine.HTTP to use Fluent Validation
    // middleware to validate any request bodies where
    // there's a known validator (or many validators)
    opts.UseFluentValidationProblemDetailMiddleware();
    
    // Creates a User object in HTTP requests based on
    // the "user-id" claim
    opts.AddMiddleware(typeof(UserDetectionMiddleware));
});


// Opt into Oakton command line usage for quite a few
// diagnostics and utilities around Marten & Wolverine
return await app.RunOaktonCommands(args);


// This is necessary for the Alba specification we'll
// do shortly
public partial class Program
{
}