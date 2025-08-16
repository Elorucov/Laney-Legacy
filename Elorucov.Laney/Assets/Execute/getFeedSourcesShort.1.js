var friends = wait(fork(API.friends.get({ "fields": "sex,has_photo,photo_100", "count": 5, "order": "hints" })));
var groups = wait(fork(API.groups.get({ "extended": 1, "fields": "photo_200,wall", "count": 5 })));
var admGroups = wait(fork(API.groups.get({ "extended": 1, "fields": "photo_200", "count": 100, "filter": "admin" })));
var lists = wait(fork(API.newsfeed.getLists()));

var fc = parseInt(friends.count);
var gc = parseInt(groups.count);
var admgc = parseInt(admGroups.count);

return {
    lists: lists.items,

    friends: friends.items,
    has_more_friends: fc > 5,

    groups: groups.items,
    has_more_groups: gc > 5,

    admined_groups: admGroups.items
};