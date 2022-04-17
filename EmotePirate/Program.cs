using System.Globalization;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using Foxite.Common.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp.Extensions;

namespace EmotePirate;

public sealed class Program {
	public static IHost Host { get; set; }

	private static IHostBuilder CreateHostBuilder(string[] args) =>
		Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((hostingContext, configuration) => {
				configuration.Sources.Clear();

				configuration
					.AddJsonFile("appsettings.json", true, true)
					.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
					.AddEnvironmentVariables("PIRATE_")
					.AddCommandLine(args);
			});

	private static async Task Main(string[] args) {
		using IHost host = CreateHostBuilder(args)
			.ConfigureLogging((_, builder) => {
				builder.AddExceptionDemystifyer();
			})
			.ConfigureServices((hbc, isc) => {
				//isc.Configure<DiscordConfiguration>(hbc.Configuration.GetSection("Discord"));

				isc.AddSingleton(isp => {
					var clientConfig = new DiscordConfiguration {
						Token = hbc.Configuration.GetSection("Discord").GetValue<string>("Token"),
						Intents = DiscordIntents.GuildMessages | DiscordIntents.Guilds,
						LoggerFactory = isp.GetRequiredService<ILoggerFactory>(),
						MinimumLogLevel = LogLevel.Information,
					};
					
					var client = new DiscordClient(clientConfig);
					
					var commandsConfig = new CommandsNextConfiguration {
						Services = isp,
						EnableDms = false,
						EnableMentionPrefix = true,
						UseDefaultCommandHandler = false
					};

					client.UseCommandsNext(commandsConfig);

					return client;
				});

				isc.AddNotifications().AddDiscord(hbc.Configuration.GetSection("DiscordNotifications"));
				
				isc.AddSingleton<HttpClient>();
			})
			.Build();

		Host = host;

		var discord = host.Services.GetRequiredService<DiscordClient>();
		var commands = discord.GetCommandsNext();
		commands.RegisterCommands<EmoteCommandModule>();
		
		commands.CommandErrored += (_, eventArgs) => {
			return eventArgs.Exception switch {
				CommandNotFoundException => eventArgs.Context.RespondAsync("Unknown command."),
				ChecksFailedException cfe => eventArgs.Context.RespondAsync($"Checks failed 🙁{string.Join("", cfe.FailedChecks.Select(cba => $"\n- {cba.GetType().Name}"))}"),
				ArgumentException { Message: "Could not find a suitable overload for the command." } => eventArgs.Context.RespondAsync("Invalid arguments."),
				_ => Host.Services.GetRequiredService<NotificationService>().SendNotificationAsync("Exception while executing command", eventArgs.Exception).ContinueWith(t => eventArgs.Context.RespondAsync("Internal error; devs notified."))
			};
		};

		discord.ClientErrored += (_, eventArgs) => Host.Services.GetRequiredService<NotificationService>().SendNotificationAsync($"Exception in {eventArgs.EventName}", eventArgs.Exception);
		
		discord.MessageCreated += (client, eventArgs) => {
			if (eventArgs.Author.IsBot || eventArgs.Channel.IsPrivate) {
				return Task.CompletedTask;
			}

			int mentionLength = GetMentionPrefixLength(eventArgs.Message, discord.CurrentUser);

			if (mentionLength == -1) {
				return Task.CompletedTask;
			}

			var prefix = eventArgs.Message.Content.Substring(0, mentionLength);
			var input = eventArgs.Message.Content.Substring(mentionLength);
			
			Command? cmd = commands.FindCommand("emote " + input, out string? rawArguments);

			if (cmd is null) {
				return eventArgs.Message.RespondAsync("Unknown command.");
			}
			
			CommandContext ctx = commands.CreateContext(eventArgs.Message, prefix, cmd, rawArguments);
			_ = cmd.ExecuteAsync(ctx);
			return Task.CompletedTask;
		};

		await discord.ConnectAsync();

		await host.RunAsync();
	}
	
	private static Regex UserRegex { get; } = new Regex(@"<@\!?(\d+?)>", RegexOptions.ECMAScript);
	private static int GetMentionPrefixLength(DiscordMessage msg, DiscordUser user)
	{
		var content = msg.Content;
		if (!content.StartsWith("<@"))
			return -1;

		var cni = content.IndexOf('>');
		if (cni == -1)
			return -1;

		var cnp = content.Substring(0, cni + 1);
		var m =  UserRegex.Match(cnp);
		if (!m.Success)
			return -1;

		var userId = ulong.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
		return user.Id != userId ? -1 : m.Value.Length;
	}
}
