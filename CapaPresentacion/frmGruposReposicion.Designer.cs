namespace CapaPresentacion
{
    partial class frmGruposReposicion
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.label13 = new System.Windows.Forms.Label();
            this.txtIndice = new System.Windows.Forms.TextBox();
            this.txtBusqueda = new System.Windows.Forms.TextBox();
            this.cboBuscar = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtId = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.btnEliminarGrupo = new FontAwesome.Sharp.IconButton();
            this.btnCancelarGrupo = new FontAwesome.Sharp.IconButton();
            this.btnGuardarGrupo = new FontAwesome.Sharp.IconButton();
            this.label12 = new System.Windows.Forms.Label();
            this.dgvGrupos = new System.Windows.Forms.DataGridView();
            this.btnSeleccionar = new System.Windows.Forms.DataGridViewButtonColumn();
            this.Id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Descripcion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.EstadoValor = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Estado = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label9 = new System.Windows.Forms.Label();
            this.cboEstado = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtNombreGrupo = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.numMinimoGrupo = new System.Windows.Forms.NumericUpDown();
            this.numMaximoGrupo = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvGrupos)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinimoGrupo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaximoGrupo)).BeginInit();
            this.SuspendLayout();
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label13.Location = new System.Drawing.Point(452, 83);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(21, 16);
            this.label13.TabIndex = 78;
            this.label13.Text = "Id:";
            this.label13.Visible = false;
            // 
            // txtIndice
            // 
            this.txtIndice.Location = new System.Drawing.Point(388, 80);
            this.txtIndice.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtIndice.Name = "txtIndice";
            this.txtIndice.ReadOnly = true;
            this.txtIndice.Size = new System.Drawing.Size(39, 22);
            this.txtIndice.TabIndex = 76;
            this.txtIndice.Text = "0";
            this.txtIndice.Visible = false;
            // 
            // txtBusqueda
            // 
            this.txtBusqueda.Location = new System.Drawing.Point(948, 63);
            this.txtBusqueda.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtBusqueda.Name = "txtBusqueda";
            this.txtBusqueda.Size = new System.Drawing.Size(167, 22);
            this.txtBusqueda.TabIndex = 75;
            // 
            // cboBuscar
            // 
            this.cboBuscar.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBuscar.FormattingEnabled = true;
            this.cboBuscar.Location = new System.Drawing.Point(734, 59);
            this.cboBuscar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboBuscar.Name = "cboBuscar";
            this.cboBuscar.Size = new System.Drawing.Size(193, 24);
            this.cboBuscar.TabIndex = 74;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label11.Location = new System.Drawing.Point(625, 63);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(76, 16);
            this.label11.TabIndex = 73;
            this.label11.Text = "Buscar Por:";
            // 
            // txtId
            // 
            this.txtId.Location = new System.Drawing.Point(483, 80);
            this.txtId.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtId.Name = "txtId";
            this.txtId.ReadOnly = true;
            this.txtId.Size = new System.Drawing.Size(32, 22);
            this.txtId.TabIndex = 72;
            this.txtId.Text = "0";
            this.txtId.Visible = false;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label10.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(285, 43);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(979, 59);
            this.label10.TabIndex = 71;
            this.label10.Text = "Lista Categoria:";
            // 
            // btnEliminarGrupo
            // 
            this.btnEliminarGrupo.BackColor = System.Drawing.Color.Red;
            this.btnEliminarGrupo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEliminarGrupo.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnEliminarGrupo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEliminarGrupo.ForeColor = System.Drawing.SystemColors.Control;
            this.btnEliminarGrupo.IconChar = FontAwesome.Sharp.IconChar.TrashAlt;
            this.btnEliminarGrupo.IconColor = System.Drawing.Color.White;
            this.btnEliminarGrupo.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnEliminarGrupo.IconSize = 16;
            this.btnEliminarGrupo.Location = new System.Drawing.Point(44, 428);
            this.btnEliminarGrupo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnEliminarGrupo.Name = "btnEliminarGrupo";
            this.btnEliminarGrupo.Size = new System.Drawing.Size(179, 41);
            this.btnEliminarGrupo.TabIndex = 68;
            this.btnEliminarGrupo.Text = "Eliminar";
            this.btnEliminarGrupo.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnEliminarGrupo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnEliminarGrupo.UseVisualStyleBackColor = false;
            // 
            // btnCancelarGrupo
            // 
            this.btnCancelarGrupo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnCancelarGrupo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancelarGrupo.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnCancelarGrupo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelarGrupo.ForeColor = System.Drawing.SystemColors.Control;
            this.btnCancelarGrupo.IconChar = FontAwesome.Sharp.IconChar.Edit;
            this.btnCancelarGrupo.IconColor = System.Drawing.Color.White;
            this.btnCancelarGrupo.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnCancelarGrupo.IconSize = 16;
            this.btnCancelarGrupo.Location = new System.Drawing.Point(43, 381);
            this.btnCancelarGrupo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnCancelarGrupo.Name = "btnCancelarGrupo";
            this.btnCancelarGrupo.Size = new System.Drawing.Size(179, 41);
            this.btnCancelarGrupo.TabIndex = 67;
            this.btnCancelarGrupo.Text = "Cancelar";
            this.btnCancelarGrupo.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancelarGrupo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancelarGrupo.UseVisualStyleBackColor = false;
            // 
            // btnGuardarGrupo
            // 
            this.btnGuardarGrupo.BackColor = System.Drawing.Color.ForestGreen;
            this.btnGuardarGrupo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGuardarGrupo.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnGuardarGrupo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGuardarGrupo.ForeColor = System.Drawing.SystemColors.Control;
            this.btnGuardarGrupo.IconChar = FontAwesome.Sharp.IconChar.FolderOpen;
            this.btnGuardarGrupo.IconColor = System.Drawing.Color.White;
            this.btnGuardarGrupo.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnGuardarGrupo.IconSize = 16;
            this.btnGuardarGrupo.Location = new System.Drawing.Point(44, 334);
            this.btnGuardarGrupo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnGuardarGrupo.Name = "btnGuardarGrupo";
            this.btnGuardarGrupo.Size = new System.Drawing.Size(179, 41);
            this.btnGuardarGrupo.TabIndex = 66;
            this.btnGuardarGrupo.Text = "Guardar";
            this.btnGuardarGrupo.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnGuardarGrupo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnGuardarGrupo.UseVisualStyleBackColor = false;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label12.Location = new System.Drawing.Point(302, 83);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(46, 16);
            this.label12.TabIndex = 77;
            this.label12.Text = "Indice:";
            this.label12.Visible = false;
            // 
            // dgvGrupos
            // 
            this.dgvGrupos.AllowUserToAddRows = false;
            this.dgvGrupos.AllowUserToDeleteRows = false;
            this.dgvGrupos.AllowUserToOrderColumns = true;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(2);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvGrupos.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvGrupos.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvGrupos.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.btnSeleccionar,
            this.Id,
            this.Descripcion,
            this.EstadoValor,
            this.Estado});
            this.dgvGrupos.Location = new System.Drawing.Point(318, 149);
            this.dgvGrupos.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dgvGrupos.MultiSelect = false;
            this.dgvGrupos.Name = "dgvGrupos";
            this.dgvGrupos.ReadOnly = true;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.ControlLightLight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.Desktop;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvGrupos.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvGrupos.RowHeadersWidth = 51;
            this.dgvGrupos.RowTemplate.Height = 24;
            this.dgvGrupos.Size = new System.Drawing.Size(683, 226);
            this.dgvGrupos.TabIndex = 70;
            // 
            // btnSeleccionar
            // 
            this.btnSeleccionar.HeaderText = "";
            this.btnSeleccionar.MinimumWidth = 6;
            this.btnSeleccionar.Name = "btnSeleccionar";
            this.btnSeleccionar.ReadOnly = true;
            this.btnSeleccionar.Width = 30;
            // 
            // Id
            // 
            this.Id.HeaderText = "ID";
            this.Id.MinimumWidth = 6;
            this.Id.Name = "Id";
            this.Id.ReadOnly = true;
            this.Id.Visible = false;
            this.Id.Width = 125;
            // 
            // Descripcion
            // 
            this.Descripcion.FillWeight = 160F;
            this.Descripcion.HeaderText = "DESCRIPCION";
            this.Descripcion.MinimumWidth = 6;
            this.Descripcion.Name = "Descripcion";
            this.Descripcion.ReadOnly = true;
            this.Descripcion.Width = 250;
            // 
            // EstadoValor
            // 
            this.EstadoValor.HeaderText = "ESTADO VALOR";
            this.EstadoValor.MinimumWidth = 6;
            this.EstadoValor.Name = "EstadoValor";
            this.EstadoValor.ReadOnly = true;
            this.EstadoValor.Visible = false;
            this.EstadoValor.Width = 125;
            // 
            // Estado
            // 
            this.Estado.HeaderText = "ESTADO";
            this.Estado.MinimumWidth = 6;
            this.Estado.Name = "Estado";
            this.Estado.ReadOnly = true;
            this.Estado.Width = 125;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(40, 25);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(150, 20);
            this.label9.TabIndex = 69;
            this.label9.Text = "Registrar Grupo:";
            // 
            // cboEstado
            // 
            this.cboEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEstado.FormattingEnabled = true;
            this.cboEstado.Location = new System.Drawing.Point(43, 139);
            this.cboEstado.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboEstado.Name = "cboEstado";
            this.cboEstado.Size = new System.Drawing.Size(177, 24);
            this.cboEstado.TabIndex = 65;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label8.Location = new System.Drawing.Point(43, 118);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 16);
            this.label8.TabIndex = 64;
            this.label8.Text = "Estado:";
            // 
            // txtNombreGrupo
            // 
            this.txtNombreGrupo.Location = new System.Drawing.Point(43, 84);
            this.txtNombreGrupo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtNombreGrupo.Name = "txtNombreGrupo";
            this.txtNombreGrupo.Size = new System.Drawing.Size(177, 22);
            this.txtNombreGrupo.TabIndex = 63;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label2.Location = new System.Drawing.Point(40, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 16);
            this.label2.TabIndex = 62;
            this.label2.Text = "Descripcion:";
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.White;
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label1.Dock = System.Windows.Forms.DockStyle.Left;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(274, 512);
            this.label1.TabIndex = 61;
            // 
            // numMinimoGrupo
            // 
            this.numMinimoGrupo.Location = new System.Drawing.Point(47, 208);
            this.numMinimoGrupo.Name = "numMinimoGrupo";
            this.numMinimoGrupo.Size = new System.Drawing.Size(120, 22);
            this.numMinimoGrupo.TabIndex = 79;
            // 
            // numMaximoGrupo
            // 
            this.numMaximoGrupo.Location = new System.Drawing.Point(47, 272);
            this.numMaximoGrupo.Name = "numMaximoGrupo";
            this.numMaximoGrupo.Size = new System.Drawing.Size(120, 22);
            this.numMaximoGrupo.TabIndex = 80;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label3.Location = new System.Drawing.Point(44, 189);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 16);
            this.label3.TabIndex = 81;
            this.label3.Text = "Minimo:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.label4.Location = new System.Drawing.Point(44, 253);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 16);
            this.label4.TabIndex = 82;
            this.label4.Text = "Maximo:";
            // 
            // frmGruposReposicion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1160, 512);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.numMaximoGrupo);
            this.Controls.Add(this.numMinimoGrupo);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.txtIndice);
            this.Controls.Add(this.txtBusqueda);
            this.Controls.Add(this.cboBuscar);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.txtId);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.btnEliminarGrupo);
            this.Controls.Add(this.btnCancelarGrupo);
            this.Controls.Add(this.btnGuardarGrupo);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.dgvGrupos);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.cboEstado);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtNombreGrupo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "frmGruposReposicion";
            this.Text = "frmGruposReposicion";
            ((System.ComponentModel.ISupportInitialize)(this.dgvGrupos)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinimoGrupo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaximoGrupo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtIndice;
        private System.Windows.Forms.TextBox txtBusqueda;
        private System.Windows.Forms.ComboBox cboBuscar;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtId;
        private System.Windows.Forms.Label label10;
        private FontAwesome.Sharp.IconButton btnEliminarGrupo;
        private FontAwesome.Sharp.IconButton btnCancelarGrupo;
        private FontAwesome.Sharp.IconButton btnGuardarGrupo;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.DataGridView dgvGrupos;
        private System.Windows.Forms.DataGridViewButtonColumn btnSeleccionar;
        private System.Windows.Forms.DataGridViewTextBoxColumn Id;
        private System.Windows.Forms.DataGridViewTextBoxColumn Descripcion;
        private System.Windows.Forms.DataGridViewTextBoxColumn EstadoValor;
        private System.Windows.Forms.DataGridViewTextBoxColumn Estado;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cboEstado;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtNombreGrupo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numMinimoGrupo;
        private System.Windows.Forms.NumericUpDown numMaximoGrupo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
    }
}