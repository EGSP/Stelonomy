using System.Threading.Tasks;
using JetBrains.Lifetimes;
using Serilog;

namespace Stelonomy.Services
{
    public class LoggerService : IService
    {
        private Lifetime _lifetime;

        public Lifetime Lifetime
        {
            get => _lifetime;
            set
            {
                _lifetime = value;
                _lifetime.OnTermination(() =>
                {
                    Log.Information("Logger service terminated.");
                    Log.CloseAndFlush();
                });
            }
        }
        
        private ILogger _logger;
        public ILogger Logger
        {
            get => _logger;
            private set
            {
                _logger = value;
                Log.Logger = _logger;
            }
        }

        public async Task Init(ServicesMaster servicesMaster)
        {
            Logger = CreateLogger();
        }
        
        private ILogger CreateLogger()
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Async(x=>x.Console())
                .CreateLogger();
            return logger;
        }
    }
}