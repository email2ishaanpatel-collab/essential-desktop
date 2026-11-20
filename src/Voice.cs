using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Windows.Threading;
namespace SuperDeck {
// Native PCM capture. Buffers and the microphone exist only during a hold.
public sealed class Microphone : IDisposable {
    [StructLayout(LayoutKind.Sequential)] struct Format { public ushort tag,channels; public uint rate,bytes; public ushort align,bits,extra; }
    [StructLayout(LayoutKind.Sequential)] struct Header { public IntPtr data; public uint length,recorded; public UIntPtr user; public uint flags,loops; public IntPtr next,reserved; }
    delegate void Callback(IntPtr h,uint msg,IntPtr user,IntPtr p1,IntPtr p2);
    [DllImport("winmm.dll")] static extern uint waveInOpen(out IntPtr h,uint device,ref Format f,Callback cb,IntPtr user,uint flags);
    [DllImport("winmm.dll")] static extern uint waveInPrepareHeader(IntPtr h,IntPtr p,uint n);
    [DllImport("winmm.dll")] static extern uint waveInUnprepareHeader(IntPtr h,IntPtr p,uint n);
    [DllImport("winmm.dll")] static extern uint waveInAddBuffer(IntPtr h,IntPtr p,uint n);
    [DllImport("winmm.dll")] static extern uint waveInStart(IntPtr h);
    [DllImport("winmm.dll")] static extern uint waveInReset(IntPtr h);
    [DllImport("winmm.dll")] static extern uint waveInClose(IntPtr h);
    IntPtr handle; readonly Callback callback; readonly List<IntPtr> buffers=new List<IntPtr>();
    readonly MemoryStream pcm=new MemoryStream(); readonly object gate=new object(); volatile bool stopping;
    readonly float[] levels = new float[5];
    public double Peak;

