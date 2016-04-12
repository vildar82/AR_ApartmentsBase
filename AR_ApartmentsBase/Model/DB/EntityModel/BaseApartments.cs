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
using Autodesk.AutoCAD.Runtime;
using MoreLinq;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
    public static class BaseApartments
    {
        private static List<Tuple<F_nn_Elements_Modules, List<F_nn_Elements_Modules>>> doorsAndHostWall;

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
                entities.F_R_Flats.RemoveRange(entities.F_R_Flats);
                entities.F_R_Modules.RemoveRange(entities.F_R_Modules);
                entities.F_S_Elements.RemoveRange(entities.F_S_Elements);
                entities.F_S_FamilyInfos.RemoveRange(entities.F_S_FamilyInfos);

                entities.SaveChanges();
            }
        }

        /// <summary>
        /// Экспорт квартир в базу.
        /// </summary>      
        public static List<ExportDBInfo> Export(List<Apartment> apartments)
        {
            //TestInfoApartmentstoDb(apartments);

            List<ExportDBInfo> exportInfos = new List<ExportDBInfo>();
            if (apartments.Count == 0)
            {
                return exportInfos;
            }

            using (entities = ConnectEntities())
            {
                //entities.Configuration.AutoDetectChangesEnabled = false;
                //entities.Configuration.ValidateOnSaveEnabled = false;

                // Загрузка таблиц
                entities.F_R_Flats.Load();
                entities.F_R_Modules.Load();
                entities.F_nn_FlatModules.Load();
                entities.F_nn_ElementParam_Value.Load();
                entities.F_nn_Elements_Modules.Load();
                entities.F_S_Elements.Load();
                entities.F_S_FamilyInfos.Load();
                entities.F_S_Categories.Load();
                entities.F_nn_Category_Parameters.Load();

                try
                {
                    // Элементы дверей и их стены - для обновления параметрв idWall
                    doorsAndHostWall = new List<Tuple<F_nn_Elements_Modules, List<F_nn_Elements_Modules>>>();

                    // Модули - новые или с изменениями
                    var modules = apartments.SelectMany(a => a.Modules)
                                            .Where(m => !m.BaseStatus.HasFlag(EnumBaseStatus.Error) &&
                                                     (
                                                        m.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
                                                        m.BaseStatus.HasFlag(EnumBaseStatus.New)
                                                     )
                                                  )
                                            .GroupBy(g => g.Name).Select(g => g.First());

                    using (var progress = new ProgressMeter())
                    {
                        progress.SetLimit(modules.Count());
                        progress.Start("Запись модулей в базу...");

                        foreach (var module in modules)
                        {
                            progress.MeterProgress();
                            // поиск модуля
                            var moduleEnt = defineModuleEnt(module);
                        }
                        progress.Stop();
                    }

                    using (var progress = new ProgressMeter())
                    {
                        progress.SetLimit(modules.Count());
                        progress.Start("Запись квартир в базу...");

                        // Определение квартир и привязка модулей
                        foreach (Apartment apart in apartments)
                        {
                            progress.MeterProgress();
                            // Определение квартиры
                            var flatEnt = defineFlatEnt(apart);
                        }
                        progress.Stop();
                    }

                    // Сохранение изменений
                    entities.SaveChanges();

                    // обновление параметра стен для дверей
                    if (doorsAndHostWall.Count > 0)
                    {
                        foreach (var doorAndHostWall in doorsAndHostWall)
                        {
                            F_nn_Elements_Modules doorEmEnt = doorAndHostWall.Item1;
                            string hostWallsValue = string.Join(";", doorAndHostWall.Item2.Select(w => w.ID_ELEMENT_IN_MODULE.ToString()));                            
                            // Запись id стены в параметр
                            // Поиск параметра IdWall для заполнения
                            var paramHostWall = doorEmEnt.F_S_Elements.F_nn_ElementParam_Value.Single(p =>
                                  p.F_nn_Category_Parameters.F_S_Parameters.NAME_PARAMETER
                                     .Equals(Options.Instance.DoorHostWallParameter, StringComparison.OrdinalIgnoreCase));
                            paramHostWall.PARAMETER_VALUE = hostWallsValue;// wallEmEnt.ID_ELEMENT_IN_MODULE.ToString();
                        }
                    }
                    entities.SaveChanges();
                }
                catch (System.Exception ex)
                {
                    Inspector.AddError(ex.Message, icon: System.Drawing.SystemIcons.Error);
                }
            }
            return exportInfos;
        }

        private static void TestInfoApartmentstoDb(List<Apartment> apartments)
        {
            StringBuilder resVal = new StringBuilder("Квартиры для записи в базу:\n");
            foreach (var apart in apartments)
            {
                resVal.AppendLine("- - - - - - - -");
                resVal.AppendLine($"Квартира - {apart.Name}, {apart.BaseStatus}");
                foreach (var module in apart.Modules)
                {
                    resVal.AppendLine($"Модуль - {module.Name}, {module.BaseStatus}");
                    foreach (var elem in module.Elements)
                    {
                        resVal.AppendLine($"Элемент - {elem.Name}, {elem.BaseStatus}");
                    }
                }
            }
            Logger.Log.Error(resVal.ToString());
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
                flatEnt = entities.F_R_Flats.Add(new F_R_Flats() { WORKNAME = apart.Name, COMMERCIAL_NAME = "",
                    REVISION = revision, TYPE_FLAT = apart.TypeFlat });
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

        /// <summary>
        /// Поиск модуля, если он изменился, то создание ревизии (с обновлением квартир), если его нет, то создание
        /// </summary>      
        private static F_R_Modules defineModuleEnt(Module module)
        {
            F_R_Modules moduleEnt = null;
            int revision = 0;
            if (module.BaseStatus.HasFlag(EnumBaseStatus.Changed))
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
                //Logger.Log.Error($"\nНовая ревизия модуля {module.Name} - {revision}.");
                moduleEnt = entities.F_R_Modules.Add(new F_R_Modules() { NAME_MODULE = module.Name, REVISION = revision });
                // Добавление элементов в модуль
                addElementsToModule(module, moduleEnt);
                // Если это новая ревизия модуля, то обновление модуля во всех квартиро-модулей
                if (revision != 0)
                {
                    // Предыдущая ревизия модуля
                    var modulePrevRev = entities.F_R_Modules.Local.Single(m =>
                          m.NAME_MODULE.Equals(moduleEnt.NAME_MODULE, StringComparison.OrdinalIgnoreCase) &&
                          m.REVISION == revision - 1);
                    // квартиро-модули в которых есть пред ревизия модуля
                    var fmsPrevRevM = entities.F_nn_FlatModules.Local.Where(fm => fm.ID_MODULE == modulePrevRev.ID_MODULE).ToList();
                    foreach (var fmPrevRevM in fmsPrevRevM)
                    {
                        entities.F_nn_FlatModules.Add(new F_nn_FlatModules()
                        {
                            F_R_Flats = fmPrevRevM.F_R_Flats,
                            F_R_Modules = moduleEnt,
                            LOCATION = fmPrevRevM.LOCATION,
                            DIRECTION = fmPrevRevM.DIRECTION,
                            ANGLE = fmPrevRevM.ANGLE
                        });
                    }
                }
            }
            return moduleEnt;
        }

        private static void addElementsToModule(Module module, F_R_Modules moduleEnt)
        {
            if (moduleEnt.F_nn_Elements_Modules.Count != 0)
            {
                // Непредвиденная ситуация. Не должно быть элементов в модуле
                //Logger.Log.Error("addElementsToModule() Непредвиденная ситуация. Не должно быть элементов в модуле");
                entities.F_nn_Elements_Modules.RemoveRange(moduleEnt.F_nn_Elements_Modules);
            }
            // Элементы                                  
            foreach (var elem in module.Elements)
            {
                // Определение элемента в базе                                          
                var elemEnt = getElement(elem);
                // Добавление элемента в модуль
                var elemInModEnt = addElemToModule(elemEnt, moduleEnt, elem);
            }
            // Для дверей - найти стену в базе и сохранить в список doorsAndHostWall для записи реального idWall после первого обновления базы.
            var doors = module.Elements.OfType<DoorElement>();
            foreach (var door in doors)
            {
                F_nn_Elements_Modules doorEmEnt = (F_nn_Elements_Modules)door.DBObject;
                List<F_nn_Elements_Modules> wallsHost = new List<F_nn_Elements_Modules>();
                foreach (var item in door.HostWall)
                {
                    F_nn_Elements_Modules wallEmEnt = (F_nn_Elements_Modules)item.DBObject;
                    wallsHost.Add(wallEmEnt);
                }
                doorsAndHostWall.Add(new Tuple<F_nn_Elements_Modules, List<F_nn_Elements_Modules>>(item1: doorEmEnt, item2: wallsHost));
            }
        }

        /// <summary>
        /// Поиск модуля, не создается новая запись ни при каких условиях
        /// </summary>      
        private static F_R_Modules getModuleEnt(Module module)
        {
            return entities.F_R_Modules.Local
               .Where(m => m.NAME_MODULE.Equals(module.Name, StringComparison.OrdinalIgnoreCase))
               .OrderByDescending(r => r.REVISION).FirstOrDefault();
        }

        private static F_nn_FlatModules getFMEnt(F_R_Flats flatEnt, F_R_Modules moduleEnt, Module module)
        {
            var fmEnt = flatEnt.F_nn_FlatModules.SingleOrDefault
                                 (fm =>
                                       fm.F_R_Flats == flatEnt &&
                                       fm.F_R_Modules == moduleEnt &&
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
            F_S_Elements elemEnt = findElementEnt(elem);

            // Если такого элемента с параметрами нет, то создание
            if (elemEnt == null)
            {
                // поиск семейство элемента
                var famInfoEnt = entities.F_S_FamilyInfos.Local.SingleOrDefault(f =>
                         f.FAMILY_NAME.Equals(elem.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                         f.FAMILY_SYMBOL.Equals(elem.FamilySymbolName, StringComparison.OrdinalIgnoreCase));
                // если нет семейства, то создание
                if (famInfoEnt == null)
                {
                    famInfoEnt = entities.F_S_FamilyInfos.Add(new F_S_FamilyInfos()
                    {
                        FAMILY_NAME = elem.FamilyName,
                        FAMILY_SYMBOL = elem.FamilySymbolName
                    });
                }

                // Создание элемента
                elemEnt = entities.F_S_Elements.Add(new F_S_Elements()
                {
                    // Категория элемента
                    F_S_Categories = entities.F_S_Categories.Local.SingleOrDefault(c =>
                          c.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase)),
                    // Семейство
                    F_S_FamilyInfos = famInfoEnt
                });

                // Заполнение параметров элемента
                var paramsElemEnt = entities.F_nn_Category_Parameters.Local
                      .Where(c => c.F_S_Categories.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase))
                      .Select(p => p).ToList();
                foreach (var paramElemEnt in paramsElemEnt)
                {
                    var elemParam = elem.Parameters.Single(p => p.Name.Equals(paramElemEnt.F_S_Parameters.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));
                    var elemParaValueEnt = entities.F_nn_ElementParam_Value.Add(new F_nn_ElementParam_Value()
                    {
                        F_nn_Category_Parameters = paramElemEnt,
                        F_S_Elements = elemEnt,
                        PARAMETER_VALUE = elemParam.Value
                    });
                }
            }
            return elemEnt;
        }

        private static F_S_Elements findElementEnt(Element elem)
        {
            // Поиск элемента с такими параметрами
            return entities.F_S_Elements.Local
               // Категория элемента
               .Where(e => e.F_S_Categories.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase))
               // Семейство элемента
               .Where(e => e.F_S_FamilyInfos.FAMILY_NAME.Equals(elem.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                         e.F_S_FamilyInfos.FAMILY_SYMBOL.Equals(elem.FamilySymbolName, StringComparison.OrdinalIgnoreCase))
               // Параметры элемента
               .Where(e => e.F_nn_ElementParam_Value.All(p =>
                     elem.Parameters.Any(ep =>
                        p.F_nn_Category_Parameters.F_S_Categories.NAME_RUS_CATEGORY.Equals(ep.Name, StringComparison.OrdinalIgnoreCase) &&
                        p.PARAMETER_VALUE.Equals(ep.Value, StringComparison.OrdinalIgnoreCase)))).SingleOrDefault();
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
            elem.DBObject = emEnt;
            return emEnt;
        }
    }
}