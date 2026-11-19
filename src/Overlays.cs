// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - tools that sit over other programs: draw on screen, spotlight, reading guide, cover-up.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    // Freezes a picture of the screen and lets the teacher draw on it.
    class AnnotateForm : Form
    {
        class Stroke { public List<Point> Points = new List<Point>(); public Color Color; public float Width; }

        readonly Bitmap shot;
        readonly List<Stroke> strokes = new List<Stroke>();
        Stroke current;
        Color pen = Theme.Red;
        float width = 6;
        bool highlighter;
        readonly ToolBarPanel tools;
        readonly List<Swatch> swatches = new List<Swatch>(), sizes = new List<Swatch>();
        int sizeIndex = 1;
        FlatButton hiBtn;

        public AnnotateForm()
        {
            Text = "Draw on Screen";
            Screen screen = Screen.FromPoint(Cursor.Position);
            Rectangle b = screen.Bounds;
            shot = new Bitmap(b.Width, b.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (var g = Graphics.FromImage(shot)) g.CopyFromScreen(b.Location, Point.Empty, b.Size);
            }
            catch
            {
                using (var g = Graphics.FromImage(shot)) g.Clear(Color.White);
            }
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = b;
            TopMost = true; ShowInTaskbar = false; KeyPreview = true;
            Cursor = Cursors.Cross;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            tools = new ToolBarPanel();
            tools.Cursor = Cursors.Default;
            Color[] colours = { Color.FromArgb(239, 68, 68), Color.FromArgb(59, 130, 246), Color.FromArgb(34, 197, 94), Color.FromArgb(250, 204, 21), Color.FromArgb(17, 17, 17), Color.White };
            foreach (Color c in colours)
            {
                var sw = new Swatch(c, 0);
                sw.Click += delegate { pen = sw.Colour; highlighter = false; Refresh(swatches, sw); Refresh(sizes, sizes[sizeIndex]); hiBtn.Toggled = false; };
                swatches.Add(sw);
                tools.Controls.Add(sw);
            }
            tools.Controls.Add(new Divider());
            float[] widths = { 3, 6, 12 };
            for (int i = 0; i < widths.Length; i++)
            {
                int k = i;
                var sz = new Swatch(Color.White, widths[i]);
                sz.Tip = new[] { "Thin pen", "Medium pen", "Thick pen" }[i];
                sz.Click += delegate { width = widths[k]; sizeIndex = k; highlighter = false; hiBtn.Toggled = false; Refresh(sizes, sz); };
                sizes.Add(sz);
                tools.Controls.Add(sz);
            }
            hiBtn = Add("Highlighter", Glyph.Edit, delegate { highlighter = !highlighter; hiBtn.Toggled = highlighter; Refresh(sizes, highlighter ? null : sizes[sizeIndex]); });
            tools.Controls.Add(new Divider());
            AddIcon(Glyph.Undo, "Undo (Ctrl+Z)", delegate { if (strokes.Count > 0) strokes.RemoveAt(strokes.Count - 1); Invalidate(); });
            AddIcon(Glyph.Trash, "Clear all drawing", delegate { strokes.Clear(); Invalidate(); });
            Add("Whiteboard", Glyph.None, delegate { using (var g = Graphics.FromImage(shot)) g.Clear(Color.White); strokes.Clear(); Invalidate(); }).Tip = "Swap the frozen screen for a blank white page";
            tools.Controls.Add(new Divider());
            AddIcon(Glyph.Save, "Save as a picture", delegate { SavePicture(); });
            AddIcon(Glyph.Copy, "Copy to the clipboard", delegate { using (var bmp = Compose()) Clipboard.SetImage(bmp); });
            var close = new FlatButton("Done", Glyph.Check, true);
            close.Tip = "Close and return to the screen (Esc)";
            close.Click += delegate { Close(); };
            tools.Controls.Add(close);
            Refresh(swatches, swatches[0]);
            Refresh(sizes, sizes[1]);
            Controls.Add(tools);
            // Size the floating toolbar to its contents and round its corners.
            tools.Width = Math.Min(b.Width - Theme.S(20), tools.SingleRowWidth());
            tools.PerformLayout();
            tools.Location = new Point(Math.Max(0, (b.Width - tools.Width) / 2), Theme.S(12));
            using (var path = Draw.Round(new RectangleF(0, 0, tools.Width, tools.Height), Theme.S(14))) tools.Region = new Region(path);

            MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                current = new Stroke();
                current.Color = highlighter ? Color.FromArgb(110, 250, 220, 30) : pen;
                current.Width = (highlighter ? 26 : width) * Theme.Scale;
                current.Points.Add(e.Location);
                strokes.Add(current);
            };
            MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (current == null) return;
                current.Points.Add(e.Location);
                Invalidate();
            };
            MouseUp += delegate { current = null; };
            FormClosed += delegate { shot.Dispose(); };
        }

        FlatButton Add(string text, Glyph glyph, EventHandler click)
        {
            var b = new FlatButton(text, glyph, false);
            b.Click += click;
            tools.Controls.Add(b);
            return b;
        }

        void AddIcon(Glyph glyph, string tip, EventHandler click)
        {
            var b = new FlatButton("", glyph, false);
            b.Tip = tip;
            b.Click += click;
            tools.Controls.Add(b);
        }

        static void Refresh(List<Swatch> group, Swatch chosen)
        {
            foreach (var s in group) s.Chosen = s == chosen;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            if (keyData == (Keys.Control | Keys.Z)) { if (strokes.Count > 0) strokes.RemoveAt(strokes.Count - 1); Invalidate(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void PaintAll(Graphics g)
        {
            g.DrawImageUnscaled(shot, 0, 0);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var s in strokes)
            {
                using (var p = new Pen(s.Color, s.Width))
                {
                    p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                    if (s.Points.Count > 1) g.DrawLines(p, s.Points.ToArray());
                    else if (s.Points.Count == 1)
                        using (var b = new SolidBrush(s.Color)) g.FillEllipse(b, s.Points[0].X - s.Width / 2, s.Points[0].Y - s.Width / 2, s.Width, s.Width);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) { PaintAll(e.Graphics); }

        Bitmap Compose()
        {
            var bmp = new Bitmap(shot.Width, shot.Height);
            using (var g = Graphics.FromImage(bmp)) PaintAll(g);
            return bmp;
        }

        void SavePicture()
        {
            TopMost = false;
            using (var d = new SaveFileDialog())
            {
                d.Filter = "PNG picture (*.png)|*.png";
                d.FileName = "Screen " + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".png";
                if (d.ShowDialog(this) == DialogResult.OK)
                    using (var bmp = Compose()) bmp.Save(d.FileName, ImageFormat.Png);
            }
            TopMost = true;
        }
    }

    // Small floating control strip for overlays that the mouse passes straight through.
    class OverlayRemote : Form
    {
        public OverlayRemote(string title, Form overlay, Action smaller, Action bigger, Action colour)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Panel;
            var bar = new ToolBarPanel();
            bar.Cursor = Cursors.SizeAll;
            bar.MouseDown += delegate { Native.DragWindow(this); };
            var label = new Label();
            label.Text = title; label.AutoSize = true; label.ForeColor = Theme.Text; label.Font = Theme.Semi(10f);
            label.Margin = new Padding(Theme.S(6), 0, Theme.S(6), 0);
            label.Cursor = Cursors.SizeAll;
            label.MouseDown += delegate { Native.DragWindow(this); };
            bar.Controls.Add(label);
            if (smaller != null) { var b = new FlatButton("", Glyph.Minus, false); b.Tip = "Smaller"; b.Click += delegate { smaller(); }; bar.Controls.Add(b); }
            if (bigger != null) { var b = new FlatButton("", Glyph.Plus, false); b.Tip = "Bigger"; b.Click += delegate { bigger(); }; bar.Controls.Add(b); }
            if (colour != null) { var b = new FlatButton("Colour", Glyph.None, false); b.Click += delegate { colour(); }; bar.Controls.Add(b); }
            var close = new FlatButton("", Glyph.Close, true);
            close.Tip = "Close " + title.ToLowerInvariant();
            close.Click += delegate { overlay.Close(); };
            bar.Controls.Add(close);
            bar.Width = bar.SingleRowWidth();
            bar.PerformLayout();
            bar.Location = Point.Empty;
            Controls.Add(bar);
            ClientSize = bar.Size;
            using (var path = Draw.Round(new RectangleF(0, 0, Width, Height), Theme.S(14))) Region = new Region(path);
            var wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - Theme.S(20), wa.Bottom - Height - Theme.S(20));
            overlay.FormClosed += delegate { if (!IsDisposed) Close(); };
        }
    }

    class ClickThroughForm : Form
    {
        protected readonly Timer Follow = new Timer();

        public ClickThroughForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Follow.Interval = 25;
            Follow.Tick += delegate { Track(); };
            FormClosed += delegate { Follow.Dispose(); };
            Shown += delegate { Follow.Start(); };
        }

        protected virtual void Track() { }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // layered + transparent to the mouse + tool window + never takes focus
                if (Env.IsWindows) cp.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x08000000;
                return cp;
            }
        }
    }

    // Dims the whole screen except a circle around the mouse pointer.
    class SpotlightForm : ClickThroughForm
    {
        int radius;
        Point last = new Point(-9999, -9999);
        int lastRadius;

        public SpotlightForm()
        {
            Text = "Spotlight";
            BackColor = Color.Black;
            Opacity = 0.72;
            Bounds = SystemInformation.VirtualScreen;
            radius = Store.GetInt("spotlight.radius", Theme.S(160));
            var remote = new OverlayRemote("Spotlight", this,
                delegate { radius = Math.Max(Theme.S(50), radius - Theme.S(25)); Store.Set("spotlight.radius", radius); },
                delegate { radius = Math.Min(Theme.S(700), radius + Theme.S(25)); Store.Set("spotlight.radius", radius); }, null);
            Shown += delegate { remote.Show(); };
        }

        protected override void Track()
        {
            Point p = Cursor.Position;
            if (p == last && radius == lastRadius) return;
            last = p; lastRadius = radius;
            p.Offset(-Left, -Top);
            using (var path = new GraphicsPath())
            {
                path.AddRectangle(new Rectangle(0, 0, Width, Height));
                path.AddEllipse(p.X - radius, p.Y - radius, radius * 2, radius * 2);
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }
    }

    // A coloured band that follows the mouse up and down, to help readers keep their place.
    class ReadingGuideForm : ClickThroughForm
    {
        int band;
        int colour;
        static readonly Color[] Colours = { Color.FromArgb(250, 204, 21), Color.FromArgb(56, 189, 248), Color.FromArgb(74, 222, 128), Color.FromArgb(244, 114, 182), Color.FromArgb(40, 40, 40) };

        public ReadingGuideForm()
        {
            Text = "Reading Guide";
            Opacity = 0.35;
            band = Store.GetInt("guide.height", Theme.S(44));
            colour = Store.GetInt("guide.colour", 0) % Colours.Length;
            BackColor = Colours[colour];
            var remote = new OverlayRemote("Reading guide", this,
                delegate { band = Math.Max(Theme.S(12), band - Theme.S(8)); Store.Set("guide.height", band); },
                delegate { band = Math.Min(Theme.S(300), band + Theme.S(8)); Store.Set("guide.height", band); },
                delegate { colour = (colour + 1) % Colours.Length; BackColor = Colours[colour]; Store.Set("guide.colour", colour); });
            Shown += delegate { remote.Show(); };
            Track();
        }

        protected override void Track()
        {
            Point p = Cursor.Position;
            Rectangle s = Screen.FromPoint(p).Bounds;
            var want = new Rectangle(s.Left, p.Y - band / 2, s.Width, band);
            if (Bounds != want) Bounds = want;
        }
    }

    // A solid panel to hide part of the screen. Drag to move, drag the edges to resize.
    class CoverForm : Form
    {
        static readonly Color[] Colours = { Color.FromArgb(24, 28, 36), Color.White, Theme.Accent, Color.FromArgb(20, 110, 70), Color.FromArgb(250, 204, 21) };
        int colour;

        public CoverForm()
        {
            Text = "Cover-up";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual;
            var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            Bounds = new Rectangle(wa.Left + wa.Width / 4, wa.Top + wa.Height / 4, wa.Width / 2, wa.Height / 2);
            MinimumSize = new Size(Theme.S(60), Theme.S(40));
            BackColor = Colours[0];
            SetStyle(ControlStyles.ResizeRedraw, true);
            var menu = new ContextMenuStrip();
            menu.Items.Add("Change colour", null, delegate { colour = (colour + 1) % Colours.Length; BackColor = Colours[colour]; Invalidate(); });
            menu.Items.Add("Cover whole screen", null, delegate { Bounds = Screen.FromControl(this).Bounds; });
            menu.Items.Add("Another cover", null, delegate { new CoverForm().Show(); });
            menu.Items.Add("Close", null, delegate { Close(); });
            Menus.Style(menu);
            ContextMenuStrip = menu;
            // Dragging hands the mouse to Windows as a title-bar drag, so the second click of a
            // double-click arrives as a title-bar double-click (handled in WndProc), not a DoubleClick event.
            MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                if (e.Clicks >= 2) { Close(); return; }
                Native.DragWindow(this);
            };
            KeyPreview = true;
            KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color ink = BackColor.GetBrightness() > 0.6f ? Color.FromArgb(120, 0, 0, 0) : Color.FromArgb(120, 255, 255, 255);
            Draw.Label(e.Graphics, "Drag to move  •  drag an edge to resize  •  double-click or Esc to close  •  right-click for options",
                new RectangleF(0, Height - Theme.S(30), Width, Theme.S(26)), ink, 9f, FontStyle.Regular, StringAlignment.Center);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0xA3 && m.WParam.ToInt64() == 2) { BeginInvoke((MethodInvoker)Close); return; } // WM_NCLBUTTONDBLCLK: double-click to close
            base.WndProc(ref m);
            if (m.Msg == 0x84 && Env.IsWindows) // WM_NCHITTEST: make the borders resizable
            {
                long lp = m.LParam.ToInt64();
                Point p = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                int e = Theme.S(10);
                bool l = p.X < e, r = p.X > Width - e, t = p.Y < e, b = p.Y > Height - e;
                int hit = t && l ? 13 : t && r ? 14 : b && l ? 16 : b && r ? 17 : l ? 10 : r ? 11 : t ? 12 : b ? 15 : 0;
                if (hit != 0) m.Result = (IntPtr)hit;
            }
        }
    }
}
