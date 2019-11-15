namespace EFSpecRepro
{
    using System;

    public class User
    {
        public User() { }

        public User(Guid uid, String name, DateTime dateOfBirth, Gender gender)
        {
            Id = uid;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DateOfBirth = dateOfBirth;
            Gender = gender;
        }

        public Guid Id { get; }
        public String Name { get; }
        public DateTime DateOfBirth { get; }
        public Gender Gender { get; }
    }
}
