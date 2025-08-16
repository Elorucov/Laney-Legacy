using System.Collections.Generic;

namespace Elorucov.Laney.Models.Stats {
    public class MemberCounters {
        public long MemberId { get; private set; }
        public int Count { get; set; }
        public int UniqCount { get; set; }
        public double MoneySentTotal { get; set; }
        public int AttachmentsTotal { get; set; }
        public int AudioDuration { get; set; }
        public int ReactionsTotal { get; set; }
        public Dictionary<int, int> Reactions { get; set; } = new Dictionary<int, int>(); // key — id реакции, value — кол-во.

        public MemberCounters(long memberId) {
            MemberId = memberId;
        }
    }
}