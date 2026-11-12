using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// 紧凑格子数据描述模型（支持 UI-UX-07 结构化三列键值对比行 InspectorRows 与回退文本 DetailText）
    /// </summary>
    public class CompactTileData
    {
        public string Id { get; set; } = "";
        public string ShortTitle { get; set; } = "";
        public string SubValueTop { get; set; } = "";
        public string SubValueBottom { get; set; } = "";
        public string DetailText { get; set; } = "";
        public string InspectorTitle { get; set; } = "";
        public string InspectorNote { get; set; } = "";
        public List<InspectorRowData> InspectorRows { get; set; } = new List<InspectorRowData>();
        public bool IsActiveEquipped { get; set; } = false;
        public bool IsLocked { get; set; } = false;
    }

    internal sealed class NonInteractiveStackedCellsPanel : Panel
    {
        private string _topText = "";
        private string _bottomText = "";

        public NonInteractiveStackedCellsPanel()
        {
            this.DoubleBuffered = true;
            this.Enabled = false;
        }

        public void SetTexts(string top, string bottom)
        {
            top = top ?? "";
            bottom = bottom ?? "";
            if (_topText == top && _bottomText == bottom) return;
            _topText = top;
            _bottomText = bottom;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = this.ClientRectangle;
            if (rect.Width <= 2 || rect.Height <= 2) return;

            var p = UiStyleHelper.Current;
            int halfH = rect.Height / 2;
            var topRect = new Rectangle(0, 0, rect.Width, halfH);
            var botRect = new Rectangle(0, halfH + 1, rect.Width, rect.Height - halfH - 1);

            using (var bTop = new SolidBrush(p.TileRightTopBg))
            using (var bBot = new SolidBrush(p.TileRightBotBg))
            using (var penDiv = new Pen(p.BorderSubtle))
            {
                g.FillRectangle(bTop, topRect);
                g.FillRectangle(bBot, botRect);
                g.DrawLine(penDiv, 0, halfH, rect.Width, halfH);
                g.DrawLine(penDiv, 0, 0, 0, rect.Height);
            }

            TextRenderer.DrawText(g, _topText, UiStyleHelper.SmallMonoBold, topRect,
                p.TileRightTopFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, _bottomText, UiStyleHelper.SmallMonoReg, botRect,
                p.TileRightBotFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>
    /// 单个紧凑格子卡片控件（无彩色 Emoji 污染，采用纯几何字符标记状态）
    /// </summary>
    public class UcCompactTile : UserControl, IThemeableView
    {
        private readonly InteractiveSlotButton _btnMain;
        private readonly NonInteractiveStackedCellsPanel _pnlRightStack;

        private string _id = "";
        private bool _isSelected = false;
        private bool _isActiveEquipped = false;
        private bool _isLocked = false;

        public event Action<string> OnTileClicked;
        public event Action<string> OnTileDoubleClicked;
        public event Action<string, Control, Point> OnTileRightClicked;

        public string TileId
        {
            get { return _id; }
        }

        public UcCompactTile(int tileWidth = 144, int tileHeight = 42)
        {
            this.DoubleBuffered = true;
            this.Size = new Size(tileWidth, tileHeight);
            this.Margin = new Padding(3);
            this.Padding = new Padding(1);

            _pnlRightStack = new NonInteractiveStackedCellsPanel
            {
                Dock = DockStyle.Right,
                Width = 42
            };

            _btnMain = new InteractiveSlotButton
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = UiStyleHelper.BoldFont,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            _btnMain.FlatAppearance.BorderSize = 0;
            _btnMain.Click += (s, e) =>
            {
                if (OnTileClicked != null) OnTileClicked(_id);
            };
            _btnMain.DoubleClick += (s, e) =>
            {
                if (OnTileDoubleClicked != null) OnTileDoubleClicked(_id);
            };
            _btnMain.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && OnTileRightClicked != null)
                {
                    OnTileRightClicked(_id, _btnMain, e.Location);
                }
            };

            this.Controls.Add(_btnMain);
            this.Controls.Add(_pnlRightStack);
            ApplyTheme();
        }

        public void BindData(CompactTileData data, bool selected)
        {
            _id = data.Id;
            _isSelected = selected;
            _isActiveEquipped = data.IsActiveEquipped;
            _isLocked = data.IsLocked;

            string title = data.IsActiveEquipped ? ("■ " + data.ShortTitle) : data.ShortTitle;
            if (_btnMain.Text != title) _btnMain.Text = title;
            _pnlRightStack.SetTexts(data.SubValueTop, data.SubValueBottom);

            ApplyStateColors();
        }

        public void ApplyTheme()
        {
            ApplyStateColors();
            _pnlRightStack.Invalidate();
        }

        private void ApplyStateColors()
        {
            var p = UiStyleHelper.Current;
            Color borderCol;
            Color mainBg;
            Color mainFg;

            if (_isSelected)
            {
                borderCol = p.TileSelectedBorder;
                mainBg = p.TileSelectedBg;
                mainFg = p.TileSelectedFg;
            }
            else if (_isActiveEquipped)
            {
                borderCol = p.TileEquippedBorder;
                mainBg = p.TileEquippedBg;
                mainFg = p.TileEquippedFg;
            }
            else if (_isLocked)
            {
                borderCol = p.TileLockedBorder;
                mainBg = p.TileLockedBg;
                mainFg = p.TileLockedFg;
            }
            else
            {
                borderCol = p.TileNormalBorder;
                mainBg = p.TileNormalBg;
                mainFg = p.TileNormalFg;
            }

            Padding targetPad = _isSelected ? new Padding(2) : new Padding(1);
            if (this.Padding != targetPad) this.Padding = targetPad;
            if (this.BackColor != borderCol) this.BackColor = borderCol;
            if (_btnMain.BackColor != mainBg) _btnMain.BackColor = mainBg;
            if (_btnMain.ForeColor != mainFg) _btnMain.ForeColor = mainFg;
        }
    }

    /// <summary>
    /// 多列流式紧凑格子容器 + 底部内置 UcKeyValueInspector 结构化键值差值检查器（UI-UX-00/05/07）
    /// </summary>
    public class UcCompactTileGrid : UserControl, IThemeableView
    {
        private readonly FlowLayoutPanel _flowTiles;
        private readonly Panel _spacer;
        private readonly UcKeyValueInspector _inspectorFooter;
        private readonly List<UcCompactTile> _tileControls = new List<UcCompactTile>();
        private readonly Dictionary<string, CompactTileData> _dataMap = new Dictionary<string, CompactTileData>();

        private readonly int _tileWidth;
        private readonly int _tileHeight;
        private string _selectedId = "";
        private ContextMenuStrip _activeContextMenu;

        public event Action<CompactTileData> OnSelectionChanged;
        public event Action<CompactTileData> OnTileDoubleClicked;
        public Func<CompactTileData, ContextMenuStrip> ContextMenuBuilder { get; set; }

        public string SelectedId
        {
            get { return _selectedId; }
            set
            {
                _selectedId = value ?? "";
                RefreshSelectionVisualsOnly();
            }
        }

        public CompactTileData SelectedData
        {
            get
            {
                CompactTileData d;
                return (!string.IsNullOrEmpty(_selectedId) && _dataMap.TryGetValue(_selectedId, out d)) ? d : null;
            }
        }

        public UcCompactTileGrid(int footerHeight = 92, int tileWidth = 144, int tileHeight = 42)
        {
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;

            _flowTiles = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(4)
            };
            UiStyleHelper.EnableDoubleBuffer(_flowTiles);

            _inspectorFooter = new UcKeyValueInspector
            {
                Dock = DockStyle.Bottom,
                Height = footerHeight
            };

            this.Controls.Add(_flowTiles);
            if (footerHeight > 0)
            {
                _spacer = new Panel { Dock = DockStyle.Bottom, Height = 4 };
                this.Controls.Add(_spacer);
                this.Controls.Add(_inspectorFooter);
            }

            ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _activeContextMenu != null)
            {
                _activeContextMenu.Dispose();
                _activeContextMenu = null;
            }
            base.Dispose(disposing);
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;
            _flowTiles.BackColor = p.BgSubtle;
            if (_spacer != null) _spacer.BackColor = p.BgWindow;
            _inspectorFooter.ApplyTheme();

            for (int i = 0; i < _tileControls.Count; i++)
            {
                _tileControls[i].ApplyTheme();
            }
        }

        public void SetItems(IList<CompactTileData> items)
        {
            if (items == null) items = new List<CompactTileData>();
            _dataMap.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                _dataMap[items[i].Id] = items[i];
            }

            if ((string.IsNullOrEmpty(_selectedId) || !_dataMap.ContainsKey(_selectedId)) && items.Count > 0)
            {
                _selectedId = items[0].Id;
            }

            bool countChanged = (_tileControls.Count != items.Count);
            if (countChanged) _flowTiles.SuspendLayout();

            while (_tileControls.Count < items.Count)
            {
                var tile = new UcCompactTile(_tileWidth, _tileHeight);
                tile.OnTileClicked += HandleTileClicked;
                tile.OnTileDoubleClicked += HandleTileDoubleClicked;
                tile.OnTileRightClicked += HandleTileRightClicked;
                _tileControls.Add(tile);
                _flowTiles.Controls.Add(tile);
            }

            while (_tileControls.Count > items.Count)
            {
                int last = _tileControls.Count - 1;
                var rem = _tileControls[last];
                _tileControls.RemoveAt(last);
                _flowTiles.Controls.Remove(rem);
                rem.Dispose();
            }

            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i];
                _tileControls[i].BindData(d, d.Id == _selectedId);
            }

            if (countChanged) _flowTiles.ResumeLayout();
            UpdateFooterContent();
        }

        private void HandleTileClicked(string id)
        {
            _selectedId = id;
            RefreshSelectionVisualsOnly();
            var sel = SelectedData;
            if (sel != null && OnSelectionChanged != null)
            {
                OnSelectionChanged(sel);
            }
        }

        private void HandleTileDoubleClicked(string id)
        {
            _selectedId = id;
            RefreshSelectionVisualsOnly();
            var sel = SelectedData;
            if (sel != null && OnTileDoubleClicked != null)
            {
                OnTileDoubleClicked(sel);
            }
        }

        private void HandleTileRightClicked(string id, Control anchorCtrl, Point pt)
        {
            HandleTileClicked(id);
            var sel = SelectedData;
            if (sel == null || ContextMenuBuilder == null) return;

            if (_activeContextMenu != null)
            {
                _activeContextMenu.Dispose();
                _activeContextMenu = null;
            }

            _activeContextMenu = ContextMenuBuilder(sel);
            if (_activeContextMenu != null && _activeContextMenu.Items.Count > 0)
            {
                UiStyleHelper.StyleContextMenu(_activeContextMenu);
                _activeContextMenu.Show(anchorCtrl, pt);
            }
        }

        private void RefreshSelectionVisualsOnly()
        {
            for (int i = 0; i < _tileControls.Count; i++)
            {
                var ctrl = _tileControls[i];
                CompactTileData d;
                if (_dataMap.TryGetValue(ctrl.TileId, out d))
                {
                    ctrl.BindData(d, d.Id == _selectedId);
                }
            }
            UpdateFooterContent();
        }

        private void UpdateFooterContent()
        {
            var sel = SelectedData;
            if (sel == null)
            {
                _inspectorFooter.SetContent("", "", null, "当前筛选条件下暂无条目。");
                return;
            }
            _inspectorFooter.SetContent(sel.InspectorTitle, sel.InspectorNote, sel.InspectorRows, sel.DetailText);
        }
    }
}
