using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.Revit;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.Model.Utils
{
    public static class RoomsTypeEditor
    {
        public static int SetRoomsType(List<Apartment> apartments)
        {
            int resCount = 0;
            Database db = HostApplicationServices.WorkingDatabase;
            using (var t = db.TransactionManager.StartTransaction())
            {
                foreach (var apart in apartments)
                {
                    string typeRoom = getRoomType(apart.Layer);
                    if (string.IsNullOrEmpty(typeRoom))
                    {
                        Inspector.AddError($"Не определен параметр типа квартиры '{Options.Instance.RoomTypeFlatParameter}' по слою {apart.Layer} блока квартиры {apart.Name}",
                            apart.IdBlRef, System.Drawing.SystemIcons.Error);
                        continue;
                    }
                    var rooms = apart.Modules.SelectMany(m => m.Elements).Where(e => e.CategoryElement.Equals("Помещения", StringComparison.OrdinalIgnoreCase));
                    foreach (var room in rooms)
                    {
                        var blRef = room.IdBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference;
                        if(blRef.AttributeCollection != null)
                        {
                            foreach (ObjectId idAtr in blRef.AttributeCollection)
                            {
                                var atr = idAtr.GetObject(OpenMode.ForRead, false, true) as AttributeReference;
                                if (atr.Tag.Equals(Options.Instance.RoomTypeFlatParameter, StringComparison.OrdinalIgnoreCase))
                                {
                                    atr.UpgradeOpen();
                                    atr.TextString = typeRoom.Trim();
                                    resCount++;
                                    break;
                                }
                            }
                        }
                    }
                }
                t.Commit();
            }
            return resCount;
        }

        private static string getRoomType(string layer)
        {
            var splitDash = layer.Split('_');
            if (splitDash.Length>2)
            {
                return splitDash[2];
            }
            return null;
        }
    }
}
