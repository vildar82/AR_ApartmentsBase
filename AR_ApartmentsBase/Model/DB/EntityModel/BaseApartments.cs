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
using MoreLinq;

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
      /// Экспорт квартир в базу.
      /// </summary>      
      public static List<ExportDBInfo> Export (List<Apartment> apartments)
      {
         List<ExportDBInfo> exportInfos = new List<ExportDBInfo>();
         if (apartments.Count == 0)
         {
            return exportInfos;
         }

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

            try
            {
               // Новые и изменившиеся квартиры
               var apartmentsToDb = apartments.Where(a =>
                        a.BaseStatus == EnumBaseStatus.Changed ||
                        a.BaseStatus == EnumBaseStatus.New);

               var otherApartments = apartments.Where(a => !apartmentsToDb.Contains(a));
               foreach (var otherApart in otherApartments)
               {
                  ExportDBInfo exportInfp = new ExportDBInfo()
                  {
                     NameElement = otherApart.Name,
                     ExporInfo = $"Квартира не записана в базу, статус {otherApart.BaseStatus}."
                  };
               }

               foreach (Apartment apart in apartmentsToDb)
               {
                  // Определение квартиры
                  var flatEnt = defineFlatEnt(apart);                  
               }

               // Модули - новые или с изменениями
               var modules = apartments.SelectMany(a => a.Modules)
                              .Where(m =>
                                   m.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
                                   m.BaseStatus.HasFlag(EnumBaseStatus.New))
                              .GroupBy(g => g.Name)
                              .Select(g => g.First());

               foreach (var module in modules)
               {
                  // Элементы                  
                  foreach (var elem in module.Elements)
                  {
                     // Определение элемента в базе                                          
                     var elemEnt = getElement(elem);

                     var moduleEnt = getModuleEnt(module);

                     // Добавление элемента в модуль
                     var elemInModEnt = addElemToModule(elemEnt, moduleEnt, elem);

                     // Заполнение параметров
                     setElemValues(elemInModEnt, elemEnt, elem);
                  }
               }

               // Сохранение изменений
               entities.SaveChanges();
            }
            catch (Exception ex)
            {
               Inspector.AddError(ex.Message, icon: System.Drawing.SystemIcons.Error);               
            }            
         }
         return exportInfos;
      }      

      private static F_R_Flats defineFlatEnt(Apartment apart)
      {
         // Определение квартиры - для новой записи квартиры выполняется привязка модулей.
         F_R_Flats flatEnt = null;
         int revision = 0;
         if (apart.BaseStatus == EnumBaseStatus.Changed)
         {
            // Новая ревизия квартиры
            var lastRevision = entities.F_R_Flats.Local
                              .Where(f => f.WORKNAME.Equals(apart.Name, StringComparison.OrdinalIgnoreCase))
                              .Max(r => r.REVISION);
            revision = lastRevision + 1;            
         }
         else
         {
            flatEnt = entities.F_R_Flats.Local
                              .Where(f => f.WORKNAME.Equals(apart.Name, StringComparison.OrdinalIgnoreCase))                              
                              .OrderByDescending(r => r.REVISION).FirstOrDefault();
         }

         if (flatEnt == null)
         {
            flatEnt = entities.F_R_Flats.Add(new F_R_Flats() { WORKNAME = apart.Name, COMMERCIAL_NAME = "", REVISION = revision });
            // Привязка модулей
            attachModulesToFlat(flatEnt, apart);
         }

         return flatEnt;
      }

      private static void attachModulesToFlat(F_R_Flats flatEnt, Apartment apart)
      {
         foreach (var module in apart.Modules)
         {
            // Модуль
            var moduleEnt = getModuleEnt(module);
            // Квартира-модуль
            var fmEnt = getFMEnt(flatEnt, moduleEnt, module);
         }
      }

      private static F_R_Modules getModuleEnt(Module module)
      {
         F_R_Modules moduleEnt = null;
         int revision = 0;
         if (module.BaseStatus==EnumBaseStatus.Changed)
         {
            // Новая ревизия модуля
            var lastRevision = entities.F_R_Modules.Local
                                 .Where(m => m.NAME_MODULE.Equals(module.Name, StringComparison.OrdinalIgnoreCase))
                                 .Max(r => r.REVISION);
            revision = lastRevision + 1;
         }
         else
         {
            moduleEnt = entities.F_R_Modules.Local
                                 .Where(m => m.NAME_MODULE.Equals(module.Name, StringComparison.OrdinalIgnoreCase))                                 
                                 .OrderByDescending(r => r.REVISION).FirstOrDefault();                                 
         }
                  
         if (moduleEnt == null)
         {
            moduleEnt = entities.F_R_Modules.Add(new F_R_Modules() { NAME_MODULE = module.Name, REVISION = revision });
         }
         return moduleEnt;
      }

      private static F_nn_FlatModules getFMEnt(F_R_Flats flatEnt, F_R_Modules moduleEnt, Module module)
      {
         var fmEnt = flatEnt.F_nn_FlatModules.SingleOrDefault
                              (fm =>
                                    fm.ID_FLAT == moduleEnt.ID_MODULE &&
                                    fm.LOCATION.Equals(module.LocationPoint) &&
                                    fm.DIRECTION.Equals(module.Direction)                                                                   
                              );         
         if (fmEnt == null)
         {            
            fmEnt = new F_nn_FlatModules()
            {
               F_R_Flats = flatEnt,
               F_R_Modules = moduleEnt,
               LOCATION = module.LocationPoint,
               DIRECTION = module.Direction,
               ANGLE = module.Rotation               
            };
            flatEnt.F_nn_FlatModules.Add(fmEnt);
         }
         return fmEnt;
      }

      private static F_S_Elements getElement(Element elem)
      {
         // Категория элемента
         var catEnt = entities.F_S_Categories.Single(
                       c => c.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase));
         // Семейство элемента         
         var famInfoEnt = entities.F_S_FamilyInfos.Local.SingleOrDefault(f =>
                  f.FAMILY_NAME.Equals(elem.FamilyName.Value, StringComparison.OrdinalIgnoreCase) &&
                  f.FAMILY_SYMBOL.Equals(elem.FamilySymbolName.Value, StringComparison.OrdinalIgnoreCase));
         if (famInfoEnt == null)
         {
            famInfoEnt = entities.F_S_FamilyInfos.Add(
               new F_S_FamilyInfos()
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
               F_S_FamilyInfos = famInfoEnt,
            });
         }
         return elemEnt;
      }

      private static F_nn_Elements_Modules addElemToModule(F_S_Elements elemEnt, F_R_Modules moduleEnt, Element elem)
      {
         var emEnt = new F_nn_Elements_Modules()
         {
            F_R_Modules = moduleEnt,
            F_S_Elements = elemEnt,
            DIRECTION = elem.Direction,
            LOCATION = elem.LocationPoint
         };
         moduleEnt.F_nn_Elements_Modules.Add(emEnt);
         return emEnt;
      }

      private static void setElemValues(F_nn_Elements_Modules emEnt, F_S_Elements elemEnt, Element elem)
      {         
         // Параметры элемента которые нужно заполнить
         foreach (var paramEnt in elemEnt.F_S_Categories.F_nn_Category_Parameters)
         {
            var val = elem.Parameters.Single(p => p.Name.Equals(paramEnt.F_S_Parameters.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));

            var elemValue = new F_nn_ElementParam_Value()
            {
               F_nn_Category_Parameters = paramEnt,
               F_nn_Elements_Modules = emEnt,
               PARAMETER_VALUE = val.Value
            };
            emEnt.F_nn_ElementParam_Value.Add(elemValue);
         }          
      }
   }
}
