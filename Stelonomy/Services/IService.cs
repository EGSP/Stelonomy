using System.Threading.Tasks;
using Stelonomy.Data;

namespace Stelonomy.Services
{
    /// <summary>
    /// Lifetime для сервисов устанавливается ServicesMaster через рефлексию.
    /// </summary>
    public interface IService : ILifetimed
    { 
        Task Init(ServicesMaster servicesMaster);
    }
}