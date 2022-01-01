using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemStache;
using Microsoft.AspNetCore.Builder;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Lib.MemStache.WebAPI.Commands
{
    public class WebApiCommand : Command<WebApiCommand.Settings>
    {
        public class Settings : CommandSettings { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.MarkupLine("Launching WebApi");
            string[] args = { };
            WebApplication webApp = MemStacheWebApi.Run(args, new StacheMeister("stash.cli"));
            webApp.RunAsync().Wait();
            return 0;
        }
    }
}
