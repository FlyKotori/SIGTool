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
    /// 页签7（SYS-03 动态修仙界演化 · 九州浮动坊市 · 多分支奇遇事件链）：
    /// • 左上：非打断【天机奇遇待决台】，清晰列出当前奇遇叙事与 2~3 个标明消耗/成率/预期收益的抉择按钮；
    /// • 左下：【九州岁运与周边势力演化】，双缓冲表格呈现五大宗门兴衰与邦交档位（双击/右键行即可遣使结交或威慑）；
    /// • 右侧：【九州浮动坊市】，复用 UcCompactTileGrid + UcBatchStepBar + 底部三列差值检查器（双击/Enter=按批量采购，右键/Delete=按批量抛售）。
    /// </summary>
    public class UcWorldEvolutionPanel : UserControl, IThemeableView
    {
        private readonly SIGEngine _engine;

        // 左上：天机奇遇待决台
        private Label _lblEncounterStatus;
        private Label _lblEncounterNarrative;
        private readonly Button[] _optButtons = new Button[3];
        private string _activeEncounterUid = "";

        // 左下：九州岁运与周边宗门演化
        private DoubleBufferedStatusLabel _lblTrendBanner;
        private DataGridView _gridFactions;
        private ContextMenuStrip _factionMenu;
        private string _lastFactionSig = "";

        // 右侧：九州浮动坊市
        private UcBatchStepBar _batchStepBar;
        private DoubleBufferedStatusLabel _lblMarketHeader;
        private UcCompactTileGrid _marketTileGrid;

        public UcWorldEvolutionPanel(SIGEngine engine)
        {
            _engine = engine;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            this.BackColor = UiStyleHelper.BgWindow;

            BuildUI();
            ApplyTheme();
        }

        public void FocusPendingEncounter()
        {
            RefreshView();
            if (_optButtons[0] != null && _optButtons[0].Visible && _optButtons[0].Enabled)
            {
                _optButtons[0].Focus();
            }
        }

        public bool TriggerEncounterOptionByIndex(int optionIndex)
        {
            if (_engine.Save == null || _engine.Save.World == null) return false;
            var pending = _engine.Save.World.PendingEncounters.FirstOrDefault();
            if (pending == null) return false;

            var evtCfg = SIGConfigTable.GetEncounterEvent(pending.EventId);
            if (evtCfg == null || evtCfg.Options == null || optionIndex < 0 || optionIndex >= evtCfg.Options.Count)
                return false;

            DoChooseOption(pending.InstanceUid, optionIndex);
            return true;
        }

        public bool HandleKeyboardNavigation(Keys keyData)
        {
            if (keyData == Keys.Delete || keyData == Keys.Back)
            {
                string selId = _marketTileGrid != null ? _marketTileGrid.SelectedId : "";
                if (!string.IsNullOrEmpty(selId))
                {
                    DoSellListing(selId, _batchStepBar != null ? _batchStepBar.SelectedBatch : 1);
                    return true;
                }
            }
            return _marketTileGrid != null && _marketTileGrid.HandleKeyboardNavigation(keyData);
        }

        private void BuildUI()
        {
            var splitMain = UiStyleHelper.CreateProportionalSplit(
                Orientation.Vertical, 0.47, 420, 440, "Tab7.Main");

            var splitLeft = UiStyleHelper.CreateProportionalSplit(
                Orientation.Horizontal, 0.49, 195, 195, "Tab7.Left");

            // ==================== 左上：天机奇遇待决台 ====================
            var grpEncounter = new GroupBox
            {
                Text = "【天机奇遇待决台（非打断入队 · 标明成率与收益）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8, 6, 8, 6)
            };

            var tblEnc = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0)
            };
            tblEnc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblEnc.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblEnc.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblEnc.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tblEnc.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tblEnc.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            _lblEncounterStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.SmallMonoReg,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "待决奇遇: 0/3"
            };

            _lblEncounterNarrative = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.NormalFont,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(0, 2, 0, 4),
                Text = "山门清静，静候天机..."
            };

            for (int i = 0; i < 3; i++)
            {
                int idxCapture = i;
                var btn = UiStyleHelper.CreateButton("选项 " + (i + 1), (s, e) =>
                {
                    if (!string.IsNullOrEmpty(_activeEncounterUid))
                    {
                        DoChooseOption(_activeEncounterUid, idxCapture);
                    }
                }, i == 0 ? UiButtonRole.PrimaryBlue : UiButtonRole.Default, false);
                btn.AutoSize = false;
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(6, 0, 6, 0);
                btn.Margin = new Padding(0, 1, 0, 1);
                _optButtons[i] = btn;
                tblEnc.Controls.Add(btn, 0, 2 + i);
            }

            tblEnc.Controls.Add(_lblEncounterStatus, 0, 0);
            tblEnc.Controls.Add(_lblEncounterNarrative, 0, 1);
            grpEncounter.Controls.Add(tblEnc);

            // ==================== 左下：九州岁运与周边势力演化 ====================
            var grpFactions = new GroupBox
            {
                Text = "【九州岁运天象 & 周边宗门兴衰（双击/右键行遣使往来）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8, 6, 8, 6)
            };

            var tblFac = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblFac.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblFac.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblFac.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblTrendBanner = new DoubleBufferedStatusLabel(UiStyleHelper.SmallMonoReg, hasBorder: true, useCardBg: true)
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(0, 0, 0, 6),
                Text = "今岁天象读取中..."
            };

            _gridFactions = UiStyleHelper.CreateDoubleBufferedGrid();
            _gridFactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "势力宗门", FillWeight = 20F });
            _gridFactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPros", HeaderText = "兴盛", FillWeight = 11F });
            _gridFactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColRel", HeaderText = "邦交档位", FillWeight = 18F });
            _gridFactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColBonus", HeaderText = "交好 / 盟友常驻加成", FillWeight = 26F });
            _gridFactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColNote", HeaderText = "近期演化动向", FillWeight = 25F });
            UiStyleHelper.SetToolTip(_gridFactions, "双击任意势力行或右键打开菜单，可耗费灵石遣使结交（对魔宗则为演武震慑/通商缓和）");

            _gridFactions.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _gridFactions.Rows.Count) return;
                string fid = Convert.ToString(_gridFactions.Rows[e.RowIndex].Tag);
                DoDiplomacy(fid);
            };

            _gridFactions.CellMouseUp += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0 || e.RowIndex >= _gridFactions.Rows.Count) return;
                _gridFactions.ClearSelection();
                _gridFactions.Rows[e.RowIndex].Selected = true;
                string fid = Convert.ToString(_gridFactions.Rows[e.RowIndex].Tag);
                ShowFactionContextMenu(fid, Cursor.Position);
            };

            tblFac.Controls.Add(_lblTrendBanner, 0, 0);
            tblFac.Controls.Add(_gridFactions, 0, 1);
            grpFactions.Controls.Add(tblFac);

            splitLeft.Panel1.Controls.Add(grpEncounter);
            splitLeft.Panel2.Controls.Add(grpFactions);

            // ==================== 右侧：九州浮动坊市 ====================
            var grpMarket = new GroupBox
            {
                Text = "【九州浮动坊市（双击/Enter=采购入库 · 右键/Delete=抛售变现）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8, 6, 8, 6)
            };

            var tblMkt = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblMkt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMkt.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            tblMkt.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var barMkt = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 28);
            _batchStepBar = new UcBatchStepBar("交易批量:");
            UiStyleHelper.SetToolTip(_batchStepBar, "控制双击采购或右键/Delete抛售时的单次交易批数（自动按灵石或库存上限截断）");

            _lblMarketHeader = new DoubleBufferedStatusLabel(UiStyleHelper.SmallMonoReg, hasBorder: false, useCardBg: false)
            {
                AutoSize = false,
                Size = new Size(390, 24),
                Padding = new Padding(4, 0, 0, 0),
                Margin = new Padding(6, 2, 0, 0),
                Text = "灵石: 0 | 下季调价: 余 3 月"
            };
            barMkt.Controls.AddRange(new Control[] { _batchStepBar, _lblMarketHeader });

            _marketTileGrid = new UcCompactTileGrid(footerHeight: 122, tileWidth: 148, tileHeight: 44);
            _marketTileGrid.OnTileDoubleClicked += tile =>
            {
                DoBuyListing(tile.Id, _batchStepBar.SelectedBatch);
            };
            _marketTileGrid.ContextMenuBuilder = BuildMarketContextMenu;

            tblMkt.Controls.Add(barMkt, 0, 0);
            tblMkt.Controls.Add(_marketTileGrid, 0, 1);
            grpMarket.Controls.Add(tblMkt);

            splitMain.Panel1.Controls.Add(splitLeft);
            splitMain.Panel2.Controls.Add(grpMarket);
            this.Controls.Add(splitMain);
        }

        private void DoChooseOption(string instanceUid, int optionIdx)
        {
            if (_engine.Save == null) return;
            string msg;
            bool ok = WorldEvolutionLogic.ResolveEncounterOption(
                _engine.Save, instanceUid, optionIdx, _engine.Rng, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoDiplomacy(string factionId)
        {
            if (string.IsNullOrEmpty(factionId) || _engine.Save == null) return;
            string msg;
            bool ok = WorldEvolutionLogic.TryFactionDiplomacy(_engine.Save, factionId, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoBuyListing(string listingId, int batch)
        {
            if (_engine.Save == null) return;
            string msg;
            bool ok = WorldEvolutionLogic.TryBuyMarketListing(_engine.Save, listingId, batch, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoSellListing(string listingId, int batch)
        {
            if (_engine.Save == null) return;
            string msg;
            bool ok = WorldEvolutionLogic.TrySellMarketListing(_engine.Save, listingId, batch, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void ShowFactionContextMenu(string factionId, Point screenPos)
        {
            if (string.IsNullOrEmpty(factionId) || _engine.Save == null) return;
            var cfg = SIGConfigTable.GetWorldFaction(factionId);
            var rt = WorldEvolutionLogic.FindFaction(_engine.Save, factionId);
            if (cfg == null || rt == null) return;

            if (_factionMenu != null) _factionMenu.Dispose();
            _factionMenu = UiStyleHelper.CreateThemedContextMenu();

            string actLabel = cfg.AlignmentKey == "Demonic"
                ? string.Format("遣使演武震慑 / 通商【{0}】(耗 {1:F0} 灵石 · 关系+18)", cfg.NameCn, cfg.DiplomacyStoneCost)
                : string.Format("遣使备礼结交【{0}】(耗 {1:F0} 灵石 · 好感+16)", cfg.NameCn, cfg.DiplomacyStoneCost);

            bool canAct = rt.DiplomacyCooldownMonths <= 0 &&
                          rt.Relation < 100 &&
                          _engine.Save.Sect.GetResource(ResourceType.SpiritStone) >= cfg.DiplomacyStoneCost;

            if (rt.DiplomacyCooldownMonths > 0)
            {
                actLabel = string.Format("【{0}】邦交冷却中（余 {1} 个月）", cfg.NameCn, rt.DiplomacyCooldownMonths);
            }

            _factionMenu.Items.Add(new ToolStripMenuItem(actLabel, null, (s, e) => DoDiplomacy(factionId))
            {
                Enabled = canAct
            });
            _factionMenu.Items.Add(new ToolStripSeparator());
            _factionMenu.Items.Add(new ToolStripMenuItem("专精说明：" + cfg.FriendlyBonusDesc) { Enabled = false });
            _factionMenu.Show(screenPos);
        }

        private ContextMenuStrip BuildMarketContextMenu(CompactTileData tile)
        {
            if (tile == null || _engine.Save == null) return null;
            var cfg = SIGConfigTable.GetMarketListing(tile.Id);
            var slot = _engine.Save.World.MarketSlots.FirstOrDefault(x => x.ListingId == tile.Id);
            if (cfg == null || slot == null) return null;

            var menu = UiStyleHelper.CreateThemedContextMenu();
            double unitBuy = WorldEvolutionLogic.GetUnitBuyStone(_engine.Save, cfg, slot);
            double unitSell = WorldEvolutionLogic.GetUnitSellStone(_engine.Save, cfg, slot);
            int ownedBatches = WorldEvolutionLogic.GetOwnedTradableBatches(_engine.Save, cfg);
            int selBatch = _batchStepBar != null ? _batchStepBar.SelectedBatch : 1;

            bool canBuy = slot.RemainingBuyStock > 0 && _engine.Save.Sect.GetResource(ResourceType.SpiritStone) >= unitBuy;
            menu.Items.Add(new ToolStripMenuItem(
                string.Format("采购 1 批【{0}】(耗 {1:F0} 灵石 · 当季余 {2})", cfg.ShortName, unitBuy, slot.RemainingBuyStock),
                null,
                (s, e) => DoBuyListing(cfg.Id, 1))
            {
                Enabled = canBuy
            });

            if (selBatch > 1)
            {
                string batchLbl = selBatch >= 999 ? "Max" : ("×" + selBatch);
                menu.Items.Add(new ToolStripMenuItem(
                    string.Format("按当前批量 ({0}) 采购【{1}】", batchLbl, cfg.ShortName),
                    null,
                    (s, e) => DoBuyListing(cfg.Id, selBatch))
                {
                    Enabled = canBuy
                });
            }

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(new ToolStripMenuItem(
                string.Format("抛售 1 批【{0}】换取 +{1:F0} 灵石 (库存可售 {2} 批)", cfg.ShortName, unitSell, ownedBatches),
                null,
                (s, e) => DoSellListing(cfg.Id, 1))
            {
                Enabled = ownedBatches > 0
            });

            if (ownedBatches > 1)
            {
                int sellQty = Math.Min(ownedBatches, selBatch);
                menu.Items.Add(new ToolStripMenuItem(
                    string.Format("按批量抛售 {0} 批【{1}】(共得 +{2:F0} 灵石)", sellQty, cfg.ShortName, unitSell * sellQty),
                    null,
                    (s, e) => DoSellListing(cfg.Id, selBatch))
                {
                    Enabled = true
                });
            }

            return menu;
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;

            if (_lblEncounterStatus != null) _lblEncounterStatus.ForeColor = p.TextMuted;
            if (_lblEncounterNarrative != null)
            {
                _lblEncounterNarrative.BackColor = p.BgCard;
                _lblEncounterNarrative.ForeColor = p.TextPrimary;
            }
            if (_lblTrendBanner != null)
            {
                _lblTrendBanner.BackColor = p.BgCard;
                _lblTrendBanner.ForeColor = p.TextPrimary;
            }
            if (_lblMarketHeader != null) _lblMarketHeader.ForeColor = p.TextMuted;
            if (_gridFactions != null) UiStyleHelper.ApplyGridTheme(_gridFactions);
            if (_marketTileGrid != null) _marketTileGrid.ApplyTheme();
        }

        public void RefreshView()
        {
            if (_engine == null || _engine.Save == null) return;
            var save = _engine.Save;
            WorldEvolutionLogic.EnsureWorldInitialized(save);
            var w = save.World;

            RefreshEncounterDeck(save, w);
            RefreshWorldTrendAndFactions(save, w);
            RefreshMarketGrid(save, w);
        }

        private void RefreshEncounterDeck(SIGSaveData save, WorldEvolutionStateData w)
        {
            string buffSummary = "无阶段气运";
            if (w.TempBuffs != null && w.TempBuffs.Count > 0)
            {
                var bParts = w.TempBuffs.Select(b => string.Format("{0}({1}月)", b.BuffName, b.RemainingMonths)).ToArray();
                buffSummary = string.Join(" / ", bParts);
            }

            string statusTxt = string.Format("待决机缘: {0}/{1}  |  已历奇遇: {2} 桩  |  气运: {3}",
                w.PendingEncounters.Count, WorldEvolutionLogic.MaxPendingEncounters, w.TotalResolvedEncounters, buffSummary);
            if (_lblEncounterStatus.Text != statusTxt) _lblEncounterStatus.Text = statusTxt;

            var cur = w.PendingEncounters.FirstOrDefault();
            if (cur == null)
            {
                _activeEncounterUid = "";
                string chainHint = (w.ScheduledFollowups != null && w.ScheduledFollowups.Count > 0)
                    ? string.Format("\r\n● 因果牵引：尚有 {0} 桩前缘后续将在近期应验（最快 {1} 个月后）。",
                        w.ScheduledFollowups.Count, w.ScheduledFollowups.Min(f => f.RemainingMonths))
                    : "";
                string latestChronicle = (w.WorldChronicles != null && w.WorldChronicles.Count > 0)
                    ? ("\r\n● 最新天机：" + w.WorldChronicles[0])
                    : "";

                string idleText = string.Format(
                    "当前暂无待决奇遇（挂机期间随仙历月推进或大境界突破自动入队，绝不弹窗打断修行）。{0}{1}",
                    chainHint, latestChronicle);
                if (_lblEncounterNarrative.Text != idleText) _lblEncounterNarrative.Text = idleText;

                for (int i = 0; i < 3; i++)
                {
                    _optButtons[i].Visible = false;
                }
                return;
            }

            var cfg = SIGConfigTable.GetEncounterEvent(cur.EventId);
            if (cfg == null)
            {
                w.PendingEncounters.Remove(cur);
                return;
            }

            _activeEncounterUid = cur.InstanceUid;
            string headTag = cfg.ChainTotal > 1
                ? string.Format("[连续奇遇链 {0}/{1}]", cfg.ChainStep, cfg.ChainTotal)
                : (cfg.TriggerTypeKey == "RealmMilestone" ? "[大道境界机缘]" : "[九州游历奇遇]");

            string narText = string.Format("{0} 《{1}》 （触发于 {2}）\r\n{3}",
                headTag, cfg.TitleCn, cur.TriggerDateText, cfg.NarrativeDesc);
            if (_lblEncounterNarrative.Text != narText) _lblEncounterNarrative.Text = narText;

            for (int i = 0; i < 3; i++)
            {
                var btn = _optButtons[i];
                if (cfg.Options == null || i >= cfg.Options.Count)
                {
                    btn.Visible = false;
                    continue;
                }

                var opt = cfg.Options[i];
                string blockReason;
                bool canPick = WorldEvolutionLogic.CanChooseEncounterOption(save, opt, out blockReason);
                double effRate = WorldEvolutionLogic.GetEffectiveOptionSuccessRate(save, opt);

                string rateStr = effRate >= 0.999 ? "100%稳妥" : string.Format("成率{0:P0}", effRate);
                string btnLabel = canPick
                    ? string.Format("▶ {0}   [{1} · {2} ➔ {3}]", opt.OptionTitle, opt.RiskTag, rateStr, opt.PreviewGainText)
                    : string.Format("[受限:{0}] {1}   ({2})", blockReason, opt.OptionTitle, opt.PreviewGainText);

                btn.Visible = true;
                btn.Enabled = canPick;
                if (btn.Text != btnLabel) btn.Text = btnLabel;
                UiStyleHelper.SetToolTip(btn, string.Format(
                    "选项：{0}\r\n风险与消耗：{1}（算入掌门机缘后实际成功率 {2:P0}）\r\n预期收益：{3}\r\n快捷键：本页签下按 Alt+{4} 可直接择定",
                    opt.OptionTitle, opt.RiskTag, effRate, opt.PreviewGainText, i + 1));
            }
        }

        private void RefreshWorldTrendAndFactions(SIGSaveData save, WorldEvolutionStateData w)
        {
            double disc = WorldEvolutionLogic.GetTianjiTradeDiscount(save);
            string banner = string.Format(
                "今岁天象: 【{0}】(余 {1} 月更替) — {2}\r\n邦交与气运总加成: 全宗灵气 +{3:P0}  |  全宗攻伐 +{4:P0}  |  坊市税惠 {5:P0}",
                WorldEvolutionLogic.TrendNameCn(w.CurrentTrend),
                w.MonthsUntilTrendShift,
                WorldEvolutionLogic.TrendEffectDesc(w.CurrentTrend),
                save.Sect.WorldAuraBonusCache,
                save.Sect.WorldAtkPctBonusCache,
                disc);
            if (_lblTrendBanner.Text != banner) _lblTrendBanner.Text = banner;

            var sigSb = new StringBuilder();
            for (int i = 0; i < w.Factions.Count; i++)
            {
                var f = w.Factions[i];
                sigSb.Append(f.FactionId).Append(':').Append(f.Prosperity).Append('/')
                     .Append(f.Relation).Append('/').Append(f.DiplomacyCooldownMonths)
                     .Append('/').Append(f.RecentTrendNote).Append('|');
            }
            string sig = sigSb.ToString();
            if (sig == _lastFactionSig) return;
            _lastFactionSig = sig;

            string prevSelId = null;
            if (_gridFactions.SelectedRows.Count > 0)
            {
                prevSelId = Convert.ToString(_gridFactions.SelectedRows[0].Tag);
            }

            _gridFactions.Rows.Clear();
            for (int i = 0; i < w.Factions.Count; i++)
            {
                var f = w.Factions[i];
                var cfg = SIGConfigTable.GetWorldFaction(f.FactionId);
                if (cfg == null) continue;

                string cdTail = f.DiplomacyCooldownMonths > 0 ? string.Format(" [冷{0}月]", f.DiplomacyCooldownMonths) : "";
                int rIdx = _gridFactions.Rows.Add(
                    cfg.NameCn,
                    f.Prosperity.ToString(),
                    string.Format("{0} ({1}){2}", WorldEvolutionLogic.RelationTierCn(f.Relation), f.Relation, cdTail),
                    cfg.FriendlyBonusDesc,
                    f.RecentTrendNote);
                _gridFactions.Rows[rIdx].Tag = f.FactionId;
                if (prevSelId != null && prevSelId == f.FactionId)
                {
                    _gridFactions.Rows[rIdx].Selected = true;
                }
            }
        }

        private void RefreshMarketGrid(SIGSaveData save, WorldEvolutionStateData w)
        {
            double stone = save.Sect.GetResource(ResourceType.SpiritStone);
            double disc = WorldEvolutionLogic.GetTianjiTradeDiscount(save);
            string hdr = string.Format("灵石库存:{0} | 下季补货:余{1}月 | 商盟税惠:{2:P0}",
                UiStyleHelper.PadInt(stone, 6), UiStyleHelper.PadInt(w.MonthsUntilMarketRefresh, 2), disc);
            if (_lblMarketHeader.Text != hdr) _lblMarketHeader.Text = hdr;

            int selBatch = _batchStepBar != null ? _batchStepBar.SelectedBatch : 1;
            var tiles = new List<CompactTileData>();

            for (int i = 0; i < w.MarketSlots.Count; i++)
            {
                var slot = w.MarketSlots[i];
                var cfg = SIGConfigTable.GetMarketListing(slot.ListingId);
                if (cfg == null) continue;

                double unitBuy = WorldEvolutionLogic.GetUnitBuyStone(save, cfg, slot);
                double unitSell = WorldEvolutionLogic.GetUnitSellStone(save, cfg, slot);
                int ownedBatches = WorldEvolutionLogic.GetOwnedTradableBatches(save, cfg);

                string arrow = slot.PriceMultiplier >= 1.08 ? "▲" : (slot.PriceMultiplier <= 0.92 ? "▼" : "─");
                string topTxt = string.Format("买{0:F0} {1}{2:F2}x", unitBuy, arrow, slot.PriceMultiplier);
                string botTxt = string.Format("存:{0}批 余:{1}", ownedBatches, slot.RemainingBuyStock);

                int effBuyBatch = slot.RemainingBuyStock > 0
                    ? Math.Max(1, Math.Min(selBatch, slot.RemainingBuyStock))
                    : 0;
                int effSellBatch = ownedBatches > 0
                    ? Math.Max(1, Math.Min(selBatch, ownedBatches))
                    : 0;

                int priceSign = slot.PriceMultiplier <= 0.92 ? 1 : (slot.PriceMultiplier >= 1.15 ? -1 : 0);
                var rows = new List<InspectorRowData>
                {
                    new InspectorRowData(
                        "当季行情指数",
                        string.Format("{0:F2}x (基准买 {1:F0} / 卖 {2:F0})", slot.PriceMultiplier, cfg.BaseBuyStone, cfg.BaseSellStone),
                        slot.RemainingBuyStock > 0 ? string.Format("当季可购余量 {0}/{1} 批", slot.RemainingBuyStock, cfg.QuarterMaxStock) : "当季配额已售罄",
                        priceSign),
                    new InspectorRowData(
                        "采购入库试算",
                        string.Format("实付单价 {0:F0} 灵石/批", unitBuy),
                        effBuyBatch > 0 ? string.Format("双击购 {0} 批 ➔ 耗 {1:F0} 灵石", effBuyBatch, unitBuy * effBuyBatch) : "等待下季补货",
                        stone >= unitBuy && slot.RemainingBuyStock > 0 ? 1 : -1),
                    new InspectorRowData(
                        "抛售变现试算",
                        string.Format("回收单价 {0:F0} 灵石/批", unitSell),
                        effSellBatch > 0 ? string.Format("右键/Del售 {0} 批 ➔ 得 +{1:F0} 灵石", effSellBatch, unitSell * effSellBatch) : "当前无可售整批库存",
                        effSellBatch > 0 ? 1 : 0),
                    new InspectorRowData(
                        "库存与商路优惠",
                        string.Format("本宗现存可售 {0} 批", ownedBatches),
                        disc > 0 ? string.Format("天机商盟税惠已生效 ({0:P0})", disc) : "结交天机商盟可享买卖优待",
                        disc > 0 ? 1 : 0)
                };

                tiles.Add(new CompactTileData
                {
                    Id = cfg.Id,
                    ShortTitle = cfg.GetShortLabel(),
                    SubValueTop = topTxt,
                    SubValueBottom = botTxt,
                    IsActiveEquipped = (slot.PriceMultiplier <= 0.88 && slot.RemainingBuyStock > 0),
                    IsLocked = (slot.RemainingBuyStock <= 0),
                    InspectorTitle = string.Format("【{0}】  行情 {1}{2:F2}x", cfg.NameCn, arrow, slot.PriceMultiplier),
                    InspectorNote = cfg.Desc,
                    InspectorRows = rows,
                    DetailText = cfg.Desc
                });
            }

            _marketTileGrid.SetItems(tiles);
        }
    }
}
