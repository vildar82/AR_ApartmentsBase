using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.Revit;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
   /// <summary>
   /// проверка квартир
   /// </summary>
   public class CheckApartments
   {
      public static void Check(List<Apartment> apartments)
      {
         //var elementsAll = apartments.SelectMany(a => a.Modules.SelectMany(m => m.Elements));

         //DataSet ds = new DataSet();
         //DataSetTableAdapters.CategoryParametersTableAdapter adapterCatParamView = new DataSetTableAdapters.CategoryParametersTableAdapter();
         //adapterCatParamView.Fill(ds.CategoryParameters);

         //foreach (var elem in elementsAll)
         //{
         //   string err = string.Empty;
         //   // Получить категорию элемента 
         //   var catParams = ds.CategoryParameters.Where(cp => cp.NAME_RUS_CATEGORY.Equals(elem.TypeElement, StringComparison.OrdinalIgnoreCase));
         //   if (catParams.Count() == 0)
         //   {
         //      // Нет такой категории элемента
         //      err += $"Не найдена категория в базе - {elem.TypeElement}";               
         //   }
         //   else
         //   {
         //      foreach (var cp in catParams)
         //      {
         //         if (!elem.Parameters.Exists(p => p.Name.Equals(cp.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase)))
         //         {
         //            err += $"Нет необходимого параметра '{cp.NAME_PARAMETER}'. ";
         //         }
         //      }
         //   }

         //   if (!string.IsNullOrEmpty(err))
         //   {
         //      elem.Error = new Error($"Ошибка в элементе {elem.BlockName}: {err}.", elem.ExtentsInModel, elem.IdBlRefElement,
         //         icon: System.Drawing.SystemIcons.Error);
         //   }            
         //}
      }          
   }
}
