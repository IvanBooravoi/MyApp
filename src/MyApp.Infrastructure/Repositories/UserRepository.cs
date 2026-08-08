using MyApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MyApp.Infrastructure.Repositories {
    public interface IUserRepository {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUserNameAsync(string userName);
        Task AddAsync(User user);
    }

    public class UserRepository : IUserRepository {
        private readonly Db.AppDbContext _ctx;
        public UserRepository(Db.AppDbContext ctx) => _ctx = ctx;

        public Task<User?> GetByEmailAsync(string email) =>
            _ctx.Users.FirstOrDefaultAsync(x => x.Email == email);

        public Task<User?> GetByUserNameAsync(string userName) =>
            _ctx.Users.FirstOrDefaultAsync(x => x.UserName == userName);

        public async Task AddAsync(User user)
        {
            await _ctx.Users.AddAsync(user);
        }
    }
}
