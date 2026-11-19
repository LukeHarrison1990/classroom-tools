// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - start-up, tool list, launcher window and notification-area icon.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Classroom Toolkit")]
[assembly: AssemblyProduct("Classroom Toolkit")]
[assembly: AssemblyDescription("Timers, pickers, quiz games and other classroom tools")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Luke Harrison. MIT licence.")]
[assembly: AssemblyCompany("Luke Harrison")]
[assembly: AssemblyVersion("1.1.5.0")]
[assembly: AssemblyFileVersion("1.1.5.0")]
[assembly: AssemblyInformationalVersion("1.1.5")]

namespace ClassroomToolkit
{
    class Tool
    {
        public string Category, Name, About;
        public Color Tint;
        public Glyph Icon;
        public Func<Form> Create;   // a window, opened once and re-activated if already open
        public Action Run;          // or an action
        public bool Many;           // more than one window allowed
        public bool HideLauncher;   // launcher must be out of the way first
    }

    static class Tools
    {
        public static readonly List<Tool> All = new List<Tool>();
        static readonly Dictionary<Tool, Form> open = new Dictionary<Tool, Form>();

        static void Add(string cat, Color tint, string name, string about, Func<Form> create)
        {
            All.Add(new Tool { Category = cat, Tint = tint, Name = name, About = about, Create = create });
        }

        public static void Register()
        {
            string c = "Classroom";
            Add(c, Theme.Green, "Countdown Timer", "Big on-screen countdown with a sound at the end", () => new CountdownForm());
            Add(c, Theme.Green, "Interval Timer", "Repeating work and rest periods", () => new IntervalForm());
            Add(c, Theme.Green, "Stopwatch", "Count up, with lap times", () => new StopwatchForm());
            Add(c, Theme.Green, "Exam Clock", "Clock with start, finish and extra-time finish", () => new ExamClockForm());
            Add(c, Theme.Green, "Traffic Lights", "Red, amber and green for noise level or understanding", () => new TrafficForm());
            Add(c, Theme.Green, "Random Student Picker", "Pick a name from a class list, or make groups", () => new PickerForm());
            Add(c, Theme.Green, "Dice", "Roll up to six dice", () => new DiceForm());
            Add(c, Theme.Green, "Random Number", "Pick a number in any range", () => new NumberForm());
            Add(c, Theme.Green, "Scoreboard", "Team scores for up to six teams", () => new ScoreboardForm());
            Add(c, Theme.Green, "Plenary Prompts", "Question prompts at six levels of thinking", () => new PlenaryForm());
            Add(c, Theme.Green, "Learning Outcomes", "Large-text note that stays on top", () => new FrameForm("Learning Outcomes", "frame.outcomes", "Today we are learning to...\r\n• "));
            Add(c, Theme.Green, "Key Words", "Large-text key word list that stays on top", () => new FrameForm("Key Words", "frame.keywords", "Key words\r\n• "));
            Add(c, Theme.Green, "News Ticker", "Scrolling message along the edge of the screen", () => new TickerForm());

            c = "On-screen";
            Add(c, Theme.Amber, "Draw on Screen", "Freeze the screen and write over it", () => new AnnotateForm());
            All[All.Count - 1].HideLauncher = true;
            Add(c, Theme.Amber, "Spotlight", "Dim everything except around the pointer", () => new SpotlightForm());
            Add(c, Theme.Amber, "Reading Guide", "Coloured band that follows the pointer", () => new ReadingGuideForm());
            Add(c, Theme.Amber, "Cover-up", "Hide part of the screen with a movable panel", () => new CoverForm());
            All[All.Count - 1].Many = true;

            c = "Quiz games";
            Add(c, Theme.Purple, "Flash Cards", "Flip through questions and answers", () => new FlashCardsForm());
            Add(c, Theme.Purple, "Noughts and Crosses Quiz", "Answer correctly to claim a square", () => new NoughtsForm());
            Add(c, Theme.Purple, "Four in a Row Quiz", "Answer correctly to drop a counter", () => new FourForm());
            Add(c, Theme.Purple, "Grid Quiz", "Categories and points board for teams", () => new GridQuizForm());

            c = "Teacher";
            All.Add(new Tool { Category = c, Tint = Theme.Accent, Name = "Sticky Notes", About = "Notes that stay on the desktop between sessions", Run = StickyNote.ShowOrCreate });
            Add(c, Theme.Accent, "Register Reminders", "Banner reminders at set times of day", () => new RemindersForm());
            Add(c, Theme.Accent, "Exam Countdown", "Days remaining until exams and deadlines", () => new ExamCountdownForm());
            Add(c, Theme.Accent, "Learn Student Names", "Practise names from a folder of photos", () => new NameLearnerForm());
            Add(c, Theme.Accent, "Report Statement Builder", "Build report comments from a statement bank", () => new StatementForm());
            Add(c, Theme.Accent, "Special Characters", "Maths, Greek, accents and symbols to copy", () => new CharactersForm());
            Add(c, Theme.Accent, "Text Tidy", "Change case, sort lists, remove duplicates", () => new TextTidyForm());

            c = "Learner";
            Add(c, Theme.Teal, "Text to Speech", "Read typed or copied text aloud", () => new SpeechForm());
            Add(c, Theme.Teal, "Speed Reader", "Practise reading one word at a time", () => new SpeedReaderForm());
            Add(c, Theme.Teal, "Breathing Exercise", "Guided calm breathing", () => new BreathingForm());
            Add(c, Theme.Teal, "Quotes and Goals", "Show quotes or class goals full-screen", () => new QuotesForm());
            AssignIcons();
        }

        static readonly Dictionary<string, Glyph> Icons = new Dictionary<string, Glyph>
        {
            { "Countdown Timer", Glyph.Clock }, { "Interval Timer", Glyph.Reset }, { "Stopwatch", Glyph.Flag }, { "Exam Clock", Glyph.Clock },
            { "Traffic Lights", Glyph.Lights }, { "Random Student Picker", Glyph.Users }, { "Dice", Glyph.Dice }, { "Random Number", Glyph.Shuffle },
            { "Scoreboard", Glyph.Plus }, { "Plenary Prompts", Glyph.Card }, { "Learning Outcomes", Glyph.Note }, { "Key Words", Glyph.Text },
            { "News Ticker", Glyph.Speak }, { "Draw on Screen", Glyph.Edit }, { "Spotlight", Glyph.Spot }, { "Reading Guide", Glyph.Minus },
            { "Cover-up", Glyph.Stop }, { "Flash Cards", Glyph.Swap }, { "Noughts and Crosses Quiz", Glyph.Close }, { "Four in a Row Quiz", Glyph.Grid },
            { "Grid Quiz", Glyph.Grid }, { "Sticky Notes", Glyph.Note }, { "Register Reminders", Glyph.Bell }, { "Exam Countdown", Glyph.Flag },
            { "Learn Student Names", Glyph.Users }, { "Report Statement Builder", Glyph.Edit }, { "Special Characters", Glyph.Text }, { "Text Tidy", Glyph.Check },
            { "Text to Speech", Glyph.Speak }, { "Speed Reader", Glyph.Next }, { "Breathing Exercise", Glyph.Breath }, { "Quotes and Goals", Glyph.Quote }
        };

        public static void AssignIcons()
        {
            foreach (Tool t in All) { Glyph g; t.Icon = Icons.TryGetValue(t.Name, out g) ? g : Glyph.Card; }
        }

        public static void Launch(Tool t, Form launcher)
        {
            try
            {
                if (t.Run != null) { t.Run(); return; }
                Form existing;
                if (!t.Many && open.TryGetValue(t, out existing) && !existing.IsDisposed)
                {
                    if (existing.WindowState == FormWindowState.Minimized) existing.WindowState = FormWindowState.Normal;
                    existing.Show(); existing.Activate();
                    return;
                }
                if (t.HideLauncher && launcher != null && launcher.Visible)
                {
                    launcher.Hide();
                    Application.DoEvents();
                    Thread.Sleep(280);
                }
                Form f = t.Create();
                open[t] = f;
                f.Show();
                f.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sorry, " + t.Name + " could not be opened.\n\n" + ex.Message, "Classroom Toolkit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    // Owner-drawn launcher grid: responsive columns, category headings, slim scrollbar,
    // smooth wheel scrolling and keyboard selection. Drawing everything here keeps
    // spacing identical on every version of Windows.
    class LauncherGrid : Control
    {
        class Item
        {
            public Tool Tool;          // null for a heading
            public string Heading;
            public int Count;
            public Color Tint;
            public Rectangle Box;      // content coordinates (before scrolling)
        }

        readonly List<Item> items = new List<Item>();
        readonly List<Item> tiles = new List<Item>();
        readonly System.Windows.Forms.Timer anim = new System.Windows.Forms.Timer();
        readonly StringFormat nameFormat, aboutFormat;
        string query = "";
        float scroll, target;
        int contentHeight;
        Item hot, pressed;
        int selected = -1;
        bool showRing;
        bool dragging, thumbHot;
        int dragOffset;

        public event Action<Tool> Open;

        public LauncherGrid()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Bg;
            nameFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
            nameFormat.FormatFlags |= StringFormatFlags.NoWrap;
            nameFormat.Trimming = StringTrimming.EllipsisCharacter;
            aboutFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
            aboutFormat.FormatFlags |= StringFormatFlags.LineLimit;
            aboutFormat.Trimming = StringTrimming.EllipsisWord;
            anim.Interval = 15;
            anim.Tick += delegate
            {
                float d = target - scroll;
                if (Math.Abs(d) < 0.5f) { scroll = target; anim.Stop(); }
                else scroll += d * 0.28f;
                UpdateHot(PointToClient(Cursor.Position));
                Invalidate();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { anim.Dispose(); nameFormat.Dispose(); aboutFormat.Dispose(); }
            base.Dispose(disposing);
        }

        // ---------- content ----------

        public void Filter(string q)
        {
            query = (q ?? "").Trim();
            string l = query.ToLowerInvariant();
            items.Clear(); tiles.Clear();
            Item head = null;
            foreach (Tool t in Tools.All)
            {
                if (l.Length > 0 && !(t.Name + " " + t.About + " " + t.Category).ToLowerInvariant().Contains(l)) continue;
                if (head == null || head.Heading != t.Category)
                {
                    head = new Item { Heading = t.Category, Tint = t.Tint };
                    items.Add(head);
                }
                head.Count++;
                var it = new Item { Tool = t, Tint = t.Tint };
                items.Add(it); tiles.Add(it);
            }
            hot = pressed = null;
            selected = tiles.Count > 0 && l.Length > 0 ? 0 : -1;
            showRing = selected >= 0;
            scroll = target = 0;
            DoLayout();
            Invalidate();
        }

        public Tool Current
        {
            get
            {
                if (selected >= 0 && selected < tiles.Count) return tiles[selected].Tool;
                return tiles.Count > 0 ? tiles[0].Tool : null;
            }
        }

        public int Count { get { return tiles.Count; } }

        // ---------- layout ----------

        int Pad { get { return Theme.S(24); } }
        int BarWidth { get { return Theme.S(12); } }

        void DoLayout()
        {
            int pad = Pad, gap = Theme.S(12), minW = Theme.S(250), tileH = Theme.S(84);
            int usable = Math.Max(Theme.S(200), Width - pad * 2 - BarWidth / 2);
            int cols = Math.Max(1, (usable + gap) / (minW + gap));
            int tileW = cols == 1 ? usable : (usable - gap * (cols - 1)) / cols;
            int y = Theme.S(18), col = 0;
            bool first = true;
            foreach (Item it in items)
            {
                if (it.Tool == null)
                {
                    if (col > 0) { y += tileH; col = 0; }
                    if (!first) y += Theme.S(26);
                    first = false;
                    it.Box = new Rectangle(pad, y, usable, Theme.S(26));
                    y += Theme.S(26) + Theme.S(10);
                    continue;
                }
                if (col == cols) { col = 0; y += tileH + gap; }
                it.Box = new Rectangle(pad + col * (tileW + gap), y, tileW, tileH);
                col++;
            }
            if (col > 0) y += tileH;
            contentHeight = y + Theme.S(24);
            target = Clamp(target); scroll = Clamp(scroll);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
            Invalidate();
        }

        float MaxScroll { get { return Math.Max(0, contentHeight - Height); } }
        float Clamp(float v) { return Math.Max(0, Math.Min(MaxScroll, v)); }

        public void ScrollBy(float px)
        {
            target = Clamp(target + px);
            if (target != scroll) anim.Start();
        }

        public void Wheel(int delta)
        {
            ScrollBy(-delta / 120f * Theme.S(96));
        }

        void EnsureVisible(Item it)
        {
            int margin = Theme.S(16);
            Rectangle b = it.Box;
            int idx = items.IndexOf(it);
            // Keep the category heading on screen when moving to the first row of a group.
            if (idx > 0 && items[idx - 1].Tool == null) b = Rectangle.Union(b, items[idx - 1].Box);
            if (b.Top - margin < target) ScrollBy(b.Top - margin - target);
            else if (b.Bottom + margin > target + Height) ScrollBy(b.Bottom + margin - target - Height);
        }

        // ---------- keyboard ----------

        public void MoveSelection(int dx, int dy)
        {
            if (tiles.Count == 0) return;
            if (selected < 0 || !showRing) { selected = Math.Max(0, selected); showRing = true; EnsureVisible(tiles[selected]); Invalidate(); return; }
            Item cur = tiles[selected];
            int next = selected;
            if (dx != 0) next = Math.Max(0, Math.Min(tiles.Count - 1, selected + dx));
            else
            {
                int cx = cur.Box.X + cur.Box.Width / 2;
                Item best = null; int bestRow = 0, bestDx = 0;
                foreach (Item t in tiles)
                {
                    bool ok = dy > 0 ? t.Box.Top > cur.Box.Top : t.Box.Top < cur.Box.Top;
                    if (!ok) continue;
                    int row = Math.Abs(t.Box.Top - cur.Box.Top);
                    int ddx = Math.Abs(t.Box.X + t.Box.Width / 2 - cx);
                    if (best == null || row < bestRow || (row == bestRow && ddx < bestDx)) { best = t; bestRow = row; bestDx = ddx; }
                }
                if (best != null) next = tiles.IndexOf(best);
            }
            selected = next;
            EnsureVisible(tiles[selected]);
            Invalidate();
        }

        public void Home(bool end)
        {
            if (tiles.Count == 0) return;
            selected = end ? tiles.Count - 1 : 0; showRing = true;
            EnsureVisible(tiles[selected]); Invalidate();
        }

        public void Page(int dir) { ScrollBy(dir * (Height - Theme.S(80))); }

        // ---------- mouse ----------

        Rectangle Track { get { return new Rectangle(Width - BarWidth, Theme.S(6), BarWidth, Height - Theme.S(12)); } }

        Rectangle Thumb
        {
            get
            {
                if (MaxScroll <= 0) return Rectangle.Empty;
                Rectangle tr = Track;
                int h = Math.Max(Theme.S(40), (int)(tr.Height * (float)Height / contentHeight));
                int y = tr.Y + (int)((tr.Height - h) * (scroll / MaxScroll));
                return new Rectangle(tr.X, y, tr.Width, h);
            }
        }

        Item HitTest(Point p)
        {
            if (p.X >= Width - BarWidth) return null;
            var c = new Point(p.X, p.Y + (int)scroll);
            foreach (Item it in tiles) if (it.Box.Contains(c)) return it;
            return null;
        }

        void UpdateHot(Point p)
        {
            Item h = ClientRectangle.Contains(p) && !dragging ? HitTest(p) : null;
            bool th = MaxScroll > 0 && p.X >= Width - BarWidth && ClientRectangle.Contains(p);
            if (h != hot || th != thumbHot) { hot = h; thumbHot = th; Cursor = h != null ? Cursors.Hand : Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging)
            {
                Rectangle tr = Track, th = Thumb;
                float range = tr.Height - th.Height;
                if (range > 0) { scroll = target = Clamp((e.Y - dragOffset - tr.Y) / range * MaxScroll); Invalidate(); }
                return;
            }
            UpdateHot(e.Location);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!dragging) { hot = null; pressed = null; thumbHot = false; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (MaxScroll > 0 && e.X >= Width - BarWidth)
            {
                Rectangle th = Thumb;
                if (th.Contains(e.Location)) { dragging = true; dragOffset = e.Y - th.Y; Capture = true; }
                else Page(e.Y < th.Y ? -1 : 1);
                Invalidate();
                return;
            }
            pressed = HitTest(e.Location);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (dragging) { dragging = false; Capture = false; UpdateHot(e.Location); Invalidate(); return; }
            Item p = pressed; pressed = null;
            Invalidate();
            if (p != null && p == HitTest(e.Location))
            {
                selected = tiles.IndexOf(p); showRing = false;
                if (Open != null) Open(p.Tool);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Wheel(e.Delta);
        }

        // ---------- painting ----------

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Bg);
            Draw.Quality(g);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int off = (int)Math.Round(scroll);

            if (tiles.Count == 0)
            {
                using (var f = new StringFormat { Alignment = StringAlignment.Center })
                using (var b1 = new SolidBrush(Theme.Text))
                using (var b2 = new SolidBrush(Theme.Muted))
                {
                    var r = new RectangleF(Pad, Theme.S(60), Width - Pad * 2, Theme.S(30));
                    g.DrawString("No tools match “" + query + "”", Theme.Semi(12f), b1, r, f);
                    r.Y += Theme.S(32);
                    g.DrawString("Try a shorter word, or press Esc to clear the search.", Theme.Font(9.5f), b2, r, f);
                }
                return;
            }

            foreach (Item it in items)
            {
                var b = it.Box; b.Offset(0, -off);
                if (b.Bottom < 0 || b.Top > Height) continue;
                if (it.Tool == null) PaintHeading(g, it, b);
                else PaintTile(g, it, b);
            }
            PaintScrollbar(g);
        }

        void PaintHeading(Graphics g, Item it, Rectangle b)
        {
            Font f = Theme.Semi(12f);
            string label = it.Heading;
            SizeF sz = g.MeasureString(label, f, PointF.Empty, StringFormat.GenericTypographic);
            float ty = b.Y + (b.Height - sz.Height) / 2;
            float dot = Theme.S(8);
            Draw.FillRound(g, new RectangleF(b.X + Theme.S(2), b.Y + (b.Height - dot) / 2, dot, dot), dot / 2, it.Tint);
            float x = b.X + Theme.S(18);
            using (var br = new SolidBrush(Theme.Text)) g.DrawString(label, f, br, x, ty, StringFormat.GenericTypographic);
            x += sz.Width + Theme.S(10);
            string count = it.Count == 1 ? "1 tool" : it.Count + " tools";
            Font cf = Theme.Font(9f);
            SizeF cs = g.MeasureString(count, cf, PointF.Empty, StringFormat.GenericTypographic);
            using (var br = new SolidBrush(Theme.Muted)) g.DrawString(count, cf, br, x, b.Y + (b.Height - cs.Height) / 2 + Theme.S(1), StringFormat.GenericTypographic);
            x += cs.Width + Theme.S(14);
            if (x < b.Right)
                using (var p = new Pen(Draw.Mix(Theme.Bg, Theme.Line, 0.6), 1))
                    g.DrawLine(p, x, b.Y + b.Height / 2f + 0.5f, b.Right, b.Y + b.Height / 2f + 0.5f);
        }

        void PaintTile(Graphics g, Item it, Rectangle b)
        {
            bool isHot = it == hot, isDown = it == pressed && isHot;
            bool isSel = showRing && selected >= 0 && selected < tiles.Count && tiles[selected] == it;
            var r = new RectangleF(b.X + 0.5f, b.Y + 0.5f, b.Width - 1, b.Height - 1);
            Color fill = isDown ? Draw.Mix(Theme.Panel2, Color.Black, 0.12) : isHot ? Theme.Panel2 : Theme.Panel;
            Draw.FillRound(g, r, Theme.S(12), fill);
            if (isSel)
                using (var path = Draw.Round(r, Theme.S(12)))
                using (var p = new Pen(Theme.Accent, Theme.S(2)))
                    g.DrawPath(p, path);

            float box = Theme.S(46);
            var ib = new RectangleF(b.X + Theme.S(16), b.Y + (b.Height - box) / 2, box, box);
            Draw.FillRound(g, ib, Theme.S(11), Draw.Mix(fill, it.Tint, isHot ? 0.36 : 0.24));
            float ic = Theme.S(24);
            Glyphs.Draw(g, it.Tool.Icon, new RectangleF(ib.X + (box - ic) / 2, ib.Y + (box - ic) / 2, ic, ic), Draw.Mix(it.Tint, Color.White, 0.25));

            // Name and description share one left edge and one text method.
            float x = ib.Right + Theme.S(14);
            float w = b.Right - Theme.S(14) - x;
            Font nf = Theme.Semi(10.5f);
            if (g.MeasureString(it.Tool.Name, nf, PointF.Empty, StringFormat.GenericTypographic).Width > w) nf = Theme.Semi(9.5f);
            Font af = Theme.Font(9f);
            float nh = nf.GetHeight(g);
            float lh = af.GetHeight(g);
            SizeF asz = g.MeasureString(it.Tool.About, af, new SizeF(w, lh * 2 + 1), aboutFormat);
            float ah = Math.Min(lh * 2, Math.Max(lh, asz.Height));
            float gapY = Theme.S(4);
            float y = b.Y + (b.Height - (nh + gapY + ah)) / 2;
            using (var br = new SolidBrush(Theme.Text))
                g.DrawString(it.Tool.Name, nf, br, new RectangleF(x, y, w, nh + 2), nameFormat);
            using (var br = new SolidBrush(Theme.Muted))
                g.DrawString(it.Tool.About, af, br, new RectangleF(x, y + nh + gapY, w, lh * 2 + 1), aboutFormat);
        }

        void PaintScrollbar(Graphics g)
        {
            Rectangle th = Thumb;
            if (th.IsEmpty) return;
            bool wide = thumbHot || dragging;
            float w = wide ? Theme.S(8) : Theme.S(5);
            var r = new RectangleF(th.Right - Theme.S(3) - w, th.Y, w, th.Height);
            Color c = dragging ? Draw.Mix(Theme.Line, Theme.Text, 0.45) : wide ? Draw.Mix(Theme.Line, Theme.Text, 0.25) : Theme.Line;
            Draw.FillRound(g, r, w / 2, c);
        }
    }

    class Launcher : BaseForm
    {
        readonly LauncherGrid grid = new LauncherGrid();
        readonly TextBox search;
        readonly TextField searchField;
        public bool Exiting;

        public Launcher()
        {
            Text = "Classroom Toolkit";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(1110), Theme.S(720));
            MinimumSize = new Size(Theme.S(540), Theme.S(360));
            KeyPreview = true;

            var head = new ToolBarPanel();
            head.Dock = DockStyle.Top;
            head.Padding = new Padding(Theme.S(24), Theme.S(12), Theme.S(24), Theme.S(12));
            var logo = new Label();
            logo.Size = new Size(Theme.S(32), Theme.S(32)); logo.Margin = new Padding(0, 0, Theme.S(10), 0);
            logo.Paint += delegate(object s, PaintEventArgs e)
            {
                Draw.Quality(e.Graphics);
                e.Graphics.Clear(Theme.Panel);
                Draw.FillRound(e.Graphics, new RectangleF(0, 0, logo.Width - 1, logo.Height - 1), Theme.S(8), Theme.Accent);
                float ic = Theme.S(20);
                Glyphs.Draw(e.Graphics, Glyph.Lights, new RectangleF((logo.Width - ic) / 2, (logo.Height - ic) / 2, ic, ic), Color.White);
            };
            var title = new Label();
            title.Text = "Classroom Toolkit"; title.Font = Theme.Semi(15f); title.ForeColor = Theme.Text; title.AutoSize = true;
            title.Margin = new Padding(0, 0, Theme.S(10), 0);
            var sub = new Label();
            sub.Text = Tools.All.Count + " tools"; sub.Font = Theme.Font(9.5f); sub.ForeColor = Theme.Muted; sub.AutoSize = true;
            head.Controls.AddRange(new Control[] { logo, title, sub });
            searchField = new TextField("", 280);
            searchField.Placeholder = "Search tools";
            search = searchField.Box;
            search.TextChanged += delegate { grid.Filter(search.Text); };
            search.MouseWheel += delegate(object s, MouseEventArgs e) { grid.Wheel(e.Delta); };
            head.AddRight(searchField);

            grid.Dock = DockStyle.Fill;
            grid.Open += delegate(Tool t) { Tools.Launch(t, this); };
            Controls.Add(grid); Controls.Add(head);
            grid.Filter("");
            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (Exiting || e.CloseReason != CloseReason.UserClosing) return;
                e.Cancel = true;
                Hide();
                TrayApp.HiddenNotice();
            };
        }

        // Narrow the search box rather than letting it wrap under the title.
        protected override void OnLayout(LayoutEventArgs e)
        {
            if (searchField != null)
            {
                int w = Math.Max(Theme.S(150), Math.Min(Theme.S(280), ClientSize.Width - Theme.S(400)));
                if (searchField.Width != w) searchField.Width = w;
            }
            base.OnLayout(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool caretKeys = search.Focused && search.Text.Length > 0;
            switch (keyData)
            {
                case Keys.Escape: if (search.Text.Length > 0) search.Text = ""; else Close(); return true;
                case Keys.Enter:
                    { Tool t = grid.Current; if (t != null) Tools.Launch(t, this); return true; }
                case Keys.Down: grid.MoveSelection(0, 1); return true;
                case Keys.Up: grid.MoveSelection(0, -1); return true;
                case Keys.Right: if (caretKeys) break; grid.MoveSelection(1, 0); return true;
                case Keys.Left: if (caretKeys) break; grid.MoveSelection(-1, 0); return true;
                case Keys.PageDown: grid.Page(1); return true;
                case Keys.PageUp: grid.Page(-1); return true;
                case Keys.Control | Keys.Home: grid.Home(false); return true;
                case Keys.Control | Keys.End: grid.Home(true); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void Reveal()
        {
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Show(); Activate();
            search.Focus();
        }
    }

    static class TrayApp
    {
        static NotifyIcon tray;
        static Launcher launcher;
        static bool noticeShown;
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void HiddenNotice()
        {
            if (noticeShown || tray == null) return;
            noticeShown = true;
            try { tray.ShowBalloonTip(4000, "Classroom Toolkit is still running", "Click this icon to open it again, or right-click it for the tools and Exit.", ToolTipIcon.Info); } catch { }
        }

        static bool StartsWithWindows
        {
            get
            {
                try { using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue("ClassroomToolkit") != null; }
                catch { return false; }
            }
            set
            {
                try
                {
                    using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKey))
                    {
                        if (value) k.SetValue("ClassroomToolkit", "\"" + Application.ExecutablePath + "\" --tray");
                        else k.DeleteValue("ClassroomToolkit", false);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Could not change the start-up setting.\n\n" + ex.Message, "Classroom Toolkit"); }
            }
        }

        public static void Run(bool startHidden)
        {
            launcher = new Launcher();
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Classroom Toolkit", null, delegate { launcher.Reveal(); }).Font = new Font(menu.Font, FontStyle.Bold);
            menu.Items.Add(new ToolStripSeparator());
            foreach (var group in Tools.All.GroupBy(t => t.Category))
            {
                var sub = new ToolStripMenuItem(group.Key);
                foreach (Tool t in group)
                {
                    Tool tool = t;
                    sub.DropDownItems.Add(t.Name, null, delegate { Tools.Launch(tool, launcher); });
                }
                menu.Items.Add(sub);
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("New sticky note", null, delegate { StickyNote.Create(); });
            menu.Items.Add("Open my lists folder", null, delegate { try { System.Diagnostics.Process.Start(Store.DataDir); } catch { } });
            var startup = new ToolStripMenuItem("Start with Windows");
            startup.Click += delegate { StartsWithWindows = !StartsWithWindows; };
            var showWindow = new ToolStripMenuItem("Show window when starting");
            showWindow.Click += delegate { Store.Set("start.window", Store.Get("start.window", "0") == "1" ? "0" : "1"); };
            menu.Opening += delegate { startup.Checked = StartsWithWindows; showWindow.Checked = Store.Get("start.window", "0") == "1"; };
            if (Env.IsWindows) menu.Items.Add(startup);
            menu.Items.Add(showWindow);
            menu.Items.Add("About", null, delegate
            {
                MessageBox.Show("Classroom Toolkit " + Application.ProductVersion + "\n\nA free, open-source set of classroom tools.\nCopyright (c) 2026 Luke Harrison.\nReleased under the MIT licence: free to use, copy, change and share.\n\nYour class lists, question sets and settings are kept in:\n" + Store.DataDir +
                    (Store.Portable ? "\n(portable mode: the Data folder beside the program)" : "\n\nTip: create a folder called Data beside the program to keep everything on a USB stick."),
                    "About Classroom Toolkit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { Exit(); });

            Menus.Style(menu);
            tray = new NotifyIcon();
            tray.Icon = AppIcon.Small();
            tray.Text = "Classroom Toolkit";
            tray.ContextMenuStrip = menu;
            tray.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) launcher.Reveal(); };
            tray.BalloonTipClicked += delegate { launcher.Reveal(); };
            tray.Visible = true;

            var remind = new System.Windows.Forms.Timer();
            remind.Interval = 15000;
            remind.Tick += delegate { try { Reminders.Check(); } catch { } };
            remind.Start();

            try { if (File.Exists(Path.Combine(Store.DataDir, "notes.txt"))) StickyNote.Restore(); } catch { }
            if (!startHidden) launcher.Show();
            else
            {
                noticeShown = true;
                try { tray.ShowBalloonTip(5000, "Classroom Toolkit is running", "Click the Classroom Toolkit icon by the clock to open your tools.", ToolTipIcon.Info); } catch { }
            }
            var again = Program.SecondInstanceSignal;
            if (again != null)
            {
                var watcher = new Thread(delegate()
                {
                    while (true)
                    {
                        again.WaitOne();
                        try { launcher.BeginInvoke((Action)delegate { launcher.Reveal(); }); } catch { return; }
                    }
                });
                watcher.IsBackground = true;
                watcher.Start();
            }
            // Touch the handle so BeginInvoke works even when the launcher starts hidden.
            IntPtr unused = launcher.Handle;
            Application.Run();
        }

        static void Exit()
        {
            launcher.Exiting = true;
            tray.Visible = false;
            tray.Dispose();
            Application.Exit();
        }
    }

    static class Program
    {
        public static EventWaitHandle SecondInstanceSignal;

        [STAThread]
        static void Main(string[] args)
        {
            Native.DpiAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
            {
                MessageBox.Show("Something went wrong in Classroom Toolkit, but it will keep running.\n\n" + e.Exception.Message, "Classroom Toolkit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            if (args.Length >= 2 && args[0] == "--selftest") { SelfTest.Run(args[1]); return; }

            try
            {
                bool created;
                SecondInstanceSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "ClassroomToolkit.Show." + Environment.UserName, out created);
                if (!created) { SecondInstanceSignal.Set(); return; }
            }
            catch { SecondInstanceSignal = null; }

            Theme.Init();
            Store.Init();
            Tools.Register();
            // Starts in the notification area unless asked otherwise.
            bool show = args.Contains("--show") || (!args.Contains("--tray") && Store.Get("start.window", "0") == "1");
            TrayApp.Run(!show);
        }
    }

    // Opens every tool once and saves a picture of it. Used when building, to check nothing is broken.
    static class SelfTest
    {
        public static void Run(string outDir)
        {
            Directory.CreateDirectory(outDir);
            Theme.Init();
            Store.Init();
            Tools.Register();
            var log = new List<string>();
            var shots = new List<KeyValuePair<string, Func<Form>>>();
            shots.Add(new KeyValuePair<string, Func<Form>>("Launcher", () => new Launcher()));
            foreach (Tool t in Tools.All)
                if (t.Create != null) shots.Add(new KeyValuePair<string, Func<Form>>(t.Name, t.Create));
            foreach (var shot in shots)
            {
                try
                {
                    Form f = shot.Value();
                    f.StartPosition = FormStartPosition.Manual;
                    if (f.FormBorderStyle != FormBorderStyle.None || f.Width < 1200) f.Location = new Point(0, 0);
                    f.Show();
                    DateTime until = DateTime.UtcNow.AddMilliseconds(700);
                    while (DateTime.UtcNow < until) { Application.DoEvents(); Thread.Sleep(20); }
                    Rectangle b = Rectangle.Intersect(f.Bounds, Screen.PrimaryScreen.Bounds);
                    if (b.Width > 0 && b.Height > 0)
                        using (var bmp = new Bitmap(b.Width, b.Height))
                        {
                            using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(b.Location, Point.Empty, b.Size);
                            bmp.Save(Path.Combine(outDir, shot.Key.Replace(' ', '_') + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    f.Close();
                    Application.DoEvents();
                    log.Add("OK    " + shot.Key);
                }
                catch (Exception ex) { log.Add("FAIL  " + shot.Key + ": " + ex); }
            }
            File.WriteAllLines(Path.Combine(outDir, "selftest.txt"), log.ToArray());
        }
    }
}
