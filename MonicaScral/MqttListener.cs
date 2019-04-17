using System;
using System.Collections.Generic;
using System.Threading;
using BlubbFish.Utils.IoT.Connector;
using BlubbFish.Utils.IoT.Events;

namespace Fraunhofer.Fit.IoT.MonicaScral {
  class MqttListener {
    protected readonly Thread connectionWatcher;
    protected ABackend mqtt;
    private readonly Dictionary<String, String> config;

    public delegate void InputData(Object sender, MqttEvents e);
    public event InputData Update;

    public MqttListener(Dictionary<String, String> settings) {
      this.config = settings;
      if(this.config.ContainsKey("type")) {
        this.connectionWatcher = new Thread(this.ConnectionWatcherRunner);
        this.connectionWatcher.Start();
      } else {
        throw new ArgumentException("Setting section [mqtt] is missing or wrong!");
      }
    }

    protected void ConnectionWatcherRunner() {
      while(true) {
        try {
          if(this.mqtt == null || !this.mqtt.IsConnected) {
            this.Reconnect();
          }
          Thread.Sleep(10000);
        } catch(Exception) { }
      }
    }

    protected void Reconnect() {
      Console.WriteLine("Fraunhofer.Fit.IoT.MonicaScral.Reconnect()");
      this.Disconnect();
      this.Connect();
    }

    protected void Connect() {
      this.mqtt = ABackend.GetInstance(this.config, ABackend.BackendType.Data);
      this.mqtt.MessageIncomming += this.MqttData;
      Console.WriteLine("Fraunhofer.Fit.IoT.MonicaScral.Connect()");
    }

    private void MqttData(Object sender, BackendEvent e) => this.Update?.Invoke(this, new MqttEvents(e.From.ToString(), e.Message, e.Date));

    protected void Disconnect() {
      if(this.mqtt != null) {
        this.mqtt.MessageIncomming -= this.MqttData;
        this.mqtt.Dispose();
      }
      this.mqtt = null;
      Console.WriteLine("Fraunhofer.Fit.IoT.MonicaScral.Disconnect()");
    }

    public void Dispose() {
      this.connectionWatcher.Abort();
      while(this.connectionWatcher.ThreadState == ThreadState.Running) { Thread.Sleep(10); }
      this.Disconnect();
    }
  }
}
