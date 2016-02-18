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
using AR_ApartmentBase.Model.Revit.Elements;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
   /// <summary>
   /// проверка квартир
   /// </summary>
   public class CheckApartments
   {
      //   private static void CheckElementsOld(List<Apartment> apartments, SAPREntities entities)
      //   {
      //      var elementsAll = apartments.SelectMany(a => a.Modules.SelectMany(m => m.Elements));

      //      // Все категории и их свойства в базе

      //      foreach (var elem in elementsAll)
      //      {
      //         string err = string.Empty;
      //         // Получить категорию элемента 
      //         var catEnt = entities.F_S_Categories.FirstOrDefault(c => c.NAME_RUS_CATEGORY.Equals(elem.TypeElement, StringComparison.OrdinalIgnoreCase));

      //         if (catEnt == null)
      //         {
      //            // Нет такой категории элемента
      //            err += $"Не найдена категория в базе - {elem.TypeElement}";
      //         }
      //         else
      //         {
      //            // Параметры для этой категории
      //            var paramsEnt = catEnt.F_nn_Category_Parameters.Select(p => p.F_S_Parameters);
      //            foreach (var paramEnt in paramsEnt)
      //            {
      //               if (!elem.Parameters.Exists(p => p.Name.Equals(paramEnt.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase)))
      //               {
      //                  err += $"Нет необходимого параметра '{paramEnt.NAME_PARAMETER}'. ";
      //               }
      //            }
      //         }

      //         if (!string.IsNullOrEmpty(err))
      //         {
      //            elem.Error = new Error($"Ошибка в элементе {elem.BlockName}: {err}.", elem.ExtentsInModel, elem.IdBlRefElement,
      //               icon: System.Drawing.SystemIcons.Error);
      //         }
      //      }
      //   }

      //   public static void CheckAndChangesOld(List<Apartment> apartments)
      //   {
      //      // Проверка квартир и проверка изменений.
      //      using (SAPREntities entities = BaseApartments.NewEntities())
      //      {
      //         // Проверка всех блоков элементов - 
      //         CheckElements(apartments, entities);

      //         foreach (var apart in apartments)
      //         {
      //            var apartEnt = entities.F_R_Flats.SingleOrDefault(f => f.WORKNAME.Equals(apart.BlockName, StringComparison.OrdinalIgnoreCase));
      //            if (apartEnt == null)
      //            {
      //               // Нет такой квартиры в базе
      //               apart.BaseStatus |= EnumBaseStatus.NotInBase;
      //            }

      //         }
      //      }
      //   }

      /// <summary>
      /// Проверка квартир в чертеже и в базе
      /// </summary>      
      public static void Check(List<Apartment> apartments, List<Apartment> apartmentsInBase)
      {
         // Проверка квартир и проверка изменений.         
         // квартиры которых нет в чертеже
         var apartmentsMissingInDwg = apartmentsInBase.Where(
            aBase => !apartments.Any(a => a.BlockName.Equals(aBase.BlockName, StringComparison.OrdinalIgnoreCase))).ToList();
         apartmentsMissingInDwg.ForEach(a =>
                                 {
                                    a.BaseStatus = EnumBaseStatus.NotInDwg;
                                    apartments.Add(a);
                                 });

         foreach (var apart in apartments)
         {
            string errApart = string.Empty;

            // Проверка квартиры
            checkApart(apart, apartmentsInBase, ref errApart);

            if (!string.IsNullOrEmpty(errApart))
            {
               apart.Error = new Error(errApart, apart.ExtentsInModel, apart.IdBlRef, System.Drawing.SystemIcons.Error);
            }
         }
         // Проверить элементы в блоках - наличие необходимых параметров, категории               
         var allElements = apartments.SelectMany(a => a.Modules.SelectMany(m => m.Elements)).ToList();
         checkElements(allElements);
      }    

      private static void checkApart(Apartment apart, List<Apartment> apartmentsInBase ,ref string errApart)
      {
         // Поиск квартиры в базе
         Apartment apartInBase = null;
         try
         {
            apartInBase = apartmentsInBase.SingleOrDefault(a => a.BlockName.Equals(apart.BlockName, StringComparison.OrdinalIgnoreCase));
         }
         catch
         {
            // Ошибка в базе - несколько квартир с одним именем
            apart.BaseStatus |= EnumBaseStatus.Error;
            errApart += "В базе несколько квартир с таким именем - нужно устранить ошибку в базе. ";
            return;
         }

         if (apartInBase == null)
         {
            // Квартиры нет в базе
            apart.BaseStatus |= EnumBaseStatus.NotInBase;
            errApart += "Квартиры нет в базе. ";
         }
         else
         {
            //Сверка модулей
            if (apart.Modules.Count == 0)
            {
               errApart += "В блоке квартиры нет модулей. ";
               apart.BaseStatus |= EnumBaseStatus.Error;
            }
            else
            {
               if (apart.Modules.Count != apartInBase.Modules.Count)
               {
                  // Не совпадают количества модулей в базе и в двг файле
                  errApart += $"Не совпадает количество модулей в базе '{apartInBase.Modules.Count}' и в чертеже '{apart.Modules.Count}'. ";
                  apart.BaseStatus = EnumBaseStatus.Changed;
               }

               foreach (var module in apart.Modules)
               {
                  string errModule = string.Empty;

                  // проверка модуля
                  checkModule(module, apartInBase, ref errModule);

                  if (!string.IsNullOrEmpty(errModule))
                  {                     
                     module.Error = new Error(errModule, module.ExtentsInModel, module.IdBlRefModule, System.Drawing.SystemIcons.Error);                     
                  }                  
               }
               if (apart.Modules.Any(m=>m.BaseStatus.HasFlag(EnumBaseStatus.Error)))
               {
                  errApart += "Есть модули с ошибками. ";
                  apart.BaseStatus |= EnumBaseStatus.Error;
               }
               if (apart.Modules.Any(m => m.BaseStatus.HasFlag(EnumBaseStatus.Changed)))
               {
                  errApart += "Есть изменившиеся модули. ";
                  apart.BaseStatus |= EnumBaseStatus.Changed;
               }
            }
         }         
      }

      private static void checkModule(Module module, Apartment apartInBase, ref string errModule)
      {         
         // Есть ли модуль с таким именем в квартирах из базы
         if (apartInBase.Modules.Exists(m => m.BlockName.Equals(module.BlockName)))
         {
            // Проверка параеметров модудля
            Module moduleInBase = null;
            try
            {
               moduleInBase = apartInBase.Modules.SingleOrDefault(m =>
                              m.BlockName.Equals(module.BlockName, StringComparison.OrdinalIgnoreCase) &&
                              m.Direction.Equals(module.Direction) &&
                              m.LocationPoint.Equals(module.LocationPoint));
            }
            catch
            {
               // Несколько одинаковых модулей
               errModule += "Несколько одинаковых модулей в базе. ";
               module.BaseStatus |= EnumBaseStatus.Error;
               return;
            }
            if (moduleInBase == null)
            {
               // Не найден модуль с такими параметрами - изменился
               module.BaseStatus |= EnumBaseStatus.Changed;
               errModule += "Параметры модуля изменились. ";               
            }
            // Модуль с такими параметрами найден в базе квартир
            else
            {
               // Проверка элементов в модуле          
               if (module.Elements.Count==0)
               {
                  errModule += "В блоке модуля нет элементов. ";
                  module.BaseStatus |= EnumBaseStatus.Error;                  
               }
               else
               {
                  if (module.Elements.Count != moduleInBase.Elements.Count)
                  {
                     // Не совпадают количества элементов в модуле в базе и в двг файле
                     errModule += $"Не совпадает количество элементов в базе '{moduleInBase.Elements.Count}' и в чертеже '{module.Elements.Count}'. ";
                     module.BaseStatus = EnumBaseStatus.Error;                     
                  }

                  foreach (var elem in module.Elements)
                  {
                     string errElem = string.Empty;

                     // Проверка элемента
                     checkElement(elem, moduleInBase, ref errElem);

                     if (!string.IsNullOrEmpty(errElem))
                     {
                        elem.Error = new Error(errElem, elem.ExtentsInModel, elem.IdBlRefElement, System.Drawing.SystemIcons.Error);
                     }
                  }

                  if (module.Elements.Any(e=>e.BaseStatus.HasFlag( EnumBaseStatus.Error)))
                  {
                     errModule += "Есть елементы с ошибками. ";
                     module.BaseStatus |= EnumBaseStatus.Error;
                  }
                  if (module.Elements.Any(e => e.BaseStatus.HasFlag(EnumBaseStatus.Changed)))
                  {
                     errModule += "Есть изменившиеся елементы. ";
                     module.BaseStatus |= EnumBaseStatus.Changed;
                  }
               }
            }
         }
         else
         {
            // Нет такого модуля в базе
            module.BaseStatus |= EnumBaseStatus.NotInBase;
            errModule += "Модуля с таким именем нет в базе. ";
         }         
      }

      private static void checkElement(Element elem, Module moduleInBase, ref string errElem)
      {
         Element elemInBase = null;
         try
         {
            elemInBase = moduleInBase.Elements.SingleOrDefault
                        (e =>
                              e.CategoryElement.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase) &&
                              e.FamilyName.Equals(elem.FamilyName) &&
                              e.FamilySymbolName.Equals(elem.FamilySymbolName) &&                              
                              Parameter.Equal(e.Parameters, elem.Parameters)
                        );
         }
         catch
         {
            // Несколько одинаковых элементов            
            errElem += "Несколько одинаковых элементов в базе. ";
            elem.BaseStatus |= EnumBaseStatus.Error;
            return;
         }
         if (elemInBase == null)
         {
            // Не найден елемент с такими параметрами - изменился
            elem.BaseStatus |= EnumBaseStatus.Changed;
            errElem += "Параметры модуля изменились. ";
         }
      }

      /// <summary>
      /// Проверка блоков элементов - соответствие параметров с базой
      /// </summary>      
      private static void checkElements(List<Element> allElements)
      {
         using (var entities = BaseApartments.ConnectEntities())
         {
            // Категории и параметры элементов
            var cps = entities.F_S_Categories.Select(s =>
                        new
                        {
                           category = s.NAME_RUS_CATEGORY,
                           parameters = s.F_nn_Category_Parameters.Select(t => t.F_S_Parameters.NAME_PARAMETER)
                        });

            foreach (var elem in allElements)
            {
               string errElem = string.Empty;

               var catParamsEnt = cps.SingleOrDefault(c => c.category.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase));
               if (catParamsEnt == null)
               {
                  // Нет такой категории в базе                  
                  errElem += $"Нет категории в базе - {elem.CategoryElement}. ";
               }
               else
               {
                  // Проверка имен всех параметров
                  foreach (var paramEnt in catParamsEnt.parameters)
                  {
                     Parameter paramElem = null;
                     try
                     {
                        paramElem = elem.Parameters.SingleOrDefault(p => p.Name.Equals(paramEnt, StringComparison.OrdinalIgnoreCase));
                     }
                     catch
                     {
                        // Дублирование параметров
                        errElem += $"Дублирование параметра {paramEnt}. ";
                     }
                     if (paramElem == null)
                     {
                        // Нет такого параметра
                        errElem += $"Нет параметра {paramEnt}. ";
                     }
                  }
               }

               if (!string.IsNullOrEmpty(errElem))
               {
                  elem.BaseStatus |= EnumBaseStatus.Error;
                  elem.Error = new Error(errElem, elem.ExtentsInModel, elem.IdBlRefElement, System.Drawing.SystemIcons.Error);
               }
            }
         }
      }
   }
}
