using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AR_ApartmentBase.Model
{
   public partial class FormOptions : Form
   {
      public Options Options { get; set; }

      public FormOptions(Options options)
      {
         InitializeComponent();

         Options = options;
         propertyGrid1.SelectedObject = options;
      }
   }
}
