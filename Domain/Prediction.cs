using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Domain
{
    public class Prediction
    {
        [Key]
        public int PredictionId { get; set; }

        [Index("Prediction_UserId_MatchId_Index", IsUnique = true, Order = 1)]
        [Display(Name = "User")]
        public int UserId { get; set; }

        [Index("Prediction_UserId_MatchId_Index", IsUnique = true, Order = 2)]
        [Display(Name = "Match")]
        public int MatchId { get; set; }

        [Display(Name = "Local goals")]
        public int LocalGoals { get; set; }

        [Display(Name = "Visitor goals")]
        public int VisitorGoals { get; set; }

        public int Points { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; }
        [JsonIgnore]
        public virtual Match Match { get; set; }
    }

}
