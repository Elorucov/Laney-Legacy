using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Upload;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Messages {
        public static async Task<object> GetConversations(int count = 60, int offset = 0, string filter = null, int folderId = 0, string token = null) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "count", count.ToString() },
                { "offset", offset.ToString() }
            };
            if (folderId > 0) req.Add("folder_id", folderId.ToString());
            req.Add("extended", "1");
            req.Add("fields", API.Fields);
            if (!String.IsNullOrEmpty(filter)) req.Add("filter", filter);
            if (!String.IsNullOrEmpty(token)) req.Add("access_token", token);

            var res = await API.SendRequestAsync("messages.getConversations", req);
            return VKResponseHelper.ParseResponse<ConversationsResponse>(res);
        }

        public static async Task<object> GetConversationById(long peerId, string token = null) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_ids", peerId.ToString() },
                { "extended", "1" },
                { "fields", API.Fields }
            };
            if (!String.IsNullOrEmpty(token)) req.Add("access_token", token);

            var res = await API.SendRequestAsync("messages.getConversationsById", req);
            return VKResponseHelper.ParseResponse<VKList<Conversation>>(res);
        }

        public static async Task<object> GetConversationMembers(long peerId, int offset = 0, int count = 0) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "extended", "1" },
                { "fields", API.Fields }
            };


            if (offset > 0) req.Add("offset", offset.ToString());
            if (count > 0) req.Add("count", count.ToString());

            var res = await API.SendRequestAsync("messages.getConversationMembers", req);
            return VKResponseHelper.ParseResponse<VKList<ChatMember>>(res);
        }

        public static async Task<object> SearchConversationMembers(long peerId, string query, int offset = 0, int count = 0) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "q", query },
                { "extended", "1" },
                { "fields", API.Fields }
            };


            if (offset > 0) req.Add("offset", offset.ToString());
            if (count > 0) req.Add("count", count.ToString());

            var res = await API.SendRequestAsync("messages.searchConversationMembers", req);
            return VKResponseHelper.ParseResponse<VKList<ChatMember>>(res);
        }

        public static async Task<object> Search(string query, long peerId, int previewLength, int offset, int count, string date = null, string token = null) {
            Dictionary<string, string> req = new Dictionary<string, string>();
            req.Add("q", query);
            if (peerId != 0) req.Add("peer_id", peerId.ToString());
            req.Add("date", date);
            req.Add("offset", offset.ToString());
            req.Add("count", count.ToString());
            req.Add("extended", "1");
            req.Add("fields", API.Fields);
            if (!String.IsNullOrEmpty(token)) req.Add("access_token", token);

            var res = await API.SendRequestAsync("messages.search", req);
            return VKResponseHelper.ParseResponse<MessagesHistoryResponse>(res);
        }

        public static async Task<object> SearchConversations(string query, int count = 100) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "q", query },
                { "count", count.ToString() },
                { "extended", "1" },
                { "fields", API.Fields }
            };

            var res = await API.SendRequestAsync("messages.searchConversations", req);
            return VKResponseHelper.ParseResponse<VKList<Conversation>>(res);
        }

        public static async Task<object> GetById(List<KeyValuePair<long, int>> peerCmids, string token = null) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "extended", "1" },
                { "fields", API.Fields }
            };
            List<string> pcs = new List<string>();
            foreach (var kv in peerCmids) {
                pcs.Add($"{kv.Key}_{kv.Value}");
            }
            req.Add("peer_cmids", String.Join(",", pcs));
            if (!String.IsNullOrEmpty(token)) req.Add("access_token", token);

            var res = await API.SendRequestAsync("messages.getById", req);
            return VKResponseHelper.ParseResponse<MessagesHistoryResponse>(res);
        }

        public static async Task<object> GetByConversationMessageId(long peerId, List<int> ids, string token = null) {
            string id = "";
            foreach (int i in ids) { id += $"{i},"; }
            id = id.Substring(0, id.Length - 1);

            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "conversation_message_ids", id },
                { "extended", "1" },
                { "fields", API.Fields }
            };
            if (!String.IsNullOrEmpty(token)) req.Add("access_token", token);

            var res = await API.SendRequestAsync("messages.getByConversationMessageId", req);
            return VKResponseHelper.ParseResponse<MessagesHistoryResponse>(res);
        }

        public static async Task<object> GetInviteLink(long peerId, bool reset, int visibleMessagesCount = 0) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "reset", reset ? "1" : "0" },
                { "visible_messages_count", visibleMessagesCount.ToString() }
            };

            var res = await API.SendRequestAsync("messages.getInviteLink", req);
            return VKResponseHelper.ParseResponse<ChatLink>(res);
        }

        public static async Task<object> GetChatPreview(string link) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "link", link },
                { "extended", "1" },
                { "fields", "photo_100,has_photo,photo_200" }
            };

            var res = await API.SendRequestAsync("messages.getChatPreview", req);
            return VKResponseHelper.ParseResponse<ChatPreviewResponse>(res);
        }

        public static async Task<object> JoinChatByInviteLink(string link) {
            Dictionary<string, string> req = new Dictionary<string, string>();
            req.Add("link", link);

            var res = await API.SendRequestAsync("messages.joinChatByInviteLink", req);
            try {
                if (res is string) {
                    string restr = res.ToString();
                    if (restr.Contains("{\"response\":")) {
                        restr = VKResponseHelper.GetJSONInResponseObject(restr);
                        JObject j = JObject.Parse(restr);
                        return j["chat_id"].Value<long>();
                    } else if (restr.Contains("{\"error\":")) {
                        VKErrorResponse er = JsonConvert.DeserializeObject<VKErrorResponse>(restr);
                        return er.error;
                    } else {
                        throw new Exception($"A non-standart response was received:\n{restr}");
                    }
                } else {
                    return res;
                }
            } catch (Exception ex) {
                return ex;
            }
        }

        public static async Task<object> EditChat(long id, string title = null, string permissions = null, string description = null) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "chat_id", id.ToString() }
            };
            if (title != null) req.Add("title", title);
            if (permissions != null) req.Add("permissions", permissions);
            if (description != null) req.Add("description", description);

            var res = await API.SendRequestAsync("messages.editChat", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> MarkAsRead(long peerId, int startMessageId, bool markConvAsRead = false) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "up_to_cmid", startMessageId.ToString() }
            };
            if (markConvAsRead) req.Add("mark_conversation_as_read", "1");

            var res = await API.SendRequestAsync("messages.markAsRead", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> MarkAsUnreadConversation(long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.markAsUnreadConversation", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> MarkAsImportant(long peerId, int cmid, bool important) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmids", cmid.ToString() },
                { "important", important ? "1" : "0" }
            };

            var res = await API.SendRequestAsync("messages.markAsImportant", req);
            return VKResponseHelper.ParseResponse<MarkAsImportantResponse>(res);
        }

        public static async Task<object> RemoveChatUser(long id, long memberid) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "chat_id", id.ToString() },
                { "member_id", memberid.ToString() }
            };

            var res = await API.SendRequestAsync("messages.removeChatUser", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> DeleteConversation(long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.deleteConversation", req);
            return VKResponseHelper.ParseResponse<object>(res);
        }

        public static async Task<object> GetLongPollServer() {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "need_pts", "1" },
                { "version", "19" }
            };

            var res = await API.SendRequestAsync("messages.getLongPollServer", req);
            return VKResponseHelper.ParseResponse<LongPollServerInfo>(res);
        }

        public static async Task<object> GetLongPollHistory(string ts, string pts, int eventsLimit, int msgsLimit) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "ts", ts },
                { "pts", pts },
                { "events_limit", eventsLimit.ToString() },
                { "msgs_limit", msgsLimit.ToString() },
                { "credentials", "1" },
                { "version", "19" }
            };

            var res = await API.SendRequestAsync("messages.getLongPollHistory", req);
            return VKResponseHelper.ParseResponse<LongPollHistoryResponse>(res);
        }

        public static async Task<object> Pin(long peerId, int messageId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "conversation_message_id", messageId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.pin", req);
            return VKResponseHelper.ParseResponse<Message>(res);
        }

        public static async Task<object> Unpin(long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.unpin", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> Send(long peerId, int randomId, string message, double? glat, double? glong, string attachment, string forward, long? stickerId, string payload = null, bool dontParseLinks = false, bool disableMentions = false, bool silent = false, int expireTtl = 0, string reference = null, string refSource = null, MessageFormatData formatData = null, string token = null) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "random_id", randomId.ToString() }
            };

            if (glat != null) req.Add("lat", glat.ToString().Replace(",", "."));
            if (glat != null) req.Add("long", glong.ToString().Replace(",", "."));

            if (!String.IsNullOrEmpty(message)) req.Add("message", message);
            if (!String.IsNullOrEmpty(attachment)) req.Add("attachment", attachment);
            if (!String.IsNullOrEmpty(forward)) req.Add("forward", forward);
            if (stickerId != null && stickerId > 0) req.Add("sticker_id", stickerId.ToString());
            if (!String.IsNullOrEmpty(payload)) req.Add("payload", payload);
            if (dontParseLinks) req.Add("dont_parse_links", "1");
            if (disableMentions) req.Add("disable_mentions", "1");
            if (silent) req.Add("silent", "1");
            if (expireTtl > 0) req.Add("expire_ttl", expireTtl.ToString());
            if (!String.IsNullOrEmpty(reference)) req.Add("ref", reference);
            if (!String.IsNullOrEmpty(refSource)) req.Add("ref_source", refSource);
            if (formatData != null && formatData.Items.Count > 0) req.Add("format_data", JsonConvert.SerializeObject(formatData, Formatting.None, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            }));
            if (!String.IsNullOrEmpty(token)) req.Add("access_token", token);

            var res = await API.SendRequestAsync("messages.send", req);
            return VKResponseHelper.ParseResponse<MessageSendResponse>(res);
        }

        public static async Task<object> SendMulti(List<long> peerIds, int randomId, string message, string attachment, string forward) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_ids", String.Join(",", peerIds) },
                { "random_id", randomId.ToString() }
            };

            if (!String.IsNullOrEmpty(message)) req.Add("message", message);
            if (!String.IsNullOrEmpty(attachment)) req.Add("attachment", attachment);
            if (!String.IsNullOrEmpty(forward)) req.Add("forward", forward);
            req.Add("access_token", API.WebToken);

            var res = await API.SendRequestAsync("messages.send", req);
            return VKResponseHelper.ParseResponse<List<MessageSendMultiResponse>>(res);
        }

        public static async Task<object> Edit(long peerId, int cmid, string message, double? glat, double? glong, string attachment, bool dontParseLinks = false, bool keepForwardMessages = true, MessageFormatData formatData = null, string token = null) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "conversation_message_id", cmid.ToString() }
            };

            if (glat != null) req.Add("lat", glat.ToString().Replace(",", "."));
            if (glat != null) req.Add("long", glong.ToString().Replace(",", "."));

            if (!String.IsNullOrEmpty(message)) req.Add("message", message);
            if (!String.IsNullOrEmpty(attachment)) req.Add("attachment", attachment);
            req.Add("keep_forward_messages", keepForwardMessages ? "1" : "0");
            if (dontParseLinks) req.Add("dont_parse_links", "1");
            req.Add("keep_snippets", "1");
            if (formatData != null && formatData.Items.Count > 0) req.Add("format_data", JsonConvert.SerializeObject(formatData, Formatting.None, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            }));
            if (!String.IsNullOrEmpty(token)) req.Add("access_token", token);

            var res = await API.SendRequestAsync("messages.edit", req);
            return VKResponseHelper.ParseResponse<int>(res);
        }

        public static async Task<object> Delete(long peerId, List<int> messageIds, bool spam, bool forAll) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmids", String.Join(",", messageIds) },
                { "spam", spam ? "1" : "0" },
                { "delete_for_all", forAll ? "1" : "0" },
                { "v", "5.131" }
            };

            var res = await API.SendRequestAsync("messages.delete", req);
            return VKResponseHelper.ParseResponse<Dictionary<string, int>>(res);
        }

        public static async Task<object> Restore(long peerId, int messageId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmid", messageId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.restore", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        //public static async Task<object> GetHistory(long peerId, int startCmid, int offset = 0, int count = 40) {
        //    Dictionary<string, string> req = new Dictionary<string, string> {
        //        { "peer_id", peerId.ToString() },
        //        { "start_cmid", startCmid.ToString() },
        //        { "offset", offset.ToString() },
        //        { "count", count.ToString() },
        //        { "extended", "1" },
        //        { "fields", "photo_50,photo_100,has_photo,photo_200" }
        //    };

        //    var res = await API.SendRequestAsync("messages.getHistory", req);
        //    return VKResponseHelper.ParseResponse<VKList<Message>>(res);
        //}

        public static async Task<object> GetHistoryAttachments(long peerId, string mediaType, int cmid, int offset, int count) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "attachment_types", mediaType },
                { "cmid", cmid.ToString() },
                { "attachment_position", "10" },
                { "offset", offset.ToString() },
                { "count", count.ToString() }
            };
            req.Add("photo_sizes", "1");
            req.Add("fields", API.Fields);

            var res = await API.SendRequestAsync("messages.getHistoryAttachments", req);
            return VKResponseHelper.ParseResponse<ConversationAttachmentsResponse>(res);
        }

        public static async Task<object> CreateChat(List<long> userIds, string title, ChatPermissions permissions = null) {
            Dictionary<string, string> req = new Dictionary<string, string>();
            if (userIds.Count > 0) req.Add("user_ids", String.Join(",", userIds));
            if (!String.IsNullOrEmpty(title)) req.Add("title", title);
            if (permissions != null) req.Add("permissions", JsonConvert.SerializeObject(permissions, Formatting.None, new StringEnumConverter()));
            req.Add("v", "5.191");

            var res = await API.SendRequestAsync("messages.createChat", req);
            return VKResponseHelper.ParseResponse<CreateChatResponse>(res);
        }

        public static async Task<object> AddChatUser(long chatId, long userId, int visibleMessagesCount = 0) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "chat_id", chatId.ToString() },
                { "user_id", userId.ToString() },
                { "visible_messages_count", visibleMessagesCount.ToString() }
            };

            var res = await API.SendRequestAsync("messages.addChatUser", req);
            return VKResponseHelper.ParseResponse<VKResult>(res);
        }

        public static async Task<object> SetActivity(long peerId, string type) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "type", type }
            };

            var res = await API.SendRequestAsync("messages.setActivity", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> AllowMessagesFromGroup(long groupId, string key = null) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "group_id", groupId.ToString() }
            };
            if (!String.IsNullOrEmpty(key)) req.Add("key", key);

            var res = await API.SendRequestAsync("messages.allowMessagesFromGroup", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> DenyMessagesFromGroup(long groupId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "group_id", groupId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.denyMessagesFromGroup", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> ChangeConversationMemberRestrictions(long peerId, long memberId, bool deny, int denySeconds = 0) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "member_ids", memberId.ToString() },
                { "action", deny ? "ro" : "rw" }
            };
            if (denySeconds > 0 && deny) req.Add("for", denySeconds.ToString());

            var res = await API.SendRequestAsync("messages.changeConversationMemberRestrictions", req);
            return VKResponseHelper.ParseResponse<MemberRestrictionResponse>(res);
        }

        public static async Task<object> SetMemberRole(long peerId, long memberId, string role) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "member_id", memberId.ToString() },
                { "role", role }
            };

            var res = await API.SendRequestAsync("messages.setMemberRole", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> SendMessageEvent(long peerId, string payload, int messageId = 0, long authorId = 0) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() }
            };
            if (messageId > 0) req.Add("conversation_message_id", messageId.ToString());
            if (authorId != 0) req.Add("author_id", authorId.ToString());
            req.Add("payload", payload);

            var res = await API.SendRequestAsync("messages.sendMessageEvent", req);
            return VKResponseHelper.ParseResponse<string>(res);
        }

        public static async Task<object> SetChatPhoto(string file) {
            var reqs = new Dictionary<string, string> {
                { "file", file }
            };

            var res = await API.SendRequestAsync("messages.setChatPhoto", reqs);
            return VKResponseHelper.ParseResponse<SetChatPhotoResult>(res);
        }

        public static async Task<object> DeleteChatPhoto(long chatId) {
            var reqs = new Dictionary<string, string> {
                { "chat_id", chatId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.deleteChatPhoto", reqs);
            return VKResponseHelper.ParseResponse<SetChatPhotoResult>(res);
        }

        public static async Task<object> PinConversation(long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.pinConversation", req);
            return VKResponseHelper.ParseResponse<int>(res);
        }

        public static async Task<object> UnpinConversation(long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.unpinConversation", req);
            return VKResponseHelper.ParseResponse<int>(res);
        }

        public static async Task<object> ArchiveConversation(long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.archiveConversation", req);
            return VKResponseHelper.ParseResponse<int>(res);
        }

        public static async Task<object> UnarchiveConversation(long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.unarchiveConversation", req);
            return VKResponseHelper.ParseResponse<int>(res);
        }

        public static async Task<object> Translate(long peerId, int cmid, string lang) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmids", cmid.ToString() },
                { "language", lang },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.translate", req);
            return VKResponseHelper.ParseResponse<int>(res);
        }

        public static async Task<object> GetFolders() {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.getFolders", req);
            return VKResponseHelper.ParseResponse<VKList<Folder>>(res);
        }

        public static async Task<object> CreateFolder(string name, List<long> peerIds) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "name", name },
                { "included_peer_ids", String.Join(',', peerIds) },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.createFolder", req);
            return VKResponseHelper.ParseResponse<FolderCreatedResponse>(res);
        }

        public static async Task<object> UpdateFolder(int folderId, string name, List<long> add, List<long> remove) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "folder_id", folderId.ToString() }
            };
            if (!String.IsNullOrEmpty(name)) req.Add("name", name);
            if (add != null && add.Count > 0) req.Add("add_included_peer_ids", String.Join(',', add));
            if (remove != null && remove.Count > 0) req.Add("remove_included_peer_ids", String.Join(',', remove));
            req.Add("access_token", API.WebToken);

            var res = await API.SendRequestAsync("messages.updateFolder", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> DeleteFolder(int folderId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "folder_id", folderId.ToString() },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.deleteFolder", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> ReorderFolders(List<int> folderIds) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "folder_ids", String.Join(',', folderIds) },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.reorderFolders", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> WhoReadMessage(long peerId, int cmId, int offset = 0, int count = 50) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmid", cmId.ToString() },
                { "offset_major_id", offset.ToString() },
                { "count", count.ToString() },
                { "extended", "1" },
                { "fields", "photo_100,has_photo" }
            };

            var res = await API.SendRequestAsync("messages.getMessageReadPeers", req);
            return VKResponseHelper.ParseResponse<VKList<long>>(res);
        }

        public static async Task<object> WhoReadMessageLite(long peerId, int cmId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmid", cmId.ToString() },
                { "count", "3" },
                { "extended", "1" },
                { "fields", "sex,photo_50" }
            };

            var res = await API.SendRequestAsync("messages.getMessageReadPeers", req);
            return VKResponseHelper.ParseResponse<VKList<long>>(res);
        }

        public static async Task<object> SendReaction(long peerId, int cmId, int reactionId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmid", cmId.ToString() },
                { "reaction_id", reactionId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.sendReaction", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> DeleteReaction(long peerId, int cmId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmid", cmId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.deleteReaction", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> MarkReactionsAsRead(long peerId, List<int> cmIds) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmids", String.Join(",", cmIds) }
            };

            var res = await API.SendRequestAsync("messages.markReactionsAsRead", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> GetReactedPeers(long peerId, int cmId, int reactionId = 0) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "cmid", cmId.ToString() },
                { "extended", "1" },
                { "fields", "sex,photo_100,has_photo" }
            };
            if (reactionId > 0) req.Add("reaction_id", reactionId.ToString());

            var res = await API.SendRequestAsync("messages.getReactedPeers", req);
            return VKResponseHelper.ParseResponse<GetReactedPeersResponse>(res);
        }

        public static async Task<object> GetImportantMessages(int count = 80, int offset = 0) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "count", count.ToString() },
                { "offset", offset.ToString() },
                { "fields", API.Fields },
                { "extended", "1" },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.getImportantMessages", p);
            return VKResponseHelper.ParseResponse<ImportantMessagesResponse>(res);
        }

        public static async Task<object> GetSharedConversations(long peerId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.getSharedConversations", req);
            return VKResponseHelper.ParseResponse<VKList<Conversation>>(res);
        }

        public static async Task<object> GetVideoMessageShapes() {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.getVideoMessageShapes", req);
            return VKResponseHelper.ParseResponse<VideoMessageShapesResponse>(res);
        }

        public static async Task<object> EnableChatWriting(long chatId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "chat_id", chatId.ToString() },
            };

            var res = await API.SendRequestAsync("messages.enableChatWriting", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> DisableChatWriting(long chatId, long durationSeconds = 0) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "chat_id", chatId.ToString() },
                { "duration_sec", durationSeconds.ToString() }
            };

            var res = await API.SendRequestAsync("messages.disableChatWriting", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> GetAudioMessageUploadServer() {
            var reqs = new Dictionary<string, string>();

            var res = await API.SendRequestAsync("messages.getAudioMessageUploadServer", reqs);
            return VKResponseHelper.ParseResponse<VkUploadServer>(res);
        }

        public static async Task<object> SaveAudioMessage(string file) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "file", file }
            };

            var res = await API.SendRequestAsync("messages.saveAudioMessage", req);
            return VKResponseHelper.ParseResponse<AudioMessage>(res);
        }

        public static async Task<object> DropChatForAll(int chatId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "chat_id", chatId.ToString() }
            };

            var res = await API.SendRequestAsync("messages.dropChatForAll", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }

        public static async Task<object> MuteChatMentions(long peerId, string mentionStatus) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "mention_status", mentionStatus.ToString() },
                { "access_token", API.WebToken }
            };

            var res = await API.SendRequestAsync("messages.muteChatMentions", req);
            return VKResponseHelper.ParseResponse<bool>(res);
        }
    }
}