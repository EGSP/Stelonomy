using System;
using System.Threading.Tasks;
using JetBrains.Lifetimes;
using LiteDB;
using Serilog;
using Stelonomy.Data;
using Stelonomy.Entities;
using Stelonomy.Services;

namespace Stelonomy.Program
{
    public class Program: ILifetimed
    {
        // Здесь мы вызываем основной метод (асинхронный).
        static void Main(string[] args)
        {
            Console.WriteLine("Bot app started.");

            try
            {
                var program = new Program();
                program.MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

            }
            finally
            {
                Console.WriteLine("Closing console.");
            }
        }
        
        private const string Token = "ODgyMjY0MzgwODI0MjQ0MjU0.YS42vA.RYvFKSHy_m7nXGcnyU1oMUY6sPI";

        private LifetimeDefinition _mainLifetimeDefinition;
        private LifetimeDefinition _loggerLifetimeDefinition;

        public Lifetime Lifetime => _mainLifetimeDefinition.Lifetime;

        public Program()
        {
            _mainLifetimeDefinition = new LifetimeDefinition();
            _loggerLifetimeDefinition = new LifetimeDefinition();
            Lifetime.OnTermination(() =>
            {
                Log.Information("Program terminated.");
            });
        }
        
        private async Task MainAsync()
        {
            try
            {
                // Инициализируем все наши сервисы. Порядок добавления сервисов влияет только на порядок терминации.
                // Поэтому логгер лучше добавлять первым, т.к. тогда он терминируется последним.
                var servicesMaster = new ServicesMaster(Lifetime);
                await servicesMaster.Add(() => new LoggerService(), false, _loggerLifetimeDefinition.Lifetime);
                await servicesMaster.Add(() => new DiscordService(Token));
                await servicesMaster.Add(() => new LiteDbService());

                await servicesMaster.InitializeServices();

                // Вечное ожидание, чтобы программа не завершилась.
                await Task.Delay(3000);
            }
            finally
            {
                _mainLifetimeDefinition.Terminate();
                _loggerLifetimeDefinition.Terminate();
                
                Console.ReadKey();
            }
        }
    }

    public sealed class LiteDbInitializer
    {
        public void Initialize(LiteDatabase db)
        {
            Map(db.Mapper);
        }

        private void Map(BsonMapper mapper)
        {
            EntitiesMapper.User(mapper);
        }
    }
}