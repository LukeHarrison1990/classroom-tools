// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - traffic lights, pickers, dice, frames, ticker, plenary prompts, scoreboard.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    class TrafficForm : ToolForm
    {
        int lit = -1;
        string[] captions;
        static readonly Color[] On = { Color.FromArgb(244, 63, 63), Color.FromArgb(250, 170, 20), Color.FromArgb(34, 210, 100) };
        static readonly string[] Names = { "Red", "Amber", "Green" };
        RectangleF[] lamps = new RectangleF[3];

        public TrafficForm() : base("Traffic Lights", 400, 740)
        {
            var cap = AddButton("Captions", Glyph.Edit, delegate { EditCaptions(); }, false);
            cap.Tip = "Change the words shown under each light";
            var off = AddIcon(Glyph.Close, "Turn all the lights off (Space cycles them)", delegate { lit = -1; View.Invalidate(); });
            AddWindowButtons(true);
            LoadCaptions();
            View.MouseDown += delegate(object s, MouseEventArgs e)
            {
                for (int i = 0; i < 3; i++)
                    if (lamps[i].Contains(e.Location)) { lit = lit == i ? -1 : i; View.Invalidate(); }
            };
        }

        void LoadCaptions()
        {
            captions = Store.Get("traffic.captions", "Silent - working alone\nQuiet - partner voices\nTalk - group discussion")
                .Replace("\r\n", "\n").Split('\n');
            Array.Resize(ref captions, 3);
        }

        void EditCaptions()
        {
            string t = TextEditDialog.Show(this, "Traffic light captions", "Three lines: the caption shown for red, amber and green.\nLeave a line empty for no caption.", string.Join("\r\n", captions.Select(c => c ?? "").ToArray()));
            if (t == null) return;
            Store.Set("traffic.captions", t);
            LoadCaptions();
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.R || key == Keys.D1) lit = 0;
            else if (key == Keys.A || key == Keys.D2) lit = 1;
            else if (key == Keys.G || key == Keys.D3) lit = 2;
            else if (key == Keys.Space) lit = (lit + 1) % 3;
            else if (key == Keys.D0 || key == Keys.O) lit = -1;
            else return false;
            View.Invalidate();
            return true;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            if (lit >= 0)
                using (var wash = new SolidBrush(Color.FromArgb(22, On[lit]))) g.FillRectangle(wash, r);
            string cap = lit >= 0 ? (captions[lit] ?? "").Trim() : "";
            bool sideways = r.Width > r.Height * 1.1f;
            float capH = Math.Max(Theme.S(40), r.Height * (sideways ? 0.2f : 0.13f));
            float m = Math.Max(Theme.S(12), Math.Min(r.Width, r.Height) * 0.04f);
            float availW = r.Width - 2 * m, availH = r.Height - capH - 3 * m;
            // Lamp size: three lamps plus the housing around them.
            float d = sideways ? Math.Min(availW / 3.5f, availH / 1.24f) : Math.Min(availH / 3.5f, availW / 1.24f);
            float edge = d * 0.12f, gap = d * 0.13f;
            float bodyW = sideways ? 3 * d + 2 * gap + 2 * edge : d + 2 * edge;
            float bodyH = sideways ? d + 2 * edge : 3 * d + 2 * gap + 2 * edge;
            float bx = (r.Width - bodyW) / 2f, by = m + (availH - bodyH) / 2f;
            var body = new RectangleF(bx, by, bodyW, bodyH);
            using (var path = Draw.Round(body, d * 0.28f))
            using (var grad = new LinearGradientBrush(body, Color.FromArgb(40, 46, 58), Color.FromArgb(14, 17, 23), sideways ? 90f : 0f))
                g.FillPath(grad, path);
            using (var path = Draw.Round(Draw.Inset(body, 1), d * 0.28f))
            using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1.5f)) g.DrawPath(pen, path);
            for (int i = 0; i < 3; i++)
            {
                var lamp = sideways ? new RectangleF(bx + edge + i * (d + gap), by + edge, d, d) : new RectangleF(bx + edge, by + edge + i * (d + gap), d, d);
                lamps[i] = lamp;
                // Recessed ring
                using (var ring = new SolidBrush(Color.FromArgb(6, 8, 11))) g.FillEllipse(ring, lamp);
                var glass = Draw.Inset(lamp, d * 0.06f);
                if (lit == i)
                {
                    for (int k = 3; k >= 1; k--)
                        using (var halo = new SolidBrush(Color.FromArgb(18, On[i]))) g.FillEllipse(halo, Draw.Inset(lamp, -d * 0.06f * k));
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(glass);
                        using (var pg = new PathGradientBrush(path))
                        {
                            pg.CenterPoint = new PointF(glass.X + glass.Width * 0.42f, glass.Y + glass.Height * 0.38f);
                            pg.CenterColor = Draw.Mix(On[i], Color.White, 0.55);
                            pg.SurroundColors = new[] { On[i] };
                            g.FillEllipse(pg, glass);
                        }
                    }
                }
                else
                    using (var b = new SolidBrush(Draw.Mix(Color.FromArgb(12, 14, 18), On[i], 0.2))) g.FillEllipse(b, glass);
            }
            var capBox = new RectangleF(m, r.Height - capH - m, r.Width - 2 * m, capH);
            if (cap.Length > 0)
            {
                Draw.FillRound(g, capBox, Theme.S(14), Draw.Mix(Theme.Panel, On[lit], 0.18));
                Draw.Text(g, cap, Draw.Inset(capBox, capH * 0.12f), Draw.Mix(On[lit], Color.White, 0.25), FontStyle.Bold, true, 80f, null);
            }
            else if (lit < 0)
                Draw.Text(g, "Click a light, or press R, A or G", capBox, Theme.Muted, FontStyle.Regular, false, 14f, null);
        }
    }

    class PickerForm : ToolForm
    {
        FileChooser chooser;
        Toggle noRepeat;
        Stepper groupSize;
        List<Student> students = new List<Student>();
        readonly List<string> used = new List<string>();
        string shown = "";
        int spinLeft;
        string finalName;
        List<List<string>> groups;
        DisplayButtons buttons;

        public PickerForm() : base("Random Student Picker", 940, 580)
        {
            chooser = new FileChooser(this, Bar.Controls, "Classes", "class list", Samples.ClassHelp);
            chooser.Changed += delegate { LoadClass(); };
            AddDivider();
            noRepeat = AddCheck("No repeats", true);
            noRepeat.CheckedChanged += delegate { View.Invalidate(); };
            AddDivider();
            AddLabel("Groups of");
            groupSize = AddNumber(2, 12, Store.GetInt("picker.group", 4), 36);
            AddButton("Make groups", Glyph.Users, delegate { MakeGroups(); }, false);
            AddWindowButtons(false);
            buttons = new DisplayButtons(View);
            Tick.Interval = 70;
            LoadClass();
        }

        void LoadClass()
        {
            students = Store.LoadClass(chooser.Path);
            used.Clear(); shown = ""; groups = null;
            View.Invalidate();
        }

        void StartAgain() { used.Clear(); shown = ""; groups = null; View.Invalidate(); }

        List<Student> Pool()
        {
            return students.Where(s => s.Weight > 0 && !(noRepeat.Checked && used.Contains(s.Name))).ToList();
        }

        void Pick()
        {
            if (spinLeft > 0) return;
            groups = null;
            var pool = Pool();
            if (pool.Count == 0 && students.Count > 0 && noRepeat.Checked) { used.Clear(); pool = Pool(); }
            if (pool.Count == 0) { shown = ""; View.Invalidate(); return; }
            int totalWeight = pool.Sum(s => s.Weight);
            int ticket = Store.Rng.Next(totalWeight);
            foreach (var s in pool)
            {
                ticket -= s.Weight;
                if (ticket < 0) { finalName = s.Name; break; }
            }
            spinLeft = 16;
            Tick.Start();
        }

        protected override void OnTick()
        {
            if (spinLeft > 0)
            {
                spinLeft--;
                if (spinLeft == 0)
                {
                    shown = finalName;
                    used.Add(finalName);
                    Tick.Stop();
                    Sound.Blip();
                }
                else shown = students[Store.Rng.Next(students.Count)].Name;
            }
            View.Invalidate();
        }

        void MakeGroups()
        {
            Store.Set("picker.group", (int)groupSize.Value);
            var names = students.Select(s => s.Name).ToList();
            if (names.Count == 0) return;
            Store.Shuffle(names);
            int size = (int)groupSize.Value;
            int count = Math.Max(1, (int)Math.Round(names.Count / (double)size));
            groups = new List<List<string>>();
            for (int i = 0; i < count; i++) groups.Add(new List<string>());
            for (int i = 0; i < names.Count; i++) groups[i % count].Add(names[i]);
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space || key == Keys.Enter) { Pick(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            buttons.Clear();
            float w = r.Width, h = r.Height;
            float pad = Math.Max(Theme.S(14), h * 0.045f);
            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(80), h * 0.12f));
            if (students.Count == 0)
            {
                Draw.Wrapped(g, "This class list is empty.\nClick the pencil button to add names, one per line.", Draw.Inset(r, w * 0.1f), Theme.Muted, FontStyle.Regular, 26f);
                return;
            }
            float by = h - pad - btnH;
            if (groups != null)
            {
                RenderGroups(g, new RectangleF(pad, pad, w - 2 * pad, by - 2 * pad));
                float gw = btnH * 3.6f, x0 = (w - (gw * 2 + btnH * 0.3f)) / 2;
                buttons.Add(new RectangleF(x0, by, gw, btnH), Glyph.Shuffle, "Shuffle again", false, MakeGroups);
                buttons.Add(new RectangleF(x0 + gw + btnH * 0.3f, by, gw, btnH), Glyph.Check, "Done", true, delegate { groups = null; View.Invalidate(); });
                buttons.Paint(g);
                return;
            }
            float chipH = Math.Max(Theme.S(26), h * 0.05f);
            float infoH = Math.Max(Theme.S(22), h * 0.05f);
            float chipsY = by - pad * 0.8f - chipH;
            float infoY = chipsY - pad * 0.4f - infoH;
            var card = new RectangleF(w * 0.06f, pad, w * 0.88f, infoY - pad * 1.6f);
            bool settled = spinLeft == 0 && shown.Length > 0;
            if (shown.Length == 0)
            {
                Draw.FillRound(g, card, Theme.S(24), Theme.Panel);
                Draw.Text(g, "Who's next?", Draw.Inset(card, card.Height * 0.2f), Theme.Muted, FontStyle.Regular, false, 70f, null);
            }
            else
            {
                Draw.FillRound(g, card, Theme.S(24), settled ? Draw.Mix(Theme.Panel, Theme.Accent, 0.35) : Theme.Panel);
                Draw.Text(g, shown, Draw.Inset(card, card.Height * 0.1f), settled ? Color.White : Theme.Muted, FontStyle.Bold, true, 260f, null);
            }
            int remaining = Pool().Count;
            string info = noRepeat.Checked ? remaining + " of " + students.Count(s => s.Weight > 0) + " still to pick" : students.Count + " students";
            Draw.Text(g, info, new RectangleF(0, infoY, w, infoH), Theme.Muted, FontStyle.Regular, false, 14f, null);
            var recent = used.AsEnumerable().Reverse().Skip(settled ? 1 : 0).Take(12).ToList();
            Draw.Chips(g, recent, new RectangleF(w * 0.05f, chipsY, w * 0.9f, chipH), Theme.Panel, Theme.Text, Math.Max(9f, chipH * 0.4f * 72f / g.DpiY));

            float main = btnH * 2.8f, side = btnH, gp = btnH * 0.3f;
            float x = (w - (side + gp + main)) / 2;
            buttons.Add(new RectangleF(x, by, side, btnH), Glyph.Reset, null, false, StartAgain);
            buttons.Add(new RectangleF(x + side + gp, by, main, btnH), Glyph.Users, "Pick", true, Pick);
            buttons.Paint(g);
        }

        void RenderGroups(Graphics g, RectangleF area)
        {
            int n = groups.Count;
            int cols = (int)Math.Ceiling(Math.Sqrt(n * area.Width / (double)Math.Max(1, area.Height)));
            cols = Math.Max(1, Math.Min(n, cols));
            int rows = (int)Math.Ceiling(n / (double)cols);
            float gap = Theme.S(10);
            float cw = (area.Width - (cols - 1) * gap) / cols, ch = (area.Height - (rows - 1) * gap) / rows;
            Color[] tints = { Theme.Accent, Theme.Green, Theme.Amber, Theme.Purple, Theme.Teal, Theme.Pink, Theme.Red };
            for (int i = 0; i < n; i++)
            {
                var cell = new RectangleF(area.X + (i % cols) * (cw + gap), area.Y + (i / cols) * (ch + gap), cw, ch);
                Color tint = tints[i % tints.Length];
                Draw.FillRound(g, cell, Theme.S(16), Theme.Panel);
                Draw.FillRound(g, new RectangleF(cell.X, cell.Y, cell.Width, Theme.S(6)), Theme.S(3), tint);
                float head = Math.Max(Theme.S(26), cell.Height * 0.18f);
                Draw.Text(g, "Group " + (i + 1), new RectangleF(cell.X, cell.Y + Theme.S(8), cell.Width, head), tint, FontStyle.Bold, false, 36f, null);
                Draw.Lines(g, groups[i], new RectangleF(cell.X + Theme.S(10), cell.Y + head + Theme.S(12), cell.Width - Theme.S(20), cell.Height - head - Theme.S(20)), Theme.Text, 32f);
            }
        }
    }

    class DiceForm : ToolForm
    {
        Stepper count;
        int[] values = { 5, 2 };
        float[] tilt = new float[6];
        int rolling;
        DisplayButtons buttons;

        public DiceForm() : base("Dice", 760, 480)
        {
            AddLabel("Number of dice");
            count = AddNumber(1, 6, Store.GetInt("dice.count", 2), 36);
            count.ValueChanged += delegate { Store.Set("dice.count", (int)count.Value); Randomise(); View.Invalidate(); };
            AddWindowButtons(true);
            buttons = new DisplayButtons(View);
            Tick.Interval = 60;
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !buttons.Contains(e.Location)) Roll(); };
            Randomise();
        }

        void Randomise()
        {
            values = new int[(int)count.Value];
            for (int i = 0; i < values.Length; i++) values[i] = Store.Rng.Next(1, 7);
        }

        void Roll() { if (rolling > 0) return; rolling = 12; Tick.Start(); }

        protected override void OnTick()
        {
            if (rolling > 0)
            {
                Randomise();
                rolling--;
                for (int i = 0; i < tilt.Length; i++) tilt[i] = rolling == 0 ? 0 : (float)(Store.Rng.NextDouble() * 30 - 15) * rolling / 12f;
                if (rolling == 0) { Tick.Stop(); Sound.Blip(); }
            }
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space || key == Keys.Enter) { Roll(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            Color felt = Color.FromArgb(20, 83, 61);
            using (var bg = new LinearGradientBrush(r, Draw.Mix(felt, Color.White, 0.06), Draw.Mix(felt, Color.Black, 0.25), 90f)) g.FillRectangle(bg, r);
            float w = r.Width, h = r.Height;
            float pad = Math.Max(Theme.S(14), h * 0.045f);
            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(80), h * 0.12f));
            int n = values.Length;
            float totalH = n > 1 ? Math.Max(Theme.S(36), h * 0.1f) : 0;
            float areaH = h - pad * 3 - btnH - totalH;
            int cols = n <= 3 ? n : (n + 1) / 2;
            int rows = n <= 3 ? 1 : 2;
            float size = Math.Min((w * 0.9f) / cols * 0.8f, areaH / rows * 0.82f);
            float gap = size * 0.22f;
            for (int i = 0; i < n; i++)
            {
                int row = rows == 1 ? 0 : i / cols, col = rows == 1 ? i : i % cols;
                int inRow = rows == 1 ? n : (row == 0 ? cols : n - cols);
                float rowW = inRow * size + (inRow - 1) * gap;
                float x = (w - rowW) / 2 + col * (size + gap);
                float y = pad + (areaH - (rows * size + (rows - 1) * gap)) / 2 + row * (size + gap);
                var state = g.Save();
                g.TranslateTransform(x + size / 2, y + size / 2);
                g.RotateTransform(tilt[i]);
                var die = new RectangleF(-size / 2, -size / 2, size, size);
                Draw.FillRound(g, new RectangleF(die.X + size * 0.03f, die.Y + size * 0.06f, size, size), size * 0.18f, Color.FromArgb(80, 0, 0, 0));
                using (var path = Draw.Round(die, size * 0.18f))
                using (var face = new LinearGradientBrush(die, Color.White, Color.FromArgb(226, 230, 236), 90f)) g.FillPath(face, path);
                Pips(g, die, values[i]);
                g.Restore(state);
            }
            if (n > 1)
            {
                string t = rolling > 0 ? "Rolling..." : "Total  " + values.Sum();
                Draw.Text(g, t, new RectangleF(0, h - pad * 2 - btnH - totalH, w, totalH), Color.White, FontStyle.Bold, false, 60f, null);
            }
            buttons.Clear();
            float bw = btnH * 3f;
            buttons.Add(new RectangleF((w - bw) / 2, h - pad - btnH, bw, btnH), Glyph.Dice, "Roll", true, Roll);
            buttons.Paint(g);
        }

        static void Pips(Graphics g, RectangleF d, int v)
        {
            float p = d.Width * 0.18f;
            float[] pos = { 0.26f, 0.5f, 0.74f };
            var spots = new List<Point>();
            if (v % 2 == 1) spots.Add(new Point(1, 1));
            if (v >= 2) { spots.Add(new Point(0, 0)); spots.Add(new Point(2, 2)); }
            if (v >= 4) { spots.Add(new Point(2, 0)); spots.Add(new Point(0, 2)); }
            if (v == 6) { spots.Add(new Point(0, 1)); spots.Add(new Point(2, 1)); }
            using (var b = new SolidBrush(Color.FromArgb(24, 28, 36)))
                foreach (var s in spots)
                    g.FillEllipse(b, d.X + d.Width * pos[s.X] - p / 2, d.Y + d.Height * pos[s.Y] - p / 2, p, p);
        }
    }

    class NumberForm : ToolForm
    {
        Stepper min, max;
        Toggle noRepeat;
        readonly List<int> used = new List<int>();
        int shown = int.MinValue, spinning, final;
        DisplayButtons buttons;

        public NumberForm() : base("Random Number", 760, 480)
        {
            AddLabel("From");
            min = AddNumber(-9999, 99999, Store.GetInt("number.min", 1), 56);
            AddLabel("to");
            max = AddNumber(-9999, 99999, Store.GetInt("number.max", 30), 56);
            AddDivider();
            noRepeat = AddCheck("No repeats", false);
            noRepeat.CheckedChanged += delegate { View.Invalidate(); };
            EventHandler range = delegate { Store.Set("number.min", (int)min.Value); Store.Set("number.max", (int)max.Value); View.Invalidate(); };
            min.ValueChanged += range; max.ValueChanged += range;
            AddWindowButtons(true);
            buttons = new DisplayButtons(View);
            Tick.Interval = 60;
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !buttons.Contains(e.Location)) Pick(); };
        }

        int Lo { get { return (int)Math.Min(min.Value, max.Value); } }
        int Hi { get { return (int)Math.Max(min.Value, max.Value); } }

        void StartAgain() { used.Clear(); shown = int.MinValue; View.Invalidate(); }

        void Pick()
        {
            if (spinning > 0) return;
            int lo = Lo, hi = Hi;
            var pool = Enumerable.Range(lo, Math.Min(200000, hi - lo + 1)).Where(v => !(noRepeat.Checked && used.Contains(v))).ToList();
            if (pool.Count == 0) { used.Clear(); pool = Enumerable.Range(lo, Math.Min(200000, hi - lo + 1)).ToList(); }
            final = pool[Store.Rng.Next(pool.Count)];
            spinning = 14;
            Tick.Start();
        }

        protected override void OnTick()
        {
            if (spinning > 0)
            {
                spinning--;
                if (spinning == 0) { shown = final; used.Add(final); Tick.Stop(); Sound.Blip(); }
                else shown = Store.Rng.Next(Lo, Hi + 1);
            }
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space || key == Keys.Enter) { Pick(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            float w = r.Width, h = r.Height;
            float pad = Math.Max(Theme.S(14), h * 0.045f);
            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(80), h * 0.12f));
            float chipH = Math.Max(Theme.S(26), h * 0.05f);
            float by = h - pad - btnH;
            float chipsY = by - pad * 0.8f - chipH;
            float infoH = Math.Max(Theme.S(22), h * 0.05f);
            float infoY = chipsY - pad * 0.4f - infoH;
            var box = new RectangleF(w * 0.08f, pad, w * 0.84f, infoY - pad * 1.4f);
            if (shown == int.MinValue) Draw.Text(g, Lo + " to " + Hi, Draw.Inset(box, box.Height * 0.15f), Theme.Muted, FontStyle.Regular, false, 120f, null);
            else Draw.Text(g, shown.ToString(), box, spinning > 0 ? Theme.Muted : Theme.Text, FontStyle.Bold, false, 600f, null);
            int range = Hi - Lo + 1;
            string info = noRepeat.Checked ? Math.Max(0, range - used.Count(v => v >= Lo && v <= Hi)) + " of " + range + " numbers left" : "Any number from " + Lo + " to " + Hi;
            Draw.Text(g, info, new RectangleF(0, infoY, w, infoH), Theme.Muted, FontStyle.Regular, false, 14f, null);
            var recent = used.AsEnumerable().Reverse().Skip(spinning == 0 && shown != int.MinValue ? 1 : 0).Take(20).Select(v => v.ToString()).ToList();
            Draw.Chips(g, recent, new RectangleF(w * 0.05f, chipsY, w * 0.9f, chipH), Theme.Panel, Theme.Text, Math.Max(9f, chipH * 0.42f * 72f / g.DpiY));
            buttons.Clear();
            float main = btnH * 2.8f, side = btnH, gp = btnH * 0.3f;
            float x = (w - (side + gp + main)) / 2;
            buttons.Add(new RectangleF(x, by, side, btnH), Glyph.Reset, null, false, StartAgain);
            buttons.Add(new RectangleF(x + side + gp, by, main, btnH), Glyph.Shuffle, "Pick", true, Pick);
            buttons.Paint(g);
        }
    }

    // A large-text notepad that stays on top: learning outcomes, key words, instructions.
    class FrameForm : BaseForm
    {
        readonly TextBox box;
        readonly Panel paper;
        readonly string key;
        readonly Choice colour;
        readonly FlatButton pin;
        float size;
        static readonly Color[] Papers = { Color.White, Color.FromArgb(255, 249, 196), Color.FromArgb(214, 234, 255), Color.FromArgb(214, 245, 222), Color.FromArgb(255, 222, 235), Color.FromArgb(24, 28, 36) };
        static readonly string[] PaperNames = { "White", "Yellow", "Blue", "Green", "Pink", "Dark" };

        public FrameForm(string title, string key, string starter)
        {
            this.key = key;
            Text = title;
            StartPosition = FormStartPosition.Manual;
            var wa = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Theme.S(560), Theme.S(380));
            MinimumSize = new Size(Theme.S(300), Theme.S(200));
            Location = new Point(wa.Right - Width - Theme.S(20), wa.Top + Theme.S(20));
            TopMost = true;
            size = Store.GetInt(key + ".size", 24);

            paper = new Panel();
            paper.Dock = DockStyle.Fill;
            paper.Padding = new Padding(Theme.S(18), Theme.S(14), Theme.S(10), Theme.S(10));
            box = new TextBox();
            box.Multiline = true; box.AcceptsReturn = true; box.ScrollBars = ScrollBars.Vertical;
            box.Dock = DockStyle.Fill; box.BorderStyle = BorderStyle.None;
            box.Text = Store.Get(key + ".text", starter);
            box.TextChanged += delegate { Store.Set(key + ".text", box.Text); };
            paper.Controls.Add(box);
            Controls.Add(paper);

            var bar = new ToolBarPanel();
            bar.Dock = DockStyle.Top;
            Func<string, Glyph, string, EventHandler, FlatButton> add = (text, glyph, tip, click) =>
            {
                var b = new FlatButton(text, glyph, false);
                b.Tip = tip; b.Click += click;
                bar.Controls.Add(b);
                return b;
            };
            add("A−", Glyph.None, "Smaller text", delegate { Zoom(-2); }).MinimumSize = new Size(Theme.S(40), 0);
            add("A+", Glyph.None, "Bigger text", delegate { Zoom(2); }).MinimumSize = new Size(Theme.S(40), 0);
            add("•", Glyph.None, "Add a bullet point", delegate { box.SelectedText = "• "; box.Focus(); }).MinimumSize = new Size(Theme.S(36), 0);
            colour = new Choice(92);
            colour.Items.AddRange(PaperNames);
            colour.SelectedIndex = Math.Max(0, Math.Min(Papers.Length - 1, Store.GetInt(key + ".paper", 0)));
            colour.SelectedIndexChanged += delegate { Store.Set(key + ".paper", colour.SelectedIndex); Apply(); };
            colour.Tip = "Paper colour";
            bar.Controls.Add(colour);
            bar.Controls.Add(new Divider());
            add("", Glyph.Folder, "Open a saved text file", delegate { Open(); });
            add("", Glyph.Save, "Save as a text file", delegate { Save(); });
            add("", Glyph.Trash, "Clear the text", delegate { if (box.TextLength == 0 || MessageBox.Show(this, "Clear all the text?", Text, MessageBoxButtons.YesNo) == DialogResult.Yes) box.Text = ""; });
            pin = new FlatButton("", Glyph.Pin, false);
            pin.Tip = "Keep on top of other windows";
            pin.Toggled = true;
            pin.Click += delegate { TopMost = !TopMost; pin.Toggled = TopMost; };
            bar.AddRight(pin);
            Controls.Add(bar);
            Apply();
            Shown += delegate { box.SelectionStart = box.TextLength; box.SelectionLength = 0; box.Focus(); };
        }

        void Zoom(int by)
        {
            size = Math.Max(10, Math.Min(96, size + by));
            Store.Set(key + ".size", (int)size);
            Apply();
        }

        void Apply()
        {
            int p = Math.Max(0, Math.Min(Papers.Length - 1, colour.SelectedIndex));
            paper.BackColor = Papers[p];
            box.BackColor = Papers[p];
            box.ForeColor = p == Papers.Length - 1 ? Color.White : Color.FromArgb(17, 24, 39);
            box.Font = Theme.Font(size, FontStyle.Bold);
        }

        void Open()
        {
            using (var d = new OpenFileDialog())
            {
                d.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                d.InitialDirectory = Store.Dir("Frames");
                if (d.ShowDialog(this) == DialogResult.OK) box.Text = Store.ReadText(d.FileName).Replace("\r\n", "\n").Replace("\n", "\r\n");
            }
        }

        void Save()
        {
            using (var d = new SaveFileDialog())
            {
                d.Filter = "Text files (*.txt)|*.txt";
                d.InitialDirectory = Store.Dir("Frames");
                d.FileName = Text + ".txt";
                if (d.ShowDialog(this) == DialogResult.OK) Store.WriteText(d.FileName, box.Text);
            }
        }
    }

    // A strip of scrolling text along the top or bottom of the screen.
    class TickerForm : Form
    {
        readonly Timer timer = new Timer();
        float offset;
        string text;
        float textWidth = -1;
        int speed;
        bool bottom;
        readonly ContextMenuStrip menu = new ContextMenuStrip();

        public TickerForm()
        {
            Text = "News Ticker";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(17, 24, 39);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            text = Store.Get("ticker.text", "Welcome to today's lesson   •   Double-click this bar to change the message   •   Right-click for options");
            speed = Store.GetInt("ticker.speed", 3);
            bottom = Store.Get("ticker.bottom", "1") == "1";
            Place();
            menu.Items.Add("Edit message", null, delegate { EditText(); });
            menu.Items.Add("Faster", null, delegate { speed = Math.Min(12, speed + 1); Store.Set("ticker.speed", speed); });
            menu.Items.Add("Slower", null, delegate { speed = Math.Max(1, speed - 1); Store.Set("ticker.speed", speed); });
            menu.Items.Add("Move to top / bottom", null, delegate { bottom = !bottom; Store.Set("ticker.bottom", bottom ? "1" : "0"); Place(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Close ticker", null, delegate { Close(); });
            Menus.Style(menu);
            ContextMenuStrip = menu;
            DoubleClick += delegate { EditText(); };
            timer.Interval = 30;
            timer.Tick += delegate { offset += speed * Theme.Scale; if (textWidth > 0 && offset > textWidth) offset -= textWidth; Invalidate(); };
            timer.Start();
            FormClosed += delegate { timer.Dispose(); };
        }

        void Place()
        {
            var s = Screen.PrimaryScreen.Bounds;
            int h = Theme.S(56);
            Bounds = new Rectangle(s.Left, bottom ? Screen.PrimaryScreen.WorkingArea.Bottom - h : s.Top, s.Width, h);
        }

        void EditText()
        {
            TopMost = false;
            string t = TextEditDialog.Show(this, "Ticker message", "The message scrolls across the screen. Separate lines are joined with a bullet.", text.Replace("   •   ", "\r\n"));
            TopMost = true;
            if (t == null) return;
            text = string.Join("   •   ", t.Replace("\r\n", "\n").Split('\n').Where(l => l.Trim().Length > 0).Select(l => l.Trim()).ToArray());
            Store.Set("ticker.text", text);
            textWidth = -1; offset = 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Draw.Quality(g);
            g.Clear(BackColor);
            Font f = Theme.Font(Height * 0.4f * 72f / g.DpiY, FontStyle.Bold);
            string run = text + "   \u2022   ";
            if (textWidth < 0) textWidth = Math.Max(10, g.MeasureString(run, f, int.MaxValue, StringFormat.GenericTypographic).Width + g.MeasureString(" ", f).Width * 3);
            float badge = Height * 1.1f;
            using (var b = new SolidBrush(Color.White))
                for (float x = badge - offset; x < Width; x += textWidth)
                    g.DrawString(run, f, b, x, (Height - f.GetHeight(g)) / 2f);
            // Soft fade at the right-hand edge, and a badge on the left that the text slides under.
            float fade = Height * 1.5f;
            using (var lg = new LinearGradientBrush(new RectangleF(Width - fade, 0, fade + 1, Height), Color.FromArgb(0, BackColor), BackColor, 0f))
                g.FillRectangle(lg, Width - fade, 0, fade + 1, Height);
            using (var lg = new LinearGradientBrush(new RectangleF(badge, 0, fade * 0.6f, Height), BackColor, Color.FromArgb(0, BackColor), 0f))
                g.FillRectangle(lg, badge, 0, fade * 0.6f, Height);
            using (var b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, 0, 0, badge, Height);
            float ic = Height * 0.5f;
            Glyphs.Draw(g, Glyph.Speak, new RectangleF((badge - ic) / 2, (Height - ic) / 2, ic, ic), Color.White);
            using (var b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, 0, bottom ? 0 : Height - Theme.S(2), Width, Theme.S(2));
        }
    }

    class PlenaryForm : ToolForm
    {
        static readonly string[] Levels = { "Remember", "Understand", "Apply", "Analyse", "Evaluate", "Create" };
        static readonly Color[] Tints = { Theme.Red, Theme.Amber, Theme.Green, Theme.Teal, Theme.Accent, Theme.Purple };
        static readonly string[][] Stems =
        {
            new[] { "Write down three facts you learned today.", "What were the key words from this lesson, and what do they mean?", "List the steps we followed today, in order.", "What is the definition of the main idea we studied?", "Who or what was today's lesson about?" },
            new[] { "Explain today's main idea in your own words.", "Summarise the lesson in one sentence.", "Give an example that shows what today's key term means.", "How would you describe this topic to someone who missed the lesson?", "What is the difference between the two main ideas we met today?" },
            new[] { "Where could you use today's learning outside this classroom?", "Solve a new problem using the method from today.", "What would happen if you used today's idea in a different situation?", "Show how today's learning links to something we did last week.", "Write a question that someone could answer using today's method." },
            new[] { "What are the most important parts of today's topic, and how do they fit together?", "What evidence supports the main idea from today?", "Compare two things from today's lesson. How are they alike and different?", "What caused the result we saw today?", "Which piece of information was least useful today, and why?" },
            new[] { "What was the most important thing you learned today? Justify your choice.", "How confident are you with today's work, and what is your evidence?", "Which method from today is best, and why?", "What would you do differently if you repeated today's task?", "Do you agree with today's main conclusion? Explain your reasons." },
            new[] { "Design a quiz question, with its answer, about today's lesson.", "Invent a way to remember today's key idea.", "Plan how you would teach today's lesson to a younger student.", "Suggest a different way we could have investigated today's question.", "Create a title and a headline that sum up today's learning." }
        };
        int level = 0, index = 0;
        DisplayButtons buttons;
        readonly RectangleF[] tabs = new RectangleF[6];

        public PlenaryForm() : base("Plenary Prompts", 960, 560)
        {
            AddLabel("Click a level, or press 1 to 6  ·  Space for any level");
            AddWindowButtons(false);
            buttons = new DisplayButtons(View);
            View.MouseClick += delegate(object s, MouseEventArgs e)
            {
                for (int i = 0; i < tabs.Length; i++) if (tabs[i].Contains(e.Location)) { ShowLevel(i); return; }
            };
            ShowLevel(0);
        }

        void ShowLevel(int l)
        {
            int next = Store.Rng.Next(Stems[l].Length);
            if (l == level && next == index) next = (next + 1) % Stems[l].Length;
            level = l; index = next;
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { ShowLevel(Store.Rng.Next(Levels.Length)); return true; }
            if (key == Keys.Enter || key == Keys.Right) { ShowLevel(level); return true; }
            if (key >= Keys.D1 && key <= Keys.D6) { ShowLevel(key - Keys.D1); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            Color tint = Tints[level];
            g.Clear(Theme.Bg);
            float w = r.Width, h = r.Height;
            float pad = Math.Max(Theme.S(14), h * 0.04f);
            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(76), h * 0.11f));
            float tabH = Math.Max(Theme.S(34), h * 0.075f);
            float gap = Theme.S(8);
            float tw = (w - 2 * pad - 5 * gap) / 6;
            for (int i = 0; i < 6; i++)
            {
                tabs[i] = new RectangleF(pad + i * (tw + gap), pad, tw, tabH);
                bool on = i == level;
                Draw.FillRound(g, tabs[i], tabH / 2, on ? Tints[i] : Draw.Mix(Theme.Panel, Tints[i], 0.18));
                Draw.Text(g, (i + 1) + "  " + Levels[i], Draw.Inset(tabs[i], tabH * 0.2f), on ? Color.White : Draw.Mix(Tints[i], Color.White, 0.35), FontStyle.Bold, false, 16f, null);
            }
            float by = h - pad - btnH;
            var card = new RectangleF(pad, pad + tabH + pad, w - 2 * pad, by - pad * 2 - tabH - pad);
            Draw.FillRound(g, card, Theme.S(24), Draw.Mix(Theme.Panel, tint, 0.14));
            Draw.FillRound(g, new RectangleF(card.X, card.Y + card.Height * 0.15f, Theme.S(6), card.Height * 0.7f), Theme.S(3), tint);
            Draw.Wrapped(g, Stems[level][index], new RectangleF(card.X + card.Width * 0.06f, card.Y + card.Height * 0.08f, card.Width * 0.88f, card.Height * 0.84f), Theme.Text, FontStyle.Bold, 110f);
            buttons.Clear();
            float bw = btnH * 3.4f;
            float x = (w - (bw * 2 + btnH * 0.3f)) / 2;
            buttons.Add(new RectangleF(x, by, bw, btnH), Glyph.Next, "Another", false, delegate { ShowLevel(level); });
            buttons.Add(new RectangleF(x + bw + btnH * 0.3f, by, bw, btnH), Glyph.Shuffle, "Any level", true, delegate { ShowLevel(Store.Rng.Next(Levels.Length)); });
            buttons.Paint(g);
        }
    }

    class ScoreboardForm : ToolForm
    {
        Stepper teams;
        string[] names;
        int[] scores;
        static readonly Color[] Tints = { Theme.Accent, Theme.Red, Theme.Green, Theme.Amber, Theme.Purple, Theme.Teal };
        readonly List<KeyValuePair<RectangleF, Action>> nameHits = new List<KeyValuePair<RectangleF, Action>>();
        DisplayButtons buttons;

        public ScoreboardForm() : base("Scoreboard", 960, 520)
        {
            AddLabel("Teams");
            teams = AddNumber(2, 6, Store.GetInt("score.teams", 2), 36);
            teams.ValueChanged += delegate { Store.Set("score.teams", (int)teams.Value); View.Invalidate(); };
            var reset = AddButton("Reset scores", Glyph.Reset, delegate
            {
                if (scores.Any(v => v != 0) && MessageBox.Show(this, "Set every team's score back to 0?", "Scoreboard", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                for (int i = 0; i < 6; i++) scores[i] = 0;
                Save();
            }, false);
            AddLabel("Click a name to rename  ·  keys 1-6 add a point");
            AddWindowButtons(false);
            names = Store.Get("score.names", "Team A|Team B|Team C|Team D|Team E|Team F").Split('|');
            Array.Resize(ref names, 6);
            scores = Store.Get("score.values", "0|0|0|0|0|0").Split('|').Select(v => { int n; int.TryParse(v, out n); return n; }).ToArray();
            Array.Resize(ref scores, 6);
            buttons = new DisplayButtons(View);
            View.MouseClick += delegate(object s, MouseEventArgs e)
            {
                foreach (var h in nameHits.ToArray())
                    if (h.Key.Contains(e.Location)) { h.Value(); break; }
            };
        }

        void Save()
        {
            Store.Set("score.names", string.Join("|", names));
            Store.Set("score.values", string.Join("|", scores.Select(v => v.ToString()).ToArray()));
            View.Invalidate();
        }

        void Add(int team, int by)
        {
            if (team >= (int)teams.Value) return;
            scores[team] += by;
            Save();
            if (by > 0) Sound.Blip();
        }

        protected override bool OnToolKey(Keys key)
        {
            Keys k = key & Keys.KeyCode;
            int t = k >= Keys.D1 && k <= Keys.D6 ? k - Keys.D1 : k >= Keys.NumPad1 && k <= Keys.NumPad6 ? k - Keys.NumPad1 : -1;
            if (t < 0) return false;
            Add(t, (key & Keys.Shift) == Keys.Shift ? -1 : 1);
            return true;
        }

        void Rename(int t)
        {
            string v = InputDialog.Ask(this, "Team name", "Name for this team:", names[t]);
            if (!string.IsNullOrEmpty(v)) { names[t] = v.Replace("|", " "); Save(); }
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            nameHits.Clear();
            buttons.Clear();
            int n = (int)teams.Value;
            float pad = Math.Max(Theme.S(10), r.Height * 0.03f);
            float gap = Theme.S(10);
            float w = (r.Width - 2 * pad - (n - 1) * gap) / n;
            int best = scores.Take(n).Max();
            bool tie = scores.Take(n).Count(v => v == best) > 1;
            for (int i = 0; i < n; i++)
            {
                int t = i;
                var cell = new RectangleF(pad + i * (w + gap), pad, w, r.Height - 2 * pad);
                bool lead = best > 0 && scores[i] == best;
                Draw.FillRound(g, cell, Theme.S(20), Draw.Mix(Theme.Panel, Tints[i], lead ? 0.32 : 0.14));
                Draw.FillRound(g, new RectangleF(cell.X, cell.Y, cell.Width, Theme.S(8)), Theme.S(4), Tints[i]);
                var nameBox = new RectangleF(cell.X + cell.Width * 0.08f, cell.Y + cell.Height * 0.05f, cell.Width * 0.84f, cell.Height * 0.13f);
                Draw.Text(g, names[i] ?? "Team", nameBox, Color.White, FontStyle.Bold, false, 54f, null);
                nameHits.Add(new KeyValuePair<RectangleF, Action>(nameBox, delegate { Rename(t); }));
                if (lead)
                {
                    string badge = tie ? "Joint lead" : "Leading";
                    float bh = Math.Max(Theme.S(22), cell.Height * 0.06f);
                    float bw = bh * (tie ? 4.6f : 3.6f);
                    var br = new RectangleF(cell.X + (cell.Width - bw) / 2, nameBox.Bottom + Theme.S(4), bw, bh);
                    Draw.FillRound(g, br, bh / 2, Tints[i]);
                    Draw.Text(g, badge, Draw.Inset(br, bh * 0.18f), Color.White, FontStyle.Bold, false, 14f, null);
                }
                float bH = Math.Max(Theme.S(44), Math.Min(Theme.S(76), cell.Height * 0.16f));
                float scoreTop = nameBox.Bottom + cell.Height * 0.09f;
                Draw.Text(g, scores[i].ToString(), new RectangleF(cell.X, scoreTop, cell.Width, cell.Bottom - bH - pad * 2 - scoreTop), Color.White, FontStyle.Bold, false, 500f, null);
                float bw2 = (cell.Width - pad * 3) / 2;
                float y = cell.Bottom - pad - bH;
                buttons.Add(new RectangleF(cell.X + pad, y, bw2, bH), Glyph.Minus, null, false, delegate { Add(t, -1); });
                buttons.Add(new RectangleF(cell.X + pad * 2 + bw2, y, bw2, bH), Glyph.Plus, null, Tints[i], delegate { Add(t, 1); });
            }
            buttons.Paint(g);
        }
    }
}
