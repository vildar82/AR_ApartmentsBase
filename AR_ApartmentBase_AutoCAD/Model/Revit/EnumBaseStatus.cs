using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_ApartmentBase.AutoCAD
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

   public static class BaseColor
   {
      public static Color GetColor(EnumBaseStatus status)
      {
         Color resColor = Color.Black;
         if (status.HasFlag(EnumBaseStatus.Error))
         {
            // Ошибка               
            resColor = Color.Red;
         }
         else if (status.HasFlag(EnumBaseStatus.New))
         {
            // Новый
            resColor = Color.Lime;
         }
         else if (status.HasFlag(EnumBaseStatus.NotInDwg))
         {
            // Нет в чертеже, но есть в базе
            resColor = Color.DarkViolet;
         }
         else if (status.HasFlag(EnumBaseStatus.Changed))
         {
            // Изменился
            resColor = Color.Olive;
         }
         else if (status == EnumBaseStatus.OK)
         {
            // Не изменился
            resColor = Color.Blue;
         }
         return resColor;
      }
   }
}
