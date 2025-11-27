using System;


namespace Messenger.Shared
{
    public class UserModel
    {
        //свойство Id
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; }
    }
}