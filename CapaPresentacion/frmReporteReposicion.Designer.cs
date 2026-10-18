namespace CapaPresentacion
{
    partial class frmReporteReposicion
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
            this.cboProveedorCotizacion = new System.Windows.Forms.ComboBox();
            this.dbsistemA_VENTADataSet1 = new CapaEntidad.DBSISTEMA_VENTADataSet();
            this.btnConsultarCotizacion = new FontAwesome.Sharp.IconButton();
            this.dgvCotizacion = new System.Windows.Forms.DataGridView();
            this.btnExportarExcel = new FontAwesome.Sharp.IconButton();
            ((System.ComponentModel.ISupportInitialize)(this.dbsistemA_VENTADataSet1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCotizacion)).BeginInit();
            this.SuspendLayout();
            // 
            // cboProveedorCotizacion
            // 
            this.cboProveedorCotizacion.FormattingEnabled = true;
            this.cboProveedorCotizacion.Location = new System.Drawing.Point(39, 68);
            this.cboProveedorCotizacion.Name = "cboProveedorCotizacion";
            this.cboProveedorCotizacion.Size = new System.Drawing.Size(179, 24);
            this.cboProveedorCotizacion.TabIndex = 0;
            // 
            // dbsistemA_VENTADataSet1
            // 
            this.dbsistemA_VENTADataSet1.DataSetName = "DBSISTEMA_VENTADataSet";
            this.dbsistemA_VENTADataSet1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // btnConsultarCotizacion
            // 
            this.btnConsultarCotizacion.BackColor = System.Drawing.Color.ForestGreen;
            this.btnConsultarCotizacion.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnConsultarCotizacion.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnConsultarCotizacion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConsultarCotizacion.ForeColor = System.Drawing.SystemColors.Control;
            this.btnConsultarCotizacion.IconChar = FontAwesome.Sharp.IconChar.FolderOpen;
            this.btnConsultarCotizacion.IconColor = System.Drawing.Color.White;
            this.btnConsultarCotizacion.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnConsultarCotizacion.IconSize = 16;
            this.btnConsultarCotizacion.Location = new System.Drawing.Point(39, 113);
            this.btnConsultarCotizacion.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnConsultarCotizacion.Name = "btnConsultarCotizacion";
            this.btnConsultarCotizacion.Size = new System.Drawing.Size(179, 41);
            this.btnConsultarCotizacion.TabIndex = 67;
            this.btnConsultarCotizacion.Text = "Consultar";
            this.btnConsultarCotizacion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnConsultarCotizacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnConsultarCotizacion.UseVisualStyleBackColor = false;
            // 
            // dgvCotizacion
            // 
            this.dgvCotizacion.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCotizacion.Location = new System.Drawing.Point(252, 68);
            this.dgvCotizacion.Name = "dgvCotizacion";
            this.dgvCotizacion.RowHeadersWidth = 51;
            this.dgvCotizacion.RowTemplate.Height = 24;
            this.dgvCotizacion.Size = new System.Drawing.Size(734, 319);
            this.dgvCotizacion.TabIndex = 68;
            // 
            // btnExportarExcel
            // 
            this.btnExportarExcel.BackColor = System.Drawing.Color.White;
            this.btnExportarExcel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExportarExcel.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnExportarExcel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportarExcel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportarExcel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.btnExportarExcel.IconChar = FontAwesome.Sharp.IconChar.FileExcel;
            this.btnExportarExcel.IconColor = System.Drawing.Color.Lime;
            this.btnExportarExcel.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnExportarExcel.IconSize = 25;
            this.btnExportarExcel.Location = new System.Drawing.Point(39, 171);
            this.btnExportarExcel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnExportarExcel.Name = "btnExportarExcel";
            this.btnExportarExcel.Size = new System.Drawing.Size(179, 44);
            this.btnExportarExcel.TabIndex = 84;
            this.btnExportarExcel.Text = "Descargar Excel";
            this.btnExportarExcel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnExportarExcel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnExportarExcel.UseVisualStyleBackColor = false;
            // 
            // frmReporteReposicion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 606);
            this.Controls.Add(this.btnExportarExcel);
            this.Controls.Add(this.dgvCotizacion);
            this.Controls.Add(this.btnConsultarCotizacion);
            this.Controls.Add(this.cboProveedorCotizacion);
            this.Name = "frmReporteReposicion";
            this.Text = "frmReporteReposicion";
            ((System.ComponentModel.ISupportInitialize)(this.dbsistemA_VENTADataSet1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCotizacion)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cboProveedorCotizacion;
        private CapaEntidad.DBSISTEMA_VENTADataSet dbsistemA_VENTADataSet1;
        private FontAwesome.Sharp.IconButton btnConsultarCotizacion;
        private System.Windows.Forms.DataGridView dgvCotizacion;
        private FontAwesome.Sharp.IconButton btnExportarExcel;
    }
}