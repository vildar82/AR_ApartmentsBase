using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_ApartmentBase.Model.DB
{
   public static class AccessUsers
   {
      private static List<string> _accessUsers = new List<string>() { "bystrovds", "khisyametdinovvt", "bimusr09", "bimusr08" };

      public static bool HasAccess()
      {
         return _accessUsers.Contains(Environment.UserName, StringComparer.OrdinalIgnoreCase);
      }
   }
}
