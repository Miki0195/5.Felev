using System.Text.Json.Serialization;

namespace Managly.Models
{
    public class SignalData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("stream")]
        public object Stream { get; set; }

        [JsonPropertyName("candidate")]
        public object Candidate { get; set; }

        [JsonPropertyName("sdp")]
        public string Sdp { get; set; }
    }
}