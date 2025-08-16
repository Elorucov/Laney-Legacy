var fields = Args.fields;
var q = Args.query;
var forks = {
    'users.search': fork(API.users.search({ "q": q, "count": 50, "extended": "1", "fields": fields })),
    'groups.search': fork(API.groups.search({ "q": q, "count": 50, "extended": "1", "fields": fields }))
};
var su = wait(forks['users.search']);
var sg = wait(forks['groups.search']);
var response = {
    'users': su.items,
    'groups': sg.items
};
return response;