using System.Collections.Generic;

namespace Jmo.Common.Models
{
    public class GroupResponse
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public ICollection<GroupDetailResponse> GroupDetails { get; set; }

        public ICollection<MatchResponse> Matches { get; set; }
    }

}
