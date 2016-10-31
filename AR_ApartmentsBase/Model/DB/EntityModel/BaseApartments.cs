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
        private static List<string> baseParametersElemInFlat;
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
            using (var connectEntities = ConnectEntities())
            {
                connectEntities.F_R_Flats.RemoveRange(connectEntities.F_R_Flats);                
                connectEntities.F_S_Elements.RemoveRange(connectEntities.F_S_Elements);
                connectEntities.F_nn_ElementInFlatValue.RemoveRange(connectEntities.F_nn_ElementInFlatValue);
                connectEntities.F_S_FamilyInfos.RemoveRange(connectEntities.F_S_FamilyInfos);
                connectEntities.SaveChanges();
            }
        }

        /// <summary>
        /// Список параметров по категориям - имя категории - список параметров
        /// </summary>        
        public static List<KeyValuePair<string, List<F_S_Parameters>>> GetBaseCategoryParameters ()
        {
            if (baseCategoryParameters == null)
            {
                using (var connectEntities = ConnectEntities())
                {
                    baseCategoryParameters = connectEntities.F_nn_Category_Parameters.ToList().GroupBy(cp => cp.F_S_Categories).Select(p =>
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
                using (var connectEntities = ConnectEntities())
                {                    
                    baseCategoryParametersById = connectEntities.F_nn_Category_Parameters.ToList().GroupBy(cp => cp.F_S_Categories).Select(p =>
                        new KeyValuePair<int, List<F_S_Parameters>>(p.Key.ID_CATEGORY, p.Select(i => i.F_S_Parameters).ToList())).ToList();
                }
            }
            return baseCategoryParametersById;
        }

        /// <summary>
        /// Имена параметров которые связаны с элементами-квартир
        /// </summary>        
        public static List<string> GetBaseParametersElementInFlat ()
        {
            if (baseParametersElemInFlat == null)
            {
                using (var connectEntities = ConnectEntities())
                {
                    baseParametersElemInFlat = entities.F_S_Parameters.Where(p => p.RELATE == (int)ParamRelateEnum.ElementInModule).
                        Select(s => s.NAME_PARAMETER).ToList();
                }
            }
            return baseParametersElemInFlat;
        }

        /// <summary>
        /// Получение значения для параметра IdWall - спсок id стен через ;
        /// </summary>
        /// <param name="idsWall">Id стен</param>
        /// <returns>Строка для заполнения параметра IdWall в элементе хосте стены (дверь, окно)</returns>
        public static string GetHostsValue (List<int> idsWall)
        {
            idsWall.Sort();
            return string.Join(";", idsWall);
        }

        /// <summary>
        /// Экспорт квартир в базу.
        /// </summary>      
        public static void Export (List<Apartment> apartments)
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

                // Запись квартир (с удалением елементо-квартир)
                FillApartments(apartments);

                // Запись елементов
                FillElements(apartments);

                // Прицеп элементов в квартиры (и запись параметров элементо-квартир)
                FillElementsToApartments(apartments);
            }
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
                        TYPE_FLAT = apart.TypeFlat
                    });
                }
                else
                {
                    // Уже есть такая квартира - удаление элементо-квартир
                    // Проверка типа квартиры - обновление
                    if (apartDB.TYPE_FLAT == null || !apartDB.TYPE_FLAT.Equals(apart.TypeFlat, StringComparison.OrdinalIgnoreCase))
                        apartDB.TYPE_FLAT = apart.TypeFlat;
                    // Удаление елементо-квартир                                                                   
                    entities.F_nn_Elements_Modules.RemoveRange(apartDB.F_nn_Elements_Modules.ToList());
                }
                apart.DBElement = apartDB;
            }
            entities.SaveChanges();
        }

        /// <summary>
        /// Запись елементов чертежа в базу.
        /// </summary>
        /// <param name="apartments">Элементы чертежа</param>
        private static void FillElements (IEnumerable<Apartment> apartments)
        {
            var allElements = apartments.SelectMany(a => a.Elements).ToList();
            var elementsBD = entities.F_S_Elements.ToList().Select(s => new Element(s)).Cast<IElement>().ToList();

            entities.F_S_FamilyInfos.Load();
            entities.F_S_Categories.Load();
            entities.F_nn_Category_Parameters.Load();

            // Записать все элементы
            AddElements(allElements, elementsBD);
        }

        /// <summary>
        /// Прицепка элементов к квартирам - и заполнение парампетиров элементо-квартир
        /// </summary>        
        private static void FillElementsToApartments (IEnumerable<Apartment> apartments)
        {
            foreach (var apart in apartments)
            {
                var apartDB = apart.DBElement;
                
                foreach (var item in apart.Elements)
                {
                    F_nn_Elements_Modules elemApart;
                    AddElementToApartment(apartDB, item, out elemApart);

                    item.DBElementInApart = elemApart;
                }
                entities.SaveChanges();

                // Запись параметров - со списком элементов - список id елем-квартир
                foreach (var elem in apart.Elements)
                {
                    foreach (var itemParam in elem.Parameters)
                    {
                        var elemListParam = itemParam.ObjectValue as List<IElement>;
                        if (elemListParam != null)
                        {
                            List<int> idsElem = elemListParam.Select(s => s.DBElementInApart.ID_ELEMENT_IN_FLAT).ToList();
                            itemParam.Value = GetHostsValue(idsElem);

                            var elemDb = elem.DBElement;
                            var paramDb = GetBaseCategoryParametersById().First(c => c.Key == elemDb.ID_CATEGORY)
                                .Value.First(p=>p.NAME_PARAMETER.Equals (itemParam.Name, StringComparison.OrdinalIgnoreCase));
                            var catParam = entities.F_nn_Category_Parameters.First(p => p.ID_CATEGORY == elemDb.ID_CATEGORY &&
                                p.ID_PARAMETER == paramDb.ID_PARAMETER);

                            var eifParam = elem.DBElementInApart.F_nn_ElementInFlatValue.FirstOrDefault(p => p.ID_CAT_PARAMETER == catParam.ID_CAT_PARAMETER);
                            if (eifParam != null)
                            {
                                eifParam.PARAMETER_VALUE = itemParam.Value;
                            }                            
                            entities.SaveChanges();
                        }
                    }
                }                                              
            }
        }

        /// <summary>
        /// Добавление элемента в квартиру (елементо-квартира) - прицепка
        /// </summary>        
        private static void AddElementToApartment (F_R_Flats apartDB, IElement elem, out F_nn_Elements_Modules elemApart)
        {
            var elemDB = elem.DBElement;
            elemApart = new F_nn_Elements_Modules {
                F_R_Flats = apartDB,
                F_S_Elements = elemDB,
                DIRECTION = elem.Direction,
                LOCATION = elem.LocationPoint
            };
            apartDB.F_nn_Elements_Modules.Add(elemApart);
            // Параметры елементо-квартры            
            // Параметры елементо-квартиры для этой категории в базе
            var paramsDb = GetBaseCategoryParametersById().First(c => c.Key == elemDB.ID_CATEGORY).Value
                    .Where(p => p.RELATE == (int)ParamRelateEnum.ElementInModule);
            if (paramsDb.Any())
            {
                foreach (var item in paramsDb)
                {
                    var paramElem = elem.Parameters.FirstOrDefault(p => p.Name.Equals(item.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(paramElem?.Value))
                    {
                        var catParam = entities.F_nn_Category_Parameters.First(p => p.ID_CATEGORY == elemDB.ID_CATEGORY &&
                                p.ID_PARAMETER == item.ID_PARAMETER);
                        entities.F_nn_ElementInFlatValue.Add(new F_nn_ElementInFlatValue {
                            F_nn_Category_Parameters = catParam,
                            F_nn_Elements_Modules = elemApart,
                            PARAMETER_VALUE = paramElem.Value
                        });
                    }
                }
            }
        }

        private static void AddElements (List<IElement> elements, List<IElement> elementsBD)
        {
            if (elements == null || elements.Count == 0) return;

            // Найти новые элементов
            var newElements = new List<KeyValuePair<IElement, List<IElement>>>();
            var uniqElems = elements.GroupBy(g => g).ToList();
            foreach (var group in uniqElems)
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
                        item.DBElement = elemBD.DBElement;
                    }
                }
            }        

            // Запись новых элементов
            if (newElements.Any())
            {
                // Запись новых семейств если есть
                FillFamilys(newElements.Select(s => s.Key).ToList());

                foreach (var group in newElements)
                {
                    // Запись нового элемента
                    FillNewElement(group.Key);
                    foreach (var item in group.Value)
                    {
                        item.DBElement = group.Key.DBElement;
                    }
                }
                entities.SaveChanges();
            }
        }

        private static void FillNewElement (IElement elem)
        {
            var elemBd = new F_S_Elements
            {
                F_S_FamilyInfos = entities.F_S_FamilyInfos.Local.First(f =>
                    f.FAMILY_NAME.Equals(elem.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                    f.FAMILY_SYMBOL.Equals(elem.FamilySymbolName, StringComparison.OrdinalIgnoreCase)),
                F_S_Categories = entities.F_S_Categories.Local.
                    First(c => c.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase))
            };

            // Запись параметров относящихся к элементу
            var categoryParametersBd = entities.F_nn_Category_Parameters.Local
                      .Where(c => c.F_S_Categories.NAME_RUS_CATEGORY.Equals(elem.CategoryElement, StringComparison.OrdinalIgnoreCase))
                      .Where(p=>p.F_S_Parameters.RELATE == (int)ParamRelateEnum.Element)
                      .Select(p => p).ToList();

            var paramsBd = new List<F_nn_ElementParam_Value>();
            foreach (var paramBd in categoryParametersBd)
            {
                Parameter elemParam = elem.Parameters.FirstOrDefault(p => p.Name.Equals(paramBd.F_S_Parameters.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));
                if (elemParam == null) continue;
                paramsBd.Add(entities.F_nn_ElementParam_Value.Add(new F_nn_ElementParam_Value
                {
                    F_nn_Category_Parameters = paramBd, F_S_Elements = elemBd, PARAMETER_VALUE = elemParam.Value
                }));
            }
            elemBd.F_nn_ElementParam_Value = paramsBd;
            entities.F_S_Elements.Add(elemBd);
            elem.DBElement = elemBd;
        }

        private static void FillFamilys (IReadOnlyCollection<IElement> elements)
        {
            if (elements == null || elements.Count == 0) return;

            var familys = elements.GroupBy(g => g.FamilyName + "|" + g.FamilySymbolName).Select(s => s.Key);
            var familysBd = entities.F_S_FamilyInfos.Select(s => s.FAMILY_NAME + "|" + s.FAMILY_SYMBOL);
            var newFamilys = familys.Except(familysBd, StringComparer.OrdinalIgnoreCase);
            if (newFamilys.Any())
            {
                foreach (var item in newFamilys)
                {
                    var values = item.Split('|');
                    entities.F_S_FamilyInfos.Add(new F_S_FamilyInfos {FAMILY_NAME = values[0], FAMILY_SYMBOL = values[1]});
                }
                entities.SaveChanges();
            }
        }
    }
}