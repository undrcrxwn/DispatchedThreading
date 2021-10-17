using DispatchedThreading;

namespace Examples
{
    public enum AccountState
    {
        Valid,
        Invalid,
        Reserved,
        Unchecked
    }

    public class Account
    {
        public string Email;
        public string Password;
        public AccountState State;
        public MasterToken Token;

        public Account(string email, string password, AccountState state = AccountState.Unchecked)
        {
            Email = email;
            Password = password;
            State = state;
        }

        public override string ToString() => $"{Email}:{Password}";
    }
}
