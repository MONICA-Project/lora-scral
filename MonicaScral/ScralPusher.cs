using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlubbFish.Utils;
using LitJson;

namespace Fraunhofer.Fit.IoT.MonicaScral {
  class ScralPusher {
    private readonly List<String> nodes = new List<String>();
    private readonly Dictionary<String, String> config;
    private readonly Object getLock = new Object();
    private readonly Boolean authRequired = false;
    private readonly String auth = "";

    public ScralPusher(Dictionary<String, String> settings) => this.config = settings;

    internal async void DataInput(Object sender, MqttEvents e) => await Task.Run(() => {
      try {
        JsonData data = JsonMapper.ToObject(e.Message);
        if(this.CheckRegister(data)) {
          if(e.Topic.StartsWith("lora/data")) {
            this.SendUpdate(data);
          } else if(e.Topic.StartsWith("lora/panic")) {
            this.SendPanic(data);
          }
        }
      } catch(Exception ex) {
        Helper.WriteError("Something is wrong: " + ex.Message);
      }
    });

    private Boolean CheckRegister(JsonData data) {
      if(data.ContainsKey("Name") && data["Name"].IsString) {
        if(!this.nodes.Contains((String)data["Name"])) {
          this.SendRegister(data);
          this.nodes.Add((String)data["Name"]);
        }
        return true;
      }
      return false;
    }

    private void SendRegister(JsonData data) {
      Dictionary<String, Object> d = new Dictionary<String, Object> {
        { "device", "wearable" },
        { "sensor", "tag" },
        { "type", "uwb" },
        { "tagId", (String)data["Name"] },
        { "timestamp", DateTime.UtcNow.ToString("o") },
        { "unitOfMeasurements", "meters" },
        { "observationType", "propietary" },
        { "state", "active" }
      };
      try {
        String addr = this.config["register_addr"];
        if(Enum.TryParse(this.config["register_method"], true, out RequestMethod meth)) {
          this.RequestString(addr, JsonMapper.ToJson(d), false, meth);
          Console.WriteLine(meth.ToString() + " " + this.config["register_addr"] + ": " + JsonMapper.ToJson(d));
        }
      } catch(Exception e) {
        Helper.WriteError("Fraunhofer.Fit.IoT.MonicaScral.SendRegister: " + e.Message);
      }
    }

    private void SendUpdate(JsonData data) {
      if((Boolean)data["Gps"]["Fix"]) {
        Dictionary<String, Object> d = new Dictionary<String, Object> {
        { "type", "uwb" },
        { "tagId", (String)data["Name"] },
        { "timestamp", DateTime.UtcNow.ToString("o") },
        { "lat", (Double)data["Gps"]["Latitude"] },
        { "lon", (Double)data["Gps"]["Longitude"] },
        { "height", (Double)data["Gps"]["Height"] },
        { "hdop", (Double)data["Gps"]["Hdop"] },
        { "snr", (Double)data["Snr"] },
        { "battery_level", (Double)data["BatteryLevel"] },
        { "host", (String)data["Host"]}
      };
        try {
          String addr = this.config["update_addr"];
          if(Enum.TryParse(this.config["update_method"], true, out RequestMethod meth)) {
            this.RequestString(addr, JsonMapper.ToJson(d), false, meth);
            Console.WriteLine(meth.ToString() + " " + this.config["update_addr"] + ": " + JsonMapper.ToJson(d));
          }
        } catch(Exception e) {
          Helper.WriteError("Fraunhofer.Fit.IoT.MonicaScral.SendUpdate: " + e.Message);
        }
      }
    }

    private void SendPanic(JsonData data) {
      Dictionary<String, Object> d = new Dictionary<String, Object> {
        { "type", "uwb" },
        { "tagId", (String)data["Name"] },
        { "timestamp", DateTime.Now.ToString("o") },
        { "last_known_lat", (Double)data["Gps"]["LastLatitude"] },
        { "last_known_lon", (Double)data["Gps"]["LastLongitude"] },
        { "last_known_gps", DateTime.Parse((String)data["Gps"]["LastGPSPos"], DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal).ToString("o") }
      };
      try {
        String addr = this.config["panic_addr"];
        if(Enum.TryParse(this.config["panic_method"], true, out RequestMethod meth)) {
          this.RequestString(addr, JsonMapper.ToJson(d), false, meth);
          Console.WriteLine(meth.ToString() + " " + this.config["panic_addr"] + ": " + JsonMapper.ToJson(d));
        }
      } catch(Exception e) {
        Helper.WriteError("Fraunhofer.Fit.IoT.MonicaScral.SendRegister: " + e.Message);
      }
    }

    #region HTTP Request
    private String RequestString(String address, String json = "", Boolean withoutput = true, RequestMethod method = RequestMethod.GET) {
      String ret = null;
      lock(this.getLock) {
        try {
          HttpWebRequest request = WebRequest.CreateHttp(this.config["server"] + address);
          request.Timeout = 2000;
          if(this.authRequired) {
            request.Headers.Add(HttpRequestHeader.Authorization, this.auth);
          }
          if(method == RequestMethod.POST || method == RequestMethod.PUT) {
            Byte[] requestdata = Encoding.ASCII.GetBytes(json);
            request.ContentLength = requestdata.Length;
            request.Method = method.ToString();
            request.ContentType = "application/json";
            using(Stream stream = request.GetRequestStream()) {
              stream.Write(requestdata, 0, requestdata.Length);
            }
          }
          using(HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
            if(response.StatusCode == HttpStatusCode.Unauthorized) {
              Console.Error.WriteLine("Benutzer oder Passwort falsch!");
              throw new Exception("Benutzer oder Passwort falsch!");
            }
            if(withoutput) {
              StreamReader reader = new StreamReader(response.GetResponseStream());
              ret = reader.ReadToEnd();
            }
          }
        } catch(Exception e) {
          throw new WebException("Error while uploading to Scal. Resource: \"" + this.config["server"] + address + "\" Method: " + method + " Data: " + json + " Fehler: " + e.Message);
        }
      }
      return ret;
    }
    private enum RequestMethod {
      GET,
      POST,
      PUT
    }
    #endregion

    internal void Dispose() { }
  }
}
