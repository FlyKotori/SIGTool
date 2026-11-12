using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// 页签1（v0.6 无冗余按钮精简 + UI-UX-02 批量步进 + UI-UX-07 三列差值检查器）：
    /// • 移除法器槽【切换】、丹药槽【服用/切换】及藏经阁顶部【转修/参悟】等全部重复按钮，统一采用双击与右键菜单操作；
    /// • 修行快捷丹药改为整洁的 1×3 横排槽位卡片，配合 UcBatchStepBar（×1/×5/×10/Max）支持一键连服与功法连升。
    /// </summary>
    public class UcCultivationPanel : UserControl, IThemeableView
    {
        private readonly SIGEngine _engine;
        private readonly Action<CharacterData> _onOpenCharSheet;

        private Label _lblLeaderRealmTitle;
        private Label _lblLifespanAlert;
        private Label _lblCoreStatsLine;
        private SmoothGaugeBar _barHpMp;
        private SmoothGaugeBar _barQiPool;
        private Label _lblBreakthroughRate;
        private Button _btnBreakthrough;
        private UcBatchStepBar _batchStepBar;

        private InteractiveSlotButton _btnWeaponSlot;
        private InteractiveSlotButton _btnArmorSlot;
        private InteractiveSlotButton _btnTalismanSlot;
        private readonly InteractiveSlotButton[] _btnQuickPillSlots = new InteractiveSlotButton[3];

        private UcCompactTileGrid _techTileGrid;
        private ContextMenuStrip _slotPopupMenu;

        public UcCultivationPanel(SIGEngine engine, Action<CharacterData> onOpenCharSheet)
        {
            _engine = engine;
            _onOpenCharSheet = onOpenCharSheet;

            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            this.BackColor = UiStyleHelper.BgWindow;

            BuildUI();
            ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _slotPopupMenu != null)
            {
                _slotPopupMenu.Dispose();
                _slotPopupMenu = null;
            }
            base.Dispose(disposing);
        }

        private void BuildUI()
        {
            var splitMain = UiStyleHelper.CreateProportionalSplit(Orientation.Vertical, 0.43, 390, 420, "Tab1.Main");

            var grpLeft = new GroupBox
            {
                Text = "【掌门修行核心台】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8)
            };

            var flowLeft = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Margin = new Padding(0)
            };
            flowLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            flowLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            flowLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            flowLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            flowLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            flowLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 114F));
            flowLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlTitleRow = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 30);
            _lblLeaderRealmTitle = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.HeaderFont,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 0),
                Text = "云霄掌门 [炼气期·初期]"
            };
            _lblLeaderRealmTitle.DoubleClick += (s, e) =>
            {
                if (_onOpenCharSheet != null) _onOpenCharSheet(_engine.Save.Leader);
            };

            _lblLifespanAlert = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallBoldFont,
                Margin = new Padding(0, 6, 8, 0),
                Visible = false,
                Text = "[▲寿元将近]"
            };

            var btnFullSheet = UiStyleHelper.CreateButton("全属性详单", (s, e) =>
            {
                if (_onOpenCharSheet != null) _onOpenCharSheet(_engine.Save.Leader);
            }, UiButtonRole.Default, true);
            UiStyleHelper.SetToolTip(btnFullSheet, "打开掌门先天根骨与全量属性法卷 — 快捷键: C");
            UiStyleHelper.SetToolTip(_lblLeaderRealmTitle, "双击掌门名号亦可打开全属性详单 — 快捷键: C");

            pnlTitleRow.Controls.AddRange(new Control[] { _lblLeaderRealmTitle, _lblLifespanAlert, btnFullSheet });

            _lblCoreStatsLine = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.MonoFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "战力: 0 | 攻: 0 | 防: 0 | 聚灵: +0%"
            };

            var pnlBars = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _barHpMp = new SmoothGaugeBar(GaugeBarRole.HpJade) { Dock = DockStyle.Top, Height = 22 };
            var spacerBar = new Panel { Dock = DockStyle.Top, Height = 4 };
            _barQiPool = new SmoothGaugeBar(GaugeBarRole.QiBlue) { Dock = DockStyle.Top, Height = 22 };
            pnlBars.Controls.Add(_barQiPool);
            pnlBars.Controls.Add(spacerBar);
            pnlBars.Controls.Add(_barHpMp);

            var pnlBreakRow = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 32);
            _btnBreakthrough = UiStyleHelper.CreateButton("★ 冲击破境", (s, e) =>
            {
                string msg;
                bool ok = CultivationLogic.TryBreakthrough(_engine.Save, _engine.Save.Leader, _engine.Rng, _engine.EventBus, out msg);
                UiFeedbackService.ReportAction(ok, msg);
                RefreshView();
            }, UiButtonRole.WarningAmber, true);

            _lblBreakthroughRate = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(6, 6, 8, 0),
                Text = "突破准备中..."
            };

            UiStyleHelper.SetToolTip(_btnBreakthrough, "气海盈满时冲击下一小境界或渡劫破入大境界 — 快捷键: P");
            _batchStepBar = new UcBatchStepBar("批量:");
            pnlBreakRow.Controls.AddRange(new Control[] { _btnBreakthrough, _lblBreakthroughRate, _batchStepBar });

            var grpEquip = new GroupBox
            {
                Text = "【穿戴法器（双击选装 · 右键快换/卸下）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.NormalFont,
                Padding = new Padding(6, 3, 6, 4)
            };
            var tblEquip = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
            tblEquip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 3; i++) tblEquip.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

            _btnWeaponSlot = MakeInteractiveBarSlot(
                () => PickEquipSlot(ItemCategory.Weapon, VaultFilterMode.WeaponOnly, "选择佩戴飞剑"),
                (ctrl, pt) => ShowQuickEquipContextMenu(ItemCategory.Weapon, ctrl, pt));

            _btnArmorSlot = MakeInteractiveBarSlot(
                () => PickEquipSlot(ItemCategory.Armor, VaultFilterMode.ArmorOnly, "选择护体宝衣"),
                (ctrl, pt) => ShowQuickEquipContextMenu(ItemCategory.Armor, ctrl, pt));

            _btnTalismanSlot = MakeInteractiveBarSlot(
                () => PickEquipSlot(ItemCategory.Talisman, VaultFilterMode.TalismanOnly, "选择主动本命法宝"),
                (ctrl, pt) => ShowQuickEquipContextMenu(ItemCategory.Talisman, ctrl, pt));

            UiStyleHelper.SetToolTip(_btnWeaponSlot, "飞剑槽：双击打开储物阁挑选，右键直接快换库存飞剑或卸下");
            UiStyleHelper.SetToolTip(_btnArmorSlot, "宝衣槽：双击打开储物阁挑选，右键直接快换库存宝衣或卸下");
            UiStyleHelper.SetToolTip(_btnTalismanSlot, "本命法宝槽：双击打开储物阁挑选，右键直接快换库存法宝或卸下");
            tblEquip.Controls.Add(_btnWeaponSlot, 0, 0);
            tblEquip.Controls.Add(_btnArmorSlot, 0, 1);
            tblEquip.Controls.Add(_btnTalismanSlot, 0, 2);
            grpEquip.Controls.Add(tblEquip);

            var grpQuickPills = new GroupBox
            {
                Text = "【修行快捷丹药槽（双击按批量服丹 · 右键就地绑定/置空）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.NormalFont,
                Padding = new Padding(6, 4, 6, 6)
            };
            var tblPills = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            for (int i = 0; i < 3; i++) tblPills.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            tblPills.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 3; i++)
            {
                int slotIdx = i;
                var slotBtn = MakeInteractiveBarSlot(
                    () => UseQuickCultivationSlotBatch(slotIdx, _batchStepBar.SelectedBatch),
                    (ctrl, pt) => ShowQuickPillBindMenu(slotIdx, ctrl, pt));
                slotBtn.TextAlign = ContentAlignment.MiddleCenter;
                slotBtn.Margin = new Padding(3, 2, 3, 2);
                UiStyleHelper.SetToolTip(slotBtn, string.Format("修行快捷丹药槽 {0}：双击按上方所选批量服丹，右键改绑丹药 — 快捷键: {0}", i + 1));
                _btnQuickPillSlots[i] = slotBtn;
                tblPills.Controls.Add(slotBtn, i, 0);
            }
            grpQuickPills.Controls.Add(tblPills);

            flowLeft.Controls.Add(pnlTitleRow, 0, 0);
            flowLeft.Controls.Add(_lblCoreStatsLine, 0, 1);
            flowLeft.Controls.Add(pnlBars, 0, 2);
            flowLeft.Controls.Add(pnlBreakRow, 0, 3);
            flowLeft.Controls.Add(grpEquip, 0, 4);
            flowLeft.Controls.Add(grpQuickPills, 0, 5);
            grpLeft.Controls.Add(flowLeft);

            var grpRight = new GroupBox
            {
                Text = "【宗门藏经阁（双击未主修=转修，双击已主修=按批量参悟升层，右键=快捷菜单）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8)
            };

            _techTileGrid = new UcCompactTileGrid(footerHeight: 108, tileWidth: 146, tileHeight: 44);
            _techTileGrid.OnTileDoubleClicked += sel =>
            {
                if (_engine.Save == null) return;
                if (_engine.Save.Leader.EquippedTechniqueId == sel.Id)
                {
                    DoUpgradeTechniqueBatch(sel.Id, _batchStepBar.SelectedBatch);
                }
                else
                {
                    DoEquipTechnique(sel.Id);
                }
            };
            _techTileGrid.ContextMenuBuilder = BuildTechniqueContextMenu;

            grpRight.Controls.Add(_techTileGrid);

            splitMain.Panel1.Controls.Add(grpLeft);
            splitMain.Panel2.Controls.Add(grpRight);
            this.Controls.Add(splitMain);
        }

        private InteractiveSlotButton MakeInteractiveBarSlot(Action onDoubleClick, Action<Control, Point> onRightClick)
        {
            var btn = new InteractiveSlotButton
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = UiStyleHelper.SmallMonoReg,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(1, 1, 1, 1),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 1;
            if (onDoubleClick != null)
            {
                btn.DoubleClick += (s, e) => onDoubleClick();
            }
            if (onRightClick != null)
            {
                btn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right) onRightClick(btn, e.Location);
                };
            }
            return btn;
        }

        private void DoEquipTechnique(string techId)
        {
            string msg;
            bool ok = CultivationLogic.EquipTechnique(_engine.Save, _engine.Save.Leader, techId, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoUpgradeTechniqueBatch(string techId, int batchCount)
        {
            if (_engine.Save == null) return;
            int done = 0;
            string lastMsg = "";
            int maxLoop = Math.Max(1, Math.Min(50, batchCount));

            for (int i = 0; i < maxLoop; i++)
            {
                string msg;
                if (CultivationLogic.UpgradeTechniqueLevel(_engine.Save, _engine.Save.Leader, techId, _engine.EventBus, out msg))
                {
                    done++;
                    lastMsg = msg;
                }
                else
                {
                    lastMsg = msg;
                    break;
                }
            }

            if (done > 1)
            {
                int finalLv = _engine.Save.Leader.GetTechniqueLevel(techId);
                UiFeedbackService.PushSuccess(string.Format("批量参悟完成：连续精进 {0} 层，当前已达 Lv.{1}。", done, finalLv));
            }
            else
            {
                UiFeedbackService.ReportAction(done > 0, lastMsg);
            }
            RefreshView();
        }

        private ContextMenuStrip BuildTechniqueContextMenu(CompactTileData tile)
        {
            if (tile == null || _engine.Save == null) return null;
            var ld = _engine.Save.Leader;
            var tCfg = SIGConfigTable.GetTechnique(tile.Id);
            if (tCfg == null) return null;

            var menu = UiStyleHelper.CreateThemedContextMenu();
            bool isEquipped = (ld.EquippedTechniqueId == tCfg.Id);
            int curLv = ld.GetTechniqueLevel(tCfg.Id);

            menu.Items.Add(new ToolStripMenuItem(
                isEquipped ? "■ 当前正在主修此功法" : ("转修为主修功法：" + tCfg.NameCn),
                null,
                (s, e) => DoEquipTechnique(tCfg.Id))
            {
                Enabled = !isEquipped
            });

            if (curLv >= tCfg.MaxLevel)
            {
                menu.Items.Add(new ToolStripMenuItem("已参悟至圆满层数") { Enabled = false });
            }
            else
            {
                double cost = CultivationLogic.GetTechniqueUpgradeStoneCost(ld, tCfg);
                menu.Items.Add(new ToolStripMenuItem(
                    string.Format("参悟升 1 层 -> Lv.{0} ({1:F0} 灵石)", curLv + 1, cost),
                    null,
                    (s, e) => DoUpgradeTechniqueBatch(tCfg.Id, 1)));

                if (_batchStepBar.SelectedBatch > 1)
                {
                    int batch = _batchStepBar.SelectedBatch;
                    string bText = batch >= 999 ? "Max" : ("×" + batch);
                    menu.Items.Add(new ToolStripMenuItem(
                        string.Format("按当前批量 ({0}) 连续参悟升层", bText),
                        null,
                        (s, e) => DoUpgradeTechniqueBatch(tCfg.Id, batch)));
                }
            }

            return menu;
        }

        private void ShowQuickEquipContextMenu(ItemCategory slotCat, Control anchor, Point pt)
        {
            if (_engine.Save == null) return;
            if (_slotPopupMenu != null) { _slotPopupMenu.Dispose(); _slotPopupMenu = null; }

            _slotPopupMenu = UiStyleHelper.CreateThemedContextMenu();
            var ld = _engine.Save.Leader;
            var sect = _engine.Save.Sect;

            int added = 0;
            foreach (var it in SIGConfigTable.Current.Items)
            {
                if (it.GetCategory() != slotCat) continue;
                int qty = sect.GetItemCount(it.Id);
                if (qty <= 0) continue;

                string capturedId = it.Id;
                string delta = CultivationLogic.PreviewEquipmentDelta(ld, capturedId);
                _slotPopupMenu.Items.Add(new ToolStripMenuItem(
                    string.Format("换装：{0} (×{1})  {2}", it.NameCn, qty, delta),
                    null,
                    (s, e) =>
                    {
                        string msg;
                        bool ok = CultivationLogic.EquipOrSwapSlot(_engine.Save, ld, slotCat, capturedId, _engine.EventBus, out msg);
                        UiFeedbackService.ReportAction(ok, msg);
                        RefreshView();
                    }));
                added++;
            }

            if (added == 0)
            {
                _slotPopupMenu.Items.Add(new ToolStripMenuItem("储物阁暂无同类备选法器") { Enabled = false });
            }

            _slotPopupMenu.Items.Add(new ToolStripSeparator());
            _slotPopupMenu.Items.Add(new ToolStripMenuItem("打开储物阁挑选...", null, (s, e) =>
            {
                VaultFilterMode fm = slotCat == ItemCategory.Weapon ? VaultFilterMode.WeaponOnly
                    : (slotCat == ItemCategory.Armor ? VaultFilterMode.ArmorOnly : VaultFilterMode.TalismanOnly);
                PickEquipSlot(slotCat, fm, "选择穿戴法器");
            }));
            _slotPopupMenu.Items.Add(new ToolStripMenuItem("卸下当前槽位法器", null, (s, e) =>
            {
                string msg;
                bool ok = CultivationLogic.EquipOrSwapSlot(_engine.Save, ld, slotCat, "", _engine.EventBus, out msg);
                UiFeedbackService.ReportAction(ok, msg);
                RefreshView();
            }));

            UiStyleHelper.StyleContextMenu(_slotPopupMenu);
            _slotPopupMenu.Show(anchor, pt);
        }

        private void ShowQuickPillBindMenu(int slotIdx, Control anchor, Point pt)
        {
            if (_engine.Save == null) return;
            if (_slotPopupMenu != null) { _slotPopupMenu.Dispose(); _slotPopupMenu = null; }

            _slotPopupMenu = UiStyleHelper.CreateThemedContextMenu();
            var ld = _engine.Save.Leader;
            ld.EnsureQuickSlotsInitialized();

            string curId = ld.QuickCultivationPills[slotIdx];
            if (!string.IsNullOrEmpty(curId))
            {
                int batch = _batchStepBar.SelectedBatch;
                string bTxt = batch >= 999 ? "Max" : ("×" + batch);
                _slotPopupMenu.Items.Add(new ToolStripMenuItem(
                    string.Format("立即服用此槽丹药 ({0})", bTxt),
                    null,
                    (s, e) => UseQuickCultivationSlotBatch(slotIdx, batch)));
                _slotPopupMenu.Items.Add(new ToolStripSeparator());
            }

            foreach (var it in SIGConfigTable.Current.Items)
            {
                var cat = it.GetCategory();
                if (cat != ItemCategory.CultivationPill && cat != ItemCategory.CombatPill) continue;
                int qty = _engine.Save.Sect.GetItemCount(it.Id);
                string capturedId = it.Id;
                var mi = new ToolStripMenuItem(
                    string.Format("绑定：{0} (库存 ×{1})", it.NameCn, qty),
                    null,
                    (s, e) =>
                    {
                        ld.QuickCultivationPills[slotIdx] = capturedId;
                        UiFeedbackService.PushSuccess(string.Format("已将修行快捷槽 {0} 绑定为【{1}】。", slotIdx + 1, it.NameCn));
                        RefreshView();
                    })
                {
                    Checked = (curId == capturedId)
                };
                _slotPopupMenu.Items.Add(mi);
            }

            _slotPopupMenu.Items.Add(new ToolStripSeparator());
            _slotPopupMenu.Items.Add(new ToolStripMenuItem("置空此快捷槽", null, (s, e) =>
            {
                ld.QuickCultivationPills[slotIdx] = "";
                UiFeedbackService.PushInfo(string.Format("已置空修行快捷槽 {0}。", slotIdx + 1));
                RefreshView();
            }));

            UiStyleHelper.StyleContextMenu(_slotPopupMenu);
            _slotPopupMenu.Show(anchor, pt);
        }

        public void TriggerQuickPillByHotkey(int slotIdx)
        {
            if (slotIdx < 0 || slotIdx >= 3) return;
            UseQuickCultivationSlotBatch(slotIdx, _batchStepBar != null ? _batchStepBar.SelectedBatch : 1);
        }

        public void TriggerBreakthroughByHotkey()
        {
            if (_engine.Save == null) return;
            string msg;
            bool ok = CultivationLogic.TryBreakthrough(_engine.Save, _engine.Save.Leader, _engine.Rng, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        public bool HandleKeyboardNavigation(Keys keyData)
        {
            return _techTileGrid != null && _techTileGrid.HandleKeyboardNavigation(keyData);
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;
            if (_lblLeaderRealmTitle != null) _lblLeaderRealmTitle.ForeColor = p.TextPrimary;
            if (_lblLifespanAlert != null) _lblLifespanAlert.ForeColor = p.AccentCrimson;
            if (_lblCoreStatsLine != null) _lblCoreStatsLine.ForeColor = p.AccentJade;
            if (_lblBreakthroughRate != null) _lblBreakthroughRate.ForeColor = p.AccentGold;

            StyleBarSlot(_btnWeaponSlot, p);
            StyleBarSlot(_btnArmorSlot, p);
            StyleBarSlot(_btnTalismanSlot, p);
            for (int i = 0; i < 3; i++) StyleBarSlot(_btnQuickPillSlots[i], p);
        }

        private static void StyleBarSlot(InteractiveSlotButton btn, UiThemePalette p)
        {
            if (btn == null) return;
            btn.BackColor = p.BgCard;
            btn.ForeColor = p.TextPrimary;
            btn.FlatAppearance.BorderColor = p.BorderStrong;
        }

        private void PickEquipSlot(ItemCategory slotCat, VaultFilterMode filter, string title)
        {
            string pickedId;
            if (FormStorageVault.ShowSlotPicker(this, _engine, _engine.Save.Leader, filter, title, out pickedId))
            {
                string msg;
                bool ok = CultivationLogic.EquipOrSwapSlot(_engine.Save, _engine.Save.Leader, slotCat, pickedId, _engine.EventBus, out msg);
                UiFeedbackService.ReportAction(ok, msg);
                RefreshView();
            }
        }

        private void UseQuickCultivationSlotBatch(int slotIdx, int batchCount)
        {
            var ld = _engine.Save.Leader;
            ld.EnsureQuickSlotsInitialized();
            string pillId = ld.QuickCultivationPills[slotIdx];
            if (string.IsNullOrEmpty(pillId))
            {
                UiFeedbackService.PushWarning(string.Format("丹药槽 {0} 为空，请右键该槽位直接绑定丹药。", slotIdx + 1));
                return;
            }

            int used = 0;
            string lastMsg = "";
            int maxLoop = Math.Max(1, Math.Min(999, batchCount));

            for (int i = 0; i < maxLoop; i++)
            {
                // 修行丹若气海已满且非纯回血用途则自动停服避免浪费
                var pCfg = SIGConfigTable.GetItem(pillId);
                if (used > 0 && pCfg != null && pCfg.QiGrant > 0 && ld.CurQi >= ld.MaxQi - 0.05)
                {
                    break;
                }

                string msg;
                if (CultivationLogic.UseCultivationPill(_engine.Save, ld, pillId, _engine.EventBus, out msg))
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
                var pCfg = SIGConfigTable.GetItem(pillId);
                int left = _engine.Save.Sect.GetItemCount(pillId);
                UiFeedbackService.PushSuccess(string.Format("已连续服用 {0} 枚【{1}】（剩余库存 {2}）。",
                    used, pCfg != null ? pCfg.NameCn : pillId, left));
            }
            else
            {
                UiFeedbackService.ReportAction(used > 0, lastMsg);
            }
            RefreshView();
        }

        public void RefreshView()
        {
            var save = _engine.Save;
            if (save == null) return;
            var ld = save.Leader;
            ld.EnsureQuickSlotsInitialized();

            var rCfg = SIGConfigTable.GetRealm(ld.Realm);
            double curQiRate = CultivationLogic.GetQiRatePerSec(ld, save.Sect);
            double sectAura = SectBuildLogic.GetTotalSectAuraBonus(save.Sect);

            _lblLeaderRealmTitle.Text = string.Format("{0}  [{1}]",
                ld.Name, EnumTextMap.RealmFullCn(ld.Realm, ld.SubStage, rCfg.MaxSubStage));

            double remainRatio = (ld.MaxLifespanYears - ld.AgeYears) / Math.Max(1.0, ld.MaxLifespanYears);
            _lblLifespanAlert.Visible = (remainRatio < 0.15);
            if (_lblLifespanAlert.Visible)
            {
                _lblLifespanAlert.Text = string.Format("[▲寿元剩{0:F1}年]", Math.Max(0, ld.MaxLifespanYears - ld.AgeYears));
            }

            string statsLine = string.Format("战力:{0:F0} | 攻:{1:F1} | 防:{2:F1} | 吸血:{3:P1} | 宗门灵气:+{4:P0}",
                ld.EstimateCombatPower(), ld.BattleStats.Attack, ld.BattleStats.Defense, ld.BattleStats.LifestealRate, sectAura);
            if (_lblCoreStatsLine.Text != statsLine) _lblCoreStatsLine.Text = statsLine;

            _barHpMp.SetProgress(ld.BattleStats.CurHp / Math.Max(1.0, ld.BattleStats.MaxHp),
                string.Format("气血 HP: {0:F0}/{1:F0}   |   法力 MP: {2:F0}/{3:F0}",
                    ld.BattleStats.CurHp, ld.BattleStats.MaxHp, ld.BattleStats.CurMp, ld.BattleStats.MaxMp));

            _barQiPool.SetProgress(ld.CurQi / Math.Max(1.0, ld.MaxQi),
                string.Format("气海修为: {0:F0} / {1:F0}  (+{2:F1}/s)", ld.CurQi, ld.MaxQi, curQiRate));

            string bonusPillId;
            double winRate = CultivationLogic.GetBreakthroughSuccessRate(ld, save.Sect, out bonusPillId);
            bool canBreak = ld.CurQi >= ld.MaxQi - 0.01;
            _btnBreakthrough.Enabled = canBreak;

            if (ld.SubStage < rCfg.MaxSubStage)
            {
                _lblBreakthroughRate.Text = canBreak ? "★气海已满 (小境界100%)" : "小境界积蓄中 (100%)";
            }
            else
            {
                var pCfg = SIGConfigTable.GetItem(bonusPillId);
                _lblBreakthroughRate.Text = string.Format("大境界瓶颈 · 成率 {0:P0}{1}",
                    winRate, pCfg != null ? string.Format(" (含{0})", pCfg.GetShortLabel()) : "");
            }

            var wCfg = SIGConfigTable.GetItem(ld.EquippedWeaponId);
            var aCfg = SIGConfigTable.GetItem(ld.EquippedArmorId);
            var tCfg = SIGConfigTable.GetItem(ld.EquippedTalismanId);
            _btnWeaponSlot.Text = string.Format("飞剑：{0}", wCfg != null ? string.Format("{0}  (攻+{1}/速+{2})", wCfg.NameCn, wCfg.AtkBonus, wCfg.SpeedBonus) : "[未佩戴 · 双击选装 / 右键快换]");
            _btnArmorSlot.Text = string.Format("宝衣：{0}", aCfg != null ? string.Format("{0}  (防+{1}/血+{2})", aCfg.NameCn, aCfg.DefBonus, aCfg.HpBonus) : "[未穿戴 · 双击选装 / 右键快换]");
            _btnTalismanSlot.Text = string.Format("法宝：{0}", tCfg != null ? string.Format("{0}  (主动技:{1})", tCfg.NameCn, tCfg.ActiveSkillName) : "[未祭炼 · 双击选装 / 右键快换]");

            for (int i = 0; i < 3; i++)
            {
                string pid = ld.QuickCultivationPills[i];
                var item = SIGConfigTable.GetItem(pid);
                string txt = item == null
                    ? string.Format("[槽{0}] 空\n右键绑定", i + 1)
                    : string.Format("[槽{0}] {1}\n库存 ×{2}", i + 1, item.GetShortLabel(), save.Sect.GetItemCount(pid));
                if (_btnQuickPillSlots[i].Text != txt) _btnQuickPillSlots[i].Text = txt;
            }

            double curAtk = ld.BattleStats.Attack;
            double curDef = ld.BattleStats.Defense;
            double curHp = ld.BattleStats.MaxHp;
            string origTech = ld.EquippedTechniqueId;

            var techTiles = new List<CompactTileData>();
            foreach (var tech in SIGConfigTable.Current.Techniques)
            {
                int curLv = ld.GetTechniqueLevel(tech.Id);
                bool isEquipped = (origTech == tech.Id);

                // 无损测算转修该功法后的真实面板差值
                ld.EquippedTechniqueId = tech.Id;
                CultivationLogic.RecalculateStats(ld, save.Sect);
                double candQi = CultivationLogic.GetQiRatePerSec(ld, save.Sect);
                double candAtk = ld.BattleStats.Attack;
                double candDef = ld.BattleStats.Defense;
                double candHp = ld.BattleStats.MaxHp;

                ld.EquippedTechniqueId = origTech;
                CultivationLogic.RecalculateStats(ld, save.Sect);

                double upCost = curLv < tech.MaxLevel ? CultivationLogic.GetTechniqueUpgradeStoneCost(ld, tech) : 0;

                var rows = new List<InspectorRowData>
                {
                    new InspectorRowData("参悟层数", isEquipped ? "当前主修" : "备选心法", string.Format("Lv.{0} / {1}", curLv, tech.MaxLevel), 0),
                    new InspectorRowData("升层消耗", string.Format("存:{0:F0}石", save.Sect.GetResource(ResourceType.SpiritStone)),
                        curLv < tech.MaxLevel ? string.Format("需 {0:F0} 灵石", upCost) : "已圆满", 0),
                    BuildDeltaRow("吐纳速率", string.Format("{0:F1}/s", curQiRate), candQi - curQiRate, "/s", "F1"),
                    BuildDeltaRow("攻击面板", string.Format("{0:F1}", curAtk), candAtk - curAtk, "", "F1"),
                    BuildDeltaRow("防御面板", string.Format("{0:F1}", curDef), candDef - curDef, "", "F1"),
                    BuildDeltaRow("气血上限", string.Format("{0:F0}", curHp), candHp - curHp, "", "F0")
                };

                techTiles.Add(new CompactTileData
                {
                    Id = tech.Id,
                    ShortTitle = tech.GetShortLabel(),
                    SubValueTop = "L" + curLv,
                    SubValueBottom = "M" + tech.MaxLevel,
                    InspectorTitle = string.Format("{0} [{1}]  {2}", tech.NameCn, EnumTextMap.ElemCn(tech.GetElement()), isEquipped ? "■ 当前主修中（双击升层）" : "（双击切换为主修）"),
                    InspectorNote = tech.Desc,
                    InspectorRows = rows,
                    IsActiveEquipped = isEquipped
                });
            }
            _techTileGrid.SetItems(techTiles);
        }

        private static InspectorRowData BuildDeltaRow(string label, string curText, double diff, string unit, string fmt)
        {
            if (Math.Abs(diff) < 0.05)
            {
                return new InspectorRowData(label, curText, "±0" + unit, 0);
            }
            string sign = diff > 0 ? "+" : "";
            return new InspectorRowData(label, curText, sign + diff.ToString(fmt) + unit, diff > 0 ? 1 : -1);
        }
    }
}
