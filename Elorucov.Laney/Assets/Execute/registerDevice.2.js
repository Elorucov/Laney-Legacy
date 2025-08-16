var p = 0;
var u = 0;

var pnstype = parseInt(Args.push_type);
u = API.account.unregisterDevice({ "device_id": Args.device_id });
if (pnstype == 1) {
    p = API.account.registerDevice({ "token": Args.token, "device_id": Args.device_id });
}
if (!u) {
    u = 0;
}
if (!p) {
    p = 0;
}
return {
    unregistered: u,
    registered: p
};