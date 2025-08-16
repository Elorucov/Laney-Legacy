var p = parseInt(Args.peer_id);
var c = parseInt(Args.count);
var o = parseInt(Args.offset);
var s = parseInt(Args.start_cmid);
var f = Args.fields;
var rev = parseInt(Args.rev);
var h = parseInt(Args.do_not_return_members);

var forks = {
    'h': fork(API.messages.getHistory({"peer_id":p, "count":c, "offset":o, "start_cmid":s, "extended":1, "fields":f, "rev":rev})),
    'c': fork(API.messages.getConversationsById({"peer_ids":p, "extended":"1"}))
};

var his = wait(forks['h']);
var ls = wait(forks['c']);
var lmsg = null;
if (his.conversations[0].last_conversation_message_id != null) {
  lmsg = API.messages.getByConversationMessageId({"peer_id":p, "conversation_message_ids": his.conversations[0].last_conversation_message_id}).items[0];
}

var resp = {};
resp.conversation = ls.items[0];
resp.messages = his.items;
if (resp.messages == null) resp.messages = [];
resp.messages_count = parseInt(his.count);
resp.last_cmid = parseInt(ls.items[0].last_conversation_message_id);
resp.last_message = lmsg;
if(h == 0) {
    if(resp.conversation.peer.type == "chat") {
        var mem = API.messages.getConversationMembers({"peer_id":p, "extended": "1", "fields":f});
        resp.members = mem.items;
    }
}
if(resp.conversation.peer.type == "group") {
  var u = API.users.get();
  var grp = API.messages.isMessagesFromGroupAllowed({"group_id": resp.conversation.peer.local_id, "user_id": u[0].id });
  resp.is_messages_allowed = grp.is_allowed;
}
resp.profiles = his.profiles;
resp.groups = his.groups;
resp.contacts = ls.contacts;

if(resp.conversation.peer.type == "user") {
  var u = API.users.get({"user_ids": p, "fields": "online_info"})[0];
  if(u.online_info.visible && u.online_info.app_id) {
    var oa = API.apps.get({"app_id": u.online_info.app_id}).items[0];
    u.online_info.app_name = oa.title;
  }
  resp.online_info = u.online_info;
}

return resp;