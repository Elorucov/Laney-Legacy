var id = parseInt(Args.peer_id);

var forks = {
    "l": fork(API.messages.getHistory({ peer_id: id, count: 1, offset: 0 })),
    "f": fork(API.messages.getHistory({ peer_id: id, count: 1, offset: 0, rev: 1 })),
    "c": fork(API.messages.getConversationsById({ peer_ids: id, extended: 1, fields: "photo_200,photo_100,photo_50" })),
    "u": fork(API.users.get({ fields: "has_photo,photo_200,photo_100,photo_50" })),
    "r": fork(API.messages.getReactionsAssets())
};

var first = wait(forks.f);
var last = wait(forks.l);
var convr = wait(forks.c);
var current_user = wait(forks.u);
var reactions = wait(forks.r);
current_user = current_user[0];
var last_date = parseInt(last.items[0].date);
var conv = convr.items[0];

var name = "Peer " + id;
var avatar = null;
var members = [];
var profiles = [];
var groups = [];

if (conv.peer.type == "chat") {
    name = conv.chat_settings.title;
    avatar = conv.chat_settings.photo.photo_200;
    if (avatar == null) avatar = conv.chat_settings.photo.photo_100;
    if (avatar == null) avatar = conv.chat_settings.photo.photo_50;
    var m = API.messages.getConversationMembers({ peer_id: id, extended: 1, fields: "photo_200,photo_100,photo_50" });
} else if (conv.peer.type == "user") {
    var user = API.users.get({ user_ids: conv.peer.local_id, fields: "photo_200,photo_100,photo_50" })[0];
    name = user.first_name + " " + user.last_name;
    avatar = user.photo_200;
} else if (conv.peer.type == "group") {
    var group = API.groups.getById({ group_ids: conv.peer.local_id, fields: "photo_200,photo_100,photo_50" }).groups[0];
    name = group.name;
    avatar = group.photo_200;
}

return {
    name: name,
    avatar: avatar,
    messages_count: parseInt(first.count),
    first_cmid: parseInt(first.items[0].conversation_message_id),
    first_date: parseInt(first.items[0].date),
    last_date: last_date,
    can_write: conv.can_write.allowed,
    type: conv.peer.type,
    reactions_assets: reactions.assets
};