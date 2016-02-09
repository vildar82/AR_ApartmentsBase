using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AR_SEM.Commands))]

namespace AR_SEM
{
   public class Commands
   {
      [CommandMethod("PIK", "AR-SEM-ExportApartments", CommandFlags.Modal | CommandFlags.UsePickSet)]
      public void ExportApartments()
      {

      }
   }
}
