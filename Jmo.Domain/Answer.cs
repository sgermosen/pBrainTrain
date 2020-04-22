using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.Data.Domain
{
    public class Answer : IEntity
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Option { get; set; }

        public bool IsCorrect { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; }
    }
}
