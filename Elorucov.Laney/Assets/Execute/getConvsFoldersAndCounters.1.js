var count = parseInt(Args.count);
var offset = parseInt(Args.offset);
var userId = parseInt(Args.user_id);
var fields = Args.fields;
var filter = "messages,messages_unread_unmuted,messages_archive,messages_archive_unread,messages_archive_unread_unmuted,messages_archive_mentions_count,messages_folders";

var only_unmuted = API.account.getInfo({ "fields": "show_only_not_muted_messages" }).show_only_not_muted_messages;

var forks = {
  'messages.getFolders': fork(API.messages.getFolders({"supported_types": "business,personal"})),
  'messages.getConversations': fork(API.messages.getConversations({"count": count, "offset": offset, "extended": 1, "fields": fields})),
  'account.getCounters': fork(API.account.getCounters({"filter": filter})),
  'messages.getVideoMessageShapes': fork(API.messages.getVideoMessageShapes({ "filter": filter }))
};
var response = {
  'folders': wait(forks['messages.getFolders']),
  'conversations': wait(forks['messages.getConversations']),
  'counters': wait(forks['account.getCounters']),
  'video_message_shapes': wait(forks['messages.getVideoMessageShapes']),
  'show_only_not_muted_messages': only_unmuted == true ? true : false // чтобы при ошибках не прилетел null.
};

return response;