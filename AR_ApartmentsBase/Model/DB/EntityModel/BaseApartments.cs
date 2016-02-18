using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
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
   public static class BaseApartments
   {
      private static SAPREntities entities;

      public static SAPREntities ConnectEntities()
      {
         return new SAPREntities(new EntityConnection(Settings.Default.SaprCon));
      }

      /// <summary>
      /// Очистка базы
      /// </summary>
      public static void Clear()
      {
         using (var entities = ConnectEntities())
         {
            entities.F_S_FamilyInfos.RemoveRange(entities.F_S_FamilyInfos);
            entities.F_nn_FlatModules.RemoveRange(entities.F_nn_FlatModules);            

            entities.SaveChanges();
         }
      }

      /// <summary>
      /// Экспорт квартир в базу
      /// </summary>      
      public static void Export (List<Apartment> apartments)
      {
         using (entities = ConnectEntities())
         {
            // Загрузка таблиц
            entities.F_R_Flats.Load();
            entities.F_R_Modules.Load();
            entities.F_nn_FlatModules.Load();
            entities.F_nn_ElementParam_Value.Load();
            entities.F_nn_Elements_Modules.Load();
            entities.F_S_Elements.Load();
            entities.F_S_FamilyInfos.Load();            

            bool hasError = false;
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
                        var efmEnt = getElemInM(moduleEnt, elemEnt, elem);

                        // Заполнение параметров элемента
                        setElemParams(efmEnt, elemEnt, elem);
                     }
                  }                  
               }
               catch (Exception ex)
               {
                  Inspector.AddError(ex.Message, icon: System.Drawing.SystemIcons.Error);
                  hasError = true;
               }
            }
            if (!hasError)
            {
               // Сохранение изменений
               entities.SaveChanges();
            }            
         }
      }      

      private static F_R_Flats getFlat(Apartment apart)
      {
         var flatEnt = entities.F_R_Flats.Local.SingleOrDefault(f => f.WORKNAME.Equals(apart.BlockName, StringComparison.OrdinalIgnoreCase));
         if (flatEnt == null)
         {
            flatEnt = entities.F_R_Flats.Add(new F_R_Flats() { WORKNAME = apart.BlockName, COMMERCIAL_NAME = "" });
         }
         return flatEnt;
      }

      private static F_R_Modules getModuleRow(Module module)
      {
         var moduleEnt = entities.F_R_Modules.Local.SingleOrDefault(m => m.NAME_MODULE.Equals(module.BlockName, StringComparison.OrdinalIgnoreCase));
         if (moduleEnt == null)
         {
            moduleEnt = entities.F_R_Modules.Add(new F_R_Modules() { NAME_MODULE = module.BlockName });
         }
         return moduleEnt;
      }

      private static F_nn_FlatModules getFlatModuleRow(F_R_Flats flatEnt, F_R_Modules moduleEnt, Module module)
      {
         var fmEnt = flatEnt.F_nn_FlatModules.SingleOrDefault
                              (fm =>
                                    fm.ID_FLAT == moduleEnt.ID_MODULE &&
                                    fm.LOCATION.Equals(module.LocationPoint) &&
                                    fm.DIRECTION.Equals(module.Direction)
                              );         
         if (fmEnt == null)
         {
            fmEnt = entities.F_nn_FlatModules.Add(new F_nn_FlatModules()
            {
               F_R_Flats = flatEnt,
               F_R_Modules = moduleEnt,
               LOCATION = module.LocationPoint,
               DIRECTION = module.Direction,
               ANGLE = module.Rotation,
               REVISION = 0                
            });
         }
         return fmEnt;
      }

      private static F_S_Elements getElement(Element elem)
      {
         // Категория элемента
         var catEnt = entities.F_S_Categories.Single(c => c.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase));
         // Семейство элемента
         var famInfoEnt = entities.F_S_FamilyInfos.Local.SingleOrDefault(f =>
                  f.FAMILY_NAME.Equals(elem.FamilyName.Value, StringComparison.OrdinalIgnoreCase) &&
                  f.FAMILY_SYMBOL.Equals(elem.FamilySymbolName.Value, StringComparison.OrdinalIgnoreCase));
         if (famInfoEnt == null)
         {
            famInfoEnt = entities.F_S_FamilyInfos.Add(new F_S_FamilyInfos()
            {
               FAMILY_NAME = elem.FamilyName.Value,
               FAMILY_SYMBOL = elem.FamilySymbolName.Value                                              
            });
         }

         // Елемент
         var elemEnt = entities.F_S_Elements.Local.SingleOrDefault(e => 
                  e.ID_CATEGORY == catEnt.ID_CATEGORY &&
                  e.ID_FAMILY_INFO == famInfoEnt.ID_FAMILY_INFO);
         if (elemEnt == null)
         {
            elemEnt = entities.F_S_Elements.Add(new F_S_Elements()
            {
               F_S_Categories = catEnt,
               F_S_FamilyInfos = famInfoEnt                
            });
         }
         return elemEnt;
      }

      private static F_nn_Elements_Modules getElemInM(F_R_Modules mEnt, F_S_Elements elemEnt, Element elem)
      {
         var efmEnt = entities.F_nn_Elements_Modules.Add(new F_nn_Elements_Modules()
         {
             F_R_Modules = mEnt,
             DIRECTION = elem.Direction,
             LOCATION = elem.LocationPoint                                 
         });
         return efmEnt;
      }

      private static void setElemParams(F_nn_Elements_Modules emEnt, F_S_Elements elemEnt, Element elem)
      {
         // Параметры для этой категории элемента         
         var cpEnts = entities.F_nn_Category_Parameters.Where(cp => cp.ID_CATEGORY == elemEnt.ID_CATEGORY);
         foreach (var cp in cpEnts)
         {
            // Поиск этого параметра в блоке
            var param = elem.Parameters.Find(p => p.Name.Equals(cp.F_S_Parameters.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));            

            // если нет, то добавление            
            var val = entities.F_nn_ElementParam_Value.Add(new F_nn_ElementParam_Value()
            {                
                F_nn_Category_Parameters = cp,
                F_S_Elements = elemEnt, 
                PARAMETER_VALUE = param.Value
            });
            emEnt.F_nn_ElementParam_Value = val;
         }
      }
   }
}
