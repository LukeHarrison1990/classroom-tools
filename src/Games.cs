// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - quiz games driven by question sets: flash cards, noughts and crosses, four in a row, grid quiz.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    // Shows one question full-window; the teacher reveals the answer and marks it right or wrong.
    class QuestionDialog : BaseForm
    {
        readonly QA qa;
        bool revealed;
        readonly Canvas view = new Canvas();
        readonly DisplayButtons buttons;
        readonly string heading;
        readonly Color tint;

        QuestionDialog(QA qa, string heading, Color tint)
        {
            this.qa = qa; this.heading = heading; this.tint = tint;
            Text = heading;
            FormBorderStyle = FormBorderStyle.Sizable; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;
            view.Dock = DockStyle.Fill;
            view.Paint += delegate(object s, PaintEventArgs e) { Render(e.Graphics, view.ClientRectangle); };
            Controls.Add(view);
            buttons = new DisplayButtons(view);
            if (string.IsNullOrEmpty(qa.A)) revealed = true;
        }

        void Reveal() { revealed = true; view.Invalidate(); }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter) { if (!revealed) Reveal(); return true; }
            if (keyData == Keys.Y) { DialogResult = DialogResult.Yes; return true; }
            if (keyData == Keys.N) { DialogResult = DialogResult.No; return true; }
            if (keyData == Keys.Escape) { DialogResult = DialogResult.Cancel; return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void Render(Graphics g, Rectangle r)
        {
            Draw.Quality(g);
            g.Clear(Theme.Bg);
            float w = r.Width, h = r.Height;
            float pad = Math.Max(Theme.S(14), h * 0.04f);
            float btnH = Math.Max(Theme.S(48), Math.Min(Theme.S(84), h * 0.12f));
            using (var b = new SolidBrush(tint)) g.FillRectangle(b, 0, 0, w, Theme.S(8));
            float headH = Math.Max(Theme.S(30), h * 0.08f);
            Draw.Text(g, heading, new RectangleF(pad, pad, w - 2 * pad, headH), tint, FontStyle.Bold, false, 40f, null);
            float by = h - pad - btnH;
            float top = pad + headH + pad * 0.6f;
            float avail = by - pad - top;
            bool answer = revealed && !string.IsNullOrEmpty(qa.A);
            var qCard = new RectangleF(pad, top, w - 2 * pad, answer ? avail * 0.58f : avail);
            Draw.FillRound(g, qCard, Theme.S(22), Theme.Panel);
            Draw.Wrapped(g, qa.Q, Draw.Inset(qCard, Math.Min(qCard.Height, qCard.Width) * 0.08f), Theme.Text, FontStyle.Bold, 90f);
            if (answer)
            {
                var aCard = new RectangleF(pad, qCard.Bottom + pad * 0.6f, w - 2 * pad, avail - qCard.Height - pad * 0.6f);
                Draw.FillRound(g, aCard, Theme.S(22), Draw.Mix(Theme.Panel, Theme.Green, 0.4));
                Draw.Wrapped(g, qa.A, Draw.Inset(aCard, Math.Min(aCard.Height, aCard.Width) * 0.12f), Color.White, FontStyle.Bold, 80f);
            }
            buttons.Clear();
            if (!revealed)
            {
                float bw = btnH * 4.4f;
                buttons.Add(new RectangleF((w - bw) / 2, by, bw, btnH), Glyph.Swap, "Show answer", true, Reveal);
            }
            else
            {
                float bw = btnH * 3.6f, gap = btnH * 0.4f;
                float x = (w - (bw * 2 + gap)) / 2;
                buttons.Add(new RectangleF(x, by, bw, btnH), Glyph.Close, "Wrong", Theme.Red, delegate { DialogResult = DialogResult.No; });
                buttons.Add(new RectangleF(x + bw + gap, by, bw, btnH), Glyph.Check, "Correct", Color.FromArgb(22, 163, 74), delegate { DialogResult = DialogResult.Yes; });
            }
            buttons.Paint(g);
            Draw.Label(g, revealed ? "Y correct  ·  N wrong  ·  Esc cancel" : "Space shows the answer  ·  Esc cancels", new RectangleF(0, h - pad * 0.95f, w, pad * 0.9f), Theme.Muted, 9f, FontStyle.Regular, StringAlignment.Center);
        }

        // Returns true (correct), false (wrong) or null (cancelled).
        public static bool? Ask(Form owner, QA qa, string heading, Color tint)
        {
            using (var d = new QuestionDialog(qa, heading, tint))
            {
                d.Size = new Size((int)(owner.Width * 0.86), (int)(owner.Height * 0.86));
                d.TopMost = owner.TopMost;
                var result = d.ShowDialog(owner);
                if (result == DialogResult.Yes) { Sound.Good(); return true; }
                if (result == DialogResult.No) { Sound.Bad(); return false; }
                return null;
            }
        }
    }

    class GameForm : ToolForm
    {
        protected FileChooser Chooser;
        protected List<QA> Questions = new List<QA>();
        List<QA> queue = new List<QA>();
        protected static readonly Color[] TeamTint = { Theme.Accent, Theme.Red };
        protected string[] TeamName = { "Blue team", "Red team" };
        protected int Turn;
        protected DisplayButtons Buttons;

        public GameForm(string title, int w, int h) : base(title, w, h)
        {
            Chooser = new FileChooser(this, Bar.Controls, "Questions", "question set", Samples.QuestionHelp);
            Chooser.Changed += delegate { Reload(); };
            Buttons = new DisplayButtons(View);
        }

        protected void AddNewGameButton()
        {
            AddDivider();
            AddButton("New game", Glyph.Reset, delegate { NewGame(); }, false);
        }

        protected void Reload()
        {
            Questions = Store.LoadQuestions(Chooser.Path).Where(q => !string.IsNullOrEmpty(q.Q)).ToList();
            queue.Clear();
            NewGame();
        }

        protected virtual void NewGame() { View.Invalidate(); }

        protected QA NextQuestion()
        {
            if (Questions.Count == 0) return null;
            if (queue.Count == 0) { queue = new List<QA>(Questions); Store.Shuffle(queue); }
            QA q = queue[queue.Count - 1];
            queue.RemoveAt(queue.Count - 1);
            return q;
        }

        // Asks the next question for the team whose turn it is.
        protected bool? AskTurn()
        {
            QA q = NextQuestion();
            if (q == null) { MessageBox.Show(this, "This question set is empty. Click the pencil button to add some questions.", Text); return null; }
            return QuestionDialog.Ask(this, q, TeamName[Turn], TeamTint[Turn]);
        }

        // A coloured pill across the top: whose turn it is, or the result.
        protected float Banner(Graphics g, Rectangle r, string text, Color tint)
        {
            float h = Math.Max(Theme.S(36), r.Height * 0.075f);
            float pad = Math.Max(Theme.S(10), r.Height * 0.025f);
            Font f = Theme.Semi(Math.Max(10f, h * 0.42f * 72f / g.DpiY));
            float w = Math.Min(r.Width - 2 * pad, g.MeasureString(text, f).Width + h * 1.4f);
            var pill = new RectangleF((r.Width - w) / 2, pad, w, h);
            Draw.FillRound(g, pill, h / 2, tint);
            Draw.Text(g, text, Draw.Inset(pill, h * 0.16f), Color.White, FontStyle.Bold, false, f.SizeInPoints, null);
            return pill.Bottom + pad;
        }

        protected bool NoQuestions(Graphics g, Rectangle r)
        {
            if (Questions.Count > 0) return false;
            g.Clear(Theme.Bg);
            Draw.Wrapped(g, "This question set is empty.\nClick the pencil button to add questions, one per line, as  Question | Answer", Draw.Inset(r, r.Width * 0.1f), Theme.Muted, FontStyle.Regular, 24f);
            return true;
        }
    }

    class FlashCardsForm : GameForm
    {
        int index;
        bool flipped;
        List<QA> deck = new List<QA>();
        RectangleF card;

        public FlashCardsForm() : base("Flash Cards", 940, 600)
        {
            AddDivider();
            AddButton("Shuffle", Glyph.Shuffle, delegate { Store.Shuffle(deck); index = 0; flipped = false; View.Invalidate(); }, false);
            AddWindowButtons(false);
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && card.Contains(e.Location) && !Buttons.Contains(e.Location)) Flip(); };
            Reload();
        }

        protected override void NewGame() { deck = new List<QA>(Questions); index = 0; flipped = false; View.Invalidate(); }
        void Flip() { flipped = !flipped; View.Invalidate(); }

        void Step(int by)
        {
            if (deck.Count == 0) return;
            index = (index + by + deck.Count) % deck.Count;
            flipped = false;
            View.Invalidate();
        }

        protected override bool OnToolKey(Keys key)
        {
            if (key == Keys.Space || key == Keys.Up || key == Keys.Down) { Flip(); return true; }
            if (key == Keys.Right || key == Keys.Enter) { Step(1); return true; }
            if (key == Keys.Left) { Step(-1); return true; }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            if (NoQuestions(g, r)) return;
            g.Clear(Theme.Bg);
            float w = r.Width, h = r.Height;
            float pad = Math.Max(Theme.S(14), h * 0.04f);
            float btnH = Math.Max(Theme.S(44), Math.Min(Theme.S(76), h * 0.11f));
            float by = h - pad - btnH;
            QA q = deck[index];
            card = new RectangleF(w * 0.06f, pad, w * 0.88f, by - pad * 2);
            Draw.FillRound(g, new RectangleF(card.X + Theme.S(4), card.Y + Theme.S(8), card.Width, card.Height), Theme.S(26), Color.FromArgb(70, 0, 0, 0));
            Draw.FillRound(g, card, Theme.S(26), flipped ? Color.FromArgb(21, 128, 61) : Color.White);
            Color ink = flipped ? Color.White : Color.FromArgb(17, 24, 39);
            Color soft = flipped ? Color.FromArgb(200, 255, 255, 255) : Color.FromArgb(110, 120, 135);
            float labelH = Math.Max(Theme.S(20), card.Height * 0.07f);
            string tag = (flipped ? "ANSWER" : "QUESTION") + (string.IsNullOrEmpty(q.Category) ? "" : "  ·  " + q.Category.ToUpperInvariant());
            Draw.Text(g, tag, new RectangleF(card.X, card.Y + labelH * 0.6f, card.Width, labelH), soft, FontStyle.Bold, false, 14f, null);
            string text = flipped ? (string.IsNullOrEmpty(q.A) ? "(no answer given)" : q.A) : q.Q;
            Draw.Wrapped(g, text, new RectangleF(card.X + card.Width * 0.07f, card.Y + labelH * 2f, card.Width * 0.86f, card.Height - labelH * 3.2f), ink, FontStyle.Bold, 110f);
            Draw.Text(g, (index + 1) + " / " + deck.Count, new RectangleF(card.X, card.Bottom - labelH * 1.5f, card.Width, labelH), soft, FontStyle.Regular, false, 14f, null);

            Buttons.Clear();
            float main = btnH * 3f, side = btnH, gp = btnH * 0.3f;
            float x = (w - (side + gp + main + gp + side)) / 2;
            Buttons.Add(new RectangleF(x, by, side, btnH), Glyph.Prev, null, false, delegate { Step(-1); });
            Buttons.Add(new RectangleF(x + side + gp, by, main, btnH), Glyph.Swap, flipped ? "Question" : "Answer", true, Flip);
            Buttons.Add(new RectangleF(x + side + gp + main + gp, by, side, btnH), Glyph.Next, null, false, delegate { Step(1); });
            Buttons.Paint(g);
        }
    }

    class NoughtsForm : GameForm
    {
        int[] cells = new int[9]; // 0 empty, 1 first team, 2 second team
        int winner;               // 0 playing, 1/2 won, 3 draw
        RectangleF board;
        int hover = -1;
        static readonly int[][] Lines = { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } };

        public NoughtsForm() : base("Noughts and Crosses Quiz", 780, 760)
        {
            AddNewGameButton();
            AddWindowButtons(false);
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !Buttons.Contains(e.Location)) Hit(e.Location); };
            View.MouseMove += delegate(object s, MouseEventArgs e) { int c = CellAt(e.Location); if (c != hover) { hover = c; View.Invalidate(); } };
            Reload();
        }

        protected override void NewGame() { cells = new int[9]; winner = 0; Turn = 0; View.Invalidate(); }

        int CellAt(Point p)
        {
            if (!board.Contains(p)) return -1;
            int c = (int)((p.X - board.X) / (board.Width / 3)), r = (int)((p.Y - board.Y) / (board.Height / 3));
            return Math.Min(2, r) * 3 + Math.Min(2, c);
        }

        void Hit(Point p)
        {
            int i = CellAt(p);
            if (winner != 0 || i < 0 || cells[i] != 0) return;
            bool? ok = AskTurn();
            if (ok == null) return;
            if (ok.Value) cells[i] = Turn + 1;
            winner = Winner();
            if (winner == 0) Turn = 1 - Turn; else if (winner != 3) Sound.Alarm();
            View.Invalidate();
        }

        int Winner()
        {
            foreach (var l in Lines)
                if (cells[l[0]] != 0 && cells[l[0]] == cells[l[1]] && cells[l[1]] == cells[l[2]]) return cells[l[0]];
            return cells.All(c => c != 0) ? 3 : 0;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            if (NoQuestions(g, r)) return;
            g.Clear(Theme.Bg);
            string msg = winner == 3 ? "It's a draw" : winner != 0 ? TeamName[winner - 1] + " wins!" : TeamName[Turn] + " (" + (Turn == 0 ? "X" : "O") + ") - choose a square";
            Color tint = winner == 3 ? Theme.Panel2 : winner != 0 ? TeamTint[winner - 1] : TeamTint[Turn];
            float top = Banner(g, r, msg, tint);
            float pad = Math.Max(Theme.S(10), r.Height * 0.025f);
            float btnH = winner != 0 ? Math.Max(Theme.S(44), Math.Min(Theme.S(72), r.Height * 0.1f)) : 0;
            float availH = r.Height - top - pad - (btnH > 0 ? btnH + pad : 0);
            float size = Math.Min(r.Width - 2 * pad, availH);
            board = new RectangleF((r.Width - size) / 2f, top + (availH - size) / 2f, size, size);
            float cs = size / 3f;
            for (int i = 0; i < 9; i++)
            {
                var cell = Draw.Inset(new RectangleF(board.X + (i % 3) * cs, board.Y + (i / 3) * cs, cs, cs), cs * 0.04f);
                Color fill = cells[i] == 0 ? (i == hover && winner == 0 ? Draw.Mix(Theme.Panel, TeamTint[Turn], 0.2) : Theme.Panel) : Draw.Mix(Theme.Panel, TeamTint[cells[i] - 1], 0.25);
                Draw.FillRound(g, cell, cs * 0.12f, fill);
                if (cells[i] == 0) continue;
                using (var pen = new Pen(TeamTint[cells[i] - 1], cs * 0.1f))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                    var m = Draw.Inset(cell, cs * 0.24f);
                    if (cells[i] == 1) { g.DrawLine(pen, m.Left, m.Top, m.Right, m.Bottom); g.DrawLine(pen, m.Right, m.Top, m.Left, m.Bottom); }
                    else g.DrawEllipse(pen, m);
                }
            }
            if (winner == 1 || winner == 2)
                foreach (var l in Lines)
                    if (cells[l[0]] == winner && cells[l[1]] == winner && cells[l[2]] == winner)
                    {
                        Func<int, PointF> centre = i => new PointF(board.X + (i % 3 + 0.5f) * cs, board.Y + (i / 3 + 0.5f) * cs);
                        using (var pen = new Pen(Color.FromArgb(230, Color.White), cs * 0.07f))
                        {
                            pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                            g.DrawLine(pen, centre(l[0]), centre(l[2]));
                        }
                        break;
                    }
            Buttons.Clear();
            if (winner != 0)
            {
                float bw = btnH * 3.6f;
                Buttons.Add(new RectangleF((r.Width - bw) / 2, r.Height - pad - btnH, bw, btnH), Glyph.Reset, "Play again", true, NewGame);
            }
            Buttons.Paint(g);
        }
    }

    class FourForm : GameForm
    {
        const int Cols = 7, Rows = 6;
        int[,] grid = new int[Cols, Rows]; // [column, row] with row 0 at the bottom
        int winner;
        RectangleF board;
        int hover = -1;

        public FourForm() : base("Four in a Row Quiz", 920, 760)
        {
            AddNewGameButton();
            AddWindowButtons(false);
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !Buttons.Contains(e.Location)) Drop(Column(e.Location)); };
            View.MouseMove += delegate(object s, MouseEventArgs e) { int c = Column(e.Location); if (c != hover) { hover = c; View.Invalidate(); } };
            View.MouseLeave += delegate { hover = -1; View.Invalidate(); };
            Reload();
        }

        protected override void NewGame() { grid = new int[Cols, Rows]; winner = 0; Turn = 0; View.Invalidate(); }

        int Column(Point p)
        {
            if (board.Width <= 0 || p.X < board.X || p.X >= board.Right) return -1;
            return Math.Min(Cols - 1, (int)((p.X - board.X) / (board.Width / Cols)));
        }

        int FreeRow(int c)
        {
            for (int y = 0; y < Rows; y++) if (grid[c, y] == 0) return y;
            return -1;
        }

        void Drop(int c)
        {
            if (winner != 0 || c < 0) return;
            int row = FreeRow(c);
            if (row < 0) return;
            bool? ok = AskTurn();
            if (ok == null) return;
            if (ok.Value)
            {
                grid[c, row] = Turn + 1;
                if (Wins(c, row)) { winner = Turn + 1; Sound.Alarm(); }
                else if (Full()) winner = 3;
            }
            if (winner == 0) Turn = 1 - Turn;
            View.Invalidate();
        }

        bool Full()
        {
            for (int c = 0; c < Cols; c++) if (grid[c, Rows - 1] == 0) return false;
            return true;
        }

        bool Wins(int c, int r) { return Line(c, r) != null; }

        // The four (or more) cells in a winning line through (c, r), or null.
        List<Point> Line(int c, int r)
        {
            int who = grid[c, r];
            if (who == 0) return null;
            int[][] dirs = { new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 }, new[] { 1, -1 } };
            foreach (var d in dirs)
            {
                var run = new List<Point> { new Point(c, r) };
                for (int sign = -1; sign <= 1; sign += 2)
                    for (int k = 1; k < 4; k++)
                    {
                        int x = c + d[0] * k * sign, y = r + d[1] * k * sign;
                        if (x < 0 || y < 0 || x >= Cols || y >= Rows || grid[x, y] != who) break;
                        run.Add(new Point(x, y));
                    }
                if (run.Count >= 4) return run;
            }
            return null;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            if (NoQuestions(g, r)) return;
            g.Clear(Theme.Bg);
            string msg = winner == 3 ? "It's a draw" : winner != 0 ? TeamName[winner - 1] + " wins!" : TeamName[Turn] + " - choose a column";
            Color tint = winner == 3 ? Theme.Panel2 : winner != 0 ? TeamTint[winner - 1] : TeamTint[Turn];
            float top = Banner(g, r, msg, tint);
            float pad = Math.Max(Theme.S(10), r.Height * 0.025f);
            float btnH = winner != 0 ? Math.Max(Theme.S(44), Math.Min(Theme.S(72), r.Height * 0.1f)) : 0;
            float availH = r.Height - top - pad - (btnH > 0 ? btnH + pad : 0);
            float cs = Math.Min((r.Width - 2 * pad) / Cols, availH / (Rows + 0.9f));
            float previewH = cs * 0.9f;
            board = new RectangleF((r.Width - cs * Cols) / 2f, top + previewH + (availH - previewH - cs * Rows) / 2f, cs * Cols, cs * Rows);
            // Preview counter above the column under the mouse
            if (winner == 0 && hover >= 0 && FreeRow(hover) >= 0)
                using (var b = new SolidBrush(Color.FromArgb(150, TeamTint[Turn])))
                    g.FillEllipse(b, Draw.Inset(new RectangleF(board.X + hover * cs, board.Y - previewH, cs, cs), cs * 0.14f));
            Draw.FillRound(g, Draw.Inset(board, -cs * 0.1f), cs * 0.22f, Color.FromArgb(44, 54, 76));
            if (winner == 0 && hover >= 0)
                using (var b = new SolidBrush(Color.FromArgb(18, 255, 255, 255))) g.FillRectangle(b, board.X + hover * cs, board.Y, cs, board.Height);
            List<Point> line = null;
            if (winner == 1 || winner == 2)
                for (int c = 0; c < Cols && line == null; c++)
                    for (int y = 0; y < Rows && line == null; y++)
                        if (grid[c, y] == winner) line = Line(c, y);
            for (int c = 0; c < Cols; c++)
                for (int y = 0; y < Rows; y++)
                {
                    var hole = Draw.Inset(new RectangleF(board.X + c * cs, board.Bottom - (y + 1) * cs, cs, cs), cs * 0.1f);
                    if (grid[c, y] == 0) { using (var b = new SolidBrush(Theme.Bg)) g.FillEllipse(b, hole); continue; }
                    Color col = TeamTint[grid[c, y] - 1];
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(hole);
                        using (var pg = new PathGradientBrush(path))
                        {
                            pg.CenterPoint = new PointF(hole.X + hole.Width * 0.4f, hole.Y + hole.Height * 0.38f);
                            pg.CenterColor = Draw.Mix(col, Color.White, 0.3);
                            pg.SurroundColors = new[] { col };
                            g.FillEllipse(pg, hole);
                        }
                    }
                    if (line != null && line.Contains(new Point(c, y)))
                        using (var pen = new Pen(Color.White, Math.Max(3, cs * 0.07f))) g.DrawEllipse(pen, Draw.Inset(hole, cs * 0.06f));
                }
            Buttons.Clear();
            if (winner != 0)
            {
                float bw = btnH * 3.6f;
                Buttons.Add(new RectangleF((r.Width - bw) / 2, r.Height - pad - btnH, bw, btnH), Glyph.Reset, "Play again", true, NewGame);
            }
            Buttons.Paint(g);
        }
    }

    // A board of point values. Categories come from [headings] in the question set.
    class GridQuizForm : GameForm
    {
        class Cell { public QA Q; public int Points; public bool Done; public int Team = -1; public bool Right; public RectangleF Rect; }
        readonly List<string> cats = new List<string>();
        readonly List<List<Cell>> columns = new List<List<Cell>>();
        Stepper teams;
        int[] scores = new int[4];
        static readonly Color[] Tints = { Theme.Accent, Theme.Red, Theme.Green, Theme.Amber };
        static readonly string[] Names = { "Blue", "Red", "Green", "Gold" };
        int turn, hover = -1;

        public GridQuizForm() : base("Grid Quiz", 1040, 700)
        {
            AddDivider();
            AddLabel("Teams");
            teams = AddNumber(1, 4, Store.GetInt("grid.teams", 2), 36);
            teams.ValueChanged += delegate { Store.Set("grid.teams", (int)teams.Value); turn = 0; View.Invalidate(); };
            AddButton("New game", Glyph.Reset, delegate { NewGame(); }, false);
            AddWindowButtons(false);
            View.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) Hit(e.Location); };
            View.MouseMove += delegate(object s, MouseEventArgs e)
            {
                int h = -1, k = 0;
                foreach (var col in columns) foreach (var cell in col) { if (!cell.Done && cell.Rect.Contains(e.Location)) h = k; k++; }
                if (h != hover) { hover = h; View.Cursor = h >= 0 ? Cursors.Hand : Cursors.Default; View.Invalidate(); }
            };
            Reload();
        }

        protected override void NewGame()
        {
            cats.Clear(); columns.Clear(); scores = new int[4]; turn = 0;
            var groups = Questions.GroupBy(q => q.Category ?? "").ToList();
            List<List<QA>> sets;
            if (groups.Count >= 2 && groups.Count(x => x.Count() >= 3) >= 2)
            {
                sets = groups.Where(x => x.Count() >= 1).Take(6).Select(x => x.ToList()).ToList();
                cats.AddRange(groups.Take(6).Select(x => x.Key.Length == 0 ? "General" : x.Key));
            }
            else
            {
                var all = new List<QA>(Questions);
                Store.Shuffle(all);
                int n = Math.Max(1, Math.Min(5, all.Count / 5));
                sets = new List<List<QA>>();
                for (int i = 0; i < n; i++) { sets.Add(all.Skip(i * 5).Take(5).ToList()); cats.Add("Round " + (i + 1)); }
            }
            foreach (var set in sets)
            {
                Store.Shuffle(set);
                var col = new List<Cell>();
                for (int i = 0; i < Math.Min(5, set.Count); i++) col.Add(new Cell { Q = set[i], Points = (i + 1) * 100 });
                columns.Add(col);
            }
            View.Invalidate();
        }

        void Hit(Point p)
        {
            foreach (var col in columns)
                foreach (var cell in col)
                {
                    if (cell.Done || !cell.Rect.Contains(p)) continue;
                    int n = (int)teams.Value;
                    bool? ok = QuestionDialog.Ask(this, cell.Q, Names[turn] + " team  ·  " + cell.Points + " points", Tints[turn]);
                    if (ok == null) return;
                    cell.Done = true; cell.Team = turn; cell.Right = ok.Value;
                    if (ok.Value) scores[turn] += cell.Points;
                    turn = (turn + 1) % n;
                    View.Invalidate();
                    return;
                }
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            if (NoQuestions(g, r)) return;
            g.Clear(Theme.Bg);
            int n = (int)teams.Value;
            float pad = Math.Max(Theme.S(10), r.Height * 0.02f), gap = Theme.S(8);
            float foot = Math.Max(Theme.S(48), r.Height * 0.12f), head = Math.Max(Theme.S(40), r.Height * 0.09f);
            int nc = Math.Max(1, columns.Count);
            float cw = (r.Width - 2 * pad - (nc - 1) * gap) / nc;
            float ch = (r.Height - 2 * pad - foot - head - gap * 6) / 5f;
            int k = 0;
            for (int c = 0; c < columns.Count; c++)
            {
                float x = pad + c * (cw + gap);
                var hr = new RectangleF(x, pad, cw, head);
                Draw.FillRound(g, hr, Theme.S(12), Theme.Panel);
                Draw.Text(g, cats[c], Draw.Inset(hr, head * 0.16f), Theme.Text, FontStyle.Bold, true, 30f, null);
                for (int i = 0; i < columns[c].Count; i++, k++)
                {
                    Cell cell = columns[c][i];
                    cell.Rect = new RectangleF(x, pad + head + gap + i * (ch + gap), cw, ch);
                    if (cell.Done)
                    {
                        Color t = Tints[Math.Max(0, cell.Team)];
                        Draw.FillRound(g, cell.Rect, Theme.S(12), cell.Right ? Draw.Mix(Theme.Panel, t, 0.25) : Draw.Mix(Theme.Bg, Theme.Panel, 0.5));
                        float ic = Math.Min(cell.Rect.Height, cell.Rect.Width) * 0.36f;
                        Glyphs.Draw(g, cell.Right ? Glyph.Check : Glyph.Close, new RectangleF(cell.Rect.X + (cell.Rect.Width - ic) / 2, cell.Rect.Y + (cell.Rect.Height - ic) / 2, ic, ic), cell.Right ? t : Theme.Line);
                        continue;
                    }
                    Color face = k == hover ? Color.FromArgb(48, 92, 210) : Color.FromArgb(32, 68, 170);
                    using (var path = Draw.Round(cell.Rect, Theme.S(12)))
                    using (var grad = new LinearGradientBrush(cell.Rect, Draw.Mix(face, Color.White, 0.08), Draw.Mix(face, Color.Black, 0.15), 90f))
                        g.FillPath(grad, path);
                    Draw.Text(g, cell.Points.ToString(), Draw.Inset(cell.Rect, cell.Rect.Height * 0.2f), Color.FromArgb(253, 210, 60), FontStyle.Bold, false, 120f, "000");
                }
            }
            float tw = (r.Width - 2 * pad - (n - 1) * gap) / n;
            for (int t = 0; t < n; t++)
            {
                var box = new RectangleF(pad + t * (tw + gap), r.Height - pad - foot, tw, foot);
                bool now = t == turn && n > 1;
                Draw.FillRound(g, box, Theme.S(14), now ? Tints[t] : Draw.Mix(Theme.Panel, Tints[t], 0.25));
                Draw.Text(g, Names[t] + "   " + scores[t] + (now ? "   ▲ to play" : ""), Draw.Inset(box, foot * 0.18f), Color.White, FontStyle.Bold, false, 40f, null);
            }
        }
    }
}
