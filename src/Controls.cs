// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - modern owner-drawn controls shared by every tool:
// icon buttons, steppers, switches, drop-downs, the toolbar layout, themed menus and dark title bars.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    enum Glyph
    {
        None, Play, Pause, Stop, Reset, Pin, FullScreen, ExitFullScreen, Plus, Minus, Edit, Folder, Copy,
        Shuffle, Next, Prev, ChevronDown, Check, Close, SoundOn, SoundOff, Flag, Settings, Swap, Dice, Users, Speak, Save, Undo, Trash, Clock, Lights, Grid, Note, Spot, Card, Breath, Quote, Bell, Text
    }

    // Line icons drawn as vectors on a 24-unit grid, so they are sharp at any size and need no image files.
    static class Glyphs
    {
        public static void Draw(Graphics g, Glyph k, RectangleF r, Color c)
        {
            if (k == Glyph.None) return;
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(r.Width, r.Height) / 24f;
            float ox = r.X + (r.Width - 24 * s) / 2f, oy = r.Y + (r.Height - 24 * s) / 2f;
            Func<float, float, PointF> P = (x, y) => new PointF(ox + x * s, oy + y * s);
            Func<float, float, float, float, RectangleF> R = (x, y, w, h) => new RectangleF(ox + x * s, oy + y * s, w * s, h * s);
            using (var pen = new Pen(c, Math.Max(1.2f, 2f * s)))
            using (var fill = new SolidBrush(c))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                switch (k)
                {
                    case Glyph.Play:
                        g.FillPolygon(fill, new[] { P(7, 4.5f), P(19.5f, 12), P(7, 19.5f) });
                        break;
                    case Glyph.Pause:
                        FillRound(g, fill, R(6, 4.5f, 4.2f, 15), 1.2f * s);
                        FillRound(g, fill, R(13.8f, 4.5f, 4.2f, 15), 1.2f * s);
                        break;
                    case Glyph.Stop:
                        FillRound(g, fill, R(6, 6, 12, 12), 2 * s);
                        break;
                    case Glyph.Reset:
                        Arrow(g, pen, fill, ox, oy, s, 12, 12, 7.5f, 210, 280);
                        break;
                    case Glyph.Swap:
                        g.DrawLine(pen, P(4, 8), P(19, 8)); g.DrawLines(pen, new[] { P(15, 4), P(19, 8), P(15, 12) });
                        g.DrawLine(pen, P(20, 16), P(5, 16)); g.DrawLines(pen, new[] { P(9, 12), P(5, 16), P(9, 20) });
                        break;
                    case Glyph.Pin:
                        g.DrawPolygon(pen, new[] { P(9, 3.5f), P(15, 3.5f), P(14.5f, 9.5f), P(18, 13.5f), P(6, 13.5f), P(9.5f, 9.5f) });
                        g.DrawLine(pen, P(12, 13.5f), P(12, 20.5f));
                        break;
                    case Glyph.FullScreen:
                        g.DrawLines(pen, new[] { P(4, 9), P(4, 4), P(9, 4) }); g.DrawLines(pen, new[] { P(15, 4), P(20, 4), P(20, 9) });
                        g.DrawLines(pen, new[] { P(20, 15), P(20, 20), P(15, 20) }); g.DrawLines(pen, new[] { P(9, 20), P(4, 20), P(4, 15) });
                        break;
                    case Glyph.ExitFullScreen:
                        g.DrawLines(pen, new[] { P(4, 9), P(9, 9), P(9, 4) }); g.DrawLines(pen, new[] { P(15, 4), P(15, 9), P(20, 9) });
                        g.DrawLines(pen, new[] { P(20, 15), P(15, 15), P(15, 20) }); g.DrawLines(pen, new[] { P(9, 20), P(9, 15), P(4, 15) });
                        break;
                    case Glyph.Plus:
                        g.DrawLine(pen, P(12, 5), P(12, 19)); g.DrawLine(pen, P(5, 12), P(19, 12));
                        break;
                    case Glyph.Minus:
                        g.DrawLine(pen, P(5, 12), P(19, 12));
                        break;
                    case Glyph.Edit:
                        g.DrawPolygon(pen, new[] { P(15.5f, 4), P(20, 8.5f), P(9, 19.5f), P(4, 20), P(4.5f, 15) });
                        g.DrawLine(pen, P(13, 6.5f), P(17.5f, 11));
                        break;
                    case Glyph.Folder:
                        g.DrawPolygon(pen, new[] { P(3, 6), P(9.5f, 6), P(11.5f, 8.5f), P(21, 8.5f), P(21, 19), P(3, 19) });
                        break;
                    case Glyph.Copy:
                        using (var p = RoundPath(R(9, 9, 11, 11), 2 * s)) g.DrawPath(pen, p);
                        g.DrawLines(pen, new[] { P(15, 5.5f), P(15, 4), P(4, 4), P(4, 15), P(5.5f, 15) });
                        break;
                    case Glyph.Shuffle:
                        g.DrawLines(pen, new[] { P(3, 7), P(7.5f, 7), P(15.5f, 17), P(20, 17) });
                        g.DrawLines(pen, new[] { P(3, 17), P(7.5f, 17), P(15.5f, 7), P(20, 7) });
                        g.DrawLines(pen, new[] { P(17, 4), P(20, 7), P(17, 10) }); g.DrawLines(pen, new[] { P(17, 14), P(20, 17), P(17, 20) });
                        break;
                    case Glyph.Next:
                        g.DrawLines(pen, new[] { P(9, 5), P(16, 12), P(9, 19) });
                        break;
                    case Glyph.Prev:
                        g.DrawLines(pen, new[] { P(15, 5), P(8, 12), P(15, 19) });
                        break;
                    case Glyph.ChevronDown:
                        g.DrawLines(pen, new[] { P(6, 9), P(12, 15), P(18, 9) });
                        break;
                    case Glyph.Check:
                        g.DrawLines(pen, new[] { P(4.5f, 12.5f), P(9.5f, 17.5f), P(19.5f, 6.5f) });
                        break;
                    case Glyph.Close:
                        g.DrawLine(pen, P(6, 6), P(18, 18)); g.DrawLine(pen, P(18, 6), P(6, 18));
                        break;
                    case Glyph.SoundOn:
                    case Glyph.SoundOff:
                        g.FillPolygon(fill, new[] { P(3, 9), P(7, 9), P(12, 4.5f), P(12, 19.5f), P(7, 15), P(3, 15) });
                        if (k == Glyph.SoundOn)
                        {
                            g.DrawArc(pen, R(9, 8, 8, 8), -50, 100);
                            g.DrawArc(pen, R(7, 4.5f, 15, 15), -50, 100);
                        }
                        else { g.DrawLine(pen, P(15.5f, 9), P(21, 15)); g.DrawLine(pen, P(21, 9), P(15.5f, 15)); }
                        break;
                    case Glyph.Flag:
                        g.DrawLine(pen, P(6, 3.5f), P(6, 21));
                        g.DrawPolygon(pen, new[] { P(6, 4.5f), P(18.5f, 4.5f), P(15.5f, 8.5f), P(18.5f, 12.5f), P(6, 12.5f) });
                        break;
                    case Glyph.Settings:
                        for (int i = 0; i < 8; i++)
                        {
                            double a = i * Math.PI / 4;
                            g.DrawLine(pen, P(12 + 6.5f * (float)Math.Cos(a), 12 + 6.5f * (float)Math.Sin(a)), P(12 + 9 * (float)Math.Cos(a), 12 + 9 * (float)Math.Sin(a)));
                        }
                        g.DrawEllipse(pen, R(5.5f, 5.5f, 13, 13));
                        g.DrawEllipse(pen, R(9.5f, 9.5f, 5, 5));
                        break;
                    case Glyph.Dice:
                        using (var p = RoundPath(R(4, 4, 16, 16), 3.5f * s)) g.DrawPath(pen, p);
                        g.FillEllipse(fill, R(7.5f, 7.5f, 3, 3)); g.FillEllipse(fill, R(13.5f, 13.5f, 3, 3));
                        break;
                    case Glyph.Users:
                        g.DrawEllipse(pen, R(5, 4, 7, 7));
                        g.DrawArc(pen, R(2.5f, 13, 12, 12), 180, 180);
                        g.DrawArc(pen, R(13, 5, 6, 6), -150, 260);
                        g.DrawArc(pen, R(13, 13.5f, 9, 10), 230, 130);
                        break;
                    case Glyph.Save:
                        g.DrawLine(pen, P(12, 3.5f), P(12, 14.5f)); g.DrawLines(pen, new[] { P(7.5f, 10), P(12, 14.5f), P(16.5f, 10) });
                        g.DrawLines(pen, new[] { P(4, 15), P(4, 20), P(20, 20), P(20, 15) });
                        break;
                    case Glyph.Undo:
                        g.DrawLines(pen, new[] { P(8, 4.5f), P(4, 8.5f), P(8, 12.5f) });
                        g.DrawLine(pen, P(4, 8.5f), P(14, 8.5f));
                        g.DrawArc(pen, R(8.5f, 8.5f, 11, 11), -90, 180);
                        g.DrawLine(pen, P(14, 19.5f), P(9, 19.5f));
                        break;
                    case Glyph.Trash:
                        g.DrawLine(pen, P(4, 6.5f), P(20, 6.5f)); g.DrawLines(pen, new[] { P(9, 6.5f), P(9.5f, 3.5f), P(14.5f, 3.5f), P(15, 6.5f) });
                        g.DrawLines(pen, new[] { P(6, 6.5f), P(7, 20.5f), P(17, 20.5f), P(18, 6.5f) });
                        g.DrawLine(pen, P(10, 10.5f), P(10, 16.5f)); g.DrawLine(pen, P(14, 10.5f), P(14, 16.5f));
                        break;
                    case Glyph.Clock:
                        g.DrawEllipse(pen, R(3.5f, 3.5f, 17, 17));
                        g.DrawLines(pen, new[] { P(12, 7), P(12, 12), P(15.5f, 14) });
                        break;
                    case Glyph.Lights:
                        using (var p = RoundPath(R(7, 2.5f, 10, 19), 3 * s)) g.DrawPath(pen, p);
                        g.FillEllipse(fill, R(10, 5, 4, 4)); g.FillEllipse(fill, R(10, 10, 4, 4)); g.FillEllipse(fill, R(10, 15, 4, 4));
                        break;
                    case Glyph.Grid:
                        using (var p = RoundPath(R(3.5f, 3.5f, 17, 17), 2.5f * s)) g.DrawPath(pen, p);
                        g.DrawLine(pen, P(9.2f, 3.5f), P(9.2f, 20.5f)); g.DrawLine(pen, P(14.8f, 3.5f), P(14.8f, 20.5f));
                        g.DrawLine(pen, P(3.5f, 9.2f), P(20.5f, 9.2f)); g.DrawLine(pen, P(3.5f, 14.8f), P(20.5f, 14.8f));
                        break;
                    case Glyph.Note:
                        g.DrawPolygon(pen, new[] { P(4, 4), P(20, 4), P(20, 14), P(14, 20), P(4, 20) });
                        g.DrawLines(pen, new[] { P(20, 14), P(14, 14), P(14, 20) });
                        break;
                    case Glyph.Spot:
                        g.DrawEllipse(pen, R(7, 7, 10, 10));
                        for (int i = 0; i < 8; i++)
                        {
                            double a = i * Math.PI / 4;
                            g.DrawLine(pen, P(12 + 7.5f * (float)Math.Cos(a), 12 + 7.5f * (float)Math.Sin(a)), P(12 + 9.5f * (float)Math.Cos(a), 12 + 9.5f * (float)Math.Sin(a)));
                        }
                        break;
                    case Glyph.Card:
                        using (var p = RoundPath(R(3, 5, 18, 14), 2.5f * s)) g.DrawPath(pen, p);
                        g.DrawLine(pen, P(7, 10), P(17, 10)); g.DrawLine(pen, P(7, 14), P(13, 14));
                        break;
                    case Glyph.Breath:
                        g.DrawEllipse(pen, R(3.5f, 3.5f, 17, 17)); g.FillEllipse(fill, R(8.5f, 8.5f, 7, 7));
                        break;
                    case Glyph.Quote:
                        g.FillEllipse(fill, R(4, 7, 6, 6)); g.FillEllipse(fill, R(13, 7, 6, 6));
                        g.DrawArc(pen, R(1.5f, 5, 8.5f, 13), 0, 90); g.DrawArc(pen, R(10.5f, 5, 8.5f, 13), 0, 90);
                        break;
                    case Glyph.Bell:
                        g.DrawLines(pen, new[] { P(5, 17), P(7, 14.5f), P(7, 10), P(8.5f, 6.5f), P(12, 5), P(15.5f, 6.5f), P(17, 10), P(17, 14.5f), P(19, 17), P(5, 17) });
                        g.DrawLine(pen, P(10.5f, 20), P(13.5f, 20));
                        break;
                    case Glyph.Text:
                        g.DrawLine(pen, P(5, 5), P(19, 5)); g.DrawLine(pen, P(12, 5), P(12, 19.5f));
                        break;
                    case Glyph.Speak:
                        g.FillPolygon(fill, new[] { P(3, 9), P(7, 9), P(12, 4.5f), P(12, 19.5f), P(7, 15), P(3, 15) });
                        g.DrawArc(pen, R(9, 8, 8, 8), -50, 100);
                        break;
                }
            }
            g.SmoothingMode = old;
        }

        static void FillRound(Graphics g, Brush b, RectangleF r, float rad)
        {
            using (var p = RoundPath(r, rad)) g.FillPath(b, p);
        }

        static GraphicsPath RoundPath(RectangleF r, float rad) { return ClassroomToolkit.Draw.Round(r, rad); }

        // An arc with an arrowhead at its end, angles in degrees, clockwise from 3 o'clock.
        static void Arrow(Graphics g, Pen pen, Brush fill, float ox, float oy, float s, float cx, float cy, float rad, float start, float sweep)
        {
            g.DrawArc(pen, ox + (cx - rad) * s, oy + (cy - rad) * s, 2 * rad * s, 2 * rad * s, start, sweep);
            double a = (start + sweep) * Math.PI / 180;
            float ex = ox + (cx + rad * (float)Math.Cos(a)) * s, ey = oy + (cy + rad * (float)Math.Sin(a)) * s;
            float tx = -(float)Math.Sin(a), ty = (float)Math.Cos(a);   // direction of travel
            float nx = (float)Math.Cos(a), ny = (float)Math.Sin(a);    // outward
            float h = 3.6f * s;
            g.FillPolygon(fill, new[]
            {
                new PointF(ex + tx * h, ey + ty * h),
                new PointF(ex + nx * h - tx * h * 0.35f, ey + ny * h - ty * h * 0.35f),
                new PointF(ex - nx * h - tx * h * 0.35f, ey - ny * h - ty * h * 0.35f)
            });
        }
    }

    // Base for every window: dark title bar and the app icon.
    class BaseForm : Form
    {
        public BaseForm()
        {
            Icon = AppIcon.Get();
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Font(10f);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.DarkTitleBar(this);
        }
    }

    // Common painting for the owner-drawn controls: corners take the parent's colour.
    class SmoothControl : Control
    {
        protected bool Hot, Down;

        public SmoothControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Cursor = Cursors.Hand;
            Margin = new Padding(Theme.S(3));
        }

        protected Color ParentColour
        {
            get
            {
                for (Control p = Parent; p != null; p = p.Parent)
                    if (p.BackColor.A == 255) return p.BackColor;
                return Theme.Bg;
            }
        }

        protected override void OnMouseEnter(EventArgs e) { Hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { Hot = false; Down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { Down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected Graphics Begin(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(ParentColour);
            ClassroomToolkit.Draw.Quality(g);
            return g;
        }

        protected Color Surface(Color baseColour)
        {
            if (!Enabled) return Color.FromArgb(120, baseColour);
            if (Down) return ClassroomToolkit.Draw.Mix(baseColour, Color.Black, 0.18);
            if (Hot) return ClassroomToolkit.Draw.Mix(baseColour, Color.White, 0.10);
            return baseColour;
        }

        protected static float Radius { get { return Theme.S(8); } }

        public static void TextIn(Graphics g, string text, Font f, Rectangle r, Color c, TextFormatFlags align)
        {
            TextRenderer.DrawText(g, text, f, r, c, align | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        static ToolTip tips;
        public string Tip
        {
            set
            {
                if (tips == null) { tips = new ToolTip(); tips.InitialDelay = 400; }
                tips.SetToolTip(this, value);
            }
        }
    }

    class FlatButton : SmoothControl, IButtonControl
    {
        Glyph glyph;
        bool primary, toggled, auto = true;
        Color? tint;
        ContentAlignment align = ContentAlignment.MiddleCenter;

        public FlatButton(string text, Glyph glyph, bool primary)
        {
            this.glyph = glyph; this.primary = primary;
            Text = text;
            Font = Theme.Semi(10f);
            ForeColor = Color.White;
            Height = Theme.S(36);
            Width = GetPreferredSize(Size.Empty).Width;
        }

        public Glyph Glyph { get { return glyph; } set { glyph = value; AutoFit(); Invalidate(); } }
        public bool Primary { get { return primary; } set { primary = value; Invalidate(); } }
        public bool Toggled { get { return toggled; } set { toggled = value; Invalidate(); } }
        public ContentAlignment TextAlign { get { return align; } set { align = value; Invalidate(); } }
        public DialogResult DialogResult { get; set; }

        // BackColor sets the button's own fill colour; the corners always match the parent.
        public override Color BackColor
        {
            get { return tint ?? (primary || toggled ? Theme.Accent : Theme.Panel2); }
            set { tint = value; Invalidate(); }
        }

        public override bool AutoSize { get { return auto; } set { auto = value; AutoFit(); } }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); AutoFit(); Invalidate(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); AutoFit(); }

        void AutoFit()
        {
            if (!auto) return;
            Size s = GetPreferredSize(Size.Empty);
            if (s.Width != Width || s.Height != Height) Size = s;
        }

        public override Size GetPreferredSize(Size proposed)
        {
            int h = Math.Max(Theme.S(36), MinimumSize.Height);
            if (string.IsNullOrEmpty(Text)) return new Size(Math.Max(h, MinimumSize.Width), h);
            int w = TextRenderer.MeasureText(Text, Font).Width + Theme.S(glyph == Glyph.None ? 24 : 44);
            return new Size(Math.Max(w, MinimumSize.Width), h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            Color fill = BackColor;
            if (!primary && !toggled && tint == null && !Hot && !Down) fill = Theme.Panel2;
            // An icon-only button that is switched on gets a softer highlight than a main action.
            if (toggled && !primary && tint == null && string.IsNullOrEmpty(Text)) fill = ClassroomToolkit.Draw.Mix(Theme.Panel2, Theme.Accent, 0.45);
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            ClassroomToolkit.Draw.FillRound(g, r, Radius, Surface(fill));
            Color ink = Enabled ? ForeColor : Color.FromArgb(140, ForeColor);
            int icon = Theme.S(18);
            if (string.IsNullOrEmpty(Text))
            {
                Glyphs.Draw(g, glyph, new RectangleF((Width - icon) / 2f, (Height - icon) / 2f, icon, icon), ink);
                return;
            }
            int pad = Theme.S(12);
            int textW = TextRenderer.MeasureText(Text, Font).Width;
            int contentW = textW + (glyph == Glyph.None ? 0 : icon + Theme.S(6));
            int x = align == ContentAlignment.MiddleLeft ? pad : Math.Max(pad / 2, (Width - contentW) / 2);
            if (glyph != Glyph.None)
            {
                Glyphs.Draw(g, glyph, new RectangleF(x, (Height - icon) / 2f, icon, icon), ink);
                x += icon + Theme.S(6);
            }
            TextIn(g, Text, Font, new Rectangle(x, 0, Width - x - pad / 2, Height), ink, TextFormatFlags.Left);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (DialogResult != DialogResult.None)
            {
                Form f = FindForm();
                if (f != null) f.DialogResult = DialogResult;
            }
        }

        public void NotifyDefault(bool value) { }
        public void PerformClick() { if (Enabled) OnClick(EventArgs.Empty); }
    }

    // A number with - and + either side. Click the number to type one; the mouse wheel also changes it.
    class Stepper : SmoothControl
    {
        decimal min, max = 100, value, step = 1;
        public string Unit = "";
        public event EventHandler ValueChanged;
        readonly Timer repeat = new Timer();
        int heldDirection;
        TextBox editor;
        int hover; // -1 minus, 0 middle, 1 plus

        public Stepper()
        {
            Font = Theme.Semi(11f);
            Size = new Size(Theme.S(120), Theme.S(36));
            repeat.Tick += delegate { repeat.Interval = 60; Nudge(heldDirection); };
        }

        public decimal Minimum { get { return min; } set { min = value; if (this.value < min) Value = min; } }
        public decimal Maximum { get { return max; } set { max = value; if (this.value > max) Value = max; } }
        public decimal Increment { get { return step; } set { step = value; } }

        public decimal Value
        {
            get { return value; }
            set
            {
                decimal v = Math.Max(min, Math.Min(max, value));
                if (v == this.value) return;
                this.value = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        int Zone(Point p)
        {
            int b = Height;
            if (p.X < b) return -1;
            if (p.X > Width - b) return 1;
            return 0;
        }

        protected virtual void Nudge(int dir) { if (dir != 0) Value = value + dir * step; }
        protected virtual string Format(decimal v) { return v.ToString("0.##"); }
        protected virtual bool Parse(string text, out decimal v) { return decimal.TryParse(text, out v); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int z = Zone(e.Location);
            if (z != hover) { hover = z; Cursor = z == 0 ? Cursors.IBeam : Cursors.Hand; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int z = Zone(e.Location);
            if (z == 0) { BeginEdit(); return; }
            heldDirection = z;
            Nudge(z);
            repeat.Interval = 420;
            repeat.Start();
        }

        protected override void OnMouseUp(MouseEventArgs e) { repeat.Stop(); base.OnMouseUp(e); }
        protected override void OnMouseLeave(EventArgs e) { repeat.Stop(); hover = 0; base.OnMouseLeave(e); }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Nudge(e.Delta > 0 ? 1 : -1);
            base.OnMouseWheel(e);
        }

        void BeginEdit()
        {
            if (editor != null) return;
            editor = new TextBox();
            editor.BorderStyle = BorderStyle.None;
            editor.BackColor = Theme.Panel2; editor.ForeColor = Color.White;
            editor.Font = Font; editor.TextAlign = HorizontalAlignment.Center;
            editor.Text = Format(value);
            int h = editor.PreferredHeight;
            editor.SetBounds(Height, (Height - h) / 2, Width - 2 * Height, h);
            editor.KeyDown += delegate(object s, KeyEventArgs k)
            {
                if (k.KeyCode == Keys.Enter) { EndEdit(true); k.SuppressKeyPress = true; }
                else if (k.KeyCode == Keys.Escape) { EndEdit(false); k.SuppressKeyPress = true; }
                else if (k.KeyCode == Keys.Up) { Nudge(1); editor.Text = Format(value); editor.SelectAll(); k.SuppressKeyPress = true; }
                else if (k.KeyCode == Keys.Down) { Nudge(-1); editor.Text = Format(value); editor.SelectAll(); k.SuppressKeyPress = true; }
            };
            editor.LostFocus += delegate { EndEdit(true); };
            Controls.Add(editor);
            editor.Focus();
            editor.SelectAll();
        }

        void EndEdit(bool keep)
        {
            if (editor == null) return;
            TextBox ed = editor;
            editor = null;
            decimal v;
            if (keep && Parse(ed.Text.Trim(), out v)) Value = v;
            Controls.Remove(ed);
            ed.Dispose();
            Invalidate();
            Form f = FindForm();
            var tool = f as ToolForm;
            if (tool != null) tool.FocusDisplay();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            ClassroomToolkit.Draw.FillRound(g, r, Radius, Theme.Panel2);
            int b = Height;
            if (Hot && hover != 0)
            {
                var hz = hover < 0 ? new RectangleF(0.5f, 0.5f, b, Height - 1) : new RectangleF(Width - b - 0.5f, 0.5f, b, Height - 1);
                using (var clip = ClassroomToolkit.Draw.Round(r, Radius))
                {
                    g.SetClip(clip);
                    using (var br = new SolidBrush(Down ? Theme.Accent : ClassroomToolkit.Draw.Mix(Theme.Panel2, Color.White, 0.12))) g.FillRectangle(br, hz);
                    g.ResetClip();
                }
            }
            int icon = Theme.S(14);
            Glyphs.Draw(g, Glyph.Minus, new RectangleF((b - icon) / 2f, (Height - icon) / 2f, icon, icon), value > min ? Theme.Text : Theme.Muted);
            Glyphs.Draw(g, Glyph.Plus, new RectangleF(Width - b + (b - icon) / 2f, (Height - icon) / 2f, icon, icon), value < max ? Theme.Text : Theme.Muted);
            if (editor != null) return;
            string v = Format(value);
            var mid = new Rectangle(b, 0, Width - 2 * b, Height);
            if (Unit.Length == 0) TextIn(g, v, Font, mid, Color.White, TextFormatFlags.HorizontalCenter);
            else
            {
                Font uf = Theme.Font(9f);
                int vw = TextRenderer.MeasureText(v, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
                int uw = TextRenderer.MeasureText(Unit, uf, Size.Empty, TextFormatFlags.NoPadding).Width;
                int x = mid.X + (mid.Width - vw - uw - Theme.S(3)) / 2;
                TextRenderer.DrawText(g, v, Font, new Rectangle(x, 0, vw + 2, Height), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                TextRenderer.DrawText(g, Unit, uf, new Rectangle(x + vw + Theme.S(3), Theme.S(1), uw + 2, Height), Theme.Muted, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }

        protected override void Dispose(bool disposing) { if (disposing) repeat.Dispose(); base.Dispose(disposing); }
    }

    // A stepper for a length of time in seconds, shown as m:ss. The buttons step whole minutes
    // (or 10 seconds below a minute); click it to type a time such as 7, 7:30 or 90s.
    class TimeStepper : Stepper
    {
        public TimeStepper()
        {
            Minimum = 5; Maximum = 599 * 60 + 59;
            Width = Theme.S(124);
        }

        protected override void Nudge(int dir)
        {
            if (dir == 0) return;
            int v = (int)Value;
            int step = v < 60 || (v == 60 && dir < 0) ? 10 : 60;
            int next = dir > 0 ? (v / step + 1) * step : ((v + step - 1) / step - 1) * step;
            Value = Math.Max(Minimum, next);
        }

        protected override string Format(decimal v)
        {
            int s = (int)v;
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        protected override bool Parse(string text, out decimal v)
        {
            v = 0;
            text = text.Trim().ToLowerInvariant().Replace(" ", "");
            int m, sec;
            if (text.EndsWith("s") && int.TryParse(text.TrimEnd('s'), out sec)) { v = sec; return true; }
            if (text.EndsWith("m") && int.TryParse(text.TrimEnd('m'), out m)) { v = m * 60; return true; }
            int colon = text.IndexOf(':');
            if (colon < 0) colon = text.IndexOf('.');
            if (colon >= 0)
            {
                string a = text.Substring(0, colon), b = text.Substring(colon + 1);
                if ((a.Length == 0 || int.TryParse(a, out m)) && int.TryParse(b, out sec))
                {
                    if (a.Length == 0) m = 0; else int.TryParse(a, out m);
                    v = m * 60 + sec;
                    return true;
                }
                return false;
            }
            if (int.TryParse(text, out m)) { v = m * 60; return true; }
            return false;
        }
    }

    // A time of day in minutes after midnight, shown as HH:mm. The buttons step 5 minutes; click it to type 9:15.
    class ClockStepper : Stepper
    {
        public ClockStepper()
        {
            Minimum = 0; Maximum = 24 * 60 - 1; Increment = 5;
            Width = Theme.S(118);
        }

        protected override void Nudge(int dir)
        {
            if (dir == 0) return;
            int v = (int)Value;
            int next = dir > 0 ? (v / 5 + 1) * 5 : ((v + 4) / 5 - 1) * 5;
            Value = (next + 24 * 60) % (24 * 60);
        }

        protected override string Format(decimal v)
        {
            int m = (int)v;
            return (m / 60).ToString("00") + ":" + (m % 60).ToString("00");
        }

        protected override bool Parse(string text, out decimal v)
        {
            v = 0;
            text = text.Trim().Replace(".", ":").Replace(" ", "");
            int h, m = 0;
            int colon = text.IndexOf(':');
            if (colon >= 0)
            {
                if (!int.TryParse(text.Substring(0, colon), out h) || !int.TryParse(text.Substring(colon + 1), out m)) return false;
            }
            else
            {
                int n;
                if (!int.TryParse(text, out n)) return false;
                if (text.Length >= 3) { h = n / 100; m = n % 100; } else h = n;
            }
            if (h < 0 || h > 23 || m < 0 || m > 59) return false;
            v = h * 60 + m;
            return true;
        }
    }

    // An on/off switch with a label.
    class Toggle : SmoothControl
    {
        bool on;
        public event EventHandler CheckedChanged;

        public Toggle(string text, bool on)
        {
            this.on = on;
            Text = text;
            Font = Theme.Font(10f);
            Height = Theme.S(36);
            Width = GetPreferredSize(Size.Empty).Width;
        }

        public bool Checked
        {
            get { return on; }
            set { if (on == value) return; on = value; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); }
        }

        public override Size GetPreferredSize(Size proposed)
        {
            return new Size(Theme.S(44) + TextRenderer.MeasureText(Text, Font).Width + Theme.S(6), Theme.S(36));
        }

        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            float tw = Theme.S(36), th = Theme.S(20);
            var track = new RectangleF(Theme.S(4), (Height - th) / 2f, tw, th);
            ClassroomToolkit.Draw.FillRound(g, track, th / 2, on ? Surface(Theme.Accent) : Surface(Theme.Line));
            float k = th - Theme.S(6);
            using (var b = new SolidBrush(Color.White))
                g.FillEllipse(b, on ? track.Right - k - Theme.S(3) : track.X + Theme.S(3), track.Y + Theme.S(3), k, k);
            TextIn(g, Text, Font, new Rectangle((int)track.Right + Theme.S(8), 0, Width - (int)track.Right - Theme.S(8), Height), on ? Theme.Text : Theme.Muted, TextFormatFlags.Left);
        }
    }

    // A drop-down list drawn to match the other controls.
    class Choice : SmoothControl
    {
        public readonly List<string> Items = new List<string>();
        int index = -1;
        public event EventHandler SelectedIndexChanged;

        public Choice(int width)
        {
            Font = Theme.Font(10.5f);
            Size = new Size(Theme.S(width), Theme.S(36));
        }

        public int SelectedIndex
        {
            get { return index; }
            set
            {
                int v = value < -1 || value >= Items.Count ? -1 : value;
                if (v == index) return;
                index = v;
                Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        public string SelectedItem { get { return index >= 0 && index < Items.Count ? Items[index] : null; } }

        public override string Text
        {
            get { return SelectedItem ?? ""; }
            set { int i = Items.IndexOf(value); if (i >= 0) SelectedIndex = i; }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (Items.Count == 0) return;
            var menu = Menus.Create();
            for (int i = 0; i < Items.Count; i++)
            {
                int pick = i;
                var item = new ToolStripMenuItem(Items[i], null, delegate { SelectedIndex = pick; });
                item.Checked = i == index;
                menu.Items.Add(item);
            }
            menu.MinimumSize = new Size(Width, 0);
            menu.Closed += delegate { BeginInvoke((Action)delegate { menu.Dispose(); }); };
            menu.Show(this, new Point(0, Height + Theme.S(2)));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Items.Count > 0) SelectedIndex = Math.Max(0, Math.Min(Items.Count - 1, index + (e.Delta > 0 ? -1 : 1)));
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            ClassroomToolkit.Draw.FillRound(g, new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Radius, Surface(Theme.Panel2));
            int icon = Theme.S(14);
            TextIn(g, Text, Font, new Rectangle(Theme.S(12), 0, Width - Theme.S(20) - icon, Height), Color.White, TextFormatFlags.Left);
            Glyphs.Draw(g, Glyph.ChevronDown, new RectangleF(Width - Theme.S(10) - icon, (Height - icon) / 2f, icon, icon), Theme.Muted);
        }
    }

    // A round colour button (or a pen-size dot when Dot > 0); the chosen one gets a ring.
    class Swatch : SmoothControl
    {
        public Color Colour;
        public float Dot;
        bool chosen;

        public Swatch(Color colour, float dot)
        {
            Colour = colour; Dot = dot;
            Size = new Size(Theme.S(36), Theme.S(36));
        }

        public bool Chosen { get { return chosen; } set { chosen = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            var r = new RectangleF(Theme.S(3), Theme.S(3), Width - Theme.S(6), Height - Theme.S(6));
            if (Dot > 0)
            {
                ClassroomToolkit.Draw.FillRound(g, new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Theme.S(8), chosen ? ClassroomToolkit.Draw.Mix(Theme.Panel2, Theme.Accent, 0.45) : Surface(Theme.Panel2));
                float d = Theme.S(4) + Dot * Theme.Scale;
                using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, (Width - d) / 2f, (Height - d) / 2f, d, d);
                return;
            }
            if (chosen) using (var p = new Pen(Color.White, Theme.S(2))) g.DrawEllipse(p, ClassroomToolkit.Draw.Inset(r, -Theme.S(1)));
            var inner = ClassroomToolkit.Draw.Inset(r, Theme.S(chosen ? 4 : 2));
            using (var b = new SolidBrush(Colour)) g.FillEllipse(b, inner);
            if (Colour.GetBrightness() < 0.2f || Colour.GetBrightness() > 0.9f)
                using (var p = new Pen(Color.FromArgb(90, 128, 128, 128), 1)) g.DrawEllipse(p, inner);
        }
    }

    // A text box with a rounded background, for the toolbars.
    class TextField : Panel
    {
        public readonly TextBox Box = new TextBox();

        public TextField(string text, int width)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Theme.S(width), Theme.S(36));
            Margin = new Padding(Theme.S(3));
            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = Theme.Panel2; Box.ForeColor = Color.White;
            Box.Font = Theme.Font(11f);
            Box.Text = text;
            Controls.Add(Box);
            Cursor = Cursors.IBeam;
            MouseDown += delegate { Box.Focus(); };
        }

        // Grey hint text shown while the box is empty (Windows only).
        public string Placeholder
        {
            set
            {
                if (!Env.IsWindows) return;
                string v = value;
                EventHandler set = delegate { try { Native.SendMessage(Box.Handle, 0x1501, (IntPtr)1, v); } catch { } };
                if (Box.IsHandleCreated) set(Box, EventArgs.Empty); else Box.HandleCreated += set;
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int h = Box.PreferredHeight;
            Box.SetBounds(Theme.S(10), (Height - h) / 2, Width - Theme.S(20), h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Color parent = Parent != null ? Parent.BackColor : Theme.Bg;
            g.Clear(parent);
            ClassroomToolkit.Draw.Quality(g);
            ClassroomToolkit.Draw.FillRound(g, new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Theme.S(8), Theme.Panel2);
            if (Box.Focused)
                using (var p = ClassroomToolkit.Draw.Round(new RectangleF(1, 1, Width - 2, Height - 2), Theme.S(8)))
                using (var pen = new Pen(Theme.Accent, Theme.S(2))) g.DrawPath(pen, p);
        }
    }

    // A thin vertical line between groups of toolbar controls.
    class Divider : Control
    {
        public Divider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            Size = new Size(Theme.S(1), Theme.S(24));
            Margin = new Padding(Theme.S(7), Theme.S(9), Theme.S(7), Theme.S(9));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.Line);
        }
    }

    // Toolbar: controls flow from the left and wrap if needed; controls marked as right-hand sit at the right of the first row.
    class ToolBarPanel : Panel
    {
        readonly HashSet<Control> right = new HashSet<Control>();
        bool laying;

        public ToolBarPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Theme.Panel;
            Padding = new Padding(Theme.S(8), Theme.S(6), Theme.S(8), Theme.S(6));
            Height = Theme.S(52);
        }

        public void AddRight(Control c) { right.Add(c); Controls.Add(c); }

        // Width needed to show every control on a single row.
        public int SingleRowWidth()
        {
            int w = Padding.Horizontal;
            foreach (Control c in Controls) w += SizeOf(c).Width + c.Margin.Horizontal;
            return w + Theme.S(12) * (right.Count > 0 ? 1 : 0);
        }

        static Size SizeOf(Control c)
        {
            if (c is FlatButton || c is Toggle || (c is Label && c.AutoSize)) return c.GetPreferredSize(Size.Empty);
            return c.Size;
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            if (laying) return;
            laying = true;
            try
            {
                int left = Padding.Left, top = Padding.Top, maxRight = Width - Padding.Right;
                // Right-hand group first, so the left group knows how much room it has on the first row.
                int rx = maxRight;
                int rowH = Theme.S(40);
                var rights = new List<Control>();
                foreach (Control c in Controls) if (right.Contains(c) && c.Visible) rights.Add(c);
                for (int i = rights.Count - 1; i >= 0; i--)
                {
                    Control c = rights[i];
                    Size s = SizeOf(c);
                    rx -= s.Width + c.Margin.Right;
                    c.SetBounds(rx, top + (rowH - s.Height) / 2, s.Width, s.Height);
                    rx -= c.Margin.Left;
                }
                int limit = rights.Count > 0 ? rx - Theme.S(12) : maxRight;
                int x = left, y = top;
                bool firstRow = true;
                foreach (Control c in Controls)
                {
                    if (right.Contains(c) || !c.Visible) continue;
                    Size s = SizeOf(c);
                    int need = c.Margin.Left + s.Width + c.Margin.Right;
                    int edge = firstRow ? limit : maxRight;
                    if (x > left && x + need > edge) { x = left; y += rowH; firstRow = false; }
                    c.SetBounds(x + c.Margin.Left, y + (rowH - s.Height) / 2, s.Width, s.Height);
                    x += need;
                }
                int h = y + rowH + Padding.Bottom;
                if (Height != h) Height = h;
            }
            finally { laying = false; }
        }
    }

    // Dark, rounded-looking menus for drop-downs, right-click menus and the notification-area icon.
    static class Menus
    {
        class Colours : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Theme.Panel; } }
            public override Color ImageMarginGradientBegin { get { return Theme.Panel; } }
            public override Color ImageMarginGradientMiddle { get { return Theme.Panel; } }
            public override Color ImageMarginGradientEnd { get { return Theme.Panel; } }
            public override Color MenuBorder { get { return Theme.Line; } }
            public override Color MenuItemBorder { get { return Theme.Panel2; } }
            public override Color MenuItemSelected { get { return Theme.Panel2; } }
            public override Color MenuItemSelectedGradientBegin { get { return Theme.Panel2; } }
            public override Color MenuItemSelectedGradientEnd { get { return Theme.Panel2; } }
            public override Color MenuItemPressedGradientBegin { get { return Theme.Panel2; } }
            public override Color MenuItemPressedGradientEnd { get { return Theme.Panel2; } }
            public override Color MenuStripGradientBegin { get { return Theme.Panel; } }
            public override Color MenuStripGradientEnd { get { return Theme.Panel; } }
            public override Color SeparatorDark { get { return Theme.Line; } }
            public override Color SeparatorLight { get { return Theme.Panel; } }
            public override Color CheckBackground { get { return Theme.Panel; } }
            public override Color CheckSelectedBackground { get { return Theme.Panel2; } }
            public override Color CheckPressedBackground { get { return Theme.Panel2; } }
            public override Color ButtonSelectedBorder { get { return Theme.Panel2; } }
        }

        class Renderer : ToolStripProfessionalRenderer
        {
            public Renderer() : base(new Colours()) { RoundedEdges = false; }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Muted;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = Theme.Muted;
                base.OnRenderArrow(e);
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                int s = Math.Min(e.ImageRectangle.Width, e.ImageRectangle.Height);
                var r = new RectangleF(e.ImageRectangle.X + (e.ImageRectangle.Width - s) / 2f, e.ImageRectangle.Y + (e.ImageRectangle.Height - s) / 2f, s, s);
                Glyphs.Draw(e.Graphics, Glyph.Check, r, Theme.Accent);
            }
        }

        static Renderer renderer;
        public static ToolStripRenderer Look { get { return renderer ?? (renderer = new Renderer()); } }

        public static ContextMenuStrip Create()
        {
            var m = new ContextMenuStrip();
            Style(m);
            return m;
        }

        public static void Style(ToolStrip m)
        {
            m.Renderer = Look;
            m.Font = Theme.Font(10.5f);
            m.ForeColor = Theme.Text;
            m.BackColor = Theme.Panel;
            foreach (ToolStripItem i in m.Items) StyleItem(i);
            m.ItemAdded += delegate(object s, ToolStripItemEventArgs e) { StyleItem(e.Item); };
        }

        static void StyleItem(ToolStripItem i)
        {
            i.ForeColor = Theme.Text;
            i.Padding = new Padding(Theme.S(4), Theme.S(3), Theme.S(4), Theme.S(3));
            var mi = i as ToolStripMenuItem;
            if (mi != null)
            {
                if (mi.DropDown != null) { mi.DropDown.Renderer = Look; mi.DropDown.BackColor = Theme.Panel; }
                foreach (ToolStripItem sub in mi.DropDownItems) StyleItem(sub);
                mi.DropDownOpening += delegate { foreach (ToolStripItem sub in mi.DropDownItems) StyleItem(sub); };
            }
        }
    }

    static partial class Native
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        // Asks Windows 10 (1809+) and 11 for a dark title bar; ignored elsewhere.
        public static void DarkTitleBar(Form f)
        {
            if (!Env.IsWindows) return;
            try
            {
                int on = 1;
                if (DwmSetWindowAttribute(f.Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(f.Handle, 19, ref on, 4);
            }
            catch { }
        }
    }
}
