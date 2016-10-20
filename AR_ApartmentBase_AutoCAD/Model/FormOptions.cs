using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AR_ApartmentBase.AutoCAD
{
   public partial class FormOptions : Form
   {
      public OptionsAC Options { get; set; }

      public FormOptions(OptionsAC options)
      {
         InitializeComponent();

         Options = options;
         propertyGrid1.SelectedObject = options;
      }
   }
}
