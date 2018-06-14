using pBrainTrain.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace pBrainTrain.Backend.Models
{
    public class CategoryView : Category
    {
        public HttpPostedFileBase ImageFile { get; set; }
    }
}