var o = Args.owner_id;
var p = Args.poll_id;
var answer_ids = Args.answer_ids;
var result = {};

var vr = API.polls.addVote({ "owner_id": o, "poll_id": p, "answer_ids": answer_ids });
if (vr) {
    result.success = true;
    var poll = API.polls.getById({ "owner_id": o, "poll_id": p, "extended": "1" });
    result.poll = poll;
} else {
    result.success = false;
}

return result;