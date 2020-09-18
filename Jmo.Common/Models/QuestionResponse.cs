using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Jmo.Common.Models
{
    public class QuestionResponse
    {
        public int Id { get; set; }

        public string Questionant { get; set; }

        public string ImageFullPath { get; set; }

        public int CategoryId { get; set; }
        public CategoryResponse Category { get; set; }

        public ICollection<ChoiseResponse> Choises { get; set; }

        public QuestionResponse()
        {
            Choises = new Collection<ChoiseResponse>();
        }
    }
}
