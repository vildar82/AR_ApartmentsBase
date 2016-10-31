using AR_ApartmentBase.Model;
using System;
using System.Collections.Generic;
public interface IElement : IEquatable<IElement>
{
    int IdInRevit { get; set; }
    string CategoryElement { get; set; }
    object DBObject { get; set; }
    string Direction { get; set; }
    string FamilyName { get; set; }
    string FamilySymbolName { get; set; }
    string LocationPoint { get; set; }
    string Name { get; set; }
    List<Parameter> Parameters { get; set; }
}