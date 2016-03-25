using System.Drawing;

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
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOk = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.buttonOptions = new System.Windows.Forms.Button();
            this.buttonShow = new System.Windows.Forms.Button();
            this.labelCount = new System.Windows.Forms.Label();
            this.treeViewApartments = new System.Windows.Forms.TreeView();
            this.textBoxInfo = new System.Windows.Forms.TextBox();
            this.labelNotInBase = new System.Windows.Forms.Label();
            this.labelError = new System.Windows.Forms.Label();
            this.labelChanged = new System.Windows.Forms.Label();
            this.labelNotInDwg = new System.Windows.Forms.Label();
            this.labelOK = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(450, 495);
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
            this.buttonOk.Location = new System.Drawing.Point(369, 495);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 1;
            this.buttonOk.Text = "ОК";
            this.toolTip1.SetToolTip(this.buttonOk, "Выполнить экспорт блоков");
            this.buttonOk.UseVisualStyleBackColor = true;
            // 
            // buttonOptions
            // 
            this.buttonOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOptions.BackgroundImage = global::AR_ApartmentBase.Properties.Resources.options;
            this.buttonOptions.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.buttonOptions.Location = new System.Drawing.Point(117, 491);
            this.buttonOptions.Name = "buttonOptions";
            this.buttonOptions.Size = new System.Drawing.Size(30, 30);
            this.buttonOptions.TabIndex = 4;
            this.toolTip1.SetToolTip(this.buttonOptions, "Настройки. После изменения настроек нужно перезапустить команду экспорта.");
            this.buttonOptions.UseVisualStyleBackColor = true;
            this.buttonOptions.Click += new System.EventHandler(this.buttonOptions_Click);
            // 
            // buttonShow
            // 
            this.buttonShow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonShow.BackgroundImage = global::AR_ApartmentBase.Properties.Resources.Show;
            this.buttonShow.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.buttonShow.Location = new System.Drawing.Point(60, 489);
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
            this.labelCount.Location = new System.Drawing.Point(12, 499);
            this.labelCount.Name = "labelCount";
            this.labelCount.Size = new System.Drawing.Size(16, 13);
            this.labelCount.TabIndex = 3;
            this.labelCount.Text = "-1";
            // 
            // treeViewApartments
            // 
            this.treeViewApartments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewApartments.Location = new System.Drawing.Point(0, 0);
            this.treeViewApartments.Name = "treeViewApartments";
            this.treeViewApartments.Size = new System.Drawing.Size(513, 235);
            this.treeViewApartments.TabIndex = 5;
            this.treeViewApartments.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewApartments_AfterSelect);
            this.treeViewApartments.DoubleClick += new System.EventHandler(this.treeViewApartments_DoubleClick);
            // 
            // textBoxInfo
            // 
            this.textBoxInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxInfo.Location = new System.Drawing.Point(0, 0);
            this.textBoxInfo.Multiline = true;
            this.textBoxInfo.Name = "textBoxInfo";
            this.textBoxInfo.ReadOnly = true;
            this.textBoxInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxInfo.Size = new System.Drawing.Size(513, 232);
            this.textBoxInfo.TabIndex = 6;
            // 
            // labelNotInBase
            // 
            this.labelNotInBase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelNotInBase.AutoSize = true;
            this.labelNotInBase.ForeColor = System.Drawing.Color.Lime;
            this.labelNotInBase.Location = new System.Drawing.Point(166, 489);
            this.labelNotInBase.Name = "labelNotInBase";
            this.labelNotInBase.Size = new System.Drawing.Size(41, 13);
            this.labelNotInBase.TabIndex = 7;
            this.labelNotInBase.Text = "Новые";
            // 
            // labelError
            // 
            this.labelError.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelError.AutoSize = true;
            this.labelError.ForeColor = System.Drawing.Color.Red;
            this.labelError.Location = new System.Drawing.Point(213, 489);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(47, 13);
            this.labelError.TabIndex = 7;
            this.labelError.Text = "Ошибки";
            // 
            // labelChanged
            // 
            this.labelChanged.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelChanged.AutoSize = true;
            this.labelChanged.ForeColor = System.Drawing.Color.Olive;
            this.labelChanged.Location = new System.Drawing.Point(266, 489);
            this.labelChanged.Name = "labelChanged";
            this.labelChanged.Size = new System.Drawing.Size(65, 13);
            this.labelChanged.TabIndex = 7;
            this.labelChanged.Text = "Изменение";
            // 
            // labelNotInDwg
            // 
            this.labelNotInDwg.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelNotInDwg.AutoSize = true;
            this.labelNotInDwg.ForeColor = System.Drawing.Color.DarkViolet;
            this.labelNotInDwg.Location = new System.Drawing.Point(166, 505);
            this.labelNotInDwg.Name = "labelNotInDwg";
            this.labelNotInDwg.Size = new System.Drawing.Size(80, 13);
            this.labelNotInDwg.TabIndex = 7;
            this.labelNotInDwg.Text = "Нет в чертеже";
            // 
            // labelOK
            // 
            this.labelOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOK.AutoSize = true;
            this.labelOK.ForeColor = System.Drawing.Color.Blue;
            this.labelOK.Location = new System.Drawing.Point(252, 505);
            this.labelOK.Name = "labelOK";
            this.labelOK.Size = new System.Drawing.Size(85, 13);
            this.labelOK.TabIndex = 7;
            this.labelOK.Text = "Нет изменений";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeViewApartments);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.textBoxInfo);
            this.splitContainer1.Size = new System.Drawing.Size(513, 471);
            this.splitContainer1.SplitterDistance = 235;
            this.splitContainer1.TabIndex = 8;
            // 
            // FormBlocksExport
            // 
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(537, 521);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.labelOK);
            this.Controls.Add(this.labelNotInDwg);
            this.Controls.Add(this.labelChanged);
            this.Controls.Add(this.labelError);
            this.Controls.Add(this.labelNotInBase);
            this.Controls.Add(this.buttonOptions);
            this.Controls.Add(this.labelCount);
            this.Controls.Add(this.buttonShow);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.buttonCancel);
            this.Name = "FormBlocksExport";
            this.Text = "Блоки квартир для экспорта";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

      }

      #endregion
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOk;
      private System.Windows.Forms.Button buttonShow;
      private System.Windows.Forms.ToolTip toolTip1;
      private System.Windows.Forms.Label labelCount;
      private System.Windows.Forms.Button buttonOptions;
      private System.Windows.Forms.TreeView treeViewApartments;
      private System.Windows.Forms.TextBox textBoxInfo;
      private System.Windows.Forms.Label labelNotInBase;
      private System.Windows.Forms.Label labelError;
      private System.Windows.Forms.Label labelChanged;
      private System.Windows.Forms.Label labelNotInDwg;
      private System.Windows.Forms.Label labelOK;
        private System.Windows.Forms.SplitContainer splitContainer1;
    }
}