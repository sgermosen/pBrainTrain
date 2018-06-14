using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pBrainTrain.Domain
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [MaxLength(100, ErrorMessage = "Max Lenght is {1}")]
        [Display(Name = "Category")]
        public string Name { get; set; }

        [DataType(DataType.ImageUrl)] //we can add restriction of what type of and element can be nested on each field, for example, on this field as string i can insert any type of characters that i want, but, with this type of restriction, Razor knows what can pass from the view to the controller
        public string Picture { get; set; }

        [JsonIgnore]
        public virtual ICollection<Question> Questions { get; set; }
    }
}
