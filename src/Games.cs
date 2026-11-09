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
    class QuestionDialog : Form
    {
        readonly QA qa;
        bool revealed;
        readonly Canvas view = new Canvas();
        readonly Button reveal, right, wrong;
        readonly string heading;
        readonly Color tint;

        QuestionDialog(QA qa, string heading, Color tint)
        {
            this.qa = qa; this.heading = heading; this.tint = tint;
            Text = "Question";
            BackColor = Theme.Bg; Font = Theme.Font(10f);
            FormBorderStyle = FormBorderStyle.Sizable; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Icon = AppIcon.Get();
            KeyPreview = true;

            var foot = new FlowLayoutPanel();
            foot.Dock = DockStyle.Bottom; foot.AutoSize = true; foot.BackColor = Theme.Panel;
            foot.Padding = new Padding(Theme.S(10)); foot.FlowDirection = FlowDirection.RightToLeft;
            wrong = ToolForm.MakeButton("Wrong (N)", false); wrong.BackColor = Theme.Red; wrong.Font = Theme.Font(13f, FontStyle.Bold);
            right = ToolForm.MakeButton("Correct (Y)", false); right.BackColor = Theme.Green; right.Font = Theme.Font(13f, FontStyle.Bold);
            reveal = ToolForm.MakeButton("Show answer (Space)", true); reveal.Font = Theme.Font(13f, FontStyle.Bold);
            wrong.Click += delegate { DialogResult = DialogResult.No; };
            right.Click += delegate { DialogResult = DialogResult.Yes; };
            reveal.Click += delegate { Reveal(); };
            foot.Controls.Add(wrong); foot.Controls.Add(right); foot.Controls.Add(reveal);
            view.Dock = DockStyle.Fill;
            view.Paint += delegate(object s, PaintEventArgs e) { Render(e.Graphics, view.ClientRectangle); };
            Controls.Add(view); Controls.Add(foot);
            if (string.IsNullOrEmpty(qa.A)) { revealed = true; reveal.Visible = false; }
        }

        void Reveal() { revealed = true; reveal.Visible = false; view.Invalidate(); }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter) { if (!revealed) Reveal(); return true; }
            if (keyData == Keys.Y) { DialogResult = DialogResult.Yes; return true; }
            if (keyData == Keys.N) { DialogResult = DialogResult.No; return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void Render(Graphics g, Rectangle r)
        {
            Draw.Quality(g);
            g.Clear(Theme.Bg);
            Draw.Text(g, heading, new RectangleF(0, r.Height * 0.03f, r.Width, r.Height * 0.1f), tint, FontStyle.Bold, false, 44f, null);
            float qh = revealed && !string.IsNullOrEmpty(qa.A) ? 0.44f : 0.8f;
            Draw.Wrapped(g, qa.Q, new RectangleF(r.Width * 0.06f, r.Height * 0.15f, r.Width * 0.88f, r.Height * qh), Theme.Text, FontStyle.Bold, 90f);
            if (revealed && !string.IsNullOrEmpty(qa.A))
            {
                var box = new RectangleF(r.Width * 0.06f, r.Height * 0.63f, r.Width * 0.88f, r.Height * 0.32f);
                Draw.FillRound(g, box, 18, Draw.Mix(Theme.Bg, Theme.Green, 0.3));
                Draw.Wrapped(g, qa.A, Draw.Inset(box, 12), Color.White, FontStyle.Bold, 80f);
            }
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

        public GameForm(string title, int w, int h) : base(title, w, h)
        {
            AddLabel("Questions");
            Chooser = new FileChooser(this, Bar.Controls, "Questions", "question set", Samples.QuestionHelp);
            Chooser.Changed += delegate { Reload(); };
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
            if (q == null) { MessageBox.Show(this, "This question set is empty. Press Edit to add some questions.", Text); return null; }
            return QuestionDialog.Ask(this, q, TeamName[Turn], TeamTint[Turn]);
        }

        protected void DrawTurn(Graphics g, RectangleF r, string message)
        {
            Draw.Text(g, message ?? (TeamName[Turn] + " to play"), r, message != null ? Theme.Text : TeamTint[Turn], FontStyle.Bold, false, 44f, null);
        }

        protected bool NoQuestions(Graphics g, Rectangle r)
        {
            if (Questions.Count > 0) return false;
            g.Clear(Theme.Bg);
            Draw.Wrapped(g, "This question set is empty. Press Edit to add questions, one per line, as  Question | Answer", Draw.Inset(r, r.Width * 0.1f), Theme.Muted, FontStyle.Regular, 26f);
            return true;
        }
    }

    class FlashCardsForm : GameForm
    {
        int index;
        bool flipped;
        List<QA> deck = new List<QA>();

        public FlashCardsForm() : base("Flash Cards", 900, 560)
        {
            AddButton("Previous", delegate { Step(-1); });
            AddButton("Flip (Space)", delegate { Flip(); }, true);
            AddButton("Next", delegate { Step(1); });
            AddButton("Shuffle", delegate { Store.Shuffle(deck); index = 0; flipped = false; View.Invalidate(); });
            AddWindowButtons(false);
            View.Click += delegate { Flip(); };
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
            QA q = deck[index];
            var card = new RectangleF(r.Width * 0.06f, r.Height * 0.06f, r.Width * 0.88f, r.Height * 0.78f);
            Draw.FillRound(g, card, 26, flipped ? Draw.Mix(Theme.Bg, Theme.Green, 0.34) : Color.White);
            string text = flipped ? (string.IsNullOrEmpty(q.A) ? "(no answer given)" : q.A) : q.Q;
            Draw.Wrapped(g, text, Draw.Inset(card, card.Height * 0.09f), flipped ? Color.White : Color.FromArgb(20, 24, 31), FontStyle.Bold, 110f);
            string cat = string.IsNullOrEmpty(q.Category) ? "" : q.Category + "   -   ";
            Draw.Label(g, cat + "Card " + (index + 1) + " of " + deck.Count + (flipped ? "   -   answer" : "   -   question"),
                new RectangleF(0, r.Height * 0.88f, r.Width, Theme.S(30)), Theme.Muted, 12f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    class NoughtsForm : GameForm
    {
        int[] cells = new int[9]; // 0 empty, 1 first team, 2 second team
        int winner;               // 0 playing, 1/2 won, 3 draw
        RectangleF board;

        public NoughtsForm() : base("Noughts and Crosses Quiz", 760, 700)
        {
            AddButton("New game", delegate { NewGame(); }, true);
            AddWindowButtons(false);
            View.MouseDown += delegate(object s, MouseEventArgs e) { Hit(e.Location); };
            Reload();
        }

        protected override void NewGame() { cells = new int[9]; winner = 0; Turn = 0; View.Invalidate(); }

        void Hit(Point p)
        {
            if (winner != 0 || !board.Contains(p)) return;
            int c = (int)((p.X - board.X) / (board.Width / 3)), r = (int)((p.Y - board.Y) / (board.Height / 3));
            int i = Math.Min(2, r) * 3 + Math.Min(2, c);
            if (cells[i] != 0) return;
            bool? ok = AskTurn();
            if (ok == null) return;
            if (ok.Value) cells[i] = Turn + 1;
            winner = Winner();
            if (winner == 0) Turn = 1 - Turn; else if (winner != 3) Sound.Alarm();
            View.Invalidate();
        }

        int Winner()
        {
            int[][] lines = { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } };
            foreach (var l in lines)
                if (cells[l[0]] != 0 && cells[l[0]] == cells[l[1]] && cells[l[1]] == cells[l[2]]) return cells[l[0]];
            return cells.All(c => c != 0) ? 3 : 0;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            if (NoQuestions(g, r)) return;
            g.Clear(Theme.Bg);
            float head = r.Height * 0.12f;
            string msg = winner == 3 ? "It's a draw" : winner != 0 ? TeamName[winner - 1] + " wins!" : null;
            if (msg == null) Draw.Text(g, TeamName[Turn] + " (" + (Turn == 0 ? "X" : "O") + ") - choose a square", new RectangleF(0, 4, r.Width, head), TeamTint[Turn], FontStyle.Bold, false, 40f, null);
            else DrawTurn(g, new RectangleF(0, 4, r.Width, head), msg);
            float size = Math.Min(r.Width * 0.92f, (r.Height - head) * 0.94f);
            board = new RectangleF((r.Width - size) / 2f, head + (r.Height - head - size) / 2f, size, size);
            float cs = size / 3f;
            for (int i = 0; i < 9; i++)
            {
                var cell = Draw.Inset(new RectangleF(board.X + (i % 3) * cs, board.Y + (i / 3) * cs, cs, cs), cs * 0.04f);
                Draw.FillRound(g, cell, cs * 0.1f, cells[i] == 0 ? Theme.Panel : Draw.Mix(Theme.Bg, TeamTint[cells[i] - 1], 0.3));
                if (cells[i] == 0) continue;
                using (var pen = new Pen(TeamTint[cells[i] - 1], cs * 0.11f))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                    var m = Draw.Inset(cell, cs * 0.2f);
                    if (cells[i] == 1) { g.DrawLine(pen, m.Left, m.Top, m.Right, m.Bottom); g.DrawLine(pen, m.Right, m.Top, m.Left, m.Bottom); }
                    else g.DrawEllipse(pen, m);
                }
            }
        }
    }

    class FourForm : GameForm
    {
        const int Cols = 7, Rows = 6;
        int[,] grid = new int[Cols, Rows]; // [column, row] with row 0 at the bottom
        int winner;
        RectangleF board;
        int hover = -1;

        public FourForm() : base("Four in a Row Quiz", 900, 720)
        {
            AddButton("New game", delegate { NewGame(); }, true);
            AddWindowButtons(false);
            View.MouseDown += delegate(object s, MouseEventArgs e) { Drop(Column(e.Location)); };
            View.MouseMove += delegate(object s, MouseEventArgs e) { int c = Column(e.Location); if (c != hover) { hover = c; View.Invalidate(); } };
            Reload();
        }

        protected override void NewGame() { grid = new int[Cols, Rows]; winner = 0; Turn = 0; View.Invalidate(); }

        int Column(Point p)
        {
            if (board.Width <= 0 || p.X < board.X || p.X >= board.Right) return -1;
            return Math.Min(Cols - 1, (int)((p.X - board.X) / (board.Width / Cols)));
        }

        void Drop(int c)
        {
            if (winner != 0 || c < 0) return;
            int row = -1;
            for (int y = 0; y < Rows; y++) if (grid[c, y] == 0) { row = y; break; }
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

        bool Wins(int c, int r)
        {
            int who = grid[c, r];
            int[][] dirs = { new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 }, new[] { 1, -1 } };
            foreach (var d in dirs)
            {
                int run = 1;
                for (int sign = -1; sign <= 1; sign += 2)
                    for (int k = 1; k < 4; k++)
                    {
                        int x = c + d[0] * k * sign, y = r + d[1] * k * sign;
                        if (x < 0 || y < 0 || x >= Cols || y >= Rows || grid[x, y] != who) break;
                        run++;
                    }
                if (run >= 4) return true;
            }
            return false;
        }

        protected override void Render(Graphics g, Rectangle r)
        {
            if (NoQuestions(g, r)) return;
            g.Clear(Theme.Bg);
            float head = r.Height * 0.11f;
            string msg = winner == 3 ? "It's a draw" : winner != 0 ? TeamName[winner - 1] + " wins!" : null;
            if (msg == null) Draw.Text(g, TeamName[Turn] + " - choose a column", new RectangleF(0, 4, r.Width, head), TeamTint[Turn], FontStyle.Bold, false, 40f, null);
            else DrawTurn(g, new RectangleF(0, 4, r.Width, head), msg);
            float cs = Math.Min(r.Width * 0.94f / Cols, (r.Height - head) * 0.95f / Rows);
            board = new RectangleF((r.Width - cs * Cols) / 2f, head + (r.Height - head - cs * Rows) / 2f, cs * Cols, cs * Rows);
            Draw.FillRound(g, Draw.Inset(board, -cs * 0.08f), cs * 0.2f, Color.FromArgb(30, 64, 140));
            for (int c = 0; c < Cols; c++)
                for (int y = 0; y < Rows; y++)
                {
                    var hole = Draw.Inset(new RectangleF(board.X + c * cs, board.Bottom - (y + 1) * cs, cs, cs), cs * 0.1f);
                    Color col = grid[c, y] == 1 ? Color.FromArgb(250, 204, 21) : grid[c, y] == 2 ? Theme.Red : (c == hover && winner == 0 ? Theme.Panel2 : Theme.Bg);
                    using (var b = new SolidBrush(col)) g.FillEllipse(b, hole);
                }
        }

    }

    // A board of point values. Categories come from [headings] in the question set.
    class GridQuizForm : GameForm
    {
        class Cell { public QA Q; public int Points; public bool Done; public RectangleF Rect; }
        readonly List<string> cats = new List<string>();
        readonly List<List<Cell>> columns = new List<List<Cell>>();
        NumericUpDown teams;
        int[] scores = new int[4];
        static readonly Color[] Tints = { Theme.Accent, Theme.Red, Theme.Green, Theme.Amber };
        static readonly string[] Names = { "Blue", "Red", "Green", "Gold" };
        int turn;

        public GridQuizForm() : base("Grid Quiz", 1000, 680)
        {
            AddLabel("Teams");
            teams = AddNumber(1, 4, Store.GetInt("grid.teams", 2), 48);
            teams.ValueChanged += delegate { Store.Set("grid.teams", (int)teams.Value); turn = 0; View.Invalidate(); };
            AddButton("New game", delegate { NewGame(); }, true);
            AddWindowButtons(false);
            View.MouseDown += delegate(object s, MouseEventArgs e) { Hit(e.Location); };
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
                    bool? ok = QuestionDialog.Ask(this, cell.Q, Names[turn] + " team - for " + cell.Points, Tints[turn]);
                    if (ok == null) return;
                    cell.Done = true;
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
            float foot = r.Height * 0.14f, head = r.Height * 0.11f;
            float cw = r.Width / (float)Math.Max(1, columns.Count);
            float ch = (r.Height - foot - head) / 5f;
            for (int c = 0; c < columns.Count; c++)
            {
                Draw.Text(g, cats[c], Draw.Inset(new RectangleF(c * cw, 0, cw, head), 8), Theme.Text, FontStyle.Bold, true, 30f, null);
                for (int i = 0; i < columns[c].Count; i++)
                {
                    Cell cell = columns[c][i];
                    cell.Rect = Draw.Inset(new RectangleF(c * cw, head + i * ch, cw, ch), 6);
                    Draw.FillRound(g, cell.Rect, 14, cell.Done ? Theme.Panel : Color.FromArgb(30, 64, 160));
                    if (!cell.Done) Draw.Text(g, cell.Points.ToString(), Draw.Inset(cell.Rect, cell.Rect.Height * 0.18f), Color.FromArgb(250, 204, 21), FontStyle.Bold, false, 120f, "000");
                }
            }
            float tw = r.Width / (float)n;
            for (int t = 0; t < n; t++)
            {
                var box = Draw.Inset(new RectangleF(t * tw, r.Height - foot, tw, foot), 6);
                Draw.FillRound(g, box, 14, Draw.Mix(Theme.Bg, Tints[t], t == turn ? 0.6 : 0.22));
                Draw.Text(g, Names[t] + ": " + scores[t] + (t == turn && n > 1 ? "  ◀ turn" : ""), Draw.Inset(box, 8), Color.White, FontStyle.Bold, false, 44f, null);
            }
        }
    }
}
