namespace Jmo.Common.Models
{
    public class AnswerResponse
    {
        public int Id { get; set; }

        public string Option { get; set; }

        public bool IsCorrect { get; set; }

        public int QuestionId { get; set; }
        public QuestionResponse Question { get; set; }
    }
}