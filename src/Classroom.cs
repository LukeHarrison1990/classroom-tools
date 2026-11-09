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
        static readonly Color[] On = { Theme.Red, Theme.Amber, Theme.Green };
        RectangleF[] lamps = new RectangleF[3];

        public TrafficForm() : base("Traffic Lights", 380, 720)
        {
            AddButton("Captions", delegate { EditCaptions(); });
            AddButton("Off", delegate { lit = -1; View.Invalidate(); });
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
            else return false;
            View.Invalidate();
            return true;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            string cap = lit >= 0 ? (captions[lit] ?? "").Trim() : "";
            float capH = r.Height * 0.14f;
            float availH = r.Height - capH - r.Height * 0.04f;
            float d = Math.Min(availH / 3.35f, r.Width * 0.72f);
            float bodyW = d * 1.3f, bodyH = d * 3.35f;
            float x = (r.Width - bodyW) / 2f, y = (availH - bodyH) / 2f + r.Height * 0.02f;
            Draw.FillRound(g, new RectangleF(x, y, bodyW, bodyH), d * 0.3f, Color.FromArgb(8, 10, 14));
            for (int i = 0; i < 3; i++)
            {
                var lamp = new RectangleF(x + (bodyW - d) / 2f, y + d * 0.12f + i * d * 1.06f, d, d);
                lamps[i] = lamp;
                Color c = lit == i ? On[i] : Draw.Mix(Color.FromArgb(8, 10, 14), On[i], 0.16);
                if (lit == i)
                    using (var glow = new SolidBrush(Color.FromArgb(50, On[i]))) g.FillEllipse(glow, Draw.Inset(lamp, -d * 0.07f));
                using (var b = new SolidBrush(c)) g.FillEllipse(b, Draw.Inset(lamp, d * 0.04f));
                if (lit == i)
                    using (var hi = new SolidBrush(Color.FromArgb(70, Color.White))) g.FillEllipse(hi, lamp.X + d * 0.2f, lamp.Y + d * 0.14f, d * 0.3f, d * 0.2f);
            }
            if (cap.Length > 0)
                Draw.Text(g, cap, new RectangleF(r.Width * 0.04f, r.Height - capH - r.Height * 0.02f, r.Width * 0.92f, capH), On[lit], FontStyle.Bold, true, 80f, null);
            else if (lit < 0)
                Draw.Label(g, "Click a light, or press R, A or G", new RectangleF(0, r.Height - capH, r.Width, capH), Theme.Muted, 11f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    class PickerForm : ToolForm
    {
        FileChooser chooser;
        CheckBox noRepeat;
        NumericUpDown groupSize;
        List<Student> students = new List<Student>();
        readonly List<string> used = new List<string>();
        string shown = "";
        int spinLeft;
        string finalName;
        List<List<string>> groups;

        public PickerForm() : base("Random Student Picker", 900, 540)
        {
            AddLabel("Class");
            chooser = new FileChooser(this, Bar.Controls, "Classes", "class list", Samples.ClassHelp);
            chooser.Changed += delegate { LoadClass(); };
            AddButton("Pick (Space)", delegate { Pick(); }, true);
            noRepeat = AddCheck("No repeats", true);
            AddButton("Start again", delegate { used.Clear(); shown = ""; groups = null; View.Invalidate(); });
            AddLabel("Groups of");
            groupSize = AddNumber(2, 12, Store.GetInt("picker.group", 4), 50);
            AddButton("Make groups", delegate { MakeGroups(); });
            AddWindowButtons(false);
            Tick.Interval = 70;
            LoadClass();
        }

        void LoadClass()
        {
            students = Store.LoadClass(chooser.Path);
            used.Clear(); shown = ""; groups = null;
            View.Invalidate();
        }

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
            if (students.Count == 0)
            {
                Draw.Wrapped(g, "This class list is empty. Press Edit to add names, one per line.", Draw.Inset(r, r.Width * 0.1f), Theme.Muted, FontStyle.Regular, 28f);
                return;
            }
            if (groups != null) { RenderGroups(g, r); return; }
            bool settled = spinLeft == 0 && shown.Length > 0;
            var box = new RectangleF(r.Width * 0.05f, r.Height * 0.12f, r.Width * 0.9f, r.Height * 0.62f);
            if (shown.Length == 0)
                Draw.Text(g, "Press Space to pick", box, Theme.Muted, FontStyle.Regular, false, 60f, null);
            else
            {
                if (settled) Draw.FillRound(g, box, 28, Draw.Mix(Theme.Bg, Theme.Accent, 0.28));
                Draw.Text(g, shown, Draw.Inset(box, box.Height * 0.08f), settled ? Color.White : Theme.Muted, FontStyle.Bold, true, 260f, null);
            }
            int remaining = noRepeat.Checked ? Pool().Count : students.Count(s => s.Weight > 0);
            string info = students.Count + " students" + (noRepeat.Checked ? "   -   " + remaining + " not yet picked" : "");
            Draw.Label(g, info, new RectangleF(0, r.Height * 0.78f, r.Width, Theme.S(28)), Theme.Muted, 12f, FontStyle.Regular, StringAlignment.Center);
            if (used.Count > 0)
            {
                string recent = "Picked: " + string.Join(", ", used.AsEnumerable().Reverse().Take(8).ToArray());
                Draw.Label(g, recent, new RectangleF(r.Width * 0.05f, r.Height * 0.86f, r.Width * 0.9f, Theme.S(28)), Theme.Muted, 11f, FontStyle.Regular, StringAlignment.Center);
            }
        }

        void RenderGroups(Graphics g, Rectangle r)
        {
            int n = groups.Count;
            int cols = (int)Math.Ceiling(Math.Sqrt(n * r.Width / (double)Math.Max(1, r.Height)));
            cols = Math.Max(1, Math.Min(n, cols));
            int rows = (int)Math.Ceiling(n / (double)cols);
            float cw = r.Width / (float)cols, ch = r.Height / (float)rows;
            Color[] tints = { Theme.Accent, Theme.Green, Theme.Amber, Theme.Purple, Theme.Teal, Theme.Pink, Theme.Red };
            for (int i = 0; i < n; i++)
            {
                var cell = Draw.Inset(new RectangleF((i % cols) * cw, (i / cols) * ch, cw, ch), 8);
                Color tint = tints[i % tints.Length];
                Draw.FillRound(g, cell, 16, Draw.Mix(Theme.Bg, tint, 0.22));
                float head = cell.Height * 0.2f;
                Draw.Text(g, "Group " + (i + 1), new RectangleF(cell.X, cell.Y + 4, cell.Width, head), tint, FontStyle.Bold, false, 40f, null);
                Draw.Wrapped(g, string.Join("\n", groups[i].ToArray()), new RectangleF(cell.X + 8, cell.Y + head + 4, cell.Width - 16, cell.Height - head - 12), Theme.Text, FontStyle.Regular, 40f);
            }
        }
    }

    class DiceForm : ToolForm
    {
        NumericUpDown count;
        int[] values = { 5, 2 };
        int rolling;

        public DiceForm() : base("Dice", 720, 440)
        {
            AddLabel("Dice");
            count = AddNumber(1, 6, Store.GetInt("dice.count", 2), 48);
            count.ValueChanged += delegate { Store.Set("dice.count", (int)count.Value); Randomise(); View.Invalidate(); };
            AddButton("Roll (Space)", delegate { Roll(); }, true);
            AddWindowButtons(true);
            Tick.Interval = 70;
            View.Click += delegate { Roll(); };
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
            g.Clear(Color.FromArgb(18, 70, 52));
            int n = values.Length;
            float totalH = n > 1 ? r.Height * 0.16f : 0;
            float size = Math.Min((r.Width * 0.92f) / n * 0.86f, (r.Height - totalH) * 0.78f);
            float gap = size * 0.16f;
            float x = (r.Width - (n * size + (n - 1) * gap)) / 2f;
            float y = (r.Height - totalH - size) / 2f;
            for (int i = 0; i < n; i++)
            {
                var die = new RectangleF(x + i * (size + gap), y, size, size);
                Draw.FillRound(g, new RectangleF(die.X + size * 0.03f, die.Y + size * 0.04f, size, size), size * 0.16f, Color.FromArgb(70, 0, 0, 0));
                Draw.FillRound(g, die, size * 0.16f, Color.White);
                Pips(g, die, values[i]);
            }
            if (n > 1 && rolling == 0)
                Draw.Text(g, "Total " + values.Sum(), new RectangleF(0, r.Height - totalH - r.Height * 0.03f, r.Width, totalH), Color.White, FontStyle.Bold, false, 90f, null);
        }

        static void Pips(Graphics g, RectangleF d, int v)
        {
            float p = d.Width * 0.19f;
            float[] pos = { 0.25f, 0.5f, 0.75f };
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
        NumericUpDown min, max;
        CheckBox noRepeat;
        readonly List<int> used = new List<int>();
        int shown = int.MinValue, spinning, final;

        public NumberForm() : base("Random Number", 700, 440)
        {
            AddLabel("From");
            min = AddNumber(-9999, 99999, Store.GetInt("number.min", 1), 76);
            AddLabel("to");
            max = AddNumber(-9999, 99999, Store.GetInt("number.max", 30), 76);
            AddButton("Pick (Space)", delegate { Pick(); }, true);
            noRepeat = AddCheck("No repeats", false);
            AddButton("Start again", delegate { used.Clear(); shown = int.MinValue; View.Invalidate(); });
            AddWindowButtons(true);
            Tick.Interval = 60;
            View.Click += delegate { Pick(); };
        }

        void Pick()
        {
            if (spinning > 0) return;
            int lo = (int)Math.Min(min.Value, max.Value), hi = (int)Math.Max(min.Value, max.Value);
            Store.Set("number.min", lo); Store.Set("number.max", hi);
            var pool = Enumerable.Range(lo, Math.Min(200000, hi - lo + 1)).Where(v => !(noRepeat.Checked && used.Contains(v))).ToList();
            if (pool.Count == 0) { used.Clear(); pool = Enumerable.Range(lo, hi - lo + 1).ToList(); }
            final = pool[Store.Rng.Next(pool.Count)];
            spinning = 14;
            Tick.Start();
        }

        protected override void OnTick()
        {
            if (spinning > 0)
            {
                spinning--;
                int lo = (int)Math.Min(min.Value, max.Value), hi = (int)Math.Max(min.Value, max.Value);
                if (spinning == 0) { shown = final; used.Add(final); Tick.Stop(); Sound.Blip(); }
                else shown = Store.Rng.Next(lo, hi + 1);
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
            var box = new RectangleF(r.Width * 0.08f, r.Height * 0.06f, r.Width * 0.84f, r.Height * 0.74f);
            if (shown == int.MinValue) Draw.Text(g, "Press Space to pick", box, Theme.Muted, FontStyle.Regular, false, 50f, null);
            else Draw.Text(g, shown.ToString(), box, spinning > 0 ? Theme.Muted : Theme.Text, FontStyle.Bold, false, 600f, null);
            if (used.Count > 0)
                Draw.Label(g, "Picked: " + string.Join(", ", used.AsEnumerable().Reverse().Take(20).Select(v => v.ToString()).ToArray()),
                    new RectangleF(r.Width * 0.04f, r.Height * 0.84f, r.Width * 0.92f, Theme.S(30)), Theme.Muted, 12f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    // A large-text notepad that stays on top: learning outcomes, key words, instructions.
    class FrameForm : Form
    {
        readonly TextBox box;
        readonly string key;
        float size;
        static readonly Color[] Papers = { Color.White, Color.FromArgb(255, 249, 196), Color.FromArgb(214, 234, 255), Color.FromArgb(214, 245, 222), Color.FromArgb(255, 222, 235), Color.FromArgb(24, 28, 36) };
        static readonly string[] PaperNames = { "White", "Yellow", "Blue", "Green", "Pink", "Dark" };

        public FrameForm(string title, string key, string starter)
        {
            this.key = key;
            Text = title; Icon = AppIcon.Get();
            StartPosition = FormStartPosition.Manual;
            var wa = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Theme.S(460), Theme.S(340));
            Location = new Point(wa.Right - Width - Theme.S(20), wa.Top + Theme.S(20));
            TopMost = true;
            Font = Theme.Font(10f);
            size = Store.GetInt(key + ".size", 22);

            box = new TextBox();
            box.Multiline = true; box.AcceptsReturn = true; box.ScrollBars = ScrollBars.Vertical;
            box.Dock = DockStyle.Fill; box.BorderStyle = BorderStyle.None;
            box.Text = Store.Get(key + ".text", starter);
            box.TextChanged += delegate { Store.Set(key + ".text", box.Text); };
            Controls.Add(box);

            var menu = new MenuStrip();
            menu.Items.Add("Bigger", null, delegate { Zoom(2); });
            menu.Items.Add("Smaller", null, delegate { Zoom(-2); });
            menu.Items.Add("Bullet", null, delegate { box.SelectedText = "• "; box.Focus(); });
            var paper = new ToolStripMenuItem("Colour");
            for (int i = 0; i < Papers.Length; i++)
            {
                int index = i;
                paper.DropDownItems.Add(PaperNames[i], null, delegate { Store.Set(key + ".paper", index); Apply(); });
            }
            menu.Items.Add(paper);
            menu.Items.Add("Open", null, delegate { Open(); });
            menu.Items.Add("Save as", null, delegate { Save(); });
            menu.Items.Add("Clear", null, delegate { box.Text = ""; });
            var top = new ToolStripMenuItem("On top");
            top.Checked = true;
            top.Click += delegate { TopMost = !TopMost; top.Checked = TopMost; };
            menu.Items.Add(top);
            MainMenuStrip = menu;
            Controls.Add(menu);
            Apply();
            Shown += delegate { box.SelectionStart = box.TextLength; box.SelectionLength = 0; };
        }

        void Zoom(int by)
        {
            size = Math.Max(10, Math.Min(96, size + by));
            Store.Set(key + ".size", (int)size);
            Apply();
        }

        void Apply()
        {
            int p = Math.Max(0, Math.Min(Papers.Length - 1, Store.GetInt(key + ".paper", 0)));
            box.BackColor = Papers[p];
            box.ForeColor = p == Papers.Length - 1 ? Color.White : Color.FromArgb(20, 24, 31);
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
            BackColor = Color.FromArgb(14, 30, 70);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            text = Store.Get("ticker.text", "Welcome to today's lesson   •   Double-click this bar to change the message   •   Right-click for options");
            speed = Store.GetInt("ticker.speed", 3);
            bottom = Store.Get("ticker.bottom", "1") == "1";
            Place();
            menu.Items.Add("Edit message", null, delegate { EditText(); });
            menu.Items.Add("Faster", null, delegate { speed = Math.Min(12, speed + 1); Store.Set("ticker.speed", speed); });
            menu.Items.Add("Slower", null, delegate { speed = Math.Max(1, speed - 1); Store.Set("ticker.speed", speed); });
            menu.Items.Add("Move to top / bottom", null, delegate { bottom = !bottom; Store.Set("ticker.bottom", bottom ? "1" : "0"); Place(); });
            menu.Items.Add("Close ticker", null, delegate { Close(); });
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
            Font f = Theme.Font(Height * 0.42f * 72f / g.DpiY, FontStyle.Bold);
            string run = text + "   •   ";
            if (textWidth < 0) textWidth = Math.Max(10, g.MeasureString(run, f, int.MaxValue, StringFormat.GenericTypographic).Width + g.MeasureString(" ", f).Width * 3);
            using (var b = new SolidBrush(Color.White))
                for (float x = -offset; x < Width; x += textWidth)
                    g.DrawString(run, f, b, x, (Height - f.GetHeight(g)) / 2f);
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

        public PlenaryForm() : base("Plenary Prompts", 900, 520)
        {
            for (int i = 0; i < Levels.Length; i++)
            {
                int l = i;
                var b = AddButton(Levels[i], delegate { Show(l); });
                b.BackColor = Draw.Mix(Theme.Panel, Tints[i], 0.6);
            }
            AddButton("Random (Space)", delegate { Show(Store.Rng.Next(Levels.Length)); }, true);
            AddWindowButtons(false);
            Show(0);
        }

        void Show(int l)
        {
            int next = Store.Rng.Next(Stems[l].Length);
            if (l == level && next == index) next = (next + 1) % Stems[l].Length;
            level = l; index = next;
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { Show(Store.Rng.Next(Levels.Length)); return true; }
            if (key >= Keys.D1 && key <= Keys.D6) { Show(key - Keys.D1); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Draw.Mix(Theme.Bg, Tints[level], 0.16));
            Draw.Text(g, Levels[level], new RectangleF(0, r.Height * 0.05f, r.Width, r.Height * 0.14f), Tints[level], FontStyle.Bold, false, 80f, null);
            Draw.Wrapped(g, Stems[level][index], new RectangleF(r.Width * 0.07f, r.Height * 0.22f, r.Width * 0.86f, r.Height * 0.68f), Theme.Text, FontStyle.Bold, 110f);
        }
    }

    class ScoreboardForm : ToolForm
    {
        NumericUpDown teams;
        string[] names;
        int[] scores;
        static readonly Color[] Tints = { Theme.Accent, Theme.Red, Theme.Green, Theme.Amber, Theme.Purple, Theme.Teal };
        readonly List<KeyValuePair<RectangleF, Action>> hits = new List<KeyValuePair<RectangleF, Action>>();

        public ScoreboardForm() : base("Scoreboard", 900, 480)
        {
            AddLabel("Teams");
            teams = AddNumber(2, 6, Store.GetInt("score.teams", 2), 48);
            teams.ValueChanged += delegate { Store.Set("score.teams", (int)teams.Value); View.Invalidate(); };
            AddButton("Reset scores", delegate { for (int i = 0; i < 6; i++) scores[i] = 0; Save(); });
            AddLabel("Click a team name to rename it");
            AddWindowButtons(false);
            names = Store.Get("score.names", "Team A|Team B|Team C|Team D|Team E|Team F").Split('|');
            Array.Resize(ref names, 6);
            scores = Store.Get("score.values", "0|0|0|0|0|0").Split('|').Select(v => { int n; int.TryParse(v, out n); return n; }).ToArray();
            Array.Resize(ref scores, 6);
            View.MouseDown += delegate(object s, MouseEventArgs e)
            {
                foreach (var h in hits.ToArray())
                    if (h.Key.Contains(e.Location)) { h.Value(); break; }
            };
        }

        void Save()
        {
            Store.Set("score.names", string.Join("|", names));
            Store.Set("score.values", string.Join("|", scores.Select(v => v.ToString()).ToArray()));
            View.Invalidate();
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            hits.Clear();
            int n = (int)teams.Value;
            float w = r.Width / (float)n;
            int best = scores.Take(n).Max();
            for (int i = 0; i < n; i++)
            {
                int t = i;
                var cell = Draw.Inset(new RectangleF(i * w, 0, w, r.Height), 8);
                bool lead = best > 0 && scores[i] == best;
                Draw.FillRound(g, cell, 18, Draw.Mix(Theme.Bg, Tints[i], lead ? 0.42 : 0.2));
                var nameBox = new RectangleF(cell.X + 6, cell.Y + cell.Height * 0.03f, cell.Width - 12, cell.Height * 0.15f);
                Draw.Text(g, names[i] ?? "Team", nameBox, Color.White, FontStyle.Bold, false, 60f, null);
                hits.Add(new KeyValuePair<RectangleF, Action>(nameBox, delegate
                {
                    string v = InputDialog.Ask(this, "Team name", "Name for this team:", names[t]);
                    if (!string.IsNullOrEmpty(v)) { names[t] = v.Replace("|", " "); Save(); }
                }));
                Draw.Text(g, scores[i].ToString(), new RectangleF(cell.X, cell.Y + cell.Height * 0.2f, cell.Width, cell.Height * 0.5f), Color.White, FontStyle.Bold, false, 500f, null);
                float bh = cell.Height * 0.2f, bw = (cell.Width - 30) / 2f, by = cell.Bottom - bh - 10;
                var minus = new RectangleF(cell.X + 10, by, bw, bh);
                var plus = new RectangleF(cell.X + 20 + bw, by, bw, bh);
                Draw.FillRound(g, minus, 12, Theme.Panel2);
                Draw.FillRound(g, plus, 12, Tints[i]);
                Draw.Text(g, "−", minus, Color.White, FontStyle.Bold, false, 80f, null);
                Draw.Text(g, "+", plus, Color.White, FontStyle.Bold, false, 80f, null);
                hits.Add(new KeyValuePair<RectangleF, Action>(minus, delegate { scores[t]--; Save(); }));
                hits.Add(new KeyValuePair<RectangleF, Action>(plus, delegate { scores[t]++; Save(); Sound.Blip(); }));
            }
        }
    }
}
