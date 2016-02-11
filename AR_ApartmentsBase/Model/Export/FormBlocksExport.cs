using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using System.IO;
using AR_ApartmentBase.Model.Revit;

namespace AR_ApartmentBase.Model.Export
{
   public partial class FormBlocksExport : Form
   {
      private BindingSource _binding;
      private Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

      public FormBlocksExport(List<Apartment> blocksToExport)
      {
         InitializeComponent();

         _binding = new BindingSource();
         _binding.ListChanged += labelCountUpdate;
         _binding.DataSource = blocksToExport;

         listBoxBlocksToExport.DisplayMember = "Name";         
         listBoxBlocksToExport.DataSource = _binding;         
      }

      private void buttonShow_Click(object sender, EventArgs e)
      {
         Apartment blockToExp =listBoxBlocksToExport.SelectedItem as Apartment;
         if (blockToExp != null && blockToExp.Extents.Diagonal()>0)
         {  
            ed.Zoom(blockToExp.Extents);
         }
      }

      private void listBoxBlocksToExport_MouseDoubleClick(object sender, MouseEventArgs e)
      {
         buttonShow_Click(null, null);
      }

      private void listBoxBlocksToExport_DrawItem(object sender, DrawItemEventArgs e)
      {
         ListBox list = (ListBox)sender;
         if (e.Index > -1)
         {
            e.DrawBackground();
            e.DrawFocusRectangle();

            Apartment blToExport = list.Items[e.Index] as Apartment;
            if (blToExport != null)
            {
               Brush brush;
               if (File.Exists(blToExport.File))
               {
                  brush = Brushes.Red;
               }
               else
               {
                  brush = Brushes.Black;
               }                                            
               e.Graphics.DrawString(blToExport.BlockName, e.Font, brush, e.Bounds.X, e.Bounds.Y);
            }
         }
      }

      private void listBoxBlocksToExport_MeasureItem(object sender, MeasureItemEventArgs e)
      {
         e.ItemHeight = listBoxBlocksToExport.Font.Height;
      }

      private void labelCountUpdate(object sender, EventArgs e)
      {
         labelCount.Text = _binding.Count.ToString();
      }
   }
}
