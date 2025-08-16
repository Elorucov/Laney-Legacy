var ownerId = parseInt(API.users.get()[0].id);

var forks = {
    'audio.get': fork(API.audio.get({ "count": 500 })),
    'audio.getPlaylists': fork(API.audio.getPlaylists({ "owner_id": ownerId, "count": 200 })),
};
var response = {
    'playlists': wait(forks['audio.getPlaylists']),
    'audios': wait(forks['audio.get']),
};

return response;