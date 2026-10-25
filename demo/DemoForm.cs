using System;
using System.Drawing;
using System.Windows.Forms;
using ArkUi;

namespace ArkUiDemo
{
    /// <summary>
    /// 演示界面：顶栏 72 / 左导航 180 / 内容区 / 底部状态条 30 / 右侧抽屉 356。
    /// 这几个数就是 Ark UI 在桌面端的骨架尺寸，改之前先想清楚，
    /// 因为页头那一排右对齐按钮是照着内容区宽度算的。
    /// </summary>
    public class DemoForm : Form
    {
        private const int TopH = 72;
        private const int RailW = 180;
        private const int StatusH = 30;

        private Panel pnlTop;
        private Panel pnlRail;
        private Panel pnlStage;
        private Panel pnlStatus;
        private Panel railBar;
        private Panel pnlWipe;
        private Label lbStatus;
        private ArkRailItem[] btnNav;
        private Panel[] pages;
        private ArkDrawer drawer;
        private ArkListView lv;

        private int _page = -1;

        public DemoForm()
        {
            Text = "Ark UI for WinForms";
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(1180, 660);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ArkTheme.Paper;
            Font = ArkTheme.Body(9.5f);
            DoubleBuffered = true;

            BuildStatus();
            BuildRail();
            BuildStage();
            BuildTop();

            // Dock 的先后由 z-order 决定：后添加的在最上层，先被分配空间，
            // 所以顶栏最后加、内容区最先加，四条边才不会被 Fill 吃掉。
            Controls.Add(pnlStage);
            Controls.Add(pnlStatus);
            Controls.Add(pnlRail);
            Controls.Add(pnlTop);

            ShowPage(0);
            SetStatus("按左侧导航切换页面，列表页右上角的按钮会展开右侧抽屉。");
        }

        // ---------------- 顶栏 ----------------

        private void BuildTop()
        {
            pnlTop = new Panel { Dock = DockStyle.Top, Height = TopH, BackColor = ArkTheme.Ink };

            // 左端 4px 信号黄，给黑条一个起点，免得整块黑压过去
            Panel edge = new Panel { Left = 0, Top = 0, Width = 4, Height = TopH, BackColor = ArkTheme.Signal };

            Label brand = new Label
            {
                Text = "ARK UI",
                Font = ArkTheme.Display(15f),
                ForeColor = ArkTheme.Paper,
                AutoSize = true,
                Left = 26,
                Top = 16
            };

            Label micro = new Label
            {
                Text = "WINFORMS CONTROL SET  ·  ENDFIELD FAMILY  ·  DEPTH 3",
                Font = ArkTheme.Micro(8f),
                ForeColor = ArkTheme.Muted,
                AutoSize = true,
                Left = 27,
                Top = 44
            };

            pnlTop.Controls.Add(edge);
            pnlTop.Controls.Add(brand);
            pnlTop.Controls.Add(micro);

            // 三个工具按钮从右往左排，间距 8
            string[] caption = { "关于", "重置数据", "导出" };
            int right = 24;
            for (int i = 0; i < caption.Length; i++)
            {
                ArkButton b = new ArkButton
                {
                    Text = caption[i],
                    Kind = ArkButtonKind.Tool,
                    Width = 96,
                    Height = 32,
                    Top = 20
                };
                b.Tag = right;
                b.Click += OnToolButtonClick;
                pnlTop.Controls.Add(b);
                right += b.Width + 8;
            }

            pnlTop.Resize += delegate { AlignTopButtons(); };
            AlignTopButtons();
        }

        /// <summary>右对齐靠 Tag 存"离右边多远"，窗口一窄就按新宽度重算，不写死坐标。</summary>
        private void AlignTopButtons()
        {
            foreach (Control c in pnlTop.Controls)
            {
                if (c.Tag == null) continue;
                c.Left = pnlTop.ClientSize.Width - (int)c.Tag - c.Width;
            }
        }

        private void OnToolButtonClick(object sender, EventArgs e)
        {
            ArkButton b = (ArkButton)sender;
            SetStatus("「" + b.Text + "」被点击。工具条上只放跟当前页面无关的操作。");
            if (b.Text == "重置数据") FillList();
        }

        // ---------------- 左侧导航 ----------------

