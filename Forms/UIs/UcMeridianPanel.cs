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
    /// 页签6（SYS-02 经脉穴位树系统 · 纯灰度清洁办公流）：
    /// • 左侧内置无闪烁 TreeView 呈现督脉/任脉/冲脉/带脉层级脉络与小周天/大周天共鸣里程碑；
    /// • 右侧复用紧凑多列格子 UcCompactTileGrid + 底部三列键值差值检查器 UcKeyValueInspector + 批量连冲步进条（无冗余重复按钮，双击/回车即冲穴，右键打开快捷菜单）。
    /// </summary>
    public class UcMeridianPanel : UserControl, IThemeableView
    {
        private readonly SIGEngine _engine;

        private Label _lblMeridianHeader;
        private TreeView _treeMeridians;
        private Label _lblMilestoneBox;

        private UcBatchStepBar _batchStepBar;
        private Label _lblLiveResources;
        private UcCompactTileGrid _acupointTileGrid;

        private string _branchFilterKey = "ALL"; // ALL / DuMai / RenMai / ChongMai / DaiMai
        private bool _suppressTreeSelect = false;
        private string _lastTreeSignature = "";

        private TreeNode _rootAllNode;
        private readonly Dictionary<MeridianBranchType, TreeNode> _branchNodes = new Dictionary<MeridianBranchType, TreeNode>();
        private readonly Dictionary<string, TreeNode> _acuTreeNodes = new Dictionary<string, TreeNode>();

        public UcMeridianPanel(SIGEngine engine)
        {
            _engine = engine;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            this.BackColor = UiStyleHelper.BgWindow;

            BuildUI();
            InitStaticTreeStructure();
            ApplyTheme();
        }

        public void FocusAcupoint(string acupointId)
        {
            if (string.IsNullOrEmpty(acupointId)) return;
            _branchFilterKey = "ALL";
            _acupointTileGrid.SelectedId = acupointId;
            RefreshView();
        }

        public bool HandleKeyboardNavigation(Keys keyData)
        {
            if (_treeMeridians != null && _treeMeridians.Focused)
            {
                if (keyData == Keys.Enter && _treeMeridians.SelectedNode != null)
                {
                    string tag = Convert.ToString(_treeMeridians.SelectedNode.Tag);
                    if (!string.IsNullOrEmpty(tag) && tag.StartsWith("ACU:"))
                    {
                        string acuId = tag.Substring(4);
                        DoImpulseBatch(acuId, _batchStepBar != null ? _batchStepBar.SelectedBatch : 1);
                        return true;
                    }
                }
                return false;
            }
            return _acupointTileGrid != null && _acupointTileGrid.HandleKeyboardNavigation(keyData);
        }

        private void BuildUI()
        {
            var splitMain = UiStyleHelper.CreateProportionalSplit(
                Orientation.Vertical, 0.39, 340, 440, "Tab6.Main");

            // ==================== 左侧：奇经脉络层级树与周天共鸣总纲 ====================
            var grpLeft = new GroupBox
            {
                Text = "【奇经周天脉络树（单击脉络筛选 · 双击穴位直接冲穴）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(8)
            };

            var tblLeft = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            tblLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));

            _lblMeridianHeader = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.MonoFont,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(0, 0, 0, 6),
                Text = "经脉周天读取中..."
            };

            _treeMeridians = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = UiStyleHelper.NormalFont,
                HideSelection = false,
                FullRowSelect = true,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                ItemHeight = 22,
                Margin = new Padding(0, 0, 0, 6)
            };
            UiStyleHelper.SetToolTip(_treeMeridians, "单击脉络分类可过滤右侧穴位格；单击具体穴位同步查看右侧差值；双击穴位节点直接催动真元冲穴");

            _treeMeridians.AfterSelect += (s, e) =>
            {
                if (_suppressTreeSelect || e.Node == null) return;
                string tag = Convert.ToString(e.Node.Tag);
                if (tag == "ALL")
                {
                    _branchFilterKey = "ALL";
                    RefreshView();
                }
                else if (tag != null && tag.StartsWith("BR:"))
                {
                    _branchFilterKey = tag.Substring(3);
                    RefreshView();
                }
                else if (tag != null && tag.StartsWith("ACU:"))
                {
                    string acuId = tag.Substring(4);
                    var cfg = SIGConfigTable.GetAcupoint(acuId);
                    if (cfg != null && _branchFilterKey != "ALL" && _branchFilterKey != cfg.BranchKey)
                    {
                        _branchFilterKey = "ALL";
                    }
                    _acupointTileGrid.SelectedId = acuId;
                    RefreshView();
                }
            };

            _treeMeridians.NodeMouseDoubleClick += (s, e) =>
            {
                if (e.Node == null) return;
                string tag = Convert.ToString(e.Node.Tag);
                if (tag != null && tag.StartsWith("ACU:"))
                {
                    string acuId = tag.Substring(4);
                    DoImpulseBatch(acuId, _batchStepBar != null ? _batchStepBar.SelectedBatch : 1);
                }
            };

            _lblMilestoneBox = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.SmallMonoReg,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(0),
                Text = "周天里程碑计算中..."
            };

            tblLeft.Controls.Add(_lblMeridianHeader, 0, 0);
            tblLeft.Controls.Add(_treeMeridians, 0, 1);
            tblLeft.Controls.Add(_lblMilestoneBox, 0, 2);
            grpLeft.Controls.Add(tblLeft);

            // ==================== 右侧：紧凑穴位矩阵 + 批量连冲步进 + 三列键值差值检查器 ====================
            var grpRight = new GroupBox
            {
                Text = "【诸穴贯通矩阵（双击/Enter=按上限冲穴 · 右键=快捷菜单 · 失败自动累加+8%保底成率）】",
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
            tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlTopBar = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 28);
            _batchStepBar = new UcBatchStepBar("连冲上限:");
            UiStyleHelper.SetToolTip(_batchStepBar, "双击未贯通穴位时的最大连续尝试次数（一旦成功贯通或真元/灵材不足会自动停手）");

            var btnShowAll = UiStyleHelper.CreateButton("显示全24穴", (s, e) =>
            {
                _branchFilterKey = "ALL";
                RefreshView();
            }, UiButtonRole.Default, true);
            UiStyleHelper.SetToolTip(btnShowAll, "取消左侧单脉筛选，显示全部四脉 24 处要穴");

            _lblLiveResources = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallMonoReg,
                Margin = new Padding(8, 5, 0, 0),
                Text = "真元: 0/0"
            };

            pnlTopBar.Controls.AddRange(new Control[] { _batchStepBar, btnShowAll, _lblLiveResources });

            _acupointTileGrid = new UcCompactTileGrid(footerHeight: 120, tileWidth: 148, tileHeight: 44);
            _acupointTileGrid.OnTileDoubleClicked += tile =>
            {
                DoImpulseBatch(tile.Id, _batchStepBar.SelectedBatch);
            };
            _acupointTileGrid.ContextMenuBuilder = BuildAcupointContextMenu;

            tblRight.Controls.Add(pnlTopBar, 0, 0);
            tblRight.Controls.Add(_acupointTileGrid, 0, 1);
            grpRight.Controls.Add(tblRight);

            splitMain.Panel1.Controls.Add(grpLeft);
            splitMain.Panel2.Controls.Add(grpRight);
            this.Controls.Add(splitMain);
        }

        private void InitStaticTreeStructure()
        {
            _treeMeridians.BeginUpdate();
            _treeMeridians.Nodes.Clear();
            _branchNodes.Clear();
            _acuTreeNodes.Clear();

            _rootAllNode = new TreeNode("全部奇经总览 (0/24)") { Tag = "ALL" };
            _treeMeridians.Nodes.Add(_rootAllNode);

            MeridianBranchType[] branches =
            {
                MeridianBranchType.DuMai,
                MeridianBranchType.RenMai,
                MeridianBranchType.ChongMai,
                MeridianBranchType.DaiMai
            };

            for (int b = 0; b < branches.Length; b++)
            {
                var br = branches[b];
                var brNode = new TreeNode(MeridianLogic.BranchNameCn(br))
                {
                    Tag = "BR:" + br.ToString()
                };
                _branchNodes[br] = brNode;
                _rootAllNode.Nodes.Add(brNode);

                var nodes = SIGConfigTable.Current.Acupoints
                    .Where(x => x.GetBranch() == br)
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                for (int i = 0; i < nodes.Count; i++)
                {
                    var acu = nodes[i];
                    var child = new TreeNode(acu.NameCn)
                    {
                        Tag = "ACU:" + acu.Id
                    };
                    _acuTreeNodes[acu.Id] = child;
                    brNode.Nodes.Add(child);
                }
                brNode.Expand();
            }

            _rootAllNode.Expand();
            _treeMeridians.EndUpdate();
        }

        private void DoImpulseBatch(string acuId, int maxTries)
        {
            if (_engine.Save == null || _engine.Save.Leader == null) return;
            string msg;
            bool ok = MeridianLogic.TryImpulseBatchUntilUnlocked(
                _engine.Save, _engine.Save.Leader, acuId, maxTries, _engine.Rng, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private ContextMenuStrip BuildAcupointContextMenu(CompactTileData tile)
        {
            if (tile == null || _engine.Save == null) return null;
            var node = SIGConfigTable.GetAcupoint(tile.Id);
            if (node == null) return null;

            var ld = _engine.Save.Leader;
            var menu = UiStyleHelper.CreateThemedContextMenu();
            bool unlocked = MeridianLogic.IsUnlocked(ld, node.Id);

            if (unlocked)
            {
                menu.Items.Add(new ToolStripMenuItem("■ 该穴位已彻底贯通") { Enabled = false });
            }
            else
            {
                string blockReason;
                bool canTry = MeridianLogic.CanAttemptImpulse(_engine.Save, ld, node, out blockReason);
                double rate = MeridianLogic.GetEffectiveImpulseRate(ld, node);

                menu.Items.Add(new ToolStripMenuItem(
                    canTry
                        ? string.Format("冲击【{0}】 1 次 (成率 {1:P0} · {2})", node.NameCn, rate, MeridianLogic.FormatCostLine(node))
                        : string.Format("暂不可冲：{0}", blockReason),
                    null,
                    (s, e) => DoImpulseBatch(node.Id, 1))
                {
                    Enabled = canTry
                });

                menu.Items.Add(new ToolStripMenuItem(
                    string.Format("连续冲穴直至贯通【{0}】(最多尝试 10 次)", node.NameCn),
                    null,
                    (s, e) => DoImpulseBatch(node.Id, 10))
                {
                    Enabled = canTry
                });
            }

            menu.Items.Add(new ToolStripSeparator());
            string brKey = node.BranchKey;
            if (_branchFilterKey != brKey)
            {
                menu.Items.Add(new ToolStripMenuItem(
                    "仅筛选显示：" + MeridianLogic.BranchNameCn(node.GetBranch()),
                    null,
                    (s, e) =>
                    {
                        _branchFilterKey = brKey;
                        RefreshView();
                    }));
            }
            if (_branchFilterKey != "ALL")
            {
                menu.Items.Add(new ToolStripMenuItem(
                    "显示全部四脉 24 穴",
                    null,
                    (s, e) =>
                    {
                        _branchFilterKey = "ALL";
                        RefreshView();
                    }));
            }

            return menu;
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;
            if (_lblMeridianHeader != null)
            {
                _lblMeridianHeader.BackColor = p.BgCard;
                _lblMeridianHeader.ForeColor = p.TextPrimary;
            }
            if (_lblMilestoneBox != null)
            {
                _lblMilestoneBox.BackColor = p.BgCard;
                _lblMilestoneBox.ForeColor = p.TextMuted;
            }
            if (_lblLiveResources != null)
            {
                _lblLiveResources.ForeColor = p.TextMuted;
            }
            if (_treeMeridians != null)
            {
                _treeMeridians.BackColor = p.BgCard;
                _treeMeridians.ForeColor = p.TextPrimary;
                _treeMeridians.LineColor = p.BorderStrong;
            }
            if (_acupointTileGrid != null)
            {
                _acupointTileGrid.ApplyTheme();
            }
        }

        public void RefreshView()
        {
            if (_engine == null || _engine.Save == null || _engine.Save.Leader == null) return;
            var save = _engine.Save;
            var ld = save.Leader;
            var sect = save.Sect;
            MeridianLogic.EnsureMeridianInitialized(ld);

            var sum = MeridianLogic.ComputeMeridianSummary(ld);
            string ztState = sum.HasDaZhouTian ? "★乾坤大周天圆满"
                : (sum.HasXiaoZhouTian ? "★任督小周天贯通" : "奇经筑脉中");

            string headerTxt = string.Format(
                "已贯通穴位: {0}/{1}  |  周天境况: {2}\r\n功法独立乘区: +{3:P0}  |  吐纳速率: +{4:P0}  |  渡劫加成: +{5:P1}",
                sum.UnlockedCount, sum.TotalCount, ztState,
                sum.TechMultiplierAdd, sum.QiRatePctAdd, sum.BreakthroughRateAdd);
            if (_lblMeridianHeader.Text != headerTxt) _lblMeridianHeader.Text = headerTxt;

            string milestoneTxt = string.Format(
                "[{0}] 督脉满(6/6): 功法乘区+12% / 攻+10%    [{1}] 任脉满(6/6): 吐纳+18% / 气海+15% / 寿+40\r\n" +
                "[{2}] 冲脉满(6/6): 血防+15% / 吸血+3%       [{3}] 带脉满(6/6): 暴击+5% / 速+12 / 渡劫+6%\r\n" +
                "[{4}] 任督小周天: 吐纳+20% / 功法+10%       [{5}] 乾坤大周天: 攻防血+18% / 吐纳+30% / 渡劫+8%",
                sum.DuMaiComplete ? "■" : "○",
                sum.RenMaiComplete ? "■" : "○",
                sum.ChongMaiComplete ? "■" : "○",
                sum.DaiMaiComplete ? "■" : "○",
                sum.HasXiaoZhouTian ? "■" : "○",
                sum.HasDaZhouTian ? "■" : "○");
            if (_lblMilestoneBox.Text != milestoneTxt) _lblMilestoneBox.Text = milestoneTxt;

            string liveResTxt = string.Format(
                "掌门可用真元: {0:F0}/{1:F0}  |  灵石: {2:F0}  |  灵草: {3:F0}  |  妖丹: {4:F0}",
                ld.CurQi, ld.MaxQi,
                sect.GetResource(ResourceType.SpiritStone),
                sect.GetResource(ResourceType.SpiritHerb),
                sect.GetResource(ResourceType.MonsterCore));
            if (_lblLiveResources.Text != liveResTxt) _lblLiveResources.Text = liveResTxt;

            UpdateTreeNodesInPlace(save, ld, sum);
            RebuildAcupointTiles(save, ld, sect);
        }

        private void UpdateTreeNodesInPlace(SIGSaveData save, CharacterData ld, MeridianBonusSummary sum)
        {
            var sigSb = new StringBuilder();
            sigSb.Append(sum.UnlockedCount).Append('|').Append((int)ld.Realm).Append('|');

            var readyIds = new HashSet<string>();
            foreach (var acu in SIGConfigTable.Current.Acupoints)
            {
                string dummy;
                bool ready = MeridianLogic.CanAttemptImpulse(save, ld, acu, out dummy);
                if (ready) readyIds.Add(acu.Id);
                int pity = MeridianLogic.GetFailPityCount(ld, acu.Id);
                sigSb.Append(MeridianLogic.IsUnlocked(ld, acu.Id) ? '1' : (ready ? 'R' : '0')).Append(pity).Append(',');
            }

            string sig = sigSb.ToString();
            if (sig == _lastTreeSignature) return;
            _lastTreeSignature = sig;

            _suppressTreeSelect = true;
            _treeMeridians.BeginUpdate();
            try
            {
                if (_rootAllNode != null)
                {
                    _rootAllNode.Text = string.Format("全部奇经总览 ({0}/{1})", sum.UnlockedCount, sum.TotalCount);
                }

                UpdateBranchTreeNode(MeridianBranchType.DuMai, sum.DuMaiUnlocked, 6, "功法乘区/攻伐");
                UpdateBranchTreeNode(MeridianBranchType.RenMai, sum.RenMaiUnlocked, 6, "吐纳/气海/寿元");
                UpdateBranchTreeNode(MeridianBranchType.ChongMai, sum.ChongMaiUnlocked, 6, "气血/防御/汲血");
                UpdateBranchTreeNode(MeridianBranchType.DaiMai, sum.DaiMaiUnlocked, 6, "速度/暴击/渡劫");

                foreach (var acu in SIGConfigTable.Current.Acupoints)
                {
                    TreeNode tNode;
                    if (!_acuTreeNodes.TryGetValue(acu.Id, out tNode)) continue;

                    bool unlocked = MeridianLogic.IsUnlocked(ld, acu.Id);
                    bool ready = readyIds.Contains(acu.Id);
                    string mark = unlocked ? "[■]" : (ready ? "[▶]" : "[○]");
                    string stateSuffix;
                    if (unlocked)
                    {
                        stateSuffix = "已贯通";
                    }
                    else if (ready)
                    {
                        stateSuffix = string.Format("可冲 {0:P0}", MeridianLogic.GetEffectiveImpulseRate(ld, acu));
                    }
                    else
                    {
                        bool preOk = string.IsNullOrEmpty(acu.PrerequisiteId) || MeridianLogic.IsUnlocked(ld, acu.PrerequisiteId);
                        if (!preOk) stateSuffix = "待前置";
                        else if (ld.Realm < acu.GetMinRealm()) stateSuffix = "需" + EnumTextMap.RealmBaseCn(acu.GetMinRealm());
                        else stateSuffix = string.Format("成率 {0:P0}", MeridianLogic.GetEffectiveImpulseRate(ld, acu));
                    }

                    string targetText = string.Format("{0} {1}.{2} ({3})", mark, acu.OrderIndex, acu.NameCn, stateSuffix);
                    if (tNode.Text != targetText) tNode.Text = targetText;
                }
            }
            finally
            {
                _treeMeridians.EndUpdate();
                _suppressTreeSelect = false;
            }
        }

        private void UpdateBranchTreeNode(MeridianBranchType br, int unlocked, int total, string roleTag)
        {
            TreeNode node;
            if (!_branchNodes.TryGetValue(br, out node)) return;
            string mark = unlocked >= total ? "■满 " : "";
            string txt = string.Format("{0}{1} ({2}/{3}) [{4}]", mark, MeridianLogic.BranchNameCn(br), unlocked, total, roleTag);
            if (node.Text != txt) node.Text = txt;
        }

        private void RebuildAcupointTiles(SIGSaveData save, CharacterData ld, SectManageData sect)
        {
            var list = new List<CompactTileData>();
            var allNodes = SIGConfigTable.Current.Acupoints;

            double curAtk = ld.BattleStats.Attack;
            double curDef = ld.BattleStats.Defense;
            double curHp = ld.BattleStats.MaxHp;
            double curQiSpd = CultivationLogic.GetQiRatePerSec(ld, sect);
            string dummyPill;
            double curBreakRate = CultivationLogic.GetBreakthroughSuccessRate(ld, sect, out dummyPill);
            var curMd = MeridianLogic.ComputeMeridianSummary(ld);

            for (int i = 0; i < allNodes.Count; i++)
            {
                var node = allNodes[i];
                if (_branchFilterKey != "ALL" && node.BranchKey != _branchFilterKey) continue;

                bool unlocked = MeridianLogic.IsUnlocked(ld, node.Id);
                string blockReason;
                bool canImpulse = MeridianLogic.CanAttemptImpulse(save, ld, node, out blockReason);
                bool preUnlocked = string.IsNullOrEmpty(node.PrerequisiteId) || MeridianLogic.IsUnlocked(ld, node.PrerequisiteId);
                double effRate = MeridianLogic.GetEffectiveImpulseRate(ld, node);
                int pity = MeridianLogic.GetFailPityCount(ld, node.Id);

                string shortRealm = EnumTextMap.RealmBaseCn(node.GetMinRealm()).Replace("期", "").Replace("境", "");
                string subTop = unlocked ? "已通" : (canImpulse ? string.Format("▶{0:P0}", effRate) : string.Format("{0:P0}", effRate));
                string subBot = MeridianLogic.BranchShortCn(node.GetBranch()) + "·" + shortRealm;

                var rows = BuildInspectorRowsForAcupoint(
                    ld, sect, node, unlocked, canImpulse, effRate, pity,
                    curAtk, curDef, curHp, curQiSpd, curBreakRate, curMd);

                string statusHead = unlocked
                    ? "[■ 已贯通]"
                    : (canImpulse ? "[▶ 条件齐备·双击立即冲穴]" : ("[受限: " + blockReason + "]"));

                list.Add(new CompactTileData
                {
                    Id = node.Id,
                    ShortTitle = _branchFilterKey == "ALL"
                        ? string.Format("{0}·{1}", MeridianLogic.BranchShortCn(node.GetBranch()).Substring(0, 1), node.GetShortLabel())
                        : node.GetShortLabel(),
                    SubValueTop = subTop,
                    SubValueBottom = subBot,
                    IsActiveEquipped = unlocked,
                    IsLocked = (!unlocked && (!preUnlocked || ld.Realm < node.GetMinRealm())),
                    InspectorTitle = string.Format("【{0} · 第 {1} 穴：{2}】  {3}",
                        MeridianLogic.BranchNameCn(node.GetBranch()), node.OrderIndex, node.NameCn, statusHead),
                    InspectorNote = node.Desc,
                    InspectorRows = rows,
                    DetailText = node.Desc
                });
            }

            _acupointTileGrid.SetItems(list);
        }

        private List<InspectorRowData> BuildInspectorRowsForAcupoint(
            CharacterData ld,
            SectManageData sect,
            AcupointNodeConfig node,
            bool unlocked,
            bool canImpulse,
            double effRate,
            int pity,
            double curAtk,
            double curDef,
            double curHp,
            double curQiSpd,
            double curBreakRate,
            MeridianBonusSummary curMd)
        {
            var rows = new List<InspectorRowData>();

            // 1. 临时在掌门快照上测试打通该穴后的真实面板差值（自动包含若为第6穴触发的单脉小成/周天连携！）
            double dAtk = 0, dDef = 0, dHp = 0, dQiSpd = 0, dTechMult = 0, dBreak = 0;
            if (!unlocked)
            {
                ld.UnlockedAcupointIds.Add(node.Id);
                try
                {
                    CultivationLogic.RecalculateStats(ld, sect);
                    var nextMd = MeridianLogic.ComputeMeridianSummary(ld);
                    dAtk = ld.BattleStats.Attack - curAtk;
                    dDef = ld.BattleStats.Defense - curDef;
                    dHp = ld.BattleStats.MaxHp - curHp;
                    dQiSpd = CultivationLogic.GetQiRatePerSec(ld, sect) - curQiSpd;
                    dTechMult = nextMd.TechMultiplierAdd - curMd.TechMultiplierAdd;
                    string p2;
                    dBreak = CultivationLogic.GetBreakthroughSuccessRate(ld, sect, out p2) - curBreakRate;
                }
                finally
                {
                    ld.UnlockedAcupointIds.Remove(node.Id);
                    CultivationLogic.RecalculateStats(ld, sect);
                }
            }

            rows.Add(new InspectorRowData(
                "冲穴耗费",
                unlocked ? "已贯通免耗" : MeridianLogic.FormatCostLine(node),
                unlocked ? "永久生效" : (canImpulse ? "资材齐备" : "待集齐"),
                unlocked || canImpulse ? 1 : 0));

            rows.Add(new InspectorRowData(
                "贯通成功率",
                unlocked ? "100% (已通)" : string.Format("{0:P0} (基础{1:P0})", effRate, node.BaseSuccessRate),
                pity > 0 ? string.Format("保底+{0:P0}({1}次)", pity * 0.08, pity) : "悟性加成中",
                pity > 0 ? 1 : 0));

            rows.Add(new InspectorRowData(
                "功法乘区 / 吐纳",
                string.Format("+{0:P0} / {1:F1}/s", curMd.TechMultiplierAdd, curQiSpd),
                unlocked ? "已计入面板" : string.Format("功法+{0:P0} | 吐纳+{1:F1}/s", dTechMult, dQiSpd),
                (!unlocked && (dTechMult > 0.001 || dQiSpd > 0.05)) ? 1 : 0));

            rows.Add(new InspectorRowData(
                "攻防气血增益",
                string.Format("攻{0:F0} 防{1:F0} 血{2:F0}", curAtk, curDef, curHp),
                unlocked
                    ? string.Format("本穴: 攻+{0:F0} 防+{1:F0} 血+{2:F0}", node.FlatAtk, node.FlatDef, node.FlatHp)
                    : string.Format("攻+{0:F1} 防+{1:F1} 血+{2:F0}", dAtk, dDef, dHp),
                (!unlocked && (dAtk > 0.1 || dDef > 0.1 || dHp > 0.5)) ? 1 : 0));

            string preText = "无前置（脉络起穴）";
            if (!string.IsNullOrEmpty(node.PrerequisiteId))
            {
                var pre = SIGConfigTable.GetAcupoint(node.PrerequisiteId);
                bool preDone = MeridianLogic.IsUnlocked(ld, node.PrerequisiteId);
                preText = string.Format("{0} ({1})", pre != null ? pre.NameCn : node.PrerequisiteId, preDone ? "已通" : "未通");
            }

            rows.Add(new InspectorRowData(
                "前置穴位 / 境界",
                preText,
                "需 " + EnumTextMap.RealmBaseCn(node.GetMinRealm()),
                ld.Realm >= node.GetMinRealm() ? 1 : -1));

            if (node.FlatSpeed > 0 || node.CritRateAdd > 0 || node.DodgeRateAdd > 0 || dBreak > 0.001 || node.LifespanAdd > 0)
            {
                var extras = new List<string>();
                if (node.FlatSpeed > 0) extras.Add(string.Format("速+{0:F0}", node.FlatSpeed));
                if (node.CritRateAdd > 0) extras.Add(string.Format("暴+{0:P1}", node.CritRateAdd));
                if (node.DodgeRateAdd > 0) extras.Add(string.Format("闪+{0:P1}", node.DodgeRateAdd));
                if (dBreak > 0.001) extras.Add(string.Format("渡劫+{0:P1}", dBreak));
                if (node.LifespanAdd > 0) extras.Add(string.Format("寿+{0:F0}载", node.LifespanAdd));

                rows.Add(new InspectorRowData(
                    "特殊道果造化",
                    string.Format("寿限 {0:F0} 载", ld.MaxLifespanYears),
                    string.Join(" ", extras.ToArray()),
                    1));
            }

            return rows;
        }
    }
}
