using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace pBrainTrain.Domain
{
    public class Answer
    {
        [Key]
        public int AnswerId { get; set; }

        [MaxLength(100, ErrorMessage = "Max Lenght is {1}")]
        [Display(Name = "Answer")]
        public string Name { get; set; }

        public int QuestionId { get; set; }

        [DataType(DataType.ImageUrl)] //we can add restriction of what type of and element can be nested on each field, for example, on this field as string i can insert any type of characters that i want, but, with this type of restriction, Razor knows what can pass from the view to the controller
        public string Picture { get; set; }

        public bool IsTheAnswer { get; set; }

        [JsonIgnore]
        public virtual Question Question { get; set; }
    }
}