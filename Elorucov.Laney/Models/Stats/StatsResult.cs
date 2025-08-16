using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Elorucov.Laney.Models.Stats {
    public class StatsResult {
        public List<Entity> General { get; private set; } // Title у Entity — иконка для FontIcon, Subtitle — кол-во вместе с текстом
        public List<Entity<string>> Attachments { get; private set; } // Title у Entity — иконка для FontIcon, Subtitle — кол-во вместе с текстом, Object — type вложения
        public List<Entity> TopStickers { get; private set; } // Title — только кол-во
        public List<Entity> TopUGCStickers { get; private set; } // Title — только кол-во
        public List<Entity> TopReactions { get; private set; } // Id у Entity — Id реакции. Кол-во в Title.
        public List<Entity> TopWords { get; private set; } // Id у Entity — кол-во слов. Само слово в Title.

        // Какой же универсальный этот Entity, не надо плодить дофига классов, и удобно в XAML binding-ах юзать)))

        public Dictionary<string, List<Entity>> TopMembers { get; private set; } = new Dictionary<string, List<Entity>>(); // Ключ — заголовок топа. Поле Id у Entity — порядковый номер, а id участника — в Object. 
        public List<Tuple<int, int, List<Entity>>> TopMembersByReactions { get; private set; } // id, count, Entity участника (Object = URL иконки реакции)

        public string TechnicalInfo { get; private set; }

        private readonly ReadOnlyCollection<string> UselessWords = new ReadOnlyCollection<string>(new List<string> {
            "без", "безо", "близ", "в", "во", "вместо", "вне", "для", "до", "за", "из", "изо", "из-за", "из-под", "к", "ко", "кроме", "между", "меж", "на", "над", "о", "об", "обо", "от", "ото", "перед", "передо", "пред", "пред", "по", "под", "подо", "при", "про", "ради", "с", "со", "сквозь", "среди", "у", "через", "чрез", "как", "так", "даже", "ведь", "б", "бы", "не", "ни", "все", "лишь", "ли", "уж", "едва", "ах", "ой", "уф", "фу", "эй", "я", "а", "и", "или", "же", "уже", "есть", "если", "вот"
        });

        public StatsResult(List<MessageLite> messages, Dictionary<int, List<ReactedPeer>> reactedPeers, TimeSpan loadingTime, TimeSpan loadingReactionsInfoTime, bool isChat) {
            TechnicalInfo = $"{Locale.Get("stats_tech_load_time")}: {loadingTime.ToHumanizedString()}";
            if (loadingReactionsInfoTime.TotalSeconds > 1)
                TechnicalInfo += $"\n{Locale.Get("stats_tech_reactions_load_time")}: {loadingReactionsInfoTime.ToHumanizedString()}";

            Stopwatch sw = Stopwatch.StartNew();

            int count = 0;
            int uniqCount = 0;
            int reactionsTotal = 0;
            int audioDuration = 0;
            int moneySentTotal = 0;
            int expiredMessagesTotal = 0;
            Dictionary<string, int> attachmentsDict = new Dictionary<string, int>();
            int attachmentsTotal = 0;
            Dictionary<long, int> stickers = new Dictionary<long, int>();
            int stickersTotal = 0;
            List<UGCSticker> ugcStickersList = new List<UGCSticker>();
            Dictionary<long, int> ugcStickers = new Dictionary<long, int>();
            int ugcStickersTotal = 0;
            int stylesSet = 0;
            int stylesReset = 0;
            int join = 0;
            int joinByLink = 0;
            int leave = 0;
            int returns = 0;
            int kick = 0;
            int callDuration = 0;
            Dictionary<string, int> words = new Dictionary<string, int>();
            Dictionary<int, int> reactions = new Dictionary<int, int>();
            Dictionary<int, int> years = new Dictionary<int, int>();
            List<string> moneyTransferHashes = new List<string>();

            Dictionary<long, MemberCounters> memberCounters = new Dictionary<long, MemberCounters>();
            long fromUniq = 0;

            foreach (MessageLite message in messages) {
                long from = message.FromId;

                if (message.Action != null) {
                    switch (message.Action.Type) {
                        // TODO: Кол-ва смены имени и фото чата.
                        case "chat_invite_user_by_link": joinByLink++; break;
                        case "chat_invite_user":
                            if (from == message.Action.MemberId) {
                                returns++;
                            } else {
                                join++;
                            }
                            break;
                        case "chat_kick_user":
                            if (from == message.Action.MemberId) {
                                leave++;
                            } else {
                                kick++;
                            }
                            break;
                        case "conversation_style_update":
                            if (!string.IsNullOrEmpty(message.Action.Style)) {
                                stylesSet++;
                            } else {
                                stylesReset++;
                            }
                            break;
                    }
                    continue;
                }

                if (!memberCounters.ContainsKey(from)) memberCounters.Add(from, new MemberCounters(from));

                // Реакции
                if (message.Reactions != null) {
                    foreach (var reaction in message.Reactions) {
                        reactionsTotal = reactionsTotal + reaction.Count;
                        if (!reactions.ContainsKey(reaction.ReactionId)) reactions.Add(reaction.ReactionId, 0);
                        reactions[reaction.ReactionId] = reactions[reaction.ReactionId] + reaction.Count;

                        if (!reactedPeers.ContainsKey(message.ConversationMessageId)) { // Попробуем брать id-ы участников, поставившие реакции, из самого объекта сообщения (если нет в reactedPeers)
                            foreach (long mid in reaction.UserIds) {
                                if (!memberCounters.ContainsKey(mid)) memberCounters.Add(mid, new MemberCounters(mid));
                                memberCounters[mid].ReactionsTotal = memberCounters[mid].ReactionsTotal + 1;

                                if (!memberCounters[mid].Reactions.ContainsKey(reaction.ReactionId)) memberCounters[mid].Reactions.Add(reaction.ReactionId, 0);
                                memberCounters[mid].Reactions[reaction.ReactionId] = memberCounters[mid].Reactions[reaction.ReactionId] + 1;
                            }
                        }
                    }

                    if (reactedPeers.ContainsKey(message.ConversationMessageId)) { // В объекте сообщения нет id-ы участников, поставившие реакции, берём сначала из списка reactedPeers
                        foreach (var rp in reactedPeers[message.ConversationMessageId]) {
                            if (!memberCounters.ContainsKey(rp.UserId)) memberCounters.Add(rp.UserId, new MemberCounters(rp.UserId));
                            memberCounters[rp.UserId].ReactionsTotal++;

                            if (!memberCounters[rp.UserId].Reactions.ContainsKey(rp.ReactionId)) memberCounters[rp.UserId].Reactions.Add(rp.ReactionId, 0);
                            memberCounters[rp.UserId].Reactions[rp.ReactionId] = memberCounters[rp.UserId].Reactions[rp.ReactionId] + 1;
                        }
                    }
                }

                // Год
                var year = message.DateTime.Year;
                if (!years.ContainsKey(year)) years.Add(year, 0);
                years[year] = years[year] + 1;

                // Счётчик сообщений
                count++;
                memberCounters[from].Count = memberCounters[from].Count + 1;

                // Уникальные сообщения
                if (fromUniq != from) {
                    uniqCount++;
                    memberCounters[from].UniqCount = memberCounters[from].UniqCount + 1;
                    fromUniq = from;
                }

                // Исчезающие сообщения
                if (message.IsExpired || message.TTL > 0 || message.ExpireTTL > 0) expiredMessagesTotal++;

                // Слова
                if (!string.IsNullOrEmpty(message.Text)) {
                    string text = VKTextParser.GetOnlyParsedText(message.Text);
                    if (!Regex.IsMatch(text, @"/Статистика за \d+\.\d+\.\d+/")) {
                        text = text.ToLower();
                        text = Regex.Replace(text, @"/(?:(?:http|https):\/\/)?([-a-zA-Z0-9а-яА-Я.]{2,256}\.[a-zа-я]{2,8})\b(?:\/[-a-zA-Z0-9а-яА-Я@:%_+.~#?&//=]*)?/g", "");

                        var mwords = Regex.Matches(text, @"([\w'-]+)");
                        foreach (Match mword in mwords) {
                            var word = mword.Value;
                            if (UselessWords.Contains(word) || word.Length < 3) continue;
                            if (!words.ContainsKey(word)) words.Add(word, 0);
                            words[word] = words[word] + 1;
                        }
                    }
                }

                // Вложения
                if (message.Attachments != null) {
                    foreach (var attachment in message.Attachments) {
                        string type = attachment.TypeString;
                        if (type == "sticker") {
                            long sid = attachment.Sticker.StickerId;
                            if (!stickers.ContainsKey(sid)) stickers.Add(sid, 0);
                            stickers[sid] = stickers[sid] + 1;
                            stickersTotal++;
                        } else if (type == "call") {
                            callDuration = callDuration + attachment.Call.DurationSeconds;
                        } else if (type == "ugc_sticker") {
                            long usid = attachment.UGCSticker.Id;
                            if (!ugcStickers.ContainsKey(usid)) {
                                ugcStickers.Add(usid, 0);
                                ugcStickersList.Add(attachment.UGCSticker);
                            }
                            ugcStickers[usid] = ugcStickers[usid] + 1;
                            ugcStickersTotal++;
                        } else if (type == "group_call_in_progress") {
                            // Ignore and don't add this attachments to stats.
                        } else {
                            if (!attachmentsDict.ContainsKey(type)) attachmentsDict.Add(type, 0);
                            attachmentsDict[type] = attachmentsDict[type] + 1;
                            attachmentsTotal++;
                            memberCounters[from].AttachmentsTotal = memberCounters[from].AttachmentsTotal + 1;
                        }

                        // Голосовые
                        if (type == "audio_message") {
                            int d = attachment.AudioMessage.Duration;
                            audioDuration = audioDuration + d;
                            memberCounters[from].AudioDuration = memberCounters[from].AudioDuration + d;
                        }

                        // Денежный перевод
                        if (type == "link" && attachment.Link.Url.Contains("accept_money_transfer")) {
                            var link = attachment.Link;
                            var hp = Regex.Matches(link.Url, @"hash=(\w+)");
                            if (hp.Count == 1) {
                                string[] h = hp[0].Value.Split('=');
                                if (h.Length == 2 && !moneyTransferHashes.Contains(h[1])) {
                                    moneyTransferHashes.Add(h[1]);
                                    string moneystr = Regex.Match(link.Title, @"\w+")?.Value;
                                    int money = 0;
                                    if (int.TryParse(moneystr, out money)) {
                                        moneySentTotal = moneySentTotal + money;
                                        memberCounters[from].MoneySentTotal = memberCounters[from].MoneySentTotal + money;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            sw.Stop();
            TechnicalInfo += $"\n{Locale.Get("stats_tech_analyzing_time")}: {sw.ElapsedMilliseconds} ms.";

            Stopwatch sw2 = Stopwatch.StartNew();

            List<MemberCounters> memberCountersList = memberCounters.Values.ToList();
            var topByMessages = memberCountersList.OrderByDescending(mc => mc.Count).Where(mc => mc.Count > 0).Take(50).ToList(); // Сортированный по кол-ву сообщений список участников 
            var topByUniqueMessages = memberCountersList.OrderByDescending(mc => mc.UniqCount).Where(mc => mc.UniqCount > 0).Take(50).ToList(); // ...по кол-ву уникальных сообщений список участников
            var topByMoneySent = memberCountersList.OrderByDescending(mc => mc.MoneySentTotal).Where(mc => mc.MoneySentTotal > 0).Take(50).ToList(); // ...по отправленным деньгам
            var topByAttachments = memberCountersList.OrderByDescending(mc => mc.AttachmentsTotal).Where(mc => mc.AttachmentsTotal > 0).Take(50).ToList(); // ...по кол-ву отправленных вложений
            var topByReactions = memberCountersList.OrderByDescending(mc => mc.ReactionsTotal).Where(mc => mc.ReactionsTotal > 0).Take(50).ToList(); // ...по кол-ву реакций
            var topByAudioMessage = memberCountersList.OrderByDescending(mc => mc.AudioDuration).Where(mc => mc.AudioDuration > 0).Take(50).ToList(); // ...по длительности голосовых сообщений

            var attachmentsTop = attachmentsDict.OrderByDescending(a => a.Value).Take(50).ToDictionary(k => k.Key, k => k.Value); // сортируем типы вложений по кол-ву.
            var yearsTop = years.OrderByDescending(a => a.Value).Take(50).ToDictionary(k => k.Key, k => k.Value); // сортируем годы по кол-ву отправленных в году сообщений.
            var stickersTop = stickers.OrderByDescending(a => a.Value).Take(50).ToDictionary(k => k.Key, k => k.Value); // сортируем стикеры по кол-ву.
            var ugcStickersTop = ugcStickers.OrderByDescending(a => a.Value).Take(50).ToDictionary(k => k.Key, k => k.Value); // сортируем ugc-стикеры по кол-ву.
            var reactionsTop = reactions.OrderByDescending(a => a.Value).Take(50).ToDictionary(k => k.Key, k => k.Value); // сортируем реакции по кол-ву.
            var wordsTop = words.OrderByDescending(a => a.Value).Take(50).ToDictionary(k => k.Key, k => k.Value); // сортируем слова по кол-ву.

            // Part 2.

            List<Entity> general = new List<Entity>();
            if (count > 0) general.Add(new Entity(0, "", Locale.GetDeclensionForFormatSimple(count, "messages")));
            if (isChat && uniqCount > 0) general.Add(new Entity(1, "", Locale.GetDeclensionForFormatSimple(uniqCount, "unique_messages")));
            if (expiredMessagesTotal > 0) general.Add(new Entity(2, "", Locale.GetDeclensionForFormatSimple(expiredMessagesTotal, "expired_message")));
            if (reactionsTotal > 0) general.Add(new Entity(3, "", Locale.GetDeclensionForFormatSimple(reactionsTotal, "reaction")));
            if (audioDuration > 0) general.Add(new Entity(4, "", TimeSpan.FromSeconds(audioDuration).ToHumanizedString()));
            if (callDuration > 0) general.Add(new Entity(5, "", TimeSpan.FromSeconds(callDuration).ToHumanizedString()));
            if (moneySentTotal > 0) general.Add(new Entity(6, "", Locale.GetDeclensionForFormatSimple(moneySentTotal, "stats_moneysent"))); // TODO: разделитель цифр (т. е. вместо 15462 вывести 15,462)
            if (attachmentsTotal > 0) general.Add(new Entity(7, "", Locale.GetDeclensionForFormatSimple(attachmentsTotal, "attachments_f")));
            if (stickersTotal > 0) general.Add(new Entity(8, "", Locale.GetDeclensionForFormatSimple(stickersTotal, "stickers_f")));
            if (ugcStickersTotal > 0) general.Add(new Entity(9, "", Locale.GetDeclensionForFormatSimple(ugcStickersTotal, "ugc_stickers_f")));
            if (stylesSet > 0) general.Add(new Entity(10, "", Locale.GetDeclensionForFormatSimple(stylesSet, "stats_styles_set")));
            if (stylesReset > 0) general.Add(new Entity(11, "", Locale.GetDeclensionForFormatSimple(stylesReset, "stats_styles_reset")));
            if (join > 0) general.Add(new Entity(12, "", Locale.GetDeclensionForFormatSimple(join, "stats_joined")));
            if (joinByLink > 0) general.Add(new Entity(13, "", Locale.GetDeclensionForFormatSimple(joinByLink, "stats_joined_by_link")));
            if (leave > 0) general.Add(new Entity(14, "", Locale.GetDeclensionForFormatSimple(leave, "stats_left")));
            if (returns > 0) general.Add(new Entity(15, "", Locale.GetDeclensionForFormatSimple(returns, "stats_return")));
            if (kick > 0) general.Add(new Entity(16, "", Locale.GetDeclensionForFormatSimple(kick, "stats_kicked")));
            if (general.Count > 0) General = general;

            List<Entity<string>> attachments = new List<Entity<string>>();
            foreach (var a in attachmentsTop) {
                string icon = APIHelper.GetFontIconForAttachment(a.Key);
                attachments.Add(new Entity<string>(a.Key, a.Value, icon, $"{a.Value} {APIHelper.GetHumanReadableAttachmentName(a.Key, a.Value)}"));
            }
            if (attachments.Count > 0) Attachments = attachments;

            List<Entity> topStickers = new List<Entity>();
            foreach (var s in stickersTop) {
                Uri uri = new Uri($"https://vk.com/sticker/1-{s.Key}-128");
                topStickers.Add(new Entity(s.Key, s.Value.ToString(), null, uri));
            }
            if (topStickers.Count > 0) TopStickers = topStickers;

            List<Entity> topUGCStickers = new List<Entity>();
            foreach (var s in ugcStickersTop) {
                UGCSticker sticker = ugcStickersList.Where(u => u.Id == s.Key).FirstOrDefault();
                if (sticker != null && sticker.Images.Count > 0) {
                    topUGCStickers.Add(new Entity(s.Key, s.Value.ToString(), null, sticker.Images.FirstOrDefault()?.Uri));
                }
            }
            if (topUGCStickers.Count > 0) TopUGCStickers = topUGCStickers;

            List<Entity> topWords = new List<Entity>();
            foreach (var t in wordsTop) {
                topWords.Add(new Entity(t.Value, t.Key, null));
            }
            if (topWords.Count > 0) TopWords = topWords;


            // Топы участников.
            if (topByMessages.Count > 0) {
                var topMembersByMessages = GenerateTopMembers2(topByMessages, m => m.Count, "messages");
                TopMembers.Add(Locale.Get("message_gen"), topMembersByMessages);
            }

            if (isChat && topByUniqueMessages.Count > 0) {
                var topMembersByUniqueMessages = GenerateTopMembers2(topByUniqueMessages, m => m.UniqCount, "unique_messages");
                TopMembers.Add(Locale.Get("stats_tmsection_unique"), topMembersByUniqueMessages);
            }

            if (topByMoneySent.Count > 0) {
                var topMembersByMoneySent = GenerateTopMembers(topByMoneySent, m => m.MoneySentTotal, "RUB.");
                TopMembers.Add(Locale.Get("stats_tmsection_moneysent"), topMembersByMoneySent);
            }

            if (topByAttachments.Count > 0) {
                var topMembersByAttachments = GenerateTopMembers2(topByAttachments, m => m.AttachmentsTotal, "attachments_f");
                TopMembers.Add(Locale.Get("attachments"), topMembersByAttachments);
            }

            if (topByAudioMessage.Count > 0) {
                var topMembersByAudioMessages = GenerateTopMembers(topByAudioMessage, m => TimeSpan.FromSeconds(m.AudioDuration).ToHumanizedString(), "");
                TopMembers.Add(Locale.Get("stats_tmsection_audiomsgs"), topMembersByAudioMessages);
            }

            // Топы по конкретным реакциям
            if (reactionsTop.Count > 0) {
                TopMembersByReactions = new List<Tuple<int, int, List<Entity>>>();
                var allEntities = GenerateTopMembers2(topByReactions, m => m.ReactionsTotal, "reaction");
                Tuple<int, int, List<Entity>> topMembersByAllReaction = new Tuple<int, int, List<Entity>>(0, reactionsTotal, allEntities);
                TopMembersByReactions.Add(topMembersByAllReaction);

                var rids = reactionsTop.Keys.ToList();
                foreach (int rid in rids) {
                    var top = memberCountersList.Where(mc => mc.Reactions.ContainsKey(rid)).OrderByDescending(mc => mc.Reactions[rid]).Take(50).ToList();

                    if (top.Count > 0) {
                        List<Entity> entities = GenerateTopMembers2(top, m => m.Reactions[rid], "reaction");
                        Tuple<int, int, List<Entity>> topMembersByReaction = new Tuple<int, int, List<Entity>>(rid, reactionsTop[rid], entities);
                        TopMembersByReactions.Add(topMembersByReaction);
                    }
                }
            }

            sw2.Stop();
            TechnicalInfo += $"\n{Locale.Get("stats_tech_creating_time")}: {sw2.ElapsedMilliseconds} ms.";
            Log.Info($"Technical info about message stats\nMessages count: {count}\n{TechnicalInfo}");
        }

        private List<Entity> GenerateTopMembers(List<MemberCounters> membersList, Func<MemberCounters, object> prop, string suffix) {
            var topMembers = new List<Entity>();
            int i = 1;
            foreach (var m in membersList) {
                var counter = prop.Invoke(m);
                var info = AppSession.GetNameAndAvatarFromLiteCache(m.MemberId);
                string name = info != null ? String.Join(" ", new List<string> { info.Item1, info.Item2 }) : $"Member {m.MemberId}";
                Uri avatar = info != null ? info.Item3 : null;
                string subtitle = $"{counter} {suffix}";
                topMembers.Add(new Entity(i, name, subtitle, avatar) { Object = m.MemberId });
                i++;
            }
            return topMembers;
        }

        private List<Entity> GenerateTopMembers2(List<MemberCounters> membersList, Func<MemberCounters, decimal> prop, string subtitleLocaleKey) {
            var topMembers = new List<Entity>();
            int i = 1;
            foreach (var m in membersList) {
                var counter = prop.Invoke(m);
                var info = AppSession.GetNameAndAvatarFromLiteCache(m.MemberId);
                string name = info != null ? String.Join(" ", new List<string> { info.Item1, info.Item2 }) : $"Member {m.MemberId}";
                Uri avatar = info != null ? info.Item3 : null;
                string subtitle = Locale.GetDeclensionForFormatSimple(counter, subtitleLocaleKey);
                topMembers.Add(new Entity(i, name, subtitle, avatar) { Object = m.MemberId });
                i++;
            }
            return topMembers;
        }
    }
}