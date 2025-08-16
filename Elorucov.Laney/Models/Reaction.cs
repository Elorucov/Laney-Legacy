using Elorucov.Laney.Services;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elorucov.Laney.Models {
    public class Reaction : BaseViewModel {
        public int Id { get; private set; }
        public Uri ImagePath { get { return GetImagePathById(Id); } }

        private List<long> _members;
        private int _count;

        public List<long> Members { get { return _members; } set { _members = value; OnPropertyChanged(); OnPropertyChanged(nameof(MembersNames)); } }
        public int Count { get { return _count; } set { _count = value; OnPropertyChanged(); } }
        public string MembersNames { get { return GetMembersNames(); } }

        public Reaction(int id) {
            Id = id;
        }

        public override string ToString() {
            return $"Id: {Id}; count: {_count}; members: {String.Join(", ", _members)}";
        }

        private string GetMembersNames() {
            if (_members == null || _members.Count == 0) return null;
            List<string> names = new List<string>();

            foreach (long member in _members) {
                var info = AppSession.GetNameAndAvatar(member);
                names.Add(info != null ? String.Join(" ", info.Item1, info.Item2) : $"User {member}");
            }

            return String.Join("\n", names);
        }

        public static Uri GetImagePathById(int id, bool dontReturnPlaceholder = false) {
            ReactionAsset asset = AppSession.ReactionsAssets?.Where(a => a.ReactionId == id).FirstOrDefault();
            if (asset != null) {
                return new Uri(asset.Links.Static);
            } else {
                return !dontReturnPlaceholder ? new Uri($"ms-appx:///Assets/Reactions/placeholder.svg") : null;
            }
        }
    }
}