using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using AR_ApartmentBase.Model.Elements;
using AR_ApartmentBase.Properties;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
    public static class BaseApartments
    {
        private static List<KeyValuePair<string, List<F_S_Parameters>>> baseCategoryParameters;
        private static List<KeyValuePair<int, List<F_S_Parameters>>> baseCategoryParametersById;
        private static List<int> categoriesIdWithIdWallParameter;
        private static int idWallParam;
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
        /// Список параметров по категориям - имя категории - список параметров
        /// </summary>        
        public static List<KeyValuePair<string, List<F_S_Parameters>>> GetBaseCategoryParameters ()
        {
            if (baseCategoryParameters == null)
            {
                using (var entities = ConnectEntities())
                {
                    entities.F_nn_Category_Parameters.Load();
                    baseCategoryParameters = entities.F_nn_Category_Parameters.Local.GroupBy(cp => cp.F_S_Categories).Select(p =>
                                  new KeyValuePair<string, List<F_S_Parameters>>(p.Key.NAME_RUS_CATEGORY, p.Select(i => i.F_S_Parameters).ToList())).ToList();
                }
            }
            return baseCategoryParameters;
        }

        /// <summary>
        /// Список параметров по категориям - id категории - список параметров
        /// </summary>        
        public static List<KeyValuePair<int, List<F_S_Parameters>>> GetBaseCategoryParametersById ()
        {
            if (baseCategoryParametersById == null)
            {
                using (var entities = ConnectEntities())
                {
                    entities.F_nn_Category_Parameters.Load();
                    baseCategoryParametersById = entities.F_nn_Category_Parameters.Local.GroupBy(cp => cp.F_S_Categories).Select(p =>
                        new KeyValuePair<int, List<F_S_Parameters>>(p.Key.ID_CATEGORY, p.Select(i => i.F_S_Parameters).ToList())).ToList();
                }
            }
            return baseCategoryParametersById;
        }

        /// <summary>
        /// Экспорт квартир в базу.
        /// </summary>      
        public static void Export(List<Apartment> apartments)
        {
            //TestInfoApartmentstoDb(apartments);            
            if (apartments == null || apartments.Count == 0)
            {
                return;
            }          

            using (entities = ConnectEntities())
            {
                //entities.Configuration.AutoDetectChangesEnabled = false;
                //entities.Configuration.ValidateOnSaveEnabled = false;                

                // Запись квартир (с удалением старых модулей)
                FillApartments(apartments);

                // Перезапись модулей во всех переданных квартирах - удаление старых модулей (вместе с элементами)
                FillModules(apartments);                
            }  
        }

        /// <summary>
        /// Получение значения для параметра IdWall - спсок id стен через ;
        /// </summary>
        /// <param name="idsWall">Id стен</param>
        /// <returns>Строка для заполнения параметра IdWall в элементе хосте стены (дверь, окно)</returns>
        public static string GetHostWallsValue (List<string> idsWall)
        {
            idsWall.Sort();
            return string.Join(";", idsWall);
        }

        /// <summary>
        /// Запись новых квартир, и очистка модулей у существующих - из переданного списка квартир
        /// </summary>        
        private static void FillApartments (List<Apartment> apartments)
        {               
            foreach (var apart in apartments)
            {
                var apartDB = entities.F_R_Flats.FirstOrDefault(f => f.WORKNAME.Equals(apart.Name, StringComparison.OrdinalIgnoreCase));
                if (apartDB == null)
                {
                    // Новая квартира
                    apartDB = entities.F_R_Flats.Add(new F_R_Flats() {
                        WORKNAME = apart.Name,
                        COMMERCIAL_NAME = "",
                        TYPE_FLAT = apart.TypeFlat,
                        REVISION = 1
                    });                   
                }
                else
                {
                    if (apartDB.TYPE_FLAT == null || !apartDB.TYPE_FLAT.Equals(apart.TypeFlat, StringComparison.OrdinalIgnoreCase))
                        apartDB.TYPE_FLAT = apart.TypeFlat;
                    // Удаление модулей (Удалятся ли Модули и Элементы?)
                    foreach (var item in apartDB.F_nn_FlatModules.ToList())
                    {
                        // Удаление элементов стен в модуле
                        RemoveHostWallElements(item.F_R_Modules);
                        // Удаление модуля
                        entities.F_R_Modules.Remove(item.F_R_Modules);
                    }                    
                }
                apart.DBObject = apartDB;
            }
            entities.SaveChanges();
        }

        private static void RemoveHostWallElements (F_R_Modules module)
        {
            // Категории элементов с парвметром IdWall (Options.HostWallParameter)
            if (categoriesIdWithIdWallParameter == null)
            {
                categoriesIdWithIdWallParameter = GetBaseCategoryParametersById().Where(c => c.Value.Any(p =>
                            p.NAME_PARAMETER.Equals(Options.HostWallParameter, StringComparison.OrdinalIgnoreCase)))
                            .Select(s => s.Key).ToList();
            }
            // елеметы в модуле с параметром idWall
            var elemsWithIdWallParam = module.F_nn_Elements_Modules.Select(s=>s.F_S_Elements).
                    Where(e => categoriesIdWithIdWallParameter.Any(c=>c == e?.ID_CATEGORY)).ToList();
            // Удаление найденных элементов
            foreach (var elem in elemsWithIdWallParam)
            {
                entities.F_S_Elements.Remove(elem);
            }
        }

        private static void FillModules (List<Apartment> apartments)
        {
            // Все модули в квартирах уникальные и новые (старые уже бвли удалены)
            // Запись всех элементов всех модуей всех квартир
            
            FillAllElements(apartments);            

            // Запись модулей
            foreach (var apart in apartments)
            {
                var apartBD = (F_R_Flats)apart.DBObject;
                foreach (var module in apart.Modules)
                {
                    var moduleBD = entities.F_R_Modules.Add(new F_R_Modules() { NAME_MODULE = module.Name, REVISION = 1 });
                    module.DBObject = moduleBD;
                    // Прицепка модуля к квартире
                    entities.F_nn_FlatModules.Add(new F_nn_FlatModules {
                        F_R_Flats = apartBD,
                        F_R_Modules = moduleBD,
                        ANGLE = module.Rotation,
                        DIRECTION = module.Direction,
                        LOCATION = module.LocationPoint
                    });
                    // Прицепка элементов к модулю
                    AddElementsToModule(module);
                }
            }
            entities.SaveChanges();

            // Поиск и заполнение параметров IdWall элементов хостов стены
            FindAndFillIdWallParametersInApartments(apartments);
            entities.SaveChanges();
        }

        /// <summary>
        /// Поиск элементо-модулей стен для элементов хостов стены и запись параметрва idWall
        /// </summary>
        /// <param name="apartments"></param>
        private static void FindAndFillIdWallParametersInApartments (List<Apartment> apartments)
        {
            foreach (var apart in apartments)
            {
                foreach (var module in apart.Modules)
                {                    
                    FillIdWallParameterInModule(module);
                }
            }
        }

        private static void FillIdWallParameterInModule (Module module)
        {
            var moduleDb = (F_R_Modules)module.DBObject;
            var hostWallElems = module.Elements.OfType<IWallHost>();
            foreach (var hw in hostWallElems)
            {
                List<string> idsWall = new List<string>();
                foreach (var wall in hw.HostWall)
                {
                    // Найти элементо-модуль стены
                    var wallDbEM = ((F_S_Elements)wall.DBObject).F_nn_Elements_Modules.FirstOrDefault();
                    if (wallDbEM == null)
                    {                        
                        throw new Exception("Ошибка - не должно такого быть - не найден элементо-модуль стены в базе!!!");
                    }
                    idsWall.Add(wallDbEM.ID_ELEMENT_IN_MODULE.ToString());
                }
                var idWallParam = GetHostWallsValue(idsWall);
                // Запись параметра IdWall в базу для элемента хоста стены
                FillIdWallParameterElemInDb(hw, idWallParam);
            }
        }

        private static void FillIdWallParameterElemInDb (IWallHost hw, string idWallParamValue)
        {
            if (idWallParam ==0)
            {
                idWallParam = entities.F_S_Parameters.First(p => 
                    p.NAME_PARAMETER.Equals(Options.HostWallParameter, StringComparison.OrdinalIgnoreCase)).ID_PARAMETER;
            }
            var elemDb = (F_S_Elements)hw.DBObject;
            var param = elemDb.F_nn_ElementParam_Value.First(p => p.F_nn_Category_Parameters.F_S_Parameters.ID_PARAMETER == idWallParam);
            param.PARAMETER_VALUE = idWallParamValue;
        }

        private static void AddElementsToModule (Module module)
        {
            foreach (var item in module.Elements)
            {
                entities.F_nn_Elements_Modules.Add(new F_nn_Elements_Modules {
                    F_R_Modules = (F_R_Modules)module.DBObject,
                    F_S_Elements = (F_S_Elements)item.DBObject,
                    DIRECTION = item.Direction,
                    LOCATION = item.LocationPoint
                });
            }
        }

        /// <summary>
        /// Запись елементов чертежа в базу.
        /// </summary>
        /// <param name="allElements">Элементы чертежа</param>
        private static void FillAllElements (List<Apartment> apartments)
        {
            var allElements = apartments.SelectMany(a => a.Modules.SelectMany(m => m.Elements)).ToList();
            //var uniqElemGroups = allElements.GroupBy(e => e).ToList();            

            var elementsBD = entities.F_S_Elements.ToList().Select(s => new Element(s)).Cast<IElement>().ToList();

            entities.F_S_FamilyInfos.Load();
            entities.F_S_Categories.Load();
            entities.F_nn_Category_Parameters.Load();

            // Записать все элементы
            AddElements(allElements, elementsBD);            
        }

        private static void AddElements (List<IElement> elements, List<IElement> elementsBD)
        {
            if (elements == null || elements.Count == 0) return;

            // Найти новые элементов
            var newElements = new List<KeyValuePair<IElement,List<IElement>>>();
            var uniqElems = elements.GroupBy(g => g).ToList();
            foreach (var group in uniqElems)
            {
                if (group.Key is IWallHost)
                {
                    // Элементы принадлежащие стенам - записываются все как новые
                    foreach (var item in group)
                        newElements.Add(new KeyValuePair<IElement, List<IElement>>(item, new List<IElement> { item }));
                }
                else
                {
                    var elemBD = elementsBD.Find(e => e.Equals(group.Key));
                    if (elemBD == null)
                    {
                        newElements.Add(new KeyValuePair<IElement, List<IElement>>(group.Key, group.ToList()));
                    }
                    else
                    {
                        foreach (var item in group)
                        {
                            item.DBObject = elemBD.DBObject;
                        }
                    }
                }
            }
            
            // Запись новых элементов
            if (newElements.Any())
            {
                // Запись новых семейств если есть
                FillFamilys(newElements.Select(s=>s.Key).ToList());
                foreach (var group in newElements)
                {
                    // Запись нового элемента
                    FillNewElement(group.Key);
                    foreach (var item in group.Value)
                    {
                        item.DBObject = group.Key.DBObject;
                    }
                }
                entities.SaveChanges();
            }
        }

        private static void FillNewElement (IElement elem)
        {
            F_S_Elements elemBD = new F_S_Elements();
            elemBD.F_S_FamilyInfos = entities.F_S_FamilyInfos.Local.First(f =>
                                     f.FAMILY_NAME.Equals(elem.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                                     f.FAMILY_SYMBOL.Equals(elem.FamilySymbolName, StringComparison.OrdinalIgnoreCase));
            elemBD.F_S_Categories = entities.F_S_Categories.Local.
                        First(c => c.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase));



            var paramsBD = new List<F_nn_ElementParam_Value>();

            var categoryParametersBD = entities.F_nn_Category_Parameters.Local
                      .Where(c => c.F_S_Categories.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase))
                      .Select(p => p).ToList();

            foreach (var paramBD in categoryParametersBD)
            {
                var elemParam = elem.Parameters.Single(p => p.Name.Equals(paramBD.F_S_Parameters.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));

                var elemParamValueBD = entities.F_nn_ElementParam_Value.Add(
                    new F_nn_ElementParam_Value{
                        F_nn_Category_Parameters = paramBD,
                        F_S_Elements = elemBD,
                        PARAMETER_VALUE = elemParam.Value
                    });
                paramsBD.Add(elemParamValueBD);
            }
            elemBD.F_nn_ElementParam_Value = paramsBD;

            entities.F_S_Elements.Add(elemBD);

            elem.DBObject = elemBD;
        }

        private static void FillFamilys (List<IElement> elements)
        {            
            if (elements == null || elements.Count == 0) return;

            var familys = elements.GroupBy(g => g.FamilyName + "|" + g.FamilySymbolName).Select(s => s.Key);
            var familysBD = entities.F_S_FamilyInfos.Select(s => s.FAMILY_NAME + "|" + s.FAMILY_SYMBOL);
            var newFamilys = familys.Except(familysBD, StringComparer.OrdinalIgnoreCase);
            if (newFamilys.Any())
            {
                foreach (var item in newFamilys)
                {
                    var values = item.Split('|');
                    entities.F_S_FamilyInfos.Add(new F_S_FamilyInfos { FAMILY_NAME = values[0], FAMILY_SYMBOL = values[1] });
                }
                entities.SaveChanges();
            }
        }
    }
}