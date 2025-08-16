var ids = Args.peer_ids;
var message = Args.message;
var attachments = Args.attachments;

var idArray = ids.split(',');
var result = [];
var i = 0;
var max = idArray.length < 10 ? idArray.length : 10;
while (i < max) {
    var peer = idArray[i];
    var msgid = API.messages.send({ "peer_id": peer, "random_id": "0", "message": message, "attachment": attachments });
    if (msgid) result.push({ "peer_id": peer, "message_id": parseInt(msgid) });
    i = i + 1;
}

return result;