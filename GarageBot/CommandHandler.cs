using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GarageBot
{
    class CommandHandler
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider _services;

        public async Task InstallAsync(DiscordSocketClient client)
        {
            this.client = client;
            commands = new CommandService();
            _services = new ServiceCollection()
           .AddSingleton(client)
           .AddSingleton(commands)
           .BuildServiceProvider();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            client.MessageReceived += HandleCommand;

            await Program.Log("Commands loaded!");
        }
        
        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;

            // Don't handle the command if it's posted by a bot
            if (message.Author.IsBot) return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;

            if (!(message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            // Determine if the message has a valid prefix, adjust argPos 
            var context = new SocketCommandContext(client, message);

            IResult result;

            // Create a Command Context

            // Execute the Command, store the result
            result = await commands.ExecuteAsync(context, argPos, null);

            // If the command failed, notify the user
            if (!result.IsSuccess)
                await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
        }
    }
}
