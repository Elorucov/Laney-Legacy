using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class RequestParam {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class VKError {
        public int error_code { get; set; }
        public string error_msg { get; set; }
        public string error_text { get; set; }
        public List<RequestParam> request_params { get; set; }
        public string captcha_sid { get; set; }
        public string captcha_img { get; set; }
    }

    public class VKErrorResponse {
        public VKError error { get; set; }
    }
}