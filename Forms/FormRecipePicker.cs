using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Forms.UIs;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms
{
    /// <summary>
    /// 丹房/器坊百艺配方选择与手动开炉弹窗（v0.6：移除重复确认/开炉按钮，集成 UI-UX-02 批量开炉与 UI-UX-07 三列差值检查器）
    /// </summary>
    public class FormRecipePicker : Form, IThemeableView
    {
        private readonly SIGEngine _engine;
        private readonly int _targetWorkshopSlotIdx;
        private readonly string _filterBuildingId;

        private Label _lblTip;
        private CheckBox _chkAutoRepeat;
        private UcBatchStepBar _batchStepBar;
        private UcCompactTileGrid _tileGrid;
        private UcStatusFeedbackStrip _localStatusStrip;

        public FormRecipePicker(SIGEngine engine, int targetWorkshopSlotIdx = -1, string filterBuildingId = "")
        {
            _engine = engine;
            _targetWorkshopSlotIdx = targetWorkshopSlotIdx;
            _filterBuildingId = filterBuildingId ?? "";

            this.DoubleBuffered = true;
            this.Text = targetWorkshopSlotIdx >= 0
                ? string.Format("选择工坊挂机配方 (地块 #{0})", targetWorkshopSlotIdx + 1)
                : "宗门百艺丹器总谱 - 掌门手动开炉";
            this.Size = new Size(700, 500);
            this.MinimumSize = new Size(560, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = UiStyleHelper.NormalFont;

            BuildUI();
            UiStyleHelper.ApplyThemeRecursive(this);
            RefreshRecipeTiles();
        }

        private void BuildUI()
        {
            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(8, 5, 8, 2)
            };

            _lblTip = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.BoldFont,
                Margin = new Padding(2, 2, 10, 0),
                Text = _targetWorkshopSlotIdx >= 0
                    ? "双击配方格子直接装填开炉；右键格子可手动开炉或停炉："
                    : "双击配方格子按下方选定批量直接手动开炉祭炼："
            };
            pnlTop.Controls.Add(_lblTip);

            _localStatusStrip = new UcStatusFeedbackStrip(
                subscribeGlobalBus: false,
                defaultHintText: _targetWorkshopSlotIdx >= 0 ? "双击:装填工坊配方 | 右键:菜单" : "双击:按批量手动开炉 | 右键:菜单");

            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Padding = new Padding(8, 2, 8, 2),
                FlowDirection = FlowDirection.RightToLeft
            };

            var btnClose = UiStyleHelper.CreateButton("关闭", (s, e) => this.Close(), UiButtonRole.Default, true);
            _batchStepBar = new UcBatchStepBar("手动开炉批量:");

            pnlBottom.Controls.Add(btnClose);
            pnlBottom.Controls.Add(_batchStepBar);

            if (_targetWorkshopSlotIdx >= 0)
            {
                _chkAutoRepeat = new CheckBox
                {
                    Text = "自动循环开炉",
                    Checked = true,
                    AutoSize = true,
                    Font = UiStyleHelper.SmallFont,
                    Margin = new Padding(8, 5, 8, 0)
                };

                var btnStop = UiStyleHelper.CreateButton("停炉置空", (s, e) =>
                {
                    string msg;
                    bool ok = CraftAndItemLogic.SetWorkshopRecipe(_engine.Save, _targetWorkshopSlotIdx, "", false, _engine.EventBus, out msg);
                    UiFeedbackService.ReportAction(ok, msg);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }, UiButtonRole.Default, true);

                pnlBottom.Controls.Add(btnStop);
                pnlBottom.Controls.Add(_chkAutoRepeat);
            }

            _tileGrid = new UcCompactTileGrid(footerHeight: 104, tileWidth: 148, tileHeight: 42);
            _tileGrid.Padding = new Padding(8, 4, 8, 4);
            _tileGrid.OnTileDoubleClicked += tile =>
            {
                if (_targetWorkshopSlotIdx >= 0) AssignSelectedToWorkshop(tile.Id);
                else DoManualCraftBatch(tile.Id, _batchStepBar.SelectedBatch);
            };
            _tileGrid.ContextMenuBuilder = BuildRecipeContextMenu;

            this.Controls.Add(_tileGrid);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(_localStatusStrip);
            this.Controls.Add(pnlTop);
        }

        private ContextMenuStrip BuildRecipeContextMenu(CompactTileData tile)
        {
            if (tile == null) return null;
            var menu = UiStyleHelper.CreateThemedContextMenu();
            if (_targetWorkshopSlotIdx >= 0)
            {
                menu.Items.Add(new ToolStripMenuItem("设为该工坊挂机配方", null, (s, e) => AssignSelectedToWorkshop(tile.Id)));
            }
            int b = _batchStepBar != null ? _batchStepBar.SelectedBatch : 1;
            menu.Items.Add(new ToolStripMenuItem(
                string.Format("掌门立即手动开炉 ({0})", b >= 999 ? "Max" : ("×" + b)),
                null,
                (s, e) => DoManualCraftBatch(tile.Id, b)));
            return menu;
        }

        private void DoManualCraftBatch(string recipeId, int batchCount)
        {
            int done = 0;
            string lastMsg = "";
            int maxLoop = Math.Max(1, Math.Min(100, batchCount));

            for (int i = 0; i < maxLoop; i++)
            {
                string msg;
                if (CraftAndItemLogic.CraftOnceManual(_engine.Save, recipeId, _engine.Rng, _engine.EventBus, out msg))
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
                string summary = string.Format("连续开炉 {0} 炉完成（末炉结果：{1}）。", done, lastMsg);
                _localStatusStrip.SetFeedback(UiFeedbackLevel.Success, summary);
                UiFeedbackService.PushSuccess(summary);
            }
            else
            {
                _localStatusStrip.SetFeedback(done > 0 ? UiFeedbackLevel.Success : UiFeedbackLevel.Blocked, lastMsg);
                UiFeedbackService.ReportAction(done > 0, lastMsg);
            }
            RefreshRecipeTiles();
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            if (_lblTip != null) _lblTip.ForeColor = p.AccentGold;
        }

        private void AssignSelectedToWorkshop(string rcpId)
        {
            if (string.IsNullOrEmpty(rcpId)) return;
            string msg;
            if (!CraftAndItemLogic.SetWorkshopRecipe(_engine.Save, _targetWorkshopSlotIdx, rcpId,
                _chkAutoRepeat != null ? _chkAutoRepeat.Checked : true, _engine.EventBus, out msg))
            {
                _localStatusStrip.SetFeedback(UiFeedbackLevel.Blocked, msg);
                UiFeedbackService.ReportAction(false, msg);
                return;
            }
            UiFeedbackService.ReportAction(true, msg);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public void RefreshRecipeTiles()
        {
            var save = _engine.Save;
            if (save == null) return;

            BuildingInstanceData targetBld = null;
            if (_targetWorkshopSlotIdx >= 0 && _targetWorkshopSlotIdx < save.Sect.GridSlots.Count)
            {
                targetBld = save.Sect.GridSlots[_targetWorkshopSlotIdx].Building;
            }

            var list = new List<CompactTileData>();
            foreach (var rcp in SIGConfigTable.Current.Recipes)
            {
                if (!string.IsNullOrEmpty(_filterBuildingId) && rcp.RequiredBuildingId != _filterBuildingId)
                    continue;

                int mLv = save.Sect.GetRecipeMasteryLv(rcp.Id);
                var reqBld = SIGConfigTable.GetBuilding(rcp.RequiredBuildingId);
                var outItem = SIGConfigTable.GetItem(rcp.OutputItemId);
                double effRate = CraftAndItemLogic.GetEffectiveSuccessRate(save, targetBld, rcp);
                bool locked = (targetBld != null && targetBld.Level < rcp.MinBuildingLevel);
                bool active = (targetBld != null && targetBld.ActiveRecipeId == rcp.Id);
                bool canAfford = CraftAndItemLogic.CanAffordRecipe(save.Sect, rcp);

                var rows = new List<InspectorRowData>
                {
                    new InspectorRowData("工坊要求", reqBld != null ? reqBld.GetShortLabel() : rcp.RequiredBuildingId,
                        "需 Lv." + rcp.MinBuildingLevel, locked ? -1 : 1),
                    new InspectorRowData("预计成率", string.Format("耗时 {0:F0}s", rcp.CraftTimeSec),
                        string.Format("{0:P0} (熟L{1})", effRate, mLv), 1),
                    new InspectorRowData("单炉耗材", canAfford ? "耗材充足" : "耗材不足",
                        CraftAndItemLogic.FormatRecipeCostWithStock(save.Sect, rcp), canAfford ? 1 : -1),
                    new InspectorRowData("炼制产出", "现存 " + save.Sect.GetItemCount(rcp.OutputItemId),
                        string.Format("{0} ×{1}", outItem != null ? outItem.NameCn : rcp.OutputItemId, rcp.OutputCount), 0)
                };

                list.Add(new CompactTileData
                {
                    Id = rcp.Id,
                    ShortTitle = rcp.GetShortLabel(),
                    SubValueTop = "L" + mLv,
                    SubValueBottom = "M" + rcp.MaxMasteryLv,
                    InspectorTitle = string.Format("【{0}】 {1}", rcp.NameCn, active ? "■ 当前装填中" : (_targetWorkshopSlotIdx >= 0 ? "（双击设为工坊配方）" : "（双击手动开炉）")),
                    InspectorNote = rcp.Desc,
                    InspectorRows = rows,
                    IsActiveEquipped = active,
                    IsLocked = locked
                });
            }

            _tileGrid.SetItems(list);
        }
    }
}
