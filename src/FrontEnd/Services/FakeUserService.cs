using FrontEnd.Models;
using FrontEnd.Models.Id;

namespace FrontEnd.Services
{
    public class FakeUserService : IUserService
    {
        private readonly IDictionary<UserId, User> users = new Dictionary<UserId, User>();
        private readonly IList<string> names = new List<string> { "Alice", "Bob", "Charlie", "David", "Eve", "Frank", "Grace", "Heidi" };
        public async Task<User> GetUserAsync(UserId Id)
        {
            var random = new Random();
            await Task.Delay(random.Next(10000));

            if (users.TryGetValue(Id, out var user))
            {
                return user;
            }
            else
            {
                var nameIndex = random.Next(names.Count);
                var name = names.ElementAt(nameIndex);
                names.RemoveAt(nameIndex);
                user = random.Next(5) == 0 ? new User(Id): new User(Id, name) ;
                users.Add(Id, user);
                return user;
            }
            
        }
    }
}
