using System;
using System.Collections.Generic;
using System.Collections.Specialized;
//using System.Linq;
using System.Text;

namespace ScriptSqlConfig
{
    public static class ExtensionMethods
    {
        public static void Append(this StringCollection original, StringCollection newCollection)
        {
            foreach (string s in newCollection)
            {
                original.Add(s);
            }
        }
    }
}
