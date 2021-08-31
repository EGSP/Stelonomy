namespace Stelonomy.Entities
{
    public class User
    {
        public ulong Id { get; }

        public User(ulong id)
        {
            Id = id;
        }
    }
}