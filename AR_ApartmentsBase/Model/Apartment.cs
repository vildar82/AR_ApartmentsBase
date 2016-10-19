using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Drawing;
using AR_ApartmentBase.Model.Elements;
using AR_ApartmentBase.Model.DB.EntityModel;
using System.Data.Entity;

namespace AR_ApartmentBase.Model
{
    /// <summary>
    /// Квартира или МОП - блок в автокаде
    /// </summary>
    public class Apartment : IEquatable<Apartment>
    {   
        /// <summary>
        /// Имя блока
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Параметр типа квартиры - Студия, 1комн, и т.д.
        /// </summary>
        public string TypeFlat { get; set; }                
        /// <summary>
        /// Модули в квартире.
        /// </summary>
        public List<Module> Modules { get; set; }                      
        /// <summary>
        /// Угол поворота блока квартиры.
        /// </summary>
        public double Rotation { get; set; }
        public List<Parameter> Parameters { get; set; }
        public string Direction { get; set; }
        public string LocationPoint { get; set; }        
        public int Revision { get; set; }
        /// <summary>
        /// F_R_Flats
        /// </summary>
        public object DBObject { get; set; }

        public Apartment ()
        {

        }

        /// <summary>
        /// Конструктор для создания квартиры из базы
        /// </summary>
        public Apartment(F_R_Flats flatEnt)
        {
            Name = flatEnt.WORKNAME;            
            Modules = new List<Module>();
            DBObject = flatEnt;
            Revision = flatEnt.REVISION;
            TypeFlat = flatEnt.TYPE_FLAT;
        }                             

        public bool Equals(Apartment other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode();
        }
    }
}