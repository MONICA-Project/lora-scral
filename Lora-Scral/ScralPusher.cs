using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BlubbFish.Utils;

using LitJson;

namespace Fraunhofer.Fit.IoT.LoraScral {
  class ScralPusher {
    private readonly List<String> nodes = new List<String>();
    private readonly Object getLockNodes = new Object();
    private readonly Dictionary<String, String> config;
    private readonly Boolean authRequired = false;
    private readonly String auth = "";
    private readonly Dictionary<String, Tuple<Double, Double, DateTime>> last_pos = new Dictionary<String, Tuple<Double, Double, DateTime>>();

    private static readonly HttpClient client = new HttpClient();

    public ScralPusher(Dictionary<String, String> settings) {
      this.config = settings;
      if (this.authRequired) {
        client.DefaultRequestHeaders.Add("Authorization", this.auth);
      }
    }

    internal void DataInput(String topic, String message, DateTime _1) {
      try {
        JsonData data = JsonMapper.ToObject(message);
        if (this.CheckRegister(data)) {
          if (topic.StartsWith("lora/data")) {
            this.SendUpdate(data);
          } else if (topic.StartsWith("lora/panic")) {
            this.SendPanic(data);
          }
        }
      } catch (Exception ex) {
        Helper.WriteError("Something is wrong: " + ex.Message);
      }
    }

    private Boolean CheckRegister(JsonData data) {
      lock (this.getLockNodes) {
        if (data.ContainsKey("Name") && data["Name"].IsString) {
          if (!this.nodes.Contains((String)data["Name"])) {
            this.SendRegister(data);
            this.nodes.Add((String)data["Name"]);
          }
          return true;
        }
        return false;
      }
    }

    private async void SendRegister(JsonData data) {
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
          _ = await this.RequestString(addr, JsonMapper.ToJson(d), false, meth);
          Console.WriteLine(meth.ToString() + " " + this.config["register_addr"] + ": " + JsonMapper.ToJson(d));
        }
      } catch(Exception e) {
        Helper.WriteError("Fraunhofer.Fit.IoT.MonicaScral.SendRegister: " + e.Message);
      }
    }

    private async void SendUpdate(JsonData data) {
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
          if (this.last_pos.ContainsKey((String)data["Name"])) {
            this.last_pos[(String)data["Name"]] = new Tuple<Double, Double, DateTime>((Double)data["Gps"]["Latitude"], (Double)data["Gps"]["Longitude"], DateTime.UtcNow);
          } else {
            this.last_pos.Add((String)data["Name"], new Tuple<Double, Double, DateTime>((Double)data["Gps"]["Latitude"], (Double)data["Gps"]["Longitude"], DateTime.UtcNow));
          }
          String addr = this.config["update_addr"];
          if(Enum.TryParse(this.config["update_method"], true, out RequestMethod meth)) {
            _ = await this.RequestString(addr, JsonMapper.ToJson(d), false, meth);
            Console.WriteLine(meth.ToString() + " " + this.config["update_addr"] + ": " + JsonMapper.ToJson(d));
          }
        } catch(Exception e) {
          Helper.WriteError("Fraunhofer.Fit.IoT.MonicaScral.SendUpdate: " + e.Message);
        }
      }
    }

    private async void SendPanic(JsonData data) {
      Dictionary<String, Object> d = new Dictionary<String, Object> {
        { "type", "uwb" },
        { "tagId", (String)data["Name"] },
        { "timestamp", DateTime.Now.ToString("o") },
      };
      if((Boolean)data["Gps"]["Fix"]) {
        d.Add("last_known_lat", (Double)data["Gps"]["Latitude"]);
        d.Add("last_known_lon", (Double)data["Gps"]["Longitude"]);
        d.Add("last_known_gps", DateTime.UtcNow.ToString("o"));
      } else {
        if(!this.last_pos.ContainsKey((String)data["Name"])) {
          return;
        }
        d.Add("last_known_lat", this.last_pos[(String)data["Name"]].Item1);
        d.Add("last_known_lon", this.last_pos[(String)data["Name"]].Item2);
        d.Add("last_known_gps", this.last_pos[(String)data["Name"]].Item3.ToString("o"));
      }
        try {
        String addr = this.config["panic_addr"];
        if(Enum.TryParse(this.config["panic_method"], true, out RequestMethod meth)) {
          _ = await this.RequestString(addr, JsonMapper.ToJson(d), false, meth);
          Console.WriteLine(meth.ToString() + " " + this.config["panic_addr"] + ": " + JsonMapper.ToJson(d));
        }
      } catch(Exception e) {
        Helper.WriteError("Fraunhofer.Fit.IoT.MonicaScral.SendRegister: " + e.Message);
      }
    }

    #region HTTP Request
    private async Task<String> RequestString(String address, String json = "", Boolean withoutput = true, RequestMethod method = RequestMethod.GET) {
      String ret = null;
      try {
        HttpResponseMessage response = null;
        if (method == RequestMethod.POST || method == RequestMethod.PUT) {
          HttpContent content = new StringContent(json);
          content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
          //content.Headers.Add("Content-Type", "application/json");
          if (method == RequestMethod.POST) {
            response = await client.PostAsync(this.config["server"] + address, content);
          } else if (method == RequestMethod.PUT) {
            response = await client.PutAsync(this.config["server"] + address, content);
          }
          content.Dispose();
        } else if (method == RequestMethod.GET) {
          response = await client.GetAsync(this.config["server"] + address);
        }
        if (!response.IsSuccessStatusCode) {
          throw new Exception(response.StatusCode + ": " + response.ReasonPhrase);
        }
        if (withoutput && response != null) {
          ret = await response.Content.ReadAsStringAsync();
        }
      } catch (Exception e) {
        throw new WebException("Error while uploading to Scal. Resource: \"" + this.config["server"] + address + "\" Method: " + method + " Data: " + json + " Fehler: " + e.Message);
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
