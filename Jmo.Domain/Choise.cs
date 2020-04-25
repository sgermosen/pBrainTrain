using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.Data.Domain
{
    public class Choise : IEntity
    {
        public int Id { get; set; }

        [StringLength(1500)]
        public string Option { get; set; }

        public bool IsCorrect { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; }
    }
}
