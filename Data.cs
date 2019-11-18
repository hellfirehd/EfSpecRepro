namespace EFSpecRepro.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using EFSpecRepro.Specifications;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;

    public interface IRepository<TDomainModel> : IDisposable
    {
        Task<IReadOnlyList<TDomainModel>> ListAsync(IQueryableSpecification<TDomainModel> specification = null);
    }

    public sealed class InMemoryRepository : IRepository<User>
    {
        private readonly IDictionary<Guid, User> _users = new ConcurrentDictionary<Guid, User>();

        public InMemoryRepository()
        {
            AddInternal(Guid.NewGuid(), "Hellfire", new DateTime(1974, 10, 16), Gender.Male);
            AddInternal(Guid.NewGuid(), "LittleLady", new DateTime(1975, 11, 11), Gender.Female);
            AddInternal(Guid.NewGuid(), "ShyGuy", new DateTime(2007, 10, 15), Gender.Male);
            AddInternal(Guid.NewGuid(), "Pipsqueak", new DateTime(2008, 5, 19), Gender.Female);
            AddInternal(Guid.NewGuid(), "Laforge", new DateTime(2011, 9, 14), Gender.Male);
        }

        public Task AddAsync(User domainModel)
        {
            if (domainModel == null)
                throw new ArgumentNullException(nameof(domainModel));

            _users.Add(domainModel.Id, domainModel);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(User domainModel)
        {
            _users.Remove(domainModel.Id);
            return Task.CompletedTask;
        }

        public void Dispose() { }

        public Task<User> GetAsync(Guid id) => Task.FromResult(_users[id]);

        public Task<IReadOnlyList<User>> ListAsync(IQueryableSpecification<User> specification = null)
        {
            if (specification is null) {
                IReadOnlyList<User> v = _users.Values.ToList();
                return Task.FromResult(v);
            }

            IReadOnlyList<User> result = _users.Values.Where(x => specification.IsSatisfiedBy(x)).ToList();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(User domainModel)
        {
            lock (_users) {
                return Task.Run(() => DeleteAsync(domainModel))
                           .ContinueWith(_ => AddAsync(domainModel), TaskScheduler.Default);
            }
        }

        private void AddInternal(Guid uid, String name, DateTime dateOfBirth, Gender gender)
            => _users.Add(uid, new User(uid, name, dateOfBirth, gender));
    }

    public sealed class EntityFrameworkRepository : IRepository<User>
    {
        private readonly SqlConnection _connection;
        private readonly DbContextOptions<EntityFrameworkContext> _options;

        public EntityFrameworkRepository()
        {
            _connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Integrated Security=true;");
            _connection.Open();


            //lame... just to delete the existing user table
            var c = _connection.CreateCommand();
            c.CommandText = "drop table Users";
            var r = c.ExecuteNonQuery();


            _options = new DbContextOptionsBuilder<EntityFrameworkContext>()
                    .UseSqlServer(_connection)
                    .Options;

            using (var context = new EntityFrameworkContext(_options))
            {
                context.Database.EnsureCreated();
            }
        }

        public async Task<IReadOnlyList<User>> ListAsync(IQueryableSpecification<User> specification = null)
        {
            using (var ctx = new EntityFrameworkContext(_options)) {
                var query = ctx.Users as IQueryable<User>;
                if (specification != null) {
                    query = query.Where(specification.Predicate);
                }
                return await query.ToListAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }

    public sealed class EntityFrameworkContext : DbContext
    {
        public EntityFrameworkContext(DbContextOptions<EntityFrameworkContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "<Pending>")]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(x => x.Id).HasName("Id");

            using (var inMemoryRepository = new InMemoryRepository()) {
                var users = inMemoryRepository.ListAsync().GetAwaiter().GetResult();
                modelBuilder.Entity<User>(x => x.HasData(users));
            }
        }
    }
}
