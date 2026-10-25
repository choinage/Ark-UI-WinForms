using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ArkUi
{
    public enum ArkButtonKind
    {
        /// <summary>黑底工具条上的按钮：透明底、浅字、悬停亮起信号条。</summary>
        Tool,
        /// <summary>主操作：信号黄底、黑字。</summary>
        Primary,
        /// <summary>次要操作：白底、1px 黑框，悬停反色。</summary>
        Ghost
    }

    /// <summary>
    /// 方角自绘按钮。WinForms 默认按钮是圆角+渐变，跟这套语言不搭，所以整只重画。
    /// 文字里的 &amp; 用来注册 Alt 快捷键，绘制时去掉，避免出现"(&amp;I)"。
    /// </summary>
    public class ArkButton : Button
    {
        private bool _hover;
        private bool _down;
        private double _hoverAmt;

        public ArkButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            BackColor = ArkTheme.Ink;
            ForeColor = ArkTheme.Paper;
            Font = ArkTheme.Body(9.5f);
            Cursor = Cursors.Hand;
            Height = 40;
            UseVisualStyleBackColor = false;
            Kind = ArkButtonKind.Tool;
        }

        [DefaultValue(ArkButtonKind.Tool)]
        public ArkButtonKind Kind { get; set; }

        /// <summary>当前是否处于"已打开"状态，用于把按钮点亮成信号黄。</summary>
        public bool IsActive { get; set; }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            AnimateHover(true);
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _down = false;
            AnimateHover(false);
            base.OnMouseLeave(e);
        }

        /// <summary>悬停不是瞬间反色，而是 110ms 渐变过去，鼠标划过去时颜色是连续变化的。</summary>
        private void AnimateHover(bool on)
        {
            double from = _hoverAmt;
            double to = on ? 1.0 : 0.0;
            ArkAnim.Run(on ? 110 : 150, t =>
            {
                _hoverAmt = from + (to - from) * t;
                Invalidate();
            });
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _down = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _down = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            Rectangle r = ClientRectangle;
            string label = (Text ?? string.Empty).Replace("&", string.Empty);

            // 每种按钮各给"常态"和"悬停态"一组颜色，实际填充按 _hoverAmt 混合出来，
            // 这样鼠标划过去是渐变而不是硬切。
            Color baseFill, hoverFill, baseInk, hoverInk, border;
            bool signalBar = false;

            switch (Kind)
            {
                case ArkButtonKind.Primary:
                    baseFill = ArkTheme.Signal;
                    hoverFill = Color.FromArgb(0xEA, 0xE5, 0x00);
                    if (_down) { baseFill = hoverFill = Color.FromArgb(0xD8, 0xD3, 0x00); }
                    baseInk = hoverInk = ArkTheme.Ink;
                    border = ArkTheme.Ink;
                    break;

                case ArkButtonKind.Ghost:
                    baseFill = ArkTheme.Panel;
                    hoverFill = ArkTheme.Ink;          // 悬停反色：白底黑字 → 深底浅字
                    baseInk = ArkTheme.Ink;
                    hoverInk = ArkTheme.Paper;
                    border = ArkTheme.Ink;
                    break;

                default: // Tool
                    baseFill = ArkTheme.Ink;
                    hoverFill = _down ? Color.Black : ArkTheme.Hover;
                    baseInk = hoverInk = ArkTheme.Paper;
                    border = ArkTheme.LineDark;
                    signalBar = _hover || _hoverAmt > 0.02 || IsActive;
                    if (IsActive)
                    {
                        baseFill = hoverFill = ArkTheme.Signal;
                        baseInk = hoverInk = ArkTheme.Ink;
                    }
                    break;
            }

            double k = _down ? 1.0 : _hoverAmt;
            Color fill = ArkTheme.Blend(baseFill, hoverFill, k);
            Color ink = ArkTheme.Blend(baseInk, hoverInk, k);

            using (SolidBrush b = new SolidBrush(fill))
            {
                g.FillRectangle(b, r);
            }

            // 左侧 3px 信号条：Endfield 的"当前项"标记
            if (signalBar)
            {
                using (SolidBrush b = new SolidBrush(IsActive ? ArkTheme.Ink : ArkTheme.Signal))
                {
                    g.FillRectangle(b, new Rectangle(0, 0, 3, r.Height));
                }
            }

            using (Pen p = new Pen(border))
            {
                g.DrawRectangle(p, 0, 0, r.Width - 1, r.Height - 1);
            }

            // 按下时整体下移 1px，代替缩放动画
            Rectangle tr = new Rectangle(r.X, r.Y + (_down ? 1 : 0), r.Width, r.Height);
            TextRenderer.DrawText(g, label, Font, tr, ink,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            ForeColor = Enabled ? ArkTheme.Paper : ArkTheme.Muted;
            Invalidate();
        }
    }
}
