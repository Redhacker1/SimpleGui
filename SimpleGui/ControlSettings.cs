using System;
using Newtonsoft.Json;

namespace SimpleGui
{
    [Serializable]
    public class ControlSettings
    {
        [JsonIgnore]
        public ControlColorTheme Colors { get; set; } = new ControlColorTheme();
    }
}
