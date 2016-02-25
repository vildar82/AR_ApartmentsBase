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
//using MoreLinq;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
   /// <summary>
   /// проверка квартир
   /// </summary>
   public class CheckApartments
   {
      /// <summary>
      /// Проверка квартир в чертеже и в базе
      /// </summary>      
      public static void Check(List<Apartment> apartments, List<Apartment> apartmentsInBase)
      {
         if (apartments == null || apartments.Count ==0)
         {
            return;
         }

         if (apartmentsInBase == null || apartmentsInBase.Count == 0)
         {
            // В базе пусто. все квариры новые
            apartments.ForEach(a =>            
            {
               a.BaseStatus = EnumBaseStatus.New;
               a.Modules.ForEach(m => m.BaseStatus = EnumBaseStatus.New);
               });
            return;
         }

         // проверка квартир и их состава
         foreach (var apart in apartments)
         {
            string errApart = string.Empty;

            // Проверка квартиры
            checkApart(apart, apartmentsInBase, ref errApart);

            if (!string.IsNullOrEmpty(errApart))
            {
               apart.Error = new Error(errApart, apart.ExtentsInModel, apart.IdBlRef, System.Drawing.SystemIcons.Error);               
            }
            if (apart.BaseStatus == EnumBaseStatus.None)
            {
               apart.BaseStatus = EnumBaseStatus.OK;
            }
         }        

         // Все уникальные модули
         var modulesAll = apartments.SelectMany(a => a.Modules).GroupBy(m=>m.Name).Select(g=>g.First());

         //// Проверка всех элементов - имен параметров и т.п.
         // Все элементы уже проверены на этапа считывания с чертежа
         //var allElements = modulesAll.SelectMany(m => m.Elements).ToList();
         //checkElements(allElements);

         // Проверка состава модулей
         foreach (var module in modulesAll)
         {
            // Соответствующий модуль в модулях из базы
            var moduleInBase = apartmentsInBase.SelectMany(a => a.Modules).FirstOrDefault(m=>m.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase));
            
            if (moduleInBase == null)
            {
               // Нет такого модуля в базе - новый
               module.BaseStatus |= EnumBaseStatus.New;
            }
            else
            {
               string errModule = string.Empty;

               // не совпадает количество элементов в блоке модуля и в модуле из базы
               if (module.Elements.Count != moduleInBase.Elements.Count)
               {
                  // Не совпадают количества элементов в модуле в базе и в двг файле
                  errModule = $"Изменилось количество элементов в модуле, было (по базе) '{moduleInBase.Elements.Count}' стало (по блоку) '{module.Elements.Count}'. ";                                                 
                  module.BaseStatus |= EnumBaseStatus.Changed;
               }

               // Проверка каждого элемента
               foreach (var elem in module.Elements)
               {
                  string errElem = string.Empty;
                  checkElement(elem, moduleInBase, ref errElem);
               }

               // Если хоть один элемент с ошибкой - то весь модуль ошибочный
               if (module.Elements.Any(e => e.BaseStatus.HasFlag(EnumBaseStatus.Error)))
               {
                  errModule += "Есть елементы с ошибками. ";
                  module.BaseStatus = EnumBaseStatus.Error;
               }
               // Если хоть один элемент новый - то весь модуль изменился
               else if (module.Elements.Any(e =>
                    e.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
                    e.BaseStatus.HasFlag(EnumBaseStatus.New)))
               {
                  errModule += "Есть изменившиеся елементы. ";
                  module.BaseStatus = EnumBaseStatus.Changed;
               }

               // добавление строки сообщения в модуль если есть
               if (!string.IsNullOrEmpty(errModule))
               {
                  if (module.Error == null)
                  {
                     module.Error = new Error(errModule, module.ExtentsInModel, module.IdBlRefModule, System.Drawing.SystemIcons.Error);
                  }
                  else
                  {
                     module.Error.AdditionToMessage(errModule);
                  }
               }
            }
         }

         // Проверка квартир и проверка изменений.         
         // квартиры которых нет в чертеже
         var apartmentsMissingInDwg = apartmentsInBase.Where(
            aBase => !apartments.Any(a => a.Name.Equals(aBase.Name, StringComparison.OrdinalIgnoreCase))).ToList();
         apartmentsMissingInDwg.ForEach(a =>
         {
            a.BaseStatus = EnumBaseStatus.NotInDwg;
            apartments.Add(a);
         });
      }      

      private static void checkApart(Apartment apart, List<Apartment> apartmentsInBase ,ref string errApart)
      {
         // Поиск квартиры в базе
         Apartment apartInBase = null;
         try
         {
            apartInBase = apartmentsInBase.SingleOrDefault(a => a.Name.Equals(apart.Name, StringComparison.OrdinalIgnoreCase));
         }
         catch
         {
            // Ошибка в базе - несколько квартир с одним именем
            apart.BaseStatus = EnumBaseStatus.Error;
            errApart += "В базе несколько квартир с таким именем - нужно устранить ошибку в базе. ";
            return;
         }

         if (apartInBase == null)
         {
            // Квартиры нет в базе
            apart.BaseStatus = EnumBaseStatus.New;
            errApart += "Квартиры нет в базе. ";
         }
         else
         {
            //Сверка модулей
            if (apart.Modules.Count == 0)
            {
               errApart += "В блоке квартиры нет модулей. ";
               apart.BaseStatus = EnumBaseStatus.Error;
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
                  if (module.BaseStatus == EnumBaseStatus.None)
                  {
                     module.BaseStatus = EnumBaseStatus.OK;
                  }                      
               }
               // Если хоть в одном модуле есть ошибки - вся квартира считается ошибочной
               if (apart.Modules.Any(m=>m.BaseStatus.HasFlag(EnumBaseStatus.Error)))
               {
                  errApart += "Есть модули с ошибками. ";
                  apart.BaseStatus = EnumBaseStatus.Error;
               }
               // Если хоть один модуль поменял положение или новый, то изменение всей квартиры
               if (apart.Modules.Any(m => m.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
                                          m.BaseStatus.HasFlag(EnumBaseStatus.New)))
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
         var modulesInBase = apartInBase.Modules.Where(m => m.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase));

         // Нет такого модуля в базе
         if (modulesInBase.Count() == 0)
         {
            module.BaseStatus |= EnumBaseStatus.New;
            errModule += "Модуля с таким именем нет в базе. ";
         }
         else
         {
            // Проверка параеметров модудля  
            Module moduleInBase;
            try
            {
               moduleInBase = modulesInBase.SingleOrDefault(m =>                              
                              m.Direction.Equals(module.Direction) &&
                              m.LocationPoint.Equals(module.LocationPoint));
            }
            catch
            {
               // Несколько одинаковых модулей
               errModule += "Несколько одинаковых модулей в базе. ";
               module.BaseStatus = EnumBaseStatus.Error;
               return;
            }
            if (moduleInBase == null)
            {
               // Не найден модуль с такими параметрами - изменился
               module.BaseStatus |= EnumBaseStatus.New;
               errModule += "Параметры модуля в квартире изменились или это новый модуль в квартире. ";
            }
            // Модуль с такими параметрами найден в базе квартир
            else
            {
               // Ошибка если в блоке модуля нет элементов
               if (module.Elements.Count == 0)
               {
                  errModule += "В блоке модуля нет элементов. ";
                  module.BaseStatus = EnumBaseStatus.Error;
               }
               //else
               //{
               //   if (module.Elements.Count != moduleInBase.Elements.Count)
               //   {
               //      // Не совпадают количества элементов в модуле в базе и в двг файле
               //      errModule += $"Изменилось количество элементов в модуле, было (по базе) '{moduleInBase.Elements.Count}' стало (по блоку) '{module.Elements.Count}'. ";
               //      module.BaseStatus = EnumBaseStatus.Changed;
               //   }

               //   foreach (var elem in module.Elements)
               //   {
               //      string errElem = string.Empty;

               //      // Проверка элемента
               //      checkElement(elem, moduleInBase, ref errElem);

               //      if (!string.IsNullOrEmpty(errElem))
               //      {
               //         elem.Error = new Error(errElem, elem.ExtentsInModel, elem.IdBlRefElement, System.Drawing.SystemIcons.Error);
               //      }
               //      if (elem.BaseStatus == EnumBaseStatus.None)
               //      {
               //         elem.BaseStatus = EnumBaseStatus.OK;
               //      }
               //   }

               //   if (module.Elements.Any(e => e.BaseStatus.HasFlag(EnumBaseStatus.Error)))
               //   {
               //      errModule += "Есть елементы с ошибками. ";
               //      module.BaseStatus |= EnumBaseStatus.Error;
               //   }
               //   else if (module.Elements.Any(e =>
               //        e.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
               //        e.BaseStatus.HasFlag(EnumBaseStatus.New)))
               //   {
               //      errModule += "Есть изменившиеся елементы. ";
               //      module.BaseStatus |= EnumBaseStatus.Changed;
               //   }
               //}
            }
         }
      }

      private static void checkElement(Element elem, Module moduleInBase, ref string errElem)
      {
         // Проверка элемента в модуле
         Element elemInBase = null;
         try
         {
            elemInBase = moduleInBase.Elements.SingleOrDefault(e => e.Equals(elem));
         }
         catch
         {
            // Несколько одинаковых элементов            
            errElem += "Несколько одинаковых элементов в базе. ";
            elem.BaseStatus = EnumBaseStatus.Error;
            return;
         }
         if (elemInBase == null)
         {
            // Не найден елемент с такими параметрами - изменился
            elem.BaseStatus |= EnumBaseStatus.New;
            errElem += "Параметры модуля изменились или это новый элемент. ";
         }
      }

      ///// <summary>
      ///// Проверка блоков элементов - соответствие параметров с базой
      ///// </summary>      
      //private static void checkElements(List<Element> allElements)
      //{
      //   using (var entities = BaseApartments.ConnectEntities())
      //   {
      //      // Категории и параметры элементов
      //      var cps = entities.F_S_Categories.Select(s =>
      //                  new
      //                  {
      //                     category = s.NAME_RUS_CATEGORY,
      //                     parameters = s.F_nn_Category_Parameters.Select(t => t.F_S_Parameters.NAME_PARAMETER)
      //                  });

      //      foreach (var elem in allElements)
      //      {
      //         string errElem = string.Empty;

      //         var catParamsEnt = cps.SingleOrDefault(c => c.category.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase));
      //         if (catParamsEnt == null)
      //         {
      //            // Нет такой категории в базе                  
      //            errElem += $"Нет категории в базе - {elem.CategoryElement}. ";
      //         }
      //         else
      //         {
      //            // Проверка имен всех параметров
      //            foreach (var paramEnt in catParamsEnt.parameters)
      //            {
      //               Parameter paramElem = null;
      //               try
      //               {
      //                  paramElem = elem.Parameters.SingleOrDefault(p => p.Name.Equals(paramEnt, StringComparison.OrdinalIgnoreCase));
      //               }
      //               catch
      //               {
      //                  // Дублирование параметров
      //                  errElem += $"Дублирование параметра {paramEnt}. ";
      //               }
      //               if (paramElem == null)
      //               {
      //                  // Нет такого параметра
      //                  errElem += $"Нет параметра {paramEnt}. ";
      //               }
      //            }
      //         }

      //         if (!string.IsNullOrEmpty(errElem))
      //         {
      //            elem.BaseStatus |= EnumBaseStatus.Error;
      //            elem.Error = new Error(errElem, elem.ExtentsInModel, elem.IdBlRefElement, System.Drawing.SystemIcons.Error);
      //         }
      //      }
      //   }
      //}      
   }
}
