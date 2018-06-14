using pBrainTrain.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace pBrainTrain.Backend.Models
{
    public class AnswerView:Answer
    {
        public HttpPostedFileBase ImageFile { get; set; }
    }
}