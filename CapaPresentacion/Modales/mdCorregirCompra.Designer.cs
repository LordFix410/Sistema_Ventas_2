namespace CapaPresentacion.Modales
{
    partial class mdCorregirCompra
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
            this.txtNumeroCompra = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnBuscarCompra = new FontAwesome.Sharp.IconButton();
            this.txtIdCompra = new System.Windows.Forms.TextBox();
            this.cboProveedor = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtNumeroFactura = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.dgvDetalleCompra = new System.Windows.Forms.DataGridView();
            this.IdDetalleCompra = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IdProducto = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Codigo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Producto = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CantidadActual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CostoActual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TotalActual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtMotivoCorreccion = new System.Windows.Forms.TextBox();
            this.btnGuardarCorreccion = new FontAwesome.Sharp.IconButton();
            this.btnAnularCompra = new FontAwesome.Sharp.IconButton();
            this.btnCerrar = new FontAwesome.Sharp.IconButton();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDetalleCompra)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtNumeroCompra
            // 
            this.txtNumeroCompra.Location = new System.Drawing.Point(135, 36);
            this.txtNumeroCompra.Name = "txtNumeroCompra";
            this.txtNumeroCompra.Size = new System.Drawing.Size(100, 22);
            this.txtNumeroCompra.TabIndex = 0;
            this.txtNumeroCompra.TextChanged += new System.EventHandler(this.txtNumeroCompra_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(53, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Buscar:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // btnBuscarCompra
            // 
            this.btnBuscarCompra.IconChar = FontAwesome.Sharp.IconChar.None;
            this.btnBuscarCompra.IconColor = System.Drawing.Color.Black;
            this.btnBuscarCompra.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnBuscarCompra.Location = new System.Drawing.Point(274, 33);
            this.btnBuscarCompra.Name = "btnBuscarCompra";
            this.btnBuscarCompra.Size = new System.Drawing.Size(153, 29);
            this.btnBuscarCompra.TabIndex = 2;
            this.btnBuscarCompra.Text = "Buscar Compra";
            this.btnBuscarCompra.UseVisualStyleBackColor = true;
            this.btnBuscarCompra.Click += new System.EventHandler(this.btnBuscarCompra_Click);
            // 
            // txtIdCompra
            // 
            this.txtIdCompra.Location = new System.Drawing.Point(689, 35);
            this.txtIdCompra.Name = "txtIdCompra";
            this.txtIdCompra.Size = new System.Drawing.Size(57, 22);
            this.txtIdCompra.TabIndex = 3;
            this.txtIdCompra.Visible = false;
            // 
            // cboProveedor
            // 
            this.cboProveedor.FormattingEnabled = true;
            this.cboProveedor.Location = new System.Drawing.Point(107, 33);
            this.cboProveedor.Name = "cboProveedor";
            this.cboProveedor.Size = new System.Drawing.Size(160, 24);
            this.cboProveedor.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Proveedor";
            // 
            // txtNumeroFactura
            // 
            this.txtNumeroFactura.Location = new System.Drawing.Point(467, 33);
            this.txtNumeroFactura.Name = "txtNumeroFactura";
            this.txtNumeroFactura.Size = new System.Drawing.Size(162, 22);
            this.txtNumeroFactura.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(298, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(106, 16);
            this.label3.TabIndex = 7;
            this.label3.Text = "Numero Factura:";
            // 
            // dgvDetalleCompra
            // 
            this.dgvDetalleCompra.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDetalleCompra.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.IdDetalleCompra,
            this.IdProducto,
            this.Codigo,
            this.Producto,
            this.CantidadActual,
            this.CostoActual,
            this.TotalActual});
            this.dgvDetalleCompra.Location = new System.Drawing.Point(48, 213);
            this.dgvDetalleCompra.Name = "dgvDetalleCompra";
            this.dgvDetalleCompra.RowHeadersWidth = 51;
            this.dgvDetalleCompra.RowTemplate.Height = 24;
            this.dgvDetalleCompra.Size = new System.Drawing.Size(1436, 360);
            this.dgvDetalleCompra.TabIndex = 8;
            // 
            // IdDetalleCompra
            // 
            this.IdDetalleCompra.HeaderText = "Detalle Compra";
            this.IdDetalleCompra.MinimumWidth = 6;
            this.IdDetalleCompra.Name = "IdDetalleCompra";
            this.IdDetalleCompra.Visible = false;
            this.IdDetalleCompra.Width = 125;
            // 
            // IdProducto
            // 
            this.IdProducto.HeaderText = "Id Producto";
            this.IdProducto.MinimumWidth = 6;
            this.IdProducto.Name = "IdProducto";
            this.IdProducto.Visible = false;
            this.IdProducto.Width = 125;
            // 
            // Codigo
            // 
            this.Codigo.HeaderText = "Codigo";
            this.Codigo.MinimumWidth = 6;
            this.Codigo.Name = "Codigo";
            this.Codigo.Width = 125;
            // 
            // Producto
            // 
            this.Producto.HeaderText = "Producto";
            this.Producto.MinimumWidth = 6;
            this.Producto.Name = "Producto";
            this.Producto.Width = 125;
            // 
            // CantidadActual
            // 
            this.CantidadActual.HeaderText = "Cantidad actual";
            this.CantidadActual.MinimumWidth = 6;
            this.CantidadActual.Name = "CantidadActual";
            this.CantidadActual.Width = 125;
            // 
            // CostoActual
            // 
            this.CostoActual.HeaderText = "Costo actual";
            this.CostoActual.MinimumWidth = 6;
            this.CostoActual.Name = "CostoActual";
            this.CostoActual.Width = 125;
            // 
            // TotalActual
            // 
            this.TotalActual.HeaderText = "Total actual";
            this.TotalActual.MinimumWidth = 6;
            this.TotalActual.Name = "TotalActual";
            this.TotalActual.Width = 125;
            // 
            // txtMotivoCorreccion
            // 
            this.txtMotivoCorreccion.Location = new System.Drawing.Point(310, 585);
            this.txtMotivoCorreccion.Multiline = true;
            this.txtMotivoCorreccion.Name = "txtMotivoCorreccion";
            this.txtMotivoCorreccion.Size = new System.Drawing.Size(434, 67);
            this.txtMotivoCorreccion.TabIndex = 9;
            this.txtMotivoCorreccion.TextChanged += new System.EventHandler(this.txtMotivoCorreccion_TextChanged);
            // 
            // btnGuardarCorreccion
            // 
            this.btnGuardarCorreccion.IconChar = FontAwesome.Sharp.IconChar.None;
            this.btnGuardarCorreccion.IconColor = System.Drawing.Color.Black;
            this.btnGuardarCorreccion.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnGuardarCorreccion.Location = new System.Drawing.Point(774, 607);
            this.btnGuardarCorreccion.Name = "btnGuardarCorreccion";
            this.btnGuardarCorreccion.Size = new System.Drawing.Size(127, 45);
            this.btnGuardarCorreccion.TabIndex = 10;
            this.btnGuardarCorreccion.Text = "Guardar";
            this.btnGuardarCorreccion.UseVisualStyleBackColor = true;
            this.btnGuardarCorreccion.Click += new System.EventHandler(this.btnGuardarCorreccion_Click);
            // 
            // btnAnularCompra
            // 
            this.btnAnularCompra.IconChar = FontAwesome.Sharp.IconChar.None;
            this.btnAnularCompra.IconColor = System.Drawing.Color.Black;
            this.btnAnularCompra.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnAnularCompra.Location = new System.Drawing.Point(923, 607);
            this.btnAnularCompra.Name = "btnAnularCompra";
            this.btnAnularCompra.Size = new System.Drawing.Size(127, 45);
            this.btnAnularCompra.TabIndex = 11;
            this.btnAnularCompra.Text = "Anular";
            this.btnAnularCompra.UseVisualStyleBackColor = true;
            this.btnAnularCompra.Click += new System.EventHandler(this.btnAnularCompra_Click);
            // 
            // btnCerrar
            // 
            this.btnCerrar.IconChar = FontAwesome.Sharp.IconChar.None;
            this.btnCerrar.IconColor = System.Drawing.Color.Black;
            this.btnCerrar.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnCerrar.Location = new System.Drawing.Point(1073, 607);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(127, 45);
            this.btnCerrar.TabIndex = 12;
            this.btnCerrar.Text = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = true;
            this.btnCerrar.Click += new System.EventHandler(this.btnCerrar_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(66, 607);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(135, 16);
            this.label4.TabIndex = 13;
            this.label4.Text = "Motivo de corrección:";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtIdCompra);
            this.groupBox1.Controls.Add(this.cboProveedor);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtNumeroFactura);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(48, 83);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(770, 77);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.White;
            this.label6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(0, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(1496, 667);
            this.label6.TabIndex = 79;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(45, 182);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(134, 16);
            this.label7.TabIndex = 15;
            this.label7.Text = "Detalle de la Compra";
            // 
            // mdCorregirCompra
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1496, 667);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnCerrar);
            this.Controls.Add(this.btnAnularCompra);
            this.Controls.Add(this.btnGuardarCorreccion);
            this.Controls.Add(this.txtMotivoCorreccion);
            this.Controls.Add(this.dgvDetalleCompra);
            this.Controls.Add(this.btnBuscarCompra);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtNumeroCompra);
            this.Controls.Add(this.label6);
            this.Name = "mdCorregirCompra";
            this.Text = "mdCorregirCompra";
            ((System.ComponentModel.ISupportInitialize)(this.dgvDetalleCompra)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtNumeroCompra;
        private System.Windows.Forms.Label label1;
        private FontAwesome.Sharp.IconButton btnBuscarCompra;
        private System.Windows.Forms.TextBox txtIdCompra;
        private System.Windows.Forms.ComboBox cboProveedor;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtNumeroFactura;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dgvDetalleCompra;
        private System.Windows.Forms.TextBox txtMotivoCorreccion;
        private FontAwesome.Sharp.IconButton btnGuardarCorreccion;
        private FontAwesome.Sharp.IconButton btnAnularCompra;
        private FontAwesome.Sharp.IconButton btnCerrar;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DataGridViewTextBoxColumn IdDetalleCompra;
        private System.Windows.Forms.DataGridViewTextBoxColumn IdProducto;
        private System.Windows.Forms.DataGridViewTextBoxColumn Codigo;
        private System.Windows.Forms.DataGridViewTextBoxColumn Producto;
        private System.Windows.Forms.DataGridViewTextBoxColumn CantidadActual;
        private System.Windows.Forms.DataGridViewTextBoxColumn CostoActual;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalActual;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
    }
}
