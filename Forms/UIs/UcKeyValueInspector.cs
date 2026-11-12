using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// UI-UX-07：结构化键值对比行模型（属性名 | 当前基准值 | 选中项或变更差值 ±Δ）
    /// </summary>
    public class InspectorRowData
    {
        public string Label { get; set; } = "";
        public string CurrentValue { get; set; } = "";
        public string DeltaOrTargetValue { get; set; } = "";
        public int DeltaSign { get; set; } = 0; // >0 正向增益，<0 负向折损，0 中性信息

        public InspectorRowData() { }

        public InspectorRowData(string label, string currentVal, string deltaOrTarget, int deltaSign = 0)
        {
            Label = label ?? "";
            CurrentValue = currentVal ?? "";
            DeltaOrTargetValue = deltaOrTarget ?? "";
            DeltaSign = deltaSign;
        }
    }

    /// <summary>
    /// UI-UX-07：双列并排三栏对齐键值差值检查器（纯代码 GDI+ 双缓冲绘制，零闪烁，严格适配纯灰度办公皮肤）
    /// </summary>
    public class UcKeyValueInspector : Panel, IThemeableView
    {
        private string _titleLine = "";
        private string _subNoteLine = "";
        private string _fallbackPlainText = "";
        private List<InspectorRowData> _rows = new List<InspectorRowData>();

        public UcKeyValueInspector()
        {
            this.DoubleBuffered = true;
            this.BorderStyle = BorderStyle.None;
            ApplyTheme();
        }

        public void SetContent(string titleLine, string subNoteLine, List<InspectorRowData> rows, string fallbackPlainText)
        {
            _titleLine = titleLine ?? "";
            _subNoteLine = subNoteLine ?? "";
            _rows = rows ?? new List<InspectorRowData>();
            _fallbackPlainText = fallbackPlainText ?? "";
            Invalidate();
        }

        public void ApplyTheme()
        {
            this.BackColor = UiStyleHelper.Current.BgFooter;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            var rect = this.ClientRectangle;
            if (rect.Width <= 6 || rect.Height <= 6) return;

            var p = UiStyleHelper.Current;
            using (var bgBrush = new SolidBrush(p.BgFooter))
            using (var borderPen = new Pen(p.BorderStrong))
            {
                g.FillRectangle(bgBrush, rect);
                g.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1);
            }

            if (_rows == null || _rows.Count == 0)
            {
                var txtRect = new Rectangle(8, 5, rect.Width - 16, rect.Height - 10);
                TextRenderer.DrawText(g,
                    string.IsNullOrEmpty(_fallbackPlainText) ? "单击上方任意格子查看结构化属性与替换差值对比..." : _fallbackPlainText,
                    UiStyleHelper.NormalFont,
                    txtRect,
                    p.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }

            int y = 4;
            int headerH = 21;
            var titleRect = new Rectangle(8, y, rect.Width - 16, headerH);
            TextRenderer.DrawText(g, _titleLine, UiStyleHelper.BoldFont, titleRect,
                p.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            y += headerH;
            using (var divPen = new Pen(p.BorderSubtle))
            {
                g.DrawLine(divPen, 6, y, rect.Width - 6, y);
            }
            y += 3;

            int bottomReserve = string.IsNullOrEmpty(_subNoteLine) ? 4 : 20;
            int availH = Math.Max(18, rect.Height - y - bottomReserve);
            bool twoColGrid = (rect.Width >= 420 && _rows.Count >= 3);
            int colCount = twoColGrid ? 2 : 1;
            int rowCount = (int)Math.Ceiling(_rows.Count / (double)colCount);
            int rowH = Math.Max(17, Math.Min(21, availH / Math.Max(1, rowCount)));

            int colW = (rect.Width - 16) / colCount;
            using (var cellSepPen = new Pen(p.BorderSubtle))
            {
                if (twoColGrid)
                {
                    int midX = 8 + colW;
                    g.DrawLine(cellSepPen, midX - 4, y, midX - 4, Math.Min(rect.Height - bottomReserve, y + rowCount * rowH));
                }

                for (int i = 0; i < _rows.Count; i++)
                {
                    int r = i / colCount;
                    int c = i % colCount;
                    int cellY = y + r * rowH;
                    if (cellY + 13 > rect.Height - bottomReserve + 2) break;

                    int cellX = 8 + c * colW;
                    int innerW = colW - (twoColGrid ? 10 : 2);
                    var item = _rows[i];

                    int lblW = Math.Max(64, (int)(innerW * 0.34));
                    int curW = string.IsNullOrEmpty(item.CurrentValue) ? 0 : (int)(innerW * 0.28);
                    int valW = innerW - lblW - curW;

                    var rLbl = new Rectangle(cellX, cellY, lblW, rowH);
                    TextRenderer.DrawText(g, item.Label, UiStyleHelper.SmallFont, rLbl,
                        p.TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                    if (curW > 0)
                    {
                        var rCur = new Rectangle(cellX + lblW, cellY, curW, rowH);
                        TextRenderer.DrawText(g, item.CurrentValue, UiStyleHelper.SmallMonoReg, rCur,
                            p.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    }

                    var rDelta = new Rectangle(cellX + lblW + curW, cellY, valW, rowH);
                    Color deltaFg;
                    Font deltaFont = UiStyleHelper.SmallMonoBold;

                    if (p.IsGrayscale)
                    {
                        // 纯灰度皮肤下靠明度层级与字重区分正负，不使用任何彩色
                        if (item.DeltaSign > 0)
                        {
                            deltaFg = p.AccentBlue; // 最清晰高对比灰阶
                            deltaFont = UiStyleHelper.SmallMonoBold;
                        }
                        else if (item.DeltaSign < 0)
                        {
                            deltaFg = p.TextPrimary;
                            deltaFont = UiStyleHelper.SmallMonoReg;
                        }
                        else
                        {
                            deltaFg = p.TextPrimary;
                            deltaFont = UiStyleHelper.SmallMonoReg;
                        }
                    }
                    else
                    {
                        if (item.DeltaSign > 0) deltaFg = p.AccentJade;
                        else if (item.DeltaSign < 0) deltaFg = p.AccentCrimson;
                        else deltaFg = p.TextPrimary;
                    }

                    TextRenderer.DrawText(g, item.DeltaOrTargetValue, deltaFont, rDelta,
                        deltaFg, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }

            if (!string.IsNullOrEmpty(_subNoteLine))
            {
                var noteRect = new Rectangle(8, rect.Height - 19, rect.Width - 16, 17);
                TextRenderer.DrawText(g, _subNoteLine, UiStyleHelper.SmallFont, noteRect,
                    p.TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }
    }
}
