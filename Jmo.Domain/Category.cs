using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.Data.Domain
{
    public class Category : IEntity
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(250)]
        public string ImagenUrl { get; set; }

        public string ImageFullPath => string.IsNullOrEmpty(ImagenUrl)
            ? "https://braingameschallenges.azurewebsites.net//images/noimage.png"
            : $"https://braingameschallenges.azurewebsites.net{ImagenUrl.Substring(1)}";
    }
}
