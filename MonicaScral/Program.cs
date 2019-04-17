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
      MqttListener m = new MqttListener(new Dictionary<String, String>() { { "type", "mqtt" }, { "server", "10.100.0.20" }, { "topic", "lora/data/+;lora/panic/+" } });
      ScralPusher s = new ScralPusher(new Dictionary<String, String>() {
        { "server", "http://monappdwp3.monica-cloud.eu:8250" },
        { "register_addr", "/scral/v1.0/gps-tracker-gw/gps-tag" },
        { "register_method", "post" },
        { "update_addr", "/scral/v1.0/gps-tracker-gw/gps-tag/localization" },
        { "update_method", "put" },
        { "panic_addr", "/scral/v1.0/gps-tracker-gw/gps-tag/alert" },
        { "panic_method", "put" },});
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
