using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace pBrainTrain.Backend.Helpers
{
    public class Files
    {//those are on my github totally free, so dont worry for this 
        public static string UploadPhoto(HttpPostedFileBase file, string folder, string name)
        {
            var pic = string.Empty;

            if (file == null) return pic;
            // pic = Path.GetFileName(file.FileName);
            pic = name == "" ? Path.GetFileName(file.FileName) : name;
            var path = Path.Combine(HttpContext.Current.Server.MapPath(folder), pic);
            // path = Path.Combine(HttpContent.Current.Server.MapPath(folder), pic);                
            file.SaveAs(path);
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    file.InputStream.CopyTo(ms);
            //    byte[] array = ms.GetBuffer();
            //}

            return pic;
        }

        public static bool UploadPhoto(MemoryStream stream, string folder, string name)
        {

            try
            {

                stream.Position = 0;
                var path = Path.Combine(HttpContext.Current.Server.MapPath(folder), name);
                File.WriteAllBytes(path, stream.ToArray());

            }
            catch (System.Exception)
            {

                return false;

            }

            return true;

        }
    }
}