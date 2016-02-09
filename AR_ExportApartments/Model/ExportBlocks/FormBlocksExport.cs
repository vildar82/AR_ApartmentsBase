using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AR_ExportApartments.Model.ExportBlocks;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using System.IO;

namespace AR_ApartmentExport.Model.ExportBlocks
{
   public partial class FormBlocksExport : Form
   {
      private BindingSource _binding;
      private Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

      public FormBlocksExport(List<BlockToExport> blocksToExport)
      {
         InitializeComponent();

         _binding = new BindingSource();
         _binding.DataSource = blocksToExport;
         listBoxBlocksToExport.DataSource = _binding;
         listBoxBlocksToExport.DisplayMember = "Name";         
      }

      private void button1_Click(object sender, EventArgs e)
      {
         BlockToExport blockToExp =listBoxBlocksToExport.SelectedItem as BlockToExport;
         if (blockToExp != null && blockToExp.Extents.Diagonal()>0)
         {  
            ed.Zoom(blockToExp.Extents);
         }
      }

      private void listBoxBlocksToExport_MouseDoubleClick(object sender, MouseEventArgs e)
      {
         button1_Click(null, null);
      }

      private void listBoxBlocksToExport_DrawItem(object sender, DrawItemEventArgs e)
      {
         ListBox list = (ListBox)sender;
         if (e.Index > -1)
         {
            e.DrawBackground();
            e.DrawFocusRectangle();

            BlockToExport blToExport = list.Items[e.Index] as BlockToExport;
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
               e.Graphics.DrawString(blToExport.Name, e.Font, brush, e.Bounds.X, e.Bounds.Y);
            }
         }
      }

      private void listBoxBlocksToExport_MeasureItem(object sender, MeasureItemEventArgs e)
      {
         e.ItemHeight = listBoxBlocksToExport.Font.Height;
      }
   }
}
