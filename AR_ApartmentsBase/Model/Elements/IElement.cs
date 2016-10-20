using System.Collections.Generic;

namespace AR_ApartmentBase.Model.Elements
{
    public interface IElement
    {
        string CategoryElement { get; set; }
        object DBObject { get; set; }
        string Direction { get; set; }
        string FamilyName { get; set; }
        string FamilySymbolName { get; set; }
        string LocationPoint { get; set; }
        Module Module { get; set; }
        string Name { get; set; }
        List<Parameter> Parameters { get; set; }        
    }
}