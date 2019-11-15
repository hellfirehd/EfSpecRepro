namespace EFSpecRepro.IntegrationTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using EFSpecRepro;
    using EFSpecRepro.Data;
    using EFSpecRepro.Specifications;
    using FluentAssertions;
    using Xunit;

    public abstract class RepositoryTests
    {
        public abstract IRepository<User> GetRepository();

        [Fact]
        public async Task All()
        {
            using (var repo = GetRepository()) {
                (await repo.ListAsync().ConfigureAwait(false)).Count().Should().Be(5);
            }
        }

        [Fact]
        public async Task And()
        {
            using (var repo = GetRepository()) {
                var spec = new UserIsAgeOfMajority(18).And(new UserHasGender(Gender.Male));

                (await repo.ListAsync(spec).ConfigureAwait(false)).Count().Should().Be(1);
            }
        }

        [Fact]
        public async Task Not()
        {
            using (var repo = GetRepository()) {
                var spec = !new UserIsAgeOfMajority(18);

                (await repo.ListAsync(spec).ConfigureAwait(false)).Count().Should().Be(3);
            }
        }

        [Fact]
        public async Task Or()
        {
            using (var repo = GetRepository()) {
                var spec = new UserIsAgeOfMajority(18).Or(new UserHasGender(Gender.Male));

                (await repo.ListAsync(spec).ConfigureAwait(false)).Count().Should().Be(4);
            }
        }

        [Fact]
        public async Task UserIsAgeOfMajority()
        {
            using (var repo = GetRepository()) {
                (await repo.ListAsync(new UserIsAgeOfMajority(18)).ConfigureAwait(false)).Count().Should().Be(2);
            }
        }

        [Fact]
        public async Task UserHasGender()
        {
            using (var repo = GetRepository()) {
                (await repo.ListAsync(new UserHasGender(Gender.Female)).ConfigureAwait(false)).Count().Should().Be(2);
            }
        }
    }
}
