// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
// Classroom Toolkit - settings, data folders, list files and sample content.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClassroomToolkit
{
    class Student { public string Name; public int Weight; }
    class QA { public string Q; public string A; public string Category; }

    static class Store
    {
        public static string DataDir;
        public static bool Portable;
        static readonly Dictionary<string, string> settings = new Dictionary<string, string>();
        public static readonly Random Rng = new Random();

        public static void Init()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string portable = Path.Combine(exeDir, "Data");
            if (Directory.Exists(portable) && CanWrite(portable)) { DataDir = portable; Portable = true; }
            else DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Classroom Toolkit");
            try { Directory.CreateDirectory(DataDir); }
            catch { DataDir = Path.Combine(Path.GetTempPath(), "Classroom Toolkit"); Directory.CreateDirectory(DataDir); }
            LoadSettings();
            Samples.Create();
        }

        static bool CanWrite(string dir)
        {
            try
            {
                string p = Path.Combine(dir, ".write-test");
                File.WriteAllText(p, "x"); File.Delete(p);
                return true;
            }
            catch { return false; }
        }

        public static string Dir(string sub)
        {
            string p = Path.Combine(DataDir, sub);
            if (!Directory.Exists(p)) Directory.CreateDirectory(p);
            return p;
        }

        static string SettingsPath { get { return Path.Combine(DataDir, "settings.txt"); } }

        static void LoadSettings()
        {
            settings.Clear();
            try
            {
                if (!File.Exists(SettingsPath)) return;
                foreach (string line in File.ReadAllLines(SettingsPath, Encoding.UTF8))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) settings[line.Substring(0, i)] = Unescape(line.Substring(i + 1));
                }
            }
            catch { }
        }

        static string Escape(string s) { return s.Replace("\\", "\\\\").Replace("\r", "").Replace("\n", "\\n"); }

        static string Unescape(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    i++;
                    sb.Append(s[i] == 'n' ? "\r\n" : s[i].ToString());
                }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        public static string Get(string key, string def)
        {
            string v;
            return settings.TryGetValue(key, out v) ? v : def;
        }

        public static int GetInt(string key, int def)
        {
            int v;
            return int.TryParse(Get(key, ""), out v) ? v : def;
        }

        public static void Set(string key, string value)
        {
            settings[key] = value ?? "";
            try
            {
                File.WriteAllLines(SettingsPath, settings.OrderBy(k => k.Key).Select(k => k.Key + "=" + Escape(k.Value)).ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        public static void Set(string key, int value) { Set(key, value.ToString()); }

        public static string ReadText(string path)
        {
            try
            {
                if (!File.Exists(path)) return "";
                byte[] raw = File.ReadAllBytes(path);
                // Files saved by older tools are often ANSI rather than UTF-8.
                try { return new UTF8Encoding(false, true).GetString(raw).TrimStart('﻿'); }
                catch { return Encoding.GetEncoding(1252).GetString(raw); }
            }
            catch { return ""; }
        }

        public static void WriteText(string path, string text)
        {
            try { File.WriteAllText(path, text ?? "", new UTF8Encoding(true)); }
            catch (Exception ex) { MessageBox.Show("Could not save " + path + "\n\n" + ex.Message, "Classroom Toolkit"); }
        }

        public static string[] Lines(string path)
        {
            return ReadText(path).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        // One name per line. "Name=3" gives that student three tickets in the random picker.
        public static List<Student> LoadClass(string path)
        {
            var list = new List<Student>();
            if (string.IsNullOrEmpty(path)) return list;
            foreach (string raw in Lines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("[")) continue;
                var s = new Student { Name = line, Weight = 1 };
                int i = line.LastIndexOf('=');
                int w;
                if (i > 0 && int.TryParse(line.Substring(i + 1).Trim(), out w))
                {
                    s.Name = line.Substring(0, i).Trim();
                    s.Weight = Math.Max(0, Math.Min(20, w));
                }
                if (s.Name.Length > 0) list.Add(s);
            }
            return list;
        }

        // Accepts "Question | Answer" lines, or pairs of "Q=" and "A=" lines.
        // A line in [square brackets] starts a new category.
        public static List<QA> LoadQuestions(string path)
        {
            var list = new List<QA>();
            if (string.IsNullOrEmpty(path)) return list;
            string cat = "";
            QA pending = null;
            foreach (string raw in Lines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string c = line.Substring(1, line.Length - 2).Trim();
                    cat = c.Equals("QuestionAndAnswer", StringComparison.OrdinalIgnoreCase) ? "" : c;
                    continue;
                }
                if (line.StartsWith("Q=", StringComparison.OrdinalIgnoreCase))
                {
                    pending = new QA { Q = Clean(line.Substring(2)), A = "", Category = cat };
                    list.Add(pending);
                }
                else if (line.StartsWith("A=", StringComparison.OrdinalIgnoreCase) && pending != null)
                {
                    pending.A = Clean(line.Substring(2));
                    pending = null;
                }
                else
                {
                    int i = line.IndexOf('|');
                    if (i < 0) i = line.IndexOf('\t');
                    if (i > 0) list.Add(new QA { Q = Clean(line.Substring(0, i)), A = Clean(line.Substring(i + 1)), Category = cat });
                    else list.Add(new QA { Q = Clean(line), A = "", Category = cat });
                }
            }
            return list;
        }

        static string Clean(string s) { return s.Replace("_#_", " ").Trim(); }

        public static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                T t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }
    }

    // A drop-down of the text files in one data folder, with New and Edit buttons.
    class FileChooser
    {
        public readonly ComboBox Combo;
        readonly string folder, key, help, kind;
        readonly Form host;
        public event EventHandler Changed;
        bool loading;

        public FileChooser(Form host, Control.ControlCollection bar, string sub, string kind, string help)
        {
            this.host = host; this.kind = kind; this.help = help;
            folder = Store.Dir(sub);
            key = "last." + sub;

            Combo = new ComboBox();
            Combo.DropDownStyle = ComboBoxStyle.DropDownList;
            Combo.Width = Theme.S(210);
            Combo.Font = Theme.Font(10.5f);
            Combo.BackColor = Theme.Panel2; Combo.ForeColor = System.Drawing.Color.White;
            Combo.FlatStyle = FlatStyle.Flat;
            Combo.Margin = new Padding(Theme.S(3), Theme.S(6), Theme.S(3), Theme.S(3));
            Combo.TabStop = false;
            bar.Add(Combo);
            Combo.SelectedIndexChanged += delegate
            {
                if (loading) return;
                Store.Set(key, Combo.Text);
                Fire();
            };

            var edit = ToolForm.MakeButton("Edit", false);
            edit.Click += delegate { Edit(); };
            bar.Add(edit);
            var add = ToolForm.MakeButton("New", false);
            add.Click += delegate { New(); };
            bar.Add(add);
            var import = ToolForm.MakeButton("Import", false);
            import.Click += delegate { Import(); };
            bar.Add(import);
            Reload(Store.Get(key, ""));
        }

        void Fire() { if (Changed != null) Changed(this, EventArgs.Empty); }

        public string Path
        {
            get { return Combo.SelectedItem == null ? null : System.IO.Path.Combine(folder, Combo.Text + ".txt"); }
        }

        void Reload(string select)
        {
            loading = true;
            Combo.Items.Clear();
            foreach (string f in Directory.GetFiles(folder, "*.txt").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                Combo.Items.Add(System.IO.Path.GetFileNameWithoutExtension(f));
            int i = Combo.Items.IndexOf(select);
            if (Combo.Items.Count > 0) Combo.SelectedIndex = i >= 0 ? i : 0;
            loading = false;
        }

        void Edit()
        {
            if (Path == null) { New(); return; }
            string text = TextEditDialog.Show(host, "Edit " + kind + " - " + Combo.Text, help, Store.ReadText(Path));
            if (text == null) return;
            Store.WriteText(Path, text);
            Fire();
        }

        void New()
        {
            string name = InputDialog.Ask(host, "New " + kind, "Name for the new " + kind + ":", "");
            if (string.IsNullOrEmpty(name)) return;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '-');
            string p = System.IO.Path.Combine(folder, name + ".txt");
            if (!File.Exists(p)) Store.WriteText(p, "");
            Reload(name);
            Store.Set(key, name);
            Edit();
            Fire();
        }

        void Import()
        {
            using (var d = new OpenFileDialog())
            {
                d.Title = "Import " + kind;
                d.Filter = "Text files (*.txt;*.csv)|*.txt;*.csv|All files (*.*)|*.*";
                if (d.ShowDialog(host) != DialogResult.OK) return;
                string name = System.IO.Path.GetFileNameWithoutExtension(d.FileName);
                Store.WriteText(System.IO.Path.Combine(folder, name + ".txt"), Store.ReadText(d.FileName));
                Reload(name);
                Store.Set(key, name);
                Fire();
            }
        }
    }

    static class Samples
    {
        public const string ClassHelp = "One student per line.\nOptional: add =2 or =3 after a name to give extra tickets in the random picker, =0 to skip.";
        public const string QuestionHelp = "One question per line as:  Question | Answer\nOptional: a line like [Science] starts a category (used by the Grid Quiz).\nLines of Q=... followed by A=... are also understood.";
        public const string StatementHelp = "A line starting with # is a section heading. Every other line is one statement.\nPlaceholders: {name} {subject} {he} {him} {his} {himself} - use {He} or {His} to start a sentence.\nAfter {he}, write verbs as {is|are} or {works|work} so that \"they\" also reads correctly.";
        public const string QuoteHelp = "One quote per line. Optionally add the author after a | character.";

        static void Make(string sub, string name, string text)
        {
            string dir = Store.Dir(sub);
            if (Directory.GetFiles(dir, "*.txt").Length > 0) return;
            Store.WriteText(Path.Combine(dir, name + ".txt"), text.Replace("\r\n", "\n").Replace("\n", "\r\n"));
        }

        public static void Create()
        {
            try
            {
                Make("Classes", "Sample class",
@"Aisha Khan
Ben Carter
Chloe Evans
Daniel Okafor
Ella Thompson
Finn Murphy
Grace Liu
Harry Patel
Isla Robinson
Jack Novak
Katie Hughes
Leo Fernandez
Mia Campbell
Noah Ahmed
Olivia Wright
Poppy Singh
Quinn Taylor
Ryan Kowalski
Sophie Bennett
Tom Adeyemi
Uma Sharma
Violet Clarke
Will Jackson
Zara Hussain");

                Make("Questions", "Sample - mixed quiz",
@"# Replace these with your own. Use New to start a fresh set.
[Science]
What gas do plants take in from the air to make food? | Carbon dioxide
What is the chemical symbol for water? | H2O
Which planet is known as the Red Planet? | Mars
What force pulls objects towards the centre of the Earth? | Gravity
What part of a cell contains its genetic material? | The nucleus
What is the boiling point of pure water at sea level in degrees Celsius? | 100
[Maths]
What is 7 multiplied by 8? | 56
What is the square root of 144? | 12
How many degrees are there in a right angle? | 90
What is 25% of 200? | 50
What do the angles in a triangle add up to? | 180 degrees
What is the next prime number after 13? | 17
[Geography]
What is the longest river in the United Kingdom? | The River Severn
On which continent is the Sahara Desert? | Africa
What is the capital city of Australia? | Canberra
Which ocean lies between Europe and North America? | The Atlantic
What is the highest mountain in Scotland? | Ben Nevis
What imaginary line divides the Earth into northern and southern halves? | The Equator
[English]
What do we call a word that describes a noun? | An adjective
What is the plural of 'child'? | Children
Which punctuation mark ends a question? | A question mark
What is a word with the opposite meaning called? | An antonym
What do we call a story a person writes about their own life? | An autobiography
What is the past tense of 'run'? | Ran
[History]
In which year did the Battle of Hastings take place? | 1066
Which queen reigned over Britain from 1837 to 1901? | Queen Victoria
In which city did the Great Fire of 1666 start? | London
Which wall was built across northern England by the Romans? | Hadrian's Wall
In which year did the Second World War end? | 1945
Who was the first Tudor king of England? | Henry VII");

                Make("Statements", "Sample report statements",
@"# Opening
{name} has made a very positive start in {subject} this term.
{name} has settled well and is making steady progress in {subject}.
{name} has found some aspects of {subject} challenging this term.
# Effort
{He} consistently {gives|give} {his} best effort in lessons.
{He} {works|work} well when focused but can be distracted by others.
{He} {needs|need} to put more effort into completing tasks in the time given.
# Homework
{His} homework is always completed carefully and handed in on time.
{His} homework is usually completed, although more detail would help.
{He} must make sure that homework is handed in by the deadline.
# Next steps
To improve further, {he} should check {his} work carefully before handing it in.
To improve further, {he} should contribute {his} ideas more often in class discussion.
To improve further, {he} should ask for help as soon as {he} {is|are} unsure.
# Closing
Well done, {name}, on a successful term.
I look forward to seeing {name} build on this next term.");

                Make("Quotes", "Sample quotes",
@"We are what we repeatedly do. Excellence, then, is not an act, but a habit. | Will Durant, summarising Aristotle
It does not matter how slowly you go as long as you do not stop. | Attributed to Confucius
The only way to learn mathematics is to do mathematics. | Paul Halmos
I have not failed. I've just found 10,000 ways that won't work. | Attributed to Thomas Edison
Well done is better than well said. | Benjamin Franklin
The beginning is the most important part of the work. | Plato
A journey of a thousand miles begins with a single step. | Lao Tzu
Nothing in life is to be feared, it is only to be understood. | Attributed to Marie Curie
Today I will listen first, then speak.
Mistakes are proof that I am trying.");
            }
            catch { }
        }
    }
}
