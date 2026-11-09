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
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]

namespace ClassroomToolkit
{
    class Tool
    {
        public string Category, Name, About;
        public Color Tint;
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

    class Tile : Control
    {
        public readonly Tool Tool;
        bool hot;

        public Tile(Tool t)
        {
            Tool = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Theme.S(250), Theme.S(74));
            Margin = new Padding(Theme.S(5));
            Cursor = Cursors.Hand;
            MouseEnter += delegate { hot = true; Invalidate(); };
            MouseLeave += delegate { hot = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Draw.Quality(g);
            g.Clear(Theme.Bg);
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            Draw.FillRound(g, r, Theme.S(10), hot ? Theme.Panel2 : Theme.Panel);
            Draw.FillRound(g, new RectangleF(0, Theme.S(12), Theme.S(5), Height - Theme.S(24)), Theme.S(2), Tool.Tint);
            Draw.Label(g, Tool.Name, new RectangleF(Theme.S(16), Theme.S(8), Width - Theme.S(24), Theme.S(26)), Theme.Text, 11.5f, FontStyle.Bold, StringAlignment.Near);
            using (var sf = new StringFormat())
            using (var b = new SolidBrush(Theme.Muted))
            {
                sf.Trimming = StringTrimming.EllipsisWord;
                g.DrawString(Tool.About, Theme.Font(9f), b, new RectangleF(Theme.S(16), Theme.S(34), Width - Theme.S(24), Theme.S(36)), sf);
            }
        }
    }

    class Launcher : Form
    {
        readonly FlowLayoutPanel flow = new FlowLayoutPanel();
        readonly TextBox search = new TextBox();
        public bool Exiting;

        public Launcher()
        {
            Text = "Classroom Toolkit";
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.Font(10f);
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(1090), Theme.S(700));
            MinimumSize = new Size(Theme.S(320), Theme.S(300));
            KeyPreview = true;

            var head = new Panel();
            head.Width = ClientSize.Width; head.Dock = DockStyle.Top; head.Height = Theme.S(58); head.BackColor = Theme.Panel;
            var title = new Label();
            title.Text = "Classroom Toolkit"; title.Font = Theme.Font(17f, FontStyle.Bold); title.ForeColor = Theme.Text; title.AutoSize = true;
            title.Location = new Point(Theme.S(16), Theme.S(12));
            search.Font = Theme.Font(12f); search.Width = Theme.S(260);
            search.BackColor = Theme.Panel2; search.ForeColor = Color.White; search.BorderStyle = BorderStyle.FixedSingle;
            search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            search.Location = new Point(ClientSize.Width - search.Width - Theme.S(16), Theme.S(15));
            search.TextChanged += delegate { Build(); };
            var hint = new Label();
            hint.Text = "Search"; hint.ForeColor = Theme.Muted; hint.AutoSize = true; hint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            hint.Location = new Point(search.Left - Theme.S(60), Theme.S(19));
            head.Controls.AddRange(new Control[] { title, hint, search });

            flow.Dock = DockStyle.Fill; flow.AutoScroll = true; flow.BackColor = Theme.Bg;
            flow.Padding = new Padding(Theme.S(12), Theme.S(4), Theme.S(12), Theme.S(12));
            Controls.Add(flow); Controls.Add(head);
            Build();
            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (Exiting || e.CloseReason != CloseReason.UserClosing) return;
                e.Cancel = true;
                Hide();
                TrayApp.HiddenNotice();
            };
        }

        void Build()
        {
            string q = search.Text.Trim().ToLowerInvariant();
            flow.SuspendLayout();
            foreach (Control old in flow.Controls.Cast<Control>().ToArray()) old.Dispose();
            flow.Controls.Clear();
            string cat = null;
            Control last = null;
            foreach (Tool t in Tools.All)
            {
                if (q.Length > 0 && !(t.Name + " " + t.About + " " + t.Category).ToLowerInvariant().Contains(q)) continue;
                if (t.Category != cat)
                {
                    if (last != null) flow.SetFlowBreak(last, true);
                    cat = t.Category;
                    var h = new Label();
                    h.Text = cat; h.Font = Theme.Font(12.5f, FontStyle.Bold); h.ForeColor = t.Tint; h.AutoSize = true;
                    h.Margin = new Padding(Theme.S(6), Theme.S(14), 0, Theme.S(2));
                    flow.Controls.Add(h);
                    flow.SetFlowBreak(h, true);
                }
                var tile = new Tile(t);
                Tool tool = t;
                tile.Click += delegate { Tools.Launch(tool, this); };
                flow.Controls.Add(tile);
                last = tile;
            }
            if (flow.Controls.Count == 0)
            {
                var none = new Label();
                none.Text = "No tools match \"" + search.Text + "\""; none.ForeColor = Theme.Muted; none.AutoSize = true; none.Margin = new Padding(Theme.S(10), Theme.S(20), 0, 0);
                flow.Controls.Add(none);
            }
            flow.ResumeLayout();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { if (search.Text.Length > 0) search.Text = ""; else Close(); return true; }
            if (keyData == Keys.Enter && search.Focused)
            {
                var first = flow.Controls.OfType<Tile>().FirstOrDefault();
                if (first != null) Tools.Launch(first.Tool, this);
                return true;
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

            tray = new NotifyIcon();
            tray.Icon = AppIcon.Get();
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
