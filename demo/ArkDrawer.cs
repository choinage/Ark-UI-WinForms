using System;
using System.Drawing;
using System.Windows.Forms;
using ArkUi;

namespace ArkUiDemo
{
    /// <summary>
    /// 右侧录入抽屉：真正可复用的部分只有"按进度摆放"这件事，
    /// 打开/收起走 ArkAnim 的 180ms 缓动。放进 demo 而不是库，
    /// 是因为它必须知道自己的父容器，和具体怎么布局绑得很紧。
    /// </summary>
    public class ArkDrawer : Panel
    {
        private double _slide;          // 0 = 完全在右边界外，1 = 完全就位
        private bool _open;

        public ArkDrawer()
        {
            DrawerWidth = 356;
            BackColor = ArkTheme.Panel;
            Visible = false;          // 默认收起，靠 SetOpen(true) 滑出来
        }

        public int DrawerWidth { get; set; }

        public bool IsOpen { get { return _open; } }

        /// <summary>滑动进度，宿主用它算旁边列表的可见宽度。</summary>
        public double Slide { get { return _slide; } }

        /// <summary>进度变化时回调，参数是 0~1。</summary>
        public Action<double> SlideChanged { get; set; }

        public void SetOpen(bool open)
        {
            if (_open == open) return;
            _open = open;

            if (open)
            {
                Visible = true;
                BringToFront();
            }
            double from = _slide;
            double to = open ? 1.0 : 0.0;

            ArkAnim.Run(180, t =>
            {
                _slide = from + (to - from) * t;
                LayoutDrawer();
            }, () =>
            {
                if (!open) Visible = false;
            });
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            LayoutDrawer();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutDrawer();
        }

        /// <summary>宽度恒为 DrawerWidth，靠 Left 移出父容器右边界。</summary>
        public void LayoutDrawer()
        {
            if (Parent == null) return;

            Width = DrawerWidth;
            Height = Parent.ClientSize.Height;
            int hiddenLeft = Parent.ClientSize.Width;
            Left = hiddenLeft - (int)Math.Round(DrawerWidth * _slide);

            if (SlideChanged != null) SlideChanged(_slide);
        }
    }
}
