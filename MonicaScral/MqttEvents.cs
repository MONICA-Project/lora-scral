using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fraunhofer.Fit.IoT.MonicaScral {
  class MqttEvents : EventArgs {
    public MqttEvents(String topic, String message, DateTime date) {
      this.Topic = topic;
      this.Message = message;
      this.Date = date;
    }

    public String Topic { get; }
    public String Message { get; }
    public DateTime Date { get; }
  }
}
