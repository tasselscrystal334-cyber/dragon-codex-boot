using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using DragonCodexBoot;
class DesktopTests {
 [STAThread] public static int Main(string[] args) {
  try {
   Native.SetProcessDpiAwarenessContext(new IntPtr(-4));Native.SetThreadDpiAwarenessContext(new IntPtr(-4));
   Config c=Entry.ReadConfig(args[0]);c.TargetDisplay=args[1];
   bool missing=args.Length>2&&args[2]=="missing";
   bool shortVideo=args.Length>2&&args[2]=="short";
   if(shortVideo) { c.HoldAt=11.3;c.TransitionStart=12.7;c.TransitionEnd=13.65; }
   if(missing)c.MaxWaitSeconds=2;
   c.Video=args[3];c.MatchClientToPlayer=false;
   c.AutoReplaceEntrypoints=false;c.Volume=0;
   IntPtr real=Native.FindApp(c);
   if(Native.Eligible(IntPtr.Zero,c)||Native.Eligible(new IntPtr(12345),c))throw new Exception("Invalid HWND accepted");
   using(var fixture=new System.Windows.Forms.Form()) {
    fixture.Width=800;fixture.Height=600;fixture.Show();
    if(Native.Eligible(fixture.Handle,c))throw new Exception("Unpackaged unrelated top-level window accepted");
    IntPtr destroyed=fixture.Handle;fixture.Close();
    if(Native.Eligible(destroyed,c))throw new Exception("Destroyed window accepted");
   }
   Console.WriteLine("existing="+Native.Describe(real,c));
   if(real==IntPtr.Zero)throw new Exception("No eligible existing Codex window; test will not cold launch active chat.");
   var screen=Native.Monitors().First(s=>s.Device==args[1]);
   var area=screen.Work;int samples=0;bool outside=false,timedout=false;
   int launches=0;
   var app=new Application();var boot=missing?new BootWindow(c,args[0],delegate {return IntPtr.Zero;},delegate {launches++;}):new BootWindow(c,args[0]);
   string logPath=Path.Combine(args[0],"logs","launcher.log");
   long logOffset=new FileInfo(logPath).Length;
   var timer=new DispatcherTimer();timer.Interval=TimeSpan.FromMilliseconds(100);var clock=Stopwatch.StartNew();
   timer.Tick+=delegate {
    var h=new WindowInteropHelper(boot).Handle;Bounds b=Native.ClientBounds(h);
    if(b.Width>0) { samples++;if(b.Left<area.Left||b.Top<area.Top||b.Right>area.Right||b.Bottom>area.Bottom)outside=true; }
    if(clock.Elapsed.TotalSeconds>22) { timedout=true;boot.Close(); }
   };
   boot.Closed+=delegate { timer.Stop(); };timer.Start();int code=app.Run(boot);
   string log;using(var file=new FileStream(logPath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) {file.Seek(logOffset,SeekOrigin.Begin);using(var reader=new StreamReader(file))log=reader.ReadToEnd();}
   if(missing) {
    if(code!=2||outside||timedout||launches!=1||!log.Contains("handoff-failed reason=window-or-playback-timeout")||log.Contains("handed-off-to-real-window"))throw new Exception("Missing-window fallback failed exit="+code+" launches="+launches);
    Console.WriteLine("PASS desktop missing-window: one injected launch, bounded timeout, failure UI auto-exit; no real AppLaunch");return 0;
   }
   if(code!=0||outside||timedout||samples<50||!log.Contains("handed-off-to-real-window")||log.Contains("app-launch-requested"))throw new Exception("Desktop regression failed samples="+samples+" outside="+outside+" timeout="+timedout+" exit="+code);
   Console.WriteLine("PASS desktop display="+args[1]+" samples="+samples+" seconds="+clock.Elapsed.TotalSeconds.ToString("F2")+" media/window/handoff tested; no AppLaunch");return 0;
  } catch(Exception e) { Console.Error.WriteLine(e);return 1; }
 }
}
