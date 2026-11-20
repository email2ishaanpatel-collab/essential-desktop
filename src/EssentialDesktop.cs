using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Shapes = System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Threading;
using WF = System.Windows.Forms;
using DR = System.Drawing;

namespace SuperDeck
{
    /* ------------------------------------------------------------------ tokens
     *
     * panel.js owns these on Linux and every one of them is copied here rather
     * than re-derived, including the reasons: HEAD_H is 44 because a caption
     * centred in a header that starts PAD_Y down lands on the pill's own text
     * line only when PAD_Y + HEAD_H/2 = H/2. Change one, move the other two. */
    public static class P
    {
        public const double W = 720;          // the pill at full width
        public const double H = 68;
        public const double R_FROM = 0.5;     // circle at the start of the intro
        public const double R_TO = 0.44;      // relaxing to the alt-tab corner
        public const double Y_F = 0.32;       // pill middle, as a fraction of height
        public const double GAP = 12;         // pill -> results island

        public const double DECK_W = W;
        public const int DECK_COLS = 3;

        public const double PANEL_W = 368;    // notes left, to-do right
        public const double PANEL_GAP = 14;
        public const double PANEL_SLIDE = 46;

        public const double ROW_H = 54;
        public const double PAD = 10;         // island edge -> first row
        public const double SIDE = 10;        // island edge -> row highlight
        public const double RES_R = 26;
        public const int MAX_ROWS = 8;
        public const int MAX_FILES = 4;
        public const int FILE_PENALTY = 5;
        public const double SEL_R = 16;
        public const double CHEV_SIZE = 28;
        public const double CHEV_GLYPH = 16;
        public const double MARK = 22;

        public const int DEPTH = 2;
        public const int FILE_CAP = 3000;
        public const int LOOSE_FROM = 3;

        public const int IN_MS = 200;
        public const int OUT_MS = 150;
        public const int MOVE_MS = 170;
        public const int GROW_MS = 190;
        public const int DECK_DELAY_MS = 80;  // let the pill land first

        public const double ISLAND_R = 26;
        public const double PAD_X = 14;
        public const double PAD_Y = 12;
        public const double HEAD_H = 44;
        public const double HEAD_GAP = 10;
        /* Not HEAD_GAP: the deck's island begins one pill plus one pill-gap below the
         * top of the arrangement (68 + 12 = 80), and the first thing on a side panel
         * should begin on that same line - PAD_Y + HEAD_H + BODY_GAP = 80. So the
         * open note, the add-a-task bar and the deck all start on one edge right
         * across the spread. Linux: panel.js. */
        public const double BODY_GAP = 24;
        /* How far a placeholder stands clear of the caret: a hint whose first glyph
         * sits at x = 0 has the caret drawn on top of it. */
        public const double HINT_NUDGE = 4;
        public const double ICON_HEAD = 18;
        /* The one red, and the single definition of it: the bins, and the Yes half of
         * the delete-a-note question. Linux: panel.js BIN_RED. */
        public static readonly Color BIN_RED = Color.FromArgb(242, 228, 58, 54);
        public const double BTN_H = 32;
        public const double BTN_R = 12;
        public const double CARD_GAP = 10;
        public const double CARD_R = 18;
        public const double CARD_BORDER = 2;
        public const double TEXT_H_MIN = 76;
        public const double TEXT_H_MAX = 260;
        public const double BIN = 26;
        public const double BIN_INSET = 5;
        public const double WALL_H = 360;
        public const int MAX_ITEMS = 24;      // clips kept
        public const int MAX_NOTES = 60;

        public static double DECK_H { get { return PAD_Y + HEAD_H + HEAD_GAP + WALL_H + PAD_Y; } }
        public static double PANEL_H { get { return H + GAP + DECK_H; } }

        /* Neue Haas Grotesk Display Pro at 500, with the two fallbacks a Windows
         * machine will actually have. The weight is not `normal`: this family
         * registers its roman as 500 and asking for normal picks the light. */
        public const string FONT_FAMILY = "Neue Haas Grotesk Display Pro, Helvetica Neue, Segoe UI, Helvetica";
        public static readonly FontWeight FONT_WEIGHT = FontWeight.FromOpenTypeWeight(500);
        public const double FONT_HEAD = 13;
        public const double FONT_BTN = 15;
        public const double FONT_BODY = 18;   // the search field's own size

        /* ----------------------------------------------------------- palette
         *
         * Two sets, one design: light is the same arrangement with ink and ground
         * swapped, not a second look. Read through C, never captured - a module
         * that snapshots a colour at load never hears about a theme change. */
        public class Pal
        {
            public string NAME;
            public Color BODY, CARD_BG, CARD_BG_HOVER, RING, COPIED, BTN_BG, BTN_BG_HOVER;
            public Color INK_DIM, INK_BODY, INK_LOUD, SOLID, SOLID_HOVER, SOLID_INK, SEL;
            public Color MENU_BG, MENU_BORDER, CARET;
        }

        static Color Rgba(byte r, byte g, byte b, double a)
        {
            return Color.FromArgb((byte)Math.Round(a * 255), r, g, b);
        }

        public static readonly Pal DARK = new Pal
        {
            NAME = "dark",
            BODY = Rgba(0, 0, 0, 1),
            CARD_BG = Rgba(255, 255, 255, 0.07),
            CARD_BG_HOVER = Rgba(255, 255, 255, 0.12),
            RING = Rgba(255, 255, 255, 0.55),
            COPIED = Rgba(64, 214, 122, 0.95),
            BTN_BG = Rgba(255, 255, 255, 0.10),
            BTN_BG_HOVER = Rgba(255, 255, 255, 0.19),
            INK_DIM = Rgba(255, 255, 255, 0.34),
            INK_BODY = Rgba(255, 255, 255, 0.86),
            INK_LOUD = Rgba(255, 255, 255, 0.88),
            SOLID = Rgba(255, 255, 255, 1),
            SOLID_HOVER = Rgba(255, 255, 255, 0.84),
            SOLID_INK = Rgba(0, 0, 0, 0.88),
            SEL = Rgba(255, 255, 255, 0.14),
            MENU_BG = Rgba(24, 24, 26, 0.99),
            MENU_BORDER = Rgba(255, 255, 255, 0.10),
            CARET = Rgba(255, 255, 255, 1),
        };

        public static readonly Pal LIGHT = new Pal
        {
            NAME = "light",
            /* White, as asked for - not an off-white. The cards and buttons on it
             * are washes of black, so the ground has to be the brightest thing
             * there is or the two collapse towards each other. */
            BODY = Rgba(255, 255, 255, 1),
            CARD_BG = Rgba(0, 0, 0, 0.055),
            CARD_BG_HOVER = Rgba(0, 0, 0, 0.10),
            RING = Rgba(0, 0, 0, 0.55),
            /* Darkened: the dark theme's green is chosen to glow on black and is
             * barely legible on white. */
            COPIED = Rgba(26, 150, 68, 0.95),
            BTN_BG = Rgba(0, 0, 0, 0.07),
            BTN_BG_HOVER = Rgba(0, 0, 0, 0.14),
            INK_DIM = Rgba(0, 0, 0, 0.40),
            INK_BODY = Rgba(0, 0, 0, 0.84),
            INK_LOUD = Rgba(0, 0, 0, 0.88),
            SOLID = Rgba(0, 0, 0, 1),
            SOLID_HOVER = Rgba(0, 0, 0, 0.78),
            SOLID_INK = Rgba(255, 255, 255, 0.94),
            SEL = Rgba(0, 0, 0, 0.085),
            MENU_BG = Rgba(250, 250, 252, 0.99),
            MENU_BORDER = Rgba(0, 0, 0, 0.12),
            CARET = Rgba(0, 0, 0, 1),
        };

        static Pal cur = DARK;
        public static Pal C { get { return cur; } }
        public static event Action Restyle;

        public static void WhenLoaded(FrameworkElement owner, Action paint)
        {
            owner.Loaded += delegate { Restyle += paint; paint(); };
            owner.Unloaded += delegate { Restyle -= paint; };
        }

        public static void SetPalette(string name)
        {
            cur = (name == "light") ? LIGHT : DARK;
            if (Restyle != null) Restyle();
        }

        static readonly Dictionary<Color, SolidColorBrush> brushes = new Dictionary<Color, SolidColorBrush>();
        public static SolidColorBrush Br(Color c)
        {
            SolidColorBrush b;
            if (brushes.TryGetValue(c, out b)) return b;
            b = new SolidColorBrush(c);
            b.Freeze();
            brushes[c] = b;
            return b;
        }

        public static readonly FontFamily Face = new FontFamily(FONT_FAMILY);

        public static string ConfigDir
        {
            get
            {
                string d = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SuperDeck");
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                return d;
            }
        }
    }

    /* ------------------------------------------------------------------- json
     *
     * Enough of it for three flat stores and not one line more. A dependency-free
     * single file cannot pull in a serialiser, and the shapes here are known. */
    public static class J
    {
        public static string Esc(string s)
        {
            if (s == null) return "\"\"";
            StringBuilder b = new StringBuilder("\"");
            foreach (char ch in s)
            {
                if (ch == '"' || ch == '\\') b.Append('\\').Append(ch);
                else if (ch == '\n') b.Append("\\n");
                else if (ch == '\r') b.Append("\\r");
                else if (ch == '\t') b.Append("\\t");
                else if (ch < ' ') b.Append("\\u").Append(((int)ch).ToString("x4"));
                else b.Append(ch);
            }
            return b.Append('"').ToString();
        }

