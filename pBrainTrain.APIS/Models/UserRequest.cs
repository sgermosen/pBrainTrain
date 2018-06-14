using pBrainTrain.Domain;

namespace pBrainTrain.APIS.Models
{
    public class UserRequest : User
    {
        public string Password { get; set; }

        public byte[] PictureArray { get; set; }
    }
}