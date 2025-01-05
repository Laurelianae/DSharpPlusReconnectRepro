// See https://aka.ms/new-console-template for more information

using BotReproduction;
using DSharpPlus;
using DSharpPlus.Clients;
using DSharpPlus.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

builder.Services
    .AddDiscordClient(builder.Configuration.GetValue<string>("DISCORD_TOKEN", ""), DiscordIntents.AllUnprivileged)
    .Replace<IEventDispatcher, CatchingEventDispatcher>()
    .AddHostedService<BotHost>();

var host = builder.Build();

host.Run();

