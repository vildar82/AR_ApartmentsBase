using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.Revit;
using AR_ApartmentBase.Model.Revit.Elements;
using AR_ApartmentBase.Properties;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
   public class ExportModel
   {
      private Entities entities;

      public void Export (List<Apartment> apartments)
      {
         DbConnection dbCon = new EntityConnection(Settings.Default.SAPRConnectionString);         
         entities = new Entities(dbCon, true);         

         foreach (Apartment apart in apartments)
         {
            try
            {
               // Квартира
               var flatEnt = getFlat(apart);

               // Модули
               foreach (var module in apart.Modules)
               {
                  // Модуль
                  var moduleEnt = getModuleRow(module);
                  // Квартира-модуль
                  var fmEnt = getFlatModuleRow(flatEnt, moduleEnt, module);

                  // Элементы                  
                  foreach (var elem in module.Elements)
                  {
                     // Определение элемента в базе                                          
                     var elemEnt = getElement(elem);
                     // Привязка элемента к модулю  
                     var efmEnt = getElemInFM(fmEnt, elemEnt, elem);

                     // Заполнение параметров элемента
                     setElemParams(efmEnt, elemEnt, elem);
                  }
               }
            }
            catch (Exception ex)
            {
               Inspector.AddError(ex.Message, icon: System.Drawing.SystemIcons.Error);
            }
         }
      }      

      private F_R_Flats getFlat(Apartment apart)
      {
         var flatEnt = entities.F_R_Flats.SingleOrDefault(f => f.WORKNAME.Equals(apart.BlockName, StringComparison.OrdinalIgnoreCase));
         if (flatEnt == null)
         {
            flatEnt = entities.F_R_Flats.Add(new F_R_Flats() { WORKNAME = apart.BlockName });
         }
         return flatEnt;
      }

      private F_R_Modules getModuleRow(Module module)
      {
         var moduleEnt = entities.F_R_Modules.SingleOrDefault(m => m.NAME_MODULE.Equals(module.BlockName, StringComparison.OrdinalIgnoreCase));
         if (moduleEnt == null)
         {
            entities.F_R_Modules.Add(new F_R_Modules() { NAME_MODULE = module.BlockName });
         }
         return moduleEnt;
      }

      private F_nn_FlatModules getFlatModuleRow(F_R_Flats flatEnt, F_R_Modules moduleEnt, Module module)
      {
         var fmEnt = entities.F_nn_FlatModules.SingleOrDefault(fm => fm.F_R_Flats == flatEnt &&
                                                   fm.F_R_Modules == moduleEnt &&
                                                   fm.LOCATION == module.LocationPoint &&
                                                   fm.DIRECTION == module.Direction);
         if (fmEnt == null)
         {
            entities.F_nn_FlatModules.Add(new F_nn_FlatModules()
            {
               F_R_Flats = flatEnt,
               F_R_Modules = moduleEnt,
               LOCATION = module.LocationPoint,
               DIRECTION = module.Direction,
               REVISION = 0
            });
         }
         return fmEnt;
      }

      private F_S_Elements getElement(Element elem)
      {
         // Категория элемента
         var catEnt = entities.F_S_Categories.Single(c => c.NAME_RUS_CATEGORY.Equals(elem.TypeElement, StringComparison.OrdinalIgnoreCase));
         // Семейство элемента
         var famInfoEnt = entities.F_S_FamilyInfos.SingleOrDefault(f =>
                  f.FAMILY_NAME.Equals(elem.FamilyName.Value, StringComparison.OrdinalIgnoreCase) &&
                  f.FAMILY_SYMBOL.Equals(elem.FamilySymbolName.Value, StringComparison.OrdinalIgnoreCase));
         if (famInfoEnt == null)
         {
            entities.F_S_FamilyInfos.Add(new F_S_FamilyInfos()
            {
               FAMILY_NAME = elem.FamilyName.Value,
               FAMILY_SYMBOL = elem.FamilySymbolName.Value,                
            });
         }

         // Елемент
         var elemEnt = entities.F_S_Elements.SingleOrDefault(e => 
                  e.F_S_Categories == catEnt &&
                  e.F_S_FamilyInfos == famInfoEnt);
         if (elemEnt == null)
         {
            entities.F_S_Elements.Add(new F_S_Elements() { F_S_Categories = catEnt, F_S_FamilyInfos = famInfoEnt });
         }
         return elemEnt;
      }

      private F_nn_Elements_FlatModules getElemInFM(F_nn_FlatModules fmEnt, F_S_Elements elemEnt, Element elem)
      {
         var efmEnt = entities.F_nn_Elements_FlatModules.SingleOrDefault(efm =>
               efm.F_nn_FlatModules == fmEnt &&
               efm.F_S_Elements == elemEnt &&
               efm.LOCATION_POINT.Equals(elem.LocationPoint, StringComparison.OrdinalIgnoreCase) &&
               efm.DIRECTION.Equals(elem.Direction, StringComparison.OrdinalIgnoreCase)
               );
         if (efmEnt == null)
         {
            entities.F_nn_Elements_FlatModules.Add(new F_nn_Elements_FlatModules()
            {
               F_nn_FlatModules = fmEnt,
               F_S_Elements = elemEnt,
               LOCATION_POINT = elem.LocationPoint,
               DIRECTION = elem.Direction
            });
         }
         return efmEnt;
      }

      private void setElemParams(F_nn_Elements_FlatModules efmEnt, F_S_Elements elemEnt, Element elem)
      {
         // Параметры для этой категории элемента         
         var cpEnts = entities.F_nn_Category_Parameters.Where(cp => cp.F_S_Categories == elemEnt.F_S_Categories);
         foreach (var cp in cpEnts)
         {
            // Поиск параметра и его типа
            var paramEnt = entities.F_S_Parameters.Single(p => p.ID_PARAMETER == cp.ID_PARAMETER);

            // Поиск этого параметра в блоке
            var param = elem.Parameters.Find(p => p.Name.Equals(paramEnt.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));

            // Проверка естьли уже такая запись в таблице F_nn_ElementParam_Value - елем-кв-модуля и значения параметра
            var val = entities.F_nn_ElementParam_Value.SingleOrDefault(epv => 
               epv.F_nn_Elements_FlatModules == efmEnt &&
               epv.F_nn_Category_Parameters == cp &&
               epv.PARAMETER_VALUE.Equals(param.Value));

            // если нет, то добавление
            if (val == null)
            {
               entities.F_nn_ElementParam_Value.Add(new F_nn_ElementParam_Value()
               {
                  F_nn_Category_Parameters = cp,
                  F_nn_Elements_FlatModules = efmEnt,
                  PARAMETER_VALUE = param.Value
               });
            }
         }
      }
   }
}
