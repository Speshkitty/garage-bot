using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GarageBot
{
    class Program
    {

        public static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private CommandHandler commands;

        public async Task Start()
        {
            await Log("Starting!");
            
            client = new DiscordSocketClient();
            await Log("Created client!");

            commands = new CommandHandler();
            await commands.InstallAsync(client);
            
            await client.LoginAsync(TokenType.Bot, GetToken());
            await client.StartAsync();

            await Task.Delay(-1);
        }


        private string GetToken()
        {
            string s;
            try { s = File.ReadAllText("token.txt"); }
            catch
            {
                Console.WriteLine("Token file not found!");
                Console.Write("Enter token: ");
                s = Console.ReadLine();
                File.WriteAllText("token.txt", s);
            }
            return s;
        }

        internal static Task Log(string msg)
        {
            Console.WriteLine(" " + msg.ToString());
            return Task.CompletedTask;
        }
    }
}
