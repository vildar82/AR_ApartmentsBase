using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.Revit;
using AR_ApartmentBase.Model.Revit.Elements;
using Autodesk.AutoCAD.Runtime;
using MoreLinq;

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

                using (var progress = new ProgressMeter())
                {
                    progress.SetLimit(entities.F_R_Flats.Local.Count);
                    progress.Start("Считывание квартир из базы...");

                    var flatsLastRev = entities.F_R_Flats.Local.GroupBy(g => g.WORKNAME).Select(f => f.MaxBy(r => r.REVISION)).ToList();
                    foreach (var flatEnt in flatsLastRev)
                    {
                        progress.MeterProgress();

                        Apartment apart = new Apartment(flatEnt);
                        apartments.Add(apart);

                        //Все модули в квартире
                        var fmsLastModRev = flatEnt.F_nn_FlatModules.GroupBy(fm => fm.F_R_Modules.NAME_MODULE)
                                             .Select(m => m.MaxBy(r => r.F_R_Modules.REVISION)).ToList();

                        foreach (var fmEnt in fmsLastModRev)
                        {
                            Module module = new Module(fmEnt, apart);

                            // Елементы в модуле
                            var emsEnt = fmEnt.F_R_Modules.F_nn_Elements_Modules.ToList();
                            foreach (var emEnt in emsEnt)
                            {
                                // Создание элемента из элемента базы базы
                                Element elem = ElementFactory.CreateElementDB(module, emEnt);
                            }
                            // Для дверей определение элемента стены
                            var doors = module.Elements.OfType<DoorElement>();
                            foreach (var door in doors)
                            {
                                door.SearchHostWallDB(fmEnt.F_R_Modules);
                            }
                        }
                    }
                    progress.Stop();
                }
            }
            return apartments;
        }
    }
}
