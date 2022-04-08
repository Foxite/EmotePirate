using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace EmotePirate;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class RequireAttachmentAttribute : CheckBaseAttribute {
	public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help) {
		return Task.FromResult(ctx.Message.Attachments.Count > 0);
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class RequireReferencedMessageAttribute : CheckBaseAttribute {
	public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help) {
		return Task.FromResult(ctx.Message.ReferencedMessage != null);
	}
}