        private void BuildRail()
        {
            pnlRail = new Panel { Dock = DockStyle.Left, Width = RailW, BackColor = ArkTheme.Rail };

            string[] index = { "01", "02", "03" };
            string[] caption = { "设备清单", "组件标本", "关于" };
            string[] micro = { "ROSTER", "SPECIMEN", "ABOUT" };

            btnNav = new ArkRailItem[index.Length];
            for (int i = 0; i < index.Length; i++)
            {
                int idx = i;
                Panel slot = new Panel
                {
                    Width = RailW,
                    Height = 74,
                    Left = 0,
                    Top = i * 74,
                    BackColor = ArkTheme.Rail
                };

                // 文字三行交给 ArkRailItem 画，slot 只做容器和命中区域
                ArkRailItem face = new ArkRailItem
                {
                    IndexText = index[i],
                    Caption = caption[i],
                    Micro = micro[i],
                    Dock = DockStyle.Fill
                };
                face.MouseDown += delegate { ShowPage(idx); };
                slot.Controls.Add(face);

                pnlRail.Controls.Add(slot);
                btnNav[i] = face;
            }

            // 会滑动的那条黄竖条：不画在导航项里，切页时从上一项滑到下一项
            railBar = new Panel { Width = 4, Height = 74, Left = 0, Top = 0, BackColor = ArkTheme.Signal };
            pnlRail.Controls.Add(railBar);
            railBar.BringToFront();
        }

        // ---------------- 状态条 ----------------

        private void BuildStatus()
        {
            pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = StatusH, BackColor = ArkTheme.Paper };

            Panel line = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ArkTheme.Line };

