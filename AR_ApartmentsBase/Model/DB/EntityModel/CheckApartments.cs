using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.Revit;
using AR_ApartmentBase.Properties;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
   /// <summary>
   /// проверка квартир
   /// </summary>
   public class CheckApartments
   {
      public static void Check(List<Apartment> apartments)
      {
         var elementsAll = apartments.SelectMany(a => a.Modules.SelectMany(m => m.Elements));

         // Все категории и их свойства в базе
         using (SAPREntities entities = BaseApartments.NewEntities())
         {
            foreach (var elem in elementsAll)
            {
               string err = string.Empty;
               // Получить категорию элемента 
               var catEnt = entities.F_S_Categories.FirstOrDefault(c => c.NAME_RUS_CATEGORY.Equals(elem.TypeElement, StringComparison.OrdinalIgnoreCase));               
                                       
               if (catEnt == null)
               {
                  // Нет такой категории элемента
                  err += $"Не найдена категория в базе - {elem.TypeElement}";
               }
               else
               {
                  // Параметры для этой категории
                  var paramsEnt = catEnt.F_nn_Category_Parameters.Select(p => p.F_S_Parameters);
                  foreach (var paramEnt in paramsEnt)
                  {                     
                     if (!elem.Parameters.Exists(p => p.Name.Equals(paramEnt.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase)))
                     {
                        err += $"Нет необходимого параметра '{paramEnt.NAME_PARAMETER}'. ";
                     }
                  }
               }

               if (!string.IsNullOrEmpty(err))
               {
                  elem.Error = new Error($"Ошибка в элементе {elem.BlockName}: {err}.", elem.ExtentsInModel, elem.IdBlRefElement,
                     icon: System.Drawing.SystemIcons.Error);
               }
            }
         }
      }          
   }
}
