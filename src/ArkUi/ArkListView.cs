using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ArkUi
{
    /// <summary>
    /// 列表控件：黑表头 + 白行 + 斑马纹 + 信号黄选中行。
    /// 系统默认表头是灰色渐变，改不掉颜色，只能整块自绘。
    /// </summary>
    public class ArkListView : ListView
    {
        public ArkListView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            OwnerDraw = true;
            View = View.Details;
            FullRowSelect = true;
            HideSelection = false;
            MultiSelect = false;
            GridLines = false;
            BorderStyle = BorderStyle.None;
            HeaderStyle = ColumnHeaderStyle.Nonclickable;
            BackColor = ArkTheme.Panel;
            ForeColor = ArkTheme.Ink;
            Font = ArkTheme.Body(9.5f);

            // 借一张 1px 宽的空图把行高撑到 30，TooNarrow 的默认行高太挤
            SmallImageList = new ImageList();
            SmallImageList.ImageSize = new Size(1, 30);
            SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
        }

        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(ArkTheme.Ink))
            {
                g.FillRectangle(b, e.Bounds);
            }

            using (Pen p = new Pen(ArkTheme.LineDark))
            {
                g.DrawLine(p, e.Bounds.Right - 1, e.Bounds.Top + 10,
                    e.Bounds.Right - 1, e.Bounds.Bottom - 10);
            }

            TextRenderer.DrawText(g, e.Header.Text, ArkTheme.Micro(9.5f),
                new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 14, e.Bounds.Height),
                ArkTheme.Paper,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            Color bg = e.Item.Selected
                ? ArkTheme.Signal
                : (e.ItemIndex % 2 == 1 ? ArkTheme.RowAlt : ArkTheme.Panel);

            using (SolidBrush b = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }

            // 逐行"揭示"：先铺一层半透明信号黄，再画字，字保持可读
            if (HighlightProvider != null)
            {
                float a = HighlightProvider(e.ItemIndex);
                if (a > 0.01f)
                {
                    int alpha = (int)(a * 200);
                    if (alpha > 255) alpha = 255;
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(alpha, ArkTheme.Signal)))
                    {
                        e.Graphics.FillRectangle(hb, e.Bounds);
                    }
                }
            }

            // 整行文字在这里一次画完。
            // 不在 OnDrawSubItem 里画，是因为行背景（DrawItem）与子项（DrawSubItem）
            // 的触发顺序在 Details 视图下不可靠，会出现背景盖掉第 2 列以后文字的情况。
            int x = e.Bounds.X;
            for (int i = 0; i < Columns.Count; i++)
            {
                int w = Columns[i].Width;
                string text = i < e.Item.SubItems.Count ? e.Item.SubItems[i].Text : string.Empty;

                Rectangle cell = new Rectangle(x + 10, e.Bounds.Y, Math.Max(0, w - 14), e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, text, Font, cell, ArkTheme.Ink,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                    | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                x += w;
            }

            using (Pen p = new Pen(ArkTheme.Line))
            {
                e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            // 长期标记行（例如排行榜第一名）：左边一条常驻信号黄竖条
            if (e.ItemIndex == SignalRowIndex)
            {
                using (SolidBrush b = new SolidBrush(ArkTheme.Signal))
                {
                    e.Graphics.FillRectangle(b, new Rectangle(e.Bounds.Left, e.Bounds.Top, 6, e.Bounds.Height));
                }
            }
        }

        /// <summary>需要常驻信号黄竖条的行号，-1 表示不标。</summary>
        public int SignalRowIndex = -1;

        /// <summary>某一行的临时高亮强度 0~1，返回 0 表示不亮。结果逐行闪一下靠它。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<int, float> HighlightProvider { get; set; }

        /// <summary>文字已在 OnDrawItem 里统一绘制，这里留空，避免重复。</summary>
        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
        }

        /// <summary>让最后一列吃掉剩余宽度，避免右侧留一大片空白。</summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            FillLastColumnWidth();
        }

        public void FillLastColumnWidth()
        {
            if (!FillLastColumn || Columns.Count == 0) return;

            int used = 0;
            for (int i = 0; i < Columns.Count - 1; i++)
            {
                used += Columns[i].Width;
            }

            // WidthLimit 用于"右侧被浮层面板盖住"的情况：
            // 列表本身仍是全宽，但列宽只算到可见区域为止，保证每一列都露得出来。
            int avail = ClientSize.Width;
            if (WidthLimit > 0 && WidthLimit < avail) avail = WidthLimit;

            int rest = avail - used - SystemInformation.VerticalScrollBarWidth - 8;
            if (rest < 80) rest = 80;

            ColumnHeader last = Columns[Columns.Count - 1];
            if (last.Width == rest) return;

            // 宽度没变化时 setter 不会给系统发消息，横向滚动条就会赖着不走。
            // 先错开 1px 再设回目标值，强制 ListView 重算一次滚动范围。
            last.Width = rest > 90 ? rest + 1 : rest - 1;
            last.Width = rest;
        }

        /// <summary>可见宽度上限。0 表示等于控件宽度；被浮层遮挡时设成露出来的那部分宽度。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int WidthLimit { get; set; }

        /// <summary>最后一列是否拉伸填满剩余宽度。</summary>
        public bool FillLastColumn { get; set; }
    }
}
