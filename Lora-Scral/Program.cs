using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlubbFish.Utils;
using BlubbFish.Utils.IoT.Bots;
using BlubbFish.Utils.IoT.Connector;

namespace Fraunhofer.Fit.IoT.LoraScral {
  class Program : ABot {
    private ADataBackend mqtt;
    private ScralPusher scral;

    static void Main(String[] _1) => new Program();

    public Program() {
      InIReader.SetSearchPath(new List<String>() { "/etc/lorascral", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\lorascral" });
      if (!InIReader.ConfigExist("settings")) {
        Helper.WriteError("No settings.ini found. Abord!");
        return;
      }
      InIReader settings = InIReader.GetInstance("settings");
      this.logger.SetPath(settings.GetValue("logging", "path"));

      this.Connect(settings);
      this.Attach();
      this.WaitForShutdown();
      this.Dispose();
    }

    private void Connect(InIReader settings) {
      this.mqtt = (ADataBackend)ABackend.GetInstance(settings.GetSection("mqtt"), ABackend.BackendType.Data);
      this.scral = new ScralPusher(settings.GetSection("scral"));
    }

    private void Attach() => this.mqtt.MessageIncomming += this.MqttMessageIncomming;

    private async void MqttMessageIncomming(Object sender, BlubbFish.Utils.IoT.Events.BackendEvent e) => await Task.Run(() => this.scral.DataInput(e.From.ToString(), e.Message, e.Date));

    public override void Dispose() {
      this.mqtt.Dispose();
      this.scral.Dispose();
      base.Dispose();
    }
  }
}
