namespace AR_ApartmentBase.Model.Export
{
   partial class FormBlocksExport
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.components = new System.ComponentModel.Container();
         this.listBoxBlocksToExport = new System.Windows.Forms.ListBox();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.buttonOk = new System.Windows.Forms.Button();
         this.buttonShow = new System.Windows.Forms.Button();
         this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
         this.labelCount = new System.Windows.Forms.Label();
         this.SuspendLayout();
         // 
         // listBoxBlocksToExport
         // 
         this.listBoxBlocksToExport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listBoxBlocksToExport.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
         this.listBoxBlocksToExport.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
         this.listBoxBlocksToExport.FormattingEnabled = true;
         this.listBoxBlocksToExport.Location = new System.Drawing.Point(12, 12);
         this.listBoxBlocksToExport.Name = "listBoxBlocksToExport";
         this.listBoxBlocksToExport.Size = new System.Drawing.Size(373, 310);
         this.listBoxBlocksToExport.TabIndex = 0;
         this.toolTip1.SetToolTip(this.listBoxBlocksToExport, "Выбранные блоки для экспорта по своим файлам. Красным помечены блоки файлы которы" +
        "х уже существуют.");
         this.listBoxBlocksToExport.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBoxBlocksToExport_DrawItem);
         this.listBoxBlocksToExport.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.listBoxBlocksToExport_MeasureItem);
         this.listBoxBlocksToExport.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBoxBlocksToExport_MouseDoubleClick);
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(310, 358);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 1;
         this.buttonCancel.Text = "Отмена";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // buttonOk
         // 
         this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOk.Location = new System.Drawing.Point(229, 358);
         this.buttonOk.Name = "buttonOk";
         this.buttonOk.Size = new System.Drawing.Size(75, 23);
         this.buttonOk.TabIndex = 1;
         this.buttonOk.Text = "ОК";
         this.toolTip1.SetToolTip(this.buttonOk, "Выполнить экспорт блоков");
         this.buttonOk.UseVisualStyleBackColor = true;
         // 
         // buttonShow
         // 
         this.buttonShow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.buttonShow.BackgroundImage = global::AR_ApartmentBase.Properties.Resources.Show;
         this.buttonShow.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
         this.buttonShow.Location = new System.Drawing.Point(12, 353);
         this.buttonShow.Name = "buttonShow";
         this.buttonShow.Size = new System.Drawing.Size(41, 32);
         this.buttonShow.TabIndex = 2;
         this.toolTip1.SetToolTip(this.buttonShow, "Показать выбранный блок на чертеже");
         this.buttonShow.UseVisualStyleBackColor = true;
         this.buttonShow.Click += new System.EventHandler(this.buttonShow_Click);
         // 
         // labelCount
         // 
         this.labelCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.labelCount.AutoSize = true;
         this.labelCount.Location = new System.Drawing.Point(18, 325);
         this.labelCount.Name = "labelCount";
         this.labelCount.Size = new System.Drawing.Size(16, 13);
         this.labelCount.TabIndex = 3;
         this.labelCount.Text = "-1";
         // 
         // FormBlocksExport
         // 
         this.AcceptButton = this.buttonOk;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(397, 393);
         this.Controls.Add(this.labelCount);
         this.Controls.Add(this.buttonShow);
         this.Controls.Add(this.buttonOk);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.listBoxBlocksToExport);
         this.Name = "FormBlocksExport";
         this.Text = "Блоки квартир для экспорта";
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.ListBox listBoxBlocksToExport;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOk;
      private System.Windows.Forms.Button buttonShow;
      private System.Windows.Forms.ToolTip toolTip1;
      private System.Windows.Forms.Label labelCount;
   }
}