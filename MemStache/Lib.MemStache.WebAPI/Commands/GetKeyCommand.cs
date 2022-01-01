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
    public class GetKeyCommand : Command<GetKeyCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-k|--key <KEY>")]
            [Description("The Key mapped to the Value to be retrieved")]
            public string? Key { get; set; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.MarkupLine("Getting Key");
            string temp = $@"{settings.Key}";
            string url = @$"http://localhost:5000/stash/{settings.Key}";
            var client = new RestClient(url);
            var request = new RestRequest();

            var response = GetKeyValue(client, request).GetAwaiter().GetResult();

            AnsiConsole.MarkupLine($"Status Code: {response.StatusCode.ToString()}");

            AnsiConsole.MarkupLine($"{settings.Key}: {response.Content}");

            return 0;
        }
        private async Task<RestResponse> GetKeyValue(RestClient client, RestRequest request)
        {
            var response = await client.GetAsync(request);
            return response;
        }
    }
}