    public void CopyLevels(float[] destination) {
        lock (gate) Array.Copy(levels, destination, levels.Length);
    }
    static readonly uint Size=(uint)Marshal.SizeOf(typeof(Header));
    public Microphone() {
        callback=Data;
        var f=new Format { tag=1,channels=1,rate=16000,bytes=32000,align=2,bits=16 };
        if(waveInOpen(out handle,0xffffffff,ref f,callback,IntPtr.Zero,0x30000)!=0) throw new InvalidOperationException("Microphone unavailable. Check Windows microphone access and your input device.");
        try { for(int i=0;i<4;i++) { var p=Marshal.AllocHGlobal((int)Size); var h=new Header { data=Marshal.AllocHGlobal(1280),length=1280 }; Marshal.StructureToPtr(h,p,false); buffers.Add(p); if(waveInPrepareHeader(handle,p,Size)!=0 || waveInAddBuffer(handle,p,Size)!=0) throw new InvalidOperationException("Could not prepare microphone audio."); } if(waveInStart(handle)!=0) throw new InvalidOperationException("Could not start microphone."); } catch { Dispose(); throw; }
    }
    void Data(IntPtr h,uint msg,IntPtr user,IntPtr p,IntPtr extra) {
        if(msg!=0x3c0) return;
        try { var header=(Header)Marshal.PtrToStructure(p,typeof(Header)); if(header.recorded>0) { var data=new byte[header.recorded]; Marshal.Copy(header.data,data,0,data.Length); double energy=0, peak=0; for(int i=0;i+1<data.Length;i+=2) { double v=(short)(data[i]|(data[i+1]<<8))/32768.0; energy+=v*v; peak=Math.Max(peak,Math.Abs(v)); } float rms=(float)Math.Sqrt(energy/Math.Max(1,data.Length/2)); Peak=Math.Max(Peak,rms); lock(gate) { Array.Copy(levels,1,levels,0,4); levels[4]=(float)Math.Min(1,peak/0.35); if(pcm.Length < 32000*120) pcm.Write(data,0,data.Length); } } if(!stopping) waveInAddBuffer(handle,p,Size); } catch { }
    }
    public byte[] Finish() { Stop(); lock(gate) return pcm.ToArray(); }
    void Stop() { if(handle==IntPtr.Zero) return; stopping=true; waveInReset(handle); foreach(var p in buffers) { waveInUnprepareHeader(handle,p,Size); var h=(Header)Marshal.PtrToStructure(p,typeof(Header)); Marshal.FreeHGlobal(h.data); Marshal.FreeHGlobal(p); } buffers.Clear(); waveInClose(handle); handle=IntPtr.Zero; }
    public void Dispose() { Stop(); pcm.Dispose(); }
    public static void SaveWave(string path,byte[] data) { using(var w=new BinaryWriter(File.Create(path))) { w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36+data.Length); w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(16000); w.Write(32000); w.Write((short)2); w.Write((short)16); w.Write(Encoding.ASCII.GetBytes("data")); w.Write(data.Length); w.Write(data); } }
}
public sealed class VoicePill : Window {
    readonly Canvas art=new Canvas(); readonly List<System.Windows.Shapes.Rectangle> bars=new List<System.Windows.Shapes.Rectangle>();
    readonly TextBlock sign=new TextBlock(); readonly Stopwatch clock=new Stopwatch();
    public Action<float[]> ReadLevels;
    readonly float[] levels = new float[5];
    readonly DispatcherTimer linger = new DispatcherTimer();
    readonly Random random = new Random();
    readonly Canvas symbol = new Canvas();
    static readonly string[] endings = { "Donzo","Got it","Typed.","Sorted","Noted","Heard","Bosh","Word.","Nice one","Copy that","Roger","Aye","Ta","Boom","Neat","Yoink","Ship it","Easy","Onwards","Mic drop","Beep","10-4" };
    static readonly string[][] glyphs = {
        new[] { ".X.X.", "XXXXX", "XXXXX", ".XXX.", "..X.." },
        new[] { ".X.X.", ".X.X.", ".....", "X...X", ".XXX." },
        new[] { ".....", "....X", "X..X.", ".XX..", "..X.." },
        new[] { "..X..", "..X..", "XXXXX", ".XXX.", "X...X" }
    };
    public bool Thinking;
    bool animated;
    int sequence;
    double anchor;

