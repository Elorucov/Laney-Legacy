var user_ids = Args.user_ids;
var resp = {};
resp.success = [];
resp.failed = [];

if (user_ids.length > 1) {
    var ids = user_ids.split(",");

    var i = 0;
    while (i < ids.length) {
        var apiresp = API.messages.addChatUser({ "chat_id": Args.chat_id, "user_id": ids[i], "visible_messages_count": Args.visible_messages_count });
        if (apiresp.result == 1) {
            resp.success.push(parseInt(ids[i]));
        } else {
            resp.failed.push(parseInt(ids[i]));
        }
        i = i + 1;
    }
} else {
    resp.error = "user_ids is incorrect";
}
return resp;