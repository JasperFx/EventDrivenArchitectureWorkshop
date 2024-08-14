// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Hosting;
using Oakton;
using Wolverine;
using Wolverine.RabbitMQ;

return await Host.CreateDefaultBuilder()
    .UseWolverine(opts =>
    {
        // Connect to Rabbit MQ
        // The default like this expects to connect to a Rabbit MQ
        // broker running in the localhost at the default Rabbit MQ
        // port
        opts.UseRabbitMq()
            // Make it build out any missing exchanges, queues, or bindings that
            // the system knows about as necessary
            .AutoProvision()
            
            // This is just to make Wolverine help us out to configure Rabbit MQ end to end
            // This isn't mandatory, but it might help you be more productive at development 
            // time
            .BindExchange("notifications").ToQueue("notifications", "notification_binding");

        // Tell Wolverine to listen for incoming messages
        // from a Rabbit MQ queue 
        opts.ListenToRabbitQueue("notifications");
    }).RunOaktonCommands(args);


// Just to see that there is a message handler for the RingAllTheAlarms
// message
public static class RingAllTheAlarmsHandler
{
    public static void Handle(RingAllTheAlarms message)
    {
        Console.WriteLine("I'm going to scream out an alert about incident " + message.IncidentId);
    }
}