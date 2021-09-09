using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Lifetimes;
using Serilog;
using Stelonomy.Data;

namespace Stelonomy.Services
{
    public class ServicesMaster : ILifetimed
    {
        private LifetimeDefinition _lifetimeDefinition;

        private Queue<IService> _awaitingChain;

        // Все сервисы бота.
        private Dictionary<Type, ServiceInfo> _services = new Dictionary<Type, ServiceInfo>();
        
        public Lifetime Lifetime => _lifetimeDefinition.Lifetime;

        public ServicesMaster(Lifetime lifetime)
        {
            _lifetimeDefinition = lifetime.CreateNested();
            Lifetime.OnTermination(() =>
            {
                Log.Information("Services master terminated.");
            });
        }

        public async Task Add(Func<IService> service, bool immidiate = false, Lifetime parentLifetime = default)
        {
            var inst = service();

            var serviceLifetime = parentLifetime == default
                ? Lifetime.CreateNested().Lifetime : parentLifetime.CreateNested().Lifetime;
            
            // REFLECTION ---------------------------------------------------------------------------------------------------
            // Инъекция объекта.
            inst.GetType().GetProperty("Lifetime")?.SetValue(inst, 
                serviceLifetime);
            // --------------------------------------------------------------------------------------------------------------

            var info = new ServiceInfo(inst);
            if (immidiate)
            {
                await inst.Init(this)
                    .ContinueWith(x => info.IsInitialized = true);
            }

            _services.Add(inst.GetType(), info);
        }

        /// <summary>
        /// Инициализация всех сервисов.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public async Task InitializeServices()
        {
            try
            {
                _awaitingChain = new Queue<IService>();

                foreach (var keyValuePair in _services)
                {
                    // Если сервис не был инициализирован до этого или вовремя инициализации другого сервиса. 
                    if (keyValuePair.Value.IsInitialized == false)
                    {
                        var info = keyValuePair.Value;
                        await info.Service.Init(this);
                        info.IsInitialized = true;
                    }
                    
                    _awaitingChain.Clear();
                }
            }
            catch (ServiceDependencyException ex)
            {
                throw new Exception($"Service {ex.Dependency.Name} is not added to services!" +
                                    $" {ex.Caller.GetType().Name} has called awaiting", ex);
            }
            catch (ServiceAwaitDeadlockException ex)
            {
                throw new Exception($"Deadlock has caused by service awaiting on initialize phase!" +
                                    $" Caller is {ex.Caller.GetType().Name}," +
                                    $" Awaited is {ex.Awaited.GetType().Name}", ex);
            }
        }

        /// <summary>
        /// Ожидание инициализации сервиса.
        /// </summary>
        /// <exception cref="ServiceAwaitDeadlockException"></exception>
        /// <exception cref="ServiceDependencyException"></exception>
        public async Task<Option<T>> AwaitService<T>(IService caller) where T : class, IService
        {
            var type = typeof(T);

            _awaitingChain.Enqueue(caller);
            
            ServiceInfo info;
            // Если сервис существует.
            if (_services.TryGetValue(type, out info))
            {
                if (info.IsInitialized)
                {
                    return info.Service as T;
                }
                else
                {
                    if (info.Service.Lifetime.IsNotAlive)
                        return Option<T>.None;
                    
                    // Если нужный сервис кого-то ждет, значит мы пришли к deadlock.
                    if (_awaitingChain.Contains(info.Service))
                    {
                        throw new ServiceAwaitDeadlockException(caller, info.Service);
                    }
                    // Т.к. сервис не инициализирован и никого не ждет, то попытаемся инициализировать здесь.
                    else
                    {
                        await info.Service.Init(this);
                        info.IsInitialized = true;
                        return info.Service as T;
                    }
                }
            }

            throw new ServiceDependencyException(caller, type);
        }
        
        // INNER CLASSES
        
        private class ServiceInfo
        {
            public IService Service { get; set; }
            public bool IsInitialized { get; set; }

            public ServiceInfo(IService service)
            {
                Service = service;
            }
        }

        private class ServiceAwaitDeadlockException : Exception
        {
            public readonly IService Caller;
            public readonly IService Awaited;
            public ServiceAwaitDeadlockException(IService caller, IService awaited)
            {
                Caller = caller;
                Awaited = awaited;
            }
        }

        private class ServiceDependencyException : Exception
        {
            public readonly IService Caller;
            public readonly Type Dependency;

            public ServiceDependencyException(IService caller, Type dependency)
            {
                Caller = caller;
                Dependency = dependency;
            }
        }
    }
}
