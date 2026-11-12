using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// 页签2（v0.6 无冗余按钮精简 + UI-UX-07 三列差值检查器 + UI-UX-06 分栏比例持久化）：
    /// • 移除右侧重复的【营造/升阶/开辟/拆除】按钮行，统一通过双击地块/双击建筑图纸与右键菜单直达；
    /// • 底部详情采用结构化双列键值对齐面板清晰展示堪舆加成与造价差额。
    /// </summary>
    public class UcSectBuildPanel : UserControl, IThemeableView
    {
        private readonly SIGEngine _engine;

        private readonly List<InteractiveSlotButton> _slotButtons = new List<InteractiveSlotButton>(24);
        private int _selectedSlotIndex = 0;

        private Label _lblSlotSummary;
        private UcCompactTileGrid _buildTileGrid;
        private ContextMenuStrip _slotContextMenu;

        public UcSectBuildPanel(SIGEngine engine)
        {
            _engine = engine;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            this.BackColor = UiStyleHelper.BgWindow;

            BuildUI();
            ApplyTheme();
        }

        public void FocusSlot(int slotIdx)
        {
            if (slotIdx >= 0 && slotIdx < 24)
            {
                _selectedSlotIndex = slotIdx;
                RefreshView();
            }
        }

        public bool HandleKeyboardNavigation(Keys keyData)
        {
            bool slotFocused = false;
            for (int i = 0; i < _slotButtons.Count; i++)
            {
                if (_slotButtons[i].Focused) { slotFocused = true; break; }
            }

            // 按住 Ctrl+方向键 或 当前焦点在左侧 6×4 灵脉地块时，直接在 24 个灵脉格中键盘移动/回车执行
            Keys baseKey = keyData & Keys.KeyCode;
            bool ctrlNav = ((keyData & Keys.Control) == Keys.Control) &&
                           (baseKey == Keys.Left || baseKey == Keys.Right || baseKey == Keys.Up || baseKey == Keys.Down);

            if (slotFocused || ctrlNav)
            {
                if (keyData == Keys.Enter)
                {
                    HandleSlotDoubleClicked(_selectedSlotIndex);
                    return true;
                }
                if (keyData == Keys.Apps || keyData == (Keys.F10 | Keys.Shift))
                {
                    var btn = _slotButtons[_selectedSlotIndex];
                    ShowVeinSlotContextMenu(_selectedSlotIndex, btn, new Point(8, btn.Height - 4));
                    return true;
                }
                int next = _selectedSlotIndex;
                if (baseKey == Keys.Left) next = Math.Max(0, _selectedSlotIndex - 1);
                else if (baseKey == Keys.Right) next = Math.Min(23, _selectedSlotIndex + 1);
                else if (baseKey == Keys.Up) next = Math.Max(0, _selectedSlotIndex - 6);
                else if (baseKey == Keys.Down) next = Math.Min(23, _selectedSlotIndex + 6);
                else return false;

                _selectedSlotIndex = next;
                RefreshView();
                if (slotFocused) _slotButtons[_selectedSlotIndex].Focus();
                return true;
            }

            return _buildTileGrid != null && _buildTileGrid.HandleKeyboardNavigation(keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _slotContextMenu != null)
            {
                _slotContextMenu.Dispose();
                _slotContextMenu = null;
            }
            base.Dispose(disposing);
        }

        private void BuildUI()
        {
            var split = UiStyleHelper.CreateProportionalSplit(Orientation.Vertical, 0.54, 430, 380, "Tab2.Main");

            var grpGrid = new GroupBox
            {
                Text = "【宗门 6×4 五行灵脉堪舆图（双击地块=开辟/升阶/营造，右键地块=选建/拆除菜单）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8)
            };

            var tblGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 4
            };
            UiStyleHelper.EnableDoubleBuffer(tblGrid);

            for (int c = 0; c < 6; c++) tblGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666F));
            for (int r = 0; r < 4; r++) tblGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25.0F));

            for (int idx = 0; idx < 24; idx++)
            {
                int captured = idx;
                var btn = new InteractiveSlotButton
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    FlatStyle = FlatStyle.Flat,
                    Font = UiStyleHelper.SmallFont,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 2;
                btn.Click += (s, e) =>
                {
                    _selectedSlotIndex = captured;
                    RefreshView();
                };
                btn.DoubleClick += (s, e) =>
                {
                    _selectedSlotIndex = captured;
                    HandleSlotDoubleClicked(captured);
                };
                btn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        _selectedSlotIndex = captured;
                        RefreshView();
                        ShowVeinSlotContextMenu(captured, btn, e.Location);
                    }
                };
                UiStyleHelper.SetToolTip(btn, string.Format("灵脉地块 #{0}：单击选中查看堪舆，双击开辟/升阶/营造，右键打开地块菜单（聚焦地块时可用方向键与 Enter 操作）", idx + 1));
                _slotButtons.Add(btn);
                tblGrid.Controls.Add(btn, idx % 6, idx / 6);
            }
            grpGrid.Controls.Add(tblGrid);

            var grpRight = new GroupBox
            {
                Text = "【选中地块状态与建筑营造图谱（双击建筑格子直接在选中空地营造）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8)
            };

            var tblRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblSlotSummary = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.MonoFont,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(0, 0, 0, 6),
                Text = "地块详情..."
            };

            _buildTileGrid = new UcCompactTileGrid(footerHeight: 112, tileWidth: 144, tileHeight: 42);
            _buildTileGrid.OnTileDoubleClicked += tile => DoConstructBuildingOnSlot(_selectedSlotIndex, tile.Id);
            _buildTileGrid.ContextMenuBuilder = tile =>
            {
                var menu = UiStyleHelper.CreateThemedContextMenu();
                menu.Items.Add(new ToolStripMenuItem(
                    string.Format("在选中地块 #{0} 营造【{1}】", _selectedSlotIndex + 1, tile.ShortTitle),
                    null,
                    (s, e) => DoConstructBuildingOnSlot(_selectedSlotIndex, tile.Id)));
                return menu;
            };

            tblRight.Controls.Add(_lblSlotSummary, 0, 0);
            tblRight.Controls.Add(_buildTileGrid, 0, 1);
            grpRight.Controls.Add(tblRight);

            split.Panel1.Controls.Add(grpGrid);
            split.Panel2.Controls.Add(grpRight);
            this.Controls.Add(split);
        }

        private void HandleSlotDoubleClicked(int slotIdx)
        {
            if (_engine.Save == null || slotIdx < 0 || slotIdx >= _engine.Save.Sect.GridSlots.Count) return;
            var slot = _engine.Save.Sect.GridSlots[slotIdx];

            if (!slot.IsUnlocked)
            {
                DoUnlockSlot(slotIdx);
            }
            else if (slot.Building != null)
            {
                DoUpgradeSlot(slotIdx);
            }
            else
            {
                var sel = _buildTileGrid.SelectedData;
                if (sel != null) DoConstructBuildingOnSlot(slotIdx, sel.Id);
            }
        }

        private void ShowVeinSlotContextMenu(int slotIdx, Control anchor, Point pt)
        {
            if (_engine.Save == null || slotIdx < 0 || slotIdx >= _engine.Save.Sect.GridSlots.Count) return;
            if (_slotContextMenu != null) { _slotContextMenu.Dispose(); _slotContextMenu = null; }

            var save = _engine.Save;
            var slot = save.Sect.GridSlots[slotIdx];
            _slotContextMenu = UiStyleHelper.CreateThemedContextMenu();

            if (!slot.IsUnlocked)
            {
                int unlockedCount = save.Sect.GridSlots.Count(s => s.IsUnlocked);
                double cost = Math.Round(SIGConfigTable.Current.Balance.UnlockSlotBaseStoneCost *
                                         Math.Pow(1.25, Math.Max(0, unlockedCount - 14)), 0);
                _slotContextMenu.Items.Add(new ToolStripMenuItem(
                    string.Format("开辟灵脉地块 #{0} (需 {1:F0} 灵石)", slotIdx + 1, cost),
                    null,
                    (s, e) => DoUnlockSlot(slotIdx)));
            }
            else if (slot.Building == null)
            {
                foreach (var b in SIGConfigTable.Current.Buildings)
                {
                    int cnt = SectBuildLogic.GetCountOfBuilding(save.Sect, b.Id);
                    bool full = (b.MaxCount > 0 && cnt >= b.MaxCount);
                    string wuxing = SectBuildLogic.GetWuXingRelationTag(slot.VeinElement, b.GetElement());
                    string bldId = b.Id;

                    var mi = new ToolStripMenuItem(
                        string.Format("营造 {0} [{1}]  ({2:F0}石/{3:F0}木/{4:F0}铁)",
                            b.GetShortLabel(), wuxing, b.CostSpiritStone, b.CostWood, b.CostOre),
                        null,
                        (s, e) => DoConstructBuildingOnSlot(slotIdx, bldId))
                    {
                        Enabled = !full
                    };
                    _slotContextMenu.Items.Add(mi);
                }
            }
            else
            {
                var bCfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                if (slot.Building.IsUnderConstruction)
                {
                    _slotContextMenu.Items.Add(new ToolStripMenuItem("正在施工营造中...") { Enabled = false });
                }
                else if (bCfg != null && slot.Building.Level < bCfg.MaxLevel)
                {
                    double st, wd, or, sec;
                    SectBuildLogic.GetBuildOrUpgradeCost(bCfg, slot.Building.Level + 1, out st, out wd, out or, out sec);
                    _slotContextMenu.Items.Add(new ToolStripMenuItem(
                        string.Format("升阶【{0}】至 Lv.{1} ({2:F0}石/{3:F0}木/{4:F0}铁)",
                            bCfg.GetShortLabel(), slot.Building.Level + 1, st, wd, or),
                        null,
                        (s, e) => DoUpgradeSlot(slotIdx)));
                }
                else
                {
                    _slotContextMenu.Items.Add(new ToolStripMenuItem("已达最高建筑等级") { Enabled = false });
                }

                if (slot.Building.ConfigId != "BLD_HALL")
                {
                    _slotContextMenu.Items.Add(new ToolStripSeparator());
                    _slotContextMenu.Items.Add(new ToolStripMenuItem(
                        "拆除该建筑 (回收60%石木)",
                        null,
                        (s, e) => DoDemolishSlot(slotIdx)));
                }
            }

            UiStyleHelper.StyleContextMenu(_slotContextMenu);
            _slotContextMenu.Show(anchor, pt);
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;
            if (_lblSlotSummary != null)
            {
                _lblSlotSummary.BackColor = p.BgCard;
                _lblSlotSummary.ForeColor = p.TextPrimary;
            }
            RefreshView();
        }

        private void DoConstructBuildingOnSlot(int slotIdx, string buildingId)
        {
            string msg;
            bool ok = SectBuildLogic.StartConstruct(_engine.Save, slotIdx, buildingId, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoUnlockSlot(int slotIdx)
        {
            string msg;
            bool ok = SectBuildLogic.UnlockGridSlot(_engine.Save, slotIdx, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoUpgradeSlot(int slotIdx)
        {
            string msg;
            bool ok = SectBuildLogic.StartUpgrade(_engine.Save, slotIdx, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoDemolishSlot(int slotIdx)
        {
            if (_engine.Save == null || slotIdx < 0 || slotIdx >= _engine.Save.Sect.GridSlots.Count) return;
            var slot = _engine.Save.Sect.GridSlots[slotIdx];
            if (slot.Building != null && slot.Building.Level >= 2 && slot.Building.ConfigId != "BLD_HALL")
            {
                var bCfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                var dr = MessageBox.Show(
                    string.Format("确定要拆除地块 #{0} 的【{1} Lv.{2}】吗？（回收基础造价60%灵石与灵木）",
                        slotIdx + 1, bCfg != null ? bCfg.NameCn : slot.Building.ConfigId, slot.Building.Level),
                    "拆除确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr != DialogResult.Yes) return;
            }

            string msg;
            bool ok = SectBuildLogic.DemolishBuilding(_engine.Save, slotIdx, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        public void RefreshView()
        {
            var save = _engine.Save;
            if (save == null) return;
            var p = UiStyleHelper.Current;

            for (int i = 0; i < _slotButtons.Count && i < save.Sect.GridSlots.Count; i++)
            {
                var slot = save.Sect.GridSlots[i];
                var btn = _slotButtons[i];
                bool isSel = (i == _selectedSlotIndex);

                btn.FlatAppearance.BorderColor = isSel ? p.TileSelectedBorder : p.BorderStrong;

                string text;
                Color bg;
                Color fg;
                if (!slot.IsUnlocked)
                {
                    text = string.Format("#{0} [待辟]\n{1} x{2:F2}", i + 1, EnumTextMap.ElemShortCn(slot.VeinElement), slot.BaseAura);
                    bg = p.SlotLockedBg;
                    fg = p.TextMuted;
                }
                else if (slot.Building == null)
                {
                    text = string.Format("#{0} [空置]\n{1}脉 x{2:F2}", i + 1, EnumTextMap.ElemShortCn(slot.VeinElement), slot.BaseAura);
                    bg = isSel ? p.SlotEmptySelBg : p.BgCard;
                    fg = p.TextPrimary;
                }
                else
                {
                    var bCfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                    string bShort = bCfg != null ? bCfg.GetShortLabel() : slot.Building.ConfigId;
                    if (slot.Building.IsUnderConstruction)
                    {
                        double pct = slot.Building.BuildProgressSec / Math.Max(0.1, slot.Building.BuildTotalSec);
                        text = string.Format("#{0} [营造]{1}\nL{2} {3:P0}", i + 1, bShort, slot.Building.Level, pct);
                        bg = p.SlotConstructBg;
                        fg = p.SlotConstructFg;
                    }
                    else
                    {
                        double eff = SectBuildLogic.GetSlotEfficiencyMultiplier(slot, bCfg);
                        text = string.Format("#{0} {1} L{2}\n{3}脉 x{4:F2}",
                            i + 1, bShort, slot.Building.Level, EnumTextMap.ElemShortCn(slot.VeinElement), eff);
                        bg = isSel ? p.SlotBuiltSelBg : p.SlotBuiltBg;
                        fg = p.SlotBuiltFg;
                    }
                }

                if (btn.Text != text) btn.Text = text;
                if (btn.BackColor != bg) btn.BackColor = bg;
                if (btn.ForeColor != fg) btn.ForeColor = fg;
            }

            var selSlot = save.Sect.GridSlots[_selectedSlotIndex];
            var sbSlot = new StringBuilder();
            sbSlot.AppendLine(string.Format("地块 #{0} [{1}]  |  {2}灵脉 (浓度x{3:F2})  |  {4}",
                selSlot.SlotIndex + 1, selSlot.ZoneName, EnumTextMap.ElemCn(selSlot.VeinElement),
                selSlot.BaseAura, selSlot.IsUnlocked ? "已开辟" : "[未开辟 · 双击地块直接开辟]"));

            if (selSlot.Building == null)
            {
                sbSlot.Append("状态：【空置灵地】—— 右键左侧地块就地选建，或双击下方建筑图谱直接营造。");
            }
            else
            {
                var bCfg = SIGConfigTable.GetBuilding(selSlot.Building.ConfigId);
                double eff = SectBuildLogic.GetSlotEfficiencyMultiplier(selSlot, bCfg);
                if (selSlot.Building.IsUnderConstruction)
                {
                    sbSlot.AppendFormat("驻地：【{0} Lv.{1}】 施工进度 {2:F1}s / {3:F1}s",
                        bCfg.NameCn, selSlot.Building.Level, selSlot.Building.BuildProgressSec, selSlot.Building.BuildTotalSec);
                }
                else if (selSlot.Building.Level < bCfg.MaxLevel)
                {
                    double st, wd, or, sec;
                    SectBuildLogic.GetBuildOrUpgradeCost(bCfg, selSlot.Building.Level + 1, out st, out wd, out or, out sec);
                    sbSlot.AppendFormat("驻地：【{0} Lv.{1}/{2}】(效能x{3:F2}) | 升阶需: 石{4}/木{5}/铁{6} (双击地块升阶，右键可拆除)",
                        bCfg.NameCn, selSlot.Building.Level, bCfg.MaxLevel, eff, st, wd, or);
                }
                else
                {
                    sbSlot.AppendFormat("驻地：【{0} Lv.{1}】 已臻满级 (总效能 x{2:F2}，右键地块可拆除)", bCfg.NameCn, bCfg.MaxLevel, eff);
                }
            }
            string sumTxt = sbSlot.ToString();
            if (_lblSlotSummary.Text != sumTxt) _lblSlotSummary.Text = sumTxt;

            var tiles = new List<CompactTileData>();
            foreach (var b in SIGConfigTable.Current.Buildings)
            {
                int builtCnt = SectBuildLogic.GetCountOfBuilding(save.Sect, b.Id);
                string maxTxt = b.MaxCount > 0 ? b.MaxCount.ToString() : "∞";
                bool reachedMax = (b.MaxCount > 0 && builtCnt >= b.MaxCount);
                string matchTag = SectBuildLogic.GetWuXingRelationTag(selSlot.VeinElement, b.GetElement());
                double effMult = SectBuildLogic.GetSlotEfficiencyMultiplier(selSlot, b);
                int sign = EnumTextMap.IsGenerating(selSlot.VeinElement, b.GetElement()) || selSlot.VeinElement == b.GetElement()
                    ? 1 : (EnumTextMap.IsOvercoming(selSlot.VeinElement, b.GetElement()) ? -1 : 0);

                string outDesc = !string.IsNullOrEmpty(b.ProduceResourceKey)
                    ? string.Format("+{0:F2}/s", b.BaseOutputPerSec * effMult)
                    : (b.AuraBonusRate > 0 ? string.Format("聚灵+{0:P0}", b.AuraBonusRate * effMult)
                      : (b.DefenseBarrierBonus > 0 ? string.Format("结界+{0:F0}", b.DefenseBarrierBonus * effMult) : "功能工坊"));

                var rows = new List<InspectorRowData>
                {
                    new InspectorRowData("建造上限", "已建 " + builtCnt, "上限 " + maxTxt, reachedMax ? -1 : 0),
                    new InspectorRowData("地块契合", string.Format("#{0} {1}脉", selSlot.SlotIndex + 1, EnumTextMap.ElemShortCn(selSlot.VeinElement)),
                        string.Format("{0} (x{1:F2})", matchTag, effMult), sign),
                    new InspectorRowData("灵石消耗", string.Format("存:{0:F0}", save.Sect.GetResource(ResourceType.SpiritStone)),
                        string.Format("需 {0:F0}", b.CostSpiritStone), save.Sect.GetResource(ResourceType.SpiritStone) >= b.CostSpiritStone ? 0 : -1),
                    new InspectorRowData("木/铁消耗", string.Format("木{0:F0}/铁{1:F0}", save.Sect.GetResource(ResourceType.SpiritWood), save.Sect.GetResource(ResourceType.SpiritOre)),
                        string.Format("木{0:F0} / 铁{1:F0}", b.CostWood, b.CostOre), 0),
                    new InspectorRowData("预计产出", string.Format("工期 {0:F0}s", b.BuildTimeSec), outDesc, sign >= 0 ? 1 : 0),
                    new InspectorRowData("建筑定位", EnumTextMap.BldCategoryCn(b.GetCategory()), EnumTextMap.ElemCn(b.GetElement()), 0)
                };

                tiles.Add(new CompactTileData
                {
                    Id = b.Id,
                    ShortTitle = b.GetShortLabel(),
                    SubValueTop = builtCnt.ToString(),
                    SubValueBottom = maxTxt,
                    InspectorTitle = string.Format("【{0}】 {1}", b.NameCn, reachedMax ? "[已达上限]" : "（双击营造至选中空地）"),
                    InspectorNote = b.Desc,
                    InspectorRows = rows,
                    IsLocked = reachedMax
                });
            }
            _buildTileGrid.SetItems(tiles);
        }
    }
}
