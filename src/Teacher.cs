// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - teacher tools: sticky notes, reminders, exam countdown, name learner,
// report statement builder, special characters and text tidy.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    class StickyNote : Form
    {
        static readonly Color[] Papers = { Color.FromArgb(255, 241, 118), Color.FromArgb(174, 213, 255), Color.FromArgb(185, 246, 202), Color.FromArgb(255, 204, 222), Color.FromArgb(255, 213, 153) };
        public static readonly List<StickyNote> Open = new List<StickyNote>();
        static bool loading;
        readonly TextBox box;
        int paper;

        StickyNote(Rectangle bounds, int paper, string text)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            MinimumSize = new Size(Theme.S(140), Theme.S(100));
            this.paper = paper % Papers.Length;

            var bar = new Panel();
            bar.Dock = DockStyle.Top; bar.Height = Theme.S(30); bar.Cursor = Cursors.SizeAll;
            bar.MouseDown += delegate { Native.DragWindow(this); };
            bar.MouseUp += delegate { SaveAll(); };
            Glyph[] icons = { Glyph.Plus, Glyph.Swap, Glyph.Close };
            string[] tips = { "New note", "Change colour", "Delete this note" };
            EventHandler[] acts = { delegate { Create(); }, delegate { this.paper = (this.paper + 1) % Papers.Length; Apply(); SaveAll(); }, delegate { Delete(); } };
            for (int i = 0; i < icons.Length; i++)
            {
                var b = new NoteButton(icons[i]);
                b.Tip = tips[i];
                b.Dock = DockStyle.Right;
                b.Click += acts[i];
                bar.Controls.Add(b);
            }
            box = new TextBox();
            box.Multiline = true; box.AcceptsReturn = true; box.BorderStyle = BorderStyle.None; box.Dock = DockStyle.Fill;
            box.Font = Theme.Font(13f); box.ForeColor = Color.FromArgb(31, 41, 55);
            box.Text = text;
            box.TextChanged += delegate { SaveAll(); };
            var grip = new Panel();
            grip.Dock = DockStyle.Bottom; grip.Height = Theme.S(16);
            grip.Cursor = Cursors.SizeNWSE;
            grip.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(90, 0, 0, 0), 1.4f))
                    for (int k = 1; k <= 3; k++)
                    {
                        int d = Theme.S(4) * k;
                        e.Graphics.DrawLine(pen, grip.Width - Theme.S(3) - d, grip.Height - Theme.S(3), grip.Width - Theme.S(3), grip.Height - Theme.S(3) - d);
                    }
            };
            Point start = Point.Empty; Size startSize = Size.Empty; bool sizing = false;
            grip.MouseDown += delegate { sizing = true; start = Cursor.Position; startSize = Size; };
            grip.MouseMove += delegate { if (sizing) Size = new Size(startSize.Width + Cursor.Position.X - start.X, startSize.Height + Cursor.Position.Y - start.Y); };
            grip.MouseUp += delegate { sizing = false; SaveAll(); };
            var holder = new Panel();
            holder.Dock = DockStyle.Fill;
            holder.Padding = new Padding(Theme.S(12), Theme.S(4), Theme.S(6), 0);
            holder.Controls.Add(box);
            Controls.Add(holder); Controls.Add(grip); Controls.Add(bar);
            Apply();
            Open.Add(this);
            FormClosed += delegate { Open.Remove(this); };
        }

        void Apply()
        {
            BackColor = Papers[paper];
            box.BackColor = Papers[paper];
            foreach (Control c in Controls) c.BackColor = Papers[paper];
            Controls[Controls.Count - 1].BackColor = Draw.Mix(Papers[paper], Color.Black, 0.12);
        }

        void Delete()
        {
            if (box.Text.Trim().Length > 0 && MessageBox.Show(this, "Delete this note?", "Sticky note", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            Close();
            SaveAll();
        }

        static string FilePath { get { return Path.Combine(Store.DataDir, "notes.txt"); } }

        static void SaveAll()
        {
            if (loading) return;
            var lines = Open.Select(n => string.Format("{0},{1},{2},{3},{4},{5}", n.Left, n.Top, n.Width, n.Height, n.paper,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(n.box.Text)))).ToArray();
            try { File.WriteAllLines(FilePath, lines); } catch { }
        }

        public static void Create()
        {
            var wa = Screen.PrimaryScreen.WorkingArea;
            int k = Open.Count % 8;
            var n = new StickyNote(new Rectangle(wa.Left + Theme.S(60 + k * 30), wa.Top + Theme.S(60 + k * 30), Theme.S(240), Theme.S(200)), Open.Count, "");
            n.Show();
            n.box.Focus();
            SaveAll();
        }

        // Re-opens saved notes. Returns how many there were.
        public static int Restore()
        {
            if (Open.Count > 0) { foreach (var n in Open) { n.Show(); n.Activate(); } return Open.Count; }
            loading = true;
            try
            {
                if (File.Exists(FilePath))
                    foreach (string line in File.ReadAllLines(FilePath))
                    {
                        string[] p = line.Split(',');
                        if (p.Length < 6) continue;
                        var r = new Rectangle(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                        bool visible = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(r));
                        if (!visible) r.Location = Screen.PrimaryScreen.WorkingArea.Location + new Size(40, 40);
                        new StickyNote(r, int.Parse(p[4]), Encoding.UTF8.GetString(Convert.FromBase64String(p[5]))).Show();
                    }
            }
            catch { }
            loading = false;
            return Open.Count;
        }

        public static void ShowOrCreate() { if (Restore() == 0) Create(); }
    }

    // Timed reminders, for example "take the register". Checked by the tray application every few seconds.
    static class Reminders
    {
        public const string Help = "One reminder per line, as a 24-hour time followed by the message, for example:\n08:50 Take the register\nAdd a day to limit it, for example:  Fri 14:45 Hand out letters";
        static readonly HashSet<string> fired = new HashSet<string>();

        public static string Text { get { return Store.Get("reminders.text", "08:50 Take the register\n13:20 Take the afternoon register"); } }
        public static bool Enabled { get { return Store.Get("reminders.on", "0") == "1"; } }

        public class Item { public TimeSpan Time; public string Message; public DayOfWeek? Day; }

        public static List<Item> Parse(string text)
        {
            var list = new List<Item>();
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                DayOfWeek? day = null;
                string[] days = { "sun", "mon", "tue", "wed", "thu", "fri", "sat" };
                for (int i = 0; i < 7; i++)
                    if (line.ToLowerInvariant().StartsWith(days[i]))
                    {
                        int sp = line.IndexOf(' ');
                        if (sp > 0) { day = (DayOfWeek)i; line = line.Substring(sp + 1).Trim(); }
                        break;
                    }
                int space = line.IndexOf(' ');
                string time = space > 0 ? line.Substring(0, space) : line;
                TimeSpan t;
                if (!TimeSpan.TryParseExact(time.Replace('.', ':'), new[] { "h\\:mm", "hh\\:mm" }, CultureInfo.InvariantCulture, out t)) continue;
                list.Add(new Item { Time = t, Day = day, Message = space > 0 ? line.Substring(space + 1).Trim() : "Reminder" });
            }
            return list.OrderBy(i => i.Time).ToList();
        }

        public static void Check()
        {
            if (!Enabled) return;
            DateTime now = DateTime.Now;
            foreach (var item in Parse(Text))
            {
                if (item.Day.HasValue && item.Day.Value != now.DayOfWeek) continue;
                double late = (now.TimeOfDay - item.Time).TotalSeconds;
                string key = now.ToString("yyyyMMdd") + item.Time + item.Message;
                if (late >= 0 && late < 90 && fired.Add(key)) new ReminderAlert(item.Message, item.Time).Show();
            }
        }
    }

    // A small icon button that blends into a sticky note's top strip.
    class NoteButton : SmoothControl
    {
        readonly Glyph glyph;
        public NoteButton(Glyph glyph)
        {
            this.glyph = glyph;
            Size = new Size(Theme.S(30), Theme.S(30));
            Margin = Padding.Empty;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            if (Hot) Draw.FillRound(g, new RectangleF(Theme.S(2), Theme.S(3), Width - Theme.S(4), Height - Theme.S(6)), Theme.S(6), Color.FromArgb(Down ? 60 : 30, 0, 0, 0));
            float ic = Theme.S(14);
            Glyphs.Draw(g, glyph, new RectangleF((Width - ic) / 2, (Height - ic) / 2, ic, ic), Color.FromArgb(150, 0, 0, 0));
        }
    }

    class ReminderAlert : Form
    {
        public ReminderAlert(string message, TimeSpan time)
        {
            Text = "Reminder";
            FormBorderStyle = FormBorderStyle.None; TopMost = true; ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            var wa = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Math.Min(wa.Width - 40, Theme.S(640)), Theme.S(190));
            Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + Theme.S(32));
            BackColor = Theme.Panel;
            using (var path = Draw.Round(new RectangleF(0, 0, Width, Height), Theme.S(18))) Region = new Region(path);
            var view = new Canvas();
            view.Dock = DockStyle.Fill; view.BackColor = Theme.Panel; view.Cursor = Cursors.Hand;
            view.Paint += delegate(object s, PaintEventArgs e)
            {
                var g = e.Graphics;
                Draw.Quality(g);
                var r = view.ClientRectangle;
                g.Clear(Theme.Panel);
                using (var b = new SolidBrush(Theme.Amber)) g.FillRectangle(b, 0, 0, Theme.S(10), r.Height);
                float x = Theme.S(30);
                string when = string.Format("{0:00}:{1:00}", time.Hours, time.Minutes);
                var chip = new RectangleF(x, Theme.S(18), Theme.S(110), Theme.S(28));
                Draw.FillRound(g, chip, chip.Height / 2, Draw.Mix(Theme.Panel, Theme.Amber, 0.3));
                Draw.Label(g, "Reminder  " + when, chip, Draw.Mix(Theme.Amber, Color.White, 0.3), 9.5f, FontStyle.Bold, StringAlignment.Center);
                Draw.Label(g, "Click to dismiss", new RectangleF(r.Width - Theme.S(170), Theme.S(18), Theme.S(150), Theme.S(28)), Theme.Muted, 9.5f, FontStyle.Regular, StringAlignment.Far);
                Draw.Wrapped(g, message, new RectangleF(x, Theme.S(56), r.Width - x - Theme.S(24), r.Height - Theme.S(72)), Color.White, FontStyle.Bold, 44f);
            };
            view.Click += delegate { Close(); };
            Controls.Add(view);
            Shown += delegate { Sound.Change(); };
        }

        protected override bool ShowWithoutActivation { get { return true; } }
    }

    class RemindersForm : ToolForm
    {
        Toggle on;

        public RemindersForm() : base("Register Reminders", 680, 480)
        {
            AddButton("Edit reminders", Glyph.Edit, delegate
            {
                string t = TextEditDialog.Show(this, "Reminders", Reminders.Help, Reminders.Text);
                if (t != null) { Store.Set("reminders.text", t); View.Invalidate(); }
            }, true);
            on = AddCheck("Reminders on", Reminders.Enabled);
            on.CheckedChanged += delegate { Store.Set("reminders.on", on.Checked ? "1" : "0"); View.Invalidate(); };
            AddButton("Test", Glyph.Play, delegate { new ReminderAlert("This is what a reminder looks like", DateTime.Now.TimeOfDay).Show(); }, false).Tip = "Show an example banner";
            AddWindowButtons(false);
            Tick.Interval = 5000; Tick.Start();
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            var items = Reminders.Parse(Reminders.Text);
            float pad = Theme.S(16), row = Theme.S(52), y = pad;
            Draw.Label(g, on.Checked ? "A banner appears at the top of the screen at each time while Classroom Toolkit is running." : "Reminders are off. Switch on \u201CReminders on\u201D to show a banner at each time.",
                new RectangleF(pad, y, r.Width - 2 * pad, Theme.S(24)), Theme.Muted, 10f, FontStyle.Regular, StringAlignment.Near);
            y += Theme.S(36);
            TimeSpan now = DateTime.Now.TimeOfDay;
            int next = -1;
            for (int i = 0; i < items.Count; i++)
            {
                bool today = !items[i].Day.HasValue || items[i].Day.Value == DateTime.Now.DayOfWeek;
                if (today && items[i].Time >= now) { next = i; break; }
            }
            for (int i = 0; i < items.Count && y + row <= r.Height; i++)
            {
                var item = items[i];
                bool today = !item.Day.HasValue || item.Day.Value == DateTime.Now.DayOfWeek;
                bool past = today && item.Time < now;
                bool dim = !on.Checked || past || !today;
                var box = new RectangleF(pad, y, r.Width - 2 * pad, row - Theme.S(8));
                Draw.FillRound(g, box, Theme.S(12), i == next && on.Checked ? Draw.Mix(Theme.Panel, Theme.Amber, 0.16) : Theme.Panel);
                if (i == next && on.Checked) Draw.FillRound(g, new RectangleF(box.X, box.Y + box.Height * 0.2f, Theme.S(4), box.Height * 0.6f), Theme.S(2), Theme.Amber);
                string when = string.Format("{0:00}:{1:00}", item.Time.Hours, item.Time.Minutes);
                Draw.Label(g, when, new RectangleF(box.X + Theme.S(16), box.Y, Theme.S(70), box.Height), dim ? Theme.Muted : Theme.Amber, 15f, FontStyle.Bold, StringAlignment.Near);
                float mx = box.X + Theme.S(92);
                if (item.Day.HasValue)
                {
                    var chip = new RectangleF(mx, box.Y + (box.Height - Theme.S(22)) / 2, Theme.S(44), Theme.S(22));
                    Draw.FillRound(g, chip, chip.Height / 2, Theme.Panel2);
                    Draw.Label(g, item.Day.Value.ToString().Substring(0, 3), chip, Theme.Muted, 9f, FontStyle.Bold, StringAlignment.Center);
                    mx += Theme.S(54);
                }
                Draw.Label(g, item.Message, new RectangleF(mx, box.Y, box.Right - mx - Theme.S(90), box.Height), dim ? Theme.Muted : Theme.Text, 12.5f, FontStyle.Regular, StringAlignment.Near);
                string state = i == next && on.Checked ? "Next" : past ? "Done today" : !today ? "Not today" : "";
                if (state.Length > 0) Draw.Label(g, state, new RectangleF(box.Right - Theme.S(110), box.Y, Theme.S(96), box.Height), i == next ? Theme.Amber : Theme.Muted, 9.5f, FontStyle.Bold, StringAlignment.Far);
                y += row;
            }
            if (items.Count == 0) Draw.Label(g, "No reminders yet. Click Edit reminders to add some.", new RectangleF(0, y, r.Width, row), Theme.Muted, 12f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    class ExamCountdownForm : ToolForm
    {
        const string Help = "One date per line, as day/month/year followed by the name, for example:\n12/05/2027 GCSE Maths Paper 1";
        class Item { public DateTime Date; public string Name; }

        public ExamCountdownForm() : base("Exam Countdown", 820, 520)
        {
            AddButton("Edit dates", Glyph.Edit, delegate
            {
                string t = TextEditDialog.Show(this, "Exam and deadline dates", Help, Store.Get("examdates.text", Starter()));
                if (t != null) { Store.Set("examdates.text", t); View.Invalidate(); }
            }, true);
            AddWindowButtons(false);
            Tick.Interval = 60000; Tick.Start();
        }

        static string Starter()
        {
            DateTime d = DateTime.Today;
            return d.AddDays(21).ToString("dd/MM/yyyy") + " Example: end of unit test\r\n" + d.AddDays(75).ToString("dd/MM/yyyy") + " Example: coursework deadline";
        }

        static List<Item> Parse(string text)
        {
            var list = new List<Item>();
            var uk = CultureInfo.GetCultureInfo("en-GB");
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim();
                int sp = line.IndexOf(' ');
                DateTime d;
                string first = sp > 0 ? line.Substring(0, sp) : line;
                if (!DateTime.TryParse(first, uk, DateTimeStyles.None, out d)) continue;
                list.Add(new Item { Date = d.Date, Name = sp > 0 ? line.Substring(sp + 1).Trim() : "Exam" });
            }
            return list.Where(i => i.Date >= DateTime.Today).OrderBy(i => i.Date).ToList();
        }

        static int SchoolDays(DateTime to)
        {
            int n = 0;
            for (DateTime d = DateTime.Today; d < to; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) n++;
            return n;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            var items = Parse(Store.Get("examdates.text", Starter()));
            if (items.Count == 0) { Draw.Wrapped(g, "No upcoming dates. Press Edit dates to add some.", Draw.Inset(r, r.Width * 0.1f), Theme.Muted, FontStyle.Regular, 26f); return; }
            Item next = items[0];
            int days = (next.Date - DateTime.Today).Days;
            float pad = Math.Max(Theme.S(12), r.Height * 0.03f);
            float topH = (r.Height - 2 * pad) * (items.Count > 1 ? 0.6f : 1f);
            var hero = new RectangleF(pad, pad, r.Width - 2 * pad, topH);
            Color tint = days <= 7 ? Theme.Red : days <= 30 ? Theme.Amber : Theme.Green;
            Draw.FillRound(g, hero, Theme.S(22), Draw.Mix(Theme.Panel, tint, 0.2));
            Draw.FillRound(g, new RectangleF(hero.X, hero.Y, Theme.S(8), hero.Height), Theme.S(4), tint);
            var left = new RectangleF(hero.X + hero.Width * 0.03f, hero.Y + hero.Height * 0.06f, hero.Width * 0.38f, hero.Height * 0.62f);
            Draw.Text(g, days == 0 ? "Today" : days.ToString(), left, Color.White, FontStyle.Bold, false, 500f, null);
            Draw.Text(g, days == 0 ? "" : days == 1 ? "day to go" : "days to go", new RectangleF(left.X, left.Bottom, left.Width, hero.Height * 0.2f), Draw.Mix(tint, Color.White, 0.4), FontStyle.Bold, false, 40f, null);
            var right = new RectangleF(hero.X + hero.Width * 0.45f, hero.Y + hero.Height * 0.12f, hero.Width * 0.5f, hero.Height * 0.46f);
            Draw.Wrapped(g, next.Name, right, Color.White, FontStyle.Bold, 64f);
            Draw.Text(g, next.Date.ToString("dddd d MMMM yyyy"), new RectangleF(right.X, hero.Y + hero.Height * 0.62f, right.Width, hero.Height * 0.12f), Theme.Text, FontStyle.Regular, false, 26f, null);
            Draw.Text(g, SchoolDays(next.Date) + " weekdays  ·  " + (days / 7) + " weeks", new RectangleF(right.X, hero.Y + hero.Height * 0.75f, right.Width, hero.Height * 0.1f), Theme.Muted, FontStyle.Regular, false, 22f, null);
            float y = hero.Bottom + pad, row = Math.Max(Theme.S(40), Math.Min(Theme.S(54), (r.Height - hero.Bottom - 2 * pad) / Math.Max(1, Math.Min(6, items.Count - 1))));
            for (int i = 1; i < items.Count && y + row <= r.Height; i++, y += row)
            {
                var box = new RectangleF(pad, y, r.Width - 2 * pad, row - Theme.S(8));
                Draw.FillRound(g, box, Theme.S(12), Theme.Panel);
                int d = (items[i].Date - DateTime.Today).Days;
                Color c = d <= 7 ? Theme.Red : d <= 30 ? Theme.Amber : Theme.Green;
                Draw.Label(g, d + (d == 1 ? " day" : " days"), new RectangleF(box.X + Theme.S(16), box.Y, Theme.S(110), box.Height), c, 13f, FontStyle.Bold, StringAlignment.Near);
                Draw.Label(g, items[i].Name, new RectangleF(box.X + Theme.S(130), box.Y, box.Width - Theme.S(330), box.Height), Theme.Text, 12.5f, FontStyle.Regular, StringAlignment.Near);
                Draw.Label(g, items[i].Date.ToString("ddd d MMM yyyy"), new RectangleF(box.Right - Theme.S(200), box.Y, Theme.S(184), box.Height), Theme.Muted, 11.5f, FontStyle.Regular, StringAlignment.Far);
            }
        }
    }

    // Shows student photos from a folder (file name = student name) to help learn a new class.
    class NameLearnerForm : ToolForm
    {
        List<string> files = new List<string>();
        readonly List<string> again = new List<string>();
        int index, known;
        bool revealed;
        Image photo;
        DisplayButtons buttons;

        public NameLearnerForm() : base("Learn Student Names", 820, 640)
        {
            AddButton("Choose photo folder", Glyph.Folder, delegate { Choose(); }, false);
            AddButton("Start again", Glyph.Reset, delegate { LoadFolder(Store.Get("names.folder", "")); }, false);
            AddLabel("Space reveal  \u00B7  Y knew it  \u00B7  N not yet");
            AddWindowButtons(false);
            buttons = new DisplayButtons(View);
            LoadFolder(Store.Get("names.folder", ""));
            FormClosed += delegate { if (photo != null) photo.Dispose(); };
        }

        void Choose()
        {
            using (var d = new FolderBrowserDialog())
            {
                d.Description = "Choose a folder of student photos. Each file should be named after the student, for example 'Sam Jones.jpg'.";
                d.SelectedPath = Store.Get("names.folder", "");
                if (d.ShowDialog(this) != DialogResult.OK) return;
                Store.Set("names.folder", d.SelectedPath);
                LoadFolder(d.SelectedPath);
            }
        }

        void LoadFolder(string dir)
        {
            files.Clear(); again.Clear(); index = 0; known = 0; revealed = false;
            try
            {
                if (Directory.Exists(dir))
                    files = Directory.GetFiles(dir).Where(f => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
            }
            catch { }
            Store.Shuffle(files);
            LoadPhoto();
        }

        void LoadPhoto()
        {
            if (photo != null) { photo.Dispose(); photo = null; }
            if (index < files.Count)
            {
                try { using (var fs = new FileStream(files[index], FileMode.Open, FileAccess.Read)) using (var img = Image.FromStream(fs)) photo = new Bitmap(img); }
                catch { photo = null; }
            }
            View.Invalidate();
        }

        void Reveal() { revealed = true; View.Invalidate(); }

        void Mark(bool knew)
        {
            if (index >= files.Count) return;
            if (!revealed) { Reveal(); return; }
            if (knew) known++; else again.Add(files[index]);
            index++; revealed = false;
            if (index >= files.Count && again.Count > 0)
            {
                files = new List<string>(again); again.Clear(); index = 0;
                Store.Shuffle(files);
            }
            LoadPhoto();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { Reveal(); return true; }
            if (key == Keys.Y) { Mark(true); return true; }
            if (key == Keys.N) { Mark(false); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            buttons.Clear();
            float w = r.Width, h = r.Height;
            float pad = Math.Max(Theme.S(14), h * 0.035f);
            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(76), h * 0.1f));
            if (files.Count == 0 && known == 0)
            {
                Draw.Wrapped(g, "Choose a folder of student photos.\nName each picture after the student, for example \"Sam Jones.jpg\".", Draw.Inset(r, w * 0.1f), Theme.Muted, FontStyle.Regular, 24f);
                float bw = btnH * 4.6f;
                buttons.Add(new RectangleF((w - bw) / 2, h - pad - btnH, bw, btnH), Glyph.Folder, "Choose photo folder", true, Choose);
                buttons.Paint(g);
                return;
            }
            int total = known + (files.Count - index) + again.Count;
            float barH = Math.Max(Theme.S(6), h * 0.012f);
            using (var b = new SolidBrush(Theme.Panel)) g.FillRectangle(b, 0, h - barH, w, barH);
            using (var b = new SolidBrush(Theme.Green)) g.FillRectangle(b, 0, h - barH, total == 0 ? w : w * known / (float)total, barH);
            if (index >= files.Count)
            {
                Draw.Wrapped(g, "All done - you know everyone!", Draw.Inset(r, w * 0.1f), Theme.Green, FontStyle.Bold, 60f);
                return;
            }
            float by = h - pad - btnH - barH;
            float nameH = Math.Max(Theme.S(40), h * 0.11f);
            float infoH = Theme.S(22);
            var area = new RectangleF(pad, pad, w - 2 * pad, by - pad * 1.6f - nameH - infoH);
            if (photo != null)
            {
                float s = Math.Min(area.Width / photo.Width, area.Height / photo.Height);
                float pw = photo.Width * s, ph = photo.Height * s;
                var pr = new RectangleF(area.X + (area.Width - pw) / 2, area.Y + (area.Height - ph) / 2, pw, ph);
                using (var path = Draw.Round(pr, Theme.S(18)))
                {
                    g.SetClip(path);
                    g.DrawImage(photo, pr);
                    g.ResetClip();
                }
            }
            else Draw.Text(g, "(picture could not be opened)", area, Theme.Muted, FontStyle.Regular, false, 24f, null);
            var nameBox = new RectangleF(pad, area.Bottom + pad * 0.6f, w - 2 * pad, nameH);
            if (revealed) Draw.Text(g, Path.GetFileNameWithoutExtension(files[index]), nameBox, Theme.Text, FontStyle.Bold, false, 80f, null);
            else Draw.Text(g, "Who is this?", nameBox, Theme.Muted, FontStyle.Regular, false, 40f, null);
            Draw.Label(g, (files.Count - index) + " to go  ·  " + known + " known  ·  " + again.Count + " to try again", new RectangleF(0, nameBox.Bottom, w, infoH), Theme.Muted, 10.5f, FontStyle.Regular, StringAlignment.Center);
            if (!revealed)
            {
                float bw = btnH * 3.4f;
                buttons.Add(new RectangleF((w - bw) / 2, by, bw, btnH), Glyph.Swap, "Reveal", true, Reveal);
            }
            else
            {
                float bw = btnH * 3.2f, gap = btnH * 0.4f, x = (w - (2 * bw + gap)) / 2;
                buttons.Add(new RectangleF(x, by, bw, btnH), Glyph.Close, "Not yet", Theme.Red, delegate { Mark(false); });
                buttons.Add(new RectangleF(x + bw + gap, by, bw, btnH), Glyph.Check, "Knew it", Color.FromArgb(22, 163, 74), delegate { Mark(true); });
            }
            buttons.Paint(g);
        }
    }

    // Scrolling list of statement cards. Each card shows the statement as it will read in the report;
    // chosen ones are numbered in the order they were picked.
    class StatementList : Panel
    {
        public class Entry { public string Heading; public string Text; public RectangleF Rect; }
        public readonly List<Entry> Entries = new List<Entry>();
        public Func<string, string> Preview = s => s;
        public List<string> Chosen = new List<string>();
        public event Action<string> Toggled;
        int hover = -1;

        public StatementList()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            AutoScroll = true;
            BackColor = Theme.Bg;
        }

        protected override void OnScroll(ScrollEventArgs se) { base.OnScroll(se); Invalidate(); }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); if (!Focused && FindForm() != null && FindForm().ContainsFocus && !(FindForm().ActiveControl is TextBoxBase)) Focus(); }

        int HitTest(Point p)
        {
            p.Offset(-AutoScrollPosition.X, -AutoScrollPosition.Y);
            for (int i = 0; i < Entries.Count; i++) if (Entries[i].Text != null && Entries[i].Rect.Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (h != hover) { hover = h; Cursor = h >= 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int h = HitTest(e.Location);
            if (h >= 0 && Toggled != null) Toggled(Entries[h].Text);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Draw.Quality(g);
            g.Clear(BackColor);
            g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
            float pad = Theme.S(16), y = Theme.S(10);
            float width = ClientSize.Width - 2 * pad;
            Font body = Theme.Font(11.5f), head = Theme.Semi(10.5f);
            float circle = Theme.S(26);
            float textX = pad + Theme.S(14) + circle + Theme.S(12);
            float textW = Math.Max(40, width - (textX - pad) - Theme.S(14));
            using (var ink = new SolidBrush(Theme.Text))
            using (var muted = new SolidBrush(Theme.Muted))
            using (var accent = new SolidBrush(Theme.Accent))
                for (int i = 0; i < Entries.Count; i++)
                {
                    Entry en = Entries[i];
                    if (en.Text == null)
                    {
                        y += i == 0 ? 0 : Theme.S(10);
                        g.DrawString(en.Heading.ToUpperInvariant(), head, accent, pad + Theme.S(4), y);
                        en.Rect = RectangleF.Empty;
                        y += head.GetHeight(g) + Theme.S(8);
                        continue;
                    }
                    string text = Preview(en.Text);
                    float th = g.MeasureString(text, body, (int)textW).Height;
                    float h = Math.Max(circle + Theme.S(16), th + Theme.S(20));
                    en.Rect = new RectangleF(pad, y, width, h);
                    int order = Chosen.IndexOf(en.Text);
                    Color fill = order >= 0 ? Draw.Mix(Theme.Panel, Theme.Accent, 0.28) : i == hover ? Theme.Panel2 : Theme.Panel;
                    Draw.FillRound(g, en.Rect, Theme.S(12), fill);
                    var c = new RectangleF(pad + Theme.S(14), y + (h - circle) / 2, circle, circle);
                    if (order >= 0)
                    {
                        using (var b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, c);
                        Draw.Label(g, (order + 1).ToString(), c, Color.White, 10f, FontStyle.Bold, StringAlignment.Center);
                    }
                    else using (var pen = new Pen(Theme.Line, Theme.S(2))) g.DrawEllipse(pen, Draw.Inset(c, Theme.S(1)));
                    g.DrawString(text, body, order >= 0 || i == hover ? ink : (Brush)ink, new RectangleF(textX, y + (h - th) / 2, textW, th + 2));
                    y += h + Theme.S(6);
                }
            var min = new Size(0, (int)(y + Theme.S(10)));
            if (AutoScrollMinSize != min) AutoScrollMinSize = min;
        }
    }

    class StatementForm : BaseForm
    {
        readonly FileChooser chooser;
        readonly TextBox name, subject, output;
        readonly Choice pronoun;
        readonly StatementList list;
        readonly Label count;
        readonly List<string> chosen = new List<string>();

        public StatementForm()
        {
            Text = "Report Statement Builder";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(1040), Theme.S(720));
            MinimumSize = new Size(Theme.S(600), Theme.S(420));

            var bar = new ToolBarPanel();
            bar.Dock = DockStyle.Top;
            chooser = new FileChooser(this, bar.Controls, "Statements", "statement bank", Samples.StatementHelp);
            bar.Controls.Add(new Divider());
            bar.Controls.Add(Lbl("Student"));
            var nf = new TextField("Sam", 130); name = nf.Box; bar.Controls.Add(nf);
            bar.Controls.Add(Lbl("Subject"));
            var sf = new TextField(Store.Get("statements.subject", "Science"), 118); subject = sf.Box; bar.Controls.Add(sf);
            pronoun = new Choice(118);
            pronoun.Items.AddRange(new[] { "he / him", "she / her", "they / them" });
            pronoun.SelectedIndex = Math.Max(0, Math.Min(2, Store.GetInt("statements.pronoun", 2)));
            pronoun.Tip = "Pronouns used in the report";
            bar.Controls.Add(pronoun);
            var next = new FlatButton("Next student", Glyph.Next, false);
            next.Tip = "Clear the name and choices, ready for the next student";
            next.Click += delegate { name.Text = ""; ClearChecks(); name.Focus(); };
            bar.AddRight(next);

            list = new StatementList();
            list.Dock = DockStyle.Fill;
            list.Chosen = chosen;
            list.Preview = Fill;
            list.Toggled += delegate(string s) { ToggleStatement(s); };

            // Report area at the bottom
            var foot = new Panel();
            foot.Dock = DockStyle.Bottom; foot.Height = Theme.S(220); foot.BackColor = Theme.Panel;
            foot.Padding = new Padding(Theme.S(16), Theme.S(10), Theme.S(16), Theme.S(14));
            var head = new ToolBarPanel();
            head.Dock = DockStyle.Top;
            head.Padding = new Padding(0, 0, 0, Theme.S(6));
            var title = new Label(); title.Text = "Report"; title.AutoSize = true; title.ForeColor = Theme.Text; title.Font = Theme.Semi(12f); title.Margin = new Padding(0, 0, Theme.S(10), 0);
            head.Controls.Add(title);
            count = new Label(); count.AutoSize = true; count.ForeColor = Theme.Muted; count.Font = Theme.Font(9.5f);
            head.Controls.Add(count);
            var clear = new FlatButton("Clear", Glyph.Trash, false);
            clear.Click += delegate { ClearChecks(); };
            head.AddRight(clear);
            var copy = new FlatButton("Copy report", Glyph.Copy, true);
            copy.Click += delegate { if (output.Text.Length > 0) { Clipboard.SetText(output.Text); copy.Text = "Copied"; var t2 = new Timer(); t2.Interval = 1500; t2.Tick += delegate { copy.Text = "Copy report"; t2.Dispose(); }; t2.Start(); } };
            head.AddRight(copy);
            var holder = new Panel();
            holder.Dock = DockStyle.Fill; holder.BackColor = Theme.Panel2; holder.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(6), Theme.S(8));
            output = new TextBox();
            output.Multiline = true; output.ScrollBars = ScrollBars.Vertical; output.Dock = DockStyle.Fill; output.BorderStyle = BorderStyle.None;
            output.Font = Theme.Font(12f); output.BackColor = Theme.Panel2; output.ForeColor = Color.White;
            output.TextChanged += delegate { UpdateCount(); };
            holder.Controls.Add(output);
            foot.Controls.Add(holder); foot.Controls.Add(head);

            Controls.Add(list); Controls.Add(foot); Controls.Add(bar);
            EventHandler redo = delegate { Store.Set("statements.subject", subject.Text); Store.Set("statements.pronoun", pronoun.SelectedIndex); Compose(); list.Invalidate(); };
            name.TextChanged += redo; subject.TextChanged += redo; pronoun.SelectedIndexChanged += redo;
            chooser.Changed += delegate { LoadBank(); };
            LoadBank();
        }

        static Label Lbl(string t)
        {
            var l = new Label();
            l.Text = t; l.AutoSize = true; l.ForeColor = Theme.Muted; l.Font = Theme.Font(9.5f); l.Margin = new Padding(Theme.S(8), 0, Theme.S(2), 0);
            return l;
        }

        void UpdateCount()
        {
            int words = output.Text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            count.Text = chosen.Count + (chosen.Count == 1 ? " statement" : " statements") + "  ·  " + words + " words  ·  " + output.TextLength + " characters";
        }

        void LoadBank()
        {
            list.Entries.Clear(); chosen.Clear();
            bool any = false;
            foreach (string raw in Store.Lines(chooser.Path ?? ""))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#")) { list.Entries.Add(new StatementList.Entry { Heading = line.TrimStart('#', ' ') }); continue; }
                if (!any && (list.Entries.Count == 0)) list.Entries.Add(new StatementList.Entry { Heading = "Statements" });
                any = true;
                list.Entries.Add(new StatementList.Entry { Text = line });
            }
            list.AutoScrollPosition = Point.Empty;
            list.Invalidate();
            Compose();
        }

        public void ToggleStatement(string s)
        {
            if (chosen.Contains(s)) chosen.Remove(s); else chosen.Add(s);
            list.Invalidate();
            Compose();
        }

        void ClearChecks()
        {
            chosen.Clear();
            list.Invalidate();
            Compose();
        }

        // A statement as it will read in the report.
        string Fill(string s)
        {
            string[][] forms =
            {
                new[] { "he", "him", "his", "himself" },
                new[] { "she", "her", "her", "herself" },
                new[] { "they", "them", "their", "themselves" }
            };
            string[] p = forms[Math.Max(0, pronoun.SelectedIndex)];
            string who = name.Text.Trim().Length == 0 ? "[name]" : name.Text.Trim();
            string t = s.Replace("{name}", who).Replace("{Name}", who).Replace("~Name~", who)
                .Replace("{subject}", subject.Text.Trim()).Replace("{Subject}", subject.Text.Trim()).Replace("~Subject~", subject.Text.Trim())
                .Replace("{he}", p[0]).Replace("{He}", Cap(p[0])).Replace("{him}", p[1]).Replace("{Him}", Cap(p[1]))
                .Replace("{his}", p[2]).Replace("{His}", Cap(p[2])).Replace("{himself}", p[3]);
            return Verbs(t, pronoun.SelectedIndex == 2).Trim();
        }

        void Compose()
        {
            var sb = new StringBuilder();
            foreach (string s in chosen) sb.Append(Fill(s)).Append(' ');
            output.Text = sb.ToString().Trim();
            UpdateCount();
        }

        static string Cap(string s) { return char.ToUpper(s[0]) + s.Substring(1); }

        // {is|are} style choices: the first form goes with he or she, the second with they.
        static string Verbs(string t, bool plural)
        {
            var sb = new StringBuilder();
            int i = 0;
            while (i < t.Length)
            {
                int open = t.IndexOf('{', i);
                int close = open < 0 ? -1 : t.IndexOf('}', open);
                int bar = open < 0 || close < 0 ? -1 : t.IndexOf('|', open, close - open);
                if (bar < 0) { if (open < 0 || close < 0) { sb.Append(t.Substring(i)); break; } sb.Append(t.Substring(i, close + 1 - i)); i = close + 1; continue; }
                sb.Append(t.Substring(i, open - i));
                sb.Append(plural ? t.Substring(bar + 1, close - bar - 1) : t.Substring(open + 1, bar - open - 1));
                i = close + 1;
            }
            return sb.ToString();
        }
    }

    class CharactersForm : BaseForm
    {
        readonly TextBox collected;

        static readonly string[][] Sets =
        {
            new[] { "Maths", "× ÷ ± ≠ ≈ ≤ ≥ √ ∛ ∞ π ° ² ³ ⁿ ½ ¼ ¾ ⅓ ⅔ ‰ ∑ ∫ ∆ ∠ ⊥ ∥ ∴ ∵ ∝ ∈ ∩ ∪ ⊂ ∅ → ⇒ ⇌ ≡ µ" },
            new[] { "Greek", "α β γ δ ε ζ η θ ι κ λ μ ν ξ ο π ρ σ τ υ φ χ ψ ω Α Β Γ Δ Θ Λ Π Σ Φ Ψ Ω" },
            new[] { "Accents", "à á â ä ã å æ ç è é ê ë ì í î ï ñ ò ó ô ö õ ø œ ß ù ú û ü ÿ À Á Â Ä Ç È É Ê Ë Î Ï Ñ Ó Ô Ö Ù Ú Û Ü ¿ ¡ « »" },
            new[] { "Symbols", "✓ ✗ ★ ☆ • ◦ ■ □ ▲ ▼ ◆ ← ↑ → ↓ ↔ £ € ¢ ¥ © ® ™ § ¶ † … – — ‘ ’ “ ” ♀ ♂ ♪ ☺ ☹ ☀ ☂ ❤" },
            new[] { "Science", "→ ⇌ ↑ ↓ °C Δ λ Ω µ ρ α β γ ₀ ₁ ₂ ₃ ₄ ₅ ₆ ₇ ₈ ₉ ⁺ ⁻ ² ³ ⁰ ¹ ⁴ ⁵ ⁶ Å ℏ ≈ ∝ CO₂ H₂O O₂ m/s² kg/m³" }
        };

        public CharactersForm()
        {
            Text = "Special Characters";
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(700), Theme.S(500));

            var bar = new ToolBarPanel();
            bar.Dock = DockStyle.Top;
            var grid = new FlowLayoutPanel();
            grid.Dock = DockStyle.Fill; grid.AutoScroll = true; grid.Padding = new Padding(Theme.S(12)); grid.BackColor = Theme.Bg;
            var tabs = new List<FlatButton>();
            Action<int> showSet = null;
            showSet = delegate(int k)
            {
                for (int i = 0; i < tabs.Count; i++) tabs[i].Toggled = i == k;
                grid.SuspendLayout();
                foreach (Control old in grid.Controls.Cast<Control>().ToArray()) old.Dispose();
                grid.Controls.Clear();
                foreach (string ch in Sets[k][1].Split(' '))
                {
                    var tile = new CharTile(ch);
                    tile.Click += delegate { collected.Text += tile.Value; try { Clipboard.SetText(tile.Value); } catch { } tile.Flash(); };
                    grid.Controls.Add(tile);
                }
                grid.ResumeLayout();
                Store.Set("chars.set", k);
            };
            for (int i = 0; i < Sets.Length; i++)
            {
                int k = i;
                var b = new FlatButton(Sets[i][0], Glyph.None, false);
                b.Click += delegate { showSet(k); };
                tabs.Add(b);
                bar.Controls.Add(b);
            }
            var pin = new FlatButton("", Glyph.Pin, false);
            pin.Tip = "Keep on top of other windows"; pin.Toggled = true;
            pin.Click += delegate { TopMost = !TopMost; pin.Toggled = TopMost; };
            bar.AddRight(pin);

            var foot = new ToolBarPanel();
            foot.Dock = DockStyle.Bottom;
            var hint = new Label();
            hint.Text = "Click to copy  ·  collected:"; hint.AutoSize = true; hint.ForeColor = Theme.Muted; hint.Font = Theme.Font(9.5f); hint.Margin = new Padding(Theme.S(4), 0, Theme.S(4), 0);
            var field = new TextField("", 260);
            collected = field.Box;
            collected.Font = Theme.Font(14f);
            var copy = new FlatButton("Copy all", Glyph.Copy, true);
            copy.Click += delegate { if (collected.Text.Length > 0) Clipboard.SetText(collected.Text); };
            var clear = new FlatButton("", Glyph.Trash, false);
            clear.Tip = "Clear the collected characters";
            clear.Click += delegate { collected.Text = ""; };
            foot.Controls.AddRange(new Control[] { hint, field, copy, clear });

            Controls.Add(grid); Controls.Add(foot); Controls.Add(bar);
            showSet(Math.Max(0, Math.Min(Sets.Length - 1, Store.GetInt("chars.set", 0))));
        }
    }

    // One character on a large tile; shows "Copied" briefly after a click.
    class CharTile : SmoothControl
    {
        public readonly string Value;
        DateTime flashUntil;

        public CharTile(string value)
        {
            Value = value;
            int w = value.Length > 2 ? 92 : 56;
            Size = new Size(Theme.S(w), Theme.S(56));
            Margin = new Padding(Theme.S(4));
        }

        public void Flash()
        {
            flashUntil = DateTime.Now.AddMilliseconds(700);
            Invalidate();
            var t = new Timer(); t.Interval = 750;
            t.Tick += delegate { t.Dispose(); if (!IsDisposed) Invalidate(); };
            t.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            bool flash = DateTime.Now < flashUntil;
            Draw.FillRound(g, new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Theme.S(10), flash ? Color.FromArgb(22, 163, 74) : Surface(Theme.Panel));
            if (flash) { TextIn(g, "Copied", Theme.Semi(9f), ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter); return; }
            Font f = Theme.Font(Value.Length > 2 ? 13f : 19f);
            TextIn(g, Value, f, ClientRectangle, Theme.Text, TextFormatFlags.HorizontalCenter);
        }
    }

    class TextTidyForm : BaseForm
    {
        readonly TextBox box;
        readonly Label status;

        readonly Stack<string> undo = new Stack<string>();
        FlatButton undoBtn;

        public TextTidyForm()
        {
            Text = "Text Tidy";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(980), Theme.S(680));

            var bar = new ToolBarPanel();
            bar.Dock = DockStyle.Top;
            var paste = new FlatButton("Paste", Glyph.Folder, true);
            paste.Tip = "Replace the text with what's on the clipboard";
            paste.Click += delegate { if (Clipboard.ContainsText()) Apply(t => Clipboard.GetText()); };
            var copy = new FlatButton("Copy", Glyph.Copy, true);
            copy.Click += delegate { if (box.TextLength > 0) { Clipboard.SetText(box.Text); status.Text = "Copied to the clipboard"; } };
            undoBtn = new FlatButton("Undo", Glyph.Undo, false);
            undoBtn.Enabled = false;
            undoBtn.Click += delegate { if (undo.Count > 0) { box.Text = undo.Pop(); undoBtn.Enabled = undo.Count > 0; } };
            var clear = new FlatButton("", Glyph.Trash, false);
            clear.Tip = "Clear the text";
            clear.Click += delegate { Apply(t => ""); };
            bar.Controls.AddRange(new Control[] { paste, copy, new Divider(), undoBtn, clear });

            var holder = new Panel();
            holder.Dock = DockStyle.Fill; holder.BackColor = Theme.Panel2; holder.Padding = new Padding(Theme.S(14), Theme.S(10), Theme.S(4), Theme.S(4));
            box = new TextBox();
            box.Multiline = true; box.ScrollBars = ScrollBars.Both; box.WordWrap = false; box.AcceptsReturn = true; box.AcceptsTab = true;
            box.Dock = DockStyle.Fill; box.Font = Theme.Font(12f); box.MaxLength = 0; box.BorderStyle = BorderStyle.None;
            box.BackColor = Theme.Panel2; box.ForeColor = Color.White;
            holder.Controls.Add(box);

            var side = new FlowLayoutPanel();
            side.Dock = DockStyle.Left; side.Width = Theme.S(236); side.FlowDirection = FlowDirection.TopDown; side.WrapContents = false;
            side.AutoScroll = true; side.BackColor = Theme.Panel; side.Padding = new Padding(Theme.S(10), Theme.S(4), Theme.S(6), Theme.S(10));
            Action<string> heading = delegate(string h)
            {
                var l = new Label();
                l.Text = h.ToUpperInvariant(); l.AutoSize = true; l.ForeColor = Theme.Muted; l.Font = Theme.Semi(8.5f);
                l.Margin = new Padding(Theme.S(4), Theme.S(12), 0, Theme.S(4));
                side.Controls.Add(l);
            };
            Action<string, Func<string, string>> add = delegate(string label, Func<string, string> f)
            {
                var b = new FlatButton(label, Glyph.None, false);
                b.AutoSize = false; b.Size = new Size(Theme.S(208), Theme.S(34)); b.TextAlign = ContentAlignment.MiddleLeft;
                b.Font = Theme.Font(10f);
                b.Margin = new Padding(0, Theme.S(1), 0, Theme.S(1));
                b.Click += delegate { Apply(f); };
                side.Controls.Add(b);
            };
            heading("Case");
            add("UPPER CASE", t => t.ToUpper());
            add("lower case", t => t.ToLower());
            add("Title Case", t => CultureInfo.GetCultureInfo("en-GB").TextInfo.ToTitleCase(t.ToLower()));
            add("Sentence case", Sentence);
            heading("Sort");
            add("A to Z", t => Each(t, l => l.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)));
            add("Z to A", t => Each(t, l => l.OrderByDescending(x => x, StringComparer.CurrentCultureIgnoreCase)));
            add("By surname", t => Each(t, l => l.OrderBy(x => x.Trim().Split(' ').Last(), StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x)));
            add("As numbers", t => Each(t, l => l.OrderBy(x => Num(x))));
            add("Shuffle", t => Each(t, l => { var a = l.ToList(); Store.Shuffle(a); return a; }));
            heading("Clean up");
            add("Remove duplicate lines", t => Each(t, l => l.Distinct(StringComparer.CurrentCultureIgnoreCase)));
            add("Remove blank lines", t => Each(t, l => l.Where(x => x.Trim().Length > 0)));
            add("Trim extra spaces", t => Each(t, l => l.Select(x => string.Join(" ", x.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)))));
            heading("Names and numbers");
            add("Swap first and last name", t => Each(t, l => l.Select(Swap)));
            add("Number the lines", t => { int n = 0; return Each(t, l => l.Select(x => (++n) + ". " + x)); });
            add("Sum and average", t => { Stats(t); return t; });

            status = new Label();
            status.Dock = DockStyle.Bottom; status.Height = Theme.S(30); status.ForeColor = Theme.Muted; status.BackColor = Theme.Panel;
            status.Font = Theme.Font(9.5f); status.Padding = new Padding(Theme.S(12), Theme.S(7), 0, 0);
            box.TextChanged += delegate
            {
                string t = box.Text;
                int words = t.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                status.Text = (t.Length == 0 ? 0 : t.Replace("\r\n", "\n").Split('\n').Length) + " lines  ·  " + words + " words  ·  " + t.Length + " characters";
            };
            Controls.Add(holder); Controls.Add(side); Controls.Add(status); Controls.Add(bar);
            box.Text = "";
            status.Text = "Paste a list or some text, then pick a change on the left";
        }

        // Applies a change, remembering the old text for Undo.
        void Apply(Func<string, string> f)
        {
            try
            {
                string before = box.Text;
                string after = f(before);
                if (after == before) return;
                undo.Push(before);
                undoBtn.Enabled = true;
                box.Text = after;
            }
            catch (Exception ex) { status.Text = ex.Message; }
        }

        static string Each(string text, Func<IEnumerable<string>, IEnumerable<string>> f)
        {
            return string.Join("\r\n", f(text.Replace("\r\n", "\n").Split('\n')).ToArray());
        }

        static double Num(string s)
        {
            double v;
            var digits = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            return double.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : double.MaxValue;
        }

        static string Swap(string line)
        {
            string l = line.Trim();
            if (l.Contains(",")) { var p = l.Split(new[] { ',' }, 2); return p[1].Trim() + " " + p[0].Trim(); }
            int i = l.LastIndexOf(' ');
            return i < 0 ? l : l.Substring(i + 1) + ", " + l.Substring(0, i);
        }

        static string Sentence(string t)
        {
            var sb = new StringBuilder(t.ToLower());
            bool start = true;
            for (int i = 0; i < sb.Length; i++)
            {
                if (start && char.IsLetter(sb[i])) { sb[i] = char.ToUpper(sb[i]); start = false; }
                else if (sb[i] == '.' || sb[i] == '!' || sb[i] == '?' || sb[i] == '\n') start = true;
            }
            return sb.ToString();
        }

        void Stats(string t)
        {
            var nums = t.Split(new[] { ' ', '\r', '\n', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => { double v; return double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? (double?)v : null; })
                .Where(v => v.HasValue).Select(v => v.Value).ToList();
            if (nums.Count == 0) { status.Text = "No numbers found"; return; }
            status.Text = string.Format("Count {0}   -   Sum {1:0.###}   -   Average {2:0.###}   -   Lowest {3:0.###}   -   Highest {4:0.###}", nums.Count, nums.Sum(), nums.Average(), nums.Min(), nums.Max());
        }
    }
}
