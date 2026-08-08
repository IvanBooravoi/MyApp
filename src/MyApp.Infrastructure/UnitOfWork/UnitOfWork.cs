using System;
using System.Threading.Tasks;
using MyApp.Infrastructure.Repositories;

namespace MyApp.Infrastructure.UnitOfWork {
    public interface IUnitOfWork : IDisposable {
        IUserRepository Users { get; }
        Task<int> SaveChangesAsync();
    }

    public class UnitOfWork : IUnitOfWork {
        private readonly Db.AppDbContext _ctx;
        public IUserRepository Users { get; }

        public UnitOfWork(Db.AppDbContext ctx, IUserRepository users) {
            _ctx = ctx;
            Users = users;
        }

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();
        public void Dispose() => _ctx.Dispose();
    }
}
