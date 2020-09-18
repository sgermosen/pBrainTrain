using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Jmo.Common.Models
{
    public class CategoryResponse
    {
        public int Id { get; set; }
         
        public string Name { get; set; }
     
       // public string ImagenUrl { get; set; }

        public string ImageFullPath { get; set; }

        public ICollection<QuestionResponse> Questions { get; set; }

        public CategoryResponse()
        {
            Questions = new Collection<QuestionResponse>();
        }
    }
}