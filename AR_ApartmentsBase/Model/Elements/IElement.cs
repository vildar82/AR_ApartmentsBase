using AR_ApartmentBase.Model;
using System;
using System.Collections.Generic;
public interface IElement : IEquatable<IElement>
{
    string CategoryElement { get; set; }
    object DBElement { get; set; }
    object DBElementInApart { get; set; }
    string Direction { get; set; }
    string FamilyName { get; set; }
    string FamilySymbolName { get; set; }
    string LocationPoint { get; set; }
    string Name { get; set; }
    List<Parameter> Parameters { get; set; }
}