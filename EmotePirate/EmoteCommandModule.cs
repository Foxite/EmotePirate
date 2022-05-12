using System.Text;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;

namespace EmotePirate;

//[Group()]
//[Aliases("")]
[RequireUserPermissions(Permissions.ManageEmojis, false)]
//[RequireGuild] // Implicit from setting `false` above
public class EmoteCommandModule : BaseCommandModule {
	private static readonly Regex EmoteRegex = new Regex(@"<(?<animated>a?):(?<name>\w+):(?<id>\d{18})>");

	public HttpClient Http { get; set; } = null!;
	public ILogger<EmoteCommandModule> Logger { get; set; } = null!;
	
	private async Task CreateEmotes(CommandContext context, IEnumerable<CreateEmote> emotes, List<string>? failReasons = null) {
		failReasons ??= new List<string>();
		var createdEmotes = new List<DiscordEmoji>();
		
		foreach (CreateEmote emote in emotes) {
			try {
				await using MemoryStream ms = new MemoryStream();
				await using (Stream download = await Http.GetStreamAsync(emote.Url)) {
					await download.CopyToAsync(ms);
				}
				ms.Seek(0, SeekOrigin.Begin);
				DiscordGuildEmoji newEmote = await context.Guild.CreateEmojiAsync(emote.Name, ms);
				createdEmotes.Add(newEmote);
			} catch (Exception e) {
				Logger.LogError(e, "Error creating emote");
				failReasons.Add($"ERROR: {e.Message}");
			}
		}
		var result = new StringBuilder();
		if (createdEmotes.Count > 0) {
			result.Append("👌");
			foreach (var emote in createdEmotes) {
				result.Append(emote.ToString());
			}
			if (failReasons.Count > 0) {
				result.AppendLine();
			}
		}
		if (failReasons.Count > 0) {
			result.AppendInterpolated($"Could not create {failReasons.Count} emote{(failReasons.Count == 1 ? "" : "s")}:");
			foreach (string reason in failReasons) {
				result.AppendInterpolated($"\n- {reason}");
			}
		}

		await context.RespondAsync(result.ToString());
	}
	
	[Command("emote")]
	[RequireReferencedMessageAttribute]
	public Task StealEmote(CommandContext context) {
		var emoteMatches = EmoteRegex.Matches(context.Message.ReferencedMessage.Content);
		var failReasons = new List<string>();
		var emotes = new List<CreateEmote>();
		
		foreach (Match match in emoteMatches) {
			if (ulong.TryParse(match.Groups["id"].Value, out ulong emoteId)) {
				string url = $"https://cdn.discordapp.com/emojis/{emoteId}.{(string.IsNullOrEmpty(match.Groups["animated"].Value) ? "webp" : "gif")}";
				emotes.Add(new CreateEmote(match.Groups["name"].Value, url));
			} else {
				failReasons.Add("parse failed");
			}
		}
		return CreateEmotes(context, emotes, failReasons);
	}
	
	[Command("emote")]
	public Task CreateEmoteFromAttachment(CommandContext context, string name) {
		string? imageUrl =
			context.Message.Attachments.FirstOrDefault()?.Url ??
			context.Message.ReferencedMessage.Attachments.FirstOrDefault()?.Url ??
			context.Message.ReferencedMessage.Embeds.FirstOrDefault(embed => embed.Image != null && embed.Image.Url.Type == DiscordUriType.Standard)?.Image.Url.ToString();

		if (imageUrl != null) {
			return CreateEmotes(context, new[] {
				new CreateEmote(name, imageUrl)
			});
		} else {
			return context.Message.RespondAsync("No attachments on your message or its referenced message");
		}
	}

	[Command("emote")]
	public Task CreateEmoteFromUrl(CommandContext context, Uri uri, string name) => CreateEmotes(context, new[] { new CreateEmote(name, uri.ToString()) });
	
	[Command("emote")]
	public Task CreateEmoteFromUrl(CommandContext context, string name, Uri uri) => CreateEmotes(context, new[] { new CreateEmote(name, uri.ToString()) });

	private record CreateEmote(
		string Name,
		string Url
	);
}
