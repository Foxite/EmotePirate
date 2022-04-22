# EmotePirate
The bot will create emotes in the current server when it is mentioned by a user who has Manage Emotes in that server.

- If no other text is specified then it will look in the referenced message (ie the message you replied to) for emotes. It will download all emotes and create them in the current server.
- If you have an attachment you can specify a name for the new emote, and it will be created. This only works for one emote at a time. You can also reply to a message with an attachment and it will use that attachment.
- Finally, you can provide a url to an image and a name for the emote, and it will be created. The order does not matter.

## Docker deployment
See Dockerfile in EmotePirate directory. Envvars:
- `PIRATE_Discord__Token`: your bot token
- `PIRATE_DiscordNotifications__WebhookUrl`: discord webhook url where error notifications are to be sent
