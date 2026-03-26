namespace BuscaPreco.Presentation.WindowsForms
{
    partial class RelatorioForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.grpResumo = new System.Windows.Forms.GroupBox();
            this.lblTotal = new System.Windows.Forms.Label();
            this.lblEncontrados = new System.Windows.Forms.Label();
            this.lblNaoCadastrados = new System.Windows.Forms.Label();
            this.pnlFiltro = new System.Windows.Forms.Panel();
            this.lblInicio = new System.Windows.Forms.Label();
            this.dtpInicio = new System.Windows.Forms.DateTimePicker();
            this.lblFim = new System.Windows.Forms.Label();
            this.dtpFim = new System.Windows.Forms.DateTimePicker();
            this.btnAtualizar = new System.Windows.Forms.Button();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabProdutos = new System.Windows.Forms.TabPage();
            this.dgvProdutos = new System.Windows.Forms.DataGridView();
            this.tabHorario = new System.Windows.Forms.TabPage();
            this.pnlHorario = new System.Windows.Forms.Panel();
            this.tabDias = new System.Windows.Forms.TabPage();
            this.pnlDias = new System.Windows.Forms.Panel();

            this.grpResumo.Text = "Resumo do período";
            this.grpResumo.Location = new System.Drawing.Point(8, 50);
            this.grpResumo.Size = new System.Drawing.Size(780, 60);
            this.grpResumo.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;

            this.lblTotal.AutoSize = true;
            this.lblTotal.Location = new System.Drawing.Point(12, 24);
            this.lblTotal.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);

            this.lblEncontrados.AutoSize = true;
            this.lblEncontrados.Location = new System.Drawing.Point(240, 24);
            this.lblEncontrados.Font = new System.Drawing.Font("Segoe UI", 10f);
            this.lblEncontrados.ForeColor = System.Drawing.Color.DarkGreen;

            this.lblNaoCadastrados.AutoSize = true;
            this.lblNaoCadastrados.Location = new System.Drawing.Point(460, 24);
            this.lblNaoCadastrados.Font = new System.Drawing.Font("Segoe UI", 10f);
            this.lblNaoCadastrados.ForeColor = System.Drawing.Color.Crimson;

            this.grpResumo.Controls.Add(this.lblTotal);
            this.grpResumo.Controls.Add(this.lblEncontrados);
            this.grpResumo.Controls.Add(this.lblNaoCadastrados);

            this.pnlFiltro.Location = new System.Drawing.Point(8, 8);
            this.pnlFiltro.Size = new System.Drawing.Size(780, 36);
            this.pnlFiltro.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;

            this.lblInicio.Text = "De:";
            this.lblInicio.AutoSize = true;
            this.lblInicio.Location = new System.Drawing.Point(0, 8);

            this.dtpInicio.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpInicio.Location = new System.Drawing.Point(28, 4);
            this.dtpInicio.Size = new System.Drawing.Size(110, 22);

            this.lblFim.Text = "Até:";
            this.lblFim.AutoSize = true;
            this.lblFim.Location = new System.Drawing.Point(148, 8);

            this.dtpFim.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFim.Location = new System.Drawing.Point(178, 4);
            this.dtpFim.Size = new System.Drawing.Size(110, 22);

            this.btnAtualizar.Text = "Atualizar";
            this.btnAtualizar.Location = new System.Drawing.Point(298, 2);
            this.btnAtualizar.Size = new System.Drawing.Size(90, 26);
            this.btnAtualizar.Click += new System.EventHandler(this.btnAtualizar_Click);

            this.pnlFiltro.Controls.Add(this.lblInicio);
            this.pnlFiltro.Controls.Add(this.dtpInicio);
            this.pnlFiltro.Controls.Add(this.lblFim);
            this.pnlFiltro.Controls.Add(this.dtpFim);
            this.pnlFiltro.Controls.Add(this.btnAtualizar);

            this.tabMain.Location = new System.Drawing.Point(8, 116);
            this.tabMain.Size = new System.Drawing.Size(780, 460);
            this.tabMain.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;

            this.tabProdutos.Text = "Top Produtos";
            this.dgvProdutos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabProdutos.Controls.Add(this.dgvProdutos);

            this.tabHorario.Text = "Por Horário";
            this.pnlHorario.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHorario.BackColor = System.Drawing.Color.White;
            this.pnlHorario.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlHorario_Paint);
            this.tabHorario.Controls.Add(this.pnlHorario);

            this.tabDias.Text = "Por Dia da Semana";
            this.pnlDias.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDias.BackColor = System.Drawing.Color.White;
            this.pnlDias.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlDias_Paint);
            this.tabDias.Controls.Add(this.pnlDias);

            this.tabMain.Controls.Add(this.tabProdutos);
            this.tabMain.Controls.Add(this.tabHorario);
            this.tabMain.Controls.Add(this.tabDias);

            this.ClientSize = new System.Drawing.Size(800, 600);
            this.MinimumSize = new System.Drawing.Size(700, 500);
            this.Text = "BuscaPreço — Relatório de Consultas";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Controls.Add(this.pnlFiltro);
            this.Controls.Add(this.grpResumo);
            this.Controls.Add(this.tabMain);
        }

        private System.Windows.Forms.GroupBox grpResumo;
        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.Label lblEncontrados;
        private System.Windows.Forms.Label lblNaoCadastrados;
        private System.Windows.Forms.Panel pnlFiltro;
        private System.Windows.Forms.Label lblInicio;
        private System.Windows.Forms.DateTimePicker dtpInicio;
        private System.Windows.Forms.Label lblFim;
        private System.Windows.Forms.DateTimePicker dtpFim;
        private System.Windows.Forms.Button btnAtualizar;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabProdutos;
        private System.Windows.Forms.DataGridView dgvProdutos;
        private System.Windows.Forms.TabPage tabHorario;
        private System.Windows.Forms.Panel pnlHorario;
        private System.Windows.Forms.TabPage tabDias;
        private System.Windows.Forms.Panel pnlDias;
    }
}
