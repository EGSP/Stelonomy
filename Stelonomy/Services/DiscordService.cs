using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Lifetimes;
using Serilog;

namespace Stelonomy.Services
{
    public class DiscordService : IService
    {
        private readonly LoggerService _loggerService;
        private readonly string _token;
        
        private Lifetime _lifetime;

        public Lifetime Lifetime
        {
            get => _lifetime;
            private set
            {
                _lifetime = value;
                
                _lifetime.OnTermination(() =>
                {
                    SocketClient.StopAsync().ContinueWith((x) =>
                    {
                        Log.Information("Discord service terminated.");
                    }).GetAwaiter().GetResult();
                });
            }
        }

        private DiscordSocketClient SocketClient { get; set; }
        
        
        public DiscordService(string token)
        {
            _token = token;
        }
        
        public async Task Init(ServicesMaster servicesMaster)
        {
            SocketClient = new DiscordSocketClient();

            await servicesMaster.AwaitService<LoggerService>(this);
            await SetupLogging();
            
            await SocketClient.LoginAsync(TokenType.Bot, _token);
            await SocketClient.StartAsync();
        }

        private Task SetupLogging()
        {
            SocketClient.Log += LogMessage;
            return Task.CompletedTask;
        }
        
        private Task LogMessage(LogMessage msg)
        {
            Log.Information(msg.Message);
            return Task.CompletedTask;
        }
    }
}