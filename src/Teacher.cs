// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - teacher tools: sticky notes, reminders, exam countdown, name learner,
// report statement builder, special characters and text tidy.
using System;
using System.Collections.Generic;
using System.Drawing;
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
            bar.Dock = DockStyle.Top; bar.Height = Theme.S(28); bar.Cursor = Cursors.SizeAll;
            bar.MouseDown += delegate { Native.DragWindow(this); };
            bar.MouseUp += delegate { SaveAll(); };
            string[] labels = { "+", "Colour", "✕" };
            EventHandler[] acts = { delegate { Create(); }, delegate { this.paper = (this.paper + 1) % Papers.Length; Apply(); SaveAll(); }, delegate { Delete(); } };
            for (int i = labels.Length - 1; i >= 0; i--)
            {
                var b = new Label();
                b.Text = labels[i]; b.AutoSize = false; b.Width = Theme.S(i == 1 ? 58 : 28); b.Dock = DockStyle.Right;
                b.TextAlign = ContentAlignment.MiddleCenter; b.Cursor = Cursors.Hand; b.Font = Theme.Font(9.5f, FontStyle.Bold);
                b.ForeColor = Color.FromArgb(60, 60, 60);
                b.Click += acts[i];
                bar.Controls.Add(b);
            }
            box = new TextBox();
            box.Multiline = true; box.AcceptsReturn = true; box.BorderStyle = BorderStyle.None; box.Dock = DockStyle.Fill;
            box.Font = Theme.Font(13f); box.ForeColor = Color.FromArgb(30, 30, 30);
            box.Text = text;
            box.TextChanged += delegate { SaveAll(); };
            var grip = new Label();
            grip.Text = "◢"; grip.Dock = DockStyle.Bottom; grip.Height = Theme.S(16); grip.TextAlign = ContentAlignment.MiddleRight;
            grip.Cursor = Cursors.SizeNWSE; grip.ForeColor = Color.FromArgb(120, 120, 120);
            Point start = Point.Empty; Size startSize = Size.Empty; bool sizing = false;
            grip.MouseDown += delegate { sizing = true; start = Cursor.Position; startSize = Size; };
            grip.MouseMove += delegate { if (sizing) Size = new Size(startSize.Width + Cursor.Position.X - start.X, startSize.Height + Cursor.Position.Y - start.Y); };
            grip.MouseUp += delegate { sizing = false; SaveAll(); };
            Controls.Add(box); Controls.Add(grip); Controls.Add(bar);
            Padding = new Padding(Theme.S(8), 0, Theme.S(8), 0);
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
        public static bool Enabled { get { return Store.Get("reminders.on", "1") == "1"; } }

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

    class ReminderAlert : Form
    {
        public ReminderAlert(string message, TimeSpan time)
        {
            Text = "Reminder";
            FormBorderStyle = FormBorderStyle.None; TopMost = true; ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            var wa = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Math.Min(wa.Width - 40, Theme.S(620)), Theme.S(210));
            Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + Theme.S(40));
            BackColor = Theme.Amber;
            var view = new Canvas();
            view.Dock = DockStyle.Fill; view.BackColor = Theme.Amber; view.Cursor = Cursors.Hand;
            view.Paint += delegate(object s, PaintEventArgs e)
            {
                Draw.Quality(e.Graphics);
                var r = view.ClientRectangle;
                Draw.Label(e.Graphics, string.Format("{0:00}:{1:00}  -  click to dismiss", time.Hours, time.Minutes), new RectangleF(0, 6, r.Width, Theme.S(26)), Color.FromArgb(70, 40, 0), 10.5f, FontStyle.Bold, StringAlignment.Center);
                Draw.Wrapped(e.Graphics, message, new RectangleF(16, Theme.S(34), r.Width - 32, r.Height - Theme.S(46)), Color.FromArgb(40, 24, 0), FontStyle.Bold, 54f);
            };
            view.Click += delegate { Close(); };
            Controls.Add(view);
            Shown += delegate { Sound.Change(); };
        }

        protected override bool ShowWithoutActivation { get { return true; } }
    }

    class RemindersForm : ToolForm
    {
        CheckBox on;

        public RemindersForm() : base("Register Reminders", 640, 440)
        {
            AddButton("Edit reminders", delegate
            {
                string t = TextEditDialog.Show(this, "Reminders", Reminders.Help, Reminders.Text);
                if (t != null) { Store.Set("reminders.text", t); View.Invalidate(); }
            }, true);
            on = AddCheck("Reminders on", Reminders.Enabled);
            on.CheckedChanged += delegate { Store.Set("reminders.on", on.Checked ? "1" : "0"); View.Invalidate(); };
            AddButton("Test", delegate { new ReminderAlert("This is what a reminder looks like", DateTime.Now.TimeOfDay).Show(); });
            Tick.Interval = 5000; Tick.Start();
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Theme.Bg);
            var items = Reminders.Parse(Reminders.Text);
            float row = Theme.S(44), y = Theme.S(16);
            Draw.Label(g, on.Checked ? "A banner appears at the top of the screen at each time, while Classroom Toolkit is running." : "Reminders are switched off.",
                new RectangleF(Theme.S(16), y, r.Width - Theme.S(32), Theme.S(26)), Theme.Muted, 10.5f, FontStyle.Regular, StringAlignment.Near);
            y += Theme.S(36);
            TimeSpan now = DateTime.Now.TimeOfDay;
            foreach (var item in items)
            {
                bool today = !item.Day.HasValue || item.Day.Value == DateTime.Now.DayOfWeek;
                bool past = !today || item.Time < now;
                var box = new RectangleF(Theme.S(16), y, r.Width - Theme.S(32), row - 6);
                Draw.FillRound(g, box, 10, Theme.Panel);
                string when = (item.Day.HasValue ? item.Day.Value.ToString().Substring(0, 3) + " " : "") + string.Format("{0:00}:{1:00}", item.Time.Hours, item.Time.Minutes);
                Draw.Label(g, when, new RectangleF(box.X + 12, box.Y, Theme.S(110), box.Height), past || !on.Checked ? Theme.Muted : Theme.Amber, 14f, FontStyle.Bold, StringAlignment.Near);
                Draw.Label(g, item.Message, new RectangleF(box.X + Theme.S(124), box.Y, box.Width - Theme.S(136), box.Height), past || !on.Checked ? Theme.Muted : Theme.Text, 13f, FontStyle.Regular, StringAlignment.Near);
                y += row;
                if (y > r.Height) break;
            }
            if (items.Count == 0) Draw.Label(g, "No reminders yet. Press Edit reminders.", new RectangleF(0, y, r.Width, row), Theme.Muted, 12f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    class ExamCountdownForm : ToolForm
    {
        const string Help = "One date per line, as day/month/year followed by the name, for example:\n12/05/2027 GCSE Maths Paper 1";
        class Item { public DateTime Date; public string Name; }

        public ExamCountdownForm() : base("Exam Countdown", 820, 520)
        {
            AddButton("Edit dates", delegate
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
            float topH = r.Height * (items.Count > 1 ? 0.56f : 0.9f);
            var hero = Draw.Inset(new RectangleF(0, 0, r.Width, topH), 12);
            Color tint = days <= 7 ? Theme.Red : days <= 30 ? Theme.Amber : Theme.Green;
            Draw.FillRound(g, hero, 22, Draw.Mix(Theme.Bg, tint, 0.26));
            Draw.Text(g, days == 0 ? "Today" : days.ToString(), new RectangleF(hero.X, hero.Y + hero.Height * 0.04f, hero.Width * 0.42f, hero.Height * 0.7f), Color.White, FontStyle.Bold, false, 500f, null);
            Draw.Text(g, days == 0 ? "" : days == 1 ? "day to go" : "days to go", new RectangleF(hero.X, hero.Y + hero.Height * 0.72f, hero.Width * 0.42f, hero.Height * 0.2f), Color.White, FontStyle.Regular, false, 60f, null);
            var right = new RectangleF(hero.X + hero.Width * 0.44f, hero.Y + hero.Height * 0.1f, hero.Width * 0.53f, hero.Height * 0.5f);
            Draw.Wrapped(g, next.Name, right, Color.White, FontStyle.Bold, 70f);
            Draw.Wrapped(g, next.Date.ToString("dddd d MMMM yyyy") + "\n" + SchoolDays(next.Date) + " weekdays  -  " + (days / 7) + " weeks",
                new RectangleF(right.X, hero.Y + hero.Height * 0.62f, right.Width, hero.Height * 0.3f), Color.FromArgb(220, 228, 240), FontStyle.Regular, 30f);
            float y = topH + 4, row = Math.Max(Theme.S(34), Math.Min(Theme.S(52), (r.Height - topH - 12) / Math.Max(1, Math.Min(6, items.Count - 1))));
            for (int i = 1; i < items.Count && y + row <= r.Height; i++, y += row)
            {
                var box = new RectangleF(12, y, r.Width - 24, row - 6);
                Draw.FillRound(g, box, 10, Theme.Panel);
                int d = (items[i].Date - DateTime.Today).Days;
                Draw.Label(g, d + " days", new RectangleF(box.X + 12, box.Y, Theme.S(120), box.Height), Theme.Amber, 13f, FontStyle.Bold, StringAlignment.Near);
                Draw.Label(g, items[i].Name, new RectangleF(box.X + Theme.S(130), box.Y, box.Width - Theme.S(330), box.Height), Theme.Text, 13f, FontStyle.Regular, StringAlignment.Near);
                Draw.Label(g, items[i].Date.ToString("ddd d MMM yyyy"), new RectangleF(box.Right - Theme.S(190), box.Y, Theme.S(178), box.Height), Theme.Muted, 12f, FontStyle.Regular, StringAlignment.Far);
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

        public NameLearnerForm() : base("Learn Student Names", 820, 640)
        {
            AddButton("Choose photo folder", delegate { Choose(); }, true);
            AddButton("Reveal (Space)", delegate { Reveal(); });
            AddButton("Knew it (Y)", delegate { Mark(true); }).BackColor = Theme.Green;
            AddButton("Not yet (N)", delegate { Mark(false); }).BackColor = Theme.Red;
            AddButton("Start again", delegate { LoadFolder(Store.Get("names.folder", "")); });
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
            if (files.Count == 0 && known == 0)
            {
                Draw.Wrapped(g, "Choose a folder of student photos.\nName each picture after the student, for example \"Sam Jones.jpg\".", Draw.Inset(r, r.Width * 0.1f), Theme.Muted, FontStyle.Regular, 26f);
                return;
            }
            if (index >= files.Count)
            {
                Draw.Wrapped(g, "All done - you know everyone!", Draw.Inset(r, r.Width * 0.1f), Theme.Green, FontStyle.Bold, 60f);
                return;
            }
            var area = new RectangleF(r.Width * 0.05f, r.Height * 0.04f, r.Width * 0.9f, r.Height * 0.68f);
            if (photo != null)
            {
                float s = Math.Min(area.Width / photo.Width, area.Height / photo.Height);
                float w = photo.Width * s, h = photo.Height * s;
                g.DrawImage(photo, area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h);
            }
            else Draw.Text(g, "(picture could not be opened)", area, Theme.Muted, FontStyle.Regular, false, 24f, null);
            var nameBox = new RectangleF(r.Width * 0.05f, r.Height * 0.74f, r.Width * 0.9f, r.Height * 0.16f);
            if (revealed) Draw.Text(g, Path.GetFileNameWithoutExtension(files[index]), nameBox, Theme.Text, FontStyle.Bold, false, 90f, null);
            else Draw.Text(g, "Who is this?  Press Space to reveal", nameBox, Theme.Muted, FontStyle.Regular, false, 30f, null);
            Draw.Label(g, (files.Count - index) + " to go   -   " + known + " known   -   " + again.Count + " to repeat", new RectangleF(0, r.Height * 0.92f, r.Width, Theme.S(26)), Theme.Muted, 11f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    class StatementForm : Form
    {
        readonly FileChooser chooser;
        readonly TextBox name, subject, output;
        readonly ComboBox pronoun;
        readonly TreeView tree;
        readonly List<string> chosen = new List<string>();

        public StatementForm()
        {
            Text = "Report Statement Builder";
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.Font(10f);
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(980), Theme.S(680));

            var bar = new FlowLayoutPanel();
            bar.Dock = DockStyle.Top; bar.AutoSize = true; bar.BackColor = Theme.Panel; bar.Padding = new Padding(Theme.S(6));
            bar.Controls.Add(Lbl("Bank"));
            chooser = new FileChooser(this, bar.Controls, "Statements", "statement bank", Samples.StatementHelp);
            bar.Controls.Add(Lbl("Student"));
            name = Box("Sam", 130); bar.Controls.Add(name);
            bar.Controls.Add(Lbl("Subject"));
            subject = Box(Store.Get("statements.subject", "Science"), 130); bar.Controls.Add(subject);
            pronoun = new ComboBox();
            pronoun.DropDownStyle = ComboBoxStyle.DropDownList; pronoun.Items.AddRange(new object[] { "he / him", "she / her", "they / them" });
            pronoun.SelectedIndex = 2; pronoun.Width = Theme.S(110); pronoun.Font = Theme.Font(10.5f);
            pronoun.Margin = new Padding(Theme.S(6), Theme.S(6), Theme.S(3), Theme.S(3));
            bar.Controls.Add(pronoun);
            var copy = ToolForm.MakeButton("Copy report", true);
            copy.Click += delegate { if (output.Text.Length > 0) Clipboard.SetText(output.Text); };
            bar.Controls.Add(copy);
            var next = ToolForm.MakeButton("Next student", false);
            next.Click += delegate { name.Text = ""; ClearChecks(); name.Focus(); };
            bar.Controls.Add(next);

            tree = new TreeView();
            tree.CheckBoxes = true; tree.Dock = DockStyle.Fill; tree.BorderStyle = BorderStyle.None;
            tree.BackColor = Theme.Bg; tree.ForeColor = Theme.Text; tree.Font = Theme.Font(11f);
            tree.ShowLines = false; tree.FullRowSelect = true; tree.ItemHeight = Theme.S(28);
            tree.AfterCheck += delegate(object s, TreeViewEventArgs e)
            {
                if (e.Node.Tag == null) return;
                string t = (string)e.Node.Tag;
                if (e.Node.Checked) { if (!chosen.Contains(t)) chosen.Add(t); } else chosen.Remove(t);
                Compose();
            };

            output = new TextBox();
            output.Multiline = true; output.ScrollBars = ScrollBars.Vertical; output.Dock = DockStyle.Bottom; output.Height = Theme.S(190);
            output.Font = Theme.Font(12f); output.BackColor = Color.White; output.ForeColor = Color.Black;

            Controls.Add(tree); Controls.Add(output); Controls.Add(bar);
            EventHandler redo = delegate { Store.Set("statements.subject", subject.Text); Compose(); };
            name.TextChanged += redo; subject.TextChanged += redo; pronoun.SelectedIndexChanged += redo;
            chooser.Changed += delegate { LoadBank(); };
            LoadBank();
        }

        static Label Lbl(string t)
        {
            var l = new Label();
            l.Text = t; l.AutoSize = true; l.ForeColor = Theme.Muted; l.Margin = new Padding(Theme.S(8), Theme.S(10), Theme.S(2), 0);
            return l;
        }

        static TextBox Box(string t, int w)
        {
            var b = new TextBox();
            b.Text = t; b.Width = Theme.S(w); b.Font = Theme.Font(11f); b.Margin = new Padding(Theme.S(3), Theme.S(6), Theme.S(3), Theme.S(3));
            return b;
        }

        void LoadBank()
        {
            tree.BeginUpdate();
            tree.Nodes.Clear(); chosen.Clear();
            TreeNode section = null;
            foreach (string raw in Store.Lines(chooser.Path ?? ""))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#"))
                {
                    section = tree.Nodes.Add(line.TrimStart('#', ' '));
                    section.NodeFont = Theme.Font(11f, FontStyle.Bold);
                    section.ForeColor = Theme.Accent;
                    continue;
                }
                if (section == null) section = tree.Nodes.Add("Statements");
                var node = section.Nodes.Add(line);
                node.Tag = line;
            }
            tree.ExpandAll();
            tree.EndUpdate();
            Compose();
        }

        void ClearChecks()
        {
            foreach (TreeNode s in tree.Nodes) foreach (TreeNode n in s.Nodes) n.Checked = false;
            chosen.Clear();
            Compose();
        }

        void Compose()
        {
            string[][] forms =
            {
                new[] { "he", "him", "his", "himself" },
                new[] { "she", "her", "her", "herself" },
                new[] { "they", "them", "their", "themselves" }
            };
            string[] p = forms[Math.Max(0, pronoun.SelectedIndex)];
            string who = name.Text.Trim().Length == 0 ? "[name]" : name.Text.Trim();
            var sb = new StringBuilder();
            foreach (string s in chosen)
            {
                string t = s.Replace("{name}", who).Replace("{Name}", who).Replace("~Name~", who)
                    .Replace("{subject}", subject.Text.Trim()).Replace("{Subject}", subject.Text.Trim()).Replace("~Subject~", subject.Text.Trim())
                    .Replace("{he}", p[0]).Replace("{He}", Cap(p[0])).Replace("{him}", p[1]).Replace("{Him}", Cap(p[1]))
                    .Replace("{his}", p[2]).Replace("{His}", Cap(p[2])).Replace("{himself}", p[3]);
                t = Verbs(t, pronoun.SelectedIndex == 2);
                sb.Append(t.Trim()).Append(' ');
            }
            output.Text = sb.ToString().Trim();
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

    class CharactersForm : Form
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
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.Font(10f);
            Icon = AppIcon.Get(); TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(640), Theme.S(470));

            var foot = new FlowLayoutPanel();
            foot.Dock = DockStyle.Bottom; foot.AutoSize = true; foot.BackColor = Theme.Panel; foot.Padding = new Padding(Theme.S(6));
            var hint = new Label();
            hint.Text = "Click a character to copy it. Collected:"; hint.AutoSize = true; hint.ForeColor = Theme.Muted; hint.Margin = new Padding(Theme.S(4), Theme.S(10), Theme.S(4), 0);
            collected = new TextBox();
            collected.Width = Theme.S(220); collected.Font = Theme.Font(13f); collected.Margin = new Padding(Theme.S(3), Theme.S(5), Theme.S(3), Theme.S(3));
            var copy = ToolForm.MakeButton("Copy all", true);
            copy.Click += delegate { if (collected.Text.Length > 0) Clipboard.SetText(collected.Text); };
            var clear = ToolForm.MakeButton("Clear", false);
            clear.Click += delegate { collected.Text = ""; };
            foot.Controls.AddRange(new Control[] { hint, collected, copy, clear });

            var tabs = new TabControl();
            tabs.Dock = DockStyle.Fill; tabs.Font = Theme.Font(10.5f);
            foreach (var set in Sets)
            {
                var page = new TabPage(set[0]);
                page.BackColor = Theme.Bg;
                var flow = new FlowLayoutPanel();
                flow.Dock = DockStyle.Fill; flow.AutoScroll = true; flow.Padding = new Padding(Theme.S(8));
                foreach (string ch in set[1].Split(' '))
                {
                    string c = ch;
                    var b = ToolForm.MakeButton(c, false);
                    b.AutoSize = c.Length > 2; b.Size = new Size(Theme.S(50), Theme.S(50)); b.MinimumSize = b.Size;
                    b.Font = Theme.Font(c.Length > 2 ? 12f : 17f);
                    b.Click += delegate { collected.Text += c; try { Clipboard.SetText(c); } catch { } };
                    flow.Controls.Add(b);
                }
                page.Controls.Add(flow);
                tabs.TabPages.Add(page);
            }
            Controls.Add(tabs); Controls.Add(foot);
        }
    }

    class TextTidyForm : Form
    {
        readonly TextBox box;
        readonly Label status;

        public TextTidyForm()
        {
            Text = "Text Tidy";
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.Font(10f);
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(940), Theme.S(640));

            box = new TextBox();
            box.Multiline = true; box.ScrollBars = ScrollBars.Both; box.WordWrap = false; box.AcceptsReturn = true; box.AcceptsTab = true;
            box.Dock = DockStyle.Fill; box.Font = Theme.Font(12f); box.MaxLength = 0;
            box.BackColor = Color.White; box.ForeColor = Color.Black;

            var side = new FlowLayoutPanel();
            side.Dock = DockStyle.Left; side.Width = Theme.S(224); side.FlowDirection = FlowDirection.TopDown; side.WrapContents = false;
            side.AutoScroll = true; side.BackColor = Theme.Panel; side.Padding = new Padding(Theme.S(6));

            Action<string, Func<string, string>, bool> add = delegate(string label, Func<string, string> f, bool primary)
            {
                var b = ToolForm.MakeButton(label, primary);
                b.AutoSize = false; b.MinimumSize = Size.Empty; b.Size = new Size(Theme.S(204), Theme.S(30)); b.TextAlign = ContentAlignment.MiddleLeft;
                b.Margin = new Padding(Theme.S(3), Theme.S(2), Theme.S(3), Theme.S(2));
                b.Click += delegate { try { box.Text = f(box.Text); } catch (Exception ex) { status.Text = ex.Message; } };
                side.Controls.Add(b);
            };
            add("Paste from clipboard", t => Clipboard.ContainsText() ? Clipboard.GetText() : t, true);
            add("Copy to clipboard", t => { if (t.Length > 0) Clipboard.SetText(t); status.Text = "Copied"; return t; }, true);
            add("UPPER CASE", t => t.ToUpper(), false);
            add("lower case", t => t.ToLower(), false);
            add("Title Case", t => CultureInfo.GetCultureInfo("en-GB").TextInfo.ToTitleCase(t.ToLower()), false);
            add("Sentence case", Sentence, false);
            add("Sort A to Z", t => Each(t, l => l.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)), false);
            add("Sort Z to A", t => Each(t, l => l.OrderByDescending(x => x, StringComparer.CurrentCultureIgnoreCase)), false);
            add("Sort by surname", t => Each(t, l => l.OrderBy(x => x.Trim().Split(' ').Last(), StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x)), false);
            add("Sort as numbers", t => Each(t, l => l.OrderBy(x => Num(x))), false);
            add("Shuffle lines", t => Each(t, l => { var a = l.ToList(); Store.Shuffle(a); return a; }), false);
            add("Remove duplicate lines", t => Each(t, l => l.Distinct(StringComparer.CurrentCultureIgnoreCase)), false);
            add("Remove blank lines", t => Each(t, l => l.Where(x => x.Trim().Length > 0)), false);
            add("Trim spaces", t => Each(t, l => l.Select(x => string.Join(" ", x.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)))), false);
            add("Swap first and last name", t => Each(t, l => l.Select(Swap)), false);
            add("Number the lines", t => { int n = 0; return Each(t, l => l.Select(x => (++n) + ". " + x)); }, false);
            add("Sum and average", t => { Stats(t); return t; }, false);

            status = new Label();
            status.Dock = DockStyle.Bottom; status.Height = Theme.S(28); status.ForeColor = Theme.Muted; status.Padding = new Padding(Theme.S(8), Theme.S(5), 0, 0);
            box.TextChanged += delegate
            {
                string t = box.Text;
                int words = t.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                status.Text = t.Replace("\r\n", "\n").Split('\n').Length + " lines   -   " + words + " words   -   " + t.Length + " characters";
            };
            Controls.Add(box); Controls.Add(side); Controls.Add(status);
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
