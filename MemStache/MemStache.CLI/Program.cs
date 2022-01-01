using Lib.MemStache.WebAPI;
using Lib.MemStache.WebAPI.Commands;
using MemStache;
using Microsoft.AspNetCore.Builder;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
/* 
    dotnet tool install --global --add-source C:\DotNet.Tools --version 1.0.0 --framework net6.0 MemStache.CLI
    dotnet tool uninstall -g MemStache.CLI
 */
var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("stash")
    .ValidateExamples();
    config.AddCommand<WebApiCommand>("webapi");
    config.AddCommand<SetKeyCommand>("setkey");
    config.AddCommand<GetKeyCommand>("getkey");
    config.AddExample(new[] { "webapi" });
    config.SetExceptionHandler(ex =>
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    });

});
return app.Run(args);