    readonly TranslateTransform slide=new TranslateTransform();
    public VoicePill() {
        Width=76;Height=54;WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;ResizeMode=ResizeMode.NoResize;
        var card=new Border { CornerRadius=new CornerRadius(27),Background=new SolidColorBrush(Color.FromRgb(255,217,0)),Child=art,RenderTransform=slide }; Content=card;
        art.Children.Add(symbol);
        linger.Tick += delegate { linger.Stop(); SlideOut(); };
        for(int i=0;i<5;i++) { var b=new System.Windows.Shapes.Rectangle { Width=4,Height=4,RadiusX=2,RadiusY=2,Fill=new SolidColorBrush(Color.FromRgb(27,27,29)) }; Canvas.SetLeft(b,20.8+i*7.6); Canvas.SetTop(b,25); art.Children.Add(b); bars.Add(b); }
        sign.Width=76;sign.Height=32;sign.TextAlignment=TextAlignment.Center;sign.Padding=new Thickness(0);sign.FontFamily=new FontFamily("Consolas");sign.FontSize=13;sign.Foreground=new SolidColorBrush(Color.FromRgb(27,27,29));sign.Visibility=Visibility.Collapsed;art.Children.Add(sign);
        SourceInitialized+=delegate { var h=new WindowInteropHelper(this).Handle; SetWindowLong(h,-20,GetWindowLong(h,-20)|0x08000000|0x20); };
    }
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h,int n);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h,int n,int v);
    public void Listen() {
        sequence++;
        linger.Stop();
        BeginAnimation(LeftProperty, null);
        var area = SystemParameters.WorkArea;
        anchor = area.Left + 12;
        Left = anchor;
        Top = area.Top + area.Height * 0.55 - Height / 2;
        Thinking = false;
        sign.Visibility = Visibility.Collapsed;
        symbol.Children.Clear();
        foreach (var bar in bars) bar.Visibility = Visibility.Visible;
        Show();
        BeginAnimation(LeftProperty, new DoubleAnimation(anchor - 96, anchor, TimeSpan.FromMilliseconds(380)) {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        clock.Restart();
        if (!animated) { CompositionTarget.Rendering += Frame; animated = true; }
    }
    public void StartThinking() { Thinking = true; clock.Restart(); }
    void Frame(object sender, EventArgs e) {
        if (!Thinking && ReadLevels != null) ReadLevels(levels);
        double head = (clock.Elapsed.TotalMilliseconds % 1200) * 5 / 1200;
        for (int i = 0; i < bars.Count; i++) {
            double distance = Math.Abs(head - i);
            double level = Thinking ? 0.75 * Math.Max(0, 1 - Math.Min(distance, 5 - distance)) : levels[i];
            bars[i].Height = 4 + 26 * level;
            Canvas.SetTop(bars[i], (Height - bars[i].Height) / 2);
        }
    }
    void Dot(double x, double y, double radius) {
        var dot = new System.Windows.Shapes.Ellipse { Width = 2 * radius, Height = 2 * radius, Fill = sign.Foreground };
        Canvas.SetLeft(dot, x - radius);
        Canvas.SetTop(dot, y - radius);
        symbol.Children.Add(dot);
    }
    public void Done(bool ok) {
        StopFrames();
        foreach (var bar in bars) bar.Visibility = Visibility.Collapsed;
        if (ok) {
            int index = random.Next(2) == 0 ? -1 : random.Next(endings.Length + 5);
            int glyph = index - endings.Length;
            sign.Text = index < 0 ? "Done." : glyph < 0 ? endings[index] : glyph == 4 ? "Carl Pei" : "";
            sign.Visibility = Visibility.Visible;
            Canvas.SetTop(sign, glyph == 4 ? 30 : 19);
            if (glyph >= 0) {
                string[] pixels = glyphs[glyph % 4];
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 5; x++)
                        if (pixels[y][x] == 'X') Dot(38 + (x-2)*3.6, (glyph==4 ? 17 : 27) + (y-2)*3.6, 1.4);
            }
        } else {
            Dot(22.8,22,2.2); Dot(53.2,22,2.2); Dot(38,27,2.2); Dot(22.8,32,2.2); Dot(53.2,32,2.2);
        }
        linger.Interval = TimeSpan.FromMilliseconds(ok ? 420 : 620);
        linger.Start();
    }
    void SlideOut() {
        int current = sequence;
        var exit = new DoubleAnimation(anchor, anchor - 96, TimeSpan.FromMilliseconds(260)) {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        exit.Completed += delegate { if (sequence == current) Hide(); };
        BeginAnimation(LeftProperty, exit);
    }
    public void StopFrames(){if(animated)CompositionTarget.Rendering-=Frame;animated=false;clock.Stop();}
    public void Dismiss(){sequence++;linger.Stop();StopFrames();Hide();}
}
public sealed class VoiceController : IDisposable {
    readonly Dispatcher ui; readonly Action<string> notify; Microphone mic; VoicePill pill; IntPtr target; bool busy; volatile bool canceled; volatile int generation; Process worker; readonly object workerGate=new object();
    readonly DispatcherTimer limit;
    public bool Active { get { return mic!=null || busy; } }
    public VoiceController(Dispatcher d,Action<string> n) { ui=d;notify=n;limit=new DispatcherTimer {Interval=TimeSpan.FromSeconds(90)};limit.Tick+=delegate{End();}; }
    public void Begin() {
        if(Active)return;
        try { string engine=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"voice","whisper-cli.exe");if(!File.Exists(engine)||!File.Exists(Cfg.ModelPath()))throw new InvalidOperationException("The dictation engine or selected model is missing. Check the Whisper model in Settings.");
            canceled=false;target=N.GetForegroundWindow();mic=new Microphone(); if(pill==null)pill=new VoicePill();pill.ReadLevels=delegate(float[] values){if(mic!=null)mic.CopyLevels(values);};pill.Listen();limit.Start();
        } catch(Exception ex){if(mic!=null){mic.Dispose();mic=null;}notify(ex.Message);}
    }
    public void End() {
        limit.Stop();if(mic==null)return;
        byte[] data=mic.Finish();double peak=mic.Peak;mic.Dispose();mic=null;
        if(data.Length<8000||peak<0.003){pill.Done(false);return;}
        busy=true;int run=++generation; if(pill!=null)pill.StartThinking();
        System.Threading.ThreadPool.QueueUserWorkItem(delegate {
            string path=Path.Combine(Path.GetTempPath(),"essential-"+Guid.NewGuid().ToString("N")+".wav");string text="",error=null;
            try { Microphone.SaveWave(path,data);text=Transcribe(path,delegate(Process p){lock(workerGate){if(canceled||run!=generation){try{p.Kill();}catch{}}else worker=p;}}); }catch(Exception ex){error=ex.Message;} finally {try{File.Delete(path);File.Delete(path+".txt");}catch{}}
            ui.BeginInvoke(new Action(delegate {if(run!=generation)return;busy=false;worker=null;if(canceled){pill.Dismiss();return;}if(error!=null){pill.Done(false);notify(error);return;}if(string.IsNullOrWhiteSpace(text)){pill.Done(false);return;}
                if(N.GetForegroundWindow()!=target){pill.Done(false);notify("Dictation canceled because focus changed. Hold the key again in the destination field.");return;}
                bool sent=TypeText(text);pill.Done(sent);if(!sent)notify("Could not insert dictation into this app. Try a normal, non-administrator text field.");
            }));
        });
    }
    public static string Transcribe(string path,Action<Process> started) {
        string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"voice");
        var psi=new ProcessStartInfo(Path.Combine(dir,"whisper-cli.exe"),"-m \""+Cfg.ModelPath()+"\" -f \""+path+"\" -of \""+path+"\" -otxt -nt -np -ng -sns -l en -t "+Math.Max(1,Math.Min(4,Environment.ProcessorCount/2))) {UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
        using(var p=Process.Start(psi)){ if(started!=null)started(p);p.BeginErrorReadLine();p.BeginOutputReadLine();try{p.PriorityClass=ProcessPriorityClass.BelowNormal;}catch{}if(!p.WaitForExit(90000)){p.Kill();p.WaitForExit();throw new InvalidOperationException("Dictation timed out. Try a shorter recording.");}p.WaitForExit();if(p.ExitCode!=0)throw new InvalidOperationException("Local transcription failed. Check the dictation files."); }
        return File.Exists(path+".txt")?File.ReadAllText(path+".txt").Trim():"";
    }
    public static bool TypeText(string text) { var inputs=new List<N.INPUT>();foreach(char c in text){var down=new N.INPUT();down.type=1;down.u.ki.wScan=c;down.u.ki.dwFlags=4;inputs.Add(down);var up=down;up.u.ki.dwFlags=6;inputs.Add(up);}return N.SendInput((uint)inputs.Count,inputs.ToArray(),Marshal.SizeOf(typeof(N.INPUT)))==inputs.Count; }
    public void Cancel(){limit.Stop();canceled=true;generation++;if(mic!=null){mic.Dispose();mic=null;}lock(workerGate){try{if(worker!=null&&!worker.HasExited)worker.Kill();}catch{}worker=null;}busy=false;if(pill!=null)pill.Dismiss();}
    public void Dispose(){Cancel();if(pill!=null)pill.Close();}
}
}
