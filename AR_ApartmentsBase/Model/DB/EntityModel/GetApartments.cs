using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.Revit;
using AR_ApartmentBase.Model.Revit.Elements;
using MoreLinq;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
   /// <summary>
   /// Получение списка квартир в базе
   /// </summary>
   public static class GetApartments
   {
      public static List<Apartment> GetAll()
      {
         // Преобразование квартир в базе в объекты Apartment
         List<Apartment> apartments = new List<Apartment>();
         using (var entities = BaseApartments.NewEntities())
         {
            foreach (var flatEnt in entities.F_R_Flats)
            {
               Apartment apart = new Apartment(flatEnt.WORKNAME);

               //Модули в квартире для последней ревизии
               var fmEnts = entities.F_nn_FlatModules.
                                 Where(fm => fm.ID_FLAT == flatEnt.ID_FLAT).
                                 GroupBy(g => g.LOCATION + g.DIRECTION).
                                 Select(g => new { fms = g.Select(i => i), revision = g.Max(k => k.REVISION) });
               foreach (var fmEnt in fmEnts)
               {
                  Module module = new Module(fmEnt.F_R_Modules.NAME_MODULE, apart, fmEnt.DIRECTION, fmEnt.LOCATION);

                 // Елементы
                 var elemEnts = entities.F_nn_Elements_FlatModules.Where(efm => efm.ID_FLAT_MODULE == fmEnt.ID_FLAT_MODULE);
                  foreach (var elemEnt in elemEnts)
                  {
                     List<Parameter> parameters = new List<Parameter>();
                     elemEnt.F_nn_ElementParam_Value.ForEach(p => parameters.Add(new Parameter()
                     {
                        Name = p.F_nn_Category_Parameters.F_S_Categories.NAME_RUS_CATEGORY,
                        Value = p.PARAMETER_VALUE
                     }));
                     Element elem = new Element(module, elemEnt.DIRECTION, elemEnt.LOCATION_POINT,
                        elemEnt.F_S_Elements.F_S_FamilyInfos.FAMILY_NAME,
                        elemEnt.F_S_Elements.F_S_FamilyInfos.FAMILY_SYMBOL, parameters);
                  }
               }
            }
         }
         return apartments;
      }
   }
}
