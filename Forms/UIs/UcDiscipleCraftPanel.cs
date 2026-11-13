using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// 页签3（v0.6 无冗余按钮精简版）：
    /// • 门人表移除底部 7 个派工按钮与顶部重复操作按钮，仅保留【招募弟子】与各岗人数汇总；职责列点击下拉直改，双击行看详单（满气海双击/右键破境），右键行全量菜单；
    /// • 百艺表移除重复【换方/停炉】按钮与按钮列，保留【百艺总谱】入口，行双击选方、右键直切配方/单炉祭炼/停炉。
    /// </summary>
    public class UcDiscipleCraftPanel : UserControl, IThemeableView
    {
        private readonly SIGEngine _engine;
        private readonly Action<CharacterData> _onOpenCharSheet;

        private Button _btnRecruit;
        private Label _lblJobSummary;
        private DataGridView _gridDisciples;
        private DataGridView _gridWorkshops;
        private Label _lblWsTip;

        private int _lastDiscCount = -1;
        private bool _suppressDiscJobCommit = false;
        private ContextMenuStrip _rowContextMenu;

        public UcDiscipleCraftPanel(SIGEngine engine, Action<CharacterData> onOpenCharSheet)
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

        public void FocusDiscipleById(string discId)
        {
            if (string.IsNullOrEmpty(discId)) return;
            RefreshView();
            for (int i = 0; i < _gridDisciples.Rows.Count; i++)
            {
                if (Convert.ToString(_gridDisciples.Rows[i].Tag) == discId)
                {
                    _gridDisciples.ClearSelection();
                    _gridDisciples.Rows[i].Selected = true;
                    _gridDisciples.FirstDisplayedScrollingRowIndex = i;
                    break;
                }
            }
        }

        public void FocusWorkshopBySlot(int slotIdx)
        {
            if (slotIdx < 0) return;
            RefreshView();
            for (int i = 0; i < _gridWorkshops.Rows.Count; i++)
            {
                if (Convert.ToInt32(_gridWorkshops.Rows[i].Tag) == slotIdx)
                {
                    _gridWorkshops.ClearSelection();
                    _gridWorkshops.Rows[i].Selected = true;
                    _gridWorkshops.FirstDisplayedScrollingRowIndex = i;
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _rowContextMenu != null)
            {
                _rowContextMenu.Dispose();
                _rowContextMenu = null;
            }
            base.Dispose(disposing);
        }

        private void BuildUI()
        {
            var splitMain = UiStyleHelper.CreateProportionalSplit(Orientation.Vertical, 0.53, 420, 380, "Tab3.Main");

            var grpDisc = new GroupBox
            {
                Text = "【门人排班管理（职责列下拉直改 · 双击查看详单 · 右键破境/配装/改派）】",
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
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            tblLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlTopActions = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 28);
            _btnRecruit = UiStyleHelper.CreateButton("招募弟子", (s, e) => TriggerRecruitByHotkey(), UiButtonRole.PrimaryBlue, true);
            UiStyleHelper.SetToolTip(_btnRecruit, "消耗灵石与灵谷寻访新外门弟子入山 — 本页快捷键: R");

            _lblJobSummary = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallMonoReg,
                Margin = new Padding(8, 5, 0, 0),
                Text = "在岗统计: -"
            };

            pnlTopActions.Controls.AddRange(new Control[] { _btnRecruit, _lblJobSummary });

            _gridDisciples = UiStyleHelper.CreateDoubleBufferedGrid();
            _gridDisciples.ReadOnly = false;
            _gridDisciples.EditMode = DataGridViewEditMode.EditOnEnter;

            var colName = new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "门人姓名", ReadOnly = true, FillWeight = 24 };
            var colRealm = new DataGridViewTextBoxColumn { Name = "ColRealm", HeaderText = "境界", ReadOnly = true, FillWeight = 26 };
            var colQi = new DataGridViewTextBoxColumn { Name = "ColQi", HeaderText = "气海进度", ReadOnly = true, FillWeight = 22 };
            var colJob = new DataGridViewComboBoxColumn
            {
                Name = "ColJob",
                HeaderText = "挂机职责 (下拉直改)",
                ReadOnly = false,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FillWeight = 28
            };
            foreach (DiscipleJob job in Enum.GetValues(typeof(DiscipleJob)))
            {
                colJob.Items.Add(EnumTextMap.JobCn(job));
            }

            _gridDisciples.Columns.AddRange(new DataGridViewColumn[] { colName, colRealm, colQi, colJob });
            _gridDisciples.DataError += (s, e) => { e.ThrowException = false; };
            _gridDisciples.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_gridDisciples.IsCurrentCellDirty &&
                    _gridDisciples.CurrentCell != null &&
                    _gridDisciples.CurrentCell.ColumnIndex == 3)
                {
                    _gridDisciples.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            _gridDisciples.CellValueChanged += OnDiscipleCellValueChanged;
            _gridDisciples.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex == 3) return;
                var d = GetSelectedDisciple();
                if (d != null && d.CurQi >= d.MaxQi - 0.1)
                {
                    DoDiscipleBreakthrough(d);
                }
                else
                {
                    OpenSelectedDiscipleSheet();
                }
            };
            _gridDisciples.CellMouseUp += OnDiscipleCellMouseUp;

            tblLeft.Controls.Add(pnlTopActions, 0, 0);
            tblLeft.Controls.Add(_gridDisciples, 0, 1);
            grpDisc.Controls.Add(tblLeft);

            var grpWorkshops = new GroupBox
            {
                Text = "【丹房与器坊炉位（双击工坊行选方 · 右键工坊行秒切配方/单炉/停炉）】",
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
            tblRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));

            var pnlWsBar = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 28);
            var btnOpenCompendium = UiStyleHelper.CreateButton("百艺总谱 / 掌门手动开炉", (s, e) => OpenRecipeCompendiumByHotkey(), UiButtonRole.PrimaryBlue, true);
            UiStyleHelper.SetToolTip(btnOpenCompendium, "打开宗门百艺丹器总谱，支持掌门按批量手动开炉祭炼 — 本页快捷键: M");

            pnlWsBar.Controls.Add(btnOpenCompendium);

            _gridWorkshops = UiStyleHelper.CreateDoubleBufferedGrid();
            _gridWorkshops.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColBld", HeaderText = "工坊地块", FillWeight = 34 });
            _gridWorkshops.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColRcp", HeaderText = "当前配方", FillWeight = 33 });
            _gridWorkshops.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColProg", HeaderText = "炉火进度", FillWeight = 33 });

            _gridWorkshops.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenRecipePickerForWorkshopRow(e.RowIndex);
            };
            _gridWorkshops.CellMouseUp += OnWorkshopCellMouseUp;

            _lblWsTip = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.SmallFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "说明：双击工坊行打开配方谱，右键工坊行可就地切换配方、手动单炉或停炉。"
            };

            tblRight.Controls.Add(pnlWsBar, 0, 0);
            tblRight.Controls.Add(_gridWorkshops, 0, 1);
            tblRight.Controls.Add(_lblWsTip, 0, 2);
            grpWorkshops.Controls.Add(tblRight);

            splitMain.Panel1.Controls.Add(grpDisc);
            splitMain.Panel2.Controls.Add(grpWorkshops);
            this.Controls.Add(splitMain);
        }

        private void OnDiscipleCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppressDiscJobCommit || e.RowIndex < 0 || e.ColumnIndex != 3 || _engine.Save == null) return;
            var row = _gridDisciples.Rows[e.RowIndex];
            string discId = Convert.ToString(row.Tag);
            string selectedText = Convert.ToString(row.Cells[3].Value);

            foreach (DiscipleJob job in Enum.GetValues(typeof(DiscipleJob)))
            {
                if (EnumTextMap.JobCn(job) == selectedText)
                {
                    AssignDiscipleJobWithFeedback(discId, job);
                    break;
                }
            }
        }

        private void AssignDiscipleJobWithFeedback(string discId, DiscipleJob job)
        {
            string msg;
            bool ok = SectBuildLogic.AssignDiscipleJob(_engine.Save, discId, job, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoDiscipleBreakthrough(CharacterData d)
        {
            if (d == null) return;
            string msg;
            bool ok = CultivationLogic.TryBreakthrough(_engine.Save, d, _engine.Rng, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void OnDiscipleCellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0 || _engine.Save == null) return;
            if (_gridDisciples.IsCurrentCellInEditMode) _gridDisciples.EndEdit();

            _gridDisciples.ClearSelection();
            _gridDisciples.Rows[e.RowIndex].Selected = true;

            var d = GetSelectedDisciple();
            if (d == null) return;

            if (_rowContextMenu != null) { _rowContextMenu.Dispose(); _rowContextMenu = null; }
            _rowContextMenu = UiStyleHelper.CreateThemedContextMenu();

            var miJobRoot = new ToolStripMenuItem(string.Format("改派【{0}】挂机职责", d.Name));
            foreach (DiscipleJob job in Enum.GetValues(typeof(DiscipleJob)))
            {
                DiscipleJob captured = job;
                var sub = new ToolStripMenuItem(EnumTextMap.JobCn(captured), null, (s, ev) => AssignDiscipleJobWithFeedback(d.Id, captured))
                {
                    Checked = (d.AssignedJob == captured)
                };
                miJobRoot.DropDownItems.Add(sub);
            }
            _rowContextMenu.Items.Add(miJobRoot);

            _rowContextMenu.Items.Add(new ToolStripMenuItem("★ 冲击破境", null, (s, ev) => DoDiscipleBreakthrough(d)));
            _rowContextMenu.Items.Add(new ToolStripSeparator());
            _rowContextMenu.Items.Add(new ToolStripMenuItem("配置飞剑...", null, (s, ev) => EquipDiscipleSlot(d, ItemCategory.Weapon, VaultFilterMode.WeaponOnly, "为门人配置飞剑")));
            _rowContextMenu.Items.Add(new ToolStripMenuItem("配置宝衣...", null, (s, ev) => EquipDiscipleSlot(d, ItemCategory.Armor, VaultFilterMode.ArmorOnly, "为门人配置宝衣")));
            _rowContextMenu.Items.Add(new ToolStripMenuItem("查看完整属性详单", null, (s, ev) =>
            {
                if (_onOpenCharSheet != null) _onOpenCharSheet(d);
            }));

            UiStyleHelper.StyleContextMenu(_rowContextMenu);
            Rectangle cellRect = _gridDisciples.GetCellDisplayRectangle(Math.Max(0, e.ColumnIndex), e.RowIndex, true);
            _rowContextMenu.Show(_gridDisciples, new Point(cellRect.Left + e.X, cellRect.Top + e.Y));
        }

        private void OnWorkshopCellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0 || _engine.Save == null) return;
            _gridWorkshops.ClearSelection();
            _gridWorkshops.Rows[e.RowIndex].Selected = true;

            int slotIdx = Convert.ToInt32(_gridWorkshops.Rows[e.RowIndex].Tag);
            var slot = _engine.Save.Sect.GridSlots[slotIdx];
            if (slot.Building == null) return;

            if (_rowContextMenu != null) { _rowContextMenu.Dispose(); _rowContextMenu = null; }
            _rowContextMenu = UiStyleHelper.CreateThemedContextMenu();

            string bldId = slot.Building.ConfigId;
            foreach (var rcp in SIGConfigTable.Current.Recipes)
            {
                if (rcp.RequiredBuildingId != bldId) continue;
                bool unlocked = (slot.Building.Level >= rcp.MinBuildingLevel);
                string capturedRcpId = rcp.Id;
                int mLv = _engine.Save.Sect.GetRecipeMasteryLv(rcp.Id);

                var mi = new ToolStripMenuItem(
                    string.Format("切换配方：{0} (L{1}{2})", rcp.NameCn, mLv, unlocked ? "" : string.Format(" · 需Lv.{0}", rcp.MinBuildingLevel)),
                    null,
                    (s, ev) =>
                    {
                        string msg;
                        bool ok = CraftAndItemLogic.SetWorkshopRecipe(_engine.Save, slotIdx, capturedRcpId, true, _engine.EventBus, out msg);
                        UiFeedbackService.ReportAction(ok, msg);
                        RefreshView();
                    })
                {
                    Enabled = unlocked,
                    Checked = (slot.Building.ActiveRecipeId == capturedRcpId)
                };
                _rowContextMenu.Items.Add(mi);
            }

            _rowContextMenu.Items.Add(new ToolStripSeparator());
            _rowContextMenu.Items.Add(new ToolStripMenuItem("打开百艺配方谱窗口...", null, (s, ev) => OpenRecipePickerForWorkshopRow(e.RowIndex)));

            if (!string.IsNullOrEmpty(slot.Building.ActiveRecipeId))
            {
                string curRcpId = slot.Building.ActiveRecipeId;
                _rowContextMenu.Items.Add(new ToolStripMenuItem("手动开炉祭炼 1 次当前配方", null, (s, ev) =>
                {
                    string msg;
                    bool ok = CraftAndItemLogic.CraftOnceManual(_engine.Save, curRcpId, _engine.Rng, _engine.EventBus, out msg);
                    UiFeedbackService.ReportAction(ok, msg);
                    RefreshView();
                }));
                _rowContextMenu.Items.Add(new ToolStripMenuItem("停炉置空", null, (s, ev) =>
                {
                    string msg;
                    bool ok = CraftAndItemLogic.SetWorkshopRecipe(_engine.Save, slotIdx, "", false, _engine.EventBus, out msg);
                    UiFeedbackService.ReportAction(ok, msg);
                    RefreshView();
                }));
            }

            UiStyleHelper.StyleContextMenu(_rowContextMenu);
            Rectangle cellRect = _gridWorkshops.GetCellDisplayRectangle(Math.Max(0, e.ColumnIndex), e.RowIndex, true);
            _rowContextMenu.Show(_gridWorkshops, new Point(cellRect.Left + e.X, cellRect.Top + e.Y));
        }

        public void TriggerRecruitByHotkey()
        {
            if (_engine.Save == null) return;
            string msg;
            bool ok = SectBuildLogic.RecruitDisciple(_engine.Save, _engine.Rng, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        public void OpenRecipeCompendiumByHotkey()
        {
            using (var dlg = new FormRecipePicker(_engine, -1, ""))
            {
                dlg.ShowDialog(this);
            }
            RefreshView();
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;
            if (_lblJobSummary != null) _lblJobSummary.ForeColor = p.TextMuted;
            if (_lblWsTip != null) _lblWsTip.ForeColor = p.TextMuted;
            UiStyleHelper.ApplyGridTheme(_gridDisciples);
            UiStyleHelper.ApplyGridTheme(_gridWorkshops);
        }

        private CharacterData GetSelectedDisciple()
        {
            if (_gridDisciples.SelectedRows.Count == 0 || _engine.Save == null) return null;
            string id = Convert.ToString(_gridDisciples.SelectedRows[0].Tag);
            return _engine.Save.Disciples.FirstOrDefault(d => d.Id == id);
        }

        private void OpenSelectedDiscipleSheet()
        {
            var d = GetSelectedDisciple();
            if (d != null && _onOpenCharSheet != null) _onOpenCharSheet(d);
        }

        private void EquipDiscipleSlot(CharacterData d, ItemCategory slotCat, VaultFilterMode filter, string title)
        {
            if (d == null) return;
            string pickedId;
            if (FormStorageVault.ShowSlotPicker(this, _engine, d, filter, title, out pickedId))
            {
                string msg;
                bool ok = CultivationLogic.EquipOrSwapSlot(_engine.Save, d, slotCat, pickedId, _engine.EventBus, out msg);
                UiFeedbackService.ReportAction(ok, msg);
                RefreshView();
            }
        }

        private void OpenRecipePickerForWorkshopRow(int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= _gridWorkshops.Rows.Count || _engine.Save == null) return;
            int slotIdx = Convert.ToInt32(_gridWorkshops.Rows[rowIdx].Tag);
            var slot = _engine.Save.Sect.GridSlots[slotIdx];
            string bldId = slot.Building != null ? slot.Building.ConfigId : "";

            using (var dlg = new FormRecipePicker(_engine, slotIdx, bldId))
            {
                dlg.ShowDialog(this);
            }
            RefreshView();
        }

        public void RefreshView()
        {
            var save = _engine.Save;
            if (save == null) return;

            double nextCost = SectBuildLogic.GetNextRecruitStoneCost(save);
            int cap = SectBuildLogic.GetMaxDiscipleCap(save.Sect);
            UiStyleHelper.SetTextIfChanged(_btnRecruit, string.Format("招募 ({0}/{1} · {2}石)", save.Disciples.Count, cap, nextCost));

            int cFarm = save.Disciples.Count(x => x.AssignedJob == DiscipleJob.Farm);
            int cLum = save.Disciples.Count(x => x.AssignedJob == DiscipleJob.Lumber);
            int cMin = save.Disciples.Count(x => x.AssignedJob == DiscipleJob.Mine);
            int cAlc = save.Disciples.Count(x => x.AssignedJob == DiscipleJob.Alchemy);
            int cFrg = save.Disciples.Count(x => x.AssignedJob == DiscipleJob.Forge);
            int cGrd = save.Disciples.Count(x => x.AssignedJob == DiscipleJob.Guard);
            string jobSum = string.Format("农:{0} 林:{1} 矿:{2} 丹:{3} 器:{4} 卫:{5}", cFarm, cLum, cMin, cAlc, cFrg, cGrd);
            if (_lblJobSummary.Text != jobSum) _lblJobSummary.Text = jobSum;

            _suppressDiscJobCommit = true;
            try
            {
                if (_lastDiscCount != save.Disciples.Count)
                {
                    string prevId = GetSelectedDisciple() != null ? GetSelectedDisciple().Id : "";
                    _lastDiscCount = save.Disciples.Count;
                    _gridDisciples.Rows.Clear();
                    foreach (var d in save.Disciples)
                    {
                        var rCfg = SIGConfigTable.GetRealm(d.Realm);
                        int rIdx = _gridDisciples.Rows.Add(
                            d.Name,
                            EnumTextMap.RealmFullCn(d.Realm, d.SubStage, rCfg.MaxSubStage),
                            d.CurQi >= d.MaxQi - 0.1 ? "★已满(双击破境)" : string.Format("{0:P0}", d.CurQi / Math.Max(1.0, d.MaxQi)),
                            EnumTextMap.JobCn(d.AssignedJob));
                        _gridDisciples.Rows[rIdx].Tag = d.Id;
                        if (d.Id == prevId) _gridDisciples.Rows[rIdx].Selected = true;
                    }
                }
                else
                {
                    bool editingJobCell = _gridDisciples.IsCurrentCellInEditMode &&
                                          _gridDisciples.CurrentCell != null &&
                                          _gridDisciples.CurrentCell.ColumnIndex == 3;

                    for (int i = 0; i < save.Disciples.Count && i < _gridDisciples.Rows.Count; i++)
                    {
                        var d = save.Disciples[i];
                        var rCfg = SIGConfigTable.GetRealm(d.Realm);
                        var row = _gridDisciples.Rows[i];
                        UiStyleHelper.SetCellIfChanged(row.Cells[1], EnumTextMap.RealmFullCn(d.Realm, d.SubStage, rCfg.MaxSubStage));
                        UiStyleHelper.SetCellIfChanged(row.Cells[2], d.CurQi >= d.MaxQi - 0.1
                            ? "★已满(双击破境)"
                            : string.Format("{0,4:P0}", d.CurQi / Math.Max(1.0, d.MaxQi)));

                        if (!(editingJobCell && _gridDisciples.CurrentCell.RowIndex == i))
                        {
                            string targetJob = EnumTextMap.JobCn(d.AssignedJob);
                            if (Convert.ToString(row.Cells[3].Value) != targetJob)
                            {
                                row.Cells[3].Value = targetJob;
                            }
                        }
                    }
                }
            }
            finally
            {
                _suppressDiscJobCommit = false;
            }

            var workshopSlots = save.Sect.GridSlots.Where(s =>
                s.Building != null && !s.Building.IsUnderConstruction &&
                (s.Building.ConfigId == "BLD_ALCHEMY" || s.Building.ConfigId == "BLD_FORGE")).ToList();

            if (_gridWorkshops.Rows.Count != workshopSlots.Count)
            {
                _gridWorkshops.Rows.Clear();
                foreach (var ws in workshopSlots)
                {
                    int idx = _gridWorkshops.Rows.Add("", "", "");
                    _gridWorkshops.Rows[idx].Tag = ws.SlotIndex;
                }
            }

            for (int i = 0; i < workshopSlots.Count && i < _gridWorkshops.Rows.Count; i++)
            {
                var ws = workshopSlots[i];
                var bCfg = SIGConfigTable.GetBuilding(ws.Building.ConfigId);
                var rcp = SIGConfigTable.GetRecipe(ws.Building.ActiveRecipeId);
                var row = _gridWorkshops.Rows[i];
                row.Tag = ws.SlotIndex;
                UiStyleHelper.SetCellIfChanged(row.Cells[0], string.Format("#{0} {1} Lv.{2}", ws.SlotIndex + 1, bCfg.GetShortLabel(), ws.Building.Level));
                UiStyleHelper.SetCellIfChanged(row.Cells[1], rcp != null ? rcp.GetShortLabel() : "[停炉 · 双击选方]");
                UiStyleHelper.SetCellIfChanged(row.Cells[2], rcp != null
                    ? (ws.Building.CraftProgressSec <= 0.001
                        ? "待耗材齐备"
                        : string.Format("{0,4:P0} ({1,4:F1}/{2,2:F0}s)", ws.Building.CraftProgressSec / rcp.CraftTimeSec, ws.Building.CraftProgressSec, rcp.CraftTimeSec))
                    : "-");
            }
        }
    }
}
