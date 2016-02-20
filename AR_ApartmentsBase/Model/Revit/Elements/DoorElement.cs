using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.DB.DbServices;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.Model.Revit.Elements
{
   public class DoorElement : Element
   {
      public WallElement HostWall { get; set; }
      public int count = 0;

      public DoorElement(BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category) 
            : base(blRefElem, module, blName, parameters, category)
      {
         // Добавление параметра idWall - 0 - условно
         parameters.Add(new Parameter() { Name = Options.Instance.DoorHostWallParameter, Value = count++.ToString() });         
      }

      public DoorElement(Module module, string familyName, string fsn, List<Parameter> parameters, string category)
         : base(module, familyName, fsn, parameters, category)
      {
         
      }

      /// <summary>
      ///  Поиск блока стены по границам стен и точке вставки блока двери
      /// </summary>
      public void SearchHostWallDwg(List<Element> elements)
      {
         var walls = elements.OfType<WallElement>();
         foreach (var wall in walls)
         {
            // Попадает ли точка вствавки блока двери в границы стены
            if (wall.ExtentsClean.IsPointInBounds(this.Position))
            {
               this.HostWall = wall;
               break;
            }
         }
         // Ошибка если не найдена стена
         if (this.HostWall == null)
         {
            Inspector.AddError($"Не определена стена для двери {this.FamilySymbolName}. ", 
                  IdBlRefElement, ExtentsInModel, System.Drawing.SystemIcons.Error);
         }
      }

      ///// <summary>
      ///// Поиск принадлежащей двери стены в элементах базы данных этого модуля
      ///// </summary>
      //public void SearchHostWallDB()
      //{
      //   F_S_Elements doorEnt = (F_S_Elements)this.DBObject;

      //   // Параметр стены HostWall        
      //   var idWallParamValue = Parameters.SingleOrDefault(p => p.Name.Equals(Options.Instance.DoorHostWallParameter, StringComparison.OrdinalIgnoreCase)).Value;

      //   // Поиск стены с таким параметром
      //   var hostWallElem = this.Module.Elements.OfType<WallElement>().SingleOrDefault(e =>
      //      e.Parameters.SingleOrDefault(p => p.Name.Equals(Options.Instance.DoorHostWallParameter)).Value.Equals(idWallParamValue));

      //   HostWall = hostWallElem;
      //}


      public void DefineOrientation (BlockReference blRefElem)
      {
         // Определение направления
         using (var btr = blRefElem.BlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
         {
            bool isFinded = false;
            foreach (var idEnt in btr)
            {
               using (var lineOrient = idEnt.Open(OpenMode.ForRead, false, true) as Line)
               {
                  if (lineOrient == null || lineOrient.ColorIndex != Options.Instance.DoorOrientLineColorIndex) continue;
                  Direction = TypeConverter.Point(lineOrient.Normal);
                  isFinded = true;
                  break;
               }
            }
            if (!isFinded)
            {
               Inspector.AddError($"Не определено направление открывания двери {Name}. " + 
                  $"Направление открывания двери определяется отрезком с цветом {Options.Instance.DoorOrientLineColorIndex} в блоке двери.",
                  this.ExtentsInModel, this.IdBlRefElement, System.Drawing.SystemIcons.Error);
            }
         }
      }
   }
}
