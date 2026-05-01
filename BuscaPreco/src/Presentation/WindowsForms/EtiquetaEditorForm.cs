using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Infrastructure.Services;

namespace BuscaPreco.Presentation.WindowsForms;

/// <summary>
/// Visual drag-and-drop label layout editor.
/// Canvas scale: 4 px/mm for screen preview; print uses DotsPerMm (~8 px/mm at 203 DPI).
/// </summary>
public class EtiquetaEditorForm : Form
{
    // ── constants ──────────────────────────────────────────────────────────
    private const float PxPerMm    = 4f;    // screen preview scale
    private const int   HandleSize = 8;     // resize handle side (px)

    // ── services ───────────────────────────────────────────────────────────
    private readonly IEtiquetaLayoutRepository _repo;
    private readonly LabelLayoutRenderer       _renderer;

    // ── state ──────────────────────────────────────────────────────────────
    private EtiquetaLayout        _layout  = null!;
    private EtiquetaComponente?   _selected;
    private bool                  _dragging;
    private bool                  _resizing;
    private PointF                _dragOffset;       // mm
    private PointF                _resizeOriginMm;   // component top-left at resize start
    private SizeF                 _resizeSizeOrigin; // component size at resize start
    private Point                 _mouseDownPx;

    // ── UI controls ────────────────────────────────────────────────────────
    private Panel          _canvas      = null!;
    private ToolStrip      _toolStrip   = null!;
    private Panel          _propPanel   = null!;

    // property panel controls
    private NumericUpDown  _numX        = null!;
    private NumericUpDown  _numY        = null!;
    private NumericUpDown  _numW        = null!;
    private NumericUpDown  _numH        = null!;
    private NumericUpDown  _numFontSize = null!;
    private ComboBox       _cmbFont     = null!;
    private CheckBox       _chkBold     = null!;
    private CheckBox       _chkItalic   = null!;
    private CheckBox       _chkUnder    = null!;
    private CheckBox       _chkVisible  = null!;
    private ComboBox       _cmbAlign    = null!;
    private Button         _btnColor    = null!;
    private Panel          _pnlColor    = null!;
    private TextBox        _txtContent  = null!;
    private Button         _btnDelete   = null!;

    private bool _suppressPropEvents;

    // ── sample variables for preview ───────────────────────────────────────
    private static readonly Dictionary<string, string> PreviewVars =
        LabelVariableResolver.CriarVariaveis(
            "PRODUTO DEMO LONGO 123",
            "R$ 19,90",
            "7891000315507",
            "UN",
            "MINHA EMPRESA");

    public EtiquetaEditorForm(IEtiquetaLayoutRepository repo, LabelLayoutRenderer renderer)
    {
        _repo     = repo;
        _renderer = renderer;
        InicializarComponentes();
        CarregarLayout();
    }

    // ── form build ─────────────────────────────────────────────────────────
    private void InicializarComponentes()
    {
        Text            = "Editor de Etiqueta";
        Size            = new Size(1000, 620);
        MinimumSize     = new Size(800, 520);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildToolStrip();
        BuildPropertiesPanel();
        BuildCanvas();

        Controls.Add(_canvas);
        Controls.Add(_propPanel);
        Controls.Add(_toolStrip);
    }

    private void BuildToolStrip()
    {
        _toolStrip = new ToolStrip { Dock = DockStyle.Top };

        ToolStripButton Btn(string text, string tip, EventHandler handler)
        {
            var b = new ToolStripButton(text) { ToolTipText = tip, DisplayStyle = ToolStripItemDisplayStyle.Text };
            b.Click += handler;
            return b;
        }

        _toolStrip.Items.Add(Btn("Novo Texto",    "Adicionar texto fixo",     (_, _) => AdicionarComponente(TipoComponente.TextoFixo)));
        _toolStrip.Items.Add(Btn("Nova Variável", "Adicionar variável",       (_, _) => AdicionarComponente(TipoComponente.Variavel)));
        _toolStrip.Items.Add(Btn("Novo Cód.Barras","Adicionar código de barras",(_, _) => AdicionarComponente(TipoComponente.CodigoBarras)));
        _toolStrip.Items.Add(Btn("Novo QR Code",  "Adicionar QR Code",        (_, _) => AdicionarComponente(TipoComponente.QrCode)));
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(Btn("Salvar",        "Salvar layout",            BtnSalvar_Click));
        _toolStrip.Items.Add(Btn("Carregar",      "Carregar layout",          BtnCarregar_Click));
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(Btn("Pré-visualizar","Pré-visualização de impressão", BtnPreview_Click));
    }

