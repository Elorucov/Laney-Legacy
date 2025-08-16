var id = parseInt(Args.peer_id);
var response = API.messages.getConversationsById({ "peer_ids": id, "extended": 1 });
return {
    writing_disabled: response.items[0].chat_settings.writing_disabled,
    can_write: response.items[0].can_write,
    acl: response.items[0].chat_settings.acl,
    permissions: response.items[0].chat_settings.permissions
};