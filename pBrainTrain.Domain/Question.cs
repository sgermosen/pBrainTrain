using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using pBrainTrain.Domain;

namespace pBrainTrain.Domain
{
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        [MaxLength(200, ErrorMessage = "Max Lenght is {1}")]
        [Display(Name = "Question")]
        public string QuestionName { get; set; }

        public int CategoryId { get; set; }

        [DataType(DataType.ImageUrl)] //we can add restriction of what type of and element can be nested on each field, for example, on this field as string i can insert any type of characters that i want, but, with this type of restriction, Razor knows what can pass from the view to the controller
        public string Picture { get; set; }

        [JsonIgnore]
        public virtual Category Category { get; set; }
        [JsonIgnore]
        public virtual ICollection<Answer> Answers { get; set; }

    }
}