    private void BuildPropertiesPanel()
    {
        _propPanel = new Panel
        {
            Dock      = DockStyle.Right,
            Width     = 200,
            AutoScroll = true,
            Padding   = new Padding(6),
            BorderStyle = BorderStyle.FixedSingle
        };

        var y = 8;

        Label Lbl(string text)
        {
            var l = new Label { Text = text, Left = 4, Top = y, Width = 188, AutoSize = false };
            y += 16;
            return l;
        }

        NumericUpDown Num(int min, int max, decimal inc = 1)
        {
            var n = new NumericUpDown
            {
                Left      = 4, Top = y, Width = 188,
                Minimum   = min, Maximum = max,
                DecimalPlaces = inc < 1 ? 1 : 0,
                Increment = inc
            };
            y += 26;
            return n;
        }

        _propPanel.Controls.Add(Lbl("Conteúdo / Fórmula:"));
        _txtContent = new TextBox { Left = 4, Top = y, Width = 188, Height = 50, Multiline = true };
        _txtContent.TextChanged += PropChanged;
        y += 56;
        _propPanel.Controls.Add(_txtContent);

        _propPanel.Controls.Add(Lbl("X (mm):"));
        _numX = Num(0, 500, 0.5m);
        _propPanel.Controls.Add(_numX);

        _propPanel.Controls.Add(Lbl("Y (mm):"));
        _numY = Num(0, 500, 0.5m);
        _propPanel.Controls.Add(_numY);

        _propPanel.Controls.Add(Lbl("Largura (mm):"));
        _numW = Num(1, 500, 0.5m);
        _propPanel.Controls.Add(_numW);

        _propPanel.Controls.Add(Lbl("Altura (mm):"));
        _numH = Num(1, 500, 0.5m);
        _propPanel.Controls.Add(_numH);

        _propPanel.Controls.Add(Lbl("Fonte:"));
        _cmbFont = new ComboBox { Left = 4, Top = y, Width = 188, DropDownStyle = ComboBoxStyle.DropDown };
        _cmbFont.Items.AddRange(new[] { "Arial", "Times New Roman", "Courier New", "Verdana", "Tahoma", "Calibri" });
        _cmbFont.TextChanged += PropChanged;
        y += 26;
        _propPanel.Controls.Add(_cmbFont);

        _propPanel.Controls.Add(Lbl("Tamanho fonte (pt):"));
        _numFontSize = Num(4, 200, 1);
        _propPanel.Controls.Add(_numFontSize);

        _chkBold    = new CheckBox { Text = "Negrito",    Left = 4, Top = y, Width = 90 };  y += 22;
        _chkItalic  = new CheckBox { Text = "Itálico",    Left = 4, Top = y, Width = 90 };  y += 22;
        _chkUnder   = new CheckBox { Text = "Sublinhado", Left = 4, Top = y, Width = 90 };  y += 22;
        _chkVisible = new CheckBox { Text = "Visível",    Left = 4, Top = y, Width = 90 };  y += 28;
        foreach (var chk in new[] { _chkBold, _chkItalic, _chkUnder, _chkVisible })
        {
            chk.CheckedChanged += PropChanged;
            _propPanel.Controls.Add(chk);
        }

        _propPanel.Controls.Add(Lbl("Alinhamento:"));
        _cmbAlign = new ComboBox { Left = 4, Top = y, Width = 188, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbAlign.Items.AddRange(new object[] { "Esquerda", "Centro", "Direita" });
        _cmbAlign.SelectedIndexChanged += PropChanged;
        y += 26;
        _propPanel.Controls.Add(_cmbAlign);

        _propPanel.Controls.Add(Lbl("Cor do texto:"));
        _pnlColor = new Panel { Left = 4, Top = y, Width = 30, Height = 22, BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle };
        _btnColor  = new Button { Text = "...", Left = 40, Top = y, Width = 40, Height = 22 };
        _btnColor.Click += BtnColor_Click;
        y += 28;
        _propPanel.Controls.Add(_pnlColor);
        _propPanel.Controls.Add(_btnColor);

        _btnDelete = new Button { Text = "Excluir componente", Left = 4, Top = y + 10, Width = 188, Height = 28, ForeColor = Color.DarkRed };
        _btnDelete.Click += BtnDelete_Click;
        _propPanel.Controls.Add(_btnDelete);

        // wire numeric changes
        foreach (var num in new[] { _numX, _numY, _numW, _numH, _numFontSize })
            num.ValueChanged += PropChanged;

        AtualizarPropPanel();
    }

    private void BuildCanvas()
    {
        _canvas = new Panel
        {
            Dock        = DockStyle.Fill,
            BackColor   = Color.LightGray,
            AutoScroll  = true
        };
        _canvas.Paint        += Canvas_Paint;
        _canvas.MouseDown    += Canvas_MouseDown;
        _canvas.MouseMove    += Canvas_MouseMove;
        _canvas.MouseUp      += Canvas_MouseUp;
        _canvas.MouseClick   += Canvas_MouseClick;
        _canvas.Resize       += (_, _) => _canvas.Invalidate();
    }

    // ── layout load/save ──────────────────────────────────────────────────
    private void CarregarLayout()
    {
        _layout   = _repo.ObterOuCriarPadrao();
        _selected = null;
        AtualizarCanvasSize();
        AtualizarPropPanel();
        _canvas.Invalidate();
    }

    private void AtualizarCanvasSize()
    {
        int w = Math.Max(10, (int)(_layout.LarguraMm * PxPerMm)) + 40;
        int h = Math.Max(10, (int)(_layout.AlturaMm  * PxPerMm)) + 40;
        _canvas.AutoScrollMinSize = new Size(w, h);
    }

    // ── painting ──────────────────────────────────────────────────────────
    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // label background
        var origin = LabelOrigin();
        int lw = (int)(_layout.LarguraMm * PxPerMm);
        int lh = (int)(_layout.AlturaMm  * PxPerMm);
        var labelRect = new Rectangle(origin.X, origin.Y, lw, lh);

        g.FillRectangle(Brushes.White, labelRect);
        g.DrawRectangle(Pens.DarkGray, labelRect);

        // components
        foreach (var comp in _layout.Componentes)
        {
            var r = ComponenteRectPx(comp, origin);
            var isSelected = ReferenceEquals(comp, _selected);

            if (!comp.Visivel)
            {
                using var dashPen = new Pen(Color.Gray) { DashStyle = DashStyle.Dash };
                g.DrawRectangle(dashPen, r.X, r.Y, r.Width, r.Height);
                continue;
            }

            // draw component preview using renderer at 4 px/mm
            try
            {
                var singleLayout = new EtiquetaLayout
                {
                    LarguraMm  = comp.LarguraMm,
                    AlturaMm   = comp.AlturaMm,
                    Componentes = { comp }
                };
                using var bmp = _renderer.Renderizar(singleLayout, PreviewVars, PxPerMm);
                g.DrawImage(bmp, r.Location);
            }
            catch
            {
                g.FillRectangle(Brushes.LightYellow, r);
            }

            // selection border
            if (isSelected)
            {
                using var selPen = new Pen(Color.DodgerBlue, 2f);
                g.DrawRectangle(selPen, r.X, r.Y, r.Width, r.Height);

                // resize handle (SE corner)
                var handle = ResizeHandleRect(comp, origin);
                g.FillRectangle(Brushes.DodgerBlue, handle);
            }
            else
            {
                using var borderPen = new Pen(Color.Silver);
                g.DrawRectangle(borderPen, r.X, r.Y, r.Width, r.Height);
            }
        }
    }

