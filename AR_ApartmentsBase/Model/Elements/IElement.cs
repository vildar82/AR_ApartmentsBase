using AR_ApartmentBase.Model;
using System;
using System.Collections.Generic;
using AR_ApartmentBase.Model.DB.EntityModel;

public interface IElement : IEquatable<IElement>
{
    string CategoryElement { get; set; }
    F_S_Elements DBElement { get; set; }
    F_nn_Elements_Modules DBElementInApart { get; set; }
    string Direction { get; set; }
    string FamilyName { get; set; }
    string FamilySymbolName { get; set; }
    string LocationPoint { get; set; }
    string Name { get; set; }
    List<Parameter> Parameters { get; set; }    
}