            lbStatus = new Label
            {
                Text = "",
                Font = ArkTheme.Body(9f),
                ForeColor = ArkTheme.Muted,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0)
            };

            // 先加的排在 z-order 顶层，先被 Dock 分配空间：
            // 1px 分隔线要先占顶部，Fill 的状态文字才能落在它下面
            pnlStatus.Controls.Add(line);
            pnlStatus.Controls.Add(lbStatus);
        }

        private void SetStatus(string text)
        {
            lbStatus.Text = text;
            lbStatus.ForeColor = ArkTheme.Muted;
        }

        /// <summary>确认类反馈用校验绿，只在这里出现。</summary>
        private void SetStatusOk(string text)
        {
            lbStatus.Text = text;
            lbStatus.ForeColor = Color.FromArgb(0x00, 0x8B, 0x58);
        }

        // ---------------- 内容区 ----------------

        private void BuildStage()
        {
            pnlStage = new Panel { Dock = DockStyle.Fill, BackColor = ArkTheme.Paper };

            pages = new Panel[3];
            pages[0] = BuildListPage();
            pages[1] = BuildSpecimenPage();
            pages[2] = BuildAboutPage();

            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].Dock = DockStyle.Fill;
                pages[i].Visible = false;
                pnlStage.Controls.Add(pages[i]);
            }

            // 换页时横扫过去的黄条，一次 260ms，纯粹是让切换有方向感
            pnlWipe = new Panel { Width = 0, BackColor = ArkTheme.Signal, Visible = false };
            pnlStage.Controls.Add(pnlWipe);
        }

        private void ShowPage(int index)
        {
            if (index == _page) return;
            _page = index;

            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].Visible = i == index;
                btnNav[i].IsActive = i == index;
            }

            // 黄竖条滑到目标项
            int to = index * 74;
            int from = railBar.Top;
            ArkAnim.Run(190, t => { railBar.Top = ArkAnim.Lerp(from, to, t); });

            // 横扫一次
            pnlWipe.Visible = true;
            pnlWipe.BringToFront();
            pnlWipe.Top = 0;
            pnlWipe.Height = pnlStage.ClientSize.Height;
            pnlWipe.Left = 0;
            pnlWipe.Width = 0;
            ArkAnim.Run(260, t =>
            {
                pnlWipe.Width = (int)Math.Round(pnlStage.ClientSize.Width * t);
                pnlWipe.Left = 0;
            }, delegate { pnlWipe.Visible = false; });
        }

        private Panel BuildListPage()
        {
            Panel page = new Panel { BackColor = ArkTheme.Paper };

            // 页头
            Panel head = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = ArkTheme.Paper };
            Label title = new Label
            {
                Text = "设备清单",
                Font = ArkTheme.Display(18f),
                ForeColor = ArkTheme.Ink,
                AutoSize = true,
                Left = 24,
                Top = 22
            };
            Label mt = new Label
            {
                Text = "ROSTER / FIELD RECORDS",
                Font = ArkTheme.Micro(9f),
                ForeColor = ArkTheme.Muted,
                AutoSize = true,
                Left = 26,
                Top = 58
            };
            Label no = new Label
            {
                Text = "01",
                Font = ArkTheme.Display(40f),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD4),
                AutoSize = true
            };

            ArkButton btnOpen = new ArkButton
            {
                Text = "展开录入",
                Kind = ArkButtonKind.Primary,
                Width = 120,
                Height = 36
            };
            btnOpen.Click += delegate
            {
                drawer.SetOpen(!drawer.IsOpen);
                btnOpen.Text = drawer.IsOpen ? "收起录入" : "展开录入";
            };

            head.Controls.Add(title);
            head.Controls.Add(mt);
            head.Controls.Add(no);
            head.Controls.Add(btnOpen);

            head.Resize += delegate
            {
                no.Left = head.ClientSize.Width - no.Width - 172;
                no.Top = 16;
                btnOpen.Left = head.ClientSize.Width - btnOpen.Width - 24;
                btnOpen.Top = 30;
            };
            head.HandleCreated += delegate { btnOpen.Left = head.ClientSize.Width - 144; };

            // 查询条
            Panel query = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = ArkTheme.Paper };
            TextBox key = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = ArkTheme.Body(10f),
                Left = 24,
                Top = 10,
                Width = 240
            };
            query.Controls.Add(key);

            int left = 276;
            string[] cap = { "按编号", "按名称", "全部" };
            for (int i = 0; i < cap.Length; i++)
            {
                ArkButton b = new ArkButton
                {
                    Text = cap[i],
                    Kind = ArkButtonKind.Ghost,
                    Left = left,
                    Top = 8,
                    Width = 88,
                    Height = 30
                };
                b.Click += delegate { FillList(); SetStatus("按「" + b.Text + "」重新查询。"); };
                query.Controls.Add(b);
                left += 96;
            }

            // 列表
            lv = new ArkListView { Dock = DockStyle.Fill, FillLastColumn = true };
            lv.Columns.Add("编号", 110);
            lv.Columns.Add("名称", 240);
            lv.Columns.Add("位置", 200);
            lv.Columns.Add("状态", 120);

            // 抽屉
            drawer = new ArkDrawer();
            drawer.SlideChanged = delegate(double s)
            {
                // 抽屉挡住列表右侧时，列宽只算到露出来的部分，
                // 否则最后一列永远藏在抽屉底下
                Panel host = drawer.Parent;
                lv.WidthLimit = (drawer.Visible && host != null)
                    ? host.ClientSize.Width - (int)Math.Round(drawer.DrawerWidth * s)
                    : 0;
                lv.FillLastColumnWidth();
            };
            BuildDrawerBody();

            page.Controls.Add(lv);
            page.Controls.Add(query);
            page.Controls.Add(head);
            page.Controls.Add(drawer);

            // Dock 顺序反过来：先 Fill，再 Bottom，最后 Top
            lv.BringToFront();
            query.BringToFront();
            head.BringToFront();

            page.Resize += delegate { if (drawer != null) drawer.LayoutDrawer(); };

            FillList();
            return page;
        }

        private void BuildDrawerBody()
        {
            Panel head = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = ArkTheme.Ink };
            Label t = new Label
            {
                Text = "录入设备",
                Font = ArkTheme.Display(11f),
                ForeColor = ArkTheme.Paper,
                AutoSize = true,
                Left = 18,
                Top = 12
            };
            Label m = new Label
            {
                Text = "NEW ENTRY",
                Font = ArkTheme.Micro(8f),
                ForeColor = ArkTheme.Signal,
                AutoSize = true,
                Left = 19,
                Top = 34
            };
            CloseGlyph close = new CloseGlyph();
            close.Click += delegate
            {
                drawer.SetOpen(false);
            };

            head.Controls.Add(t);
            head.Controls.Add(m);
            head.Controls.Add(close);
            head.Resize += delegate { close.Left = head.ClientSize.Width - close.Width - 14; close.Top = 18; };

            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = ArkTheme.Panel, AutoScroll = true, Padding = new Padding(18, 12, 18, 12) };

            int y = 8;
            AddField(body, ref y, "编号", "CODE", "IT-0007");
            AddField(body, ref y, "名称", "NAME", "");
            AddField(body, ref y, "位置", "LOCATION", "");

            ArkButton add = new ArkButton
            {
                Text = "添加",
                Kind = ArkButtonKind.Primary,
                Left = 18,
                Top = y + 18,
                Width = 100,
                Height = 34
            };
            add.Click += delegate
            {
                ListViewItem it = new ListViewItem("IT-0007");
                it.SubItems.Add("新录入的设备");
                it.SubItems.Add("未分配");
                it.SubItems.Add("待检");
                lv.Items.Add(it);
                drawer.SetOpen(false);
                SetStatusOk("已添加 1 条记录，写进内存，未落盘。");
            };

            ArkButton cancel = new ArkButton
            {
                Text = "清空",
                Kind = ArkButtonKind.Ghost,
                Left = 126,
                Top = y + 18,
                Width = 100,
                Height = 34
            };
            cancel.Click += delegate { drawer.SetOpen(false); SetStatus("已取消录入。"); };

            body.Controls.Add(add);
            body.Controls.Add(cancel);

            drawer.Controls.Add(body);
            drawer.Controls.Add(head);
            body.BringToFront();
            head.BringToFront();
        }

        /// <summary>一对"标签 + 输入框"，纵向排列，返回输入框方便取值。</summary>
        private TextBox AddField(Control parent, ref int y, string label, string micro, string value)
        {
            Label cap = new Label
            {
                Text = label + "  /  " + micro,
                Font = ArkTheme.Micro(8.5f),
                ForeColor = ArkTheme.Muted,
                AutoSize = true,
                Left = 20,
                Top = y
            };
            parent.Controls.Add(cap);
            y += 20;

            Panel box = new Panel
            {
                Left = 20,
                Top = y,
                Width = 300,
                Height = 34,
                BackColor = ArkTheme.FieldBg
            };
            box.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(ArkTheme.Line))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, box.Width - 1, box.Height - 1);
                }
            };

            TextBox tb = new TextBox
            {
                Text = value,
                BorderStyle = BorderStyle.None,
                BackColor = ArkTheme.FieldBg,
                ForeColor = ArkTheme.Ink,
                Font = ArkTheme.Body(10f),
                Left = 10,
                Top = 8,
                Width = 280
            };
            box.Controls.Add(tb);
            parent.Controls.Add(box);

            y += 34 + 14;
            return tb;
        }

        private void FillList()
        {
            lv.Items.Clear();
            string[,] rows =
            {
                { "IT-0001", "三相异步电机", "A 区 3 号机位", "运行" },
                { "IT-0002", "变频控制柜", "A 区 3 号机位", "运行" },
                { "IT-0003", "液压泵站", "B 区 1 号平台", "待检" },
                { "IT-0004", "冷却塔", "B 区 2 号平台", "运行" },
                { "IT-0005", "空压机", "C 区 1 号机房", "停机" },
                { "IT-0006", "配电柜", "C 区 1 号机房", "运行" }
            };
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                ListViewItem it = new ListViewItem(rows[i, 0]);
                it.SubItems.Add(rows[i, 1]);
                it.SubItems.Add(rows[i, 2]);
                it.SubItems.Add(rows[i, 3]);
                lv.Items.Add(it);
            }
            lv.SignalRowIndex = 0;
        }

        // ---------------- 组件标本页 ----------------

        private Panel BuildSpecimenPage()
        {
            Panel page = new Panel { BackColor = ArkTheme.Paper };

            Panel head = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = ArkTheme.Paper };
            Label title = new Label
            {
                Text = "组件标本",
                Font = ArkTheme.Display(18f),
                ForeColor = ArkTheme.Ink,
                AutoSize = true,
                Left = 24,
                Top = 22
            };
            Label mt = new Label
            {
                Text = "SPECIMEN / TOKENS AND CONTROLS",
                Font = ArkTheme.Micro(9f),
                ForeColor = ArkTheme.Muted,
                AutoSize = true,
                Left = 26,
                Top = 58
            };
            head.Controls.Add(title);
            head.Controls.Add(mt);

            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = ArkTheme.Paper, AutoScroll = true };

            // 四个色块
            string[,] swatch =
            {
                { "墨 INK", "#191919" },
                { "纸 PAPER", "#F2F2F0" },
                { "信号 SIGNAL", "#FFFA00" },
                { "校验 VERIFY", "#00FFA2" }
            };
            Color[] colors =
            {
                ArkTheme.Ink, ArkTheme.Paper, ArkTheme.Signal, ArkTheme.Verify
            };

            for (int i = 0; i < 4; i++)
            {
                Panel sw = new Panel
                {
                    Left = 24 + i * 232,
                    Top = 12,
                    Width = 216,
                    Height = 120,
                    BackColor = colors[i]
                };
                sw.Paint += delegate(object s, PaintEventArgs e)
                {
                    using (Pen p = new Pen(ArkTheme.Line))
                    {
                        e.Graphics.DrawRectangle(p, 0, 0, sw.Width - 1, sw.Height - 1);
                    }
                };

                Label name = new Label
                {
                    Text = swatch[i, 0],
                    Font = ArkTheme.Micro(9f),
                    ForeColor = i == 0 ? ArkTheme.Paper : ArkTheme.Ink,
                    AutoSize = true,
                    Left = 12,
                    Top = 88
                };
                Label hex = new Label
                {
                    Text = swatch[i, 1],
                    Font = ArkTheme.Micro(8f),
                    ForeColor = i == 0 ? ArkTheme.Muted : ArkTheme.Ink,
                    AutoSize = true,
                    Left = 12,
                    Top = 68
                };
                sw.Controls.Add(name);
                sw.Controls.Add(hex);
                body.Controls.Add(sw);
            }

            // 三种按钮
            ArkButton primary = new ArkButton
            {
                Text = "主操作 Primary",
                Kind = ArkButtonKind.Primary,
                Left = 24,
                Top = 160,
                Width = 160,
                Height = 38
            };
            ArkButton ghost = new ArkButton
            {
                Text = "次要操作 Ghost",
                Kind = ArkButtonKind.Ghost,
                Left = 196,
                Top = 160,
                Width = 160,
                Height = 38
            };
            ArkButton tool = new ArkButton
            {
                Text = "工具条 Tool",
                Kind = ArkButtonKind.Tool,
                Left = 368,
                Top = 160,
                Width = 160,
                Height = 38
            };
            ArkButton disabled = new ArkButton
            {
                Text = "禁用 Disabled",
                Kind = ArkButtonKind.Ghost,
                Left = 540,
                Top = 160,
                Width = 160,
                Height = 38,
                Enabled = false
            };

            primary.Click += delegate { SetStatusOk("主操作用信号黄底、黑字，一屏只留一个。"); };
            ghost.Click += delegate { SetStatus("次要操作白底黑框，悬停反色。"); };
            tool.Click += delegate { SetStatus("工具条按钮只放在顶部黑条上。"); };

            body.Controls.Add(primary);
            body.Controls.Add(ghost);
            body.Controls.Add(tool);
            body.Controls.Add(disabled);

            Label note = new Label
            {
                Text = "三种按钮的区别只有配色和悬停行为，尺寸、圆角、字号全部一致：高 38、方角、5% 的字号差。",
                Font = ArkTheme.Body(9.5f),
                ForeColor = ArkTheme.Muted,
                Left = 24,
                Top = 214,
                Width = 900,
                Height = 22
            };
            body.Controls.Add(note);

            // 抓手 + 可拖动改高度的面板
            ArkRule rule = new ArkRule { Dock = DockStyle.None, Left = 24, Width = 1000, Top = 254 };
            body.Controls.Add(rule);

            Panel grow = new Panel { Left = 24, Top = 300, Width = 1000, Height = 120, BackColor = ArkTheme.Panel };
            Label growTip = new Label
            {
                Text = "按住上面的短横上下拖，改这块面板的高度。拖拽增量用 Cursor.Position 算，别用 MouseEventArgs.Y。",
                Font = ArkTheme.Body(9.5f),
                ForeColor = ArkTheme.Muted,
                Left = 16,
                Top = 14,
                Width = 940,
                Height = 40
            };
            grow.Controls.Add(growTip);
            grow.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(ArkTheme.Line))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, grow.Width - 1, grow.Height - 1);
                }
            };

            ArkGrip grip = new ArkGrip { Dock = DockStyle.None, Left = 24, Width = 1000, Top = 270 };
            int dragStartY = 0;
            int dragStartH = 0;

            grip.MouseDown += delegate(object s, MouseEventArgs e)
            {
                dragStartY = Cursor.Position.Y;
                dragStartH = grow.Height;
                grip.Dragging = true;
                grip.Capture = true;
            };
            grip.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (!grip.Dragging) return;
                int dy = Cursor.Position.Y - dragStartY;
                int h = dragStartH - dy;         // 往上拖变高
                if (h < 80) h = 80;
                if (h > 420) h = 420;
                grow.Height = h;
            };
            grip.MouseUp += delegate { grip.Dragging = false; grip.Capture = false; };

            body.Controls.Add(grip);
            body.Controls.Add(grow);

            page.Controls.Add(body);
            page.Controls.Add(head);
            body.BringToFront();
            head.BringToFront();
            return page;
        }

        // ---------------- 关于页 ----------------

        private Panel BuildAboutPage()
        {
            Panel page = new Panel { BackColor = ArkTheme.Paper };

            Panel head = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = ArkTheme.Paper };
            Label title = new Label
            {
                Text = "关于",
                Font = ArkTheme.Display(18f),
                ForeColor = ArkTheme.Ink,
                AutoSize = true,
                Left = 24,
                Top = 22
            };
            Label mt = new Label
            {
                Text = "ABOUT / USAGE",
                Font = ArkTheme.Micro(9f),
                ForeColor = ArkTheme.Muted,
                AutoSize = true,
                Left = 26,
                Top = 58
            };
            head.Controls.Add(title);
            head.Controls.Add(mt);

            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = ArkTheme.Paper, AutoScroll = true, Padding = new Padding(24, 8, 24, 24) };

            string[] lines =
            {
                "这是一套给 Windows Forms 用的界面语言实现，从配色、几何到控件全部自绘。",
                "取色和结构参考 Ark UI 的 endfield 风格族：炭黑作墨、米白作纸，信号黄只给主操作和选中状态。",
                "",
                "库里有六个公开类型：",
                "    ArkTheme      配色、字体、两色混合",
                "    ArkAnim       共享 Timer 的补间引擎",
                "    ArkButton     三种形态的方角按钮",
                "    ArkRailItem   左侧导航项（编号 / 中文 / 英文小标）",
                "    ArkListView   黑表头、斑马纹、信号黄选中行的列表",
                "    ArkGrip       可拖动改面板高度的水平抓手",
                "",
                "Demo 里另有 ArkDrawer，演示右侧抽屉怎么滑入。它跟父容器的布局绑得紧，",
                "所以留在 demo 当示例，没有放进库。",
                "",
                "动画关掉：ArkAnim.Enabled = false（截图、远程桌面、自动化测试时用）。",
                "这一条最容易被忘，忘了就会出现截图截到动画中间帧的情况。"
            };

            int y = 8;
            for (int i = 0; i < lines.Length; i++)
            {
                Label l = new Label
                {
                    Text = lines[i],
                    Font = lines[i].StartsWith("    ") ? ArkTheme.Micro(10f) : ArkTheme.Body(10f),
                    ForeColor = lines[i].StartsWith("    ") ? ArkTheme.Ink : ArkTheme.Muted,
                    AutoSize = false,
                    Left = 24,
                    Top = y,
                    Width = 980,
                    Height = 22
                };
                body.Controls.Add(l);
                y += 24;
            }

            page.Controls.Add(body);
            page.Controls.Add(head);
            body.BringToFront();
            head.BringToFront();
            return page;
        }

        /// <summary>抽屉右上角那个小叉，用自绘做一个"×"，比 &amp;times; 字符稳。</summary>
        private class CloseGlyph : Control
        {
            private bool _hot;

            public CloseGlyph()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                       | ControlStyles.OptimizedDoubleBuffer, true);
                Width = 28;
                Height = 24;
                Cursor = Cursors.Hand;
                BackColor = ArkTheme.Ink;
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
                using (Pen p = new Pen(_hot ? ArkTheme.Signal : ArkTheme.Muted))
                {
                    int x0 = (Width - 10) / 2;
                    int y0 = (Height - 10) / 2;
                    e.Graphics.DrawLine(p, x0, y0, x0 + 10, y0 + 10);
                    e.Graphics.DrawLine(p, x0 + 10, y0, x0, y0 + 10);
                }
            }
        }
    }
}
