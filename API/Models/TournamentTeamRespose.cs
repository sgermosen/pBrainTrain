using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Domain;

namespace API.Models
{
    public class TournamentTeamRespose
    {
        
        public int TournamentTeamId { get; set; }
        public int TournamentGroupId { get; set; }
        public int TeamId { get; set; }
        public int MatchesPlayed { get; set; }
        public int MatchesWon { get; set; }
        public int MatchesLost { get; set; }
        public int MatchesTied { get; set; }
        public int FavorGoals { get; set; }
        public int AgainstGoals { get; set; }
        public int Points { get; set; }
        public int Position { get; set; }
       
      //  public   TournamentGroup TournamentGroup { get; set; }
        
        public   Team Team { get; set; }

       
    }
}