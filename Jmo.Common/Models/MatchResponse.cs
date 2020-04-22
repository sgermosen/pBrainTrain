using System;
using System.Collections.Generic;
using System.Text;

namespace Jmo.Common.Models
{

    public class MatchResponse
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public DateTime DateLocal => Date.ToLocalTime();

        public TeamResponse Local { get; set; }

        public TeamResponse Visitor { get; set; }

        public int? GoalsLocal { get; set; }

        public int? GoalsVisitor { get; set; }

        public bool IsClosed { get; set; }

        public ICollection<PredictionResponse> Predictions { get; set; }
    }

}
