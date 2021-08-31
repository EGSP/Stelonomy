using System.Threading.Tasks;
using JetBrains.Lifetimes;
using Serilog;

namespace Stelonomy.Services
{
    public class LiteDbService: IService
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
                    Log.Information("LiteDb service terminated.");
                });
            }
        }

        public async Task Init(ServicesMaster servicesMaster)
        {
            
            await servicesMaster.AwaitService<LoggerService>(this);
        }
    }
}