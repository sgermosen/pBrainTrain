namespace Jmo.Common.Models
{
    public class PredictionResponse
    {
        public int Id { get; set; }

        public int? GoalsLocal { get; set; }

        public int? GoalsVisitor { get; set; }

        public int Points { get; set; }

        public UserResponse User { get; set; }

        public MatchResponse Match { get; set; }
    }

}
