﻿using System.Collections.Generic;

namespace Messenger.Core.DAL.Models
{
    public class Room : BaseModel
    {
        public Room()
        {
            this.Users = new List<User>();

            this.Messages = new List<Message>();
        }

        public string Name { get; set; }

        public virtual ICollection<User> Users { get; set; }

        public virtual ICollection<Message> Messages { get; set; }
    }
}