using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIGTool.Datas;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// UI-UX-02：可复用紧凑批量步进选择条（×1 / ×5 / ×10 / Max）
    /// </summary>
    public class UcBatchStepBar : FlowLayoutPanel, IThemeableView
    {
        private static readonly int[] StepValues = { 1, 5, 10, 9999 };
        private static readonly string[] StepLabels = { "×1", "×5", "×10", "Max" };

        private readonly List<Button> _stepButtons = new List<Button>(4);
        private readonly Label _lblTitle;
        private int _selectedBatch = 1;

        public event Action<int> OnBatchChanged;

        public int SelectedBatch
        {
            get { return _selectedBatch; }
            set
            {
                _selectedBatch = value;
                RefreshHighlights();
            }
        }

        public UcBatchStepBar(string prefixTitle = "批量:")
        {
            this.DoubleBuffered = true;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.WrapContents = false;
            this.Margin = new Padding(2, 0, 4, 0);
            this.Padding = new Padding(0);
            this.BackColor = Color.Transparent;

            _lblTitle = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 5, 2, 0),
                Text = prefixTitle
            };
            this.Controls.Add(_lblTitle);

            for (int i = 0; i < StepValues.Length; i++)
            {
                int val = StepValues[i];
                var btn = UiStyleHelper.CreateButton(StepLabels[i], (s, e) =>
                {
                    _selectedBatch = val;
                    RefreshHighlights();
                    if (OnBatchChanged != null) OnBatchChanged(_selectedBatch);
                }, UiButtonRole.Default, true);
                _stepButtons.Add(btn);
                this.Controls.Add(btn);
            }

            ApplyTheme();
        }

        public void ApplyTheme()
        {
            _lblTitle.ForeColor = UiStyleHelper.Current.TextMuted;
            RefreshHighlights();
        }

        private void RefreshHighlights()
        {
            var p = UiStyleHelper.Current;
            for (int i = 0; i < _stepButtons.Count; i++)
            {
                var btn = _stepButtons[i];
                bool active = (StepValues[i] == _selectedBatch);
                if (active)
                {
                    btn.BackColor = p.SpeedActiveBg;
                    btn.ForeColor = p.SpeedActiveFg;
                    btn.FlatAppearance.BorderColor = p.SpeedActiveBg;
                }
                else
                {
                    UiStyleHelper.ApplyButtonRoleVisuals(btn, UiButtonRole.Default);
                }
            }
        }
    }

    /// <summary>
    /// UI-UX-02：通用紧凑筛选与排序工具栏（支持分类/品阶胶囊切换、五行快筛、关键字过滤与排序下拉）
    /// </summary>
    public class UcFilterSortBar : FlowLayoutPanel, IThemeableView
    {
        private readonly List<KeyValuePair<string, Button>> _groupButtons = new List<KeyValuePair<string, Button>>();
        private readonly List<KeyValuePair<ElementType?, Button>> _elemButtons = new List<KeyValuePair<ElementType?, Button>>();

        private readonly Label _lblSearch;
        private readonly TextBox _txtKeyword;
        private readonly ComboBox _cmbSort;

        public string ActiveGroupKey { get; private set; } = "ALL";
        public ElementType? ActiveElement { get; private set; } = null;
        public int SortIndex { get { return _cmbSort != null && _cmbSort.SelectedIndex >= 0 ? _cmbSort.SelectedIndex : 0; } }
        public string Keyword { get { return _txtKeyword != null ? _txtKeyword.Text.Trim() : ""; } }

        public event Action OnFilterChanged;

        public UcFilterSortBar(
            IList<KeyValuePair<string, string>> groupOptions,
            bool showElementFilter,
            string[] sortOptions)
        {
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Height = 28;
            this.WrapContents = false;
            this.Margin = new Padding(0);
            this.Padding = new Padding(1, 1, 1, 1);
            this.BackColor = Color.Transparent;

            if (groupOptions != null && groupOptions.Count > 0)
            {
                ActiveGroupKey = groupOptions[0].Key;
                for (int i = 0; i < groupOptions.Count; i++)
                {
                    string key = groupOptions[i].Key;
                    string label = groupOptions[i].Value;
                    var btn = UiStyleHelper.CreateButton(label, (s, e) =>
                    {
                        ActiveGroupKey = key;
                        RefreshButtonHighlights();
                        RaiseChanged();
                    }, UiButtonRole.Default, true);
                    _groupButtons.Add(new KeyValuePair<string, Button>(key, btn));
                    this.Controls.Add(btn);
                }
            }

            if (showElementFilter)
            {
                AddElemButton(null, "全系");
                AddElemButton(ElementType.Metal, "金");
                AddElemButton(ElementType.Wood, "木");
                AddElemButton(ElementType.Water, "水");
                AddElemButton(ElementType.Fire, "火");
                AddElemButton(ElementType.Earth, "土");
            }

            _lblSearch = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(4, 5, 1, 0),
                Text = "筛:"
            };

            _txtKeyword = new TextBox
            {
                Width = 74,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(1, 2, 4, 1)
            };
            _txtKeyword.TextChanged += (s, e) => RaiseChanged();

            this.Controls.Add(_lblSearch);
            this.Controls.Add(_txtKeyword);

            if (sortOptions != null && sortOptions.Length > 0)
            {
                _cmbSort = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 88,
                    Font = UiStyleHelper.SmallFont,
                    Margin = new Padding(2, 2, 2, 1)
                };
                for (int i = 0; i < sortOptions.Length; i++) _cmbSort.Items.Add(sortOptions[i]);
                _cmbSort.SelectedIndex = 0;
                _cmbSort.SelectedIndexChanged += (s, e) => RaiseChanged();
                this.Controls.Add(_cmbSort);
            }

            ApplyTheme();
        }

        private void AddElemButton(ElementType? elem, string text)
        {
            var btn = UiStyleHelper.CreateButton(text, (s, e) =>
            {
                ActiveElement = elem;
                RefreshButtonHighlights();
                RaiseChanged();
            }, UiButtonRole.Default, true);
            if (_elemButtons.Count == 0) btn.Margin = new Padding(6, 1, 1, 1);
            _elemButtons.Add(new KeyValuePair<ElementType?, Button>(elem, btn));
            this.Controls.Add(btn);
        }

        private void RaiseChanged()
        {
            if (OnFilterChanged != null) OnFilterChanged();
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            _lblSearch.ForeColor = p.TextMuted;
            _txtKeyword.BackColor = p.BgCard;
            _txtKeyword.ForeColor = p.TextPrimary;
            if (_cmbSort != null)
            {
                _cmbSort.BackColor = p.BgCard;
                _cmbSort.ForeColor = p.TextPrimary;
            }
            RefreshButtonHighlights();
        }

        private void RefreshButtonHighlights()
        {
            var p = UiStyleHelper.Current;
            for (int i = 0; i < _groupButtons.Count; i++)
            {
                var btn = _groupButtons[i].Value;
                bool active = (_groupButtons[i].Key == ActiveGroupKey);
                HighlightFilterBtn(btn, active, p);
            }
            for (int i = 0; i < _elemButtons.Count; i++)
            {
                var btn = _elemButtons[i].Value;
                bool active = (_elemButtons[i].Key == ActiveElement);
                HighlightFilterBtn(btn, active, p);
            }
        }

        private static void HighlightFilterBtn(Button btn, bool active, UiThemePalette p)
        {
            if (active)
            {
                btn.BackColor = p.SpeedActiveBg;
                btn.ForeColor = p.SpeedActiveFg;
                btn.FlatAppearance.BorderColor = p.SpeedActiveBg;
            }
            else
            {
                UiStyleHelper.ApplyButtonRoleVisuals(btn, UiButtonRole.Default);
            }
        }
    }
}
