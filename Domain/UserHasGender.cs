namespace EFSpecRepro
{
    using System;
    using System.Linq.Expressions;
    using EFSpecRepro.Specifications;

    public class UserHasGender : AbstractSpecification<User>
    {
        public UserHasGender(Gender gender) => Gender = gender;

        public Gender Gender { get; }

        public override Expression<Func<User, Boolean>> Predicate
            => user => (Gender & user.Gender) != 0;
    }
}
