// Copyright (c) 2026 Luke Harrison
// SPDX-License-Identifier: MIT
using System; using System.Collections.Generic; using System.IO; using System.Linq; using System.Reflection; using System.Windows.Forms; using System.Threading;
namespace ClassroomToolkit {
static class Tests {
  static int fails;
  static void Check(bool ok, string what) { Console.WriteLine((ok ? "ok    " : "FAIL  ") + what); if (!ok) fails++; }
  static object Priv(object o, string m, params object[] a) { return o.GetType().GetMethod(m, BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Public).Invoke(o, a); }
  static T Field<T>(object o, string f) { return (T)o.GetType().GetField(f, BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o); }
  static void SetField(object o, string f, object v) { o.GetType().GetField(f, BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(o, v); }
  static void Pump(int ms) { DateTime u = DateTime.UtcNow.AddMilliseconds(ms); while (DateTime.UtcNow < u) { Application.DoEvents(); Thread.Sleep(10); } }
  [STAThread] static int Main(string[] args) {
    Theme.Init(); Store.Init(); Tools.Register();
    string tmp = Path.Combine(Path.GetTempPath(), "ct-tests"); Directory.CreateDirectory(tmp);
    // class lists
    string cp = Path.Combine(tmp, "c.txt"); File.WriteAllText(cp, "Bob Apple\r\nTim Burr=3\r\n\r\n# note\r\nSkip Me=0\r\n");
    var cl = Store.LoadClass(cp);
    Check(cl.Count == 3 && cl[1].Name == "Tim Burr" && cl[1].Weight == 3 && cl[2].Weight == 0, "class list with weights");
    // questions: both formats, ANSI encoded
    string qp = Path.Combine(tmp, "q.txt");
    File.WriteAllBytes(qp, System.Text.Encoding.GetEncoding(1252).GetBytes("[QuestionAndAnswer]\r\nQ=What is the unit of energy?\r\nA=Joule_#_(J)\r\nQ=Temp unit?\r\nA=°C\r\n[Maths]\r\n2+2 | 4\r\nNo answer line\r\n"));
    var qs = Store.LoadQuestions(qp);
    Check(qs.Count == 4 && qs[0].A == "Joule (J)" && qs[0].Category == "" && qs[1].A == "°C" && qs[2].Category == "Maths" && qs[2].A == "4" && qs[3].A == "", "question formats and ANSI text");
    // settings round trip
    Store.Set("t.multi", "a\r\nb=c\\d"); 
    typeof(Store).GetMethod("LoadSettings", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null, null);
    Check(Store.Get("t.multi", "") == "a\r\nb=c\\d", "settings round trip with newline and backslash");
    // reminders
    var rem = Reminders.Parse("08:50 Take the register\nFri 14:45 Letters\nrubbish\n9:05 Short");
    Check(rem.Count == 3 && rem[0].Time == new TimeSpan(8,50,0) && rem[2].Day == DayOfWeek.Friday && rem[1].Time == new TimeSpan(9,5,0), "reminder parsing");
    // statement verbs
    var verbs = typeof(StatementForm).GetMethod("Verbs", BindingFlags.NonPublic|BindingFlags.Static);
    Check((string)verbs.Invoke(null, new object[]{"{He} {works|work} and {is|are} {name}", true}) == "{He} work and are {name}" && (string)verbs.Invoke(null, new object[]{"x {is|are} y", false}) == "x is y", "verb choice placeholders");
    var sf = new StatementForm(); sf.Show(); Pump(200);
    var tree = Field<TreeView>(sf, "tree"); tree.Nodes[1].Nodes[0].Checked = true; tree.Nodes[0].Nodes[0].Checked = true; Pump(100);
    string rep = Field<TextBox>(sf, "output").Text;
    Check(rep == "They consistently give their best effort in lessons. Sam has made a very positive start in Science this term.", "statement builder output: " + rep);
    Field<ComboBox>(sf, "pronoun").SelectedIndex = 1; Pump(50);
    Check(Field<TextBox>(sf, "output").Text.StartsWith("She consistently gives her best"), "statement builder she/her");
    sf.Close();
    // picker: weights, no repeats, groups
    var pf = new PickerForm(); pf.Show(); Pump(200);
    var seen = new HashSet<string>();
    for (int i = 0; i < 24; i++) { Priv(pf, "Pick"); Pump(1500); seen.Add(Field<string>(pf, "shown")); }
    Check(seen.Count == 24, "picker no-repeat covers all 24 students (got " + seen.Count + ")");
    Priv(pf, "MakeGroups");
    var groups = Field<List<List<string>>>(pf, "groups");
    Check(groups.Count == 6 && groups.Sum(g => g.Count) == 24 && groups.All(g => g.Count == 4), "groups of 4 from 24");
    pf.Close();
    // countdown runs and finishes
    var cf = new CountdownForm(); cf.Show(); Priv(cf, "SetTime", 1); Priv(cf, "Toggle"); Pump(1500);
    Check(Field<bool>(cf, "finished"), "countdown finishes"); cf.Close();
    // interval
    var iv = new IntervalForm(); iv.Show();
    Field<NumericUpDown>(iv, "work").Minimum = 1; Field<NumericUpDown>(iv, "work").Value = 1; Field<NumericUpDown>(iv, "rest").Value = 1; Field<NumericUpDown>(iv, "rounds").Value = 2;
    Priv(iv, "Reset"); Priv(iv, "Toggle"); Pump(3600);
    Check(Field<bool>(iv, "done") && Field<int>(iv, "round") == 2, "interval timer completes 2 rounds with rest"); iv.Close();
    // four in a row win detection
    var ff = new FourForm(); var grid = new int[7,6];
    for (int i = 0; i < 4; i++) grid[i+1, i] = 2;
    SetField(ff, "grid", grid);
    Check((bool)Priv(ff, "Wins", 3, 2) && (bool)Priv(ff, "Wins", 1, 0), "four in a row diagonal win");
    grid = new int[7,6]; grid[0,0]=1; grid[1,0]=1; grid[2,0]=1; SetField(ff, "grid", grid);
    Check(!(bool)Priv(ff, "Wins", 2, 0), "three is not a win"); ff.Dispose();
    // noughts winner
    var nf = new NoughtsForm(); SetField(nf, "cells", new[]{1,2,0, 2,1,0, 0,0,1});
    Check((int)Priv(nf, "Winner") == 1, "noughts diagonal winner");
    SetField(nf, "cells", new[]{1,2,1, 1,2,2, 2,1,1}); Check((int)Priv(nf, "Winner") == 3, "noughts draw"); nf.Dispose();
    // grid quiz builds categories from sample
    var gq = new GridQuizForm(); gq.Show(); Pump(100);
    Check(Field<List<string>>(gq, "cats").SequenceEqual(new[]{"Science","Maths","Geography","English","History"}), "grid quiz categories"); gq.Close();
    // text tidy helpers
    var tt = typeof(TextTidyForm);
    Check((string)tt.GetMethod("Swap", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null, new object[]{"Sam Lee Jones"}) == "Jones, Sam Lee" && (string)tt.GetMethod("Swap", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null, new object[]{"Jones, Sam"}) == "Sam Jones", "swap names");
    Check((string)tt.GetMethod("Sentence", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null, new object[]{"hELLO there. how ARE you?"}) == "Hello there. How are you?", "sentence case");
    // sticky notes persist
    StickyNote.Create(); Pump(100); var note = StickyNote.Open[0]; Field<TextBox>(note, "box").Text = "Remember: café, 1,2"; Pump(50);
    note.Close(); Pump(50); Check(StickyNote.Open.Count == 0, "note closed");
    // (closing without Delete keeps file) 
    string nfp = Path.Combine(Store.DataDir, "notes.txt");
    Check(File.ReadAllLines(nfp).Length == 1, "note saved to file");
    Check(StickyNote.Restore() == 1 && Field<TextBox>(StickyNote.Open[0], "box").Text == "Remember: café, 1,2", "note restored with text");
    // exam countdown parse
    var items = typeof(ExamCountdownForm).GetMethod("Parse", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null, new object[]{ DateTime.Today.AddDays(10).ToString("dd/MM/yyyy") + " Maths\n01/01/2001 Old\nnot a date" }) as System.Collections.IList;
    Check(items.Count == 1, "exam dates: UK format, past dates dropped");
    Console.WriteLine(fails == 0 ? "ALL PASSED" : fails + " FAILED");
    return fails;
  } } }
