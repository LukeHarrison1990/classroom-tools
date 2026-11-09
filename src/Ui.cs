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
            for (int i = 0; i < 9; i++)
            {
                float mid = (lo + hi) / 2f;
                using (var f = new Font(Theme.FontName, mid, style, GraphicsUnit.Point))
                {
                    SizeF sz = wrap ? g.MeasureString(text, f, (int)box.Width) : g.MeasureString(text, f);
                    if (sz.Width <= box.Width && sz.Height <= box.Height) lo = mid; else hi = mid;
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

    // Base window used by every tool: a toolbar along the top and a painted canvas below.
    class ToolForm : Form
    {
        protected FlowLayoutPanel Bar;
        protected Canvas View;
        protected Timer Tick;
        bool full;
        FormBorderStyle oldBorder;
        FormWindowState oldState;
        Rectangle oldBounds;
        Button pinButton;

        public ToolForm(string title, int w, int h)
        {
            Text = title;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Font(10f);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(w), Theme.S(h));
            MinimumSize = new Size(Theme.S(320), Theme.S(240));
            KeyPreview = true;
            Icon = AppIcon.Get();

            View = new Canvas();
            View.Dock = DockStyle.Fill;
            View.Paint += delegate(object s, PaintEventArgs e)
            {
                Draw.Quality(e.Graphics);
                try { Render(e.Graphics, View.ClientRectangle); }
                catch (Exception ex) { Draw.Label(e.Graphics, ex.Message, View.ClientRectangle, Theme.Red, 10f, FontStyle.Regular, StringAlignment.Center); }
            };
            Controls.Add(View);

            Bar = new FlowLayoutPanel();
            Bar.Dock = DockStyle.Top;
            Bar.AutoSize = true;
            Bar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Bar.WrapContents = true;
            Bar.BackColor = Theme.Panel;
            Bar.Padding = new Padding(Theme.S(6), Theme.S(5), Theme.S(6), Theme.S(5));
            Controls.Add(Bar);

            Tick = new Timer();
            Tick.Interval = 100;
            Tick.Tick += delegate { OnTick(); };
            FormClosed += delegate { Tick.Stop(); Tick.Dispose(); };
            Shown += delegate { View.Focus(); };
        }

        protected virtual void Render(Graphics g, Rectangle r) { }
        protected virtual void OnTick() { View.Invalidate(); }
        protected virtual bool OnToolKey(Keys key) { return false; }

        public static Button MakeButton(string text, bool primary)
        {
            var b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = primary ? Theme.Accent : Theme.Panel2;
            b.ForeColor = Color.White;
            b.Font = Theme.Font(10f, primary ? FontStyle.Bold : FontStyle.Regular);
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(Theme.S(8), Theme.S(2), Theme.S(8), Theme.S(2));
            b.MinimumSize = new Size(Theme.S(36), Theme.S(32));
            b.Margin = new Padding(Theme.S(3));
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
            b.UseVisualStyleBackColor = false;
            return b;
        }

        protected Button AddButton(string text, EventHandler click) { return AddButton(text, click, false); }

        protected Button AddButton(string text, EventHandler click, bool primary)
        {
            var b = MakeButton(text, primary);
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
            l.Font = Theme.Font(10f);
            l.Margin = new Padding(Theme.S(8), Theme.S(10), Theme.S(2), 0);
            Bar.Controls.Add(l);
            return l;
        }

        protected NumericUpDown AddNumber(int min, int max, int value, int width)
        {
            var n = new NumericUpDown();
            n.Minimum = min; n.Maximum = max;
            n.Value = Math.Max(min, Math.Min(max, value));
            n.Width = Theme.S(width);
            n.Font = Theme.Font(11f);
            n.BackColor = Theme.Panel2; n.ForeColor = Color.White;
            n.BorderStyle = BorderStyle.FixedSingle;
            n.Margin = new Padding(Theme.S(3), Theme.S(6), Theme.S(3), Theme.S(3));
            n.TextAlign = HorizontalAlignment.Center;
            Bar.Controls.Add(n);
            return n;
        }

        protected CheckBox AddCheck(string text, bool on)
        {
            var c = new CheckBox();
            c.Text = text; c.Checked = on; c.AutoSize = true;
            c.ForeColor = Theme.Text; c.Font = Theme.Font(10f);
            c.Margin = new Padding(Theme.S(8), Theme.S(9), Theme.S(4), 0);
            c.TabStop = false;
            Bar.Controls.Add(c);
            return c;
        }

        protected ComboBox AddCombo(int width, params string[] items)
        {
            var c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Width = Theme.S(width);
            c.Font = Theme.Font(10.5f);
            c.BackColor = Theme.Panel2; c.ForeColor = Color.White;
            c.FlatStyle = FlatStyle.Flat;
            c.Margin = new Padding(Theme.S(3), Theme.S(6), Theme.S(3), Theme.S(3));
            c.Items.AddRange(items);
            if (items.Length > 0) c.SelectedIndex = 0;
            c.TabStop = false;
            Bar.Controls.Add(c);
            return c;
        }

        protected TextBox AddText(string text, int width)
        {
            var t = new TextBox();
            t.Text = text; t.Width = Theme.S(width);
            t.Font = Theme.Font(11f);
            t.BackColor = Theme.Panel2; t.ForeColor = Color.White;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Margin = new Padding(Theme.S(3), Theme.S(6), Theme.S(3), Theme.S(3));
            Bar.Controls.Add(t);
            return t;
        }

        // "Keep on top" and "Full screen" buttons, added at the end of the toolbar.
        protected void AddWindowButtons(bool startOnTop)
        {
            pinButton = AddButton("On top", delegate { SetPinned(!TopMost); });
            AddButton("Full screen (F11)", delegate { ToggleFull(); });
            SetPinned(startOnTop);
        }

        protected void SetPinned(bool on)
        {
            TopMost = on;
            if (pinButton != null) pinButton.BackColor = on ? Theme.Accent : Theme.Panel2;
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
            }
            else
            {
                FormBorderStyle = oldBorder;
                Bounds = oldBounds;
                WindowState = oldState;
                full = false;
            }
        }

        static bool IsTyping(Control c)
        {
            while (c is ContainerControl && ((ContainerControl)c).ActiveControl != null) c = ((ContainerControl)c).ActiveControl;
            return c is TextBoxBase || c is NumericUpDown || c is UpDownBase || (c != null && c.Parent is UpDownBase);
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
        static Icon icon;
        public static Icon Get()
        {
            if (icon != null) return icon;
            try
            {
                icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            if (icon == null)
            {
                try
                {
                    using (var bmp = new Bitmap(32, 32))
                    {
                        using (var g = Graphics.FromImage(bmp))
                        {
                            Draw.Quality(g);
                            Draw.FillRound(g, new RectangleF(1, 1, 30, 30), 7, Theme.Accent);
                            using (var b = new SolidBrush(Color.White))
                            {
                                g.FillEllipse(b, 12, 4, 8, 8);
                                g.FillEllipse(b, 12, 12, 8, 8);
                                g.FillEllipse(b, 12, 20, 8, 8);
                            }
                        }
                        icon = Icon.FromHandle(bmp.GetHicon());
                    }
                }
                catch { icon = SystemIcons.Application; }
            }
            return icon;
        }
    }

    // Multi-line text editor used for class lists, question sets and similar files.
    class TextEditDialog : Form
    {
        TextBox box;

        TextEditDialog(string title, string help, string text)
        {
            Text = title;
            BackColor = Theme.Panel; ForeColor = Theme.Text;
            Font = Theme.Font(10f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(640), Theme.S(520));
            MinimizeBox = false; ShowInTaskbar = false;
            Icon = AppIcon.Get();

            box = new TextBox();
            box.Multiline = true; box.ScrollBars = ScrollBars.Vertical; box.AcceptsReturn = true; box.AcceptsTab = false;
            box.Dock = DockStyle.Fill;
            box.Font = new Font(FontFamily.GenericMonospace, 11f);
            box.BackColor = Color.White; box.ForeColor = Color.Black;
            box.Text = (text ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n");
            box.MaxLength = 0;
            Controls.Add(box);

            if (!string.IsNullOrEmpty(help))
            {
                var l = new Label();
                l.Text = help; l.Dock = DockStyle.Top; l.AutoSize = false;
                l.Height = Theme.S(22) * (help.Split('\n').Length) + Theme.S(12);
                l.Padding = new Padding(Theme.S(10), Theme.S(8), Theme.S(10), Theme.S(4));
                l.ForeColor = Theme.Text;
                Controls.Add(l);
            }

            var foot = new FlowLayoutPanel();
            foot.Dock = DockStyle.Bottom; foot.FlowDirection = FlowDirection.RightToLeft;
            foot.AutoSize = true; foot.Padding = new Padding(Theme.S(6));
            var ok = ToolForm.MakeButton("Save", true);
            ok.Click += delegate { DialogResult = DialogResult.OK; };
            var cancel = ToolForm.MakeButton("Cancel", false);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            foot.Controls.Add(cancel); foot.Controls.Add(ok);
            Controls.Add(foot);
            CancelButton = cancel;
            Shown += delegate { box.SelectionStart = 0; box.SelectionLength = 0; box.Focus(); };
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
            using (var d = new Form())
            {
                d.Text = title; d.BackColor = Theme.Panel; d.ForeColor = Theme.Text; d.Font = Theme.Font(10.5f);
                d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MinimizeBox = false; d.MaximizeBox = false; d.ShowInTaskbar = false;
                d.StartPosition = FormStartPosition.CenterParent;
                d.ClientSize = new Size(Theme.S(420), Theme.S(140));
                var f = owner as Form;
                if (f != null && f.TopMost) d.TopMost = true;
                var l = new Label(); l.Text = prompt; l.SetBounds(Theme.S(14), Theme.S(12), Theme.S(392), Theme.S(24));
                var t = new TextBox(); t.Text = value ?? ""; t.SetBounds(Theme.S(14), Theme.S(42), Theme.S(392), Theme.S(28)); t.Font = Theme.Font(12f);
                var ok = ToolForm.MakeButton("OK", true); ok.AutoSize = false; ok.SetBounds(Theme.S(226), Theme.S(92), Theme.S(86), Theme.S(34));
                var cancel = ToolForm.MakeButton("Cancel", false); cancel.AutoSize = false; cancel.SetBounds(Theme.S(320), Theme.S(92), Theme.S(86), Theme.S(34));
                ok.Click += delegate { d.DialogResult = DialogResult.OK; };
                cancel.Click += delegate { d.DialogResult = DialogResult.Cancel; };
                d.Controls.AddRange(new Control[] { l, t, ok, cancel });
                d.AcceptButton = ok; d.CancelButton = cancel;
                return d.ShowDialog(owner) == DialogResult.OK ? t.Text.Trim() : null;
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

    static class Native
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
