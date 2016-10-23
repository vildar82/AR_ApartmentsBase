using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase_AutoCAD.AcadServices
{
   public static class LayerService
   {
      /// <summary>
      /// Включение слоев
      /// </summary>
      /// <param name="layersToOn"></param>
      public static void LayersOn(IList<ObjectId> layersToOn)
      {
         if (layersToOn != null && layersToOn.Count > 0)
         {
            foreach (var idLayer in layersToOn)
            {
               using (var lay = idLayer.Open(OpenMode.ForWrite) as LayerTableRecord)
               {
                  if (lay != null && lay.IsFrozen)
                  {
                     lay.IsFrozen = false;
                  }
               }
            }
         }
      }

      /// <summary>
      /// Выключение слоев
      /// </summary>
      public static List<ObjectId> LayersOff(string layerNameMatch)
      {
         var layersOff = new List<ObjectId>();
         Database db = HostApplicationServices.WorkingDatabase;
         using (var lt = db.LayerTableId.Open(OpenMode.ForRead) as LayerTable)
         {
            foreach (var idLayer in lt)
            {
               using (var layer = idLayer.Open(OpenMode.ForRead) as LayerTableRecord)
               {
                  if (Regex.IsMatch(layer.Name, layerNameMatch, RegexOptions.IgnoreCase))
                  {
                     if (!layer.IsFrozen)
                     {
                        layer.UpgradeOpen();
                        layer.IsFrozen = true;
                        layersOff.Add(idLayer);
                     }
                  }
               }
            }
         }
         return layersOff;
      }
   }
}
