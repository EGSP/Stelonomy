using LiteDB;

namespace Stelonomy.Entities
{
    public class EntitiesMapper
    {
        public static void User(BsonMapper mapper)
        {
            mapper.Entity<User>()
                .Id(x => x.Id, false);
        }
    }
}