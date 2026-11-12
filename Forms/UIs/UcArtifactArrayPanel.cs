using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIGTool.Datas;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// 页签5（v0.6 无冗余按钮精简 + UI-UX-02 紧凑筛选排序与批量注灵 + UI-UX-07 三列差值检查器）：
    /// • 移除九宫阵眼下方重复【镶嵌/取回/开辟】按钮与法宝库单项【锁定/拆解】按钮，全部统一由双击与右键菜单直达；
    /// • 右侧集成 UcFilterSortBar（品阶/五行/关键字/排序）与 UcBatchStepBar（支持批量连续注灵祭炼）。
    /// </summary>
    public class UcArtifactArrayPanel : UserControl, IThemeableView
    {
        private readonly SIGEngine _engine;

        private readonly List<InteractiveSlotButton> _eyeButtons = new List<InteractiveSlotButton>(9);
        private int _selectedEyeIndex = 4;

        private Label _lblFormationOverview;
        private Label _lblVaultCount;
        private UcBatchStepBar _batchStepBar;
        private UcFilterSortBar _filterSortBar;
        private UcCompactTileGrid _artifactTileGrid;
        private ContextMenuStrip _eyeContextMenu;

        public UcArtifactArrayPanel(SIGEngine engine)
        {
            _engine = engine;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            this.BackColor = UiStyleHelper.BgWindow;

            BuildUI();
            ApplyTheme();
        }

        public void FocusEye(int eyeIdx)
        {
            if (eyeIdx >= 0 && eyeIdx < 9)
            {
                _selectedEyeIndex = eyeIdx;
                RefreshView();
            }
        }

        public bool HandleKeyboardNavigation(Keys keyData)
        {
            bool eyeFocused = false;
            for (int i = 0; i < _eyeButtons.Count; i++)
            {
                if (_eyeButtons[i].Focused) { eyeFocused = true; break; }
            }

            Keys baseKey = keyData & Keys.KeyCode;
            bool ctrlNav = ((keyData & Keys.Control) == Keys.Control) &&
                           (baseKey == Keys.Left || baseKey == Keys.Right || baseKey == Keys.Up || baseKey == Keys.Down);

            if (eyeFocused || ctrlNav)
            {
                if (keyData == Keys.Enter)
                {
                    HandleEyeDoubleClicked(_selectedEyeIndex);
                    return true;
                }
                if (keyData == Keys.Apps || keyData == (Keys.F10 | Keys.Shift))
                {
                    var btn = _eyeButtons[_selectedEyeIndex];
                    ShowEyeContextMenu(_selectedEyeIndex, btn, new Point(8, btn.Height - 4));
                    return true;
                }
                int next = _selectedEyeIndex;
                if (baseKey == Keys.Left) next = Math.Max(0, _selectedEyeIndex - 1);
                else if (baseKey == Keys.Right) next = Math.Min(8, _selectedEyeIndex + 1);
                else if (baseKey == Keys.Up) next = Math.Max(0, _selectedEyeIndex - 3);
                else if (baseKey == Keys.Down) next = Math.Min(8, _selectedEyeIndex + 3);
                else return false;

                _selectedEyeIndex = next;
                RefreshView();
                if (eyeFocused) _eyeButtons[_selectedEyeIndex].Focus();
                return true;
            }

            return _artifactTileGrid != null && _artifactTileGrid.HandleKeyboardNavigation(keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _eyeContextMenu != null)
            {
                _eyeContextMenu.Dispose();
                _eyeContextMenu = null;
            }
            base.Dispose(disposing);
        }

        private void BuildUI()
        {
            var splitMain = UiStyleHelper.CreateProportionalSplit(Orientation.Vertical, 0.44, 390, 450, "Tab5.Main");

            var grpLeft = new GroupBox
            {
                Text = "【九宫护山大阵（双击阵眼=开辟/镶入/取回 · 右键=阵眼菜单）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8)
            };

            var tblLeft = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblFormationOverview = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.MonoFont,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(0, 0, 0, 6),
                Text = "护山大阵状态计算中..."
            };

            var tblNineGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Margin = new Padding(0)
            };
            UiStyleHelper.EnableDoubleBuffer(tblNineGrid);
            for (int c = 0; c < 3; c++) tblNineGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            for (int r = 0; r < 3; r++) tblNineGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));

            for (int idx = 0; idx < 9; idx++)
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
                    _selectedEyeIndex = captured;
                    RefreshView();
                };
                btn.DoubleClick += (s, e) =>
                {
                    _selectedEyeIndex = captured;
                    HandleEyeDoubleClicked(captured);
                };
                btn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        _selectedEyeIndex = captured;
                        RefreshView();
                        ShowEyeContextMenu(captured, btn, e.Location);
                    }
                };
                UiStyleHelper.SetToolTip(btn, "九宫护山阵眼：单击选中查看共鸣差值，双击开辟/镇入/取回，右键打开阵眼菜单（Ctrl+方向键可切换选中阵眼）");
                _eyeButtons.Add(btn);
                tblNineGrid.Controls.Add(btn, idx % 3, idx / 3);
            }

            tblLeft.Controls.Add(_lblFormationOverview, 0, 0);
            tblLeft.Controls.Add(tblNineGrid, 0, 1);
            grpLeft.Controls.Add(tblLeft);

            var grpRight = new GroupBox
            {
                Text = "【随机词条法宝库（双击=镇入/取回选中阵眼 · 右键=镇入任意阵眼/锁定/拆解）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8)
            };

            var tblRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlForgeRow = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 28);
            var btnForge1 = UiStyleHelper.CreateButton("凡铸(80石/35铁/1丹)", (s, e) => DoForgeBatch(1, _batchStepBar.SelectedBatch), UiButtonRole.Default, true);
            var btnForge2 = UiStyleHelper.CreateButton("灵铸(220石/80铁/4丹)", (s, e) => DoForgeBatch(2, _batchStepBar.SelectedBatch), UiButtonRole.PrimaryBlue, true);
            var btnForge3 = UiStyleHelper.CreateButton("仙铸(560石/180铁/10丹)", (s, e) => DoForgeBatch(3, _batchStepBar.SelectedBatch), UiButtonRole.WarningAmber, true);
            _batchStepBar = new UcBatchStepBar("连铸:");

            var btnBatchSalvage = UiStyleHelper.CreateButton("批拆未锁凡品", (s, e) =>
            {
                string msg;
                int cnt = ArtifactFormationLogic.BatchSalvageUnlockedCommon(_engine.Save, _engine.EventBus, out msg);
                UiFeedbackService.ReportAction(cnt > 0, msg);
                RefreshView();
            }, UiButtonRole.Default, true);

            _lblVaultCount = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallMonoReg,
                Margin = new Padding(6, 5, 0, 0),
                Text = "0/60"
            };

            UiStyleHelper.SetToolTip(btnForge1, "按右侧所选连铸批量执行凡铸注灵（主要产出凡品与灵品词条阵宝）");
            UiStyleHelper.SetToolTip(btnForge2, "按右侧所选连铸批量执行灵铸注灵（大幅提升灵品与宝品词条阵宝概率）");
            UiStyleHelper.SetToolTip(btnForge3, "按右侧所选连铸批量执行天工仙铸（高概率产出宝品与 4 词条仙品阵宝）");
            UiStyleHelper.SetToolTip(btnBatchSalvage, "一键拆解宝库中全部未上阵且未加锁的凡品阵宝，返还灵石与玄铁");
            pnlForgeRow.Controls.AddRange(new Control[] { btnForge1, btnForge2, btnForge3, _batchStepBar, btnBatchSalvage, _lblVaultCount });

            var rarGroups = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("ALL", "全品"),
                new KeyValuePair<string, string>("Common", "凡"),
                new KeyValuePair<string, string>("Spirit", "灵"),
                new KeyValuePair<string, string>("Treasure", "宝"),
                new KeyValuePair<string, string>("Immortal", "仙")
            };
            _filterSortBar = new UcFilterSortBar(
                rarGroups,
                showElementFilter: true,
                sortOptions: new string[] { "在阵/品阶", "阵眼共鸣优先", "词条数/阶位" });
            _filterSortBar.OnFilterChanged += () => RefreshView();

            _artifactTileGrid = new UcCompactTileGrid(footerHeight: 118, tileWidth: 146, tileHeight: 44);
            _artifactTileGrid.OnTileDoubleClicked += tile =>
            {
                if (_engine.Save == null) return;
                var art = _engine.Save.Sect.FindArtifactById(tile.Id);
                if (art != null && art.SlottedEyeIndex == _selectedEyeIndex)
                {
                    DoUnslotEye(_selectedEyeIndex);
                }
                else
                {
                    DoSlotArtifact(tile.Id, _selectedEyeIndex);
                }
            };
            _artifactTileGrid.ContextMenuBuilder = BuildArtifactContextMenu;

            tblRight.Controls.Add(pnlForgeRow, 0, 0);
            tblRight.Controls.Add(_filterSortBar, 0, 1);
            tblRight.Controls.Add(_artifactTileGrid, 0, 2);
            grpRight.Controls.Add(tblRight);

            splitMain.Panel1.Controls.Add(grpLeft);
            splitMain.Panel2.Controls.Add(grpRight);
            this.Controls.Add(splitMain);
        }

        private void HandleEyeDoubleClicked(int eyeIdx)
        {
            if (_engine.Save == null || eyeIdx < 0 || eyeIdx >= _engine.Save.Sect.FormationEyes.Count) return;
            var eye = _engine.Save.Sect.FormationEyes[eyeIdx];
            if (!eye.IsUnlocked)
            {
                DoUnlockEye(eyeIdx);
            }
            else if (!string.IsNullOrEmpty(eye.EquippedArtifactInstanceId))
            {
                DoUnslotEye(eyeIdx);
            }
            else
            {
                var sel = _artifactTileGrid.SelectedData;
                if (sel != null) DoSlotArtifact(sel.Id, eyeIdx);
            }
        }

        private void ShowEyeContextMenu(int eyeIdx, Control anchor, Point pt)
        {
            if (_engine.Save == null || eyeIdx < 0 || eyeIdx >= _engine.Save.Sect.FormationEyes.Count) return;
            if (_eyeContextMenu != null) { _eyeContextMenu.Dispose(); _eyeContextMenu = null; }

            var eye = _engine.Save.Sect.FormationEyes[eyeIdx];
            _eyeContextMenu = UiStyleHelper.CreateThemedContextMenu();

            if (!eye.IsUnlocked)
            {
                _eyeContextMenu.Items.Add(new ToolStripMenuItem(
                    string.Format("开辟【{0}】(需 {1:F0}灵石 / {2:F0}妖丹)", eye.PalaceName, eye.UnlockStoneCost, eye.UnlockCoreCost),
                    null,
                    (s, e) => DoUnlockEye(eyeIdx)));
            }
            else
            {
                var selRight = _artifactTileGrid.SelectedData;
                if (selRight != null)
                {
                    _eyeContextMenu.Items.Add(new ToolStripMenuItem(
                        string.Format("将右侧选中【{0}】镇入此阵眼", selRight.ShortTitle),
                        null,
                        (s, e) => DoSlotArtifact(selRight.Id, eyeIdx)));
                }

                if (!string.IsNullOrEmpty(eye.EquippedArtifactInstanceId))
                {
                    _eyeContextMenu.Items.Add(new ToolStripMenuItem(
                        "从此阵眼取回当前法宝",
                        null,
                        (s, e) => DoUnslotEye(eyeIdx)));
                }
            }

            UiStyleHelper.StyleContextMenu(_eyeContextMenu);
            _eyeContextMenu.Show(anchor, pt);
        }

        private ContextMenuStrip BuildArtifactContextMenu(CompactTileData tile)
        {
            if (tile == null || _engine.Save == null) return null;
            var sect = _engine.Save.Sect;
            var art = sect.FindArtifactById(tile.Id);
            if (art == null) return null;

            var menu = UiStyleHelper.CreateThemedContextMenu();
            var curEye = sect.FormationEyes[_selectedEyeIndex];

            menu.Items.Add(new ToolStripMenuItem(
                string.Format("镇入当前选中阵眼【{0}】", curEye.PalaceName),
                null,
                (s, e) => DoSlotArtifact(art.InstanceId, _selectedEyeIndex))
            {
                Enabled = curEye.IsUnlocked && art.SlottedEyeIndex != _selectedEyeIndex
            });

            var miAnyEye = new ToolStripMenuItem("指定镇入已辟阵眼");
            for (int i = 0; i < sect.FormationEyes.Count; i++)
            {
                var eye = sect.FormationEyes[i];
                if (!eye.IsUnlocked) continue;
                int capturedEye = i;
                string relTag;
                ArtifactFormationLogic.GetEyeResonanceMultiplier(eye, art, out relTag);
                var sub = new ToolStripMenuItem(
                    string.Format("{0} [{1}脉] -> {2}", eye.PalaceName, EnumTextMap.ElemShortCn(eye.AffinityElement), relTag),
                    null,
                    (s, e) => DoSlotArtifact(art.InstanceId, capturedEye))
                {
                    Checked = (art.SlottedEyeIndex == capturedEye)
                };
                miAnyEye.DropDownItems.Add(sub);
            }
            menu.Items.Add(miAnyEye);

            if (art.SlottedEyeIndex >= 0 && art.SlottedEyeIndex < sect.FormationEyes.Count)
            {
                int slottedIdx = art.SlottedEyeIndex;
                menu.Items.Add(new ToolStripMenuItem(
                    string.Format("从【{0}】取回法宝", sect.FormationEyes[slottedIdx].PalaceName),
                    null,
                    (s, e) => DoUnslotEye(slottedIdx)));
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(
                art.IsLockedProtect ? "解除锁定保护" : "锁定法宝 (防误拆)",
                null,
                (s, e) => ToggleArtifactLock(art.InstanceId)));

            double refSt, refOr, refCr;
            ArtifactFormationLogic.GetSalvageRefund(art, out refSt, out refOr, out refCr);
            menu.Items.Add(new ToolStripMenuItem(
                string.Format("熔炼拆解 (+{0:F0}石/+{1:F0}铁)", refSt, refOr),
                null,
                (s, e) => DoSalvageArtifact(art.InstanceId))
            {
                Enabled = (art.SlottedEyeIndex < 0 && !art.IsLockedProtect)
            });

            return menu;
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;
            if (_lblFormationOverview != null)
            {
                _lblFormationOverview.BackColor = p.BgCard;
                _lblFormationOverview.ForeColor = p.TextPrimary;
            }
            if (_lblVaultCount != null)
            {
                _lblVaultCount.ForeColor = p.TextMuted;
            }
            RefreshView();
        }

        private void DoForgeBatch(int grade, int batchCount)
        {
            if (_engine.Save == null) return;
            int forged = 0;
            string lastMsg = "";
            int maxLoop = Math.Max(1, Math.Min(60, batchCount));

            for (int i = 0; i < maxLoop; i++)
            {
                string msg;
                if (ArtifactFormationLogic.ForgeArtifactManual(_engine.Save, grade, _engine.Rng, _engine.EventBus, out msg))
                {
                    forged++;
                    lastMsg = msg;
                }
                else
                {
                    lastMsg = msg;
                    break;
                }
            }

            if (forged > 1)
            {
                UiFeedbackService.PushSuccess(string.Format("连续注灵完成：共祭炼出 {0} 件随机词条阵宝（末件：{1}）。", forged, lastMsg));
            }
            else
            {
                UiFeedbackService.ReportAction(forged > 0, lastMsg);
            }
            RefreshView();
        }

        private void DoSlotArtifact(string artId, int eyeIdx)
        {
            string msg;
            bool ok = ArtifactFormationLogic.SlotArtifactIntoEye(_engine.Save, eyeIdx, artId, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoUnslotEye(int eyeIdx)
        {
            string msg;
            bool ok = ArtifactFormationLogic.UnslotEye(_engine.Save, eyeIdx, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoUnlockEye(int eyeIdx)
        {
            string msg;
            bool ok = ArtifactFormationLogic.UnlockFormationEye(_engine.Save, eyeIdx, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void ToggleArtifactLock(string artId)
        {
            if (_engine.Save == null) return;
            var art = _engine.Save.Sect.FindArtifactById(artId);
            if (art == null) return;
            art.IsLockedProtect = !art.IsLockedProtect;
            UiFeedbackService.PushInfo(string.Format("法宝【{0}】已{1}。", art.NameCn, art.IsLockedProtect ? "设为锁定保护" : "解除锁定"));
            RefreshView();
        }

        private void DoSalvageArtifact(string artId)
        {
            if (_engine.Save == null) return;
            var art = _engine.Save.Sect.FindArtifactById(artId);
            if (art != null && !art.IsLockedProtect && art.SlottedEyeIndex < 0 && art.Rarity >= ArtifactRarity.Treasure)
            {
                var dr = MessageBox.Show(
                    string.Format("确定要熔炼拆解高品法宝【{0}】吗？", art.NameCn),
                    "高品法宝拆解确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes) return;
            }

            string msg;
            bool ok = ArtifactFormationLogic.SalvageSingleArtifact(_engine.Save, artId, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        public void RefreshView()
        {
            var save = _engine.Save;
            if (save == null || save.Sect == null) return;

            ArtifactFormationLogic.EnsureFormationInitialized(save.Sect);
            var sect = save.Sect;
            var p = UiStyleHelper.Current;

            var fm = ArtifactFormationLogic.ComputeFormationSummary(sect);
            var synSb = new StringBuilder();
            if (fm.HasSanCaiSynergy) synSb.Append("[三才护山] ");
            if (fm.HasWuXingSynergy) synSb.Append("[五行混元] ");
            if (fm.HasJiuGongSynergy) synSb.Append("[九宫太虚] ");
            string synText = synSb.Length > 0 ? synSb.ToString().Trim() : "未成阵(镶>=3激活三才，集齐五行激活混元)";

            var ovSb = new StringBuilder();
            ovSb.AppendLine(string.Format("在阵:{0}/9 (五行:{1}/5) | 连携:{2}",
                fm.SlottedCount, fm.DistinctElementCount, synText));
            ovSb.AppendLine(string.Format("战力加持 -> 攻:+{0:F0}(+{1:P0}) 防:+{2:F0}(+{3:P0}) 血:+{4:F0}(+{5:P0}) 暴:+{6:P1} 吸:+{7:P1}",
                fm.FlatAtk, fm.PctAtk, fm.FlatDef, fm.PctDef, fm.FlatHp, fm.PctHp, fm.CritRateAdd, fm.LifestealRateAdd));
            ovSb.Append(string.Format("宗门加持 -> 吐纳:+{0:P0} 结界:+{1:F0}(+{2:P0}) 渡劫率:+{3:P0}",
                fm.QiRatePctAdd, fm.BarrierFlatAdd, fm.BarrierPctAdd, fm.BreakthroughRateAdd));

            string ovText = ovSb.ToString();
            if (_lblFormationOverview.Text != ovText) _lblFormationOverview.Text = ovText;

            for (int i = 0; i < 9 && i < sect.FormationEyes.Count; i++)
            {
                var eye = sect.FormationEyes[i];
                var btn = _eyeButtons[i];
                bool isSel = (i == _selectedEyeIndex);

                btn.FlatAppearance.BorderColor = isSel ? p.TileSelectedBorder : p.BorderStrong;

                string btnTxt;
                Color bg;
                Color fg;

                if (!eye.IsUnlocked)
                {
                    btnTxt = string.Format("[待辟] {0} [{1}]\n双击开辟 ({2:F0}石/{3:F0}丹)",
                        eye.PalaceName, EnumTextMap.ElemShortCn(eye.AffinityElement), eye.UnlockStoneCost, eye.UnlockCoreCost);
                    bg = p.SlotLockedBg;
                    fg = p.TextMuted;
                }
                else
                {
                    var slottedArt = sect.FindArtifactById(eye.EquippedArtifactInstanceId);
                    if (slottedArt == null)
                    {
                        btnTxt = string.Format("{0} [{1}脉]\n○ 空置阵眼",
                            eye.PalaceName, EnumTextMap.ElemShortCn(eye.AffinityElement));
                        bg = isSel ? p.SlotEmptySelBg : p.BgCard;
                        fg = p.TextPrimary;
                    }
                    else
                    {
                        string relTag;
                        ArtifactFormationLogic.GetEyeResonanceMultiplier(eye, slottedArt, out relTag);
                        btnTxt = string.Format("■ {0} [{1}脉]\n{2}·{3} ({4}词)\n{5}",
                            eye.PalaceName,
                            EnumTextMap.ElemShortCn(eye.AffinityElement),
                            EnumTextMap.RarityShortCn(slottedArt.Rarity),
                            slottedArt.ShortName,
                            slottedArt.Affixes.Count,
                            relTag);
                        bg = isSel ? p.SlotBuiltSelBg : p.SlotBuiltBg;
                        fg = p.SlotBuiltFg;
                    }
                }

                if (btn.Text != btnTxt) btn.Text = btnTxt;
                if (btn.BackColor != bg) btn.BackColor = bg;
                if (btn.ForeColor != fg) btn.ForeColor = fg;
            }

            var selEye = sect.FormationEyes[_selectedEyeIndex];
            string cntTxt = string.Format("库存:{0}/{1}", sect.ArtifactVault.Count, sect.ArtifactVaultCap);
            if (_lblVaultCount.Text != cntTxt) _lblVaultCount.Text = cntTxt;

            // 应用 UI-UX-02 筛选与排序
            var filtered = new List<ArtifactInstanceData>();
            string grpKey = _filterSortBar.ActiveGroupKey;
            ElementType? elemFilter = _filterSortBar.ActiveElement;
            string kw = _filterSortBar.Keyword;

            for (int i = 0; i < sect.ArtifactVault.Count; i++)
            {
                var a = sect.ArtifactVault[i];
                if (grpKey != "ALL" && !string.Equals(a.Rarity.ToString(), grpKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (elemFilter.HasValue && a.Element != elemFilter.Value)
                    continue;
                if (!string.IsNullOrEmpty(kw))
                {
                    bool hitName = a.NameCn.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool hitAff = a.Affixes.Any(af => af.FormatAffixLine(1.0).IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!hitName && !hitAff) continue;
                }
                filtered.Add(a);
            }

            int sortIdx = _filterSortBar.SortIndex;
            filtered.Sort((a, b) =>
            {
                if (sortIdx == 1)
                {
                    string tA, tB;
                    double mA = ArtifactFormationLogic.GetEyeResonanceMultiplier(selEye, a, out tA);
                    double mB = ArtifactFormationLogic.GetEyeResonanceMultiplier(selEye, b, out tB);
                    int mCmp = mB.CompareTo(mA);
                    if (mCmp != 0) return mCmp;
                }
                else if (sortIdx == 2)
                {
                    int afCmp = b.Affixes.Count.CompareTo(a.Affixes.Count);
                    if (afCmp != 0) return afCmp;
                    int trCmp = b.Tier.CompareTo(a.Tier);
                    if (trCmp != 0) return trCmp;
                }

                int slotCmp = (b.SlottedEyeIndex >= 0 ? 1 : 0).CompareTo(a.SlottedEyeIndex >= 0 ? 1 : 0);
                if (slotCmp != 0) return slotCmp;
                int rarCmp = ((int)b.Rarity).CompareTo((int)a.Rarity);
                if (rarCmp != 0) return rarCmp;
                return b.Tier.CompareTo(a.Tier);
            });

            var tiles = new List<CompactTileData>(filtered.Count);
            for (int i = 0; i < filtered.Count; i++)
            {
                var art = filtered[i];
                bool isSlotted = (art.SlottedEyeIndex >= 0 && art.SlottedEyeIndex < sect.FormationEyes.Count);
                string whereText = isSlotted
                    ? sect.FormationEyes[art.SlottedEyeIndex].PalaceName
                    : "库中待命";

                string targetResTag;
                double targetResMult = ArtifactFormationLogic.GetEyeResonanceMultiplier(selEye, art, out targetResTag);
                int resSign = targetResMult > 1.05 ? 1 : (targetResMult < 0.98 ? -1 : 0);

                double refSt, refOr, refCr;
                ArtifactFormationLogic.GetSalvageRefund(art, out refSt, out refOr, out refCr);

                var affSb = new StringBuilder();
                for (int k = 0; k < art.Affixes.Count; k++)
                {
                    if (k > 0) affSb.Append(" | ");
                    affSb.Append(art.Affixes[k].FormatAffixLine(1.0));
                }

                string rawTitle = art.IsLockedProtect ? ("[锁]" + art.ShortName) : art.ShortName;
                string previewDelta = ArtifactFormationLogic.PreviewSlotDeltaText(save, _selectedEyeIndex, art);
                int powSign = previewDelta.Contains("战力:+") ? 1 : (previewDelta.Contains("战力:-") ? -1 : 0);

                var rows = new List<InspectorRowData>
                {
                    new InspectorRowData("品阶/状态", string.Format("{0}·{1}阶", EnumTextMap.RarityCn(art.Rarity), art.Tier),
                        whereText + (art.IsLockedProtect ? " [锁]" : ""), 0),
                    new InspectorRowData("目标阵眼", selEye.PalaceName, targetResTag, resSign),
                    new InspectorRowData("白值(共鸣后)", string.Format("x{0:F2}", targetResMult),
                        string.Format("攻+{0:F0} 防+{1:F0} 血+{2:F0}", art.BaseAtk * targetResMult, art.BaseDef * targetResMult, art.BaseHp * targetResMult), 1),
                    new InspectorRowData("拆解返还", art.IsLockedProtect ? "已锁防拆" : "右键可拆",
                        string.Format("+{0:F0}石 / +{1:F0}铁", refSt, refOr), 0),
                    new InspectorRowData("副词条", art.Affixes.Count + " 条", affSb.ToString(), 1),
                    new InspectorRowData("镇入预估", "", previewDelta.Replace("镇入【" + selEye.PalaceName + "】差值 ➔ ", ""), powSign)
                };

                tiles.Add(new CompactTileData
                {
                    Id = art.InstanceId,
                    ShortTitle = rawTitle,
                    SubValueTop = EnumTextMap.RarityShortCn(art.Rarity) + "·" + EnumTextMap.ElemShortCn(art.Element),
                    SubValueBottom = art.Affixes.Count + "词",
                    InspectorTitle = string.Format("【{0}】 ({1}系)  {2}", art.NameCn, EnumTextMap.ElemShortCn(art.Element),
                        art.SlottedEyeIndex == _selectedEyeIndex ? "■ 当前已在此阵眼（双击取回）" : "（双击镇入选中阵眼，右键选任意阵眼）"),
                    InspectorRows = rows,
                    IsActiveEquipped = isSlotted
                });
            }

            _artifactTileGrid.SetItems(tiles);
        }
    }
}
