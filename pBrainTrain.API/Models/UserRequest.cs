using pBrainTrain.Domain;

namespace pBrainTrain.API.Models
{
    public class UserRequest : User
    {
        public string Password { get; set; }

        public byte[] PictureArray { get; set; }
    }
}