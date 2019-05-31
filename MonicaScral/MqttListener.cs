using System;
using System.Collections.Generic;
using BlubbFish.Utils.IoT.Connector;
using BlubbFish.Utils.IoT.Events;

namespace Fraunhofer.Fit.IoT.MonicaScral {
  class MqttListener {
    protected ABackend mqtt;
    private readonly Dictionary<String, String> config;

    public delegate void InputData(Object sender, MqttEvents e);
    public event InputData Update;

    public MqttListener(Dictionary<String, String> settings) {
      this.config = settings;
      if(this.config.ContainsKey("type")) {
        this.mqtt = ABackend.GetInstance(this.config, ABackend.BackendType.Data);
        this.mqtt.MessageIncomming += this.MqttData;
      } else {
        throw new ArgumentException("Setting section [mqtt] is missing or wrong!");
      }
    }

    private void MqttData(Object sender, BackendEvent e) => this.Update?.Invoke(this, new MqttEvents(e.From.ToString(), e.Message, e.Date));

    public void Dispose() {
      this.mqtt.MessageIncomming -= this.MqttData;
      this.mqtt.Dispose();
      this.mqtt = null;
    }
  }
}
