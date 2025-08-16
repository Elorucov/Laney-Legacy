using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Elorucov.VkAPI.Objects {
    public class MessagesHistoryResponse {
        [JsonProperty("items")]
        public List<Message> Items { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }

        [JsonProperty("contacts")]
        public List<Contact> Contacts { get; set; }

        [JsonProperty("conversations")]
        public List<Conversation> Conversations { get; set; }
    }

    //

    public class MessageSendResponse {
        [JsonProperty("message_id")]
        public int MessageId { get; set; }

        [JsonProperty("cmid")]
        public int Cmid { get; set; }

        [JsonProperty("peer_id")] // required for markAsImportant
        public long PeerId { get; set; }
    }

    public class MessageSendMultiResponse {
        [JsonProperty("message_id")]
        public int MessageId { get; set; }

        [JsonProperty("conversation_message_id")]
        public int Cmid { get; set; }

        [JsonProperty("peer_id")]
        public long PeerId { get; set; }
    }

    public class MarkAsImportantResponse {
        [JsonProperty("marked")]
        public List<MessageSendResponse> Marked { get; set; }
    }

    //

    public class MessageReaction {
        [JsonProperty("reaction_id")]
        public int ReactionId { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("user_ids")]
        public List<long> UserIds { get; set; }
    }

    public class GeoCoordinates {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }
    }

    public class GeoPlace {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("created")]
        public int Created { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }
    }

    public class Geo {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("coordinates")]
        public GeoCoordinates Coordinates { get; set; }

        [JsonProperty("place")]
        public GeoPlace Place { get; set; }
    }

    public class Action {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("member_id")]
        public long MemberId { get; set; }

        [JsonIgnore]
        public long FromId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("old_text")]
        public string OldText { get; set; }

        [JsonProperty("conversation_message_id")]
        public int ConversationMessageId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }
    }

    public static class MessageFormatDataTypes {
        public const string BOLD = "bold";
        public const string ITALIC = "italic";
        public const string UNDERLINE = "underline";
        public const string LINK = "url";
    }


    public class MessageFormatDataItem {
        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public bool IsInline { get; set; }
    }

    public class MessageFormatData {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("items")]
        public List<MessageFormatDataItem> Items { get; set; }
    }

    public class Message {
        [JsonIgnore]
        public DateTime DateTime { get { return DateTimeOffset.FromUnixTimeSeconds(DateUnix).DateTime.ToLocalTime(); } }

        [JsonIgnore]
        public DateTime UpdateTime { get { return DateTimeOffset.FromUnixTimeSeconds(UpdateTimeUnix).DateTime.ToLocalTime(); } }

        [JsonIgnore]
        public DateTime PinnedAt { get { return DateTimeOffset.FromUnixTimeSeconds(PinnedAtUnix).DateTime.ToLocalTime(); } }

        [JsonIgnore]
        public bool IsPartial { get; private set; }

        //

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("conversation_message_id")]
        public int ConversationMessageId { get; set; }

        [JsonProperty("date")]
        public long DateUnix { get; set; }

        [JsonProperty("update_time")]
        public long UpdateTimeUnix { get; set; }

        [JsonProperty("pinned_at")]
        public long PinnedAtUnix { get; set; }

        [JsonProperty("peer_id")]
        public long PeerId { get; set; }

        [JsonProperty("from_id")]
        public long FromId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("random_id")]
        public int RandomId { get; set; }

        [JsonProperty("attachments")]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        [JsonProperty("important")]
        public bool Important { get; set; }

        [JsonProperty("geo")]
        public Geo Geo { get; set; }

        [JsonProperty("payload")]
        public string PayLoad { get; set; }

        [JsonProperty("keyboard")]
        public BotKeyboard Keyboard { get; set; }

        [JsonProperty("fwd_messages")]
        public List<Message> ForwardedMessages { get; set; } = new List<Message>();

        [JsonProperty("reply_message")]
        public Message ReplyMessage { get; set; }

        [JsonProperty("action")]
        public Action Action { get; set; }

        [JsonProperty("template")]
        public BotTemplate Template { get; set; }

        [JsonProperty("expire_ttl")]
        public int ExpireTTL { get; set; } // returned only in non-casper chats.

        [JsonProperty("ttl")]
        public int TTL { get; set; } // returned only in casper-chats, lol.

        [JsonProperty("is_expired")]
        public bool IsExpired { get; set; }

        [JsonProperty("is_hidden")]
        public bool IsHidden { get; set; }

        [JsonProperty("is_unavailable")]
        public bool IsUnavailable { get; set; }

        [JsonProperty("reaction_id")]
        public int ReactionId { get; set; }

        [JsonProperty("reactions")]
        public List<MessageReaction> Reactions { get; set; }

        [JsonProperty("format_data")]
        public MessageFormatData FormatData { get; set; }

        public static Message BuildFromLP(object[] msg, long currentUserId, Func<long, bool> infoCached, out bool needToGetFullMsgFromAPI, out Exception exception) {
            exception = null;
            try {
                int cmid = Convert.ToInt32(msg[1]);
                int flags = Convert.ToInt32(msg[2]);
                int minor = Convert.ToInt32(msg[3]);
                long peer = Convert.ToInt64(msg[4]);
                int timestamp = Convert.ToInt32(msg[5]);
                string text = (string)msg[6];
                JObject additional = (JObject)msg[7];
                JObject attachments = (JObject)msg[8];
                int randomId = Convert.ToInt32(msg[9]);
                int id = Convert.ToInt32(msg[10]);
                int updateTimestamp = Convert.ToInt32(msg[11]);
                needToGetFullMsgFromAPI = false;

                bool outbound = (2 & flags) != 0;
                bool important = (8 & flags) != 0;

                Message message = new Message {
                    Id = id,
                    ConversationMessageId = cmid,
                    RandomId = randomId,
                    PeerId = peer,
                    DateUnix = timestamp,
                    UpdateTimeUnix = updateTimestamp,
                    Important = important
                };

                if (additional.ContainsKey("from")) {
                    // ¯\_(ツ)_/¯
                    message.FromId = Convert.ToInt32(additional["from"].Value<string>());
                } else {
                    message.FromId = outbound ? currentUserId : peer;
                }

                bool senderInfoCached = (bool)(infoCached?.Invoke(message.FromId));
                if (!senderInfoCached) needToGetFullMsgFromAPI = true;

                if (!String.IsNullOrEmpty(text))
                    message.Text = text.Replace("<br>", "\n").Replace("&quot;", "\"").Replace("&amp;", "&")
                                       .Replace("&lt;", "<").Replace("&gt;", ">");

                if (additional.ContainsKey("payload")) message.PayLoad = additional["payload"].Value<string>();
                if (additional.ContainsKey("expire_ttl")) message.ExpireTTL = additional["expire_ttl"].Value<int>();
                if (additional.ContainsKey("ttl")) message.ExpireTTL = additional["ttl"].Value<int>();
                if (additional.ContainsKey("is_expired")) message.IsExpired = true;
                if (additional.ContainsKey("keyboard")) {
                    message.Keyboard = additional["keyboard"].ToObject<BotKeyboard>();
                    message.Keyboard.AuthorId = message.FromId;
                }
                if (additional.ContainsKey("format_data")) {
                    message.FormatData = additional["format_data"].ToObject<MessageFormatData>();
                }

                if (attachments.Count > 0) {
                    if (attachments.ContainsKey("attach1_type") && attachments["attach1_type"].Value<string>() == "sticker") {
                        if (attachments.ContainsKey("attachments_count") && attachments["attachments_count"].Value<int>() == 1) {
                            var parsedAtchs = JsonConvert.DeserializeObject<List<Attachment>>(attachments["attachments"].Value<string>());
                            if (parsedAtchs != null) message.Attachments = parsedAtchs;
                        } else {
                            needToGetFullMsgFromAPI = true;
                        }
                    } else {
                        needToGetFullMsgFromAPI = true;
                    }
                }
                if (additional.ContainsKey("source_act")) {
                    Action act = new Action {
                        Type = additional["source_act"].Value<string>()
                    };
                    act.FromId = message.FromId;
                    if (additional.ContainsKey("source_text")) act.Text = additional["source_text"].Value<string>();
                    if (additional.ContainsKey("source_old_text")) act.OldText = additional["source_old_text"].Value<string>();
                    if (additional.ContainsKey("source_mid")) act.MemberId = Int64.Parse(additional["source_mid"].Value<string>());
                    if (additional.ContainsKey("source_chat_local_id")) act.ConversationMessageId = additional["source_chat_local_id"].Value<int>();
                    if (additional.ContainsKey("source_style")) act.Style = additional["source_style"].Value<string>();
                    if (act.Type == "chat_photo_update") needToGetFullMsgFromAPI = true;
                    message.Action = act;

                    bool memberInfoCached = (bool)(infoCached?.Invoke(act.MemberId));
                    if (!memberInfoCached) needToGetFullMsgFromAPI = true;
                }

                if (additional.ContainsKey("has_template")) needToGetFullMsgFromAPI = true;
                if (additional.ContainsKey("marked_users")) needToGetFullMsgFromAPI = true;

                message.IsPartial = needToGetFullMsgFromAPI;
                return message;
            } catch (Exception ex) {
                exception = ex;
                needToGetFullMsgFromAPI = true;
                return null;
            }
        }
    }

    public class SetChatPhotoResult {
        [JsonProperty("message_id")]
        public int MessageId { get; set; }
    }
}