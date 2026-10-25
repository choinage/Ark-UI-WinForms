using System;
using System.Drawing;

namespace ArkUi
{
    /// <summary>
    /// 统一的配色、字体与尺寸。
    /// 取值遵循 Endfield 家族：炭黑作墨、米白作纸、信号黄只用于选中和主操作，
    /// 验证绿(Verify)只表示"已保存/已落盘"这类确认状态，不作装饰。
    /// </summary>
    public static class ArkTheme
    {
        // ---- 三个基色 ----
        public static readonly Color Ink = Color.FromArgb(0x19, 0x19, 0x19);
        public static readonly Color Paper = Color.FromArgb(0xF2, 0xF2, 0xF0);
        public static readonly Color Signal = Color.FromArgb(0xFF, 0xFA, 0x00);

        // ---- 中性派生色 ----
        public static readonly Color Rail = Color.FromArgb(0xE9, 0xE9, 0xE5);
        public static readonly Color RailHover = Color.FromArgb(0xDE, 0xDE, 0xD8);
        public static readonly Color Line = Color.FromArgb(0xD2, 0xD2, 0xCD);
        public static readonly Color LineDark = Color.FromArgb(0x38, 0x38, 0x38);
        public static readonly Color Muted = Color.FromArgb(0x8A, 0x8A, 0x85);
        public static readonly Color Panel = Color.FromArgb(0xFF, 0xFF, 0xFF);
        public static readonly Color RowAlt = Color.FromArgb(0xFA, 0xFA, 0xF7);
        public static readonly Color Hover = Color.FromArgb(0x2E, 0x2E, 0x2E);
        public static readonly Color Verify = Color.FromArgb(0x00, 0xFF, 0xA2);

        // ---- 输入框 ----
        public static readonly Color FieldBg = Color.FromArgb(0xFF, 0xFF, 0xFF);
        public static readonly Color FieldReadOnly = Color.FromArgb(0xE4, 0xE4, 0xDF);

        public static Font Display(float size)
        {
            return new Font("Microsoft YaHei", size, FontStyle.Bold);
        }

        public static Font Body(float size)
        {
            return new Font("Microsoft YaHei", size, FontStyle.Regular);
        }

        /// <summary>小号大写英文标签。汉字没有窄体，英文改用 Arial Narrow 拉出技术感。</summary>
        public static Font Micro(float size)
        {
            return new Font("Arial Narrow", size, FontStyle.Bold);
        }

        /// <summary>两色线性混合，t 取 0~1。悬停和动画的中间帧都靠它算。</summary>
        public static Color Blend(Color a, Color b, double t)
        {
            if (t <= 0) return a;
            if (t >= 1) return b;
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }
    }
}
