namespace Lib.MemStache.WebAPI
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using global::MemStache;
    using Newtonsoft.Json;
    using System.Runtime.Remoting;
    using System.Diagnostics;

    public static class MemStacheWebApi
    {
        public static WebApplication Run(string[] args, StacheMeister _meister)
        {
            StacheMeister meister = _meister ?? throw new ArgumentNullException(nameof(meister));
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<StacheMeister>(s => meister);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors();
            var app = builder.Build();
            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

            app.MapGet("/stash/{key}", async (HttpContext http, [FromServices] StacheMeister meister, string key) =>
            {
                if (!http.Request.RouteValues.TryGetValue("Key", out var _key))
                {
                    http.Response.StatusCode = 400;
                    return;
                }
                string result = JsonConvert.SerializeObject(meister![key!]);
                await http.Response.WriteAsJsonAsync(result);
            });

            app.MapPost("/stash/", (HttpContext http, [FromServices] StacheMeister meister, string key, string value) =>
            {
                try
                {
                    meister[key] = value;
                }
                catch (Exception)
                {
                    http.Response.StatusCode = 500;
                }
                finally
                {
                    http.Response.StatusCode = 200;
                }
                return;
            });

            app.UseSwagger();
            app.UseSwaggerUI();
            return app;
        }
    }
}
