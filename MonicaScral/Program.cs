using System;
using System.Collections.Generic;
using System.Threading;
using BlubbFish.Utils;

namespace Fraunhofer.Fit.IoT.MonicaScral {
  class Program {
    private Thread sig_thread;
    private Boolean RunningProcess = true;
    protected ProgramLogger logger = new ProgramLogger();

    static void Main(String[] args) => new Program(args);

    public Program(String[] args) {
      InIReader.SetSearchPath(new List<String>() { "/etc/monicascral", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\monicascral" });
      if(!InIReader.ConfigExist("settings")) {
        Helper.WriteError("No settings.ini found. Abord!");
        return;
      }
      InIReader settings = InIReader.GetInstance("settings");
      this.logger.SetPath(settings.GetValue("logging", "path"));
      MqttListener m = new MqttListener(settings.GetSection("mqtt"));
      ScralPusher s = new ScralPusher(settings.GetSection("scral"));
      m.Update += s.DataInput;
      this.WaitForShutdown();
      m.Dispose();
      s.Dispose();
    }

    protected void WaitForShutdown() {
      if(Type.GetType("Mono.Runtime") != null) {
        this.sig_thread = new Thread(delegate () {
          Mono.Unix.UnixSignal[] signals = new Mono.Unix.UnixSignal[] {
            new Mono.Unix.UnixSignal(Mono.Unix.Native.Signum.SIGTERM),
            new Mono.Unix.UnixSignal(Mono.Unix.Native.Signum.SIGINT)
          };
          Console.WriteLine("BlubbFish.Utils.IoT.Bots.Bot.WaitForShutdown: Signalhandler Mono attached.");
          while(true) {
            Int32 i = Mono.Unix.UnixSignal.WaitAny(signals, -1);
            Console.WriteLine("BlubbFish.Utils.IoT.Bots.Bot.WaitForShutdown: Signalhandler Mono INT recieved " + i + ".");
            this.RunningProcess = false;
            break;
          }
        });
        this.sig_thread.Start();
      } else {
        Console.CancelKeyPress += new ConsoleCancelEventHandler(this.SetupShutdown);
        Console.WriteLine("BlubbFish.Utils.IoT.Bots.Bot.WaitForShutdown: Signalhandler Windows attached.");
      }
      while(this.RunningProcess) {
        Thread.Sleep(100);
      }
    }

    private void SetupShutdown(Object sender, ConsoleCancelEventArgs e) {
      e.Cancel = true;
      Console.WriteLine("BlubbFish.Utils.IoT.Bots.Bot.SetupShutdown: Signalhandler Windows INT recieved.");
      this.RunningProcess = false;
    }
  }
}
