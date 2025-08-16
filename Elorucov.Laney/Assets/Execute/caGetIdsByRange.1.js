var forks = {
    "s": fork(API.messages.search({ peer_id: Args.peer_id, count: 1, date: Args.start_date })),
    "e": fork(API.messages.search({ peer_id: Args.peer_id, count: 1, date: Args.end_date }))
};

var start = wait(forks.s);
var end = wait(forks.e);

var scmid = parseInt(start.items[0].conversation_message_id);
var ecmid = parseInt(end.items[0].conversation_message_id);
var tcount = end.count - start.count;

if (scmid == 0) {
    var fix = API.messages.getHistory({ peer_id: Args.peer_id, count: 1, rev: 1 });
    tcount = parseInt(fix.count);
    scmid = parseInt(fix.items[0].conversation_message_id);
}

return {
    first_cmid: scmid,
    last_cmid: ecmid,
    messages_count: tcount
};