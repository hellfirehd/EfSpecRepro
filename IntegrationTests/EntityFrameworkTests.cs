namespace EFSpecRepro.IntegrationTests
{
    using EFSpecRepro;
    using EFSpecRepro.Data;

    public class EntityFrameworkTests : RepositoryTests
    {
        public override IRepository<User> GetRepository() => new EntityFrameworkRepository();
    }
}
