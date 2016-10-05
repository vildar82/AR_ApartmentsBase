using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AcadLib.Geometry;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.Model.Revit.Elements
{
    public class WallHostBase : Element, IWallHost
    {
        public List<WallElement> HostWall { get; set; }

        public WallHostBase (BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category)
              : base(blRefElem, module, blName, parameters, category)
        {
            // Добавление параметра idWall - 0 - условно
            Parameters.Add(new Parameter(Options.Instance.HostWallParameter, "0"));
            DefineOrientation(blRefElem);
        }

        public WallHostBase (Module module, F_nn_Elements_Modules emEnt)
           : base(module, emEnt)
        {

        }

        /// <summary>
        ///  Поиск блока стены по границам стен и точке вставки блока двери
        /// </summary>
        public void SearchHostWallDwg (List<Element> elements)
        {
            this.HostWall = new List<WallElement>();
            var walls = elements.OfType<WallElement>();
            foreach (var wall in walls)
            {
                // Попадает ли точка вствавки блока двери в границы стены
                if (wall.Contour == null)
                {
                    if (wall.ExtentsClean.IsPointInBounds(Position))
                    {
                        HostWall.Add(wall);
                    }
                }
                else
                {
                    if (wall.Contour.IsPointInsidePolygon(Position))
                    {
                        HostWall.Add(wall);
                    }
                }
            }
            // Ошибка если не найдена стена
            if (this.HostWall.Count == 0)
            {
                Inspector.AddError($"Не определена стена для элемента {FamilySymbolName}. ",
                      ExtentsInModel, IdBlRef, System.Drawing.SystemIcons.Error);
                // Исключить дверь из элементов модуля - и дверь не будет записана в базк
                elements.Remove(this);
            }
        }

        /// <summary>
        /// Определение стены для двери в объектах созданных не из чертежа, а из базы
        /// </summary>
        public void SearchHostWallDB (F_R_Modules moduleEnt)
        {
            HostWall = new List<WallElement>();
            // найти стену по id в параметре двери
            var paramIdWall = Parameters.Single(p => p.Name.Equals(Options.Instance.HostWallParameter, StringComparison.OrdinalIgnoreCase));
            var idsWall = paramIdWall.Value.Split(';');
            foreach (var item in idsWall)
            {
                int idWall = Convert.ToInt32(item);
                // найти стену в элементах модуля         
                var wall = this.Module.Elements.SingleOrDefault(e => ((F_nn_Elements_Modules)e.DBObject).ID_ELEMENT_IN_MODULE == idWall);
                HostWall.Add((WallElement)wall);
            }
        }

        public override bool Equals (Element other)
        {
            WallHostBase elemHost2 = other as WallHostBase;
            if (elemHost2 == null) return false;
            if (ReferenceEquals(this, elemHost2)) return true;

            var param1 = Parameters.Where(p => !p.Name.Equals(Options.Instance.HostWallParameter, StringComparison.OrdinalIgnoreCase)).ToList();
            var param2 = elemHost2.Parameters.Where(p => !p.Name.Equals(Options.Instance.HostWallParameter, StringComparison.OrdinalIgnoreCase)).ToList();

            return FamilyName.Equals(elemHost2.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                   FamilySymbolName.Equals(elemHost2.FamilySymbolName, StringComparison.OrdinalIgnoreCase) &&
                   Direction.Equals(elemHost2.Direction) &&
                   LocationPoint.Equals(elemHost2.LocationPoint) &&
                   (HostWall != null && HostWall.Count == elemHost2.HostWall.Count) &&
                   Parameter.Equal(param1, param2);
        }
    }
}
