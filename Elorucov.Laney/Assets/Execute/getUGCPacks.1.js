var response = {
    items: []
};
var listapi = API.stickers.getUGCPackLists({ "owner_ids": parseInt(Args.owner_id) });
if (listapi.lists.length > 0) {
    var plist = listapi.lists[0];
    response.can_hide_keyboard = plist.can_hide_keyboard;
    response.is_keyboard_hidden = plist.is_keyboard_hidden;
    var ids = plist.items@.pack_id;

    // To be optimized
    var i = 0;
    while (i < ids.length) {
        var pack = API.stickers.getUGCPacks({ "owner_id": parseInt(Args.owner_id), "pack_ids": ids[i] });
        response.items.push(pack.packs[0]);
        i = i + 1;
    }
}
return response;