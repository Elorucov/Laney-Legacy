using ELOR.VKAPILib;
using ELOR.VKAPILib.Attributes;
using ELOR.VKAPILib.Methods;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.VKAPIExecute.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.Laney.VKAPIExecute
{
    [Section("execute")]
    public class Execute : MethodsSectionBase
    {
        private static VKAPI API { get; set; }

        public Execute(VKAPI api) : base(api)
        {
            // TODO: надо переделать обращения к методам execute, особенно с других окон.
            API = api;
        }

        [Method("l2StartSession")]
        public async Task<StartSessionResponse> StartSessionAsync(int userId, int groupId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("lp_version", "11");
            parameters.Add("user_id", userId.ToString());
            if (Settings.SuggestStickers)
            {
                parameters.Add("need_stickers_keywords", "1");
                parameters.Add("aliases", "1");
            }
            if (groupId != 0) parameters.Add("group_id", groupId.ToString());
            return await API.CallMethodAsync<StartSessionResponse>(this, parameters);
        }

        [Method("getUserCard")]
        public async Task<UserEx> GetUserCardAsync(int userId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("user_id", userId.ToString());
            parameters.Add("func_v", "3");
            return await API.CallMethodAsync<UserEx>(this, parameters);
        }

        [Method("getChatInfoWithMembers")]
        public async Task<ChatInfoEx> GetChatInfoAsync(int chatId, List<string> fields)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("chat_id", chatId.ToString());
            parameters.Add("fields", String.Join(",", fields));
            parameters.Add("func_v", "2");
            return await API.CallMethodAsync<ChatInfoEx>(this, parameters);
        }

        [Method("getHistoryWithMembers")]
        public async Task<MessagesHistoryEx> GetHistoryWithMembersAsync(int groupId, int peerId, int offset, int count, int startMessageId, bool rev, List<string> fields, bool dontReturnMembers = false)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (groupId > 0) parameters.Add("group_id", groupId.ToString());
            parameters.Add("peer_id", peerId.ToString());
            parameters.Add("offset", offset.ToString());
            parameters.Add("count", count.ToString());
            parameters.Add("start_message_id", startMessageId.ToString());
            if (rev) parameters.Add("rev", "1");
            if (dontReturnMembers) parameters.Add("do_not_return_members", "1");
            parameters.Add("fields", fields.Combine());
            parameters.Add("func_v", "5");
            return await API.CallMethodAsync<MessagesHistoryEx>(this, parameters);
        }

        [Method("getImportantMessages")]
        public async Task<ImportantMessagesResponse> GetImportantMessagesAsync(int offset, int count)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("offset", offset.ToString());
            parameters.Add("count", count.ToString());
            return await API.CallMethodAsync<ImportantMessagesResponse>(this, parameters);
        }

        [Method("addChatUser")]
        public async Task<AddChatUserResponse> AddChatUserAsync(int chatId, List<int> userIds, int visibleMessagesCount = 0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("chat_id", chatId.ToString());
            parameters.Add("user_ids", userIds.Combine());
            parameters.Add("visible_messages_count", visibleMessagesCount.ToString());
            return await API.CallMethodAsync<AddChatUserResponse>(this, parameters);
        }

        [Method("multiSend")]
        public async Task<List<MultiSendResult>> MultiSendAsync(VKAPI api, List<int> peerIds, string message, List<string> attachments)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("peer_ids", peerIds.Combine());
            parameters.Add("message", message);
            if (attachments.Count > 0) parameters.Add("attachments", attachments.Combine());
            return await api.CallMethodAsync<List<MultiSendResult>>(this, parameters);
        }

        [Method("getStickersKeywords")]
        public async Task<List<StickerDictionary>> GetStickersKeywordsAsync()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("aliases", "1");
            return await API.CallMethodAsync<List<StickerDictionary>>(this, parameters);
        }

        [Method("addVoteAndGetResult")]
        public async Task<AddVoteResponse> AddVoteAndGetResultAsync(int ownerId, int id, List<int> answerIds)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("owner_id", ownerId.ToString());
            parameters.Add("poll_id", id.ToString());
            parameters.Add("answer_ids", String.Join(",", answerIds));
            return await API.CallMethodAsync<AddVoteResponse>(this, parameters);
        }

        [Method("leaveAndDeleteChat")]
        public async Task<LeaveAndDeleteChatResult> LeaveAndDeleteChatAsync(int peerId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("peer_id", peerId.ToString());
            return await API.CallMethodAsync<LeaveAndDeleteChatResult>(this, parameters);
        }

        [Method("deleteConvAndDenyGroup")]
        public async Task<DeleteConvAndDenyGroupResult> DeleteConvAndDenyGroupAsync(int groupId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("group_id", groupId.ToString());
            return await API.CallMethodAsync<DeleteConvAndDenyGroupResult>(this, parameters);
        }
    }
}
