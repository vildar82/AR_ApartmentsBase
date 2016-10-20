using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.Elements;

namespace AR_ApartmentBase.Model.DB.EntityModel
{
    public static class GetBaseApartments
    {
        /// <summary>
        /// Получение списка квартир в базе
        /// </summary>
        public static List<Apartment> GetAll()
        {
            // Преобразование квартир в базе в объекты Apartment
            List<Apartment> apartments = new List<Apartment>();
            using (var entities = BaseApartments.ConnectEntities())
            {
                entities.F_R_Flats.Load();                

                    var flats = entities.F_R_Flats.ToList();
                foreach (var flatEnt in flats)
                {
                    Apartment apart = new Apartment(flatEnt);
                    apartments.Add(apart);

                    //Все модули в квартире
                    var fms = flatEnt.F_nn_FlatModules.ToList();
                    foreach (var fmEnt in fms)
                    {
                        Module module = new Module(fmEnt, apart);

                        // Елементы в модуле
                        var emsEnt = fmEnt.F_R_Modules.F_nn_Elements_Modules.ToList();
                        foreach (var emEnt in emsEnt)
                        {
                            // Создание элемента из элемента базы базы
                            Element elem = new Element(module, emEnt);
                        }
                        //// Для дверей определение элемента стены
                        //var doors = module.Elements.OfType<DoorElement>();
                        //foreach (var door in doors)
                        //{
                        //    door.SearchHostWallDB(fmEnt.F_R_Modules);
                        //}
                    }
                }
            }
            return apartments;
        }
    }
}
