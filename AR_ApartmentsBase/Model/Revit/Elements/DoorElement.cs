using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.DB.DbServices;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Revit.Elements
{
    public class DoorElement : Element
    {
        public WallElement HostWall { get; set; }
        public int count = -1;

        public DoorElement(BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category)
              : base(blRefElem, module, blName, parameters, category)
        {
            // Добавление параметра idWall - 0 - условно
            Parameters.Add(new Parameter(Options.Instance.DoorHostWallParameter, count--.ToString()));
        }

        public DoorElement(Module module, F_nn_Elements_Modules emEnt)
           : base(module, emEnt)
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
                if (wall.ExtentsClean.IsPointInBounds(this.Position, 100))
                {
                    this.HostWall = wall;
                }
            }
            // Ошибка если не найдена стена
            if (this.HostWall == null)
            {
                Inspector.AddError($"Не определена стена для двери {this.FamilySymbolName}. ",
                      ExtentsInModel, IdBlRef, System.Drawing.SystemIcons.Error);
                // Исключить дверь из элементов модуля - и дверь не будет записана в базк
                elements.Remove(this);
            }
        }

        public void DefineOrientation(BlockReference blRefElem)
        {
            // Определение направления
            using (var btr = blRefElem.BlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
            {
                bool isFinded = false;
                foreach (var idEnt in btr)
                {
                    using (var lineOrient = idEnt.Open(OpenMode.ForRead, false, true) as Line)
                    {
                        if (lineOrient == null || lineOrient.ColorIndex != Options.Instance.DoorOrientLineColorIndex || !lineOrient.Visible) continue;
                        var lineTemp = (Line)lineOrient.Clone();
                        lineTemp.TransformBy(blRefElem.BlockTransform);
                        Direction = Element.GetDirection(lineTemp.Angle);
                        isFinded = true;
                        break;
                    }
                }
                if (!isFinded)
                {
                    Inspector.AddError($"Не определено направление открывания двери {Name}. " +
                       $"Направление открывания двери определяется отрезком с цветом {Options.Instance.DoorOrientLineColorIndex} в блоке двери.",
                       this.ExtentsInModel, this.IdBlRef, System.Drawing.SystemIcons.Error);
                }
            }
        }

        /// <summary>
        /// Определение стены для двери в объектах созданных не из чертежа, а из базы
        /// </summary>
        public void SearchHostWallDB(F_R_Modules moduleEnt)
        {
            // найти стену по id в параметре двери
            var paramIdWall = Parameters.Single(p => p.Name.Equals(Options.Instance.DoorHostWallParameter, StringComparison.OrdinalIgnoreCase));
            int idWall = Convert.ToInt32(paramIdWall.Value);
            // найти стену в элементах модуля         
            var wall = this.Module.Elements.SingleOrDefault(e => ((F_nn_Elements_Modules)e.DBObject).ID_ELEMENT_IN_MODULE == idWall);
            HostWall = (WallElement)wall;
        }

        public override bool Equals(Element other)
        {
            DoorElement door2 = other as DoorElement;
            if (door2 == null) return false;
            if (ReferenceEquals(this, door2)) return true;

            var param1 = Parameters.Where(p => !p.Name.Equals(Options.Instance.DoorHostWallParameter, StringComparison.OrdinalIgnoreCase)).ToList();
            var param2 = door2.Parameters.Where(p => !p.Name.Equals(Options.Instance.DoorHostWallParameter, StringComparison.OrdinalIgnoreCase)).ToList();

            return FamilyName.Equals(door2.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                   FamilySymbolName.Equals(door2.FamilySymbolName, StringComparison.OrdinalIgnoreCase) &&
                   Direction.Equals(door2.Direction) &&
                   LocationPoint.Equals(door2.LocationPoint) &&
                   (HostWall != null && HostWall.Equals(door2.HostWall)) &&
                   Parameter.Equal(param1, param2);
        }
    }
}
