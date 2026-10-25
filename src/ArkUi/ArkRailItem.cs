using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ArkUi
{
    /// <summary>
    /// 左侧竖排导航项：编号 / 中文标题 / 英文小标三行。
    /// 选中态是黑底 + 左测信号黄竖条，不靠颜色单独区分。
    /// </summary>
    public class ArkRailItem : Control
    {
        private bool _hover;
        private bool _active;

        public ArkRailItem()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw, true);
            Height = 74;
            Cursor = Cursors.Hand;
            BackColor = ArkTheme.Rail;
        }

        /// <summary>左上角的两位编号，如 "01"。</summary>
        public string IndexText { get; set; }

        /// <summary>中文标题，如 "设备清单"。</summary>
        public string Caption { get; set; }

        /// <summary>英文小标，如 "ROSTER"。</summary>
        public string Micro { get; set; }

        public bool IsActive
        {
            get { return _active; }
            set
            {
                if (_active == value) return;
                _active = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            Rectangle r = ClientRectangle;

            Color bg = _active ? ArkTheme.Ink : (_hover ? ArkTheme.RailHover : ArkTheme.Rail);
            using (SolidBrush b = new SolidBrush(bg))
            {
                g.FillRectangle(b, r);
            }

            // 左侧那条 4px 信号黄竖条不在这里画：它是一条独立的浮层（railBar），
            // 切页时从上一项滑到下一项，所以这里只负责底色和文字。

            Color caption = _active ? ArkTheme.Paper : ArkTheme.Ink;
            Color micro = _active ? ArkTheme.Signal : ArkTheme.Muted;

            TextRenderer.DrawText(g, IndexText ?? string.Empty, ArkTheme.Micro(9f),
                new Rectangle(r.Width - 42, 10, 32, 14), micro,
                TextFormatFlags.Right | TextFormatFlags.Top | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, Caption ?? string.Empty, ArkTheme.Display(11.5f),
                new Rectangle(18, 28, r.Width - 34, 24), caption,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, Micro ?? string.Empty, ArkTheme.Micro(8.5f),
                new Rectangle(18, 52, r.Width - 34, 14), ArkTheme.Muted,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix);

            using (Pen p = new Pen(ArkTheme.Line))
            {
                g.DrawLine(p, 0, r.Height - 1, r.Width, r.Height - 1);
            }
        }
    }
}
