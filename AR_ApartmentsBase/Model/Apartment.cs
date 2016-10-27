using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AR_ApartmentBase.Model.DB.EntityModel;
using AR_ApartmentBase.Model.Elements;

namespace AR_ApartmentBase.Model
{
    /// <summary>
    /// Модуль - блок помещения в автокаде
    /// </summary>
    public class Apartment : IEquatable<Apartment>
    {

        public string Name { get; set; }
        public string TypeFlat { get; set; }
        public List<IElement> Elements { get; set; }
        public string Direction { get; set; }
        public string LocationPoint { get; set; }
        public double Rotation { get; set; }

        /// <summary>
        /// F_R_Modules
        /// </summary>
        public object DBObject { get; set; }

        public Apartment()
        {
        }

        /// <summary>
        /// Конструктор для создания модуля из Базы
        /// </summary>
        public Apartment(F_nn_FlatModules fmEnt, Apartment apart)
        {
            Name = fmEnt.F_R_Modules.NAME_MODULE;

            Elements = new List<IElement>();
            Direction = fmEnt.DIRECTION;
            LocationPoint = fmEnt.LOCATION;
            // Revision = fmEnt.F_R_Modules.REVISION;
        }

        public bool Equals(Apartment other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   this.Direction.Equals(other.Direction) &&
                   this.LocationPoint.Equals(other.LocationPoint) &&
                   this.Elements.SequenceEqual(other.Elements);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

    }
}