    // ── mouse interaction ─────────────────────────────────────────────────
    private void Canvas_MouseClick(object? sender, MouseEventArgs e)
    {
        if (_dragging || _resizing) return;

        var origin = LabelOrigin();
        _selected  = null;

        foreach (var comp in Enumerable.Reverse(_layout.Componentes))
        {
            if (ComponenteRectPx(comp, origin).Contains(e.Location))
            {
                _selected = comp;
                break;
            }
        }

        AtualizarPropPanel();
        _canvas.Invalidate();
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _mouseDownPx = e.Location;

        var origin = LabelOrigin();

        if (_selected is not null)
        {
            // check resize handle first
            if (ResizeHandleRect(_selected, origin).Contains(e.Location))
            {
                _resizing          = true;
                _resizeOriginMm    = new PointF(_selected.XMm, _selected.YMm);
                _resizeSizeOrigin  = new SizeF(_selected.LarguraMm, _selected.AlturaMm);
                return;
            }

            if (ComponenteRectPx(_selected, origin).Contains(e.Location))
            {
                _dragging   = true;
                _dragOffset = new PointF(
                    _selected.XMm - PxToMm(e.X - origin.X),
                    _selected.YMm - PxToMm(e.Y - origin.Y));
                return;
            }
        }

        // try to select a different component
        Canvas_MouseClick(sender, e);
        if (_selected is not null && ComponenteRectPx(_selected, origin).Contains(e.Location))
        {
            _dragging   = true;
            _dragOffset = new PointF(
                _selected.XMm - PxToMm(e.X - origin.X),
                _selected.YMm - PxToMm(e.Y - origin.Y));
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_selected is null) return;

        var origin = LabelOrigin();

        if (_resizing)
        {
            float dxMm = PxToMm(e.X - _mouseDownPx.X);
            float dyMm = PxToMm(e.Y - _mouseDownPx.Y);
            _selected.LarguraMm = Math.Max(5f, _resizeSizeOrigin.Width  + dxMm);
            _selected.AlturaMm  = Math.Max(3f, _resizeSizeOrigin.Height + dyMm);
            _canvas.Invalidate();
            SuppressedPropRefresh();
        }
        else if (_dragging)
        {
            float mmX = PxToMm(e.X - origin.X) + _dragOffset.X;
            float mmY = PxToMm(e.Y - origin.Y) + _dragOffset.Y;
            _selected.XMm = Math.Max(0f, Math.Min(mmX, _layout.LarguraMm - _selected.LarguraMm));
            _selected.YMm = Math.Max(0f, Math.Min(mmY, _layout.AlturaMm  - _selected.AlturaMm));
            _canvas.Invalidate();
            SuppressedPropRefresh();
        }
        else
        {
            // cursor hint
            if (_selected is not null && ResizeHandleRect(_selected, origin).Contains(e.Location))
                _canvas.Cursor = Cursors.SizeNWSE;
            else if (_selected is not null && ComponenteRectPx(_selected, origin).Contains(e.Location))
                _canvas.Cursor = Cursors.SizeAll;
            else
                _canvas.Cursor = Cursors.Default;
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        _dragging = _resizing = false;
        AtualizarPropPanel();
    }

    // ── property panel ────────────────────────────────────────────────────
    private void AtualizarPropPanel()
    {
        _suppressPropEvents = true;
        var sel = _selected;
        var hasComp = sel is not null;

        _txtContent.Enabled  =
        _numX.Enabled        =
        _numY.Enabled        =
        _numW.Enabled        =
        _numH.Enabled        =
        _numFontSize.Enabled =
        _cmbFont.Enabled     =
        _chkBold.Enabled     =
        _chkItalic.Enabled   =
        _chkUnder.Enabled    =
        _chkVisible.Enabled  =
        _cmbAlign.Enabled    =
        _btnColor.Enabled    =
        _btnDelete.Enabled   = hasComp;

        if (hasComp)
        {
            _txtContent.Text      = sel!.Conteudo;
            _numX.Value           = (decimal)Math.Round(sel.XMm, 1);
            _numY.Value           = (decimal)Math.Round(sel.YMm, 1);
            _numW.Value           = (decimal)Math.Round(sel.LarguraMm, 1);
            _numH.Value           = (decimal)Math.Round(sel.AlturaMm, 1);
            _numFontSize.Value    = (decimal)Math.Round(sel.FonteSize, 0);
            _cmbFont.Text         = sel.FonteFamily;
            _chkBold.Checked      = sel.Negrito;
            _chkItalic.Checked    = sel.Italico;
            _chkUnder.Checked     = sel.Sublinhado;
            _chkVisible.Checked   = sel.Visivel;
            _cmbAlign.SelectedIndex = sel.Alinhamento switch
            {
                AlinhamentoTexto.Centro  => 1,
                AlinhamentoTexto.Direita => 2,
                _                        => 0
            };
            try   { _pnlColor.BackColor = ColorTranslator.FromHtml(sel.Cor); }
            catch { _pnlColor.BackColor = Color.Black; }
        }

        _suppressPropEvents = false;
    }

    private void SuppressedPropRefresh()
    {
        _suppressPropEvents = true;
        if (_selected is not null)
        {
            _numX.Value = (decimal)Math.Round(_selected.XMm, 1);
            _numY.Value = (decimal)Math.Round(_selected.YMm, 1);
            _numW.Value = (decimal)Math.Round(_selected.LarguraMm, 1);
            _numH.Value = (decimal)Math.Round(_selected.AlturaMm, 1);
        }
        _suppressPropEvents = false;
    }

    private void PropChanged(object? sender, EventArgs e)
    {
        if (_suppressPropEvents || _selected is null) return;

        _selected.Conteudo    = _txtContent.Text;
        _selected.XMm         = (float)_numX.Value;
        _selected.YMm         = (float)_numY.Value;
        _selected.LarguraMm   = (float)_numW.Value;
        _selected.AlturaMm    = (float)_numH.Value;
        _selected.FonteSize   = (float)_numFontSize.Value;
        _selected.FonteFamily = string.IsNullOrWhiteSpace(_cmbFont.Text) ? "Arial" : _cmbFont.Text;
        _selected.Negrito     = _chkBold.Checked;
        _selected.Italico     = _chkItalic.Checked;
        _selected.Sublinhado  = _chkUnder.Checked;
        _selected.Visivel     = _chkVisible.Checked;
        _selected.Alinhamento = _cmbAlign.SelectedIndex switch
        {
            1 => AlinhamentoTexto.Centro,
            2 => AlinhamentoTexto.Direita,
            _ => AlinhamentoTexto.Esquerda
        };

        _canvas.Invalidate();
    }

    // ── button handlers ───────────────────────────────────────────────────
    private void BtnColor_Click(object? sender, EventArgs e)
    {
        if (_selected is null) return;
        using var dlg = new ColorDialog { Color = _pnlColor.BackColor };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _selected.Cor        = ColorTranslator.ToHtml(dlg.Color);
        _pnlColor.BackColor  = dlg.Color;
        _canvas.Invalidate();
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_selected is null) return;
        _layout.Componentes.Remove(_selected);
        _selected = null;
        AtualizarPropPanel();
        _canvas.Invalidate();
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        try
        {
            _repo.Salvar(_layout);
            MessageBox.Show("Layout salvo com sucesso.", "Salvo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnCarregar_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Descartar alterações e carregar layout salvo?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        CarregarLayout();
    }

    private void BtnPreview_Click(object? sender, EventArgs e)
    {
        try
        {
            using var bmp = _renderer.RenderizarParaImpressao(_layout, PreviewVars);
            using var frm = new Form
            {
                Text            = "Pré-visualização de Impressão (203 DPI)",
                ClientSize      = new Size(bmp.Width, bmp.Height),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                StartPosition   = FormStartPosition.CenterParent
            };
            var pb = new PictureBox
            {
                Image    = bmp,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock     = DockStyle.Fill
            };
            frm.Controls.Add(pb);
            frm.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro na pré-visualização: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── add component ─────────────────────────────────────────────────────
    private void AdicionarComponente(TipoComponente tipo)
    {
        string conteudo = tipo switch
        {
            TipoComponente.Variavel     => "{DescricaoProduto}",
            TipoComponente.CodigoBarras => "{CodigoBarras}",
            TipoComponente.QrCode       => "www.google.com.br/{DescricaoProduto}",
            _                           => "Texto"
        };

        var comp = new EtiquetaComponente
        {
            Tipo      = tipo,
            Conteudo  = conteudo,
            XMm       = 2f,
            YMm       = 2f,
            LarguraMm = tipo == TipoComponente.CodigoBarras || tipo == TipoComponente.QrCode ? 30f : 50f,
            AlturaMm  = tipo == TipoComponente.CodigoBarras ? 15f :
                        tipo == TipoComponente.QrCode       ? 25f : 8f,
            FonteSize = 10f
        };

        _layout.Componentes.Add(comp);
        _selected = comp;
        AtualizarPropPanel();
        _canvas.Invalidate();
    }

    // ── helpers ───────────────────────────────────────────────────────────
    private Point LabelOrigin() => new(
        _canvas.AutoScrollPosition.X + 20,
        _canvas.AutoScrollPosition.Y + 20);

    private Rectangle ComponenteRectPx(EtiquetaComponente comp, Point origin) =>
        new(
            origin.X + (int)(comp.XMm       * PxPerMm),
            origin.Y + (int)(comp.YMm       * PxPerMm),
            Math.Max(1, (int)(comp.LarguraMm * PxPerMm)),
            Math.Max(1, (int)(comp.AlturaMm  * PxPerMm)));

    private Rectangle ResizeHandleRect(EtiquetaComponente comp, Point origin)
    {
        var r = ComponenteRectPx(comp, origin);
        return new Rectangle(r.Right - HandleSize, r.Bottom - HandleSize, HandleSize, HandleSize);
    }

    private static float PxToMm(float px) => px / PxPerMm;
}
