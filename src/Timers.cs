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

    // Big round buttons drawn on a tool's display, for use from the front of the room or on an interactive whiteboard.
    class DisplayButtons
    {
        class Item { public RectangleF Rect; public Glyph Glyph; public string Text; public bool Primary; public Action Click; public Color? Fill; }
        readonly List<Item> items = new List<Item>();
        readonly Control view;
        int hot = -1, down = -1;
        public float Alpha = 1f;

        public DisplayButtons(Control view)
        {
            this.view = view;
            view.MouseMove += delegate(object s, MouseEventArgs e) { int h = Hit(e.Location); if (h != hot) { hot = h; view.Cursor = h >= 0 ? Cursors.Hand : Cursors.Default; view.Invalidate(); } };
            view.MouseLeave += delegate { hot = -1; down = -1; view.Invalidate(); };
            view.MouseDown += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { down = Hit(e.Location); view.Invalidate(); } };
            view.MouseUp += delegate(object s, MouseEventArgs e)
            {
                int h = Hit(e.Location);
                int was = down;
                down = -1;
                view.Invalidate();
                if (h >= 0 && h == was && Alpha > 0.2f) items[h].Click();
            };
        }

        public void Clear() { items.Clear(); }

        public void Add(RectangleF r, Glyph glyph, string text, bool primary, Action click)
        {
            items.Add(new Item { Rect = r, Glyph = glyph, Text = text, Primary = primary, Click = click });
        }

        public void Add(RectangleF r, Glyph glyph, string text, Color fill, Action click)
        {
            items.Add(new Item { Rect = r, Glyph = glyph, Text = text, Primary = true, Click = click, Fill = fill });
        }

        public int Hit(Point p)
        {
            if (Alpha <= 0.2f) return -1;
            for (int i = 0; i < items.Count; i++) if (items[i].Rect.Contains(p)) return i;
            return -1;
        }

        public bool Contains(Point p) { return Hit(p) >= 0; }

        public void Paint(Graphics g)
        {
            if (Alpha <= 0.01f) return;
            int a = (int)(255 * Math.Max(0, Math.Min(1, Alpha)));
            for (int i = 0; i < items.Count; i++)
            {
                Item it = items[i];
                Color fill = it.Fill ?? (it.Primary ? Theme.Accent : Theme.Panel2);
                if (i == down) fill = Draw.Mix(fill, Color.Black, 0.2);
                else if (i == hot) fill = Draw.Mix(fill, Color.White, 0.12);
                Draw.FillRound(g, it.Rect, it.Rect.Height / 2, Color.FromArgb(a, fill));
                float icon = it.Rect.Height * 0.42f;
                Color ink = Color.FromArgb(a, Color.White);
                if (string.IsNullOrEmpty(it.Text))
                    Glyphs.Draw(g, it.Glyph, new RectangleF(it.Rect.X + (it.Rect.Width - icon) / 2, it.Rect.Y + (it.Rect.Height - icon) / 2, icon, icon), ink);
                else
                {
                    Font f = Theme.Semi(Math.Max(9f, it.Rect.Height * 0.3f * 72f / g.DpiY));
                    SizeF ts = g.MeasureString(it.Text, f);
                    float gap = it.Glyph == Glyph.None ? 0 : icon * 0.35f;
                    float iw = it.Glyph == Glyph.None ? 0 : icon;
                    float x = it.Rect.X + (it.Rect.Width - iw - gap - ts.Width) / 2;
                    if (iw > 0) Glyphs.Draw(g, it.Glyph, new RectangleF(x, it.Rect.Y + (it.Rect.Height - icon) / 2, icon, icon), ink);
                    using (var b = new SolidBrush(ink)) g.DrawString(it.Text, f, b, x + iw + gap, it.Rect.Y + (it.Rect.Height - ts.Height) / 2);
                }
            }
        }
    }

    class CountdownForm : ToolForm
    {
        static readonly int[] Presets = { 1, 2, 3, 5, 10, 15, 20, 30 };
        double total = 300, left = 300;
        DateTime endsAt, lastMove = DateTime.Now;
        bool running, finished, syncing;
        TimeStepper custom;
        FlatButton soundBtn;
        readonly List<FlatButton> presetButtons = new List<FlatButton>();
        DisplayButtons buttons;

        public CountdownForm() : base("Countdown Timer", 820, 500)
        {
            foreach (int m in Presets)
            {
                int minutes = m;
                var b = AddButton(m.ToString(), delegate { SetTime(minutes * 60); });
                b.MinimumSize = new Size(Theme.S(38), 0);
                b.Tip = m + (m == 1 ? " minute" : " minutes");
                presetButtons.Add(b);
            }
            AddLabel("min");
            AddDivider();
            AddLabel("Custom");
            custom = new TimeStepper();
            custom.Value = 300;
            custom.Tip = "Click to type a time such as 7:30, or use - and +";
            custom.ValueChanged += delegate { if (!syncing) SetTime((int)custom.Value); };
            Bar.Controls.Add(custom);

            soundBtn = new FlatButton("", Glyph.SoundOn, false);
            soundBtn.Click += delegate { SetSound(!soundBtn.Toggled); };
            Bar.AddRight(soundBtn);
            AddWindowButtons(true);
            SetSound(Store.Get("countdown.sound", "1") == "1");

            buttons = new DisplayButtons(View);
            View.MouseMove += delegate { lastMove = DateTime.Now; };
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !buttons.Contains(e.Location)) Toggle(); };
            Tick.Interval = 50;
            Tick.Start();
            SetTime(Store.GetInt("countdown.last", 300));
        }

        void SetSound(bool on)
        {
            soundBtn.Toggled = on;
            soundBtn.Glyph = on ? Glyph.SoundOn : Glyph.SoundOff;
            soundBtn.Tip = on ? "Sound on at the end - click to mute" : "Sound off - click to turn on";
            Store.Set("countdown.sound", on ? "1" : "0");
        }

        void SetTime(int seconds)
        {
            if (seconds <= 0) seconds = 60;
            total = left = seconds;
            running = false; finished = false;
            Store.Set("countdown.last", seconds);
            syncing = true;
            custom.Value = Math.Min(custom.Maximum, seconds);
            syncing = false;
            for (int i = 0; i < Presets.Length; i++) presetButtons[i].Toggled = seconds == Presets[i] * 60;
            View.Invalidate();
        }

        void Reset() { SetTime((int)total); }

        void Adjust(int by)
        {
            left = Math.Max(1, left + by); total = Math.Max(total, left);
            if (running) endsAt = DateTime.UtcNow.AddSeconds(left);
            finished = false;
            View.Invalidate();
        }

        void Toggle()
        {
            if (finished) Reset();   // Start again straight away from the same time
            running = !running;
            if (running) endsAt = DateTime.UtcNow.AddSeconds(left);
            lastMove = DateTime.Now;
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
                    if (soundBtn.Toggled) Sound.Alarm();
                }
            }
            // Buttons on the display fade away while the timer runs in full screen and the mouse is still.
            float target = running && IsFullScreen && (DateTime.Now - lastMove).TotalSeconds > 3 ? 0f : 1f;
            buttons.Alpha += (target - buttons.Alpha) * 0.15f;
            if (Math.Abs(buttons.Alpha - target) < 0.02f) buttons.Alpha = target;
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space || key == Keys.Enter) { Toggle(); return true; }
            if (key == Keys.R) { Reset(); return true; }
            if (key == Keys.Add || key == Keys.Oemplus) { Adjust(60); return true; }
            if (key == Keys.Subtract || key == Keys.OemMinus) { Adjust(-60); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            float w = r.Width, h = r.Height;
            // Background: plain while counting; a slow, gentle red pulse when time is up (no flashing).
            Color bg = Theme.Bg;
            if (finished)
            {
                double pulse = (Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds * Math.PI) + 1) / 2;
                bg = Draw.Mix(Theme.Bg, Theme.Red, 0.18 + 0.22 * pulse);
            }
            g.Clear(bg);

            // Final minute (or final fifth of a short timer) turns amber; the last ten seconds turn red.
            double warn = Math.Max(10, Math.Min(60, total * 0.2));
            Color ink = finished ? Color.White : left <= 10 && left < total ? Theme.Red : left <= warn && left < total ? Theme.Amber : Theme.Text;

            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(88), h * 0.14f));
            float pad = Math.Max(Theme.S(12), h * 0.05f);
            float statusH = Math.Max(Theme.S(22), h * 0.07f);
            float barH = Math.Max(Theme.S(6), h * 0.014f);
            float digitsBottom = h - pad - btnH - pad * 0.8f - statusH;

            string text = finished ? "Time's up" : TimeText.Clock(left);
            Draw.Text(g, text, new RectangleF(w * 0.04f, pad * 0.6f, w * 0.92f, digitsBottom - pad * 0.6f), ink, FontStyle.Bold, false, 700f, finished ? null : TimeText.Template(text));

            string status;
            if (finished) status = "Press Space to run it again";
            else if (running) status = "Ends at " + DateTime.Now.AddSeconds(left).ToString("HH:mm");
            else if (left < total) status = "Paused";
            else status = "Press Space or Start";
            Draw.Text(g, status, new RectangleF(0, digitsBottom, w, statusH), finished ? Color.White : Theme.Muted, FontStyle.Regular, false, 40f, null);

            // Progress along the bottom edge.
            double frac = total <= 0 ? 0 : Math.Max(0, Math.Min(1, left / total));
            using (var b = new SolidBrush(Theme.Panel)) g.FillRectangle(b, 0, h - barH, w, barH);
            using (var b = new SolidBrush(finished ? Theme.Red : ink == Theme.Text ? Theme.Green : ink)) g.FillRectangle(b, 0, h - barH, (float)(w * frac), barH);

            // Reset - Start/Pause - +1 min
            buttons.Clear();
            float cy = h - pad - btnH - barH * 0.5f;
            float main = btnH * 2.6f, side = btnH;
            float plusW = btnH * 1.9f;
            float gap = btnH * 0.3f;
            float total_ = side + gap + main + gap + plusW;
            float x = (w - total_) / 2;
            buttons.Add(new RectangleF(x, cy, side, btnH), Glyph.Reset, null, false, Reset);
            buttons.Add(new RectangleF(x + side + gap, cy, main, btnH), running ? Glyph.Pause : Glyph.Play, running ? "Pause" : finished ? "Again" : left < total ? "Resume" : "Start", true, Toggle);
            buttons.Add(new RectangleF(x + side + gap + main + gap, cy, plusW, btnH), Glyph.Plus, "1 min", false, delegate { Adjust(60); });
            buttons.Paint(g);
        }
    }

    class IntervalForm : ToolForm
    {
        TimeStepper work, rest;
        Stepper rounds;
        bool running, done;
        int round = 1;
        bool resting;
        double left;
        DateTime endsAt, lastMove = DateTime.Now;
        DisplayButtons buttons;

        public IntervalForm() : base("Interval Timer", 820, 500)
        {
            AddLabel("Work");
            work = new TimeStepper();
            work.Value = Math.Max(5, Store.GetInt("interval.work", 120));
            work.Tip = "Length of each work period. Click to type a time such as 1:30";
            Bar.Controls.Add(work);
            AddLabel("Rest");
            rest = new TimeStepper();
            rest.Minimum = 0;
            rest.Value = Math.Max(0, Store.GetInt("interval.rest", 30));
            rest.Tip = "Rest between rounds. Set to 0:00 for none";
            Bar.Controls.Add(rest);
            AddLabel("Rounds");
            rounds = AddNumber(1, 99, Store.GetInt("interval.rounds", 5), 36);
            EventHandler changed = delegate
            {
                Store.Set("interval.work", (int)work.Value); Store.Set("interval.rest", (int)rest.Value); Store.Set("interval.rounds", (int)rounds.Value);
                if (!running && round == 1 && !resting && !done) left = (double)work.Value;
                View.Invalidate();
            };
            work.ValueChanged += changed; rest.ValueChanged += changed; rounds.ValueChanged += changed;
            AddWindowButtons(true);
            buttons = new DisplayButtons(View);
            View.MouseMove += delegate { lastMove = DateTime.Now; };
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !buttons.Contains(e.Location)) Toggle(); };
            Tick.Interval = 50;
            Tick.Start();
            Reset();
        }

        void Reset()
        {
            running = false; done = false; resting = false; round = 1;
            left = (double)work.Value;
            View.Invalidate();
        }

        void Toggle()
        {
            if (done) Reset();
            running = !running;
            if (running) endsAt = DateTime.UtcNow.AddSeconds(left);
            lastMove = DateTime.Now;
            View.Invalidate();
        }

        // Moves on to the next work or rest period.
        void Next()
        {
            if (done) return;
            if (!resting && rest.Value > 0 && round < rounds.Value) { resting = true; left = (double)rest.Value; Sound.Change(); }
            else if (round < rounds.Value) { resting = false; round++; left = (double)work.Value; Sound.Change(); }
            else { done = true; running = false; left = 0; Sound.Alarm(); }
            endsAt = DateTime.UtcNow.AddSeconds(left);
            View.Invalidate();
        }

        protected override void OnTick()
        {
            if (running)
            {
                left = (endsAt - DateTime.UtcNow).TotalSeconds;
                if (left <= 0) Next();
            }
            float target = running && IsFullScreen && (DateTime.Now - lastMove).TotalSeconds > 3 ? 0f : 1f;
            buttons.Alpha += (target - buttons.Alpha) * 0.15f;
            if (Math.Abs(buttons.Alpha - target) < 0.02f) buttons.Alpha = target;
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space || key == Keys.Enter) { Toggle(); return true; }
            if (key == Keys.R) { Reset(); return true; }
            if (key == Keys.N || key == Keys.Right) { Next(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            Color c = done ? Theme.Purple : resting ? Theme.Amber : Theme.Green;
            g.Clear(Draw.Mix(Theme.Bg, c, done ? 0.2 : 0.12));
            float w = r.Width, h = r.Height;
            float pad = Math.Max(Theme.S(12), h * 0.05f);
            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(88), h * 0.14f));
            float barH = Math.Max(Theme.S(6), h * 0.014f);

            // Phase label in a pill
            string phase = done ? "Finished" : resting ? "Rest" : "Work";
            float pillH = Math.Max(Theme.S(30), h * 0.1f);
            Font pf = Theme.Semi(Math.Max(10f, pillH * 0.45f * 72f / g.DpiY));
            float pillW = g.MeasureString(phase.ToUpperInvariant(), pf).Width + pillH * 1.2f;
            var pill = new RectangleF((w - pillW) / 2, pad * 0.8f, pillW, pillH);
            Draw.FillRound(g, pill, pillH / 2, c);
            Draw.Label(g, phase.ToUpperInvariant(), pill, Color.White, pf.SizeInPoints, FontStyle.Bold, StringAlignment.Center);

            float dotsH = Math.Max(Theme.S(14), h * 0.04f);
            float bottom = h - pad - btnH - pad * 0.7f - dotsH - Theme.S(26) - pad * 0.4f;
            string t = done ? "Well done" : TimeText.Clock(left);
            Color ink = !done && running && left <= 5 ? c : Theme.Text;
            Draw.Text(g, t, new RectangleF(w * 0.04f, pill.Bottom + pad * 0.2f, w * 0.92f, bottom - pill.Bottom - pad * 0.2f), ink, FontStyle.Bold, false, 700f, done ? null : TimeText.Template(t));

            // One dot per round: done rounds filled, current round outlined
            int n = (int)rounds.Value;
            float dot = Math.Min(dotsH, (w * 0.8f) / (n * 1.8f));
            float gap = dot * 0.8f;
            float x = (w - (n * dot + (n - 1) * gap)) / 2;
            float dy = bottom + (dotsH - dot) / 2;
            for (int i = 1; i <= n; i++)
            {
                var d = new RectangleF(x + (i - 1) * (dot + gap), dy, dot, dot);
                if (i < round || done) using (var b = new SolidBrush(c)) g.FillEllipse(b, d);
                else if (i == round) using (var p = new Pen(Theme.Text, Math.Max(2, dot * 0.18f))) g.DrawEllipse(p, Draw.Inset(d, dot * 0.09f));
                else using (var b = new SolidBrush(Theme.Panel2)) g.FillEllipse(b, d);
            }
            Draw.Label(g, done ? "All " + n + " rounds complete" : "Round " + round + " of " + n, new RectangleF(0, dy + dot + Theme.S(2), w, Theme.S(22)), Theme.Muted, 10.5f, FontStyle.Regular, StringAlignment.Center);

            // Progress through the current period
            double length = (double)(resting ? rest.Value : work.Value);
            double frac = done ? 0 : length <= 0 ? 0 : Math.Max(0, Math.Min(1, left / length));
            using (var b = new SolidBrush(Theme.Panel)) g.FillRectangle(b, 0, h - barH, w, barH);
            using (var b = new SolidBrush(c)) g.FillRectangle(b, 0, h - barH, (float)(w * frac), barH);

            buttons.Clear();
            float cy = h - pad - btnH - barH * 0.5f;
            float main = btnH * 2.6f, side = btnH, gp = btnH * 0.3f;
            float x0 = (w - (side + gp + main + gp + side)) / 2;
            buttons.Add(new RectangleF(x0, cy, side, btnH), Glyph.Reset, null, false, Reset);
            buttons.Add(new RectangleF(x0 + side + gp, cy, main, btnH), running ? Glyph.Pause : Glyph.Play, running ? "Pause" : done ? "Again" : (left < (double)work.Value || round > 1 || resting) ? "Resume" : "Start", true, Toggle);
            buttons.Add(new RectangleF(x0 + side + gp + main + gp, cy, side, btnH), Glyph.Next, null, false, Next);
            buttons.Paint(g);
        }
    }

    class StopwatchForm : ToolForm
    {
        readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        readonly List<TimeSpan> laps = new List<TimeSpan>();
        DisplayButtons buttons;

        public StopwatchForm() : base("Stopwatch", 820, 480)
        {
            var copy = AddButton("Copy laps", Glyph.Copy, delegate { CopyLaps(); }, false);
            copy.Tip = "Copy the lap times, ready to paste into Excel";
            AddLabel("Space start/stop  ·  L lap  ·  R reset");
            AddWindowButtons(true);
            buttons = new DisplayButtons(View);
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !buttons.Contains(e.Location)) Toggle(); };
            Tick.Interval = 50;
        }

        void Toggle()
        {
            if (watch.IsRunning) { watch.Stop(); Tick.Stop(); } else { watch.Start(); Tick.Start(); }
            View.Invalidate();
        }

        void Reset() { watch.Reset(); laps.Clear(); Tick.Stop(); View.Invalidate(); }

        void Lap()
        {
            if (watch.Elapsed.Ticks > 0) { laps.Add(watch.Elapsed); Sound.Blip(); }
            View.Invalidate();
        }

        static string Fmt(TimeSpan t)
        {
            if (t.TotalHours >= 1) return string.Format("{0}:{1:00}:{2:00}.{3}", (int)t.TotalHours, t.Minutes, t.Seconds, t.Milliseconds / 100);
            return string.Format("{0:00}:{1:00}.{2}", (int)t.TotalMinutes, t.Seconds, t.Milliseconds / 100);
        }

        TimeSpan Split(int i) { return laps[i] - (i > 0 ? laps[i - 1] : TimeSpan.Zero); }

        void CopyLaps()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Lap\tTotal\tSplit");
            for (int i = 0; i < laps.Count; i++)
                sb.AppendLine((i + 1) + "\t" + Fmt(laps[i]) + "\t" + Fmt(Split(i)));
            if (laps.Count > 0) Clipboard.SetText(sb.ToString());
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { Toggle(); return true; }
            if (key == Keys.L || key == Keys.Enter) { Lap(); return true; }
            if (key == Keys.R) { Reset(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            float w = r.Width, h = r.Height;
            float lapW = laps.Count > 0 ? Math.Min(w * 0.34f, Theme.S(320)) : 0;
            float mw = w - lapW;
            float pad = Math.Max(Theme.S(12), h * 0.05f);
            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(88), h * 0.14f));
            float statusH = Math.Max(Theme.S(22), h * 0.07f);
            float digitsBottom = h - pad - btnH - pad * 0.8f - statusH;
            bool started = watch.Elapsed.Ticks > 0;
            Draw.Text(g, Fmt(watch.Elapsed), new RectangleF(mw * 0.04f, pad * 0.6f, mw * 0.92f, digitsBottom - pad * 0.6f), watch.IsRunning || !started ? Theme.Text : Theme.Muted, FontStyle.Bold, false, 600f, watch.Elapsed.TotalHours >= 1 ? "0:00:00.0" : "00:00.0");
            string status = watch.IsRunning ? (laps.Count > 0 ? "Lap " + (laps.Count + 1) + "   " + Fmt(watch.Elapsed - laps[laps.Count - 1]) : "Running") : started ? "Stopped" : "Press Space or Start";
            Draw.Text(g, status, new RectangleF(0, digitsBottom, mw, statusH), Theme.Muted, FontStyle.Regular, false, 40f, null);

            buttons.Clear();
            float cy = h - pad - btnH;
            float main = btnH * 2.6f, side = btnH, gp = btnH * 0.3f;
            float x0 = (mw - (side + gp + main + gp + side)) / 2;
            buttons.Add(new RectangleF(x0, cy, side, btnH), Glyph.Reset, null, false, Reset);
            buttons.Add(new RectangleF(x0 + side + gp, cy, main, btnH), watch.IsRunning ? Glyph.Pause : Glyph.Play, watch.IsRunning ? "Stop" : started ? "Resume" : "Start", true, Toggle);
            buttons.Add(new RectangleF(x0 + side + gp + main + gp, cy, side, btnH), Glyph.Flag, null, false, Lap);
            buttons.Paint(g);

            if (lapW <= 0) return;
            var panel = new RectangleF(mw, 0, lapW, h);
            using (var b = new SolidBrush(Theme.Panel)) g.FillRectangle(b, panel);
            float row = Theme.S(34);
            float cx1 = panel.X + Theme.S(16), cx2 = panel.X + lapW * 0.30f, cx3 = panel.X + lapW * 0.64f;
            Draw.Label(g, "Lap", new RectangleF(cx1, Theme.S(8), Theme.S(60), row), Theme.Muted, 9.5f, FontStyle.Regular, StringAlignment.Near);
            Draw.Label(g, "Split", new RectangleF(cx2, Theme.S(8), lapW * 0.34f, row), Theme.Muted, 9.5f, FontStyle.Regular, StringAlignment.Near);
            Draw.Label(g, "Total", new RectangleF(cx3, Theme.S(8), lapW * 0.34f, row), Theme.Muted, 9.5f, FontStyle.Regular, StringAlignment.Near);
            int best = -1, worst = -1;
            if (laps.Count >= 3)
                for (int i = 0; i < laps.Count; i++)
                {
                    if (best < 0 || Split(i) < Split(best)) best = i;
                    if (worst < 0 || Split(i) > Split(worst)) worst = i;
                }
            int show = Math.Max(1, (int)((h - row - Theme.S(8)) / row));
            // Newest lap at the top
            for (int k = 0; k < Math.Min(show, laps.Count); k++)
            {
                int i = laps.Count - 1 - k;
                float y = Theme.S(8) + row * (k + 1);
                if (k % 2 == 0) using (var b = new SolidBrush(Draw.Mix(Theme.Panel, Theme.Panel2, 0.5))) g.FillRectangle(b, panel.X, y, lapW, row);
                Color splitInk = i == best ? Theme.Green : i == worst ? Theme.Amber : Theme.Text;
                Draw.Label(g, (i + 1).ToString(), new RectangleF(cx1, y, Theme.S(60), row), Theme.Muted, 11.5f, FontStyle.Regular, StringAlignment.Near);
                Draw.Label(g, Fmt(Split(i)), new RectangleF(cx2, y, lapW * 0.34f, row), splitInk, 12f, FontStyle.Bold, StringAlignment.Near);
                Draw.Label(g, Fmt(laps[i]), new RectangleF(cx3, y, lapW * 0.34f, row), Theme.Text, 12f, FontStyle.Regular, StringAlignment.Near);
            }
        }
    }

    class ExamClockForm : ToolForm
    {
        TextBox name;
        ClockStepper start;
        Stepper duration;
        Choice extra;
        static readonly int[] ExtraPercent = { 0, 10, 25, 50, 100 };

        public ExamClockForm() : base("Exam Clock", 1040, 620)
        {
            name = AddText(Store.Get("exam.name", "Examination"), 220);
            AddLabel("Starts");
            start = new ClockStepper();
            DateTime saved;
            start.Value = DateTime.TryParse(Store.Get("exam.start", "09:00"), out saved) ? (int)saved.TimeOfDay.TotalMinutes : 9 * 60;
            start.Tip = "Start time. Click to type one such as 9:15";
            Bar.Controls.Add(start);
            var now = AddButton("Now", delegate { DateTime n = DateTime.Now; start.Value = n.Hour * 60 + n.Minute; }, false);
            now.Tip = "Set the start time to now";
            AddLabel("Length");
            duration = AddNumber(1, 600, Store.GetInt("exam.minutes", 90), 44);
            duration.Unit = "min";
            duration.Increment = 5;
            AddLabel("Extra time");
            extra = AddCombo(96, "None", "10%", "25%", "50%", "100%");
            extra.SelectedIndex = Math.Max(0, Math.Min(4, Store.GetInt("exam.extra", 2)));
            AddWindowButtons(false);
            EventHandler save = delegate
            {
                Store.Set("exam.name", name.Text); Store.Set("exam.start", ((int)start.Value / 60).ToString("00") + ":" + ((int)start.Value % 60).ToString("00"));
                Store.Set("exam.minutes", (int)duration.Value); Store.Set("exam.extra", extra.SelectedIndex);
                View.Invalidate();
            };
            name.TextChanged += save; start.ValueChanged += save; duration.ValueChanged += save; extra.SelectedIndexChanged += save;
            Tick.Interval = 250;
            Tick.Start();
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            // White for exam halls and projectors.
            Color paper = Color.White, ink = Color.FromArgb(17, 24, 39), soft = Color.FromArgb(100, 110, 128), card = Color.FromArgb(241, 244, 248);
            Color green = Color.FromArgb(21, 128, 61), amber = Color.FromArgb(180, 83, 9), red = Color.FromArgb(200, 30, 30), blue = Color.FromArgb(29, 78, 216);
            g.Clear(paper);
            DateTime now = DateTime.Now;
            DateTime s = DateTime.Today.AddMinutes((double)start.Value);
            int mins = (int)duration.Value;
            DateTime end = s.AddMinutes(mins);
            int pct = ExtraPercent[Math.Max(0, extra.SelectedIndex)];
            DateTime endExtra = s.AddMinutes(mins * (100 + pct) / 100.0);
            float h = r.Height, w = r.Width, m = w * 0.04f;

            Draw.Text(g, name.Text.Trim().Length > 0 ? name.Text.Trim() : "Examination", new RectangleF(m, h * 0.03f, w - 2 * m, h * 0.1f), ink, FontStyle.Bold, false, 80f, null);
            Draw.Text(g, now.ToString("HH:mm:ss"), new RectangleF(m, h * 0.13f, w - 2 * m, h * 0.38f), ink, FontStyle.Bold, false, 600f, "00:00:00");

            string state; Color sc;
            if (now < s) { state = "Starts in " + TimeText.Clock((s - now).TotalSeconds); sc = blue; }
            else if (now < end) { state = "Time remaining  " + TimeText.Clock((end - now).TotalSeconds); sc = (end - now).TotalMinutes <= 5 ? red : green; }
            else if (pct > 0 && now < endExtra) { state = "Extra time remaining  " + TimeText.Clock((endExtra - now).TotalSeconds); sc = amber; }
            else { state = "Exam finished"; sc = red; }
            Draw.Text(g, state, new RectangleF(m, h * 0.52f, w - 2 * m, h * 0.1f), sc, FontStyle.Bold, false, 110f, null);

            // Timeline from start to the last finish, with the normal finish marked
            var line = new RectangleF(m, h * 0.655f, w - 2 * m, Math.Max(Theme.S(8), h * 0.018f));
            Draw.FillRound(g, line, line.Height / 2, card);
            double span = (endExtra - s).TotalSeconds;
            double done = span <= 0 ? 0 : Math.Max(0, Math.Min(1, (now - s).TotalSeconds / span));
            if (done > 0) Draw.FillRound(g, new RectangleF(line.X, line.Y, Math.Max(line.Height, (float)(line.Width * done)), line.Height), line.Height / 2, sc);
            if (pct > 0)
            {
                float fx = line.X + (float)(line.Width * (mins * 60.0 / span));
                using (var p = new Pen(ink, Math.Max(2, Theme.S(2)))) g.DrawLine(p, fx, line.Y - line.Height * 0.8f, fx, line.Bottom + line.Height * 0.8f);
            }

            int cols = pct > 0 ? 3 : 2;
            string[] heads = { "Start", "Finish", "Extra time finish (+" + pct + "%)" };
            string[] vals = { s.ToString("HH:mm"), end.ToString("HH:mm"), endExtra.ToString("HH:mm") };
            float gap = w * 0.015f;
            float cw = (w - 2 * m - (cols - 1) * gap) / cols;
            for (int i = 0; i < cols; i++)
            {
                var cell = new RectangleF(m + i * (cw + gap), h * 0.72f, cw, h * 0.24f);
                Draw.FillRound(g, cell, Theme.S(14), card);
                Draw.Text(g, heads[i], new RectangleF(cell.X, cell.Y + cell.Height * 0.08f, cell.Width, cell.Height * 0.24f), soft, FontStyle.Regular, false, 40f, null);
                Draw.Text(g, vals[i], new RectangleF(cell.X, cell.Y + cell.Height * 0.32f, cell.Width, cell.Height * 0.6f), ink, FontStyle.Bold, false, 200f, "00:00");
            }
        }
    }
}
