using JetBrains.Lifetimes;

namespace Stelonomy.Data
{
    public interface ILifetimed
    {
        Lifetime Lifetime { get; }
    }
}