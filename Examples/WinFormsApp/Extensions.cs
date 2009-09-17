using System;
using System.Collections.Generic;
//using System.IO;


namespace Ionic.Zip.Examples.WinForms
{
    public static class Extensions
    {
        public static string XmlEscapeIexcl(this String s)
        {
            while (s.Contains("¡"))
            {
                s = s.Replace("¡", "&#161;");
            }
            return s;
        }
        public static string XmlUnescapeIexcl(this String s)
        {
            while (s.Contains("&#161;"))
            {
                s = s.Replace("&#161;", "¡");
            }
            return s;
        }

        public static List<String> ToList(this System.Windows.Forms.AutoCompleteStringCollection coll)
        {
            var list = new List<String>();
            foreach (string  item in coll)
            {
                list.Add(item);
            }
            return list;
        }
        
    }


}