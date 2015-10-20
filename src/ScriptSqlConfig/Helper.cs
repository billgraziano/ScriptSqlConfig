using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;

namespace ScriptSqlConfig
{
    class Helper
    {
        public static void TestSMO()
        {
            /* Display the SMO version */
            //var ver = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Smo.Server)).GetName().Version;
            //Program.WriteMessage("SMO Version: " + ver.ToString());

            Assembly a1 = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Smo.Server));
            Program.WriteMessage("Microsoft.SqlServer.Smo:");
            DisplayAssemblyDetails(a1);

            Assembly a2 = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Common.ConnectionManager));
            Program.WriteMessage("");
            Program.WriteMessage("Microsoft.SqlServer.ConnectionInfo: ");
            DisplayAssemblyDetails(a2);

            Program.WriteMessage("");
            Program.WriteMessage("Microsoft.SqlServer.ConnectionInfoExtended:");
            DisplayAssemblyDetails(System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Trace.TraceFile)));

            Program.WriteMessage("");
            Program.WriteMessage("Microsoft.SqlServer.Management.Sdk.Sfc:");
            DisplayAssemblyDetails(System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Sdk.Sfc.DataProvider)));

            Program.WriteMessage("");
            Program.WriteMessage("Microsoft.SqlServer.SmoExtended:");
            DisplayAssemblyDetails(System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Smo.Backup)));


            Program.WriteMessage("");
            Program.WriteMessage("Microsoft.SqlServer.SqlEnum:");
            DisplayAssemblyDetails(System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Dmf.PolicyHealthState)));
            
            
#if DEBUG
            Console.WriteLine("Press any key to continue....");
            Console.ReadLine();
#endif
        }

        public static void DisplayAssemblyDetails(Assembly asm)
        {
            foreach (string s in asm.FullName.Split(','))
            {
                Program.WriteMessage("\t" + s.Trim());
            }
        }
    }
}
