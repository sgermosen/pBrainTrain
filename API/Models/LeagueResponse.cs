using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Domain;

namespace API.Classes
{
    public class LeagueResponse
    {
        public int LeagueId { get; set; }

       
        public string Name { get; set; }

      
        public string Logo { get; set; }

    
        public List<Team> Teams { get; set; }

    }
}