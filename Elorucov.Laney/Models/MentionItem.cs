using System;

namespace Elorucov.Laney.Models {
    public class MentionItem {
        public long Id { get; set; }
        public string Domain { get; private set; }
        public string Name { get; private set; }
        public Uri Avatar { get; private set; }

        public MentionItem(long id, string domain, string name, Uri avatar) {
            Id = id;
            Domain = domain;
            Name = name;
            Avatar = avatar;
        }
    }
}