using Newtonsoft.Json;

namespace Elorucov.VkAPI.Objects.Auth {
    public class ValidateLoginResponse {
        [JsonProperty("result")]
        public string Result { get; private set; }

        [JsonProperty("phone")]
        public string Phone { get; private set; }

        [JsonProperty("sid")]
        public string SID { get; private set; }

        [JsonProperty("is_email")]
        public bool IsEmail { get; private set; }

        [JsonProperty("email_reg_allowed")]
        public bool EmailRegAllowed { get; private set; }
    }

    public class ValidatePhoneResponse {
        [JsonProperty("code_length")]
        public int CodeLength { get; private set; }

        [JsonProperty("delay")]
        public int Delay { get; private set; }

        [JsonProperty("sid")]
        public string SID { get; private set; }

        [JsonProperty("masked_email")]
        public string MaskedEmail { get; private set; }

        [JsonProperty("validation_type")]
        public string ValidationType { get; private set; }

        [JsonProperty("validation_resend")]
        public string ValidationResend { get; private set; }
    }

    //

    public class ProfileLite {
        [JsonProperty("first_name")]
        public string FirstName { get; private set; }

        [JsonProperty("last_name")]
        public string LastName { get; private set; }

        [JsonProperty("phone")]
        public string Phone { get; private set; }

        [JsonProperty("photo_200")]
        public string Photo { get; private set; }
    }

    public class ValidatePhoneConfirmResponse {
        [JsonProperty("sid")]
        public string SID { get; private set; }

        [JsonProperty("profile_exist")]
        public bool ProfileExist { get; private set; }

        [JsonProperty("profile")]
        public ProfileLite Profile { get; private set; }
    }
}