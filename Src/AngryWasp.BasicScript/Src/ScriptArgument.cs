using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AngryWasp.BasicScript
{
    public class ScriptArgument
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Value_Type Type { get; set; }

        [JsonProperty("ref")]
        public bool Ref { get; set; }
    }
}