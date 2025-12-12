var a = API.stats.trackVisitor();
var b = API.account.setOnline({ "voip": 1 });
var cu = API.users.get()[0];

var uids = Args.user_ids;

var abuild = parseInt(Args.app_build);
var obuild = parseInt(Args.os_build);
var ulang = Args.user_lang;

var lpres = API.messages.getLongPollServer({ "need_pts": 1, "lp_version": 19 });
var uids = API.users.get({ "user_ids": uids, "fields": "has_photo,photo_200,photo_100,photo_50,sex,contacts,connections" });
var acc = API.account.getInfo({ "fields": "messages_translation_language_pairs" });
var ra = API.messages.getReactionsAssets();
var et = API.auth.getExchangeToken();

var pnsresult = 0;
var p = 0;
var u = 0;

var pst = API.account.getPushSettings({ "device_id": Args.device_id });
var isPushDisabled = pst.disabled == 1;
var pnstype = parseInt(Args.push_type);
if (pnstype == 1) {
    u = API.account.unregisterDevice({ "device_id": Args.device_id });
    if (isPushDisabled) {
        p = API.account.registerDevice({ "token": Args.token, "device_id": Args.device_id, "settings": "{\"msg\":\"on\", \"chat\":\"on\"}" });
    } else {
        p = API.account.registerDevice({ "token": Args.token, "device_id": Args.device_id });
    }
}

// Response
var resp = {};

if (et) {
    resp.exchange_token = et.users_exchange_tokens[0].common_token;
}

resp.trackVisitorResult = !a ? 0 : a;
resp.setOnlineResult = !p ? 0 : b;
resp.unregisterDeviceResult = !u ? 0 : u;
resp.registerDeviceResult = !p ? 0 : p;
resp.push_settings = { msg: ["on"], chat: ["on"] };
if (!pst.disabled) resp.push_settings = pst.settings;
resp.longpoll = lpres;
resp.users = uids;
resp.messages_translation_language_pairs = acc.messages_translation_language_pairs;
resp.ctl_source = "https://elorucov.github.io/laney/v2/chat_styles.json";
resp.alt_emoji_source = "https://elorucov.github.io/laney/v1/emoji_fonts/apple.ttf";
resp.special_events = [];
resp.reactions_assets = ra.assets;
resp.available_reactions = ra.reaction_ids;
resp.queue_config = API.queue.subscribe({ "queue_ids": "onlfriends_" + cu.id });
if (!resp.queue_config) resp.queue_config = null;

// Stickers
var products = API.store.getProducts({ "type": "stickers", "filters": "active", "merchant": "microsoft" });
var productIds = products.items@.id;
resp.sticker_product_ids = productIds;

var is_desktop = Args.device_type == "Windows.Desktop";
var can_insider = 0;
var cuid = cu.id;

var debugmenu = {
    id: 666,
    title: "Debug",
    link: "lny://debug/",
    is_internal: false,
    icon: "",
};
resp.special_events.push(debugmenu);

return resp;