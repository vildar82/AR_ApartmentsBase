using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_ApartmentBase.Model.Revit
{
   [Flags]
   public enum EnumBaseStatus
   {
      /// <summary>
      /// По умолчанию - не установлен
      /// </summary>
      None = 0x0,

      /// <summary>
      /// Есть в базе, без изменений
      /// </summary>
      OK = 0x1,

      /// <summary>
      /// Есть в базе, но с изменениями
      /// </summary>      
      Changed = 0x2,

      /// <summary>
      /// Новый, или с параметрами, которых не было раньше
      /// </summary>
      New = 0x4,

      /// <summary>
      /// Нет в файле dwg библиотеки квартир
      /// </summary>
      NotInDwg = 0x8,

      /// <summary>
      /// Ошибка в блоке
      /// </summary>
      Error = 0x16      
   }
}
