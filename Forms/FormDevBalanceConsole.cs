using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Forms.UIs;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms
{
    /// <summary>
    /// EXT-04 开发者数值平衡测试台（F12 呼出 / Esc 关闭，全模拟基于克隆快照，不污染真实存档，严格适配纯灰度办公皮肤）
    /// </summary>
    public class FormDevBalanceConsole : Form, IThemeableView
    {
        private sealed class ComboOptionItem
        {
            public string Key { get; set; }
            public string Label { get; set; }
            public int IntVal { get; set; }
            public RealmTier RealmVal { get; set; }

            public override string ToString()
            {
                return Label ?? "";
            }
        }

        private readonly SIGEngine _engine;

        private Label _lblBaselineSummary;
        private ComboBox _cmbInjectRealm;
        private ComboBox _cmbInjectSub;
        private CheckBox _chkFillQi;

        private ComboBox _cmbTrialCount;
        private CheckBox _chkInfiniteSimPills;
        private ComboBox _cmbBreakRealm;
        private CheckBox _chkUseBreakPill;
        private ComboBox _cmbSimStage;

        private DataGridView _gridStageResults;
        private TextBox _txtConsoleReport;
        private UcStatusFeedbackStrip _localStatusStrip;

        public FormDevBalanceConsole(SIGEngine engine)
        {
            _engine = engine;

            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Text = "开发者数值平衡测试台 (EXT-04)";
            this.Size = new Size(980, 620);
            this.MinimumSize = new Size(840, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = UiStyleHelper.NormalFont;

            BuildUI();
            PopulateSelectors();
            UiStyleHelper.ApplyThemeRecursive(this);
            RefreshBaselineInfo();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            if (keyData == Keys.F5)
            {
                RunAllStagesSweep();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BuildUI()
        {
            _localStatusStrip = new UcStatusFeedbackStrip(
                subscribeGlobalBus: false,
                defaultHintText: "F5:全秘境重跑  |  Esc:关闭窗口  |  千场模拟在克隆快照上执行，不修改真实存档");

            var splitRoot = UiStyleHelper.CreateProportionalSplit(
                Orientation.Horizontal, 0.43, 215, 220, "DevConsole.Root");
            splitRoot.Padding = new Padding(6, 6, 6, 4);

            // ==================== 上半区：左右双分组（快进注入 vs 概率跑表） ====================
            var tblTop = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 左分组：① 时间快进 + ② 资源与境界注入
            var grpInject = new GroupBox
            {
                Text = "【① 时间快进推演  &  ② 资源 / 境界 / 基建注入（作用于当前存档）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(0, 0, 4, 0)
            };

            var tblInject = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0)
            };
            tblInject.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblInject.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tblInject.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tblInject.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tblInject.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 左第1行：时间快进器
            var rowFastForward = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 30);
            rowFastForward.Controls.Add(MakeInlineLabel("时间快进:"));
            var btnFf1M = UiStyleHelper.CreateButton("快进1月", (s, e) => DoFastForward(1), UiButtonRole.Default, true);
            var btnFf1Y = UiStyleHelper.CreateButton("快进1年", (s, e) => DoFastForward(12), UiButtonRole.PrimaryBlue, true);
            var btnFf10Y = UiStyleHelper.CreateButton("快进10年", (s, e) => DoFastForward(120), UiButtonRole.PrimaryBlue, true);
            var btnFf100Y = UiStyleHelper.CreateButton("快进100年", (s, e) => DoFastForward(1200), UiButtonRole.WarningAmber, true);
            UiStyleHelper.SetToolTip(btnFf1M, "无延迟推演 1 个仙历月的资源收支、吐纳、丹器与秘境连战");
            UiStyleHelper.SetToolTip(btnFf1Y, "无延迟推演 1 年（12 个仙历月）");
            UiStyleHelper.SetToolTip(btnFf10Y, "无延迟推演 10 年（120 个仙历月）");
            UiStyleHelper.SetToolTip(btnFf100Y, "无延迟推演 100 年（1200 个仙历月，期间暂不强行打断触发兽潮）");
            rowFastForward.Controls.AddRange(new Control[] { btnFf1M, btnFf1Y, btnFf10Y, btnFf100Y });

            // 左第2行：资源与物品发放
            var rowRes = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 30);
            rowRes.Controls.Add(MakeInlineLabel("测试发放:"));
            var btnRes1k = UiStyleHelper.CreateButton("全资材+1000", (s, e) =>
            {
                string m;
                DevBalanceLogic.GrantAllResources(_engine.Save, 1000, _engine.EventBus, out m);
                PushConsoleLine(m, UiFeedbackLevel.Success);
            }, UiButtonRole.Default, true);
            var btnRes50k = UiStyleHelper.CreateButton("全资材+5万", (s, e) =>
            {
                string m;
                DevBalanceLogic.GrantAllResources(_engine.Save, 50000, _engine.EventBus, out m);
                PushConsoleLine(m, UiFeedbackLevel.Success);
            }, UiButtonRole.PositiveGreen, true);
            var btnItems = UiStyleHelper.CreateButton("全丹器+10", (s, e) =>
            {
                string m;
                DevBalanceLogic.GrantAllCatalogItems(_engine.Save, 10, _engine.EventBus, out m);
                PushConsoleLine(m, UiFeedbackLevel.Success);
            }, UiButtonRole.Default, true);
            var btnArts = UiStyleHelper.CreateButton("仙铸阵宝×5", (s, e) =>
            {
                string m;
                DevBalanceLogic.SpawnTestArtifactsFree(_engine.Save, _engine.Rng, 3, 5, _engine.EventBus, out m);
                PushConsoleLine(m, UiFeedbackLevel.Success);
            }, UiButtonRole.Default, true);
            UiStyleHelper.SetToolTip(btnRes50k, "全部宗门资材增加 50,000 并自动抬升仓库上限避免溢出");
            UiStyleHelper.SetToolTip(btnItems, "向乾坤储物阁发放配表中全部丹药、飞剑、宝衣与法宝各 10 件");
            UiStyleHelper.SetToolTip(btnArts, "免耗材直接生成 5 件天工仙铸高品随机词条阵宝放入宝库");
            rowRes.Controls.AddRange(new Control[] { btnRes1k, btnRes50k, btnItems, btnArts });

            // 左第3行：境界设定
            var rowRealm = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 32);
            rowRealm.Controls.Add(MakeInlineLabel("掌门境界:"));
            _cmbInjectRealm = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 96,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 3, 4, 1)
            };
            _cmbInjectSub = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 74,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(0, 3, 4, 1)
            };
            _chkFillQi = new CheckBox
            {
                Text = "满气海",
                Checked = true,
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 5, 4, 1)
            };
            var btnApplyRealm = UiStyleHelper.CreateButton("设定境界", (s, e) =>
            {
                var rOpt = _cmbInjectRealm.SelectedItem as ComboOptionItem;
                var sOpt = _cmbInjectSub.SelectedItem as ComboOptionItem;
                if (rOpt == null || sOpt == null) return;
                string m;
                DevBalanceLogic.SetLeaderRealmAndState(_engine.Save, rOpt.RealmVal, sOpt.IntVal, _chkFillQi.Checked, _engine.EventBus, out m);
                PushConsoleLine(m, UiFeedbackLevel.Success);
                RefreshBaselineInfo();
            }, UiButtonRole.PrimaryBlue, true);
            rowRealm.Controls.AddRange(new Control[] { _cmbInjectRealm, _cmbInjectSub, _chkFillQi, btnApplyRealm });

            // 左第4行：宗门设施与状态快捷拉满
            var rowFacility = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 30);
            rowFacility.Controls.Add(MakeInlineLabel("快捷拉满:"));
            var btnMaxAll = UiStyleHelper.CreateButton("满级功法/建筑/全解锁", (s, e) =>
            {
                string m;
                DevBalanceLogic.UnlockAndMaxAllFacilities(_engine.Save, _engine.EventBus, out m);
                PushConsoleLine(m, UiFeedbackLevel.Success);
                RefreshBaselineInfo();
            }, UiButtonRole.PositiveGreen, true);
            var btnFullRefill = UiStyleHelper.CreateButton("注满气海与状态", (s, e) =>
            {
                if (_engine.Save == null) return;
                _engine.Save.Leader.CurQi = _engine.Save.Leader.MaxQi;
                _engine.Save.Leader.BattleStats.CurHp = _engine.Save.Leader.BattleStats.MaxHp;
                _engine.Save.Leader.BattleStats.CurMp = _engine.Save.Leader.BattleStats.MaxMp;
                _engine.EventBus.NotifyStateChanged();
                PushConsoleLine("[测试台] 已将掌门气海修为、气血与法力恢复至满值。", UiFeedbackLevel.Success);
                RefreshBaselineInfo();
            }, UiButtonRole.Default, true);
            var btnTriggerRaid = UiStyleHelper.CreateButton("立即触发兽潮", (s, e) =>
            {
                CombatLogic.StartSectRaid(_engine.Save, _engine.EventBus);
                PushConsoleLine("[测试台] 已强制触发当前轮次护山兽潮攻城战。", UiFeedbackLevel.Warning);
                RefreshBaselineInfo();
            }, UiButtonRole.DangerRed, true);
            rowFacility.Controls.AddRange(new Control[] { btnMaxAll, btnFullRefill, btnTriggerRaid });

            tblInject.Controls.Add(rowFastForward, 0, 0);
            tblInject.Controls.Add(rowRes, 0, 1);
            tblInject.Controls.Add(rowRealm, 0, 2);
            tblInject.Controls.Add(rowFacility, 0, 3);
            grpInject.Controls.Add(tblInject);

            // 右分组：③ 渡劫突破千次实测 + ④ 秘境无延迟千场模拟
            var grpSim = new GroupBox
            {
                Text = "【③ 渡劫概率实测  &  ④ 秘境胜率跑表（克隆快照演算 · 零存档污染）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(4, 0, 0, 0)
            };

            var tblSim = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0)
            };
            tblSim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblSim.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tblSim.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tblSim.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tblSim.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 右第1行：样本数与无限战丹开关
            var rowSimCfg = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 30);
            rowSimCfg.Controls.Add(MakeInlineLabel("模拟场次:"));
            _cmbTrialCount = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 84,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 3, 8, 1)
            };
            _chkInfiniteSimPills = new CheckBox
            {
                Text = "模拟备足自动战丹（测算单场期望耗丹数）",
                Checked = true,
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 5, 2, 1)
            };
            rowSimCfg.Controls.AddRange(new Control[] { _cmbTrialCount, _chkInfiniteSimPills });

            // 右第2行：渡劫千次实测
            var rowBreakSim = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 30);
            rowBreakSim.Controls.Add(MakeInlineLabel("突破实测:"));
            _cmbBreakRealm = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 112,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 3, 6, 1)
            };
            _chkUseBreakPill = new CheckBox
            {
                Text = "携推荐破境丹",
                Checked = true,
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 5, 6, 1)
            };
            var btnRunBreak = UiStyleHelper.CreateButton("运行突破统计", (s, e) => RunBreakthroughSimulation(), UiButtonRole.PrimaryBlue, true);
            UiStyleHelper.SetToolTip(btnRunBreak, "基于当前掌门悟性、气运与护山大阵加成，执行 N 次独立渡劫抽样统计");
            rowBreakSim.Controls.AddRange(new Control[] { _cmbBreakRealm, _chkUseBreakPill, btnRunBreak });

            // 右第3行：秘境千场模拟
            var rowStageSim = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 32);
            rowStageSim.Controls.Add(MakeInlineLabel("秘境跑表:"));
            _cmbSimStage = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 128,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 3, 6, 1)
            };
            var btnRunOneStage = UiStyleHelper.CreateButton("单关千场模拟", (s, e) => RunSingleStageSimulation(), UiButtonRole.PrimaryBlue, true);
            var btnRunAllStages = UiStyleHelper.CreateButton("全秘境横评跑表", (s, e) => RunAllStagesSweep(), UiButtonRole.PositiveGreen, true);
            var btnClearReport = UiStyleHelper.CreateButton("清空输出", (s, e) =>
            {
                _gridStageResults.Rows.Clear();
                _txtConsoleReport.Clear();
                _localStatusStrip.SetFeedback(UiFeedbackLevel.Info, "已清空模拟输出表格与报告区。");
            }, UiButtonRole.Default, true);
            UiStyleHelper.SetToolTip(btnRunAllStages, "一键遍历全部秘境关卡各跑 N 场真实 ATB 战斗，生成完整梯度胜率表 — 快捷键: F5");
            rowStageSim.Controls.AddRange(new Control[] { _cmbSimStage, btnRunOneStage, btnRunAllStages, btnClearReport });

            // 右第4行：当前模拟基准快照提示
            _lblBaselineSummary = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Font = UiStyleHelper.SmallMonoReg,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(2, 2, 2, 0),
                Text = "当前模拟基准：读取中..."
            };

            tblSim.Controls.Add(rowSimCfg, 0, 0);
            tblSim.Controls.Add(rowBreakSim, 0, 1);
            tblSim.Controls.Add(rowStageSim, 0, 2);
            tblSim.Controls.Add(_lblBaselineSummary, 0, 3);
            grpSim.Controls.Add(tblSim);

            tblTop.Controls.Add(grpInject, 0, 0);
            tblTop.Controls.Add(grpSim, 1, 0);
            splitRoot.Panel1.Controls.Add(tblTop);

            // ==================== 下半区：左表格（秘境跑表） + 右文本（突破与快进明细） ====================
            var splitBottom = UiStyleHelper.CreateProportionalSplit(
                Orientation.Vertical, 0.58, 420, 300, "DevConsole.Bottom");

            var grpTable = new GroupBox
            {
                Text = "【秘境关卡模拟统计表（胜率 / 平均回合 / 战损 / 耗丹与掉落期望）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(6)
            };

            _gridStageResults = UiStyleHelper.CreateDoubleBufferedGrid();
            _gridStageResults.Columns.Add("ColStage", "秘境关卡");
            _gridStageResults.Columns.Add("ColWinRate", "胜率");
            _gridStageResults.Columns.Add("ColTurns", "均回合");
            _gridStageResults.Columns.Add("ColSec", "均耗时");
            _gridStageResults.Columns.Add("ColHp", "胜场余血");
            _gridStageResults.Columns.Add("ColPills", "均耗战丹");
            _gridStageResults.Columns.Add("ColStone", "均掉灵石");
            _gridStageResults.Columns.Add("ColCore", "均掉妖丹");
            _gridStageResults.Columns.Add("ColArt", "阵宝期望");

            _gridStageResults.Columns["ColStage"].FillWeight = 20;
            _gridStageResults.Columns["ColWinRate"].FillWeight = 11;
            _gridStageResults.Columns["ColTurns"].FillWeight = 10;
            _gridStageResults.Columns["ColSec"].FillWeight = 10;
            _gridStageResults.Columns["ColHp"].FillWeight = 11;
            _gridStageResults.Columns["ColPills"].FillWeight = 10;
            _gridStageResults.Columns["ColStone"].FillWeight = 10;
            _gridStageResults.Columns["ColCore"].FillWeight = 9;
            _gridStageResults.Columns["ColArt"].FillWeight = 9;
            grpTable.Controls.Add(_gridStageResults);

            var grpText = new GroupBox
            {
                Text = "【突破概率实测与时间快进明细日志】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(6)
            };
            _txtConsoleReport = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiStyleHelper.MonoFont
            };
            grpText.Controls.Add(_txtConsoleReport);

            splitBottom.Panel1.Controls.Add(grpTable);
            splitBottom.Panel2.Controls.Add(grpText);
            splitRoot.Panel2.Controls.Add(splitBottom);

            this.Controls.Add(splitRoot);
            this.Controls.Add(_localStatusStrip);
        }

        private Label MakeInlineLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = UiStyleHelper.SmallBoldFont,
                Margin = new Padding(0, 6, 4, 0)
            };
        }

        private void PopulateSelectors()
        {
            _cmbTrialCount.Items.Clear();
            int[] counts = { 100, 500, 1000, 2000 };
            for (int i = 0; i < counts.Length; i++)
            {
                _cmbTrialCount.Items.Add(new ComboOptionItem { IntVal = counts[i], Label = counts[i] + " 场" });
            }
            _cmbTrialCount.SelectedIndex = 2; // 默认 1000 场

            _cmbInjectSub.Items.Clear();
            _cmbInjectSub.Items.Add(new ComboOptionItem { IntVal = 1, Label = "初期 (1)" });
            _cmbInjectSub.Items.Add(new ComboOptionItem { IntVal = 2, Label = "中期 (2)" });
            _cmbInjectSub.Items.Add(new ComboOptionItem { IntVal = 3, Label = "后期 (3)" });
            _cmbInjectSub.Items.Add(new ComboOptionItem { IntVal = 99, Label = "圆满 (Max)" });
            _cmbInjectSub.SelectedIndex = 3;

            _cmbInjectRealm.Items.Clear();
            _cmbBreakRealm.Items.Clear();
            foreach (RealmTier rt in Enum.GetValues(typeof(RealmTier)))
            {
                var cfg = SIGConfigTable.GetRealm(rt);
                _cmbInjectRealm.Items.Add(new ComboOptionItem { RealmVal = rt, Label = cfg.NameCn });
                if (rt < RealmTier.Immortal)
                {
                    _cmbBreakRealm.Items.Add(new ComboOptionItem { RealmVal = rt, Label = cfg.NameCn + "圆满" });
                }
            }
            if (_cmbInjectRealm.Items.Count > 0) _cmbInjectRealm.SelectedIndex = 0;
            if (_cmbBreakRealm.Items.Count > 0) _cmbBreakRealm.SelectedIndex = 0;

            _cmbSimStage.Items.Clear();
            foreach (var stg in SIGConfigTable.Current.Stages)
            {
                _cmbSimStage.Items.Add(new ComboOptionItem
                {
                    Key = stg.Id,
                    Label = string.Format("T{0} · {1}", stg.StageTier, stg.NameCn)
                });
            }
            if (_cmbSimStage.Items.Count > 0) _cmbSimStage.SelectedIndex = 0;
        }

        public void RefreshBaselineInfo()
        {
            if (this.IsDisposed || _engine == null || _engine.Save == null) return;
            var ld = _engine.Save.Leader;
            CultivationLogic.RecalculateAllCharacters(_engine.Save);
            var rCfg = SIGConfigTable.GetRealm(ld.Realm);
            int guardCnt = _engine.Save.Disciples.Count(d => d.AssignedJob == DiscipleJob.Guard);
            var fm = ArtifactFormationLogic.ComputeFormationSummary(_engine.Save.Sect);

            _lblBaselineSummary.Text = string.Format(
                "基准: {0} | 战力 {1:F0} (攻{2:F0}/防{3:F0}/血{4:F0}) | 随战剑卫 {5} 人 | 镶阵 {6}/9 件",
                EnumTextMap.RealmFullCn(ld.Realm, ld.SubStage, rCfg.MaxSubStage),
                ld.EstimateCombatPower(),
                ld.BattleStats.Attack,
                ld.BattleStats.Defense,
                ld.BattleStats.MaxHp,
                Math.Min(2, guardCnt),
                fm.SlottedCount);
        }

        private int GetSelectedTrials()
        {
            var opt = _cmbTrialCount.SelectedItem as ComboOptionItem;
            return opt != null ? opt.IntVal : 1000;
        }

        private void DoFastForward(int months)
        {
            var sw = Stopwatch.StartNew();
            var rep = _engine.FastForwardMonths(months);
            sw.Stop();

            var sb = new StringBuilder();
            sb.AppendFormat("【时间快进结算】 {0} ➔ {1}（共 {2} 个月，耗时 {3} ms）",
                rep.StartDateText, rep.EndDateText, rep.MonthsSimulated, sw.ElapsedMilliseconds).AppendLine();
            sb.AppendFormat("  • 掌门气海净增：+{0:F0}   |   秘境胜/负：+{1} / +{2}   |   新获词条阵宝：+{3}",
                rep.LeaderQiDelta, rep.ExpeditionWinsDelta, rep.ExpeditionDefeatsDelta, rep.ArtifactsGainedDelta).AppendLine();

            if (rep.ResourceDeltas.Count > 0)
            {
                var parts = new List<string>();
                foreach (var kv in rep.ResourceDeltas)
                {
                    parts.Add(string.Format("{0}:{1}{2:F0}",
                        EnumTextMap.ResCn(kv.Key),
                        kv.Value >= 0 ? "+" : "",
                        kv.Value));
                }
                sb.AppendLine("  • 资源净变动：" + string.Join("  |  ", parts.ToArray()));
            }
            else
            {
                sb.AppendLine("  • 资源净变动：各项收支基本持平或已达库容上限。");
            }

            AppendReportBlock(sb.ToString());
            string msg = string.Format("已完成 {0} 个仙历月快进推演（耗时 {1} ms），当前至 {2}。",
                months, sw.ElapsedMilliseconds, rep.EndDateText);
            _localStatusStrip.SetFeedback(UiFeedbackLevel.Success, msg);
            UiFeedbackService.PushSuccess(msg);
            RefreshBaselineInfo();
        }

        private void RunBreakthroughSimulation()
        {
            var opt = _cmbBreakRealm.SelectedItem as ComboOptionItem;
            if (opt == null) return;
            int trials = GetSelectedTrials();

            var sw = Stopwatch.StartNew();
            var rep = DevBalanceLogic.SimulateBreakthroughMonteCarlo(
                _engine.Save, opt.RealmVal, _chkUseBreakPill.Checked, trials, _engine.Rng);
            sw.Stop();

            AppendReportBlock(rep.FormattedDetail + string.Format("\r\n  • 模拟演算耗时：{0} ms", sw.ElapsedMilliseconds));
            _localStatusStrip.SetFeedback(UiFeedbackLevel.Success,
                string.Format("{0} 突破 {1} 次实测完成：理论 {2:P1} vs 实测 {3:P1}，均需 {4:F2} 次（{5} ms）。",
                    rep.TargetRealmName, rep.Trials, rep.TheoreticalRate, rep.ObservedFirstTryRate, rep.AvgAttemptsToSucceed, sw.ElapsedMilliseconds));
        }

        private void RunSingleStageSimulation()
        {
            var opt = _cmbSimStage.SelectedItem as ComboOptionItem;
            if (opt == null) return;
            int trials = GetSelectedTrials();

            RefreshBaselineInfo();
            var sw = Stopwatch.StartNew();
            var row = DevBalanceLogic.SimulateStageMonteCarlo(
                _engine.Save, opt.Key, trials, _chkInfiniteSimPills.Checked, _engine.Rng);
            sw.Stop();
            if (row == null) return;

            _gridStageResults.Rows.Clear();
            AppendStageGridRow(row);

            string summary = string.Format(
                "【{0}】{1} 场模拟完成（{2} ms）：胜率 {3:P1} | 均回合 {4:F1} | 胜场余血 {5:P0} | 均耗战丹 {6:F2} 枚",
                row.StageName, row.Trials, sw.ElapsedMilliseconds, row.WinRate, row.AvgTurns, row.AvgRemainHpRatio, row.AvgPillsConsumed);
            AppendReportBlock(summary);
            _localStatusStrip.SetFeedback(UiFeedbackLevel.Success, summary);
        }

        private void RunAllStagesSweep()
        {
            int trials = GetSelectedTrials();
            RefreshBaselineInfo();

            var sw = Stopwatch.StartNew();
            var rows = DevBalanceLogic.SimulateAllStagesSweep(
                _engine.Save, trials, _chkInfiniteSimPills.Checked, _engine.Rng);
            sw.Stop();

            _gridStageResults.Rows.Clear();
            var sb = new StringBuilder();
            sb.AppendFormat("【全秘境梯度横评】每关 {0} 场（共 {1} 场 ATB 演算，总耗时 {2} ms）：",
                trials, rows.Count * trials, sw.ElapsedMilliseconds).AppendLine();

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                AppendStageGridRow(r);
                sb.AppendFormat("  • T{0} {1,-8} | 胜率 {2,6:P1} | 均回合 {3,5:F1} | 余血 {4,4:P0} | 耗丹 {5,4:F2} | 灵石+{6:F0} 阵宝+{7:F2}",
                    r.StageTier, r.StageName, r.WinRate, r.AvgTurns, r.AvgRemainHpRatio, r.AvgPillsConsumed, r.AvgStoneDrop, r.AvgArtifactDrop).AppendLine();
            }

            AppendReportBlock(sb.ToString().TrimEnd());
            _localStatusStrip.SetFeedback(UiFeedbackLevel.Success,
                string.Format("全秘境横评完成（{0} 关 × {1} 场，耗时 {2} ms）。", rows.Count, trials, sw.ElapsedMilliseconds));
        }

        private void AppendStageGridRow(StageBattleMonteCarloRow r)
        {
            _gridStageResults.Rows.Add(
                string.Format("T{0}·{1}", r.StageTier, r.StageName),
                string.Format("{0:P1}", r.WinRate),
                string.Format("{0:F1}", r.AvgTurns),
                string.Format("{0:F1}s", r.AvgElapsedSec),
                r.Wins > 0 ? string.Format("{0:P0}", r.AvgRemainHpRatio) : "-",
                string.Format("{0:F2}", r.AvgPillsConsumed),
                string.Format("+{0:F0}", r.AvgStoneDrop),
                string.Format("+{0:F2}", r.AvgCoreDrop),
                string.Format("{0:P1}", r.AvgArtifactDrop));
        }

        private void PushConsoleLine(string message, UiFeedbackLevel lv)
        {
            AppendReportBlock(message);
            _localStatusStrip.SetFeedback(lv, message);
            UiFeedbackService.ReportAction(lv != UiFeedbackLevel.Blocked, message);
            RefreshBaselineInfo();
        }

        private void AppendReportBlock(string block)
        {
            if (string.IsNullOrEmpty(block)) return;
            if (_txtConsoleReport.TextLength > 12000)
            {
                _txtConsoleReport.Text = _txtConsoleReport.Text.Substring(_txtConsoleReport.TextLength - 6000);
            }
            if (_txtConsoleReport.TextLength > 0)
            {
                _txtConsoleReport.AppendText("\r\n------------------------------------------------------------\r\n");
            }
            _txtConsoleReport.AppendText(block);
            _txtConsoleReport.SelectionStart = _txtConsoleReport.TextLength;
            _txtConsoleReport.ScrollToCaret();
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            if (_lblBaselineSummary != null) _lblBaselineSummary.ForeColor = p.TextMuted;
            if (_txtConsoleReport != null)
            {
                _txtConsoleReport.BackColor = p.BgLogBox;
                _txtConsoleReport.ForeColor = p.TextPrimary;
            }
        }
    }
}
