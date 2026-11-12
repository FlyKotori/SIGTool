using System;
using System.Drawing;
using System.Windows.Forms;

namespace SIGTool.Forms.UIs
{
    public enum UiFeedbackLevel
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Blocked = 3
    }

    /// <summary>
    /// UI-UX-03：全局非阻塞交互反馈分发中心
    /// </summary>
    public static class UiFeedbackService
    {
        public static event Action<UiFeedbackLevel, string> OnFeedbackPublished;

        public static void PushInfo(string message)
        {
            Publish(UiFeedbackLevel.Info, message);
        }

        public static void PushSuccess(string message)
        {
            Publish(UiFeedbackLevel.Success, message);
        }

        public static void PushWarning(string message)
        {
            Publish(UiFeedbackLevel.Warning, message);
        }

        public static void PushBlocked(string message)
        {
            Publish(UiFeedbackLevel.Blocked, message);
        }

        public static void ReportAction(bool ok, string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            Publish(ok ? UiFeedbackLevel.Success : UiFeedbackLevel.Blocked, message);
        }

        private static void Publish(UiFeedbackLevel level, string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            string clean = message.Replace("\r\n", " ").Replace("\n", " ").Trim();
            var handler = OnFeedbackPublished;
            if (handler != null)
            {
                handler(level, clean);
            }
        }
    }

    /// <summary>
    /// UI-UX-03：驻留于主窗体或子窗体底部的单行非打断状态栏（无彩色 Emoji，严格适配纯灰度办公皮肤）
    /// </summary>
    public class UcStatusFeedbackStrip : UserControl, IThemeableView
    {
        private readonly Panel _topBorder;
        private readonly Label _lblBadge;
        private readonly Label _lblMessage;
        private readonly Label _lblRightHint;

        private UiFeedbackLevel _currentLevel = UiFeedbackLevel.Info;
        private readonly bool _subscribeGlobalBus;

        public UcStatusFeedbackStrip(bool subscribeGlobalBus = true, string defaultHintText = "单击:对比详情  |  双击:直接执行  |  右键:快捷菜单")
        {
            _subscribeGlobalBus = subscribeGlobalBus;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Bottom;
            this.Height = 25;
            this.Padding = new Padding(0);

            _topBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(6, 1, 8, 1)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblBadge = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallBoldFont,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(4, 1, 4, 1),
                Margin = new Padding(0, 1, 8, 1),
                Text = "就绪"
            };

            _lblMessage = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Font = UiStyleHelper.SmallFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0),
                Text = "工作台就绪。悬停顶部资源格可查收支构成，双击格子或槽位即可直接执行操作。"
            };

            _lblRightHint = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallMonoReg,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(8, 2, 0, 0),
                Text = defaultHintText ?? ""
            };

            tbl.Controls.Add(_lblBadge, 0, 0);
            tbl.Controls.Add(_lblMessage, 1, 0);
            tbl.Controls.Add(_lblRightHint, 2, 0);

            this.Controls.Add(tbl);
            this.Controls.Add(_topBorder);

            if (_subscribeGlobalBus)
            {
                UiFeedbackService.OnFeedbackPublished += HandleGlobalFeedback;
            }

            ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _subscribeGlobalBus)
            {
                UiFeedbackService.OnFeedbackPublished -= HandleGlobalFeedback;
            }
            base.Dispose(disposing);
        }

        private void HandleGlobalFeedback(UiFeedbackLevel level, string message)
        {
            if (this.IsDisposed) return;
            SetFeedback(level, message);
        }

        public void SetFeedback(UiFeedbackLevel level, string message)
        {
            _currentLevel = level;
            string timeTag = DateTime.Now.ToString("HH:mm:ss");
            _lblMessage.Text = string.Format("[{0}] {1}", timeTag, message ?? "");
            ApplyBadgeVisuals();
        }

        public void SetRightHint(string text)
        {
            if (_lblRightHint.Text != text)
            {
                _lblRightHint.Text = text ?? "";
            }
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgSubtle;
            _topBorder.BackColor = p.BorderStrong;
            _lblRightHint.ForeColor = p.TextMuted;
            ApplyBadgeVisuals();
        }

        private void ApplyBadgeVisuals()
        {
            var p = UiStyleHelper.Current;
            UiButtonRole role;
            string badgeText;
            Color msgColor = p.TextPrimary;

            switch (_currentLevel)
            {
                case UiFeedbackLevel.Success:
                    role = UiButtonRole.PositiveGreen;
                    badgeText = "[完成]";
                    msgColor = p.IsGrayscale ? p.TextPrimary : p.AccentJade;
                    break;
                case UiFeedbackLevel.Warning:
                    role = UiButtonRole.WarningAmber;
                    badgeText = "[提示]";
                    msgColor = p.IsGrayscale ? p.TextPrimary : p.AccentGold;
                    break;
                case UiFeedbackLevel.Blocked:
                    role = UiButtonRole.DangerRed;
                    badgeText = "[受阻]";
                    msgColor = p.IsGrayscale ? p.AccentBlue : p.AccentCrimson;
                    break;
                default:
                    role = UiButtonRole.PrimaryBlue;
                    badgeText = "[信息]";
                    msgColor = p.TextPrimary;
                    break;
            }

            Color bg, fg, border;
            p.GetButtonColors(role, out bg, out fg, out border);
            _lblBadge.Text = badgeText;
            _lblBadge.BackColor = bg;
            _lblBadge.ForeColor = fg;
            _lblMessage.ForeColor = msgColor;
        }
    }
}
