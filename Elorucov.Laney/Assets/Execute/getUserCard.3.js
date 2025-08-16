var fields = "counters,photo_id,photo_avg_color,verified,sex,bdate,city,country,occupation,has_photo,photo_50,photo_100,photo_200,online_info,domain,has_mobile,contacts,site,universities,schools,status,followers_count,activities,can_write_private_message,can_send_friend_request,is_favorite,timezone,screen_name,maiden_name,is_friend,friend_status,career,blacklisted,blacklisted_by_me,first_name_gen,first_name_acc,last_name_gen,last_name_acc";

var u = null;
if (Args.user_id) {
    u = API.users.get({ "user_ids": Args.user_id, "fields": fields })[0];
} else {
    u = API.users.get({ "fields": fields })[0];
}

// Photo
if (u.photo_id) {
    var photo = API.photos.getById({ "photos": u.photo_id, "photo_sizes": "1" });
    if (photo != null) {
        u.original_photo = photo[0];
        delete (u.photo_id);
    }
}

// Deactivation & private
if (u.deactivated == "banned") {
    u.unavailable_reason = 1;
} else if (u.deactivated == "deleted") {
    u.unavailable_reason = 2;
} else if (u.is_closed && !u.can_access_closed) {
    u.unavailable_reason = 3;
} else if (u.blacklisted == 1) {
    u.unavailable_reason = 4;
} else if (u.blacklisted_by_me == 1) {
    u.unavailable_reason = 5;
}

// App name
if (u.online_info.visible && u.online_info.app_id) {
    var oa = API.apps.get({ "app_id": u.online_info.app_id }).items[0];
    u.online_info.app_name = oa.title;
}

// Live in
if (u.city != null && u.country != null) {
    u.live_in = u.city.title + ", " + u.country.title;
}

// Current career
if (u.career.length > 0) {
    var c = u.career[u.career.length - 1];
    if (c.group_id) {
        var g = API.groups.getById({ "group_ids": c.group_id });
        c.company = g.groups[0].name;
    }
    if (!c.until) {
        u.current_career = c;
    }
} else if (u.occupation) {
    if (u.occupation.type == "work") {
        u.current_career = {
            company: u.occupation.name,
            group_id: parseInt(u.occupation.id)
        };
    }
}

//Current school/university
if (u.schools.length > 0) {
    var sc = u.schools[u.schools.length - 1];
    if (!sc.year_to) {
        u.current_education = sc.name;
    }
}
if (u.universities.length > 0) {
    var un = u.universities[u.universities.length - 1];
    if (un.graduation) {
        u.current_education = un.name;
    }
} else if (u.occupation) {
    if (u.occupation.type == "university") {
        u.current_education = u.occupation.name;
    }
}

// Messages count with this user and notifications
var msg = API.messages.getHistory({ "peer_id": u.id, "extended": 1, "count": 1 });
u.messages_count = msg.count;
u.last_cmid = u.messages_count > 0 ? msg.items[0].conversation_message_id : 0; // required for getHistoryAttachments
u.notifications_disabled = false;
if (msg.conversations[0].push_settings) {
    u.notifications_disabled = msg.conversations[0].push_settings.disabled_forever;
}

if (u.city) {
    delete (u.city);
}
if (u.country) {
    delete (u.country);
}
if (u.career.length == 0) {
    delete (u.career);
}
if (u.occupation) {
    delete (u.occupation);
}
if (u.schools) {
    delete (u.schools);
}
if (u.universities) {
    delete (u.universities);
}

u.friends_count = 0;
u.followers_count = 0;
if (u.counters) {
    if (u.counters.friends) u.friends_count = u.counters.friends;
    if (u.counters.followers) u.followers_count = u.counters.followers;
    delete (u.counters);
}

return u;