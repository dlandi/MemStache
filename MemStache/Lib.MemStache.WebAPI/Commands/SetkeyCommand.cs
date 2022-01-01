using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemStache;
using Microsoft.AspNetCore.Builder;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Lib.MemStache.WebAPI.Commands
{
    public class SetKeyCommand : Command<SetKeyCommand.Settings>
    {
        public class Settings : CommandSettings
        {

            [CommandOption("-k|--key <KEY>")]
            [Description("The Key mapped to the Value")]
            public string? Key { get; set; }

            [CommandOption("-v|--value <VALUE>")]
            [Description("The Value mapped to the Key")]
            public string? Value { get; set; }

            [CommandOption("-t|--type <TYPE>")]
            [Description("The Value's Type")]
            public string? Type { get; set; }

        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.MarkupLine("Setting Key");
            string url = @$"http://localhost:5000/stash?key={settings.Key}&value={settings.Value}";
            AnsiConsole.MarkupLine($@"Posting: {url}");

            var client = new RestClient(url);
            var request = new RestRequest();

            var statusCode = SetKeyValue(client, request).GetAwaiter().GetResult().StatusCode;

            AnsiConsole.MarkupLine($"Status Code: {statusCode}");

            return 0;
        }

        private async Task<RestResponse> SetKeyValue(RestClient client, RestRequest request)
        {
            var response = await client.PostAsync(request);
            return response;
        }
    }
}
