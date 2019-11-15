namespace EFSpecRepro
{
    using System;
    using System.Linq.Expressions;
    using EFSpecRepro.Specifications;

    public class UserIsAgeOfMajority : AbstractSpecification<User>
    {
        public UserIsAgeOfMajority(Int32 ageOfMajority)
            : this(DateTime.UtcNow.Date.AddYears(0 - ageOfMajority))
        {
        }

        public UserIsAgeOfMajority(DateTime dateTime) => DateTime = dateTime;

        public DateTime DateTime { get; }

        public override Expression<Func<User, Boolean>> Predicate
            => user => user.DateOfBirth <= DateTime;
    }
}
