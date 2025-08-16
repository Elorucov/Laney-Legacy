using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;

namespace Elorucov.VkAPI.Helpers {
    public class VKResponseHelper {
        public static object ParseResponse<T>(object response, [CallerMemberName] string caller = "") {
            try {
                if (response is string restr) {
                    VKResponse<T> resp = JsonConvert.DeserializeObject<VKResponse<T>>(restr, new JsonSerializerSettings {
                        MaxDepth = 256
                    });
                    if (resp.Error != null) {
                        if (API.CheckIsSessionInvalid(resp.Error)) API.FireSessionInvalidEvent();
                        return resp.Error;
                    } else if (resp.Response != null) {
                        return resp.Response;
                    } else {
                        throw new Exception($"An incorrect response was received from {caller}:\n{restr}");
                    }
                }
                return response;
            } catch (Exception ex) {
                return ex;
            }
        }

        // Избавиться бы от этой исторической фигни...
        public static string GetJSONInResponseObject(string json) {
            string start = "{\"response\":";
            string end = "}";

            if (json.StartsWith(start) && json.EndsWith(end) && json.Length >= (start.Length + end.Length)) {
                string res = json.Substring(start.Length, json.Length - start.Length - end.Length);

                if (res.Contains(",\"execute_errors\"")) {
                    int errstart = res.IndexOf(",\"execute_errors\"", 0);
                    res = res.Substring(0, errstart);
                }
                return res;
            } else {
                return null;
            }
        }
    }
}