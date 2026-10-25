using System;
using System.Windows.Forms;

namespace ArkUi
{
    /// <summary>
    /// 极简补间引擎：一个共享的 Timer 驱动所有动画。
    /// 每帧只在 UI 线程回调一次，由各控件自己 Invalidate，
    /// 比每个控件挂一个 Timer 省事，也不会出现多个 Timer 抢着重绘的抖动。
    /// </summary>
    public static class ArkAnim
    {
        private sealed class Job
        {
            public long Start;
            public int Duration;
            public Action<double> Tick;
            public Action Done;
            public Func<double, double> Ease;
        }

        private static readonly System.Collections.Generic.List<Job> Jobs =
            new System.Collections.Generic.List<Job>();
        private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();
        private static Timer _timer;

        /// <summary>关掉后所有动画直接跳到终态，截图或远程桌面下更省事。</summary>
        public static bool Enabled = true;

        public static double EaseOutCubic(double t) { double u = 1 - t; return 1 - u * u * u; }
        public static double EaseOutQuad(double t) { double u = 1 - t; return 1 - u * u; }

        public static int Lerp(int a, int b, double t)
        {
            return a + (int)Math.Round((b - a) * t);
        }

        /// <summary>跑一段动画：tick 收到已经过缓动的 0~1，done 在最后一帧之后调用。</summary>
        public static void Run(int duration, Action<double> tick, Action done = null,
                               Func<double, double> ease = null)
        {
            if (tick == null) return;
            Func<double, double> fn = ease ?? EaseOutCubic;

            if (!Enabled || duration <= 0)
            {
                tick(1.0);
                if (done != null) done();
                return;
            }

            Job job = new Job
            {
                Start = Clock.ElapsedMilliseconds,
                Duration = duration,
                Tick = tick,
                Done = done,
                Ease = fn
            };
            Jobs.Add(job);

            // 先画一帧起点：有些动画的初始态和当前态不同，晚一帧才响应会显得卡
            job.Tick(fn(0));
            EnsureTimer();
        }

        private static void EnsureTimer()
        {
            if (_timer == null)
            {
                _timer = new Timer();
                _timer.Interval = 15;
                _timer.Tick += OnTick;
            }
            if (!_timer.Enabled) _timer.Start();
        }

        private static void OnTick(object sender, EventArgs e)
        {
            long now = Clock.ElapsedMilliseconds;
            for (int i = Jobs.Count - 1; i >= 0; i--)
            {
                Job job = Jobs[i];
                double raw = (now - job.Start) / (double)job.Duration;
                try
                {
                    if (raw >= 1.0)
                    {
                        Jobs.RemoveAt(i);
                        job.Tick(1.0);
                        if (job.Done != null) job.Done();
                    }
                    else
                    {
                        job.Tick(job.Ease(raw));
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 窗体在动画中途被关掉，丢帧就行，别让 Timer 把异常抛出去
                    Jobs.RemoveAt(i);
                }
                catch (InvalidOperationException)
                {
                    Jobs.RemoveAt(i);
                }
            }
            if (Jobs.Count == 0 && _timer != null) _timer.Stop();
        }
    }
}
