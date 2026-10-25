using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ArkUi
{
    /// <summary>1px 分隔线的快捷写法，避免每个页面都 new 一个 Panel。</summary>
    public class ArkRule : Panel
    {
        public ArkRule()
        {
            BackColor = ArkTheme.Line;
            Height = 1;
            Dock = DockStyle.Top;
        }
    }

    /// <summary>
    /// 压在面板顶上的水平抓手，按住上下拖可以改面板高度（常用于图表区）。
    ///
    /// 平时是一条细线加中间一小段灰短横，鼠标移上去短横变长变深，
    /// 按下去换成信号黄，拖的时候能清楚看到自己在动哪块。
    /// 具体改哪个面板的高度由窗体处理，这里只管画和鼠标状态。
    /// </summary>
    public class ArkGrip : Control
    {
        private bool _hot;
        private bool _down;

        public ArkGrip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);
            BackColor = ArkTheme.Panel;
            Dock = DockStyle.Top;
            Height = 12;
            Cursor = Cursors.SizeNS;
        }

        /// <summary>拖动中由窗体置 true，松手置 false。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Dragging
        {
            get { return _down; }
            set
            {
                if (_down == value) return;
                _down = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hot = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hot = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            // 顶边一条 1px 线，和页面其他分隔线同一颜色；按住时加重
            using (Pen p = new Pen(_down ? ArkTheme.Ink : ArkTheme.Line))
            {
                g.DrawLine(p, 0, 0, ClientSize.Width, 0);
            }

            // 中间短横：常态 56px 灰，悬停拉长到 76px 转墨色，按住换成信号黄
            int w = (_hot || _down) ? 76 : 56;
            int x = (ClientSize.Width - w) / 2;
            int y = ClientSize.Height / 2 - 1;
            Color c = _down ? ArkTheme.Signal : (_hot ? ArkTheme.Ink : ArkTheme.Muted);
            using (SolidBrush b = new SolidBrush(c))
            {
                g.FillRectangle(b, x, y, w, 3);
            }
        }
    }
}
