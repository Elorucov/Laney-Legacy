var peer_id = parseInt(Args.peer_id);
var cmids = Args.cmids.split(",");
if (cmids.length > 20) return [];

var i = 0;
var forks = [];
while (i < cmids.length) {
    forks.push(fork(API.messages.getReactedPeers({ peer_id: peer_id, cmid: parseInt(cmids[i]), extended: 1, fields: "photo_200,photo_100,photo_50" })));
    i = i + 1;
}

var j = 0;
var result = [];
while (j < forks.length) {
    var resp = wait(forks[j]);
    delete (resp.counters);
    resp.cmid = parseInt(cmids[j]);
    result.push(resp);
    j = j + 1;
}

return result;