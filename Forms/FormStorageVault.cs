using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Forms.UIs;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms
{
    /// <summary>
    /// 通用双模式【乾坤储物阁 / 物品选择器】窗口（v0.6：集成 UI-UX-02 筛选排序与批量步进条 + UI-UX-07 三列差值检查器，移除重复穿戴/服丹按钮）
    /// </summary>
    public class FormStorageVault : Form, IThemeableView
    {
        private readonly SIGEngine _engine;
        private readonly CharacterData _targetChar;
        private readonly bool _isPickerMode;
        private VaultFilterMode _filterMode;

        private UcFilterSortBar _filterBar;
        private UcBatchStepBar _batchStepBar;
        private UcCompactTileGrid _tileGrid;
        private UcStatusFeedbackStrip _localStatusStrip;
        private Label _lblPickerHint;

        public string PickedItemId { get; private set; } = null;
        public bool ClearSlotRequested { get; private set; } = false;

        public FormStorageVault(SIGEngine engine, CharacterData targetChar, VaultFilterMode initialFilter, bool isPickerMode, string titleOverride = null)
        {
            _engine = engine;
            _targetChar = targetChar ?? engine.Save.Leader;
            _filterMode = initialFilter;
            _isPickerMode = isPickerMode;

            this.DoubleBuffered = true;
            this.Text = titleOverride ?? (isPickerMode ? "选择槽位绑定物品" : "乾坤储物阁 - 宗门丹器总库");
            this.Size = new Size(740, 520);
            this.MinimumSize = new Size(580, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = UiStyleHelper.NormalFont;

            BuildUI();
            UiStyleHelper.ApplyThemeRecursive(this);
            RefreshVaultItems();
        }

        public static bool ShowSlotPicker(IWin32Window owner, SIGEngine engine, CharacterData targetChar,
            VaultFilterMode filter, string title, out string selectedItemId)
        {
            selectedItemId = null;
            using (var dlg = new FormStorageVault(engine, targetChar, filter, true, title))
            {
                var dr = dlg.ShowDialog(owner);
                if (dr == DialogResult.OK)
                {
                    selectedItemId = dlg.ClearSlotRequested ? "" : dlg.PickedItemId;
                    return true;
                }
                return false;
            }
        }

        private void BuildUI()
        {
            var topStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(6, 2, 6, 0)
            };

            if (!_isPickerMode)
            {
                var grpOpts = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("All", "全部"),
                    new KeyValuePair<string, string>("Pills", "丹药"),
                    new KeyValuePair<string, string>("Weapon", "飞剑"),
                    new KeyValuePair<string, string>("Armor", "宝衣"),
                    new KeyValuePair<string, string>("Talisman", "法宝")
                };
                _filterBar = new UcFilterSortBar(
                    grpOpts,
                    showElementFilter: true,
                    sortOptions: new string[] { "默认分类", "库存数量", "价值高低" });
                _filterBar.OnFilterChanged += () => RefreshVaultItems();
                topStrip.Controls.Add(_filterBar);
            }
            else
            {
                _lblPickerHint = new Label
                {
                    Dock = DockStyle.Fill,
                    Font = UiStyleHelper.BoldFont,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Text = string.Format("正在为【{0}】选择槽位绑定项（双击任意格子直接确认选定，右键亦可置空）：", _targetChar.Name)
                };
                topStrip.Controls.Add(_lblPickerHint);
            }

            _localStatusStrip = new UcStatusFeedbackStrip(
                subscribeGlobalBus: false,
                defaultHintText: _isPickerMode ? "双击格子:直接选定返回" : "双击:按批量服丹/换装 | 右键:绑定快捷槽");

            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Padding = new Padding(8, 2, 8, 2),
                FlowDirection = FlowDirection.RightToLeft
            };

            if (_isPickerMode)
            {
                var btnCancel = UiStyleHelper.CreateButton("取消", (s, e) =>
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }, UiButtonRole.Default, true);

                var btnClear = UiStyleHelper.CreateButton("置空此槽", (s, e) =>
                {
                    ClearSlotRequested = true;
                    PickedItemId = "";
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }, UiButtonRole.Default, true);

                pnlBottom.Controls.AddRange(new Control[] { btnCancel, btnClear });
            }
            else
            {
                var btnClose = UiStyleHelper.CreateButton("关闭", (s, e) => this.Close(), UiButtonRole.Default, true);
                _batchStepBar = new UcBatchStepBar("双击服丹批量:");
                pnlBottom.Controls.Add(btnClose);
                pnlBottom.Controls.Add(_batchStepBar);
            }

            _tileGrid = new UcCompactTileGrid(footerHeight: 102, tileWidth: 146, tileHeight: 42);
            _tileGrid.Padding = new Padding(8, 4, 8, 4);
            _tileGrid.OnTileDoubleClicked += tile =>
            {
                if (_isPickerMode) ConfirmSelection(tile.Id);
                else DoBrowsePrimaryActionBatch(tile.Id, _batchStepBar != null ? _batchStepBar.SelectedBatch : 1);
            };
            _tileGrid.ContextMenuBuilder = BuildVaultItemContextMenu;

            this.Controls.Add(_tileGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(_localStatusStrip);
            this.Controls.Add(topStrip);
        }

        private void ReportVaultResult(bool ok, string msg)
        {
            _localStatusStrip.SetFeedback(ok ? UiFeedbackLevel.Success : UiFeedbackLevel.Blocked, msg);
            UiFeedbackService.ReportAction(ok, msg);
        }

        private void DoBrowsePrimaryActionBatch(string itemId, int batchCount)
        {
            var cfg = SIGConfigTable.GetItem(itemId);
            if (cfg == null || _engine.Save == null) return;
            var cat = cfg.GetCategory();

            if (cat == ItemCategory.Weapon || cat == ItemCategory.Armor || cat == ItemCategory.Talisman)
            {
                string msg;
                bool ok = CultivationLogic.EquipOrSwapSlot(_engine.Save, _engine.Save.Leader, cat, cfg.Id, _engine.EventBus, out msg);
                ReportVaultResult(ok, msg);
                RefreshVaultItems();
                return;
            }

            int used = 0;
            string lastMsg = "";
            int maxLoop = Math.Max(1, Math.Min(999, batchCount));
            var ld = _engine.Save.Leader;

            for (int i = 0; i < maxLoop; i++)
            {
                if (used > 0 && cfg.QiGrant > 0 && ld.CurQi >= ld.MaxQi - 0.05) break;
                string msg;
                if (CultivationLogic.UseCultivationPill(_engine.Save, ld, cfg.Id, _engine.EventBus, out msg))
                {
                    used++;
                    lastMsg = msg;
                }
                else
                {
                    lastMsg = msg;
                    break;
                }
            }

            if (used > 1)
            {
                ReportVaultResult(true, string.Format("已连续服用 {0} 枚【{1}】（剩余库存 {2}）。",
                    used, cfg.NameCn, _engine.Save.Sect.GetItemCount(cfg.Id)));
            }
            else
            {
                ReportVaultResult(used > 0, lastMsg);
            }
            RefreshVaultItems();
        }

        private ContextMenuStrip BuildVaultItemContextMenu(CompactTileData tile)
        {
            if (tile == null || _engine.Save == null) return null;
            var cfg = SIGConfigTable.GetItem(tile.Id);
            if (cfg == null) return null;

            var menu = UiStyleHelper.CreateThemedContextMenu();
            if (_isPickerMode)
            {
                menu.Items.Add(new ToolStripMenuItem("选定此项并返回", null, (s, e) => ConfirmSelection(cfg.Id)));
                menu.Items.Add(new ToolStripMenuItem("置空此槽位", null, (s, e) =>
                {
                    ClearSlotRequested = true;
                    PickedItemId = "";
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }));
                return menu;
            }

            var cat = cfg.GetCategory();
            var ld = _engine.Save.Leader;
            ld.EnsureQuickSlotsInitialized();

            if (cat == ItemCategory.Weapon || cat == ItemCategory.Armor || cat == ItemCategory.Talisman)
            {
                menu.Items.Add(new ToolStripMenuItem("掌门立即穿戴此法器", null, (s, e) => DoBrowsePrimaryActionBatch(cfg.Id, 1)));
            }
            else if (cat == ItemCategory.CultivationPill || cat == ItemCategory.CombatPill)
            {
                int b = _batchStepBar != null ? _batchStepBar.SelectedBatch : 1;
                menu.Items.Add(new ToolStripMenuItem(
                    string.Format("掌门立即服用 ({0})", b >= 999 ? "Max" : ("×" + b)),
                    null,
                    (s, e) => DoBrowsePrimaryActionBatch(cfg.Id, b)));
                menu.Items.Add(new ToolStripSeparator());

                for (int i = 0; i < 3; i++)
                {
                    int idx = i;
                    menu.Items.Add(new ToolStripMenuItem(
                        string.Format("绑定至修行快捷槽 {0}", idx + 1),
                        null,
                        (s, e) =>
                        {
                            ld.QuickCultivationPills[idx] = cfg.Id;
                            _engine.EventBus.NotifyStateChanged();
                            ReportVaultResult(true, string.Format("已将【{0}】绑定至修行快捷槽 {1}。", cfg.NameCn, idx + 1));
                        }));
                }

                menu.Items.Add(new ToolStripSeparator());
                for (int i = 0; i < 3; i++)
                {
                    int idx = i;
                    menu.Items.Add(new ToolStripMenuItem(
                        string.Format("绑定至斗法快捷槽 {0}", idx + 1),
                        null,
                        (s, e) =>
                        {
                            ld.QuickCombatPills[idx] = cfg.Id;
                            _engine.EventBus.NotifyStateChanged();
                            ReportVaultResult(true, string.Format("已将【{0}】绑定至斗法快捷槽 {1}。", cfg.NameCn, idx + 1));
                        }));
                }
            }
            else
            {
                menu.Items.Add(new ToolStripMenuItem("破境丹药将在冲击对应大境界时自动消耗") { Enabled = false });
            }

            return menu;
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            if (_lblPickerHint != null) _lblPickerHint.ForeColor = p.AccentBlue;
        }

        private void ConfirmSelection(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            PickedItemId = itemId;
            ClearSlotRequested = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public void RefreshVaultItems()
        {
            if (_engine.Save == null || this.IsDisposed) return;
            var sect = _engine.Save.Sect;
            var candidates = new List<ItemConfig>();

            foreach (var cfg in SIGConfigTable.Current.Items)
            {
                if (_isPickerMode)
                {
                    if (!MatchesPickerFilter(cfg.GetCategory())) continue;
                }
                else if (_filterBar != null)
                {
                    if (!MatchesBrowseBar(cfg)) continue;
                }

                int qty = sect.GetItemCount(cfg.Id);
                bool isPillSlotPicker = _isPickerMode &&
                    (_filterMode == VaultFilterMode.CultivationAndHealPills || _filterMode == VaultFilterMode.CombatPillsOnly);

                if (qty <= 0 && !isPillSlotPicker) continue;
                candidates.Add(cfg);
            }

            if (!_isPickerMode && _filterBar != null)
            {
                int sortIdx = _filterBar.SortIndex;
                if (sortIdx == 1)
                {
                    candidates.Sort((a, b) => sect.GetItemCount(b.Id).CompareTo(sect.GetItemCount(a.Id)));
                }
                else if (sortIdx == 2)
                {
                    candidates.Sort((a, b) => b.PriceSpiritStone.CompareTo(a.PriceSpiritStone));
                }
            }

            var tiles = new List<CompactTileData>();
            foreach (var cfg in candidates)
            {
                int qty = sect.GetItemCount(cfg.Id);
                var cat = cfg.GetCategory();
                string delta = CultivationLogic.PreviewEquipmentDelta(_targetChar, cfg.Id);
                int dSign = delta.Contains("+") ? 1 : (delta.Contains("-") ? -1 : 0);

                var rows = new List<InspectorRowData>
                {
                    new InspectorRowData("当前库存", "上限 " + cfg.MaxStack, qty + " 件", qty > 0 ? 1 : -1),
                    new InspectorRowData("品类 / 五行", EnumTextMap.ItemCategoryCn(cat), EnumTextMap.ElemCn(cfg.GetElement()), 0),
                    new InspectorRowData("估值参考", "", cfg.PriceSpiritStone + " 灵石", 0),
                    new InspectorRowData("装备替换", "", string.IsNullOrEmpty(delta) ? "消耗/辅助品" : delta.Replace("替换差值 ➔ ", ""), dSign)
                };

                tiles.Add(new CompactTileData
                {
                    Id = cfg.Id,
                    ShortTitle = cfg.GetShortLabel(),
                    SubValueTop = qty.ToString(),
                    SubValueBottom = cfg.MaxStack.ToString(),
                    InspectorTitle = string.Format("【{0}】 {1}", cfg.NameCn, _isPickerMode ? "（双击确认选定）" : "（双击使用/穿戴，右键绑快捷槽）"),
                    InspectorNote = cfg.Desc,
                    InspectorRows = rows,
                    IsLocked = (qty <= 0)
                });
            }

            _tileGrid.SetItems(tiles);
        }

        private bool MatchesBrowseBar(ItemConfig cfg)
        {
            string grp = _filterBar.ActiveGroupKey;
            var cat = cfg.GetCategory();
            if (grp == "Pills" && cat != ItemCategory.CultivationPill && cat != ItemCategory.CombatPill && cat != ItemCategory.BreakthroughPill)
                return false;
            if (grp == "Weapon" && cat != ItemCategory.Weapon) return false;
            if (grp == "Armor" && cat != ItemCategory.Armor) return false;
            if (grp == "Talisman" && cat != ItemCategory.Talisman) return false;

            if (_filterBar.ActiveElement.HasValue && cfg.GetElement() != _filterBar.ActiveElement.Value)
                return false;

            string kw = _filterBar.Keyword;
            if (!string.IsNullOrEmpty(kw))
            {
                if (cfg.NameCn.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0 &&
                    cfg.Desc.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }

        private bool MatchesPickerFilter(ItemCategory cat)
        {
            switch (_filterMode)
            {
                case VaultFilterMode.CultivationAndHealPills:
                    return cat == ItemCategory.CultivationPill || cat == ItemCategory.CombatPill;
                case VaultFilterMode.CombatPillsOnly:
                    return cat == ItemCategory.CombatPill || cat == ItemCategory.CultivationPill;
                case VaultFilterMode.WeaponOnly:
                    return cat == ItemCategory.Weapon;
                case VaultFilterMode.ArmorOnly:
                    return cat == ItemCategory.Armor;
                case VaultFilterMode.TalismanOnly:
                    return cat == ItemCategory.Talisman;
                default:
                    return true;
            }
        }
    }

    public enum VaultFilterMode
    {
        All = 0,
        CultivationAndHealPills = 1,
        CombatPillsOnly = 2,
        WeaponOnly = 3,
        ArmorOnly = 4,
        TalismanOnly = 5
    }
}
