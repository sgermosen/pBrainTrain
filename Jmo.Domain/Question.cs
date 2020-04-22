using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.Data.Domain
{
    public class Question : IEntity
    {
        public int Id { get; set; }

        [StringLength(150)]
        public string Questionant { get; set; }

        [StringLength(250)]
        public string ImagenUrl { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<Answer> Answers { get; set; }

        public string ImageFullPath => string.IsNullOrEmpty(ImagenUrl)
           ? "https://braingameschallenges.azurewebsites.net//images/noimage.png"
           : $"https://braingameschallenges.azurewebsites.net{ImagenUrl.Substring(1)}";

        public Question()
        {
            Answers = new Collection<Answer>();
        }
    }
}
