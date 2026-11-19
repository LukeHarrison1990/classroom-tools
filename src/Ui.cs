// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - shared look and feel, base window and small dialogs.
// Written in plain C# 5 so it compiles with the csc.exe that ships with Windows.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    static class Env
    {
        public static readonly bool IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
    }

    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(18, 22, 30);
        public static readonly Color Panel = Color.FromArgb(28, 34, 46);
        public static readonly Color Panel2 = Color.FromArgb(42, 50, 66);
        public static readonly Color Line = Color.FromArgb(60, 70, 90);
        public static readonly Color Text = Color.FromArgb(236, 240, 246);
        public static readonly Color Muted = Color.FromArgb(150, 160, 178);
        public static readonly Color Accent = Color.FromArgb(56, 132, 255);
        public static readonly Color Green = Color.FromArgb(34, 197, 94);
        public static readonly Color Amber = Color.FromArgb(245, 158, 11);
        public static readonly Color Red = Color.FromArgb(239, 68, 68);
        public static readonly Color Purple = Color.FromArgb(168, 85, 247);
        public static readonly Color Teal = Color.FromArgb(20, 184, 166);
        public static readonly Color Pink = Color.FromArgb(236, 72, 153);

        public static string FontName = "Segoe UI";
        public static float Scale = 1f;
        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();

        public static void Init()
        {
            try
            {
                using (var f = new Font("Segoe UI", 10f))
                    if (f.Name != "Segoe UI") FontName = FontFamily.GenericSansSerif.Name;
                using (var b = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(b))
                    Scale = Math.Max(1f, g.DpiX / 96f);
            }
            catch { }
        }

        public static int S(int px) { return (int)Math.Round(px * Scale); }

        public static Font Font(float size, FontStyle style)
        {
            size = (float)Math.Round(size * 2) / 2f;
            if (size < 4) size = 4;
            string key = size + "|" + (int)style;
            Font f;
            if (!fonts.TryGetValue(key, out f))
            {
                if (fonts.Count > 400) fonts.Clear();
                f = new Font(FontName, size, style, GraphicsUnit.Point);
                fonts[key] = f;
            }
            return f;
        }

        public static Font Font(float size) { return Font(size, FontStyle.Regular); }

        static string semiName;
        static bool semiChecked;

        // Semibold where Windows has it (Segoe UI Semibold), otherwise bold.
        public static Font Semi(float size)
        {
            if (!semiChecked)
            {
                semiChecked = true;
                try { using (var f = new Font("Segoe UI Semibold", 10f)) if (f.Name == "Segoe UI Semibold") semiName = f.Name; } catch { }
            }
            if (semiName == null) return Font(size, FontStyle.Bold);
            size = (float)Math.Round(size * 2) / 2f;
            string key = "semi|" + size;
            Font font;
            if (!fonts.TryGetValue(key, out font)) { font = new Font(semiName, size, FontStyle.Regular, GraphicsUnit.Point); fonts[key] = font; }
            return font;
        }
    }

    static class Draw
    {
        static readonly Dictionary<string, float> fitCache = new Dictionary<string, float>();

        public static void Quality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        // Largest point size at which text fits inside the box.
        public static float FitSize(Graphics g, string text, SizeF box, FontStyle style, bool wrap, float max)
        {
            if (string.IsNullOrEmpty(text) || box.Width < 4 || box.Height < 4) return 6f;
            string key = text + "|" + (int)box.Width + "|" + (int)box.Height + "|" + (int)style + "|" + wrap + "|" + max;
            float cached;
            if (fitCache.TryGetValue(key, out cached)) return cached;
            float lo = 5f, hi = max;
            // Wrapped text gets a safety margin, and no single word may be wider than the box,
            // so the drawn text never spills out or breaks a word in half.
            float wrapW = box.Width * 0.94f;
            string longest = null;
            if (wrap)
                foreach (string word in text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    if (longest == null || word.Length > longest.Length) longest = word;
            for (int i = 0; i < 10; i++)
            {
                float mid = (lo + hi) / 2f;
                using (var f = new Font(Theme.FontName, mid, style, GraphicsUnit.Point))
                {
                    bool fits;
                    if (wrap)
                    {
                        SizeF sz = g.MeasureString(text, f, (int)wrapW);
                        fits = sz.Height <= box.Height * 0.9f && (longest == null || g.MeasureString(longest, f).Width <= wrapW);
                    }
                    else
                    {
                        SizeF sz = g.MeasureString(text, f);
                        fits = sz.Width <= box.Width && sz.Height <= box.Height;
                    }
                    if (fits) lo = mid; else hi = mid;
                }
            }
            if (fitCache.Count > 600) fitCache.Clear();
            fitCache[key] = lo;
            return lo;
        }

        public static void Text(Graphics g, string text, RectangleF r, Color c, FontStyle style, bool wrap, float max, string measureAs)
        {
            if (string.IsNullOrEmpty(text)) return;
            float size = FitSize(g, measureAs ?? text, r.Size, style, wrap, max);
            using (var sf = new StringFormat())
            using (var b = new SolidBrush(c))
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                if (!wrap) sf.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(text, Theme.Font(size, style), b, r, sf);
            }
        }

        public static void Text(Graphics g, string text, RectangleF r, Color c)
        {
            Text(g, text, r, c, FontStyle.Bold, false, 400f, null);
        }

        public static void Wrapped(Graphics g, string text, RectangleF r, Color c, FontStyle style, float max)
        {
            Text(g, text, r, c, style, true, max, null);
        }

        public static void Label(Graphics g, string text, RectangleF r, Color c, float size, FontStyle style, StringAlignment align)
        {
            using (var sf = new StringFormat())
            using (var b = new SolidBrush(c))
            {
                sf.Alignment = align;
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(text, Theme.Font(size, style), b, r, sf);
            }
        }

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            var p = new GraphicsPath();
            float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            if (d < 1) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, RectangleF r, float rad, Color c)
        {
            using (var p = Round(r, rad))
            using (var b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        public static Color Mix(Color a, Color b, double t)
        {
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        // A list drawn one item per line, all at the same size, with no item wrapping.
        public static void Lines(Graphics g, IList<string> lines, RectangleF r, Color c, float max)
        {
            if (lines.Count == 0) return;
            string longest = lines.OrderByDescending(l => g.MeasureString(l, Theme.Font(20f)).Width).First();
            float lineH = r.Height / lines.Count;
            float size = Math.Min(max, FitSize(g, longest, new SizeF(r.Width, lineH * 0.95f), FontStyle.Regular, false, max));
            Font f = Theme.Font(size);
            float used = f.GetHeight(g) * 1.12f;
            float y = r.Y + (r.Height - used * lines.Count) / 2;
            using (var sf = new StringFormat())
            using (var b = new SolidBrush(c))
            {
                sf.Alignment = StringAlignment.Center;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                foreach (string line in lines)
                {
                    g.DrawString(line, f, b, new RectangleF(r.X, y, r.Width, used), sf);
                    y += used;
                }
            }
        }

        // A centred row of rounded "chips" (for example recent picks). Shows "+N more" when they don't all fit.
        public static void Chips(Graphics g, IList<string> items, RectangleF area, Color fill, Color ink, float size)
        {
            if (items.Count == 0) return;
            Font f = Theme.Font(size);
            float h = area.Height, padX = h * 0.45f, gap = h * 0.25f;
            var widths = new List<float>();
            float total = 0;
            int fit = 0;
            for (int i = 0; i < items.Count; i++)
            {
                float wv = g.MeasureString(items[i], f).Width + padX * 2;
                float more = i < items.Count - 1 ? g.MeasureString("+" + (items.Count - i - 1) + " more", f).Width + padX * 2 + gap : 0;
                if (total + wv + (fit > 0 ? gap : 0) + more > area.Width && fit > 0) break;
                widths.Add(wv);
                total += wv + (fit > 0 ? gap : 0);
                fit++;
            }
            string extra = fit < items.Count ? "+" + (items.Count - fit) + " more" : null;
            float extraW = extra == null ? 0 : g.MeasureString(extra, f).Width + padX * 2;
            if (extra != null) total += gap + extraW;
            float x = area.X + (area.Width - total) / 2;
            for (int i = 0; i < fit; i++)
            {
                var r = new RectangleF(x, area.Y, widths[i], h);
                FillRound(g, r, h / 2, fill);
                Label(g, items[i], r, ink, size, FontStyle.Regular, StringAlignment.Center);
                x += widths[i] + gap;
            }
            if (extra != null) Label(g, extra, new RectangleF(x, area.Y, extraW, h), Theme.Muted, size, FontStyle.Regular, StringAlignment.Center);
        }

        public static RectangleF Inset(RectangleF r, float by)
        {
            return new RectangleF(r.X + by, r.Y + by, Math.Max(1, r.Width - 2 * by), Math.Max(1, r.Height - 2 * by));
        }
    }

    class Canvas : Panel
    {
        public Canvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, true);
            BackColor = Theme.Bg;
            TabStop = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }
    }

    // Base window used by every tool: a toolbar along the top and a painted display below.
    // In full screen the toolbar hides itself and slides back when the mouse goes to the top edge.
    class ToolForm : BaseForm
    {
        protected ToolBarPanel Bar;
        protected Canvas View;
        protected Timer Tick;
        bool full;
        FormBorderStyle oldBorder;
        FormWindowState oldState;
        Rectangle oldBounds;
        FlatButton pinButton, fullButton;
        readonly Timer chrome = new Timer();
        DateTime chromeUntil;

        public ToolForm(string title, int w, int h)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(w), Theme.S(h));
            MinimumSize = new Size(Theme.S(360), Theme.S(260));
            KeyPreview = true;

            View = new Canvas();
            View.Dock = DockStyle.Fill;
            View.Paint += delegate(object s, PaintEventArgs e)
            {
                Draw.Quality(e.Graphics);
                try { Render(e.Graphics, View.ClientRectangle); }
                catch (Exception ex) { Draw.Label(e.Graphics, ex.Message, View.ClientRectangle, Theme.Red, 10f, FontStyle.Regular, StringAlignment.Center); }
            };
            Controls.Add(View);

            Bar = new ToolBarPanel();
            Bar.Dock = DockStyle.Top;
            Controls.Add(Bar);

            Tick = new Timer();
            Tick.Interval = 100;
            Tick.Tick += delegate { OnTick(); };
            chrome.Interval = 150;
            chrome.Tick += delegate { UpdateChrome(); };
            Resize += delegate { if (full) Bar.Width = ClientSize.Width; };
            FormClosed += delegate { Tick.Stop(); Tick.Dispose(); chrome.Stop(); chrome.Dispose(); };
            Shown += delegate { View.Focus(); };
        }

        protected virtual void Render(Graphics g, Rectangle r) { }
        protected virtual void OnTick() { View.Invalidate(); }
        protected virtual bool OnToolKey(Keys key) { return false; }

        public void FocusDisplay() { if (View != null && !View.IsDisposed) View.Focus(); }
        protected bool IsFullScreen { get { return full; } }

        public static FlatButton MakeButton(string text, bool primary) { return new FlatButton(text, Glyph.None, primary); }
        public static FlatButton MakeButton(string text, Glyph glyph, bool primary) { return new FlatButton(text, glyph, primary); }

        protected FlatButton AddButton(string text, EventHandler click) { return AddButton(text, Glyph.None, click, false); }
        protected FlatButton AddButton(string text, EventHandler click, bool primary) { return AddButton(text, Glyph.None, click, primary); }

        protected FlatButton AddButton(string text, Glyph glyph, EventHandler click, bool primary)
        {
            var b = new FlatButton(text, glyph, primary);
            if (click != null) b.Click += click;
            Bar.Controls.Add(b);
            return b;
        }

        // A square button showing only an icon, with a tooltip.
        protected FlatButton AddIcon(Glyph glyph, string tip, EventHandler click)
        {
            var b = new FlatButton("", glyph, false);
            b.Tip = tip;
            if (click != null) b.Click += click;
            Bar.Controls.Add(b);
            return b;
        }

        protected Label AddLabel(string text)
        {
            var l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.ForeColor = Theme.Muted;
            l.Font = Theme.Font(9.5f);
            l.Margin = new Padding(Theme.S(8), 0, Theme.S(2), 0);
            Bar.Controls.Add(l);
            return l;
        }

        protected Divider AddDivider()
        {
            var d = new Divider();
            Bar.Controls.Add(d);
            return d;
        }

        protected Stepper AddNumber(int min, int max, int value, int width)
        {
            var n = new Stepper();
            n.Minimum = min; n.Maximum = max;
            n.Value = Math.Max(min, Math.Min(max, value));
            n.Width = Theme.S(Math.Max(36, width) + 64);
            Bar.Controls.Add(n);
            return n;
        }

        protected Toggle AddCheck(string text, bool on)
        {
            var c = new Toggle(text, on);
            Bar.Controls.Add(c);
            return c;
        }

        protected Choice AddCombo(int width, params string[] items)
        {
            var c = new Choice(width);
            c.Items.AddRange(items);
            if (items.Length > 0) c.SelectedIndex = 0;
            Bar.Controls.Add(c);
            return c;
        }

        protected TextBox AddText(string text, int width)
        {
            var t = new TextField(text, width);
            Bar.Controls.Add(t);
            return t.Box;
        }

        // Keep-on-top and full-screen buttons, at the right-hand end of the toolbar.
        protected void AddWindowButtons(bool startOnTop)
        {
            pinButton = new FlatButton("", Glyph.Pin, false);
            pinButton.Tip = "Keep on top of other windows";
            pinButton.Click += delegate { SetPinned(!TopMost); };
            fullButton = new FlatButton("", Glyph.FullScreen, false);
            fullButton.Tip = "Full screen (F11)";
            fullButton.Click += delegate { ToggleFull(); };
            Bar.AddRight(pinButton);
            Bar.AddRight(fullButton);
            SetPinned(startOnTop);
        }

        protected void SetPinned(bool on)
        {
            TopMost = on;
            if (pinButton != null) pinButton.Toggled = on;
        }

        public void ToggleFull()
        {
            if (!full)
            {
                oldBorder = FormBorderStyle; oldState = WindowState; oldBounds = Bounds;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = Screen.FromControl(this).Bounds;
                full = true;
                Bar.Dock = DockStyle.None;
                Bar.SetBounds(0, 0, ClientSize.Width, Bar.Height);
                Bar.BringToFront();
                if (fullButton != null) { fullButton.Glyph = Glyph.ExitFullScreen; fullButton.Tip = "Leave full screen (F11 or Esc)"; }
                chromeUntil = DateTime.Now.AddSeconds(2.5);
                chrome.Start();
            }
            else
            {
                chrome.Stop();
                FormBorderStyle = oldBorder;
                Bounds = oldBounds;
                WindowState = oldState;
                full = false;
                Bar.Visible = true;
                Bar.Dock = DockStyle.Top;
                Bar.SendToBack();
                if (fullButton != null) { fullButton.Glyph = Glyph.FullScreen; fullButton.Tip = "Full screen (F11)"; }
            }
            View.Invalidate();
        }

        void UpdateChrome()
        {
            if (!full) return;
            Point p = PointToClient(Cursor.Position);
            bool inside = p.X >= 0 && p.X < ClientSize.Width;
            int zone = Bar.Visible ? Bar.Height + Theme.S(24) : Theme.S(6);
            bool busy = Bar.ContainsFocus;
            if ((inside && p.Y >= 0 && p.Y < zone) || busy) chromeUntil = DateTime.Now.AddSeconds(1.2);
            bool show = DateTime.Now < chromeUntil;
            if (Bar.Visible != show)
            {
                Bar.Visible = show;
                if (show) { Bar.Width = ClientSize.Width; Bar.BringToFront(); }
                else FocusDisplay();
            }
        }

        static bool IsTyping(Control c)
        {
            while (c is ContainerControl && ((ContainerControl)c).ActiveControl != null) c = ((ContainerControl)c).ActiveControl;
            return c is TextBoxBase || c is UpDownBase || (c != null && c.Parent is UpDownBase);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11) { ToggleFull(); return true; }
            if (keyData == Keys.Escape && full) { ToggleFull(); return true; }
            if (!IsTyping(ActiveControl) && OnToolKey(keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    static class AppIcon
    {
        static Icon icon, small;

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern uint ExtractIconEx(string file, int index, IntPtr[] large, IntPtr[] small, uint count);

        // Window icon (32 px).
        public static Icon Get()
        {
            if (icon == null) Load();
            return icon;
        }

        // Notification-area icon (16 px at 100% scaling), so Windows does not have to shrink the large one.
        public static Icon Small()
        {
            if (small == null) Load();
            return small ?? icon;
        }

        static Icon FromResource(Size size)
        {
            try
            {
                using (var st = typeof(AppIcon).Assembly.GetManifestResourceStream("app.ico"))
                    if (st != null) return new Icon(st, size);
            }
            catch { }
            return null;
        }

        static void Load()
        {
            // The icon file is embedded in the program, so this works wherever it runs from
            // and picks the sharpest size for the current display scaling.
            icon = FromResource(SystemInformation.IconSize);
            small = FromResource(SystemInformation.SmallIconSize);
            if (icon != null && small != null) return;
            // ExtractIconEx reads the icon built into the exe and, unlike Icon.ExtractAssociatedIcon,
            // also works when the program is run from a network share (\\server\share\...).
            if (icon == null && Env.IsWindows)
            {
                try
                {
                    var l = new IntPtr[1]; var sm = new IntPtr[1];
                    if (ExtractIconEx(Application.ExecutablePath, 0, l, sm, 1) > 0)
                    {
                        if (l[0] != IntPtr.Zero) icon = Icon.FromHandle(l[0]);
                        if (sm[0] != IntPtr.Zero) small = Icon.FromHandle(sm[0]);
                    }
                }
                catch { }
            }
            if (icon == null) { try { icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { } }
            if (icon == null) icon = Drawn(32);
            if (small == null) small = Drawn(16) ?? icon;
            if (icon == null) icon = SystemIcons.Application;
        }

        // Last resort: draw the same traffic-light icon as app.ico.
        static Icon Drawn(int size)
        {
            try
            {
                using (var bmp = new Bitmap(size, size))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        Draw.Quality(g);
                        float k = size / 32f;
                        Draw.FillRound(g, new RectangleF(0.5f, 0.5f, size - 1, size - 1), 7 * k, Theme.Accent);
                        Draw.FillRound(g, new RectangleF(10.5f * k, 3 * k, 11 * k, 26 * k), 4 * k, Color.FromArgb(17, 24, 39));
                        Color[] c = { Theme.Red, Theme.Amber, Theme.Green };
                        for (int i = 0; i < 3; i++)
                            using (var b = new SolidBrush(c[i])) g.FillEllipse(b, 12.5f * k, (4.8f + i * 7.8f) * k, 7 * k, 7 * k);
                    }
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch { return null; }
        }
    }

    // Multi-line text editor used for class lists, question sets and similar files.
    class TextEditDialog : BaseForm
    {
        TextBox box;

        TextEditDialog(string title, string help, string text)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(700), Theme.S(560));
            MinimumSize = new Size(Theme.S(420), Theme.S(320));
            MinimizeBox = false; ShowInTaskbar = false;

            var holder = new Panel();
            holder.Dock = DockStyle.Fill; holder.BackColor = Theme.Panel2;
            holder.Padding = new Padding(Theme.S(14), Theme.S(12), Theme.S(4), Theme.S(4));
            box = new TextBox();
            box.Multiline = true; box.ScrollBars = ScrollBars.Vertical; box.AcceptsReturn = true; box.AcceptsTab = false;
            box.Dock = DockStyle.Fill; box.BorderStyle = BorderStyle.None;
            string mono = "Consolas";
            try { using (var f = new Font(mono, 11f)) if (f.Name != mono) mono = FontFamily.GenericMonospace.Name; } catch { mono = FontFamily.GenericMonospace.Name; }
            box.Font = new Font(mono, 11.5f);
            box.BackColor = Theme.Panel2; box.ForeColor = Color.White;
            box.Text = (text ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n");
            box.MaxLength = 0;
            holder.Controls.Add(box);
            Controls.Add(holder);

            if (!string.IsNullOrEmpty(help))
            {
                var l = new Label();
                l.Text = help; l.Dock = DockStyle.Top; l.AutoSize = false;
                l.Font = Theme.Font(9.5f);
                l.Height = Theme.S(20) * (help.Split('\n').Length) + Theme.S(18);
                l.Padding = new Padding(Theme.S(14), Theme.S(10), Theme.S(14), Theme.S(6));
                l.ForeColor = Theme.Muted; l.BackColor = Theme.Bg;
                Controls.Add(l);
            }

            var foot = new ToolBarPanel();
            foot.Dock = DockStyle.Bottom;
            var ok = new FlatButton("Save", Glyph.Check, true);
            ok.DialogResult = DialogResult.OK;
            var cancel = new FlatButton("Cancel", Glyph.None, false);
            cancel.DialogResult = DialogResult.Cancel;
            foot.AddRight(cancel); foot.AddRight(ok);
            Controls.Add(foot);
            CancelButton = cancel;
            Shown += delegate { box.SelectionStart = 0; box.SelectionLength = 0; box.Focus(); };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S) || keyData == (Keys.Control | Keys.Enter)) { DialogResult = DialogResult.OK; return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public static string Show(IWin32Window owner, string title, string help, string text)
        {
            using (var d = new TextEditDialog(title, help, text))
            {
                var f = owner as Form;
                if (f != null && f.TopMost) d.TopMost = true;
                return d.ShowDialog(owner) == DialogResult.OK ? d.box.Text : null;
            }
        }
    }

    static class InputDialog
    {
        public static string Ask(IWin32Window owner, string title, string prompt, string value)
        {
            using (var d = new BaseForm())
            {
                d.Text = title;
                d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MinimizeBox = false; d.MaximizeBox = false; d.ShowInTaskbar = false;
                d.StartPosition = FormStartPosition.CenterParent;
                d.ClientSize = new Size(Theme.S(440), Theme.S(150));
                var f = owner as Form;
                if (f != null && f.TopMost) d.TopMost = true;
                var l = new Label(); l.Text = prompt; l.ForeColor = Theme.Muted; l.Font = Theme.Font(10f); l.SetBounds(Theme.S(16), Theme.S(14), Theme.S(408), Theme.S(22));
                var field = new TextField(value ?? "", 408); field.Location = new Point(Theme.S(16), Theme.S(40));
                field.Box.Font = Theme.Font(12f);
                var ok = new FlatButton("OK", Glyph.None, true); ok.AutoSize = false; ok.SetBounds(Theme.S(240), Theme.S(98), Theme.S(88), Theme.S(36));
                var cancel = new FlatButton("Cancel", Glyph.None, false); cancel.AutoSize = false; cancel.SetBounds(Theme.S(336), Theme.S(98), Theme.S(88), Theme.S(36));
                ok.DialogResult = DialogResult.OK; cancel.DialogResult = DialogResult.Cancel;
                d.Controls.AddRange(new Control[] { l, field, ok, cancel });
                d.AcceptButton = ok; d.CancelButton = cancel;
                d.Shown += delegate { field.Box.SelectAll(); field.Box.Focus(); };
                return d.ShowDialog(owner) == DialogResult.OK ? field.Box.Text.Trim() : null;
            }
        }
    }

    // Short tones generated in memory, so no sound files need to ship with the program.
    static class Sound
    {
        static System.Media.SoundPlayer player;
        static MemoryStream current;

        public static void Tones(params int[] freqAndMs)
        {
            try
            {
                const int rate = 22050;
                var pcm = new MemoryStream();
                var w = new BinaryWriter(pcm);
                for (int i = 0; i + 1 < freqAndMs.Length; i += 2)
                {
                    int n = rate * freqAndMs[i + 1] / 1000;
                    for (int k = 0; k < n; k++)
                    {
                        double env = Math.Min(1.0, Math.Min(k, n - k) / (rate * 0.01));
                        double v = freqAndMs[i] <= 0 ? 0 : Math.Sin(2 * Math.PI * freqAndMs[i] * k / rate) * env * 0.5;
                        w.Write((short)(v * short.MaxValue));
                    }
                }
                w.Flush();
                byte[] data = pcm.ToArray();
                var wav = new MemoryStream();
                var o = new BinaryWriter(wav);
                o.Write(new[] { 'R', 'I', 'F', 'F' }); o.Write(36 + data.Length); o.Write(new[] { 'W', 'A', 'V', 'E' });
                o.Write(new[] { 'f', 'm', 't', ' ' }); o.Write(16); o.Write((short)1); o.Write((short)1);
                o.Write(rate); o.Write(rate * 2); o.Write((short)2); o.Write((short)16);
                o.Write(new[] { 'd', 'a', 't', 'a' }); o.Write(data.Length); o.Write(data);
                o.Flush();
                wav.Position = 0;
                if (!Env.IsWindows) return;
                if (player != null) { player.Stop(); player.Dispose(); }
                if (current != null) current.Dispose();
                current = wav;
                player = new System.Media.SoundPlayer(wav);
                player.Play();
            }
            catch { }
        }

        public static void Blip() { Tones(880, 90); }
        public static void Change() { Tones(660, 140, 0, 40, 880, 220); }
        public static void Alarm() { Tones(880, 220, 0, 90, 880, 220, 0, 90, 880, 220, 0, 250, 1175, 500); }
        public static void Good() { Tones(660, 110, 880, 110, 1175, 220); }
        public static void Bad() { Tones(300, 180, 220, 320); }
    }

    static partial class Native
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static void DpiAware()
        {
            if (!Env.IsWindows) return;
            try { SetProcessDPIAware(); } catch { }
        }

        // Lets a borderless window be dragged by any of its controls.
        public static void DragWindow(Form f)
        {
            if (!Env.IsWindows) return;
            try { ReleaseCapture(); SendMessage(f.Handle, 0xA1, (IntPtr)2, IntPtr.Zero); } catch { }
        }
    }
}