        public static List<Dictionary<string, string>> Read(string path)
        {
            List<Dictionary<string, string>> outp = new List<Dictionary<string, string>>();
            if (!File.Exists(path)) return outp;
            string s;
            try { s = File.ReadAllText(path, Encoding.UTF8); }
            catch { return outp; }

            int i = 0;
            while (i < s.Length)
            {
                if (s[i] != '{') { i++; continue; }
                Dictionary<string, string> obj = new Dictionary<string, string>();
                i++;
                while (i < s.Length && s[i] != '}')
                {
                    string key = null, val = null;
                    while (i < s.Length && s[i] != '"' && s[i] != '}') i++;
                    if (i >= s.Length || s[i] == '}') break;
                    key = Str(s, ref i);
                    while (i < s.Length && s[i] != ':') i++;
                    i++;
                    while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t')) i++;
                    if (i < s.Length && s[i] == '"') val = Str(s, ref i);
                    else
                    {
                        int st = i;
                        while (i < s.Length && s[i] != ',' && s[i] != '}') i++;
                        val = s.Substring(st, i - st).Trim();
                    }
                    if (key != null) obj[key] = val;
                    while (i < s.Length && (s[i] == ',' || s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t')) i++;
                }
                i++;
                outp.Add(obj);
            }
            return outp;
        }

        static string Str(string s, ref int i)
        {
            StringBuilder b = new StringBuilder();
            i++;   // opening quote
            while (i < s.Length && s[i] != '"')
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    i++;
                    char c = s[i];
                    if (c == 'n') b.Append('\n');
                    else if (c == 'r') b.Append('\r');
                    else if (c == 't') b.Append('\t');
                    else if (c == 'u' && i + 4 < s.Length)
                    {
                        b.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16));
                        i += 4;
                    }
                    else b.Append(c);
                }
                else b.Append(s[i]);
                i++;
            }
            i++;   // closing quote
            return b.ToString();
        }

        /* Written beside the target and moved into place: a store half-written by
         * a crash is worse than one that is a version behind. */
        public static void Write(string path, List<Dictionary<string, string>> rows)
        {
            StringBuilder b = new StringBuilder("[\n");
            for (int i = 0; i < rows.Count; i++)
            {
                b.Append("  {");
                bool first = true;
                foreach (KeyValuePair<string, string> kv in rows[i])
                {
                    if (!first) b.Append(", ");
                    first = false;
                    b.Append(Esc(kv.Key)).Append(": ").Append(Esc(kv.Value));
                }
                b.Append("}");
                if (i < rows.Count - 1) b.Append(",");
                b.Append("\n");
            }
            b.Append("]\n");
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, b.ToString(), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static string Get(Dictionary<string, string> d, string k, string dflt)
        {
            string v;
            if (d != null && d.TryGetValue(k, out v) && v != null) return v;
            return dflt;
        }
    }

    /* ----------------------------------------------------------------- native */
    public static class N
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLL { public uint vkCode; public uint scanCode; public uint flags; public uint time; public IntPtr extra; }
        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr extra; }
        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr extra; }
        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }
        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT { public uint type; public INPUTUNION u; }

        public delegate IntPtr HookProc(int nCode, IntPtr w, IntPtr l);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr w, IntPtr l);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vk);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
        [DllImport("user32.dll")] public static extern bool LockWorkStation();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
        [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint from, uint to, bool attach);
        [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    }

    public static class Kb
    {
        public const int LWIN = 0x5B, RWIN = 0x5C, SHIFT = 0x10, CTRL = 0x11, ALT = 0x12;
        public const int M_CTRL = 1, M_SHIFT = 2, M_ALT = 4;
        public const uint KEYUP = 2, EXT = 1;

        public static bool IsDown(int vk) { return (N.GetAsyncKeyState(vk) & 0x8000) != 0; }

        static bool Extended(int vk)
        {
            if (vk == 0x5B || vk == 0x5C || vk == 0x5D) return true;
            if (vk >= 0x21 && vk <= 0x28) return true;
            if (vk == 0x2C || vk == 0x2D || vk == 0x2E) return true;
            return false;
        }

        static void Push(List<N.INPUT> list, int vk, bool up)
        {
            N.INPUT i = new N.INPUT();
            i.type = 1;
            i.u.ki.wVk = (ushort)vk;
            i.u.ki.dwFlags = up ? KEYUP : 0u;
            if (Extended(vk)) i.u.ki.dwFlags |= EXT;
            i.u.ki.extra = IntPtr.Zero;
            list.Add(i);
        }

        /* Replayed as synthetic input, which carries the INJECTED flag the hook
         * ignores - so Windows itself runs the chord, and every native shortcut
         * behaves the way it does on the machine it is running on, 10 or 11.
         *
         * The modifiers are only injected for a chord fired from the launcher: one
         * that came from the keyboard still has them physically held, and pressing
         * a key the user is already holding leaves it released afterwards. */
        public static void SendWin(int vk, int mods, bool injectMods)
        {
            List<N.INPUT> l = new List<N.INPUT>();
            Push(l, LWIN, false);
            if (injectMods)
            {
                if ((mods & M_CTRL) != 0) Push(l, CTRL, false);
                if ((mods & M_SHIFT) != 0) Push(l, SHIFT, false);
                if ((mods & M_ALT) != 0) Push(l, ALT, false);
            }
            if (vk != 0) { Push(l, vk, false); Push(l, vk, true); }
            if (injectMods)
            {
                if ((mods & M_ALT) != 0) Push(l, ALT, true);
                if ((mods & M_SHIFT) != 0) Push(l, SHIFT, true);
                if ((mods & M_CTRL) != 0) Push(l, CTRL, true);
            }
            Push(l, LWIN, true);
            N.SendInput((uint)l.Count, l.ToArray(), Marshal.SizeOf(typeof(N.INPUT)));
        }
    }

    /* ---------------------------------------------------------------- entries */
    public class Entry
    {
        public string Name;          // lowercased, for matching
        public string Display;       // as shown
        public string[] Words;
        public string Exec;          // lowercased launch target
        public string[] Extra;       // keywords: the folder it sits in
        public bool IsFile;
        public bool IsDir;
        public string Path;
        public string Where;         // which of the five places it came from
    }

    public static class Idx
    {
        static readonly char[] SPLIT = new char[] { ' ', '-', '_', '/', ':', '.', ',', '(', ')', ';', '\\' };

        public static string[] Words(string s)
        {
            return s.ToLowerInvariant().Split(SPLIT, StringSplitOptions.RemoveEmptyEntries);
        }

        /* ------------------------------------------------------------ matching
         *
         * Ranked rather than filtered, so "fir" puts Firefox above things that
         * merely mention it. Everything above the length gate matches on a word
         * start: substring-anywhere is what makes a short query useless - "te" is
         * inside half the Start menu and those drown the two things whose names
         * actually begin with it. Shorter names win ties, because with "term" both
         * Terminal and Terminal Preferences are word-start matches and the bare
         * app is the one being asked for. */
        public static int Score(Entry e, string q)
        {
            int shorter = 20 - Math.Min(20, e.Name.Length);

            if (e.Name.StartsWith(q, StringComparison.Ordinal)) return 200 + shorter;
            for (int i = 0; i < e.Words.Length; i++)
                if (e.Words[i].StartsWith(q, StringComparison.Ordinal)) return 160 + shorter;
            if (e.Exec.StartsWith(q, StringComparison.Ordinal)) return 120 + shorter;

            if (q.Length < P.LOOSE_FROM) return -1;

            for (int i = 0; i < e.Extra.Length; i++)
                if (e.Extra[i].StartsWith(q, StringComparison.Ordinal)) return 80 + shorter;
            if (e.Name.IndexOf(q, StringComparison.Ordinal) != -1) return 40 + shorter;
            if (e.Exec.IndexOf(q, StringComparison.Ordinal) != -1) return 20 + shorter;
            return -1;
        }

        /* Filtering one folder is not the same problem as searching every
         * application: there is no field to guess at and nothing to out-rank, so a
         * plain substring is right from the first character - and the folder's own
         * order is kept for everything that matches equally well. */
        public static List<Entry> Filter(List<Entry> entries, string q)
        {
            if (string.IsNullOrEmpty(q)) return entries;
            List<Entry> hits = new List<Entry>();
            List<int> rank = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                int at = e.Name.IndexOf(q, StringComparison.Ordinal);
                if (at == -1) continue;
                int r = 0;
                if (at == 0) r = 2;
                else
                {
                    for (int j = 0; j < e.Words.Length; j++)
                        if (e.Words[j].StartsWith(q, StringComparison.Ordinal)) { r = 1; break; }
                }
                hits.Add(e);
                rank.Add(r);
            }
            Sort(hits, rank);
            return hits;
        }

        /* Insertion sort, descending, and stable - which is the whole point: equal
         * scores have to come out in the order they went in. */
        public static void Sort(List<Entry> list, List<int> score)
        {
            for (int i = 1; i < list.Count; i++)
            {
                Entry e = list[i]; int s = score[i]; int j = i - 1;
                while (j >= 0 && score[j] < s) { list[j + 1] = list[j]; score[j + 1] = score[j]; j--; }
                list[j + 1] = e; score[j + 1] = s;
            }
        }

        /* --------------------------------------------------------------- apps */
        static List<Entry> apps;

        public static List<Entry> Apps()
        {
            if (apps != null) return apps;
            apps = new List<Entry>();
            Dictionary<string, bool> seen = new Dictionary<string, bool>();
            string[] roots = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            };
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                WalkApps(root, seen);
            }
            return apps;
        }

        static void WalkApps(string dir, Dictionary<string, bool> seen)
        {
            string[] files;
            try { files = Directory.GetFiles(dir); }
            catch { return; }
            foreach (string f in files)
            {
                string ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
                if (ext != ".lnk" && ext != ".url" && ext != ".appref-ms") continue;
                string disp = System.IO.Path.GetFileNameWithoutExtension(f);
                string key = disp.ToLowerInvariant();
                if (seen.ContainsKey(key)) continue;
                seen[key] = true;

                Entry e = new Entry();
                e.Display = disp;
                e.Name = key;
                e.Words = Words(disp);
                e.Exec = System.IO.Path.GetFileName(f).ToLowerInvariant();
                /* The folder a shortcut sits in is the vendor name nine times out
                 * of ten - "Adobe", "Microsoft Office" - which is the sort of thing
                 * .desktop Keywords carried on the other side. */
                e.Extra = Words(System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(f)));
                e.Path = f;
                apps.Add(e);
            }
            string[] subs;
            try { subs = Directory.GetDirectories(dir); }
            catch { return; }
            foreach (string s in subs) WalkApps(s, seen);
        }

        /* -------------------------------------------------------------- files
         *
         * The five folders the file manager puts in its sidebar, two deep, with a
         * ceiling so a pathological Downloads cannot become an unbounded list. */
        static List<Entry> files;
        static List<Entry> publishedFiles = new List<Entry>();
        static bool filesBusy;
        public static event Action Updated;
        static DateTime filesAt = DateTime.MinValue;

        public static string[] Places()
        {
            return new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };
        }

        public static List<Entry> Files()
        {
            if (!filesBusy && (DateTime.UtcNow - filesAt).TotalSeconds >= 15)
            {
                filesBusy = true;
                var ui = Dispatcher.CurrentDispatcher;
                System.Threading.ThreadPool.QueueUserWorkItem(delegate
                {
                    List<Entry> result;
                    try { result = BuildFiles(); } catch { result = new List<Entry>(); }
                    ui.BeginInvoke(new Action(delegate { publishedFiles = result; filesAt = DateTime.UtcNow; filesBusy = false; if (Updated != null) Updated(); }));
                });
            }
            return publishedFiles;
        }
        static List<Entry> BuildFiles()
        {
            if (files != null && (DateTime.UtcNow - filesAt).TotalSeconds < 15) return files;
            files = new List<Entry>();
            foreach (string place in Places())
            {
                if (string.IsNullOrEmpty(place) || !Directory.Exists(place)) continue;
                string where = System.IO.Path.GetFileName(place.TrimEnd('\\'));
                Walk(place, where, 0);
                if (files.Count >= P.FILE_CAP) break;
            }
            filesAt = DateTime.UtcNow;
            return files;
        }

        static void Walk(string dir, string where, int depth)
        {
            if (files.Count >= P.FILE_CAP) return;
            string[] names;
            try { names = Directory.GetFileSystemEntries(dir); }
            catch { return; }
            foreach (string p in names)
            {
                if (files.Count >= P.FILE_CAP) return;
                string leaf = System.IO.Path.GetFileName(p);
                if (leaf.StartsWith(".", StringComparison.Ordinal)) continue;
                bool isDir;
                try
                {
                    FileAttributes at = File.GetAttributes(p);
                    if ((at & FileAttributes.Hidden) != 0 || (at & FileAttributes.System) != 0) continue;
                    isDir = (at & FileAttributes.Directory) != 0;
                }
                catch { continue; }

                Entry e = One(p, isDir);
                e.Where = where;
                files.Add(e);

                if (isDir && depth + 1 < P.DEPTH) Walk(p, where, depth + 1);
            }
        }

        /* Browsing is not searching: one folder, read on demand, and however many
         * rows it has - MAX_ROWS is only how many are on screen at once. */
        public static List<Entry> Browse(string dir)
        {
            List<Entry> outp = new List<Entry>();
            string[] names;
            try { names = Directory.GetFileSystemEntries(dir); }
            catch { return outp; }
            List<string> dirs = new List<string>();
            List<string> rest = new List<string>();
            foreach (string p in names)
            {
                string leaf = System.IO.Path.GetFileName(p);
                if (leaf.StartsWith(".", StringComparison.Ordinal)) continue;
                try
                {
                    FileAttributes at = File.GetAttributes(p);
                    if ((at & FileAttributes.Hidden) != 0 || (at & FileAttributes.System) != 0) continue;
                    if ((at & FileAttributes.Directory) != 0) dirs.Add(p); else rest.Add(p);
                }
                catch { }
            }
            dirs.Sort(StringComparer.OrdinalIgnoreCase);
            rest.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string p in dirs) outp.Add(One(p, true));
            foreach (string p in rest) outp.Add(One(p, false));
            if (outp.Count > 600) outp.RemoveRange(600, outp.Count - 600);
            return outp;
        }

        static Entry One(string p, bool isDir)
        {
            string leaf = System.IO.Path.GetFileName(p);
            Entry e = new Entry();
            e.Display = leaf;
            e.Name = leaf.ToLowerInvariant();
            e.Words = Words(leaf);
            e.Exec = e.Name;
            e.Extra = new string[0];
            e.IsFile = true;
            e.IsDir = isDir;
            e.Path = p;
            return e;
        }

        /* ------------------------------------------------------------- icons
         *
         * Cached by extension rather than by path: two hundred .pdf rows are one
         * icon, and asking the shell for each of them is the only thing in the
         * index that would be slow enough to see. */
        static Dictionary<string, ImageSource> iconCache = new Dictionary<string, ImageSource>();

        public static ImageSource Icon(Entry e)
        {
            string key = e.IsDir ? "<dir>"
                       : (e.IsFile ? System.IO.Path.GetExtension(e.Path).ToLowerInvariant() : e.Path);
            if (key.Length == 0) key = "<none>";
            ImageSource got;
            if (iconCache.TryGetValue(key, out got)) return got;

            ImageSource src = null;
            try
            {
                DR.Icon ico = e.IsDir ? Shell.FolderIcon() : DR.Icon.ExtractAssociatedIcon(e.Path);
                if (ico != null)
                {
                    using (DR.Icon owned = e.IsDir ? null : ico)
                    {
                        src = Imaging.CreateBitmapSourceFromHIcon(
                            ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                    }
                }
            }
            catch { }
            iconCache[key] = src;
            return src;
        }
    }

    /* ------------------------------------------------------------------ shell */
    public static class Shell
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SHGetFileInfo(string path, uint attrs, ref SHFILEINFO psfi, uint cb, uint flags);

        static DR.Icon folder;

        public static DR.Icon FolderIcon()
        {
            if (folder != null) return folder;
            SHFILEINFO info = new SHFILEINFO();
            /* USEFILEATTRIBUTES means it never touches the disk: the path is a
             * description of a folder, not one that has to exist. */
            IntPtr r = SHGetFileInfo("dir", 0x10, ref info, (uint)Marshal.SizeOf(info), 0x100 | 0x10);
            if (r != IntPtr.Zero && info.hIcon != IntPtr.Zero)
                folder = DR.Icon.FromHandle(info.hIcon);
            return folder;
        }

        public static void Open(string path)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(path);
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch (Exception ex) { WF.MessageBox.Show(ex.Message, "SuperDeck"); }
        }
    }

    /* --------------------------------------------------------------- the deck
     *
     * The deck came first and set the look for everything else around the pill:
     * a pure-black island, a letter-spaced caption with pill buttons pushed to
     * the right of it, and cards on a faint white wash. */
    public class Clip
    {
        public string Id;
        public string Kind;     // "text" | "image"
        public string Text;
        public string File;     // image on disk
        public long When;
        public double CachedH = -1;
        public double CachedFor = -1;
    }

    public static class Deck
    {
        public static List<Clip> Items = new List<Clip>();
        static string Store { get { return System.IO.Path.Combine(P.ConfigDir, "copydeck.json"); } }
        static string ClipsDir
        {
            get
            {
                string d = System.IO.Path.Combine(P.ConfigDir, "clips");
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                return d;
            }
        }

        public static event Action Changed;

        public static void Load()
        {
            Items.Clear();
            foreach (Dictionary<string, string> row in J.Read(Store))
            {
                Clip c = new Clip();
                c.Id = J.Get(row, "id", Guid.NewGuid().ToString("N"));
                c.Kind = J.Get(row, "kind", "text");
                c.Text = J.Get(row, "text", "");
                c.File = J.Get(row, "file", "");
                long w;
                long.TryParse(J.Get(row, "when", "0"), out w);
                c.When = w;
                /* A clip whose image is gone is not a clip. */
                if (c.Kind == "image" && (c.File.Length == 0 || !File.Exists(c.File))) continue;
                Items.Add(c);
            }
        }

        public static void Save()
        {
            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
            foreach (Clip c in Items)
            {
                Dictionary<string, string> d = new Dictionary<string, string>();
                d["id"] = c.Id;
                d["kind"] = c.Kind;
                d["text"] = c.Text;
                d["file"] = c.File;
                d["when"] = c.When.ToString(CultureInfo.InvariantCulture);
                rows.Add(d);
            }
            try { J.Write(Store, rows); } catch { }
        }

        public static int Count { get { return Items.Count; } }

        public static void AddText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (text.Length > 100000) text = text.Substring(0, 100000);
            /* Copying the same thing twice is one clip that moved to the front,
             * not two clips. */
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].Kind == "text" && Items[i].Text == text)
                {
                    Clip had = Items[i];
                    Items.RemoveAt(i);
                    Items.Insert(0, had);
                    Trim();
                    return;
                }
            Clip c = new Clip();
            c.Id = Guid.NewGuid().ToString("N");
            c.Kind = "text";
            c.Text = text;
            c.When = DateTime.UtcNow.Ticks;
            Items.Insert(0, c);
            Trim();
        }

        public static void AddImage(BitmapSource bmp)
        {
            if (bmp == null) return;
            try
            {
                string file = System.IO.Path.Combine(ClipsDir, Guid.NewGuid().ToString("N") + ".png");
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bmp));
                using (FileStream fs = new FileStream(file, FileMode.Create))
                    enc.Save(fs);
                Clip c = new Clip();
                c.Id = Guid.NewGuid().ToString("N");
                c.Kind = "image";
                c.File = file;
                /* The one thing a square crop cannot say about itself. */
                c.Text = bmp.PixelWidth + "\u00d7" + bmp.PixelHeight;
                c.When = DateTime.UtcNow.Ticks;
                Items.Insert(0, c);
                Trim();
            }
            catch { }
        }

        static void Trim()
        {
            while (Items.Count > P.MAX_ITEMS)
            {
                Clip gone = Items[Items.Count - 1];
                Items.RemoveAt(Items.Count - 1);
                Drop(gone);
            }
            Save();
            if (Changed != null) Changed();
        }

        public static void Remove(Clip c)
        {
            Items.Remove(c);
            Drop(c);
            Save();
            if (Changed != null) Changed();
        }

        public static void Clear()
        {
            foreach (Clip c in Items) Drop(c);
            Items.Clear();
            Save();
            if (Changed != null) Changed();
        }

        static void Drop(Clip c)
        {
            if (c.Kind == "image" && c.File.Length > 0)
                try { if (File.Exists(c.File)) File.Delete(c.File); } catch { }
        }
    }

    /* --------------------------------------------------------------- notes */
    public class Note
    {
        public string Id;
        public string Text;
        public long When;
    }

    public static class Notes
    {
        public static List<Note> Items = new List<Note>();
        static string Store { get { return System.IO.Path.Combine(P.ConfigDir, "notes.json"); } }

        public static void Load()
        {
            Items.Clear();
            foreach (Dictionary<string, string> row in J.Read(Store))
            {
                Note n = new Note();
                n.Id = J.Get(row, "id", Guid.NewGuid().ToString("N"));
                n.Text = J.Get(row, "text", "");
                long w; long.TryParse(J.Get(row, "when", "0"), out w);
                n.When = w;
                Items.Add(n);
            }
        }


        public static void Save()
        {
            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
            foreach (Note n in Items)
            {
                Dictionary<string, string> d = new Dictionary<string, string>();
                d["id"] = n.Id; d["text"] = n.Text;
                d["when"] = n.When.ToString(CultureInfo.InvariantCulture);
                rows.Add(d);
            }
            try { J.Write(Store, rows); } catch { }
        }

        public static Note Add(string text)
        {
            Note n = new Note();
            n.Id = Guid.NewGuid().ToString("N");
            n.Text = text;
            n.When = DateTime.UtcNow.Ticks;
            Items.Insert(0, n);
            while (Items.Count > P.MAX_NOTES) Items.RemoveAt(Items.Count - 1);
            Revision++;
            Save();
            return n;
        }

        /* Bumped on anything that changes the list, so a panel can tell whether its
         * wall is still the right picture of it. */
        public static int Revision;

        public static void Move(int from, int to)
        {
            if (from == to || from < 0 || from >= Items.Count) return;
            Note n = Items[from];
            Items.RemoveAt(from);
            Items.Insert(Math.Max(0, Math.Min(Items.Count, to)), n);
            Revision++;
            Save();
        }

        public static void Flush() { Save(); }

        public static void SetText(Note n, string text)
        {
            if (n.Text == text) return;
            n.Text = text;
            n.When = DateTime.UtcNow.Ticks;
            Save();
        }

        public static void Remove(Note n)
        {
            Items.Remove(n);
            Revision++;
            Save();
        }

    }

    /* --------------------------------------------------------------- to-do */
    public class Job
    {
        public string Id;
        public string Text;
        public bool Done;
        public long When;
        /* The list's own index - where a task sits is a stored fact rather than
         * something re-derived from a clock. */
        public int Ord;
    }

    public static class Todo
    {
        public static List<Job> Items = new List<Job>();
        static string Store { get { return System.IO.Path.Combine(P.ConfigDir, "todo.json"); } }

        public static void Load()
        {
            Items.Clear();
            bool ordered = true;
            foreach (Dictionary<string, string> row in J.Read(Store))
            {
                Job t = new Job();
                t.Id = J.Get(row, "id", Guid.NewGuid().ToString("N"));
                t.Text = J.Get(row, "text", "");
                t.Done = J.Get(row, "done", "0") == "1";
                long w; long.TryParse(J.Get(row, "when", "0"), out w);
                t.When = w;
                int ord;
                /* A file written before ord existed: sorted by timestamp on load and
                 * numbered from there, so nothing appears to have moved on the first
                 * open after the upgrade. */
                if (!int.TryParse(J.Get(row, "ord", ""), out ord)) { ord = int.MaxValue; ordered = false; }
                t.Ord = ord;
                Items.Add(t);
            }
            if (!ordered)
            {
                Items.Sort(delegate(Job a, Job b)
                {
                    if (a.Done != b.Done) return a.Done ? 1 : -1;
                    return b.When.CompareTo(a.When);
                });
            }
            Sort();
        }


        public static void Save()
        {
            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
            foreach (Job t in Items)
            {
                Dictionary<string, string> d = new Dictionary<string, string>();
                d["id"] = t.Id; d["text"] = t.Text; d["done"] = t.Done ? "1" : "0";
                d["when"] = t.When.ToString(CultureInfo.InvariantCulture);
                d["ord"] = t.Ord.ToString(CultureInfo.InvariantCulture);
                rows.Add(d);
            }
            try { J.Write(Store, rows); } catch { }
        }

        public static int Revision;

        public static void Move(int from, int to)
        {
            if (from == to || from < 0 || from >= Items.Count) return;
            Job t = Items[from];
            Items.RemoveAt(from);
            Items.Insert(Math.Max(0, Math.Min(Items.Count, to)), t);
            /* Renumbered before it is sorted: sorting first reads the old ord and
             * puts the task straight back where it was dragged from. */
            Renumber();
            Sort();
            Revision++;
            Save();
        }

        public static void ClearDone()
        {
            List<Job> keep = new List<Job>();
            foreach (Job t in Items) if (!t.Done) keep.Add(t);
            Items = keep;
            Renumber();
            Revision++;
            Save();
        }

        public static void Flush() { Save(); }

        /* Open work first, everything finished below it, and the order you put them
         * in within each half. It used to be newest-first by timestamp, which is the
         * right default and the wrong thing to keep once the list can be dragged: a
         * hand-made order that a clock silently overrules is not an order. */
        public static void Sort()
        {
            Items.Sort(delegate(Job a, Job b)
            {
                if (a.Done != b.Done) return a.Done ? 1 : -1;
                return a.Ord.CompareTo(b.Ord);
            });
            Renumber();
        }

        static void Renumber()
        {
            for (int i = 0; i < Items.Count; i++) Items[i].Ord = i;
        }

        public static void Toggle(Job t)
        {
            t.Done = !t.Done;
            t.When = DateTime.UtcNow.Ticks;
            /* To the head of whichever half it has just joined - the same rule in
             * both directions, which is what makes unticking feel like undoing. */
            t.Ord = -1;
            Sort();
            Revision++;
            Save();
        }

        public static int Open()
        {
            int n = 0;
            foreach (Job t in Items) if (!t.Done) n++;
            return n;
        }

        public static Job Add(string text)
        {
            Job t = new Job();
            t.Id = Guid.NewGuid().ToString("N");
            t.Text = text;
            t.When = DateTime.UtcNow.Ticks;
            /* Below every existing order, so it sorts to the head of the open half. */
            t.Ord = -1;
            Items.Insert(0, t);
            Sort();
            Revision++;
            Save();
            return t;
        }

        public static void Remove(Job t)
        {
            Items.Remove(t);
            Renumber();
            Revision++;
            Save();
        }
    }

    /* --------------------------------------------------------------- settings
     *
     * What is switched on, and which palette. The Linux side keeps the same three
     * keys in ~/.config/dynamic-island/settings.json - see
     * super/settings-and-palette.md - and this is the same idea in the shape J can
     * read: one flat row, features as "feat.<key>". Read once, written back whole,
     * because the first version of the Linux one wrote { theme } on every toggle
     * and silently dropped everything else the moment there was more than one
     * setting.
     *
     * A missing key reads as ON, so a file written before any of this existed
     * describes exactly the arrangement it was written by - and so does a fresh
     * install with no file at all. */
    public static class Cfg
    {
        static string Store { get { return System.IO.Path.Combine(P.ConfigDir, "settings.json"); } }

        static Dictionary<string, string> all;
        public static event Action FeatureChanged;

        static Dictionary<string, string> All()
        {
            if (all == null)
            {
                all = new Dictionary<string, string>();
                List<Dictionary<string, string>> rows = J.Read(Store);
                if (rows.Count > 0) all = rows[0];
            }
            return all;
        }

        static void Write()
        {
            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
            rows.Add(All());
            try { J.Write(Store, rows); } catch { }
        }

        public static string Theme()
        {
            return J.Get(All(), "theme", "dark") == "light" ? "light" : "dark";
        }

        public static void SetTheme(string next)
        {
            next = next == "light" ? "light" : "dark";
            if (Theme() == next) return;
            All()["theme"] = next;
            Write();
            P.SetPalette(next);
        }

        /* Where a circled crop goes. Set from the toggle on the lens overlay rather
         * than from the settings card, and kept here because it outlives the
         * overlay - which is built and destroyed per gesture. */
        public static string ModelPath()
        {
            string value = J.Get(All(), "voice.model", "ggml-tiny.en.bin");
            return Path.IsPathRooted(value) ? value : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice", value);
        }

        public static void SetModel(string path)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice");
            All()["voice.model"] = String.Equals(Path.GetDirectoryName(path), folder, StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileName(path) : path;
            Write();
        }

        public static string Trigger() { return J.Get(All(), "trigger", "Windows / Super"); }
        public static void SetTrigger(string value) { All()["trigger"]=value; Write(); }
        public static bool DeckCollapsed() { return J.Get(All(), "deck.collapsed", "0")=="1"; }
        public static void SetDeckCollapsed(bool value) { All()["deck.collapsed"]=value?"1":"0";Write(); }
        public static bool Feature(string key)
        {
            if (key == "ask" || key == "lens") return false;
            return J.Get(All(), "feat." + key, "1") != "0";
        }

        public static void SetFeature(string key, bool on)
        {
            if (Feature(key) == on) return;
            All()["feat." + key] = on ? "1" : "0";
            Write();
            if (FeatureChanged != null) FeatureChanged();
        }

        /* Pushed into the palette before anything is built: the chrome is styled as
         * it is constructed, so a session that starts in light mode would otherwise
         * come up black and stay black until the switch was touched. */
        public static void Start()
        {
            P.SetPalette(Theme());
        }
    }

    public static class Ui
    {
        public static TextBlock Label(string text, double size, Color ink, bool bold)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontFamily = P.Face;
            t.FontWeight = bold ? FontWeights.Bold : P.FONT_WEIGHT;
            t.FontSize = size;
            t.Foreground = P.Br(ink);
            t.TextTrimming = TextTrimming.CharacterEllipsis;
            return t;
        }

        /* WPF has no letter-spacing, and the captions are the one place the design
         * leans on it - 1.4px at 13px is most of what makes them read as captions.
         * Short strings only: per-character boxes and ellipsis do not mix, so body
         * text is set plain. */
        public static StackPanel Tracked(string text, double size, Color ink, double spacing, bool bold)
        {
            StackPanel s = new StackPanel();
            s.Orientation = Orientation.Horizontal;
            foreach (char ch in text)
            {
                TextBlock t = Label(ch.ToString(), size, ink, bold);
                t.Margin = new Thickness(0, 0, spacing, 0);
                s.Children.Add(t);
            }
            return s;
        }

        /* Opaque black rather than a blur: it is the same body the pill and the
         * alt-tab switcher use, and a panel that let the wallpaper through would
         * be the one thing on screen that did. */
        public static Border Island(double w, double h)
        {
            Border b = new Border();
            b.Width = w;
            b.Height = h;
            b.CornerRadius = new CornerRadius(P.ISLAND_R);
            b.Background = P.Br(P.C.BODY);
            b.ClipToBounds = true;
            P.Restyle += delegate { b.Background = P.Br(P.C.BODY); };
            return b;
        }

        /* The caption row: letter-spaced text along the top with pill buttons
         * pushed to the right of it. All three panels share it, which is why it is
         * here and not retyped per file. */
        public static Grid Head(double contentW, out StackPanel captionHost, StackPanel buttons)
        {
            Grid g = new Grid();
            g.Width = contentW;
            g.Height = P.HEAD_H;
            ColumnDefinition c0 = new ColumnDefinition();
            c0.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c1 = new ColumnDefinition();
            c1.Width = GridLength.Auto;
            g.ColumnDefinitions.Add(c0);
            g.ColumnDefinitions.Add(c1);

            captionHost = new StackPanel();
            captionHost.Orientation = Orientation.Horizontal;
            captionHost.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(captionHost, 0);
            g.Children.Add(captionHost);

            if (buttons != null)
            {
                buttons.Orientation = Orientation.Horizontal;
                buttons.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(buttons, 1);
                g.Children.Add(buttons);
            }
            return g;
        }

        /* A Segoe MDL2 Assets glyph, which every Windows 10 and 11 has. The icon
         * theme Linux draws these from is not here, and a drawn set of six would be
         * six marks to keep in step with nothing. */
        static readonly FontFamily IconFace = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
        public static TextBlock Glyph(string ch, double size)
        {
            TextBlock t = new TextBlock();
            t.Text = ch;
            t.FontFamily = IconFace;
            t.FontSize = size;
            t.Foreground = P.Br(P.C.INK_DIM);
            t.VerticalAlignment = VerticalAlignment.Center;
            P.WhenLoaded(t, delegate { t.Foreground = P.Br(P.C.INK_DIM); });
            return t;
        }

        public static void SetCaption(StackPanel host, string text)
        {
            host.Children.Clear();
            host.Children.Add(Tracked(text, P.FONT_HEAD, P.C.INK_DIM, 1.4, true));
        }

        /* Rounded rectangles, not pills: a pill and a text field read as the same
         * kind of object, and in here the fields genuinely are pills - the search
         * bar is one and the add-a-task bar mirrors it. Giving the buttons a
         * corner instead is what separates "type into me" from "press me". */
        public static Border Button(string text, Action click, bool destructive = false)
        {
            Border b = new Border();
            b.Height = P.BTN_H;
            b.CornerRadius = new CornerRadius(P.BTN_R);
            b.Padding = new Thickness(13, 0, 13, 0);
            b.Margin = new Thickness(8, 0, 0, 0);
            b.Background = P.Br(destructive ? P.BIN_RED : P.C.BTN_BG);
            b.Cursor = Cursors.Hand;

            StackPanel inner = Tracked(text, P.FONT_BTN, destructive ? Colors.White : P.C.INK_BODY, 0.45, true);
            inner.VerticalAlignment = VerticalAlignment.Center;
            b.Child = inner;

            /* Hover changes the colour and nothing else - the button does not move. */
            b.MouseEnter += delegate { b.Background = P.Br(destructive ? P.BIN_RED : P.C.BTN_BG_HOVER); };
            b.MouseLeave += delegate { b.Background = P.Br(destructive ? P.BIN_RED : P.C.BTN_BG); };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (click != null) click();
            };
            P.WhenLoaded(b, delegate
            {
                b.Background = P.Br(destructive ? P.BIN_RED : (b.IsMouseOver ? P.C.BTN_BG_HOVER : P.C.BTN_BG));
                foreach (TextBlock letter in inner.Children) letter.Foreground = P.Br(destructive ? Colors.White : P.C.INK_BODY);
            });
            return b;
        }

        /* A fixed-height viewport with a taller thing inside it that the wheel
         * scrolls. The deck packs masonry into one, notes packs masonry into one,
         * the to-do list stacks rows in one. */
        /* A button that is only its glyph - the notes panel's back arrow, where a
         * caption would say less than the arrow does and cost the header a third of
         * its width. Square, on the same corner as the ones with words in them. */
        public static Border IconButton(string glyph, Action click)
        {
            Border b = new Border();
            b.Width = P.BTN_H; b.Height = P.BTN_H;
            b.CornerRadius = new CornerRadius(P.BTN_R);
            b.Background = P.Br(P.C.BTN_BG);
            b.Cursor = Cursors.Hand;
            TextBlock g = Glyph(glyph, P.ICON_HEAD);
            g.HorizontalAlignment = HorizontalAlignment.Center;
            g.VerticalAlignment = VerticalAlignment.Center;
            g.Foreground = P.Br(P.C.INK_BODY);
            b.Child = g;
            P.WhenLoaded(b, delegate
            {
                b.Background = P.Br(b.IsMouseOver ? P.C.BTN_BG_HOVER : P.C.BTN_BG);
                g.Foreground = P.Br(P.C.INK_BODY);
            });
            /* Hover changes the colour and nothing else - the button does not move. */
            b.MouseEnter += delegate { b.Background = P.Br(P.C.BTN_BG_HOVER); };
            b.MouseLeave += delegate { b.Background = P.Br(P.C.BTN_BG); };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (click != null) click();
            };
            return b;
        }

        /* Throwing one card away. Quiet until the pointer is on it and red only then:
         * it used to be red the whole time it was visible, and on a wall of cards that
         * is a lot of alarm for a button you are not reaching for. */
        public static Border Bin(Action onDelete)
        {
            Border b = new Border();
            b.Width = P.BIN; b.Height = P.BIN;
            b.CornerRadius = new CornerRadius(P.BTN_R);
            b.ToolTip = "Delete";
            b.Background = P.Br(P.C.BTN_BG);
            b.Cursor = Cursors.Hand;
            TextBlock g = Glyph("", 14);
            g.HorizontalAlignment = HorizontalAlignment.Center;
            g.VerticalAlignment = VerticalAlignment.Center;
            g.Foreground = P.Br(P.C.INK_BODY);
            b.Child = g;
            Action paint = delegate
            {
                bool hot = b.IsMouseOver;
                b.Background = P.Br(hot ? P.BIN_RED : P.C.BTN_BG);
                g.Foreground = P.Br(hot ? Colors.White : P.C.INK_BODY);
            };
            paint();
            P.WhenLoaded(b, delegate { paint(); });
            b.MouseEnter += delegate { paint(); };
            b.MouseLeave += delegate { paint(); };
            /* Swallowed at the press as well, so nothing underneath ever sees the
             * click that deleted the thing it was drawn on - a press on a card is
             * the start of a drag. */
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onDelete != null) onDelete();
            };
            return b;
        }

        /* Deleting a note asks first, and it is the only delete here that does: a task
         * is a sentence you can retype and a clip is still inside whatever you copied
         * it from, but a note is a page you wrote and there is no second copy of it.
         *
         * The question expands out of the icon rather than opening a dialog somewhere
         * else - the icon is where it was asked, so the answer belongs in the same
         * place, and coming out of that point is what says the two buttons are that
         * icon. No on the left in grey, Yes on the right in the bin's own red: the
         * destructive answer is the one that needs the deliberate reach. */
        public static Border ConfirmBin(Action onYes)
        {
            Border host = new Border();
            host.Background = P.Br(Colors.Transparent);
            Grid stack = new Grid();
            host.Child = stack;

            Action<bool> arm = null;
            Border bin = Bin(delegate { arm(true); });
            stack.Children.Add(bin);

            /* Opaque, on the panel's own ground: in a header it is over nothing and
             * invisible, but on a card it is over the note's first line, and two
             * buttons with a wash behind them leave the words legible straight
             * through the question. */
            Border ask = new Border();
            ask.Background = P.Br(P.C.BODY);
            ask.CornerRadius = new CornerRadius(P.BTN_R + 4);
            ask.Padding = new Thickness(4);
            ask.Visibility = Visibility.Collapsed;
            ask.RenderTransformOrigin = new Point(1, 0.5);
            P.WhenLoaded(ask, delegate { ask.Background = P.Br(P.C.BODY); });
            StackPanel pair = new StackPanel();
            pair.Orientation = Orientation.Horizontal;
            ask.Child = pair;
            stack.Children.Add(ask);

            ScaleTransform grow = new ScaleTransform(1, 1);
            ask.RenderTransform = grow;

            arm = delegate(bool on)
            {
                if (on)
                {
                    bin.Visibility = Visibility.Collapsed;
                    ask.Visibility = Visibility.Visible;
                    grow.ScaleX = 0.35;
                    ask.Opacity = 0;
                    grow.BeginAnimation(ScaleTransform.ScaleXProperty, Back(1, 170));
                    ask.BeginAnimation(UIElement.OpacityProperty, Ease(1, 170, true));
                    return;
                }
                ask.Visibility = Visibility.Collapsed;
                grow.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                grow.ScaleX = 1;
                ask.BeginAnimation(UIElement.OpacityProperty, null);
                ask.Opacity = 1;
                bin.Visibility = Visibility.Visible;
            };
            host.Tag = arm;

            Border no = Button("NO", delegate { arm(false); });
            no.Margin = new Thickness(0);
            pair.Children.Add(no);
            Border yes = Button("YES", delegate
            {
                arm(false);
                if (onYes != null) onYes();
            }, true);
            pair.Children.Add(yes);

            /* Reaching for the bin is not the start of a drag, and the card underneath
             * must not see this press at all. */
            host.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; };
            return host;
        }

        /* Anything that takes the control off the screen - leaving the note, the
         * pointer leaving the card - puts the question away with it, or it comes back
         * still asking about something the user has moved on from. */
        public static void DisarmBin(Border host)
        {
            Action<bool> arm = host.Tag as Action<bool>;
            if (arm != null) arm(false);
        }

        public static ScrollViewer Wall(double w, double h, UIElement content)
        {
            ScrollViewer s = new ScrollViewer();
            s.Width = w;
            s.Height = h;
            s.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            s.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            s.CanContentScroll = false;
            s.Content = content;
            s.Focusable = false;
            return s;
        }

        public static double MeasureText(string text, double width, double size)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontFamily = P.Face;
            t.FontWeight = P.FONT_WEIGHT;
            t.FontSize = size;
            t.TextWrapping = TextWrapping.Wrap;
            t.Width = width;
            t.Measure(new Size(width, double.PositiveInfinity));
            return t.DesiredSize.Height;
        }

        /* The stock placeholder is a label the toolkit builds itself, with its own
         * padding and metrics - which is why one never sits quite where the typed
         * text lands. This is the same font in the same box on the same line, so
         * the first character typed appears exactly where the hint's first
         * character was. */
        public static TextBox Field(double size, string hintText, out TextBlock hintLabel)
        {
            TextBox tb = new TextBox();
            tb.FontFamily = P.Face;
            tb.FontWeight = P.FONT_WEIGHT;
            tb.FontSize = size;
            tb.Background = Brushes.Transparent;
            tb.BorderThickness = new Thickness(0);
            tb.Padding = new Thickness(0);
            tb.Margin = new Thickness(0);
            tb.Foreground = P.Br(P.C.INK_BODY);
            tb.CaretBrush = P.Br(P.C.CARET);
            tb.SelectionBrush = P.Br(P.C.SEL);
            tb.VerticalContentAlignment = VerticalAlignment.Center;

            hintLabel = Label(hintText, size, P.C.INK_DIM, false);
            hintLabel.IsHitTestVisible = false;
            hintLabel.VerticalAlignment = VerticalAlignment.Center;
            return tb;
        }

        /* Out with an overshoot, for something arriving rather than moving: the
         * settings card grows on this. */
        public static DoubleAnimation Back(double to, int ms)
        {
            DoubleAnimation a = new DoubleAnimation();
            a.To = to;
            a.Duration = TimeSpan.FromMilliseconds(ms);
            BackEase e = new BackEase();
            e.EasingMode = EasingMode.EaseOut;
            e.Amplitude = 0.4;
            a.EasingFunction = e;
            return a;
        }

        /* Bring a panel's contents up one after another rather than all at once - a
         * panel that arrives with everything already on it reads as a picture of a
         * panel. Linux's P.reveal, same numbers: 9px rise, 200ms, 26ms apart.
         * Collapsed children are skipped rather than faded, or a hidden button
         * would silently spend a step and leave a gap in the middle. */
        public static void Reveal(IEnumerable<FrameworkElement> parts, int delayMs)
        {
            int delay = delayMs;
            foreach (FrameworkElement el in parts)
            {
                if (el == null || el.Visibility != Visibility.Visible) continue;
                TranslateTransform t = el.RenderTransform as TranslateTransform;
                if (t == null) { t = new TranslateTransform(); el.RenderTransform = t; }
                el.BeginAnimation(UIElement.OpacityProperty, null);
                el.Opacity = 0;
                t.BeginAnimation(TranslateTransform.YProperty, null);
                t.Y = 9;
                DoubleAnimation fade = Ease(1, 200, true);
                DoubleAnimation rise = Ease(0, 200, true);
                fade.BeginTime = TimeSpan.FromMilliseconds(delay);
                rise.BeginTime = TimeSpan.FromMilliseconds(delay);
                el.BeginAnimation(UIElement.OpacityProperty, fade);
                t.BeginAnimation(TranslateTransform.YProperty, rise);
                delay += 26;
            }
        }

        public static DoubleAnimation Ease(double to, int ms, bool easeOut)
        {
            DoubleAnimation a = new DoubleAnimation();
            a.To = to;
            a.Duration = TimeSpan.FromMilliseconds(ms);
            CubicEase e = new CubicEase();
            e.EasingMode = easeOut ? EasingMode.EaseOut : EasingMode.EaseIn;
            a.EasingFunction = e;
            return a;
        }
    }

    /* ------------------------------------------------------------ the copy deck
     *
     * Three columns across the pill's own width, which at 720 is a 224px tile.
     * Four was right while the body text was smaller; at the search field's own
     * size it gives a clip about nine characters a line. */
    public class DeckPanel
    {
        public Border Root;
        Canvas wall;
        StackPanel caption;
        double contentW, cardW;
        Clip selected;
        bool dirty = true;

        public DeckPanel()
        {
            contentW = P.DECK_W - 2 * P.PAD_X;
            cardW = (contentW - (P.DECK_COLS - 1) * P.CARD_GAP) / P.DECK_COLS;

            Root = Ui.Island(P.DECK_W, P.DECK_H);
            StackPanel body = new StackPanel();
            body.Margin = new Thickness(P.PAD_X, P.PAD_Y, P.PAD_X, P.PAD_Y);

            StackPanel btns = new StackPanel();
            btns.Children.Add(Ui.Button("CLEAR", delegate { Deck.Clear(); }));
            body.Children.Add(Ui.Head(contentW, out caption, btns));

            wall = new Canvas();
            wall.Width = contentW;
            ScrollViewer sv = Ui.Wall(contentW, P.WALL_H, wall);
            sv.Margin = new Thickness(0, P.HEAD_GAP, 0, 0);
            body.Children.Add(sv);

            Root.Child = body;
            Deck.Changed += Invalidate;
            P.Restyle += Invalidate;
        }

        void Invalidate()
        {
            dirty = true;
            if (Root.IsVisible && Root.ActualHeight > 0) Rebuild();
        }

        public double Height() { return P.DECK_H; }

        public void Rebuild()
        {
            if (!dirty) return;
            dirty = false;
            Ui.SetCaption(caption, Deck.Count > 0 ? "COPY DECK - " + Deck.Count : "COPY DECK");
            wall.Children.Clear();
            if (Deck.Count == 0)
            {
                var empty = Ui.Label("Copied text and images will appear here.", P.FONT_BODY, P.C.INK_DIM, false);
                wall.Children.Add(empty);
            }

            /* Masonry: each card goes to whichever column is currently shortest,
             * which is what keeps the wall's bottom edge from being a staircase. */
            double[] colY = new double[P.DECK_COLS];
            foreach (Clip c in Deck.Items)
            {
                double h = CardHeight(c);
                int col = 0;
                for (int i = 1; i < P.DECK_COLS; i++) if (colY[i] < colY[col]) col = i;

                FrameworkElement card = Card(c, h);
                Canvas.SetLeft(card, col * (cardW + P.CARD_GAP));
                Canvas.SetTop(card, colY[col]);
                wall.Children.Add(card);
                colY[col] += h + P.CARD_GAP;
            }
            double tallest = 0;
            foreach (double y in colY) if (y > tallest) tallest = y;
            wall.Height = Math.Max(P.WALL_H, tallest);
        }

        /* Text takes its height from how much there is to read, which is what
         * gives the wall its rhythm. Bounded at both ends: under the floor a card
         * is too small to aim at, over the ceiling one card owns its column. */
        double CardHeight(Clip c)
        {
            if (c.CachedH > 0 && c.CachedFor == cardW) return c.CachedH;
            double h;
            if (c.Kind == "image")
            {
                double aspect = 4.0 / 3.0;   // stand-in until the real size is read
                try
                {
                    BitmapFrame f = BitmapFrame.Create(new Uri(c.File),
                        BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (f.PixelWidth > 0 && f.PixelHeight > 0)
                        aspect = (double)f.PixelWidth / f.PixelHeight;
                }
                catch { }
                h = Math.Round(Math.Max(80, Math.Min(260,
                    (cardW - 2 * P.CARD_BORDER) / aspect + 2 * P.CARD_BORDER)));
            }
            else
            {
                double want = Ui.MeasureText(c.Text, cardW - 2 * P.CARD_BORDER - 24, P.FONT_BODY) + 20;
                h = Math.Round(Math.Max(P.TEXT_H_MIN, Math.Min(P.TEXT_H_MAX, want + 2 * P.CARD_BORDER)));
            }
            c.CachedH = h;
            c.CachedFor = cardW;
            return h;
        }

        FrameworkElement Card(Clip c, double h)
        {
            Grid host = new Grid();
            host.Width = cardW;
            host.Height = h;

            Border card = new Border();
            card.Width = cardW;
            card.Height = h;
            card.CornerRadius = new CornerRadius(P.CARD_R);
            card.Background = P.Br(P.C.CARD_BG);
            card.BorderThickness = new Thickness(P.CARD_BORDER);
            card.BorderBrush = P.Br(Colors.Transparent);
            card.ClipToBounds = true;
            card.Cursor = Cursors.Hand;
            host.Children.Add(card);

            if (c.Kind == "image")
            {
                Image img = new Image();
                try
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(c.File);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.DecodePixelWidth = (int)(cardW * 2);
                    bi.EndInit();
                    img.Source = bi;
                }
                catch { }
                img.Stretch = Stretch.UniformToFill;
                card.Child = img;

                Border chip = new Border();
                chip.Background = P.Br(Color.FromArgb(150, 0, 0, 0));
                chip.CornerRadius = new CornerRadius(7);
                chip.Padding = new Thickness(6, 2, 6, 2);
                chip.HorizontalAlignment = HorizontalAlignment.Left;
                chip.VerticalAlignment = VerticalAlignment.Bottom;
                chip.Margin = new Thickness(8, 0, 0, 8);
                chip.IsHitTestVisible = false;
                chip.Child = Ui.Label(c.Text, 11, Color.FromArgb(235, 255, 255, 255), false);
                host.Children.Add(chip);
            }
            else
            {
                TextBlock t = Ui.Label(c.Text, P.FONT_BODY, P.C.INK_BODY, false);
                t.TextWrapping = TextWrapping.Wrap;
                t.Margin = new Thickness(12, 10, 12, 10);
                card.Child = t;
            }

            /* The delete button in the card's top corner, on hover. */
            Border bin = Ui.Bin(delegate { Deck.Remove(c); });
            bin.HorizontalAlignment = HorizontalAlignment.Right;
            bin.VerticalAlignment = VerticalAlignment.Top;
            bin.Margin = new Thickness(0, P.BIN_INSET, P.BIN_INSET, 0);
            bin.Visibility = Visibility.Collapsed;
            host.Children.Add(bin);

            host.MouseEnter += delegate
            {
                card.Background = P.Br(P.C.CARD_BG_HOVER);
                bin.Visibility = Visibility.Visible;
            };
            host.MouseLeave += delegate
            {
                card.Background = P.Br(P.C.CARD_BG);
                bin.Visibility = Visibility.Collapsed;
            };
            host.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                selected = c;
                Paste(c);
                /* The hairline ring, in the one colour that is not ink: the green
                 * says the clipboard now holds this and nothing else does. */
                card.BorderBrush = P.Br(P.C.COPIED);
                DispatcherTimer t = new DispatcherTimer();
                t.Interval = TimeSpan.FromMilliseconds(430);
                t.Tick += delegate { t.Stop(); card.BorderBrush = P.Br(Colors.Transparent); };
                t.Start();
            };
            return host;
        }

        void Paste(Clip c)
        {
            try
            {
                /* Writing the clipboard comes straight back as an update, and the
                 * same clip arriving as a new one is the bug that shape prevents. */
                App.Echo();
                if (c.Kind == "image")
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(c.File);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    Clipboard.SetImage(bi);
                }
                else Clipboard.SetText(c.Text);
            }
            catch { }
        }

        public void Unselect() { selected = null; }
    }

    /* --------------------------------------------------------------- reorder
     *
     * Dragging a card up or down a single-column list, with everything else moving
     * out of the way as it goes. Notes and the to-do list are the same shape, so
     * this is written once - Linux has it in panel.js as P.Reorder, with the same
     * numbers and the same rule: nothing is re-laid-out while the drag runs.
     *
     * Every card keeps the top it was built at and moves on a translate offset that
     * decays to nothing, so a drag interrupted - the launcher closing, the list
     * rebuilt underneath - leaves the list exactly as it found it. The only thing
     * committed at the end is a pair of indices, and the commit waits until the card
     * has landed in its slot: committing on the release frame snaps it there. */
    public class Reorder
    {
        public const double DRAG_PX = 6;
        const int MS = 170;

        readonly Func<List<Border>> rows;
        readonly double gap;
        readonly Action<int, int> commit;
        /* The to-do list uses this to keep a task inside its own half of the list:
         * dragging an open task in among the finished ones would only be undone by
         * the sort a frame later, and a list that springs back looks broken. */
        readonly Func<int, int[]> range;

        Border pressRow;
        Point pressAt;
        bool dragging;
        List<Border> order;
        Dictionary<Border, double> baseTop;
        Border row;
        int from, at, lo, hi;
        double y0;

        public Reorder(Func<List<Border>> rows, double gap, Action<int, int> commit, Func<int, int[]> range)
        {
            this.rows = rows; this.gap = gap; this.commit = commit; this.range = range;
        }

        public bool Active { get { return dragging; } }

        public void Press(Border r, Point p) { pressRow = r; pressAt = p; }

        static TranslateTransform Shift(Border r)
        {
            TranslateTransform t = r.RenderTransform as TranslateTransform;
            if (t == null) { t = new TranslateTransform(); r.RenderTransform = t; }
            return t;
        }

        public bool Motion(Point p)
        {
            if (dragging) { Track(p.Y); return true; }
            if (pressRow == null) return false;
            double dx = p.X - pressAt.X, dy = p.Y - pressAt.Y;
            if (dx * dx + dy * dy < DRAG_PX * DRAG_PX) return false;
            if (!Start()) { pressRow = null; return false; }
            Track(p.Y);
            return true;
        }

        /* True if this release ended a drag - the caller's cue that the click it is
         * about to act on was not a click. */
        public bool Release()
        {
            pressRow = null;
            if (!dragging) return false;
            Finish();
            return true;
        }

        public void Cancel()
        {
            pressRow = null;
            if (!dragging) return;
            dragging = false;
            foreach (Border r in order)
            {
                TranslateTransform t = Shift(r);
                t.BeginAnimation(TranslateTransform.YProperty, null);
                t.Y = 0;
                r.Opacity = 1;
                r.RenderTransform = t;
            }
        }

        bool Start()
        {
            List<Border> list = rows();
            row = pressRow;
            from = list.IndexOf(row);
            if (from < 0 || list.Count < 2) return false;

            lo = 0; hi = list.Count - 1;
            if (range != null)
            {
                int[] r = range(from);
                lo = r[0]; hi = r[1];
            }
            if (hi <= lo) return false;

            baseTop = new Dictionary<Border, double>();
            double y = 0;
            foreach (Border r in list)
            {
                baseTop[r] = y;
                y += r.Height + gap;
            }
            order = new List<Border>(list);
            at = from;
            y0 = pressAt.Y;
            dragging = true;

            Panel parent = row.Parent as Panel;
            if (parent != null)
            {
                parent.Children.Remove(row);
                parent.Children.Add(row);   /* on top of the rest while it travels */
            }
            row.Opacity = 0.92;
            return true;
        }

        void Track(double y)
        {
            Shift(row).Y = y - y0;

            /* Which slot the card is nearest, measured against the list as it would
             * be with the card taken out of it - which is what the others have
             * already moved to look like. */
            double top = baseTop[row] + (y - y0);
            List<Border> rest = new List<Border>();
            foreach (Border r in order) if (r != row) rest.Add(r);
            int best = 0;
            double bestGap = double.MaxValue, py = 0;
            for (int i = 0; i <= rest.Count; i++)
            {
                double d = Math.Abs(py - top);
                if (d < bestGap) { bestGap = d; best = i; }
                if (i < rest.Count) py += rest[i].Height + gap;
            }
            int to = Math.Max(lo, Math.Min(hi, best));
            if (to == at) return;
            at = to;
            List<Border> next = new List<Border>();
            for (int i = 0; i < to; i++) next.Add(rest[i]);
            next.Add(row);
            for (int i = to; i < rest.Count; i++) next.Add(rest[i]);
            order = next;
            Layout();
        }

        void Layout()
        {
            double y = 0;
            foreach (Border r in order)
            {
                double want = y - baseTop[r];
                y += r.Height + gap;
                if (r == row) continue;
                TranslateTransform t = Shift(r);
                if (Math.Abs(t.Y - want) < 0.01) continue;
                t.BeginAnimation(TranslateTransform.YProperty, Ui.Ease(want, MS, true));
            }
        }

        void Finish()
        {
            double y = 0;
            foreach (Border r in order)
            {
                if (r == row) break;
                y += r.Height + gap;
            }
            double want = y - baseTop[row];
            int f = from, t2 = at;
            dragging = false;
            row.Opacity = 1;

            TranslateTransform tr = Shift(row);
            DoubleAnimation a = Ui.Ease(want, MS, true);
            a.Completed += delegate { if (commit != null) commit(f, t2); };
            tr.BeginAnimation(TranslateTransform.YProperty, a);
        }
    }

    /* ---------------------------------------------------------------- the notes
     *
     * The panel to the left of the pill, and the same arrangement as Linux's
     * notes.js: it opens on the note you were last writing in, and the wall of every
     * note is one step behind the back arrow. Browsing is the occasional thing;
     * putting it first would cost a click on every single use to serve the rare one.
     *
     * Two pages at the same origin and exactly one visible. Opening a note sends the
     * list left and brings the note in from the right; back is the same movement
     * undone - mirror images, because getting that wrong is what makes a panel feel
     * like it has lost its place.
     *
     * Deleting a note asks first, and it is the only delete here that does: a task is
     * a sentence you can retype and a clip is still inside whatever you copied it
     * from, but a note is a page you wrote and there is no second copy of it. */
    public class NotesPanel
    {
        public Border Root;
        Canvas pages;
        Canvas editor, grid;
        Canvas wall;
        ScrollViewer wallView;
        StackPanel caption, noteCaption;
        TextBox sheet;
        Border sheetCard;
        TextBlock sheetHint, empty;
        Border noteBin;
        Reorder reorder;
        readonly List<Border> cards = new List<Border>();
        double contentW, bodyH;
        Note item;          /* the note on the sheet; null means a blank not yet earned */
        bool loading;       /* see OnType: set_text fires TextChanged twice */
        int builtRev = -1;

        const int SWAP_MS = 200;
        const int SWAP_IN = 130;   /* how far into one page's exit the next arrives */
        const double TX = 14, TY = 12;

        public NotesPanel()
        {
            contentW = P.PANEL_W - 2 * P.PAD_X;
            bodyH = P.PANEL_H - 2 * P.PAD_Y - P.HEAD_H - P.BODY_GAP;

            Root = Ui.Island(P.PANEL_W, P.PANEL_H);
            pages = new Canvas();
            pages.Width = P.PANEL_W; pages.Height = P.PANEL_H;
            Root.Child = pages;

            editor = BuildEditor();
            grid = BuildList();
            pages.Children.Add(grid);
            pages.Children.Add(editor);

            ShowEditor(null, false);
        }

        Canvas Page(out StackPanel body)
        {
            Canvas page = new Canvas();
            page.Width = P.PANEL_W; page.Height = P.PANEL_H;
            body = new StackPanel();
            body.Width = contentW;
            /* P.BODY_GAP under the head, not HEAD_GAP: that is what puts the sheet's
             * top edge - and the first card's, on the other page - on the copy deck's
             * top edge, so the three of them start on one line. */
            body.Margin = new Thickness(P.PAD_X, P.PAD_Y, P.PAD_X, P.PAD_Y);
            page.Children.Add(body);
            return page;
        }

        /* ------------------------------------------------------------ editor */

        Canvas BuildEditor()
        {
            StackPanel body;
            Canvas page = Page(out body);

            Grid head = new Grid();
            head.Height = P.HEAD_H;
            head.Width = contentW;
            ColumnDefinition c0 = new ColumnDefinition(); c0.Width = GridLength.Auto;
            ColumnDefinition c1 = new ColumnDefinition(); c1.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c2 = new ColumnDefinition(); c2.Width = GridLength.Auto;
            head.ColumnDefinitions.Add(c0); head.ColumnDefinitions.Add(c1); head.ColumnDefinitions.Add(c2);

            Border back = Ui.IconButton("\uE76B", delegate { ShowList(true); });
            back.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(back, 0);
            head.Children.Add(back);

            noteCaption = Ui.Tracked("NOTE", P.FONT_HEAD, P.C.INK_DIM, 1.4, true);
            noteCaption.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(noteCaption, 1);
            head.Children.Add(noteCaption);

            noteBin = Ui.ConfirmBin(delegate { DeleteOpen(); });
            Grid.SetColumn(noteBin, 2);
            head.Children.Add(noteBin);
            body.Children.Add(head);

            /* The sheet: one card, as tall as everything the header leaves behind. */
            sheetCard = new Border();
            sheetCard.Width = contentW; sheetCard.Height = bodyH;
            sheetCard.CornerRadius = new CornerRadius(P.CARD_R);
            sheetCard.BorderThickness = new Thickness(P.CARD_BORDER);
            sheetCard.Margin = new Thickness(0, P.BODY_GAP, 0, 0);
            sheetCard.ClipToBounds = true;
            Ring(false);
            P.Restyle += delegate { Ring(sheet != null && sheet.IsFocused); };

            Grid stack = new Grid();
            sheetCard.Child = stack;

            sheetHint = Ui.Label("Write a note", P.FONT_BODY, P.C.INK_DIM, false);
            sheetHint.Margin = new Thickness(TX + P.HINT_NUDGE, TY, 0, 0);
            sheetHint.VerticalAlignment = VerticalAlignment.Top;
            sheetHint.HorizontalAlignment = HorizontalAlignment.Left;
            P.Restyle += delegate { sheetHint.Foreground = P.Br(P.C.INK_DIM); };
            stack.Children.Add(sheetHint);

            /* A real multi-line box, scrolling itself and following its own caret -
             * the Linux side hand-rolls that because an St.Entry centres its text in
             * whatever allocation it is given, and WPF's TextBox does not. Enter is a
             * newline: the sheet is the document and there is nothing to submit. */
            sheet = new TextBox();
            sheet.AcceptsReturn = true;
            sheet.TextWrapping = TextWrapping.Wrap;
            sheet.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            sheet.VerticalContentAlignment = VerticalAlignment.Top;
            sheet.Background = P.Br(Colors.Transparent);
            sheet.BorderThickness = new Thickness(0);
            sheet.Padding = new Thickness(TX, TY, TX, TY);
            sheet.FontFamily = P.Face;
            sheet.FontWeight = P.FONT_WEIGHT;
            sheet.FontSize = P.FONT_BODY;
            sheet.Foreground = P.Br(P.C.INK_BODY);
            sheet.CaretBrush = P.Br(P.C.CARET);
            sheet.SelectionBrush = P.Br(P.C.SEL);
            P.Restyle += delegate
            {
                sheet.Foreground = P.Br(P.C.INK_BODY);
                sheet.CaretBrush = P.Br(P.C.CARET);
                sheet.SelectionBrush = P.Br(P.C.SEL);
            };
            sheet.TextChanged += delegate
            {
                sheetHint.Visibility = sheet.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
                OnType();
            };
            sheet.GotKeyboardFocus += delegate { Ring(true); };
            sheet.LostKeyboardFocus += delegate { Ring(false); };
            stack.Children.Add(sheet);

            /* Clicking the sheet anywhere puts the caret in it, including the margin
             * around the text. */
            sheetCard.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                sheet.Focus();
                Keyboard.Focus(sheet);
                e.Handled = true;
            };

            body.Children.Add(sheetCard);
            return page;
        }

        public string AlignmentReport()
        {
            Point p = sheetCard.TranslatePoint(new Point(0, 0), Root);
            return "Notes insets: left=" + p.X + " right=" + (Root.ActualWidth - p.X - sheetCard.ActualWidth) + " bottom=" + (Root.ActualHeight - p.Y - sheetCard.ActualHeight);
        }
        void Ring(bool on)
        {
            sheetCard.Background = P.Br(on ? P.C.CARD_BG_HOVER : P.C.CARD_BG);
            sheetCard.BorderBrush = P.Br(on ? P.C.RING : Colors.Transparent);
        }

        /* The sheet earns its note the moment there is something on it, and not
         * before - so opening the panel, reading, and leaving creates nothing.
         *
         * `loading` is not defensive tidiness: handing a box a new string is a delete
         * followed by an insert and fires this for both, and the first report is of an
         * empty field. Without the guard, opening a note writes "" over it and the
         * next commit throws it away - a note destroyed by being read. */
        void OnType()
        {
            if (loading) return;
            string text = sheet.Text;
            if (item == null)
            {
                if (text.Trim().Length == 0) return;
                item = Notes.Add("");
                noteBin.Visibility = Visibility.Visible;
                Ui.SetCaption(noteCaption, "NOTE");
            }
            Notes.SetText(item, text);
        }

        /* Called on every way out of the sheet. A note that has been emptied is
         * thrown away here rather than left as a blank card in the wall. The
         * emptiness is read off the stored note, never off the box - a torn-down
         * box answers with "", and taking that as the answer is how a real note gets
         * deleted by a teardown. */
        void Commit()
        {
            if (item != null && item.Text.Trim().Length == 0)
            {
                Notes.Remove(item);
                item = null;
            }
        }

        void DeleteOpen()
        {
            Note n = item;
            item = null;
            if (n != null) Notes.Remove(n);
            ShowEditor(Latest(), false);
            Fill(true);
        }

        static Note Latest()
        {
            Note best = null;
            foreach (Note n in Notes.Items)
                if (best == null || n.When > best.When) best = n;
            return best;
        }

        /* -------------------------------------------------------------- list */

        Canvas BuildList()
        {
            StackPanel body;
            Canvas page = Page(out body);

            Grid head = new Grid();
            head.Height = P.HEAD_H; head.Width = contentW;
            ColumnDefinition c0 = new ColumnDefinition(); c0.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c1 = new ColumnDefinition(); c1.Width = GridLength.Auto;
            head.ColumnDefinitions.Add(c0); head.ColumnDefinitions.Add(c1);

            caption = Ui.Tracked("NOTES", P.FONT_HEAD, P.C.INK_DIM, 1.4, true);
            caption.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(caption, 0);
            head.Children.Add(caption);

            Border newBtn = Ui.Button("NEW", delegate { ShowEditor(null, true); });
            Grid.SetColumn(newBtn, 1);
            head.Children.Add(newBtn);
            body.Children.Add(head);

            wall = new Canvas();
            wall.Width = contentW;
            wallView = Ui.Wall(contentW, bodyH, wall);
            wallView.Margin = new Thickness(0, P.BODY_GAP, 0, 0);
            body.Children.Add(wallView);

            empty = Ui.Label("No notes yet", P.FONT_BODY, P.C.INK_DIM, false);
            empty.Width = contentW;
            empty.TextAlignment = TextAlignment.Center;
            Canvas.SetTop(empty, 6);
            P.Restyle += delegate { empty.Foreground = P.Br(P.C.INK_DIM); };
            wall.Children.Add(empty);

            reorder = new Reorder(delegate { return cards; }, P.CARD_GAP,
                delegate(int f, int t) { Notes.Move(f, t); Fill(true); }, null);
            return page;
        }

        /* What a card shows of a note. The height ceiling is what really bounds it;
         * this only has to give it enough to fill that. */
        static string Preview(string text)
        {
            string[] lines = text.Replace("\t", "    ").Split('\n');
            int start = 0;
            while (start < lines.Length && lines[start].Trim().Length == 0) start++;
            List<string> keep = new List<string>();
            for (int i = start; i < lines.Length && keep.Count < 6; i++) keep.Add(lines[i]);
            string outp = string.Join("\n", keep.ToArray());
            return outp.Length > 240 ? outp.Substring(0, 240) : outp;
        }

        /* Rebuilt only when the list itself has changed - the wall is read-only, so
         * nothing but an add, a delete or an edit elsewhere can make it wrong. */
        void Fill(bool force)
        {
            if (!force && builtRev == Notes.Revision) return;
            builtRev = Notes.Revision;

            foreach (Border c in cards) wall.Children.Remove(c);
            cards.Clear();

            double y = 0;
            foreach (Note n in Notes.Items)
            {
                Border card = Card(n);
                Canvas.SetTop(card, y);
                Canvas.SetLeft(card, 0);
                y += card.Height + P.CARD_GAP;
                cards.Add(card);
                wall.Children.Add(card);
            }
            wall.Height = Math.Max(bodyH, Math.Max(0, y - P.CARD_GAP));
            wallView.ScrollToTop();

            int count = Notes.Items.Count;
            Ui.SetCaption(caption, count > 0 ? "NOTES - " + count : "NOTES");
            empty.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        Border Card(Note n)
        {
            double w = contentW;
            Border card = new Border();
            card.Width = w;
            card.CornerRadius = new CornerRadius(P.CARD_R);
            card.BorderThickness = new Thickness(P.CARD_BORDER);
            card.BorderBrush = P.Br(Colors.Transparent);
            card.Background = P.Br(P.C.CARD_BG);
            card.ClipToBounds = true;
            card.Cursor = Cursors.Hand;
            P.WhenLoaded(card, delegate { card.Background = P.Br(card.IsMouseOver ? P.C.CARD_BG_HOVER : P.C.CARD_BG); });

            Grid inner = new Grid();
            card.Child = inner;

            TextBlock label = Ui.Label(Preview(n.Text), P.FONT_BODY, P.C.INK_BODY, false);
            label.TextWrapping = TextWrapping.Wrap;
            label.Width = w - 2 * P.CARD_BORDER - 2 * TX;
            label.Margin = new Thickness(TX, TY, TX, TY);
            label.VerticalAlignment = VerticalAlignment.Top;
            P.WhenLoaded(label, delegate { label.Foreground = P.Br(P.C.INK_BODY); });
            inner.Children.Add(label);

            label.Measure(new Size(label.Width, double.PositiveInfinity));
            double h = Math.Max(P.TEXT_H_MIN, Math.Min(P.TEXT_H_MAX,
                Math.Ceiling(label.DesiredSize.Height) + 2 * TY + 2 * P.CARD_BORDER));
            card.Height = h;

            Border bin = Ui.ConfirmBin(delegate { Remove(n); });
            bin.HorizontalAlignment = HorizontalAlignment.Right;
            bin.VerticalAlignment = VerticalAlignment.Top;
            bin.Margin = new Thickness(0, P.BIN_INSET, P.BIN_INSET, 0);
            bin.Visibility = Visibility.Collapsed;
            inner.Children.Add(bin);

            card.MouseEnter += delegate
            {
                card.Background = P.Br(P.C.CARD_BG_HOVER);
                /* Not while something is being dragged past it. */
                bin.Visibility = reorder.Active ? Visibility.Collapsed : Visibility.Visible;
            };
            card.MouseLeave += delegate
            {
                card.Background = P.Br(P.C.CARD_BG);
                /* The pointer leaving is an answer of sorts, and it is "no": a
                 * control left armed under a hidden card comes back still asking. */
                Ui.DisarmBin(bin);
                bin.Visibility = Visibility.Collapsed;
            };
            /* Press is recorded rather than acted on: whether this is "open that
             * note" or the start of a drag is not known until the pointer moves. */
            card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                reorder.Press(card, e.GetPosition(wall));
                card.CaptureMouse();
            };
            card.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (e.LeftButton == MouseButtonState.Pressed) reorder.Motion(e.GetPosition(wall));
            };
            card.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                card.ReleaseMouseCapture();
                e.Handled = true;
                if (reorder.Release()) return;
                ShowEditor(n, true);
            };
            return card;
        }

        void Remove(Note n)
        {
            if (item == n) item = null;
            Notes.Remove(n);
            Fill(true);
        }

        /* -------------------------------------------------------------- views */

        /* `n` null means a blank sheet - a note that does not exist yet. `slide` is a
         * move the user made from the list; every other way in is the panel being set
         * up, and a transition on those would be an animation nobody asked for. */
        public void ShowEditor(Note n, bool slide)
        {
            Ui.DisarmBin(noteBin);
            item = n;
            loading = true;
            sheet.Text = n != null ? n.Text : "";
            loading = false;
            sheet.CaretIndex = sheet.Text.Length;
            sheetHint.Visibility = sheet.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            Ring(false);
            noteBin.Visibility = n != null ? Visibility.Visible : Visibility.Collapsed;
            Ui.SetCaption(noteCaption, n != null ? "NOTE" : "NEW NOTE");
            if (slide) { Swap(grid, editor, -1); return; }
            grid.Visibility = Visibility.Collapsed;
            editor.Visibility = Visibility.Visible;
        }

        public void ShowList(bool slide)
        {
            Commit();
            Fill(true);
            if (slide) { Swap(editor, grid, 1); return; }
            editor.Visibility = Visibility.Collapsed;
            grid.Visibility = Visibility.Visible;
        }

        /* One page leaves, the next arrives behind it once the first is most of the
         * way gone - sequential, not a crossfade. Two pages moving together read as a
         * carousel, and there is only one page here: it is being replaced, not
         * scrolled past. `dir` -1 is forward (into a note), 1 is back. */
        void Swap(Canvas going, Canvas coming, int dir)
        {
            going.Visibility = Visibility.Visible;
            coming.Visibility = Visibility.Visible;

            TranslateTransform a = new TranslateTransform();
            going.RenderTransform = a;
            TranslateTransform b = new TranslateTransform();
            b.X = -dir * P.PANEL_W;
            coming.RenderTransform = b;

            a.BeginAnimation(TranslateTransform.XProperty, Ui.Ease(dir * P.PANEL_W, SWAP_MS, false));
            going.BeginAnimation(UIElement.OpacityProperty, Ui.Ease(0, SWAP_MS, false));

            DoubleAnimation slide = Ui.Ease(0, SWAP_MS, true);
            slide.BeginTime = TimeSpan.FromMilliseconds(SWAP_IN);
            coming.Opacity = 0;
            DoubleAnimation fade = Ui.Ease(1, SWAP_MS, true);
            fade.BeginTime = TimeSpan.FromMilliseconds(SWAP_IN);
            b.BeginAnimation(TranslateTransform.XProperty, slide);
            coming.BeginAnimation(UIElement.OpacityProperty, fade);

            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromMilliseconds(SWAP_MS + SWAP_IN + 20);
            t.Tick += delegate
            {
                t.Stop();
                going.Visibility = Visibility.Collapsed;
                going.BeginAnimation(UIElement.OpacityProperty, null);
                going.Opacity = 1;
                a.BeginAnimation(TranslateTransform.XProperty, null);
                a.X = 0;
            };
            t.Start();
        }

        /* Called on every open of the launcher: back to the note that was last
         * written in, which is what the panel is for. */
        public void Rebuild()
        {
            Commit();
            Fill(true);
            ShowEditor(Latest(), false);
            Ui.Reveal(new FrameworkElement[] { noteCaption, sheetCard }, 90);
        }

        /* The launcher is closing: whatever is on the sheet is kept, and nothing is
         * left mid-drag. */
        public void Park()
        {
            reorder.Cancel();
            Commit();
            Notes.Flush();
        }
    }

    /* ---------------------------------------------------------------- the to-do
     *
     * The panel to the right of the pill, mirroring the notes - same island, same
     * caption, same cards on the same wash. What differs, differs only because the
     * content is a different shape: a task is one line about one thing, so these are
     * full-width rows rather than a wall, and the whole row is the checkbox.
     *
     * Done tasks sink rather than vanishing: struck through, moved to the bottom,
     * kept, because the list is also a record of what got finished - until Clear done
     * says otherwise.
     *
     * Two movements, from either end of the same idea. A task arriving pushes the
     * list down, each row starting a frame or two after the one above it; a task
     * being ticked floods with the deck's green out of its own checkbox and the row
     * kicks at its free end. The re-sort waits for the flood: a ticked task that
     * dropped to the bottom on the click frame would take the answer to the click
     * down with it. Unticking skips the ceremony. Linux: todo.js. */
    public class TodoPanel
    {
        public Border Root;
        StackPanel caption;
        TextBox add;
        TextBlock addHint, empty;
        Border clearBtn;
        Canvas wall;
        ScrollViewer wallView;
        Reorder reorder;
        readonly List<Border> rows = new List<Border>();
        readonly Dictionary<Border, Job> owner = new Dictionary<Border, Job>();
        readonly Dictionary<Border, double> boxY = new Dictionary<Border, double>();
        double contentW, wallH;
        int builtRev = -1;

        const double ADD_H = 46;
        const double ADD_GAP = P.HEAD_GAP;
        const double ADD_PAD_X = 16;
        const double ROW_R = 16;
        const double ROW_PAD_X = 14;
        const double ROW_PAD_Y = 12;
        const double ROW_GAP = 8;
        const double BOX = 22;
        const double BOX_GAP = 14;
        const double ROW_H_MIN = 54;
        const double ROW_H_MAX = 170;

        const int SHIFT_MS = 340;
        const int SHIFT_STEP = 26;
        const int TICK_MS = 380;
        const double TICK_ALPHA = 0.34;
        const double TICK_KICK = 1.035;
        const int TICK_KICK_MS = 120;
        const int TICK_BACK_MS = 250;

        public TodoPanel()
        {
            contentW = P.PANEL_W - 2 * P.PAD_X;
            wallH = P.PANEL_H - 2 * P.PAD_Y - P.HEAD_H - P.BODY_GAP - ADD_H - ADD_GAP;

            Root = Ui.Island(P.PANEL_W, P.PANEL_H);
            StackPanel body = new StackPanel();
            body.Margin = new Thickness(P.PAD_X, P.PAD_Y, P.PAD_X, P.PAD_Y);
            Root.Child = body;

            Grid head = new Grid();
            head.Height = P.HEAD_H; head.Width = contentW;
            ColumnDefinition c0 = new ColumnDefinition(); c0.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c1 = new ColumnDefinition(); c1.Width = GridLength.Auto;
            head.ColumnDefinitions.Add(c0); head.ColumnDefinitions.Add(c1);

            caption = Ui.Tracked("TO-DO", P.FONT_HEAD, P.C.INK_DIM, 1.4, true);
            caption.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(caption, 0);
            head.Children.Add(caption);

            clearBtn = Ui.Button("CLEAR DONE", delegate
            {
                Todo.ClearDone();
                Refresh(true);
            });
            Grid.SetColumn(clearBtn, 1);
            head.Children.Add(clearBtn);
            body.Children.Add(head);

            /* A bar rather than a bare field, built the way the search pill above it
             * is: a glyph, then the text, on one centred line. The pill background is
             * on the bar and the field inside it is flat, or it would paint a second
             * rounded rectangle inside the first. */
            Border bar = new Border();
            bar.Height = ADD_H; bar.Width = contentW;
            bar.CornerRadius = new CornerRadius(ADD_H / 2);
            bar.Background = P.Br(P.C.CARD_BG);
            bar.Padding = new Thickness(ADD_PAD_X, 0, ADD_PAD_X, 0);
            bar.Margin = new Thickness(0, P.BODY_GAP, 0, 0);
            P.Restyle += delegate { bar.Background = P.Br(P.C.CARD_BG); };

            Grid barGrid = new Grid();
            ColumnDefinition b0 = new ColumnDefinition(); b0.Width = GridLength.Auto;
            ColumnDefinition b1 = new ColumnDefinition(); b1.Width = new GridLength(1, GridUnitType.Star);
            barGrid.ColumnDefinitions.Add(b0); barGrid.ColumnDefinitions.Add(b1);

            TextBlock addGlyph = Ui.Glyph("", P.ICON_HEAD);
            addGlyph.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(addGlyph, 0);
            barGrid.Children.Add(addGlyph);

            Grid field = new Grid();
            Grid.SetColumn(field, 1);
            barGrid.Children.Add(field);

            addHint = Ui.Label("Add a task", P.FONT_BODY, P.C.INK_DIM, false);
            addHint.VerticalAlignment = VerticalAlignment.Center;
            addHint.Margin = new Thickness(P.HINT_NUDGE, 0, 0, 0);
            P.Restyle += delegate { addHint.Foreground = P.Br(P.C.INK_DIM); };
            field.Children.Add(addHint);

            add = new TextBox();
            add.Background = P.Br(Colors.Transparent);
            add.BorderThickness = new Thickness(0);
            add.Padding = new Thickness(0);
            add.FontFamily = P.Face;
            add.FontWeight = P.FONT_WEIGHT;
            add.FontSize = P.FONT_BODY;
            add.VerticalContentAlignment = VerticalAlignment.Center;
            add.Foreground = P.Br(P.C.INK_BODY);
            add.CaretBrush = P.Br(P.C.CARET);
            add.SelectionBrush = P.Br(P.C.SEL);
            P.Restyle += delegate
            {
                add.Foreground = P.Br(P.C.INK_BODY);
                add.CaretBrush = P.Br(P.C.CARET);
                add.SelectionBrush = P.Br(P.C.SEL);
            };
            add.TextChanged += delegate
            {
                addHint.Visibility = add.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            };
            /* Enter means "done, next" - the exact opposite of the notes sheet, where
             * the same key has to be a new line. A note is a document you come back
             * to; a task is a sentence you say once. */
            add.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter && add.Text.Trim().Length > 0)
                {
                    Job made = Todo.Add(add.Text.Trim());
                    add.Text = "";
                    Insert(made);
                    e.Handled = true;
                }
            };
            field.Children.Add(add);

            /* Clicking anywhere along the bar puts the caret in it - the glyph and the
             * padding are part of the target, not decoration beside it. */
            bar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                add.Focus();
                Keyboard.Focus(add);
                e.Handled = true;
            };
            bar.Child = barGrid;
            body.Children.Add(bar);

            wall = new Canvas();
            wall.Width = contentW;
            wallView = Ui.Wall(contentW, wallH, wall);
            wallView.Margin = new Thickness(0, ADD_GAP, 0, 0);
            body.Children.Add(wallView);

            empty = Ui.Label("Nothing to do", P.FONT_BODY, P.C.INK_DIM, false);
            empty.Width = contentW;
            empty.TextAlignment = TextAlignment.Center;
            Canvas.SetTop(empty, 6);
            P.Restyle += delegate { empty.Foreground = P.Br(P.C.INK_DIM); };
            wall.Children.Add(empty);

            /* A drag is clamped to its own half of the list: dragging an open task
             * down among the finished ones would only be undone by the sort a frame
             * later. */
            reorder = new Reorder(delegate { return rows; }, ROW_GAP,
                delegate(int f, int t) { Todo.Move(f, t); Refresh(true); },
                delegate(int from) { return Half(from); });

        }

        int[] Half(int from)
        {
            bool done = from < rows.Count && owner[rows[from]].Done;
            int lo = 0, hi = rows.Count - 1;
            for (int i = 0; i < rows.Count; i++)
            {
                if (owner[rows[i]].Done == done) { lo = i; break; }
            }
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                if (owner[rows[i]].Done == done) { hi = i; break; }
            }
            return new int[] { lo, hi };
        }

        /* -------------------------------------------------------------- building */

        public void Refresh(bool force)
        {
            if (!force && builtRev == Todo.Revision) return;
            builtRev = Todo.Revision;

            foreach (Border r in rows) wall.Children.Remove(r);
            rows.Clear();
            owner.Clear();
            boxY.Clear();

            double y = 0;
            foreach (Job t in Todo.Items)
            {
                Border row = Row(t);
                Canvas.SetTop(row, y);
                Canvas.SetLeft(row, 0);
                y += row.Height + ROW_GAP;
                rows.Add(row);
                wall.Children.Add(row);
            }
            wall.Height = Math.Max(wallH, Math.Max(0, y - ROW_GAP));
            wallView.ScrollToTop();
            SyncHead();
        }

        void SyncHead()
        {
            int open = Todo.Open();
            int done = Todo.Items.Count - open;
            Ui.SetCaption(caption, Todo.Items.Count > 0 ? "TO-DO - " + open + " OPEN" : "TO-DO");
            clearBtn.Visibility = done > 0 ? Visibility.Visible : Visibility.Collapsed;
            empty.Visibility = Todo.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /* A task arriving pushes the list down, and each row starts a frame or two
         * after the one above it: moving them all on one curve is a block of text
         * jumping, which says nothing about what happened. Nothing is torn down, so
         * there is nothing left to flicker. */
        void Insert(Job made)
        {
            if (made == null) return;
            if (Todo.Items.Count != rows.Count + 1 || Todo.Items[0] != made)
            {
                Refresh(true);
                return;
            }

            Border row = Row(made);
            Canvas.SetTop(row, 0);
            Canvas.SetLeft(row, 0);
            double drop = row.Height + ROW_GAP;

            int n = 0;
            foreach (Border r in rows)
            {
                Canvas.SetTop(r, Canvas.GetTop(r) + drop);
                TranslateTransform t = r.RenderTransform as TranslateTransform;
                if (t == null) { t = new TranslateTransform(); r.RenderTransform = t; }
                t.BeginAnimation(TranslateTransform.YProperty, null);
                t.Y = -drop;
                DoubleAnimation back = Ui.Back(0, SHIFT_MS);
                back.BeginTime = TimeSpan.FromMilliseconds(SHIFT_STEP * n);
                t.BeginAnimation(TranslateTransform.YProperty, back);
                n++;
            }

            rows.Insert(0, row);
            wall.Children.Add(row);
            wall.Height = Math.Max(wallH, wall.Height + drop);
            builtRev = Todo.Revision;
            SyncHead();

            ScaleTransform grow = new ScaleTransform(0.96, 1);
            row.RenderTransformOrigin = new Point(0, 0.5);
            row.RenderTransform = grow;
            row.Opacity = 0;
            row.BeginAnimation(UIElement.OpacityProperty, Ui.Back(1, SHIFT_MS));
            grow.BeginAnimation(ScaleTransform.ScaleXProperty, Ui.Back(1, SHIFT_MS));
        }

        Border Row(Job t)
        {
            double w = contentW;
            Border row = new Border();
            row.Width = w;
            row.CornerRadius = new CornerRadius(ROW_R);
            row.Background = P.Br(Colors.Transparent);
            row.ClipToBounds = true;
            row.Cursor = Cursors.Hand;
            owner[row] = t;

            Grid inner = new Grid();
            ColumnDefinition c0 = new ColumnDefinition(); c0.Width = GridLength.Auto;
            ColumnDefinition c1 = new ColumnDefinition(); c1.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c2 = new ColumnDefinition(); c2.Width = GridLength.Auto;
            inner.ColumnDefinitions.Add(c0); inner.ColumnDefinitions.Add(c1); inner.ColumnDefinitions.Add(c2);
            inner.Margin = new Thickness(ROW_PAD_X, ROW_PAD_Y, ROW_PAD_X, ROW_PAD_Y);
            row.Child = inner;

            /* The ring, centred on the first line of the words rather than on the
             * whole band - a two-line task with a circle floating in the middle of it
             * reads as a circle that has come loose. */
            Border box = new Border();
            box.Width = BOX; box.Height = BOX;
            box.CornerRadius = new CornerRadius(BOX / 2);
            box.BorderThickness = new Thickness(2);
            box.Margin = new Thickness(0, 0, BOX_GAP, 0);
            box.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(box, 0);
            inner.Children.Add(box);

            TextBlock label = Ui.Label(t.Text, P.FONT_BODY, t.Done ? P.C.INK_DIM : P.C.INK_BODY, false);
            label.TextWrapping = TextWrapping.Wrap;
            if (t.Done) label.TextDecorations = TextDecorations.Strikethrough;
            double textW = w - 2 * ROW_PAD_X - BOX - BOX_GAP - P.BIN - 4;
            label.Width = textW;
            Grid.SetColumn(label, 1);
            inner.Children.Add(label);

            label.Measure(new Size(textW, double.PositiveInfinity));
            double band = Math.Max(ROW_H_MIN - 2 * ROW_PAD_Y, Math.Min(ROW_H_MAX - 2 * ROW_PAD_Y,
                Math.Ceiling(label.DesiredSize.Height)));
            row.Height = band + 2 * ROW_PAD_Y;
            /* One line of the words, for the ring to sit against. */
            double line = Math.Min(band, label.FontSize * 1.35);
            box.Margin = new Thickness(0, Math.Max(0, (line - BOX) / 2), BOX_GAP, 0);
            boxY[row] = ROW_PAD_Y + Math.Max(0, (line - BOX) / 2);

            Border bin = Ui.Bin(delegate { Todo.Remove(t); Refresh(true); });
            bin.VerticalAlignment = VerticalAlignment.Center;
            bin.Visibility = Visibility.Collapsed;
            Grid.SetColumn(bin, 2);
            inner.Children.Add(bin);

            Action paint = delegate
            {
                bool hot = row.IsMouseOver;
                row.Background = P.Br(hot ? P.C.CARD_BG : Colors.Transparent);
                box.BorderBrush = P.Br(t.Done ? P.C.COPIED : P.C.INK_DIM);
                box.Background = P.Br(t.Done ? P.C.COPIED : Colors.Transparent);
                label.Foreground = P.Br(t.Done ? P.C.INK_DIM : P.C.INK_BODY);
            };
            paint();
            P.WhenLoaded(row, delegate { paint(); });
            row.MouseEnter += delegate
            {
                paint();
                bin.Visibility = reorder.Active ? Visibility.Collapsed : Visibility.Visible;
            };
            row.MouseLeave += delegate { paint(); bin.Visibility = Visibility.Collapsed; };

            row.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                reorder.Press(row, e.GetPosition(wall));
                row.CaptureMouse();
            };
            row.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (e.LeftButton == MouseButtonState.Pressed) reorder.Motion(e.GetPosition(wall));
            };
            row.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                row.ReleaseMouseCapture();
                e.Handled = true;
                if (reorder.Release()) return;
                ToggleRow(row, t);
            };
            return row;
        }

        /* Ticking is two things in a row, not one: the tick lands on the press, and
         * the re-sort waits for the green to finish crossing the row. An answer a
         * third of a second late reads as the click not having registered, so the box
         * fills first and the ceremony follows. */
        void ToggleRow(Border row, Job t)
        {
            bool willBeDone = !t.Done;
            Todo.Toggle(t);
            if (!willBeDone)
            {
                Refresh(true);
                return;
            }
            SyncHead();
            Flood(row, delegate { Refresh(true); });
        }

        /* A disc of the deck's green opens out of the checkbox, floods the row and
         * fades as it passes the edges, clipped to the row's own rounded rectangle -
         * so what is seen is the row filling rather than a circle drawn on top of one.
         * The kick is the other half: the row is pinned at its left edge, where the
         * checkbox is, and the free end is driven out and let back. */
        void Flood(Border row, Action done)
        {
            double w = row.Width, h = row.Height;
            Canvas art = new Canvas();
            art.Width = w; art.Height = h;
            art.IsHitTestVisible = false;
            art.Clip = new RectangleGeometry(new Rect(0, 0, w, h), ROW_R, ROW_R);

            double cx = ROW_PAD_X + BOX / 2;
            double cy = boxY.ContainsKey(row) ? boxY[row] + BOX / 2 : h / 2;
            double dx = Math.Max(cx, w - cx), dy = Math.Max(cy, h - cy);
            double reach = Math.Sqrt(dx * dx + dy * dy);

            Shapes.Ellipse disc = new Shapes.Ellipse();
            disc.Width = reach * 2; disc.Height = reach * 2;
            disc.Fill = P.Br(P.C.COPIED);
            Canvas.SetLeft(disc, cx - reach);
            Canvas.SetTop(disc, cy - reach);
            ScaleTransform grow = new ScaleTransform(BOX / 2 / reach, BOX / 2 / reach);
            disc.RenderTransformOrigin = new Point(0.5, 0.5);
            disc.RenderTransform = grow;
            disc.Opacity = TICK_ALPHA;
            art.Children.Add(disc);

            Grid host = row.Child as Grid;
            Grid wrap = new Grid();
            row.Child = null;
            wrap.Children.Add(host);
            wrap.Children.Add(art);
            row.Child = wrap;

            /* Out fast and gone slowly: the leading edge is the answer to the click,
             * and the fade behind it is the row getting on with being finished. */
            grow.BeginAnimation(ScaleTransform.ScaleXProperty, Ui.Ease(1, TICK_MS, true));
            grow.BeginAnimation(ScaleTransform.ScaleYProperty, Ui.Ease(1, TICK_MS, true));
            DoubleAnimation fade = new DoubleAnimation();
            fade.To = 0;
            fade.Duration = TimeSpan.FromMilliseconds(TICK_MS);
            QuadraticEase qe = new QuadraticEase();
            qe.EasingMode = EasingMode.EaseIn;
            fade.EasingFunction = qe;
            disc.BeginAnimation(UIElement.OpacityProperty, fade);

            ScaleTransform kick = new ScaleTransform(1, 1);
            row.RenderTransformOrigin = new Point(0, 0.5);
            row.RenderTransform = kick;
            DoubleAnimation out_ = Ui.Ease(TICK_KICK, TICK_KICK_MS, true);
            out_.Completed += delegate
            {
                kick.BeginAnimation(ScaleTransform.ScaleXProperty, Ui.Back(1, TICK_BACK_MS));
            };
            kick.BeginAnimation(ScaleTransform.ScaleXProperty, out_);

            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromMilliseconds(TICK_MS + 20);
            t.Tick += delegate
            {
                t.Stop();
                if (done != null) done();
            };
            t.Start();
        }

        public void Rebuild()
        {
            Refresh(true);
            Ui.Reveal(new FrameworkElement[] { caption, clearBtn, add, wallView }, 90);
        }

        public void Park()
        {
            reorder.Cancel();
            Todo.Flush();
        }
    }

    /* ------------------------------------------------------- the settings card
     *
     * Pressing the mark gathers the notes panel, the to-do panel, the copy deck and
     * the search pill into the middle of where they were, and this card comes up out
     * of the point they went into - the Linux arrangement, described in
     * super/settings-and-palette.md.
     *
     * It is deliberately smaller than the spread it replaced: one that filled the
     * same space would read as the panels having been repainted, and collapsing
     * first is what says the whole thing was folded up and put away. The card grows
     * already showing the settings, and its contents come up a beat behind it on
     * Ui.Reveal - the eye is led from the thing that arrived to the things on it.
     * There is no flip. */
    public class SettingsCard
    {
        public Border Root;
        readonly ScaleTransform grow = new ScaleTransform(1, 1);
        readonly List<FrameworkElement> parts = new List<FrameworkElement>();
        bool shown;

        const double CARD_W = 640;
        const double CARD_H = 640;
        const double CARD_R = 26;
        const double PAD_X = 26;
        const double PAD_Y = 22;
        const double ROW_H = 58;
        const double FEAT_H = 46;
        const double FEAT_GAP = 6;
        const double ROW_R = 16;
        const double SEG_R = 13;
        const double SEG_PAD = 4;
        const double CHIP_H = 30;
        const double CHIP_R = 10;
        const int GROW_MS = 230;
        /* The contents, a beat behind the card: long enough to read as a second
         * step, short enough that nothing is being waited for. */
        const int REVEAL_DELAY = 90;

        /* The one accent in the arrangement, and the mark's own blue on Linux. The
         * thing that opened this card and the thing that shows which way a switch is
         * set mean the same thing in both places. */
        static readonly Color BLUE = Color.FromRgb(0x0a, 0x84, 0xff);

        public SettingsCard(Action back)
        {
            Root = new Border();
            Root.Width = CARD_W; Root.Height = CARD_H;
            Root.CornerRadius = new CornerRadius(CARD_R);
            Root.Background = P.Br(P.C.BODY);
            Root.Visibility = Visibility.Collapsed;
            /* Grown about its own middle, so it opens out of the point the panels
             * collapsed into rather than out of its top-left corner. */
            Root.RenderTransformOrigin = new Point(0.5, 0.5);
            Root.RenderTransform = grow;
            /* A click on the card is not a click outside it - the launcher closes
             * the settings on any press that misses. */
            Root.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; };
            Root.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; };
            P.Restyle += delegate { Root.Background = P.Br(P.C.BODY); };

            StackPanel body = new StackPanel();
            body.Margin = new Thickness(PAD_X, PAD_Y, PAD_X, PAD_Y);
            Root.Child = body;

            var heading = new DockPanel();
            var backButton = Ui.IconButton("", back);
            backButton.ToolTip = "Back to launcher";
            backButton.Margin = new Thickness(0, 0, 12, 0);
            heading.Children.Add(backButton);
            heading.Children.Add(Head("SETTINGS", P.HEAD_H));
            body.Children.Add(heading);
            body.Children.Add(Row("Appearance",
                Segmented(new string[] { "Dark", "Light" }, new string[] { "dark", "light" },
                          Cfg.Theme, Cfg.SetTheme)));
            string[] keys = { "Windows / Super", "Backtick", "F8", "F9", "F10" };
            body.Children.Add(Row("Shortcut key", Segmented(keys, keys, Cfg.Trigger,
                delegate(string value) { App.ResetGesture(); Cfg.SetTrigger(value); })));
            var modelControls = new StackPanel { Orientation = Orientation.Horizontal };
            modelControls.Children.Add(ModelPicker());
            body.Children.Add(Row("Whisper model", modelControls));
            body.Children.Add(Head("FEATURES", FEAT_H - 16));

            /* The order here is the order they sit on screen: notes on the left, the
             * pill's own two in the middle, the to-do list on the right, and the two
             * that are gestures rather than panels at the end. */
            body.Children.Add(Feature("notes", "Notes", ""));
            body.Children.Add(Feature("tasks", "To-do list", ""));
            body.Children.Add(Feature("deck", "Copy deck", ""));


            body.Children.Add(Feature("voice", "Hold shortcut to dictate", ""));
        }

        FrameworkElement ModelPicker()
        {
            var menu = new ContextMenu();
            Border select = null;
            var label = Ui.Label("", 14, P.C.INK_BODY, false);
            Action refresh = delegate {
                label.Text = Path.GetFileNameWithoutExtension(Cfg.ModelPath()).Replace("ggml-", "");
                label.Foreground = P.Br(P.C.INK_BODY);
            };
            select = Ui.Button("Model", delegate {
                menu.Items.Clear();
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice");
                var paths = new List<string>();
                if (Directory.Exists(folder)) paths.AddRange(Directory.GetFiles(folder, "ggml-*.bin"));
                string current = Cfg.ModelPath();
                if (File.Exists(current) && !paths.Contains(current)) paths.Add(current);
                foreach (string path in paths) {
                    string model = path;
                    var choice = new MenuItem { Header = Path.GetFileNameWithoutExtension(model).Replace("ggml-", ""), IsChecked = model == current };
                    choice.Click += delegate { Cfg.SetModel(model); refresh(); };
                    menu.Items.Add(choice);
                }
                var browse = new MenuItem { Header = "Choose model file…" };
                browse.Click += delegate {
                    var picker = new Microsoft.Win32.OpenFileDialog { Filter = "Whisper GGML models (*.bin)|*.bin", Title = "Choose Whisper model" };
                    if (picker.ShowDialog() == true) { Cfg.SetModel(picker.FileName); refresh(); }
                };
                menu.Items.Add(browse);
                menu.PlacementTarget = select;
                menu.IsOpen = true;
            });
            var modelContent = new StackPanel { Orientation = Orientation.Horizontal };
            label.Margin = new Thickness(0, 0, 7, 0);
            modelContent.Children.Add(label);
            modelContent.Children.Add(Ui.Glyph("\uE70D", 12));
            select.Child = modelContent;
            P.Restyle += delegate { select.Child = modelContent; refresh(); };
            refresh();
            return select;
        }

        FrameworkElement Head(string text, double h)
        {
            Border b = new Border();
            b.Height = h;
            StackPanel cap = Ui.Tracked(text, P.FONT_HEAD, P.C.INK_DIM, 1.4, true);
            cap.VerticalAlignment = VerticalAlignment.Center;
            b.Child = cap;
            P.Restyle += delegate
            {
                StackPanel next = Ui.Tracked(text, P.FONT_HEAD, P.C.INK_DIM, 1.4, true);
                next.VerticalAlignment = VerticalAlignment.Center;
                b.Child = next;
            };
            parts.Add(b);
            return b;
        }

        Border Row(string title, FrameworkElement control)
        {
            Border row = new Border();
            row.Height = ROW_H;
            row.CornerRadius = new CornerRadius(ROW_R);
            row.Background = P.Br(P.C.CARD_BG);
            row.Padding = new Thickness(16, 0, 16, 0);
            row.Margin = new Thickness(0, P.HEAD_GAP, 0, 0);
            P.Restyle += delegate { row.Background = P.Br(P.C.CARD_BG); };

            Grid g = new Grid();
            ColumnDefinition c0 = new ColumnDefinition(); c0.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c1 = new ColumnDefinition(); c1.Width = GridLength.Auto;
            g.ColumnDefinitions.Add(c0); g.ColumnDefinitions.Add(c1);

            TextBlock label = Ui.Label(title, P.FONT_BODY, P.C.INK_BODY, false);
            label.VerticalAlignment = VerticalAlignment.Center;
            P.Restyle += delegate { label.Foreground = P.Br(P.C.INK_BODY); };
            Grid.SetColumn(label, 0);
            g.Children.Add(label);

            control.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(control, 1);
            g.Children.Add(control);

            row.Child = g;
            parts.Add(row);
            return row;
        }

        /* Two chips in a trough: the whole control says what the choice is between,
         * which a lone on/off switch does not. Both options stay readable and the one
         * in force is the blue one. */
        FrameworkElement Segmented(string[] labels, string[] values, Func<string> get, Action<string> set)
        {
            Border trough = new Border();
            trough.CornerRadius = new CornerRadius(SEG_R);
            trough.Padding = new Thickness(SEG_PAD);
            trough.Background = P.Br(P.C.CARD_BG_HOVER);

            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            trough.Child = row;

            List<Border> chips = new List<Border>();
            List<TextBlock> texts = new List<TextBlock>();
            Action paint = delegate
            {
                trough.Background = P.Br(P.C.CARD_BG_HOVER);
                string cur = get();
                for (int i = 0; i < chips.Count; i++)
                {
                    bool on = values[i] == cur;
                    chips[i].Background = P.Br(on ? BLUE
                        : (chips[i].IsMouseOver ? P.C.BTN_BG_HOVER : Colors.Transparent));
                    /* White on the blue in both themes: it is a saturated fill, and
                     * white is the only ink that reads on one. */
                    texts[i].Foreground = P.Br(on ? Colors.White : P.C.INK_DIM);
                }
            };

            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                Border chip = new Border();
                chip.Height = CHIP_H;
                chip.CornerRadius = new CornerRadius(CHIP_R);
                chip.Padding = new Thickness(16, 0, 16, 0);
                chip.Margin = new Thickness(i == 0 ? 0 : SEG_PAD, 0, 0, 0);
                chip.Cursor = Cursors.Hand;
                TextBlock t = Ui.Label(labels[i], P.FONT_BTN, P.C.INK_DIM, true);
                t.VerticalAlignment = VerticalAlignment.Center;
                chip.Child = t;
                /* Hover is a colour change and only a colour change - the chip does
                 * not move, grow or lift under the pointer. */
                chip.MouseEnter += delegate { paint(); };
                chip.MouseLeave += delegate { paint(); };
                chip.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    set(values[idx]);
                    paint();
                };
                chips.Add(chip); texts.Add(t);
                row.Children.Add(chip);
            }
            paint();
            P.Restyle += delegate { paint(); };
            return trough;
        }

        /* On or off, which is a different question from the segmented control above:
         * there is no second thing a feature being off could mean. The knob slides on
         * its own offset, snapped the first time and eased after - six knobs sliding
         * into place as the card opens would be six animations nobody asked for. */
        FrameworkElement Switch(Func<bool> get, Action<bool> set)
        {
            const double SW_W = 46, SW_H = 26, SW_PAD = 3, SW_MS = 160;
            double knobSize = SW_H - 2 * SW_PAD;

            Border track = new Border();
            track.Width = SW_W; track.Height = SW_H;
            track.CornerRadius = new CornerRadius(SW_H / 2);
            track.Cursor = Cursors.Hand;

            Canvas inner = new Canvas();
            track.Child = inner;
            Border knob = new Border();
            knob.Width = knobSize; knob.Height = knobSize;
            knob.CornerRadius = new CornerRadius(knobSize / 2);
            Canvas.SetTop(knob, SW_PAD - 0.5);
            inner.Children.Add(knob);

            bool first = true;
            Action paint = delegate
            {
                bool on = get();
                track.Background = P.Br(on ? BLUE : (track.IsMouseOver ? P.C.BTN_BG_HOVER : P.C.CARD_BG_HOVER));
                /* Off, the knob is the quiet ink so the whole control recedes rather
                 * than sitting there as a dot. */
                knob.Background = P.Br(on ? Colors.White : P.C.INK_DIM);
                double x = on ? SW_W - SW_PAD - knobSize - 1 : SW_PAD - 1;
                if (first)
                {
                    Canvas.SetLeft(knob, x);
                    first = false;
                    return;
                }
                knob.BeginAnimation(Canvas.LeftProperty, Ui.Ease(x, (int)SW_MS, true));
            };
            paint();
            P.Restyle += delegate { paint(); };
            track.MouseEnter += delegate { paint(); };
            track.MouseLeave += delegate { paint(); };
            track.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                set(!get());
                paint();
            };
            return track;
        }

        /* One feature, its mark, and its switch. The mark is the same one the feature
         * shows on screen, so the row is recognised before it is read. Two of them
         * are drawn rather than iconised, because there is no glyph for either. */
        Border Feature(string key, string label, string glyph)
        {
            Border row = new Border();
            row.Height = FEAT_H;
            row.CornerRadius = new CornerRadius(ROW_R);
            row.Background = P.Br(P.C.CARD_BG);
            row.Padding = new Thickness(14, 0, 14, 0);
            row.Margin = new Thickness(0, FEAT_GAP, 0, 0);
            P.Restyle += delegate { row.Background = P.Br(P.C.CARD_BG); };

            Grid g = new Grid();
            ColumnDefinition c0 = new ColumnDefinition(); c0.Width = GridLength.Auto;
            ColumnDefinition c1 = new ColumnDefinition(); c1.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c2 = new ColumnDefinition(); c2.Width = GridLength.Auto;
            g.ColumnDefinitions.Add(c0); g.ColumnDefinitions.Add(c1); g.ColumnDefinitions.Add(c2);

            FrameworkElement mark = Ui.Glyph(glyph, 18);
            mark.VerticalAlignment = VerticalAlignment.Center;
            mark.Margin = new Thickness(0, 0, 12, 0);
            Grid.SetColumn(mark, 0);
            g.Children.Add(mark);

            TextBlock text = Ui.Label(label, P.FONT_BTN, P.C.INK_BODY, false);
            text.VerticalAlignment = VerticalAlignment.Center;
            P.Restyle += delegate { text.Foreground = P.Br(P.C.INK_BODY); };
            Grid.SetColumn(text, 1);
            g.Children.Add(text);

            string k = key;
            FrameworkElement sw = Switch(delegate { return Cfg.Feature(k); },
                                         delegate(bool on) { Cfg.SetFeature(k, on); });
            sw.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(sw, 2);
            g.Children.Add(sw);

            row.Child = g;
            parts.Add(row);
            return row;
        }

        /* Where the card's middle sits, in the stage's coordinates. */
        public void Place(double cx, double cy)
        {
            Canvas.SetLeft(Root, Math.Round(cx - CARD_W / 2));
            Canvas.SetTop(Root, Math.Round(cy - CARD_H / 2));
        }

        public bool Shown { get { return shown; } }

        public void Open()
        {
            shown = true;
            Root.Visibility = Visibility.Visible;
            Root.BeginAnimation(UIElement.OpacityProperty, null);
            Root.Opacity = 1;
            grow.ScaleX = grow.ScaleY = 0.12;
            grow.BeginAnimation(ScaleTransform.ScaleXProperty, Ui.Back(1, GROW_MS));
            grow.BeginAnimation(ScaleTransform.ScaleYProperty, Ui.Back(1, GROW_MS));
            /* Started with the card rather than after it: the stagger's own delay is
             * what puts the contents behind an edge that is still arriving. */
            Ui.Reveal(parts, REVEAL_DELAY);
        }

        public void Close(Action done)
        {
            if (!shown) { if (done != null) done(); return; }
            shown = false;
            grow.BeginAnimation(ScaleTransform.ScaleXProperty, Ui.Ease(0.12, GROW_MS, false));
            grow.BeginAnimation(ScaleTransform.ScaleYProperty, Ui.Ease(0.12, GROW_MS, false));
            Root.BeginAnimation(UIElement.OpacityProperty, Ui.Ease(0, GROW_MS, false));
            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromMilliseconds(GROW_MS + 20);
            t.Tick += delegate
            {
                t.Stop();
                Reset();
                if (done != null) done();
            };
            t.Start();
        }

        /* Straight back to nothing, with nothing left running - what closing the
         * launcher outright while the settings are up has to do, so the next Win
         * press does not find a quarter-sized card. */
        public void Reset()
        {
            shown = false;
            grow.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            grow.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            grow.ScaleX = grow.ScaleY = 1;
            Root.BeginAnimation(UIElement.OpacityProperty, null);
            Root.Opacity = 1;
            Root.Visibility = Visibility.Collapsed;
            foreach (FrameworkElement el in parts)
            {
                el.BeginAnimation(UIElement.OpacityProperty, null);
                el.Opacity = 1;
                TranslateTransform t = el.RenderTransform as TranslateTransform;
                if (t != null)
                {
                    t.BeginAnimation(TranslateTransform.YProperty, null);
                    t.Y = 0;
                }
            }
        }
    }

    /* ------------------------------------------------------------ the launcher
     *
     * A single island pill in the middle of the screen: it grows out of a circle,
     * takes a query, and drops the matching applications into a second island
     * underneath it. Everything is a child of a fixed-layout canvas, so position
     * and size are real properties and the animation can drive them. Easing x and
     * width on the same curve keeps the body centred throughout - x = (W - w) / 2
     * holds for any easing as long as both ends are interpolated by the same
     * value. */
    public class Launcher : Window
    {
        Canvas stage;
        Border pill, res;
        Grid bar;
        TextBox entry;
        TextBlock hint;
        Border crumb;
        TextBlock crumbLabel;
        Canvas rowHost;
        Border sel;
        DeckPanel deckPanel;
        NotesPanel notesPanel;
        TodoPanel todoPanel;
        /* Everything the settings card replaces, in one canvas, so gathering it is
         * one transform about one point rather than four that have to agree. */
        Canvas spread;
        SettingsCard settings;
        Border deckChevron; TextBlock deckChevronText;
        readonly ScaleTransform gather = new ScaleTransform(1, 1);

        double SW, SH, cx, pillTop, slotTop, fit = 1;
        DispatcherTimer closeTimer;
        List<Entry> hits = new List<Entry>();
        List<Entry> level;                     // the folder being browsed
        List<string> levelPaths = new List<string>();
        List<string> levelNames = new List<string>();
        int index = -1, scroll;
        bool open;

        public Launcher()
        {
            Title = "Essential Desktop";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = true;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;

            DR.Rectangle b = WF.Screen.PrimaryScreen.WorkingArea;
            fit = Math.Min(1, Math.Min((b.Width - 32.0) / (P.W + 2 * (P.PANEL_W + P.PANEL_GAP)), (b.Height - 32.0) / P.PANEL_H));
            SW = b.Width / fit; SH = b.Height / fit;
            Left = b.Left; Top = b.Top;

            stage = new Canvas();
            Content = stage;

            Loaded += delegate
            {
                /* The design is in physical pixels on a 1920 screen. Undo whatever
                 * scaling the display is doing, so 720 means 720 dots and not 720
                 * device-independent units that land at 1080 on a 150% panel. */
                PresentationSource ps = PresentationSource.FromVisual(this);
                double sx = 1, sy = 1;
                if (ps != null && ps.CompositionTarget != null)
                {
                    Matrix m = ps.CompositionTarget.TransformToDevice;
                    if (m.M11 > 0) sx = m.M11;
                    if (m.M22 > 0) sy = m.M22;
                }
                Width = SW * fit / sx;
                Height = SH * fit / sy;
                stage.LayoutTransform = new ScaleTransform(fit / sx, fit / sy);
                stage.Width = SW;
                stage.Height = SH;
            };

            cx = SW / 2;
            pillTop = Math.Round(Math.Max(16 / fit, Math.Min(SH * P.Y_F - P.H / 2, SH - P.PANEL_H - 16 / fit)));
            slotTop = pillTop + P.H + P.GAP;

            spread = new Canvas();
            spread.Width = SW; spread.Height = SH;
            gather.CenterX = cx;
            gather.CenterY = pillTop + P.PANEL_H / 2;
            spread.RenderTransform = gather;
            stage.Children.Add(spread);

            BuildPanels();
            BuildPill();
            BuildResults();

            settings = new SettingsCard(CloseSettings);
            settings.Place(cx, Math.Max(336, Math.Min(SH - 336, pillTop + P.PANEL_H / 2)));
            stage.Children.Add(settings.Root);
            Cfg.FeatureChanged += ApplyFeatures;
            Idx.Updated += delegate { if (open) OnQuery(); };

            PreviewKeyDown += OnKey;
            /* A fully transparent area does not hit-test, so a click outside lands
             * on whatever is behind and takes the activation with it - which is
             * the close, and why there is no backdrop handler here. */
            Deactivated += delegate { if (open && !reviewing) Close_(); };
        }

        void BuildPanels()
        {
            /* Notes to the left of the pill, the to-do list to the right, mirrored
             * about it. Their height runs from the top of the search bar down to
             * where a full copy deck ends - fixed rather than tracking the deck,
             * because a panel whose bottom edge moved every time you copied
             * something would be the least restful thing on the screen. */
            notesPanel = new NotesPanel();
            Canvas.SetLeft(notesPanel.Root, cx - P.W / 2 - P.PANEL_GAP - P.PANEL_W);
            Canvas.SetTop(notesPanel.Root, pillTop);
            spread.Children.Add(notesPanel.Root);

            todoPanel = new TodoPanel();
            Canvas.SetLeft(todoPanel.Root, cx + P.W / 2 + P.PANEL_GAP);
            Canvas.SetTop(todoPanel.Root, pillTop);
            spread.Children.Add(todoPanel.Root);

            deckPanel = new DeckPanel();
            deckPanel.Root.Height = 0;
            deckPanel.Root.Opacity = 0;
            Canvas.SetLeft(deckPanel.Root, cx - P.DECK_W / 2);
            Canvas.SetTop(deckPanel.Root, slotTop);
            spread.Children.Add(deckPanel.Root);
        }

        void BuildPill()
        {
            pill = new Border();
            pill.Height = P.H;
            pill.Width = P.H;
            pill.CornerRadius = new CornerRadius(P.R_FROM * P.H);
            pill.Background = P.Br(P.C.BODY);
            pill.ClipToBounds = true;
            P.Restyle += delegate { pill.Background = P.Br(P.C.BODY); };
            Canvas.SetTop(pill, pillTop);
            Canvas.SetLeft(pill, cx - P.H / 2);
            spread.Children.Add(pill);

            bar = new Grid();
            bar.Width = P.W - 48;
            bar.Height = P.H;
            bar.Margin = new Thickness(24, 0, 24, 0);
            /* Anchored to the pill's left edge: the field is where it will end up
             * from the first frame, and the fade above covers the reveal. */
            bar.HorizontalAlignment = HorizontalAlignment.Left;
            ColumnDefinition a = new ColumnDefinition(); a.Width = GridLength.Auto;
            ColumnDefinition c = new ColumnDefinition(); c.Width = GridLength.Auto;
            ColumnDefinition d = new ColumnDefinition(); d.Width = new GridLength(1, GridUnitType.Star);
            bar.ColumnDefinitions.Add(a);
            bar.ColumnDefinitions.Add(c);
            bar.ColumnDefinitions.Add(d);
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
            deckChevron=Ui.IconButton("", delegate { Cfg.SetDeckCollapsed(!Cfg.DeckCollapsed()); Sync(); });
            var deckControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var deckLabel = Ui.Label("Copy deck", 13, P.C.INK_BODY, false);
            deckLabel.Margin = new Thickness(0, 0, 7, 0);
            deckChevronText = Ui.Glyph("", 12);
            deckControls.Children.Add(deckLabel);
            deckControls.Children.Add(deckChevronText);
            deckChevron.Width = Double.NaN;
            deckChevron.Padding = new Thickness(10, 0, 10, 0);
            deckChevron.Child = deckControls;
            P.Restyle += delegate { deckLabel.Foreground = P.Br(P.C.INK_BODY); };
            deckChevron.Margin=new Thickness(8,0,0,0);
            deckChevron.VerticalAlignment=VerticalAlignment.Center;
            Grid.SetColumn(deckChevron,3);bar.Children.Add(deckChevron);
            P.Restyle+=delegate{deckChevronText.Foreground=P.Br(P.C.INK_BODY);};

            var settingsButton = Ui.IconButton("", ToggleSettings);
            settingsButton.ToolTip = "Settings";
            settingsButton.Margin = new Thickness(0, 0, 12, 0);
            Grid.SetColumn(settingsButton, 0);
            bar.Children.Add(settingsButton);

            /* The folder being browsed, as a chip in front of the query - and the
             * way back out for the mouse, since Left only helps if your hands are
             * already on the keyboard. */
            crumb = new Border();
            crumb.CornerRadius = new CornerRadius(9);
            crumb.Background = P.Br(P.C.BTN_BG);
            crumb.Padding = new Thickness(10, 4, 10, 4);
            crumb.Margin = new Thickness(0, 0, 13, 0);
            crumb.VerticalAlignment = VerticalAlignment.Center;
            crumb.Visibility = Visibility.Collapsed;
            crumb.Cursor = Cursors.Hand;
            crumbLabel = Ui.Label("", 13.5, P.C.INK_BODY, false);
            crumb.Child = crumbLabel;
            crumb.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Leave();
            };
            P.Restyle += delegate
            {
                crumb.Background = P.Br(P.C.BTN_BG);
                crumbLabel.Foreground = P.Br(P.C.INK_BODY);
            };
            Grid.SetColumn(crumb, 1);
            bar.Children.Add(crumb);

            Grid field = new Grid();
            Grid.SetColumn(field, 2);
            entry = Ui.Field(P.FONT_BODY, "Search applications", out hint);
            field.Children.Add(hint);
            field.Children.Add(entry);
            bar.Children.Add(field);
            entry.TextChanged += delegate { OnQuery(); };
            P.Restyle += delegate
            {
                entry.Foreground = P.Br(P.C.INK_BODY);
                entry.CaretBrush = P.Br(P.C.CARET);
                hint.Foreground = P.Br(P.C.INK_DIM);
            };
            pill.Child = bar;

            /* The corner relaxes from a circle to the alt-tab body's own radius as
             * the width grows, and the contents fade in over the back half of it -
             * one number, read off the width it is tied to rather than run on a
             * timeline of its own that could drift from it. */
            pill.SizeChanged += delegate
            {
                double t = (pill.Width - P.H) / (P.W - P.H);
                if (double.IsNaN(t)) t = 1;
                t = Math.Max(0, Math.Min(1, t));
                pill.CornerRadius = new CornerRadius((P.R_FROM + (P.R_TO - P.R_FROM) * t) * P.H);
                bar.Opacity = Math.Max(0, (t - 0.45) / 0.55);
            };
        }

        void BuildResults()
        {
            res = new Border();
            res.Width = P.W;
            res.Height = 0;
            res.CornerRadius = new CornerRadius(P.RES_R);
            res.Background = P.Br(P.C.BODY);
            res.ClipToBounds = true;
            res.Opacity = 0;
            P.Restyle += delegate { res.Background = P.Br(P.C.BODY); };
            Canvas.SetLeft(res, cx - P.W / 2);
            Canvas.SetTop(res, slotTop);
            spread.Children.Add(res);

            Canvas holder = new Canvas();
            res.Child = holder;

            /* One actor that slides between rows rather than a highlight per row:
             * two of those can only ever cut from one box to the next. */
            sel = new Border();
            sel.Width = P.W - 2 * P.SIDE;
            sel.Height = P.ROW_H;
            sel.CornerRadius = new CornerRadius(P.SEL_R);
            sel.Background = P.Br(P.C.SEL);
            sel.Visibility = Visibility.Collapsed;
            P.Restyle += delegate { sel.Background = P.Br(P.C.SEL); };
            Canvas.SetLeft(sel, P.SIDE);
            holder.Children.Add(sel);

            rowHost = new Canvas();
            Canvas.SetLeft(rowHost, 0);
            Canvas.SetTop(rowHost, P.PAD);
            holder.Children.Add(rowHost);
        }

        /* ------------------------------------------------------------ matching */

        void OnQuery()
        {
            hint.Visibility = entry.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            Search();
            Render();
            Sync();
        }

        void Search()
        {
            string q = entry.Text.Trim().ToLowerInvariant();
            hits = new List<Entry>();

            if (level != null)
            {
                List<Entry> got = Idx.Filter(level, q);
                hits = got.Count > 60 ? got.GetRange(0, 60) : new List<Entry>(got);
                return;
            }
            if (q.Length == 0) return;

            List<Entry> keep = new List<Entry>();
            List<int> score = new List<int>();

            foreach (Entry e in Idx.Apps())
            {
                int s = Idx.Score(e, q);
                if (s >= 0) { keep.Add(e); score.Add(s); }
            }

            /* Files never take the whole list - there is always room left for
             * applications, which are what the launcher is mostly for. The penalty
             * is enough to lose a tie against an application of equal match
             * quality, and not enough to cross a band. */
            List<Entry> fileHits = new List<Entry>();
            List<int> fileScore = new List<int>();
            foreach (Entry e in Idx.Files())
            {
                int s = Idx.Score(e, q);
                if (s >= 0) { fileHits.Add(e); fileScore.Add(s - P.FILE_PENALTY); }
            }
            Idx.Sort(fileHits, fileScore);
            int take = Math.Min(P.MAX_FILES, fileHits.Count);
            for (int i = 0; i < take; i++) { keep.Add(fileHits[i]); score.Add(fileScore[i]); }

            Idx.Sort(keep, score);
            hits = keep.Count > 60 ? keep.GetRange(0, 60) : keep;
        }

        /* -------------------------------------------------------------- rows */

        void Render()
        {
            rowHost.Children.Clear();
            for (int i = 0; i < hits.Count; i++)
            {
                FrameworkElement row = Row(hits[i], i);
                Canvas.SetTop(row, i * P.ROW_H);
                rowHost.Children.Add(row);
            }
            index = hits.Count > 0 ? 0 : -1;
            scroll = 0;
            rowHost.BeginAnimation(Canvas.TopProperty, null);
            Canvas.SetTop(rowHost, P.PAD);
            SyncSel(false);
        }

        FrameworkElement Row(Entry e, int i)
        {
            Grid row = new Grid();
            row.Width = P.W;
            row.Height = P.ROW_H;
            row.Background = Brushes.Transparent;
            row.Cursor = Cursors.Hand;

            StackPanel line = new StackPanel();
            line.Orientation = Orientation.Horizontal;
            line.VerticalAlignment = VerticalAlignment.Center;
            line.Margin = new Thickness(22, 0, 22, 0);

            Image icon = new Image();
            icon.Width = 30; icon.Height = 30;
            icon.Source = Idx.Icon(e);
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.Margin = new Thickness(0, 0, 14, 0);
            line.Children.Add(icon);

            TextBlock label = Ui.Label(e.Display, 16, P.C.INK_LOUD, false);
            label.VerticalAlignment = VerticalAlignment.Center;
            label.MaxWidth = P.W - 44 - 44 - 190;
            line.Children.Add(label);
            row.Children.Add(line);

            StackPanel right = new StackPanel();
            right.Orientation = Orientation.Horizontal;
            right.HorizontalAlignment = HorizontalAlignment.Right;
            right.VerticalAlignment = VerticalAlignment.Center;
            right.Margin = new Thickness(0, 0, 22, 0);

            /* Which of the five places it came from - "report.pdf" on its own is
             * not enough to tell two of them apart. Inside a folder every row has
             * the same answer, so it is dropped. */
            if (e.IsFile && level == null && !string.IsNullOrEmpty(e.Where))
            {
                TextBlock where = Ui.Label(e.Where, 13, P.C.INK_DIM, false);
                where.VerticalAlignment = VerticalAlignment.Center;
                where.Margin = new Thickness(0, 0, 12, 0);
                right.Children.Add(where);
            }

            /* The step-in button: the same move as Right, spelled out for the
             * mouse, and it swallows its own click so the row underneath does not
             * also launch the thing. */
            if (e.IsDir)
            {
                int n1 = i;
                Border chev = Ui.IconButton("\uE76C", delegate { Select(n1); Enter(n1); });
                chev.ToolTip = "Open folder";
                right.Children.Add(chev);
            }
            row.Children.Add(right);

            int n = i;
            row.MouseEnter += delegate { Select(n); };
            row.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs ev)
            {
                ev.Handled = true;
                Launch(n);
            };
            return row;
        }

        void Select(int i)
        {
            if (i < 0 || i >= hits.Count) return;
            index = i;
            if (index < scroll) scroll = index;
            if (index > scroll + P.MAX_ROWS - 1) scroll = index - P.MAX_ROWS + 1;
            SyncSel(true);
        }

        void SyncSel(bool animate)
        {
            if (index < 0 || hits.Count == 0)
            {
                sel.Visibility = Visibility.Collapsed;
                return;
            }
            sel.Visibility = Visibility.Visible;
            double y = P.PAD + (index - scroll) * P.ROW_H;
            double listY = P.PAD - scroll * P.ROW_H;
            if (animate)
            {
                sel.BeginAnimation(Canvas.TopProperty, Ui.Ease(y, P.MOVE_MS, true));
                rowHost.BeginAnimation(Canvas.TopProperty, Ui.Ease(listY, P.MOVE_MS, true));
            }
            else
            {
                sel.BeginAnimation(Canvas.TopProperty, null);
                Canvas.SetTop(sel, y);
                Canvas.SetTop(rowHost, listY);
            }
        }

        /* ---------------------------------------------------------- the slots
         *
         * Two things live under the pill, in the same slot and never at once: the
         * results island, and the copy deck that takes its place while the query
         * is empty. One shrinks to nothing as the other grows out of the edge. */
        void Sync()
        {
            int n = hits.Count;
            double resH = n > 0 ? 2 * P.PAD + Math.Min(n, P.MAX_ROWS) * P.ROW_H : 0;

            res.BeginAnimation(HeightProperty, Ui.Ease(resH, P.GROW_MS, true));
            res.BeginAnimation(OpacityProperty, Ui.Ease(n > 0 ? 1 : 0, P.GROW_MS, true));

            deckChevron.Visibility=Cfg.Feature("deck")?Visibility.Visible:Visibility.Collapsed;
            deckChevronText.Text=Cfg.DeckCollapsed()?"\uE70D":"\uE70E";
            deckChevron.ToolTip=Cfg.DeckCollapsed()?"Expand copy deck":"Minimize copy deck";
            bool deckOn = !Cfg.DeckCollapsed() && n == 0 && level == null && Cfg.Feature("deck") &&
                          entry.Text.Trim().Length == 0;
            if (deckOn) deckPanel.Rebuild(); else deckPanel.Unselect();

            DoubleAnimation grow = Ui.Ease(deckOn ? deckPanel.Height() : 0, P.GROW_MS, true);
            /* Let the pill land first when it is arriving from nothing. */
            if (deckOn && deckPanel.Root.Height == 0)
                grow.BeginTime = TimeSpan.FromMilliseconds(P.DECK_DELAY_MS);
            deckPanel.Root.BeginAnimation(HeightProperty, grow);
            deckPanel.Root.BeginAnimation(UIElement.OpacityProperty, Ui.Ease(deckOn ? 1 : 0, P.GROW_MS, true));
        }

        /* ------------------------------------------------------- walking in
         *
         * A file row can be walked into with Right, which turns the results island
         * into a folder listing, and Left walks back out. */
        bool CanEnter(int i) { return i >= 0 && i < hits.Count && hits[i].IsDir; }

        void Enter(int i)
        {
            if (!CanEnter(i)) return;
            Entry e = hits[i];
            levelPaths.Add(e.Path);
            levelNames.Add(e.Display);
            level = Idx.Browse(e.Path);
            entry.Text = "";
            hint.Text = "Search in " + e.Display;
            ShowCrumb();
            OnQuery();
        }

        void Leave()
        {
            if (levelPaths.Count == 0) return;
            levelPaths.RemoveAt(levelPaths.Count - 1);
            levelNames.RemoveAt(levelNames.Count - 1);
            if (levelPaths.Count == 0)
            {
                level = null;
                hint.Text = "Search applications";
            }
            else
            {
                level = Idx.Browse(levelPaths[levelPaths.Count - 1]);
                hint.Text = "Search in " + levelNames[levelNames.Count - 1];
            }
            entry.Text = "";
            ShowCrumb();
            OnQuery();
        }

        void ShowCrumb()
        {
            if (levelNames.Count == 0)
            {
                crumb.Visibility = Visibility.Collapsed;
                return;
            }
            crumbLabel.Text = levelNames[levelNames.Count - 1];
            crumb.Visibility = Visibility.Visible;
        }

        void Launch(int i)
        {
            if (i < 0 || i >= hits.Count) return;
            Entry e = hits[i];
            Close_();
            Shell.Open(e.Path);
        }

        /* ------------------------------------------------------------ the keys
         *
         * Arrow keys are also how you move a text caret, so they only mean
         * navigation at the ends of the query - Right at the end of the text,
         * Left at the start - which is the same rule a column-view file manager
         * uses. */
        void OnKey(object sender, KeyEventArgs e)
        {
            /* A note being edited and the add-a-task bar are text fields of their
             * own: Enter and the arrows belong to them while they hold focus, and
             * Esc hands the query field back rather than closing the launcher. */
            if (!entry.IsFocused)
            {
                if (e.Key == Key.Escape)
                {
                    entry.Focus();
                    Keyboard.Focus(entry);
                    e.Handled = true;
                }
                return;
            }

            /* Escape puts the settings away before it closes the launcher: the
             * card is the thing in front, so it is the thing Escape answers. */
            if (e.Key == Key.Escape && settings.Shown) { CloseSettings(); e.Handled = true; return; }
            if (e.Key == Key.Escape) { Close_(); e.Handled = true; return; }
            if (e.Key == Key.Enter) { Launch(index); e.Handled = true; return; }
            if (e.Key == Key.Down) { Select(index + 1); e.Handled = true; return; }
            if (e.Key == Key.Up) { Select(index - 1); e.Handled = true; return; }
            if (e.Key == Key.Right && entry.CaretIndex == entry.Text.Length && CanEnter(index))
            {
                Enter(index); e.Handled = true; return;
            }
            if (e.Key == Key.Left && entry.CaretIndex == 0 && levelPaths.Count > 0)
            {
                Leave(); e.Handled = true; return;
            }
        }

        /* -------------------------------------------------------- open, close */

        bool reviewing;
        public void Review()
        {
            reviewing = true;
            string folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "review");
            Directory.CreateDirectory(folder);
            var log = new StringBuilder();
            var intervals = new List<double>();
            double centerError = 0;
            TimeSpan last = TimeSpan.Zero;
            EventHandler frame = delegate(object sender, EventArgs args)
            {
                var now = ((RenderingEventArgs)args).RenderingTime;
                if (last != TimeSpan.Zero && now != last) intervals.Add((now-last).TotalMilliseconds);
                last = now;
                if (open && !settings.Shown) centerError = Math.Max(centerError, Math.Abs(Canvas.GetLeft(pill) + pill.ActualWidth / 2 - cx));
            };
            CompositionTarget.Rendering += frame;
            int step = 0;
            var timer = new DispatcherTimer(); timer.Interval = TimeSpan.FromMilliseconds(700);
            timer.Tick += delegate
            {
                log.AppendLine("Step " + step + " open=" + open + " size=" + Width + "x" + Height + " fit=" + fit + " pill=" + pill.ActualWidth);
                if (step == 0 || step == 3 || step == 5 || step == 7)
                {
                    var bitmap = new RenderTargetBitmap((int)Math.Ceiling(ActualWidth), (int)Math.Ceiling(ActualHeight), 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(this);
                    var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var stream = File.Create(System.IO.Path.Combine(folder, "state-"+step+".png"))) png.Save(stream);
                }
                if (step == 0) Close_();
                if (step == 1) { intervals.Clear(); last = TimeSpan.Zero; Open_(); }
                if (step == 2) entry.Text = "note";
                if (step == 3) { entry.Text = ""; OpenSettings(); }
                if (step == 5) { CloseSettings(); P.SetPalette(P.C.NAME == "dark" ? "light" : "dark"); }
                if (step == 7) P.SetPalette(P.C.NAME == "dark" ? "light" : "dark");
                if (step == 8)
                {
                    timer.Stop(); CompositionTarget.Rendering -= frame; reviewing = false;
                    intervals.Sort();
                    log.AppendLine(notesPanel.AlignmentReport());
                    log.AppendLine("Center error (design pixels): " + centerError);
                    log.AppendLine("Render intervals count="+intervals.Count+" median="+(intervals.Count>0 ? intervals[intervals.Count/2] : 0)+" p95="+(intervals.Count>0 ? intervals[(int)(intervals.Count*0.95)] : 0));
                    log.AppendLine("Bounds left="+(cx-P.W/2-P.PANEL_GAP-P.PANEL_W)+" bottom="+(pillTop+P.PANEL_H)+" available="+SW+"x"+SH);
                    File.WriteAllText(System.IO.Path.Combine(folder,"review.txt"),log.ToString()); Close_();
                }
                step++;
            };
            timer.Start();
        }
        public bool IsOpen { get { return open; } }

        public void Open_()
        {
            if (open) { Activate(); Foreground_(); return; }
            if (closeTimer != null) { closeTimer.Stop(); closeTimer = null; }
            open = true;

            level = null;
            levelPaths.Clear();
            levelNames.Clear();
            crumb.Visibility = Visibility.Collapsed;
            hint.Text = "Search applications";
            hint.Visibility = Visibility.Visible;
            entry.Text = "";
            hits.Clear();
            Render();

            ResetSettings();
            ApplyFeatures();
            notesPanel.Rebuild();
            todoPanel.Rebuild();
            /* Every appearance gets the whole spin curve from the top. */

            Show();
            Activate();
            Foreground_();

            /* The pill grows out of a circle. x and width ease on the same curve,
             * so the body is centred at every frame of it. */
            pill.BeginAnimation(Canvas.LeftProperty, null);
            pill.BeginAnimation(WidthProperty, null);
            pill.Width = P.H;
            Canvas.SetLeft(pill, cx - P.H / 2);
            pill.BeginAnimation(WidthProperty, Ui.Ease(P.W, P.IN_MS, true));
            pill.BeginAnimation(Canvas.LeftProperty, Ui.Ease(cx - P.W / 2, P.IN_MS, true));

            /* The panels unfold a beat after the pill has landed - three steps
             * rather than one, so the eye is given an order to read them in. */
            Unfold(notesPanel.Root, cx - P.W / 2 - P.PANEL_GAP - P.PANEL_W, -P.PANEL_SLIDE);
            Unfold(todoPanel.Root, cx + P.W / 2 + P.PANEL_GAP, P.PANEL_SLIDE);

            Sync();
            entry.Focus();
            Keyboard.Focus(entry);
        }

        void Unfold(Border panel, double home, double slide)
        {
            panel.BeginAnimation(Canvas.LeftProperty, null);
            Canvas.SetLeft(panel, home + slide);
            panel.BeginAnimation(UIElement.OpacityProperty, null);
            panel.Opacity = 0;

            DoubleAnimation move = Ui.Ease(home, P.MOVE_MS, true);
            DoubleAnimation fade = Ui.Ease(1, P.MOVE_MS, true);
            move.BeginTime = TimeSpan.FromMilliseconds(90);
            fade.BeginTime = TimeSpan.FromMilliseconds(90);
            panel.BeginAnimation(Canvas.LeftProperty, move);
            panel.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        public void Close_()
        {
            if (!open) return;
            open = false;
            /* A 60Hz repaint of something nobody can see is the easiest performance
             * bug here to write by accident. */
            ResetSettings();
            /* Whatever is on the sheet is kept, and nothing is left mid-drag. */
            notesPanel.Park();
            todoPanel.Park();

            pill.BeginAnimation(WidthProperty, Ui.Ease(P.H, P.OUT_MS, false));
            pill.BeginAnimation(Canvas.LeftProperty, Ui.Ease(cx - P.H / 2, P.OUT_MS, false));
            res.BeginAnimation(HeightProperty, Ui.Ease(0, P.OUT_MS, false));
            res.BeginAnimation(OpacityProperty, Ui.Ease(0, P.OUT_MS, false));
            deckPanel.Root.BeginAnimation(HeightProperty, Ui.Ease(0, P.OUT_MS, false));
            deckPanel.Root.BeginAnimation(UIElement.OpacityProperty, Ui.Ease(0, P.OUT_MS, false));
            notesPanel.Root.BeginAnimation(UIElement.OpacityProperty, Ui.Ease(0, P.OUT_MS, false));
            todoPanel.Root.BeginAnimation(UIElement.OpacityProperty, Ui.Ease(0, P.OUT_MS, false));

            closeTimer = new DispatcherTimer();
            closeTimer.Interval = TimeSpan.FromMilliseconds(P.OUT_MS + 30);
            closeTimer.Tick += delegate { closeTimer.Stop(); if (!open) Hide(); };
            closeTimer.Start();
        }

        /* ---------------------------------------------------- the settings card
         *
         * The arrangement gathers into the middle of where it was and the card comes
         * up out of the point it went into - two movements, not one, because that is
         * what says the panels were folded up and put away rather than repainted.
         * Linux: settings.js and launcher.js's _toggleSettings. */
        void ToggleSettings()
        {
            if (settings.Shown) CloseSettings();
            else OpenSettings();
        }

        void OpenSettings()
        {
            if (settings.Shown) return;
            deckPanel.Unselect();
            gather.BeginAnimation(ScaleTransform.ScaleXProperty, Ui.Ease(0.12, 230, false));
            gather.BeginAnimation(ScaleTransform.ScaleYProperty, Ui.Ease(0.12, 230, false));
            spread.BeginAnimation(OpacityProperty, Ui.Ease(0, 230, false));
            settings.Open();
        }

        void CloseSettings()
        {
            if (!settings.Shown) return;
            settings.Close(delegate
            {
                gather.BeginAnimation(ScaleTransform.ScaleXProperty, Ui.Ease(1, 230, true));
                gather.BeginAnimation(ScaleTransform.ScaleYProperty, Ui.Ease(1, 230, true));
                spread.BeginAnimation(OpacityProperty, Ui.Ease(1, 230, true));
                entry.Focus();
                Keyboard.Focus(entry);
            });
        }

        /* Back to no card and a full-size arrangement, with nothing left running. */
        void ResetSettings()
        {
            settings.Reset();
            gather.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            gather.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            gather.ScaleX = gather.ScaleY = 1;
            spread.BeginAnimation(OpacityProperty, null);
            spread.Opacity = 1;
        }

        /* What the six switches turn off is turned off by not being shown, never by
         * not being built: a panel that was never constructed cannot be switched back
         * on without rebuilding the launcher, and the stores behind the panels still
         * have to be flushed whether or not their panel is on screen - a note typed
         * before the switch was thrown is still a note. */
        void ApplyFeatures()
        {
            notesPanel.Root.Visibility = Cfg.Feature("notes") ? Visibility.Visible : Visibility.Collapsed;
            todoPanel.Root.Visibility = Cfg.Feature("tasks") ? Visibility.Visible : Visibility.Collapsed;
            if (!Cfg.Feature("deck"))
            {
                deckPanel.Unselect();
                deckPanel.Root.Height = 0;
                deckPanel.Root.Opacity = 0;
            }
            if (open) Sync();
        }

        /* Windows only hands the foreground to a process that already owns input,
         * so borrow the current owner's input queue for the handover. */
        void Foreground_()
        {
            IntPtr me = new WindowInteropHelper(this).Handle;
            IntPtr fg = N.GetForegroundWindow();
            uint other = N.GetWindowThreadProcessId(fg, IntPtr.Zero);
            uint mine = N.GetCurrentThreadId();
            bool attached = false;
            if (other != 0 && other != mine) attached = N.AttachThreadInput(other, mine, true);
            N.SetForegroundWindow(me);
            N.BringWindowToTop(me);
            if (attached) N.AttachThreadInput(other, mine, false);
        }
    }

    /* ------------------------------------------------------------------- app */
    public static class App
    {
        public static string SelfPath = "";

        static Application app;
        static Dispatcher disp;
        static bool paused, keyDown, usedChord, holding;
        static int pressedKey;
        static IntPtr hook;
        static N.HookProc hookRef;
        static VoiceController voice;
        static DispatcherTimer holdTimer;
        static readonly HashSet<int> eaten=new HashSet<int>();
        static WF.NotifyIcon tray;
        static Launcher launcher;
        static HwndSource msgWin;
        static DateTime echoAt=DateTime.MinValue;
        static bool Trigger(int vk) {
            string key=Cfg.Trigger();
            return key=="Windows / Super" ? vk==Kb.LWIN||vk==Kb.RWIN : vk==(key=="Backtick"?0xc0:key=="F8"?0x77:key=="F9"?0x78:0x79);
        }
        static void InstallHook() {
            if(hook!=IntPtr.Zero||paused)return;
            hookRef=Proc;hook=N.SetWindowsHookEx(13,hookRef,N.GetModuleHandle(null),0);
            if(hook==IntPtr.Zero)Notify("Could not install the shortcut. Use the tray icon.");
        }
        public static void ResetGesture() {
            if(holdTimer!=null)holdTimer.Stop();
            keyDown=false;holding=false;usedChord=false;eaten.Clear();
            if(voice!=null)voice.Cancel();
        }
        static void RemoveHook(){if(hook!=IntPtr.Zero)N.UnhookWindowsHookEx(hook);hook=IntPtr.Zero;ResetGesture();}
        static void BeginPress() {
            if(holdTimer==null) {holdTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(260)};holdTimer.Tick+=delegate {holdTimer.Stop();if(keyDown&&!usedChord&&Cfg.Feature("voice")){holding=true;voice.Begin();}};}
            holdTimer.Stop();holdTimer.Start();
        }
        static IntPtr Proc(int code,IntPtr w,IntPtr l) {
            if(code>=0&&!paused) {
                var k=(N.KBDLL)Marshal.PtrToStructure(l,typeof(N.KBDLL));
                if((k.flags&0x10)==0) {
                    int vk=(int)k.vkCode,msg=(int)w;bool down=msg==0x100||msg==0x104,up=msg==0x101||msg==0x105;
                    if(vk==0x1b&&down&&voice.Active){ResetGesture();return (IntPtr)1;}
                    if((keyDown&&vk==pressedKey)||(down&&!keyDown&&Trigger(vk)&&!Kb.IsDown(Kb.CTRL)&&!Kb.IsDown(Kb.ALT)&&!Kb.IsDown(Kb.SHIFT))) {
                        if(down&&!keyDown){keyDown=true;pressedKey=vk;usedChord=false;holding=false;BeginPress();}
                        if(up&&keyDown){keyDown=false;if(holdTimer!=null)holdTimer.Stop();if(holding){holding=false;Post(voice.End);}else if(!usedChord)Post(Toggle);}
                        return (IntPtr)1;
                    }
                    if(keyDown&&down) {
                        usedChord=true;if(holdTimer!=null)holdTimer.Stop();if(holding){holding=false;Post(voice.Cancel);}
                        if(pressedKey==Kb.LWIN||pressedKey==Kb.RWIN) {
                            if(vk==Kb.CTRL||vk==Kb.SHIFT||vk==Kb.ALT||vk>=0xa0&&vk<=0xa5)return N.CallNextHookEx(hook,code,w,l);
                            int mods=(Kb.IsDown(Kb.CTRL)?Kb.M_CTRL:0)|(Kb.IsDown(Kb.SHIFT)?Kb.M_SHIFT:0)|(Kb.IsDown(Kb.ALT)?Kb.M_ALT:0);
                            if(vk==0x4c&&mods==0)Post(delegate{N.LockWorkStation();});else {int v=vk,m=mods;Post(delegate{Kb.SendWin(v,m,false);});}
                            eaten.Add(vk);return (IntPtr)1;
                        }
                    }
                    if(up&&eaten.Remove(vk))return (IntPtr)1;
                }
            }
            return N.CallNextHookEx(hook,code,w,l);
        }

        static void Post(Action a) { disp.BeginInvoke(DispatcherPriority.Send, a); }

        static void Toggle()
        {
            if (launcher.IsOpen) launcher.Close_(); else launcher.Open_();
        }

        static void TogglePause()
        {
            paused = !paused;
            if (paused) RemoveHook(); else InstallHook();
            Notify(paused ? "Shortcut paused" : "Shortcut enabled");
        }

        /* ----------------------------------------------------- the clipboard
         *
         * A message-only window, because a clipboard listener needs an hwnd and
         * the launcher's own is hidden most of the time. */
        static void UpdateClipboardListener()
        {
            if (!Cfg.Feature("deck")) { StopClipboard(); return; }
            if (msgWin == null) WatchClipboard();
        }

        static void WatchClipboard()
        {
            HwndSourceParameters ps = new HwndSourceParameters("SuperDeckClipboard");
            ps.ParentWindow = (IntPtr)(-3);     // HWND_MESSAGE
            msgWin = new HwndSource(ps);
            msgWin.AddHook(ClipProc);
            N.AddClipboardFormatListener(msgWin.Handle);
        }

        static IntPtr ClipProc(IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool handled)
        {

            if (msg == 0x031D)                  // WM_CLIPBOARDUPDATE
            {
                if (Cfg.Feature("deck") && (DateTime.UtcNow - echoAt).TotalMilliseconds > 400)
                    Post(Grab);
            }
            return IntPtr.Zero;
        }

        static void Grab()
        {
            /* The clipboard is a shared resource and the app that just wrote it
             * may still have it open, so a failed read is a normal event here. */
            try
            {
                if (Clipboard.ContainsText())
                {
                    string t = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(t) && t.Trim().Length > 0) Deck.AddText(t);
                }
                else if (Clipboard.ContainsImage())
                {
                    Deck.AddImage(Clipboard.GetImage());
                }
            }
            catch { }
        }

        public static void Echo() { echoAt = DateTime.UtcNow; }

        /* -------------------------------------------------------------- tray */

        const string RUNKEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string RUNVAL = "Essential Desktop";

        static string StartupShortcut { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Essential Desktop.lnk"); } }

        static bool StartsWithWindows()
        {
            if (File.Exists(StartupShortcut)) return true;
            Microsoft.Win32.RegistryKey k;
            try { k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RUNKEY, false); }
            catch (System.Security.SecurityException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            if (k == null) return false;
            object v = k.GetValue(RUNVAL);
            k.Close();
            return v != null;
        }

        static void SetStartsWithWindows(bool on)
        {
            if (!on && File.Exists(StartupShortcut)) File.Delete(StartupShortcut);
            Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RUNKEY, true);
            if (k == null) return;
            if (on) k.SetValue(RUNVAL, "\"" + SelfPath + "\"");
            else k.DeleteValue(RUNVAL, false);
            k.Close();
        }

        static void BuildTray()
        {
            tray = new WF.NotifyIcon();
            tray.Icon = DR.Icon.ExtractAssociatedIcon(SelfPath);
            tray.Text = "Essential Desktop";
            tray.Visible = true;

            WF.ContextMenuStrip menu = new WF.ContextMenuStrip();

            WF.ToolStripMenuItem open = new WF.ToolStripMenuItem("Open launcher");
            open.Click += delegate { Post(delegate { launcher.Open_(); }); };
            menu.Items.Add(open);

            WF.ToolStripMenuItem theme = new WF.ToolStripMenuItem("Light / dark");
            theme.Click += delegate
            {
                Post(delegate { P.SetPalette(P.C.NAME == "dark" ? "light" : "dark"); });
            };
            menu.Items.Add(theme);

            menu.Items.Add(new WF.ToolStripSeparator());

            WF.ToolStripMenuItem pause = new WF.ToolStripMenuItem("Pause / resume shortcut");
            pause.Click += delegate { Post(TogglePause); };
            menu.Items.Add(pause);

            WF.ToolStripMenuItem reload = new WF.ToolStripMenuItem("Reinstall shortcut");
            reload.Click += delegate { Post(delegate { RemoveHook(); InstallHook(); }); };
            menu.Items.Add(reload);

            WF.ToolStripMenuItem startup = new WF.ToolStripMenuItem("Start with Windows");
            startup.Checked = StartsWithWindows();
            startup.Click += delegate
            {
                bool on = !StartsWithWindows();
                try { SetStartsWithWindows(on); startup.Checked = on; }
                catch (Exception ex) { WF.MessageBox.Show(ex.Message, "SuperDeck"); }
            };
            menu.Items.Add(startup);

            menu.Items.Add(new WF.ToolStripSeparator());

            WF.ToolStripMenuItem quit = new WF.ToolStripMenuItem("Exit Essential Desktop");
            quit.Click += delegate { Post(Quit); };
            menu.Items.Add(quit);

            tray.ContextMenuStrip = menu;
            tray.MouseClick += delegate(object s, WF.MouseEventArgs e)
            {
                if (e.Button == WF.MouseButtons.Left) Post(Toggle);
            };
        }

        static void Notify(string msg)
        {
            if (tray == null) return;
            tray.BalloonTipTitle = "Essential Desktop";
            tray.BalloonTipText = msg;
            tray.ShowBalloonTip(1200);
        }

        static void StopClipboard()
        {
            if (msgWin != null)
            {
                N.RemoveClipboardFormatListener(msgWin.Handle);
                msgWin.Dispose();
                msgWin = null;
            }
        }

        static void Quit()
        {
            RemoveHook(); voice.Dispose();
            StopClipboard();
            if (tray != null) { tray.Visible = false; tray.Dispose(); tray = null; }
            app.Shutdown();
        }

        /* ------------------------------------------------------------- entry */

        [STAThread]
        public static void Run()
        {
            SelfPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            disp = Dispatcher.CurrentDispatcher;

            /* Before anything is built: the chrome is styled as it is constructed,
             * so a session saved in light mode would otherwise come up black. */
            Cfg.Start();
            Deck.Load();
            Notes.Load();
            Todo.Load();

            voice = new VoiceController(disp, Notify);
            launcher = new Launcher();
            UpdateClipboardListener();
            Cfg.FeatureChanged += UpdateClipboardListener;
            BuildTray();
            InstallHook();

            AppDomain.CurrentDomain.ProcessExit += delegate
            {
                RemoveHook();
            };

            Notify("Tap "+Cfg.Trigger()+" to open; hold to dictate.");
            var showSignal = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, @"Local\EssentialDesktopShow");
            var requests=System.Threading.ThreadPool.RegisterWaitForSingleObject(showSignal,delegate(object state,bool timedOut){Post(delegate{launcher.Open_();});},null,System.Threading.Timeout.Infinite,false);
            var initial = new DispatcherTimer();
            initial.Interval = TimeSpan.FromMilliseconds(800);
            initial.Tick += delegate { initial.Stop(); launcher.Open_(); if (Environment.CommandLine.Contains("--review")) launcher.Review(); };
            initial.Start();
            using (var shutdown = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, @"Local\EssentialDesktopUpdateQuit"))
            {
                var quitRequest = System.Threading.ThreadPool.RegisterWaitForSingleObject(shutdown,
                    delegate { Post(Quit); }, null, System.Threading.Timeout.Infinite, true);
                app.Run();
                quitRequest.Unregister(null);
            }
            requests.Unregister(null); showSignal.Dispose();
            RemoveHook();
        }
    }
}
public static class EssentialDesktopEntry
{
    [STAThread]
    public static void Main()
    {
        string build = @"Local\EssentialDesktopBuild_" + typeof(EssentialDesktopEntry).Module.ModuleVersionId.ToString("N");
        bool firstForBuild;
        using (var marker = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset, build, out firstForBuild))
        using (var mutex = new System.Threading.Mutex(false, @"Local\SuperDeck_SingleInstance_9f21"))
        {
            bool owned;
            try { owned = mutex.WaitOne(0, false); }
            catch (System.Threading.AbandonedMutexException) { owned = true; }
            if (!owned && firstForBuild && System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".installed")))
            {
                System.Threading.EventWaitHandle stop;
                if (System.Threading.EventWaitHandle.TryOpenExisting(@"Local\EssentialDesktopUpdateQuit", out stop))
                {
                    using (stop) stop.Set();
                    try { owned = mutex.WaitOne(10000); }
                    catch (System.Threading.AbandonedMutexException) { owned = true; }
                }
            }
            if (!owned)
            {
                System.Threading.EventWaitHandle show;
                if (System.Threading.EventWaitHandle.TryOpenExisting(@"Local\EssentialDesktopShow", out show))
                    using (show) show.Set();
                return;
            }
            try { SuperDeck.App.Run(); }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "error.log"), ex.ToString());
                System.Windows.Forms.MessageBox.Show(ex.Message, "Essential Desktop");
            }
            finally { mutex.ReleaseMutex(); }
        }
    }
}