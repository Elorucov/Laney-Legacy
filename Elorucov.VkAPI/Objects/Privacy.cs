using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {

    public class PrivacySettingValueOwners {
        [JsonProperty("allowed")]
        public List<int> Allowed { get; set; }
    }

    public class PrivacySettingValue {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("owners")]
        public PrivacySettingValueOwners Owners { get; set; }

        [JsonProperty("is_enabled")]
        public bool IsEnabled { get; set; }
    }

    public class PrivacySetting {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("value")]
        public PrivacySettingValue Value { get; set; }

        [JsonProperty("section")]
        public string Section { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("supported_categories")]
        public List<string> SupportedCategories { get; set; }
    }

    public class PrivacySection {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class PrivacyCategory {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        public override string ToString() => Title;
    }

    public class PrivacyResponse {
        [JsonProperty("settings")]
        public List<PrivacySetting> Settings { get; set; }

        [JsonProperty("sections")]
        public List<PrivacySection> Sections { get; set; }

        [JsonProperty("supported_categories")]
        public List<PrivacyCategory> SupportedCategories { get; set; }
    }
}