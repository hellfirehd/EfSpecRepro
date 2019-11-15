namespace EFSpecRepro.IntegrationTests
{
    using EFSpecRepro;
    using EFSpecRepro.Data;

    public class InMemoryTests : RepositoryTests
    {
        public override IRepository<User> GetRepository() => new InMemoryRepository();
    }
}
