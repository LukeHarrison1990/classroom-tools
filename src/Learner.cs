// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - learner tools: text to speech, speed reader, breathing exercise, quotes and goals.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    // Uses the speech voices built into Windows (SAPI) through late binding, so nothing extra is installed.
    class Speaker
    {
        dynamic voice;
        public readonly List<string> Voices = new List<string>();

        public Speaker()
        {
            if (!Env.IsWindows) return;
            try
            {
                Type t = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (t == null) return;
                voice = Activator.CreateInstance(t);
                dynamic tokens = voice.GetVoices();
                for (int i = 0; i < (int)tokens.Count; i++) Voices.Add((string)tokens.Item(i).GetDescription());
            }
            catch { voice = null; }
        }

        public bool Available { get { return voice != null; } }

        public void Speak(string text, int voiceIndex, int rate)
        {
            if (voice == null || string.IsNullOrWhiteSpace(text)) return;
            try
            {
                dynamic tokens = voice.GetVoices();
                if (voiceIndex >= 0 && voiceIndex < (int)tokens.Count) voice.Voice = tokens.Item(voiceIndex);
                voice.Rate = rate;
                voice.Speak(text, 1 | 2); // asynchronous, and cancel anything already being spoken
            }
            catch { }
        }

        public void Stop()
        {
            if (voice == null) return;
            try { voice.Speak("", 1 | 2); } catch { }
        }
    }

    class SpeechForm : Form
    {
        readonly Speaker speaker = new Speaker();
        readonly TextBox box;
        readonly ComboBox voices;
        readonly TrackBar rate;

        public SpeechForm()
        {
            Text = "Text to Speech";
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.Font(10f);
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(820), Theme.S(480));

            var bar = new FlowLayoutPanel();
            bar.Dock = DockStyle.Top; bar.AutoSize = true; bar.BackColor = Theme.Panel; bar.Padding = new Padding(Theme.S(6));
            var speak = ToolForm.MakeButton("Speak (F5)", true);
            var selection = ToolForm.MakeButton("Speak selection", false);
            var clip = ToolForm.MakeButton("Speak clipboard", false);
            var stop = ToolForm.MakeButton("Stop (Esc)", false);
            voices = new ComboBox();
            voices.DropDownStyle = ComboBoxStyle.DropDownList; voices.Width = Theme.S(230); voices.Font = Theme.Font(10.5f);
            voices.Margin = new Padding(Theme.S(8), Theme.S(6), Theme.S(3), Theme.S(3));
            foreach (string v in speaker.Voices) voices.Items.Add(v);
            if (voices.Items.Count > 0) voices.SelectedIndex = Math.Max(0, Math.Min(voices.Items.Count - 1, Store.GetInt("speech.voice", 0)));
            voices.SelectedIndexChanged += delegate { Store.Set("speech.voice", voices.SelectedIndex); };
            var slow = new Label();
            slow.Text = "Speed"; slow.AutoSize = true; slow.ForeColor = Theme.Muted; slow.Margin = new Padding(Theme.S(8), Theme.S(10), 0, 0);
            rate = new TrackBar();
            rate.Minimum = -8; rate.Maximum = 8; rate.Value = Math.Max(-8, Math.Min(8, Store.GetInt("speech.rate", 0)));
            rate.Width = Theme.S(140); rate.TickStyle = TickStyle.None; rate.AutoSize = false; rate.Height = Theme.S(32);
            rate.ValueChanged += delegate { Store.Set("speech.rate", rate.Value); };
            bar.Controls.AddRange(new Control[] { speak, selection, clip, stop, voices, slow, rate });

            box = new TextBox();
            box.Multiline = true; box.ScrollBars = ScrollBars.Vertical; box.AcceptsReturn = true; box.Dock = DockStyle.Fill;
            box.Font = Theme.Font(16f); box.BackColor = Color.White; box.ForeColor = Color.Black; box.MaxLength = 0;
            box.Text = speaker.Available ? "Type or paste text here, then press Speak." : "Speech is not available on this computer: no Windows speech voice was found.";

            speak.Click += delegate { Say(box.Text); };
            selection.Click += delegate { Say(box.SelectedText.Length > 0 ? box.SelectedText : box.Text); };
            clip.Click += delegate { if (Clipboard.ContainsText()) { box.Text = Clipboard.GetText(); Say(box.Text); } };
            stop.Click += delegate { speaker.Stop(); };
            Controls.Add(box); Controls.Add(bar);
            FormClosed += delegate { speaker.Stop(); };
            Shown += delegate { box.SelectAll(); box.Focus(); };
        }

        void Say(string text) { speaker.Speak(text, voices.SelectedIndex, rate.Value); }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F5) { Say(box.Text); return true; }
            if (keyData == Keys.Escape) { speaker.Stop(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    // Shows text one word at a time at a chosen pace.
    class SpeedReaderForm : ToolForm
    {
        NumericUpDown wpm;
        Button startBtn;
        string[] words = new string[0];
        int index;
        bool running;
        DateTime nextAt;

        public SpeedReaderForm() : base("Speed Reader", 900, 460)
        {
            AddButton("Paste text", delegate { if (Clipboard.ContainsText()) SetText(Clipboard.GetText()); });
            AddButton("Type text", delegate
            {
                string t = TextEditDialog.Show(this, "Text to read", "Paste or type the passage to practise with.", string.Join(" ", words));
                if (t != null) SetText(t);
            });
            AddLabel("Words per minute");
            wpm = AddNumber(60, 1000, Store.GetInt("reader.wpm", 250), 70);
            wpm.Increment = 25;
            wpm.ValueChanged += delegate { Store.Set("reader.wpm", (int)wpm.Value); };
            startBtn = AddButton("Start (Space)", delegate { Toggle(); }, true);
            AddButton("Back to start", delegate { index = 0; running = false; startBtn.Text = "Start (Space)"; View.Invalidate(); });
            AddWindowButtons(false);
            Tick.Interval = 15;
            SetText("Reading one word at a time keeps your eyes still, so you can practise reading a little faster each day. Paste your own passage, choose a speed that feels comfortable, and press Start. Raise the speed gradually and check that you still understand what you read.");
        }

        void SetText(string t)
        {
            words = t.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            index = 0; running = false; Tick.Stop();
            startBtn.Text = "Start (Space)";
            View.Invalidate();
        }

        void Toggle()
        {
            if (words.Length == 0) return;
            if (index >= words.Length - 1) index = 0;
            running = !running;
            startBtn.Text = running ? "Pause (Space)" : "Resume (Space)";
            nextAt = DateTime.UtcNow;
            if (running) Tick.Start(); else Tick.Stop();
            View.Invalidate();
        }

        protected override void OnTick()
        {
            if (!running || DateTime.UtcNow < nextAt) return;
            if (index >= words.Length - 1) { running = false; Tick.Stop(); startBtn.Text = "Start (Space)"; View.Invalidate(); return; }
            index++;
            string w = words[index];
            double ms = 60000.0 / (double)wpm.Value;
            char last = w[w.Length - 1];
            if (last == '.' || last == '!' || last == '?') ms *= 2.0; else if (last == ',' || last == ';' || last == ':') ms *= 1.5;
            if (w.Length > 8) ms *= 1.2;
            nextAt = DateTime.UtcNow.AddMilliseconds(ms);
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { Toggle(); return true; }
            if (key == Keys.Left) { index = Math.Max(0, index - 10); View.Invalidate(); return true; }
            if (key == Keys.Right) { index = Math.Min(Math.Max(0, words.Length - 1), index + 10); View.Invalidate(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            g.Clear(Color.FromArgb(250, 248, 240));
            Color ink = Color.FromArgb(24, 28, 36);
            if (words.Length == 0) { Draw.Text(g, "Paste some text to begin", Draw.Inset(r, r.Width * 0.15f), Color.Gray, FontStyle.Regular, false, 40f, null); return; }
            string w = words[Math.Min(index, words.Length - 1)];
            float size = Draw.FitSize(g, "extraordinarily", new SizeF(r.Width * 0.86f, r.Height * 0.5f), FontStyle.Bold, false, 300f);
            Font f = Theme.Font(size, FontStyle.Bold);
            // Line the word up on a focus letter a little left of its middle, drawn in red.
            int focus = Math.Min(w.Length - 1, (w.Length + 2) / 4);
            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                float before = g.MeasureString(w.Substring(0, focus), f, 9999, sf).Width;
                float letter = g.MeasureString(w.Substring(focus, 1), f, 9999, sf).Width;
                float x = r.Width * 0.42f - before - letter / 2f;
                float y = (r.Height - f.GetHeight(g)) / 2f - r.Height * 0.03f;
                using (var b = new SolidBrush(ink)) using (var red = new SolidBrush(Theme.Red))
                {
                    g.DrawString(w.Substring(0, focus), f, b, x, y, sf);
                    g.DrawString(w.Substring(focus, 1), f, red, x + before, y, sf);
                    g.DrawString(w.Substring(focus + 1), f, b, x + before + letter, y, sf);
                }
                using (var p = new Pen(Color.FromArgb(200, 196, 180), 2))
                {
                    float cx = r.Width * 0.42f;
                    g.DrawLine(p, cx, y - 14, cx, y - 2);
                    g.DrawLine(p, cx, y + f.GetHeight(g) + 2, cx, y + f.GetHeight(g) + 14);
                }
            }
            float bw = r.Width * 0.8f, bx = r.Width * 0.1f, by = r.Height * 0.88f;
            Draw.FillRound(g, new RectangleF(bx, by, bw, 8), 4, Color.FromArgb(226, 222, 208));
            Draw.FillRound(g, new RectangleF(bx, by, Math.Max(8, bw * (index + 1) / words.Length), 8), 4, Theme.Accent);
            int minsLeft = (int)Math.Ceiling((words.Length - index) / (double)wpm.Value);
            Draw.Label(g, "Word " + (index + 1) + " of " + words.Length + "   -   about " + minsLeft + " min left   -   arrow keys skip 10 words",
                new RectangleF(0, by + 12, r.Width, Theme.S(26)), Color.Gray, 10.5f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    class BreathingForm : ToolForm
    {
        ComboBox pattern;
        Button startBtn;
        bool running;
        DateTime started;
        static readonly string[] Names = { "Calm breathing (in 4, out 6)", "Box breathing (4-4-4-4)", "Slow breathing (in 5, out 5)", "Relax (in 4, hold 7, out 8)" };
        static readonly int[][] Patterns = { new[] { 4, 0, 6, 0 }, new[] { 4, 4, 4, 4 }, new[] { 5, 0, 5, 0 }, new[] { 4, 7, 8, 0 } };
        static readonly string[] Phase = { "Breathe in", "Hold", "Breathe out", "Hold" };

        public BreathingForm() : base("Breathing Exercise", 700, 620)
        {
            pattern = AddCombo(250, Names);
            pattern.SelectedIndex = Math.Max(0, Math.Min(Names.Length - 1, Store.GetInt("breath.pattern", 0)));
            pattern.SelectedIndexChanged += delegate { Store.Set("breath.pattern", pattern.SelectedIndex); started = DateTime.UtcNow; View.Invalidate(); };
            startBtn = AddButton("Start (Space)", delegate { Toggle(); }, true);
            AddWindowButtons(false);
            Tick.Interval = 33;
        }

        void Toggle()
        {
            running = !running;
            started = DateTime.UtcNow;
            startBtn.Text = running ? "Stop (Space)" : "Start (Space)";
            if (running) Tick.Start(); else Tick.Stop();
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space) { Toggle(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            Color sky = Color.FromArgb(14, 36, 58);
            g.Clear(sky);
            int[] p = Patterns[Math.Max(0, pattern.SelectedIndex)];
            double cycle = p.Sum(), t = running ? (DateTime.UtcNow - started).TotalSeconds % cycle : 0;
            int phase = 0; double into = t;
            while (phase < 3 && into >= p[phase]) { into -= p[phase]; phase++; }
            double f = p[phase] == 0 ? 0 : into / p[phase];
            double ease = (1 - Math.Cos(f * Math.PI)) / 2;
            double size = phase == 0 ? ease : phase == 1 ? 1 : phase == 2 ? 1 - ease : 0;
            float maxR = Math.Min(r.Width, r.Height * 0.8f) * 0.42f, minR = maxR * 0.38f;
            float rad = running ? (float)(minR + (maxR - minR) * size) : minR;
            float cx = r.Width / 2f, cy = r.Height * 0.46f;
            using (var ring = new Pen(Color.FromArgb(40, 255, 255, 255), 2)) g.DrawEllipse(ring, cx - maxR, cy - maxR, maxR * 2, maxR * 2);
            using (var halo = new SolidBrush(Color.FromArgb(40, Theme.Teal))) g.FillEllipse(halo, cx - rad * 1.12f, cy - rad * 1.12f, rad * 2.24f, rad * 2.24f);
            using (var b = new SolidBrush(Draw.Mix(Theme.Teal, Theme.Accent, size))) g.FillEllipse(b, cx - rad, cy - rad, rad * 2, rad * 2);
            if (running)
            {
                int count = (int)Math.Ceiling(p[phase] - into);
                Draw.Text(g, count.ToString(), new RectangleF(cx - minR * 0.6f, cy - minR * 0.6f, minR * 1.2f, minR * 1.2f), Color.White, FontStyle.Bold, false, 200f, "0");
                Draw.Text(g, Phase[phase], new RectangleF(0, r.Height * 0.86f, r.Width, r.Height * 0.11f), Color.White, FontStyle.Bold, false, 70f, null);
            }
            else Draw.Text(g, "Press Space to begin", new RectangleF(0, r.Height * 0.86f, r.Width, r.Height * 0.09f), Color.FromArgb(160, 190, 210), FontStyle.Regular, false, 40f, null);
        }
    }

    class QuotesForm : ToolForm
    {
        FileChooser chooser;
        CheckBox auto;
        List<string[]> quotes = new List<string[]>();
        int index;
        DateTime shownAt = DateTime.UtcNow;
        static readonly Color[] Tints = { Theme.Accent, Theme.Purple, Theme.Teal, Theme.Pink, Theme.Amber };

        public QuotesForm() : base("Quotes and Goals", 900, 500)
        {
            AddLabel("List");
            chooser = new FileChooser(this, Bar.Controls, "Quotes", "quote list", Samples.QuoteHelp);
            chooser.Changed += delegate { LoadQuotes(); };
            AddButton("Next (Space)", delegate { Next(); }, true);
            auto = AddCheck("Change every 30 seconds", false);
            auto.CheckedChanged += delegate { if (auto.Checked) { shownAt = DateTime.UtcNow; Tick.Start(); } else Tick.Stop(); };
            AddWindowButtons(false);
            Tick.Interval = 1000;
            View.Click += delegate { Next(); };
            LoadQuotes();
        }

        void LoadQuotes()
        {
            quotes = Store.Lines(chooser.Path ?? "").Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#"))
                .Select(l => { int i = l.LastIndexOf('|'); return i > 0 ? new[] { l.Substring(0, i).Trim(), l.Substring(i + 1).Trim() } : new[] { l, "" }; }).ToList();
            Store.Shuffle(quotes);
            index = 0;
            View.Invalidate();
        }

        void Next()
        {
            if (quotes.Count > 0) index = (index + 1) % quotes.Count;
            shownAt = DateTime.UtcNow;
            View.Invalidate();
        }

        protected override void OnTick() { if (auto.Checked && (DateTime.UtcNow - shownAt).TotalSeconds >= 30) Next(); }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space || key == Keys.Right) { Next(); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            Color tint = Tints[index % Tints.Length];
            g.Clear(Draw.Mix(Theme.Bg, tint, 0.2));
            if (quotes.Count == 0) { Draw.Wrapped(g, "This list is empty. Press Edit to add quotes or goals, one per line.", Draw.Inset(r, r.Width * 0.1f), Theme.Muted, FontStyle.Regular, 26f); return; }
            string[] q = quotes[index];
            bool hasAuthor = q[1].Length > 0;
            Draw.Text(g, "“", new RectangleF(r.Width * 0.03f, r.Height * 0.0f, r.Width * 0.14f, r.Height * 0.3f), tint, FontStyle.Bold, false, 200f, null);
            Draw.Wrapped(g, q[0], new RectangleF(r.Width * 0.1f, r.Height * 0.12f, r.Width * 0.8f, r.Height * (hasAuthor ? 0.62f : 0.76f)), Theme.Text, FontStyle.Bold, 90f);
            if (hasAuthor) Draw.Text(g, "— " + q[1], new RectangleF(r.Width * 0.1f, r.Height * 0.78f, r.Width * 0.8f, r.Height * 0.12f), tint, FontStyle.Italic, false, 40f, null);
        }
    }
}
