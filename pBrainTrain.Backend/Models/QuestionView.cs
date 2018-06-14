using pBrainTrain.Domain;
using System.Web;

namespace pBrainTrain.Backend.Models
{
    public class QuestionView :Question
    {
        public HttpPostedFileBase ImageFile { get; set; }

    }
}