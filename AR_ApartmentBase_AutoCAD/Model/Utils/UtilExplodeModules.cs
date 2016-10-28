using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace AR_ApartmentBase_AutoCAD.Utils
{
    public static class UtilExplodeModules
    {
        public static void ExplodeModules (Database db)
        {
            using (var t = db.TransactionManager.StartTransaction())
            {
                var bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                foreach (var idBtr in bt)
                {
                    var btr = idBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;
                    var rxBlRef = RXClass.GetClass(typeof(BlockReference));
                    if (ApartmentAC.IsBlockNameApartment(btr.Name))
                    {
                        foreach (var idEntInBtr in btr)
                        {
                            if (idEntInBtr.ObjectClass == rxBlRef)
                            {
                                var blRef = idEntInBtr.GetObject(OpenMode.ForWrite) as BlockReference;
                                string blName = blRef.GetEffectiveName();
                                if (blName.Contains("Модуль", StringComparison.OrdinalIgnoreCase))
                                {
                                    //var dbs = new DBObjectCollection();
                                    blRef.ExplodeToOwnerSpace();
                                    blRef.Erase();

                                    //btr.UpgradeOpen();
                                    //foreach (var item in dbs)
                                    //{
                                    //    btr.AppendEntity((Entity)item);
                                    //    t.AddNewlyCreatedDBObject((Entity)item, true);
                                    //}
                                    break;
                                }
                            }
                        }
                    }
                }    
                t.Commit();
            }
        }
    }
}
