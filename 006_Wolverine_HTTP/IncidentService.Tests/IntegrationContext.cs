using Alba;
using Alba.Security;
using JasperFx.Core;
using Marten;
using Marten.Schema;
using Microsoft.Extensions.DependencyInjection;
using Oakton;
using Wolverine;
using Wolverine.Tracking;

public class AppFixture : IAsyncLifetime
{
    public IAlbaHost Host { get; private set; }

    public async Task InitializeAsync()
    {
        // THIS IS CRUCIAL, COMPLAIN TO THE ASPNETCORE TEAM
        OaktonEnvironment.AutoStartHost = true;
        
        // This is bootstrapping the actual application using
        // its implied Program.Main() set up
        // This is using a library named "Alba". See https://jasperfx.github.io/alba for more information
        Host = await AlbaHost.For<Program>(x =>
        {
            x.ConfigureServices(services =>
            {
                // We're going to establish some baseline data
                // for testing
                services.InitializeMartenWith<BaselineData>();

                // This is helpful in a second
                services.DisableAllExternalWolverineTransports();
            });
            
        }, new AuthenticationStub());
    }

    public Task DisposeAsync()
    {
        if (Host != null)
        {
            return Host.DisposeAsync().AsTask();
        }

        return Task.CompletedTask;
    }
}

[CollectionDefinition("integration")]
public class IntegrationCollection : ICollectionFixture<AppFixture>
{
}

public class BaselineData : IInitialData
{
    public static Guid Customer1Id { get; } = Guid.NewGuid();

    public async Task Populate(IDocumentStore store, CancellationToken cancellation)
    {
        await using var session = store.LightweightSession();
        session.Store(new Customer
        {
            Id = Customer1Id
        });

        await session.SaveChangesAsync(cancellation);
    }
}

[Collection("integration")]
public abstract class IntegrationContext : IAsyncLifetime
{
    private readonly AppFixture _fixture;

    protected IntegrationContext(AppFixture fixture)
    {
        _fixture = fixture;
    }

    // more....

    public IAlbaHost Host => _fixture.Host;

    public IDocumentStore theStore => _fixture.Host.Services.GetRequiredService<IDocumentStore>();

    async Task IAsyncLifetime.InitializeAsync()
    {
        // Using Marten, wipe out all data and reset the state
        // back to exactly what we described in InitialAccountData
        await theStore.Advanced.ResetAllData();

        await theContextIs();
    }

    protected virtual Task theContextIs() => Task.CompletedTask;

    // This is required because of the IAsyncLifetime 
    // interface. Note that I do *not* tear down database
    // state after the test. That's purposeful
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }


    public async Task<IScenarioResult> Scenario(Action<Scenario> configure)
    {
        return await Host.Scenario(configure);
    }
    
    // This method allows us to make HTTP calls into our system
    // in memory with Alba, but do so within Wolverine's test support
    // for message tracking to both record outgoing messages and to ensure
    // that any cascaded work spawned by the initial command is completed
    // before passing control back to the calling test
    protected async Task<(ITrackedSession, IScenarioResult)> TrackedHttpCall(Action<Scenario> configuration)
    {
        IScenarioResult result = null;

        // The outer part is tying into Wolverine's test support
        // to "wait" for all detected message activity to complete
        var tracked = await Host.ExecuteAndWaitAsync(async () =>
        {
            // The inner part here is actually making an HTTP request
            // to the system under test with Alba
            result = await Host.Scenario(configuration);
        });

        return (tracked, result);
    }
}