using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects.Auth {
    public class OauthResponse {
        [JsonProperty("user_id")]
        public long UserId { get; private set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; private set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; private set; }
    }

    public class BanInfo {
        [JsonProperty("member_name")]
        public string MemberName { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }
    }

    public class DirectAuthResponse : OauthResponse {
        [JsonProperty("error")]
        public string Error { get; private set; }

        [JsonProperty("error_type")]
        public string ErrorType { get; private set; }

        [JsonProperty("error_description")]
        public string ErrorDescription { get; private set; }

        [JsonProperty("phone_mask")]
        public string PhoneMask { get; private set; }

        [JsonProperty("validation_type")]
        public string ValidationType { get; private set; }

        [JsonProperty("captcha_sid")]
        public string CaptchaSid { get; private set; }

        [JsonProperty("captcha_img")]
        public string CaptchaImg { get; private set; }

        [JsonProperty("ban_info")]
        public BanInfo BanInfo { get; private set; }

        [JsonProperty("extend_fields")]
        public List<string> ExtendFields { get; private set; }
    }

    public class RefreshTokensTokenResponse {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; private set; }
    }

    public class RefreshTokensSuccessResponse {
        [JsonProperty("user_id")]
        public long UserId { get; private set; }

        [JsonProperty("banned")]
        public bool Banned { get; private set; }

        [JsonProperty("access_token")]
        public RefreshTokensTokenResponse AccessToken { get; private set; }
    }

    public class RefreshTokensResponse {
        [JsonProperty("success")]
        public List<RefreshTokensSuccessResponse> Success { get; private set; }
    }

    public class UserExchangeToken {
        [JsonProperty("user_id")]
        public long UserId { get; private set; }

        [JsonProperty("common_token")]
        public string CommonToken { get; private set; }
    }

    public class ExchangeTokenResponseOld {
        [JsonProperty("token")]
        public string Token { get; private set; }
    }

    public class ExchangeTokenResponse {
        [JsonProperty("users_exchange_tokens")]
        public List<UserExchangeToken> UsersExchangeTokens { get; private set; }
    }

    public class GetAuthCodeResponse {
        [JsonProperty("auth_code")]
        public string AuthCode { get; private set; }

        [JsonProperty("auth_hash")]
        public string AuthHash { get; private set; }

        [JsonProperty("auth_id")]
        public string AuthId { get; private set; }

        [JsonProperty("auth_url")]
        public string AuthUrl { get; private set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; private set; }
    }

    public class CheckAuthCodeResponse {
        [JsonProperty("status")]
        public byte Status { get; private set; } // 2 — success.

        [JsonProperty("user_id")]
        public long UserId { get; private set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; private set; }

        [JsonProperty("super_app_token")]
        public string SuperAppToken { get; private set; }

        [JsonProperty("is_partial")]
        public bool IsPartial { get; private set; }
    }

    public class AuthInfo {
        [JsonProperty("auth_id")]
        public string AuthId { get; private set; }
    }

    public class ProcessAuthCodeResponse {
        [JsonProperty("profile")]
        public User Profile { get; private set; }

        [JsonProperty("auth_info")]
        public AuthInfo AuthInfo { get; private set; }

        [JsonProperty("status")]
        public int Status { get; private set; }
    }
}
