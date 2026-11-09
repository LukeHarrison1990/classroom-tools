// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - countdown, interval timer, stopwatch and exam clock.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    static class TimeText
    {
        public static string Clock(double seconds)
        {
            int s = (int)Math.Ceiling(Math.Max(0, seconds));
            if (s >= 3600) return string.Format("{0}:{1:00}:{2:00}", s / 3600, s / 60 % 60, s % 60);
            return string.Format("{0:00}:{1:00}", s / 60, s % 60);
        }

        public static string Template(string clock) { return clock.Length > 5 ? "0:00:00" : "00:00"; }
    }

    class CountdownForm : ToolForm
    {
        double total = 300, left = 300;
        DateTime endsAt;
        bool running, finished;
        NumericUpDown mins, secs;
        Button startBtn;
        CheckBox sound;

        public CountdownForm() : base("Countdown Timer", 760, 460)
        {
            foreach (int m in new[] { 1, 2, 3, 5, 10, 15, 20, 30 })
            {
                int minutes = m;
                AddButton(m + "m", delegate { SetTime(minutes * 60); });
            }
            AddLabel("Custom");
            mins = AddNumber(0, 600, 5, 58);
            AddLabel(":").Margin = new Padding(0, Theme.S(10), 0, 0);
            secs = AddNumber(0, 59, 0, 50);
            AddButton("Set", delegate { SetTime((int)mins.Value * 60 + (int)secs.Value); });
            startBtn = AddButton("Start", delegate { Toggle(); }, true);
            AddButton("Reset", delegate { SetTime((int)total); });
            AddButton("+1 min", delegate { Adjust(60); });
            sound = AddCheck("Sound", Store.Get("countdown.sound", "1") == "1");
            sound.CheckedChanged += delegate { Store.Set("countdown.sound", sound.Checked ? "1" : "0"); };
            AddWindowButtons(true);
            View.Click += delegate { Toggle(); };
            SetTime(Store.GetInt("countdown.last", 300));
        }

        void SetTime(int seconds)
        {
            if (seconds <= 0) seconds = 60;
            total = left = seconds;
            running = false; finished = false;
            Tick.Stop();
            Store.Set("countdown.last", seconds);
            mins.Value = Math.Min(mins.Maximum, seconds / 60); secs.Value = seconds % 60;
            startBtn.Text = "Start";
            View.Invalidate();
        }

        void Adjust(int by)
        {
            left += by; total = Math.Max(total, left);
            if (running) endsAt = endsAt.AddSeconds(by);
            finished = false;
            View.Invalidate();
        }

        void Toggle()
        {
            if (finished) { SetTime((int)total); return; }
            running = !running;
            if (running) { endsAt = DateTime.UtcNow.AddSeconds(left); Tick.Start(); }
            else Tick.Stop();
            startBtn.Text = running ? "Pause" : "Resume";
            View.Invalidate();
        }

        protected override void OnTick()
        {
            if (running)
            {
                left = (endsAt - DateTime.UtcNow).TotalSeconds;
                if (left <= 0)
                {
                    left = 0; running = false; finished = true;
                    startBtn.Text = "Restart";
                    if (sound.Checked) Sound.Alarm();
                }
            }
            else if (!finished) Tick.Stop();
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { Toggle(); return true; }
            if (key == Keys.R) { SetTime((int)total); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            double frac = total <= 0 ? 0 : left / total;
            Color c = frac > 0.25 ? Theme.Green : frac > 0.1 ? Theme.Amber : Theme.Red;
            bool flash = finished && DateTime.Now.Millisecond < 500;
            g.Clear(flash ? Theme.Red : Theme.Bg);

            float pad = r.Height * 0.06f;
            float barH = Math.Max(10, r.Height * 0.07f);
            var bar = new RectangleF(pad, r.Height - pad - barH, r.Width - 2 * pad, barH);
            Draw.FillRound(g, bar, barH / 2, Theme.Panel2);
            if (frac > 0)
                Draw.FillRound(g, new RectangleF(bar.X, bar.Y, Math.Max(barH, bar.Width * (float)frac), barH), barH / 2, c);

            string text = finished ? "Time's up" : TimeText.Clock(left);
            var box = new RectangleF(pad, pad, r.Width - 2 * pad, bar.Y - 2 * pad);
            Draw.Text(g, text, box, flash ? Color.White : (finished ? Theme.Red : Theme.Text), FontStyle.Bold, false, 600f, finished ? null : TimeText.Template(text));
            if (!running && !finished)
                Draw.Label(g, left < total ? "Paused - click or press Space to resume" : "Click or press Space to start",
                    new RectangleF(0, bar.Y - Theme.S(30), r.Width, Theme.S(26)), Theme.Muted, 11f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    class IntervalForm : ToolForm
    {
        NumericUpDown work, rest, rounds;
        Button startBtn;
        bool running, done;
        int round = 1;
        bool resting;
        double left;
        DateTime endsAt;

        public IntervalForm() : base("Interval Timer", 720, 460)
        {
            AddLabel("Work (sec)");
            work = AddNumber(5, 3600, Store.GetInt("interval.work", 120), 64);
            AddLabel("Rest (sec)");
            rest = AddNumber(0, 3600, Store.GetInt("interval.rest", 30), 64);
            AddLabel("Rounds");
            rounds = AddNumber(1, 99, Store.GetInt("interval.rounds", 5), 52);
            startBtn = AddButton("Start", delegate { Toggle(); }, true);
            AddButton("Reset", delegate { Reset(); });
            AddWindowButtons(true);
            Reset();
        }

        void Reset()
        {
            running = false; done = false; resting = false; round = 1;
            left = (double)work.Value;
            Tick.Stop();
            startBtn.Text = "Start";
            View.Invalidate();
        }

        void Toggle()
        {
            if (done) { Reset(); return; }
            running = !running;
            if (running)
            {
                Store.Set("interval.work", (int)work.Value); Store.Set("interval.rest", (int)rest.Value); Store.Set("interval.rounds", (int)rounds.Value);
                endsAt = DateTime.UtcNow.AddSeconds(left);
                Tick.Start();
            }
            else Tick.Stop();
            startBtn.Text = running ? "Pause" : "Resume";
            View.Invalidate();
        }

        protected override void OnTick()
        {
            if (!running) return;
            left = (endsAt - DateTime.UtcNow).TotalSeconds;
            if (left <= 0)
            {
                if (!resting && rest.Value > 0 && round < rounds.Value) { resting = true; left = (double)rest.Value; Sound.Change(); }
                else if (round < rounds.Value) { resting = false; round++; left = (double)work.Value; Sound.Change(); }
                else { done = true; running = false; left = 0; Tick.Stop(); startBtn.Text = "Restart"; Sound.Alarm(); }
                endsAt = DateTime.UtcNow.AddSeconds(left);
            }
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { Toggle(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            Color c = done ? Theme.Purple : resting ? Theme.Amber : Theme.Green;
            g.Clear(Draw.Mix(Theme.Bg, c, 0.18));
            float h = r.Height;
            Draw.Text(g, done ? "Finished" : resting ? "Rest" : "Work", new RectangleF(0, h * 0.04f, r.Width, h * 0.2f), c, FontStyle.Bold, false, 200f, null);
            string t = TimeText.Clock(left);
            Draw.Text(g, t, new RectangleF(r.Width * 0.05f, h * 0.24f, r.Width * 0.9f, h * 0.52f), Theme.Text, FontStyle.Bold, false, 600f, TimeText.Template(t));
            Draw.Text(g, "Round " + round + " of " + (int)rounds.Value, new RectangleF(0, h * 0.78f, r.Width, h * 0.12f), Theme.Muted, FontStyle.Regular, false, 120f, null);
            int n = (int)rounds.Value;
            float dot = Math.Min(h * 0.035f, r.Width / (n * 2.5f));
            float x = r.Width / 2f - (n * dot * 2.2f) / 2f;
            for (int i = 1; i <= n; i++)
            {
                using (var b = new SolidBrush(i < round || done ? c : i == round ? Theme.Text : Theme.Panel2))
                    g.FillEllipse(b, x + (i - 1) * dot * 2.2f, h * 0.92f, dot * 1.6f, dot * 1.6f);
            }
        }
    }

    class StopwatchForm : ToolForm
    {
        readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        readonly List<TimeSpan> laps = new List<TimeSpan>();
        Button startBtn;

        public StopwatchForm() : base("Stopwatch", 720, 420)
        {
            startBtn = AddButton("Start", delegate { Toggle(); }, true);
            AddButton("Lap", delegate { Lap(); });
            AddButton("Reset", delegate { watch.Reset(); laps.Clear(); startBtn.Text = "Start"; Tick.Stop(); View.Invalidate(); });
            AddButton("Copy laps", delegate { CopyLaps(); });
            AddWindowButtons(true);
            Tick.Interval = 50;
        }

        void Toggle()
        {
            if (watch.IsRunning) { watch.Stop(); Tick.Stop(); } else { watch.Start(); Tick.Start(); }
            startBtn.Text = watch.IsRunning ? "Stop" : "Start";
            View.Invalidate();
        }

        void Lap()
        {
            if (watch.Elapsed.Ticks > 0) laps.Add(watch.Elapsed);
            View.Invalidate();
        }

        static string Fmt(TimeSpan t)
        {
            return string.Format("{0:00}:{1:00}.{2}", (int)t.TotalMinutes, t.Seconds, t.Milliseconds / 100);
        }

        void CopyLaps()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < laps.Count; i++)
                sb.AppendLine((i + 1) + "\t" + Fmt(laps[i]) + "\t" + Fmt(laps[i] - (i > 0 ? laps[i - 1] : TimeSpan.Zero)));
            if (sb.Length > 0) Clipboard.SetText(sb.ToString());
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { Toggle(); return true; }
            if (key == Keys.L || key == Keys.Enter) { Lap(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            float lapW = laps.Count > 0 ? Math.Min(r.Width * 0.34f, Theme.S(300)) : 0;
            var main = new RectangleF(r.Width * 0.04f, r.Height * 0.08f, r.Width - lapW - r.Width * 0.08f, r.Height * 0.84f);
            Draw.Text(g, Fmt(watch.Elapsed), main, watch.IsRunning ? Theme.Text : Theme.Muted, FontStyle.Bold, false, 500f, "00:00.0");
            if (lapW > 0)
            {
                var panel = new RectangleF(r.Width - lapW, 0, lapW, r.Height);
                using (var b = new SolidBrush(Theme.Panel)) g.FillRectangle(b, panel);
                float row = Theme.S(30);
                int show = Math.Max(1, (int)(r.Height / row) - 1);
                Draw.Label(g, "Lap        Total          Split", new RectangleF(panel.X + 12, 4, lapW - 16, row), Theme.Muted, 10f, FontStyle.Regular, StringAlignment.Near);
                int first = Math.Max(0, laps.Count - show);
                for (int i = first; i < laps.Count; i++)
                {
                    float y = row + (i - first) * row;
                    TimeSpan split = laps[i] - (i > 0 ? laps[i - 1] : TimeSpan.Zero);
                    Draw.Label(g, string.Format("{0,-6}{1}     {2}", i + 1, Fmt(laps[i]), Fmt(split)), new RectangleF(panel.X + 12, y, lapW - 16, row), Theme.Text, 12f, FontStyle.Regular, StringAlignment.Near);
                }
            }
        }
    }

    class ExamClockForm : ToolForm
    {
        TextBox name;
        DateTimePicker start;
        NumericUpDown duration;
        ComboBox extra;

        public ExamClockForm() : base("Exam Clock", 1020, 600)
        {
            AddLabel("Exam");
            name = AddText(Store.Get("exam.name", "Examination"), 190);
            AddLabel("Start");
            start = new DateTimePicker();
            start.Format = DateTimePickerFormat.Custom; start.CustomFormat = "HH:mm"; start.ShowUpDown = true;
            start.Width = Theme.S(80); start.Font = Theme.Font(11f);
            start.Margin = new Padding(Theme.S(3), Theme.S(6), Theme.S(3), Theme.S(3));
            DateTime saved;
            start.Value = DateTime.TryParse(Store.Get("exam.start", "09:00"), out saved) ? DateTime.Today.Add(saved.TimeOfDay) : DateTime.Today.AddHours(9);
            Bar.Controls.Add(start);
            AddButton("Now", delegate { start.Value = DateTime.Now; });
            AddLabel("Length (min)");
            duration = AddNumber(1, 600, Store.GetInt("exam.minutes", 90), 64);
            AddLabel("Extra time");
            extra = AddCombo(84, "None", "10%", "25%", "50%", "100%");
            extra.SelectedIndex = Math.Max(0, Math.Min(4, Store.GetInt("exam.extra", 2)));
            AddWindowButtons(false);
            EventHandler save = delegate
            {
                Store.Set("exam.name", name.Text); Store.Set("exam.start", start.Value.ToString("HH:mm"));
                Store.Set("exam.minutes", (int)duration.Value); Store.Set("exam.extra", extra.SelectedIndex);
                View.Invalidate();
            };
            name.TextChanged += save; start.ValueChanged += save; duration.ValueChanged += save; extra.SelectedIndexChanged += save;
            Tick.Interval = 250;
            Tick.Start();
        }

        static readonly int[] ExtraPercent = { 0, 10, 25, 50, 100 };

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Color.White);
            DateTime now = DateTime.Now;
            DateTime s = DateTime.Today.Add(start.Value.TimeOfDay);
            int mins = (int)duration.Value;
            DateTime end = s.AddMinutes(mins);
            int pct = ExtraPercent[Math.Max(0, extra.SelectedIndex)];
            DateTime endExtra = s.AddMinutes(mins * (100 + pct) / 100.0);
            Color ink = Color.FromArgb(20, 24, 31);

            float h = r.Height, w = r.Width;
            Draw.Text(g, name.Text, new RectangleF(w * 0.04f, h * 0.02f, w * 0.92f, h * 0.12f), ink, FontStyle.Bold, false, 90f, null);
            Draw.Text(g, now.ToString("HH:mm:ss"), new RectangleF(w * 0.04f, h * 0.13f, w * 0.92f, h * 0.42f), ink, FontStyle.Bold, false, 600f, "00:00:00");

            string state; Color sc;
            if (now < s) { state = "Starts in " + TimeText.Clock((s - now).TotalSeconds); sc = Theme.Accent; }
            else if (now < end) { state = "Time remaining " + TimeText.Clock((end - now).TotalSeconds); sc = (end - now).TotalMinutes <= 5 ? Theme.Red : Color.FromArgb(22, 140, 70); }
            else if (pct > 0 && now < endExtra) { state = "Extra time remaining " + TimeText.Clock((endExtra - now).TotalSeconds); sc = Theme.Amber; }
            else { state = "Exam finished"; sc = Theme.Red; }
            Draw.Text(g, state, new RectangleF(w * 0.04f, h * 0.55f, w * 0.92f, h * 0.13f), sc, FontStyle.Bold, false, 120f, null);

            int cols = pct > 0 ? 3 : 2;
            string[] heads = { "Start", "Finish", "Finish (+" + pct + "%)" };
            string[] vals = { s.ToString("HH:mm"), end.ToString("HH:mm"), endExtra.ToString("HH:mm") };
            float cw = w * 0.92f / cols;
            for (int i = 0; i < cols; i++)
            {
                var cell = new RectangleF(w * 0.04f + i * cw + 6, h * 0.71f, cw - 12, h * 0.25f);
                Draw.FillRound(g, cell, 14, Color.FromArgb(236, 240, 246));
                Draw.Text(g, heads[i], new RectangleF(cell.X, cell.Y + cell.Height * 0.06f, cell.Width, cell.Height * 0.28f), Color.FromArgb(90, 100, 118), FontStyle.Regular, false, 60f, null);
                Draw.Text(g, vals[i], new RectangleF(cell.X, cell.Y + cell.Height * 0.32f, cell.Width, cell.Height * 0.66f), ink, FontStyle.Bold, false, 200f, "00:00");
            }
        }
    }
}
