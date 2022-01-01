using Lib.MemStache.WebAPI;
using MemStache;
using Microsoft.AspNetCore.Builder;
using Spectre.Console;
using Spectre.Console.Cli;


var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("stache")
    .ValidateExamples()
    .AddCommand<WebApiCommand>("webapi");
    config.AddExample(new[] { "webapi" });    
    config.SetExceptionHandler(ex =>
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    });

});
return app.Run(args);

public class WebApiCommand : Command<WebApiCommand.Settings>
{
    public class Settings : CommandSettings { }
    
    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("Launching WebApi");
        string[] args = { };
        WebApplication webApp = MemStacheWebApi.Run(args, new StacheMeister("memstache.cli"));
        webApp.RunAsync().Wait();
        return 0;
    }
}

/*
 
 dotnet tool install --global --add-source C:\MemStache\Setup --version 1.0.0 --framework net6.0 MemStache.Service
 
 */