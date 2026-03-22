using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BuscaPreco.Infrastructure.Repositories;

namespace BuscaPreco.Presentation.WindowsForms
{
    public partial class RelatorioForm : Form
    {
        private readonly ConsultaRepository _consultaRepository;

        private int[] _porHora = new int[24];
        private int[] _porDia = new int[7];
        private List<(string Codigo, string Nome, int Qtd)> _topProdutos = new List<(string, string, int)>();

        public RelatorioForm(ConsultaRepository consultaRepository)
        {
            _consultaRepository = consultaRepository;
            InitializeComponent();
            ConfigurarGrid();
            dtpInicio.Value = DateTime.Today.AddDays(-6);
            dtpFim.Value = DateTime.Today;
            Load += (_, __) => CarregarDados();
        }

        private void ConfigurarGrid()
        {
            dgvProdutos.AutoGenerateColumns = false;
            dgvProdutos.Columns.Clear();
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPos",
                HeaderText = "#",
                Width = 40,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCod", HeaderText = "Código", Width = 90 });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNome",
                HeaderText = "Descrição",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            dgvProdutos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colQtd",
                HeaderText = "Consultas",
                Width = 80,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            dgvProdutos.RowHeadersVisible = false;
            dgvProdutos.AllowUserToAddRows = false;
            dgvProdutos.ReadOnly = true;
            dgvProdutos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProdutos.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
            dgvProdutos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        private void CarregarDados()
        {
            var inicio = dtpInicio.Value.Date;
            var fim = dtpFim.Value.Date;

            if (inicio > fim)
            {
                MessageBox.Show(
                    "A data inicial deve ser anterior ou igual à data final.",
                    "BuscaPreço — Relatório",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var (total, encontrados, naoCadastrados) = _consultaRepository.ResumoNoPeriodo(inicio, fim);

            _topProdutos = _consultaRepository.TopProdutos(inicio, fim);
            _porHora = _consultaRepository.ConsultasPorHora(inicio, fim);
            _porDia = _consultaRepository.ConsultasPorDiaSemana(inicio, fim);

            AtualizarUI(total, encontrados, naoCadastrados);
        }

        private void AtualizarUI(int total, int encontrados, int naoCadastrados)
        {
            lblTotal.Text = $"Total de consultas: {total:N0}";
            lblEncontrados.Text = $"Encontrados: {encontrados:N0}";
            lblNaoCadastrados.Text = $"Não cadastrados: {naoCadastrados:N0}";

            dgvProdutos.Rows.Clear();
            int pos = 1;
            foreach (var (codigo, nome, qtd) in _topProdutos)
            {
                dgvProdutos.Rows.Add(pos++, codigo, nome, qtd.ToString("N0"));
            }

            pnlHorario.Invalidate();
            pnlDias.Invalidate();
        }

        private void pnlHorario_Paint(object sender, PaintEventArgs e)
        {
            DesenharBarras(
                e.Graphics,
                pnlHorario.ClientSize,
                _porHora,
                Enumerable.Range(0, 24).Select(h => $"{h:D2}h").ToArray(),
                "Consultas por hora do dia",
                Color.FromArgb(70, 130, 180));
        }

        private void pnlDias_Paint(object sender, PaintEventArgs e)
        {
            DesenharBarras(
                e.Graphics,
                pnlDias.ClientSize,
                _porDia,
                new[] { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" },
                "Consultas por dia da semana",
                Color.FromArgb(60, 160, 100));
        }

        private static void DesenharBarras(Graphics g, Size canvas, int[] valores, string[] labels, string titulo, Color corBarra)
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            const int mLeft = 55, mRight = 20, mTop = 40, mBottom = 45;
            int plotW = canvas.Width - mLeft - mRight;
            int plotH = canvas.Height - mTop - mBottom;
            if (plotW <= 0 || plotH <= 0 || valores.Length == 0)
            {
                return;
            }

            int maxVal = valores.Max() == 0 ? 1 : valores.Max();

            using var fTitulo = new Font("Segoe UI", 10f, FontStyle.Bold);
            var sz = g.MeasureString(titulo, fTitulo);
            g.DrawString(titulo, fTitulo, Brushes.Black, mLeft + (plotW - sz.Width) / 2f, 6f);

            using var fEixo = new Font("Segoe UI", 7.5f);
            using var penGrade = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dash };
            using var sfRight = new StringFormat { Alignment = StringAlignment.Far };
            using var sfCenter = new StringFormat { Alignment = StringAlignment.Center };

            for (int i = 0; i <= 5; i++)
            {
                int yVal = maxVal * i / 5;
                float yPx = mTop + plotH - plotH * i / 5f;
                g.DrawLine(penGrade, mLeft, yPx, mLeft + plotW, yPx);
                g.DrawString(yVal.ToString("N0"), fEixo, Brushes.Gray, new RectangleF(0, yPx - 8, mLeft - 4, 16), sfRight);
            }

            using var penEixo = new Pen(Color.DimGray, 1.5f);
            g.DrawLine(penEixo, mLeft, mTop, mLeft, mTop + plotH);
            g.DrawLine(penEixo, mLeft, mTop + plotH, mLeft + plotW, mTop + plotH);

            float barW = (float)plotW / valores.Length;
            float padX = barW * 0.15f;
            using var brBarra = new SolidBrush(corBarra);
            using var brVal = new SolidBrush(Color.FromArgb(50, 50, 50));
            using var brSombra = new SolidBrush(Color.FromArgb(25, 0, 0, 0));

            for (int i = 0; i < valores.Length; i++)
            {
                float barH = plotH * (float)valores[i] / maxVal;
                float xLeft = mLeft + i * barW + padX;
                float yTop = mTop + plotH - barH;
                float bw = Math.Max(1f, barW - 2 * padX);

                g.FillRectangle(brSombra, xLeft + 2, yTop + 2, bw, barH);
                g.FillRectangle(brBarra, xLeft, yTop, bw, barH);

                if (valores[i] > 0 && barH > 14)
                {
                    string vs = valores[i].ToString("N0");
                    var vsz = g.MeasureString(vs, fEixo);
                    if (vsz.Width < bw + 4)
                    {
                        g.DrawString(vs, fEixo, brVal, new RectangleF(xLeft, yTop - 14, bw, 14), sfCenter);
                    }
                }

                float labelX = mLeft + i * barW + barW / 2f;
                g.DrawString(labels[i], fEixo, Brushes.DimGray, new RectangleF(labelX - barW / 2f, mTop + plotH + 4, barW, 20), sfCenter);
            }
        }

        private void btnAtualizar_Click(object sender, EventArgs e)
        {
            CarregarDados();
        }
    }
}
