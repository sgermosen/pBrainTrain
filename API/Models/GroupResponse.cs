using System.Collections.Generic;
using Domain;

namespace API.Models
{
    public class GroupResponse
    {
        public int GroupId { get; set; }

        public string Name { get; set; }
        
        public int OwnerId { get; set; }
      
        public   User Owner { get; set; }
        
        public   List<GroupUserResponse> GroupUsers { get; set; }

    }
}