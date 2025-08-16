using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace Elorucov.Laney.Models.Stats {
    public class HTMLStatsResultExport : IStatsResultExport {
        string mainTemplate = null;
        string groupTemplate = "<div id=\"{{id}}\" class=\"group{{isFW}}\"><header>{{header}}</header>{{content}}</div>";
        string groupGVTemplate = "<div id=\"{{id}}\" class=\"group{{isFW}}\"><header>{{header}}</header><div class=\"gridView\">{{content}}</div></div>";
        string groupTabsTemplate = "<div id=\"{{id}}\" class=\"group{{isFW}}\"><header>{{header}}</header><div class=\"tabs\">{{content}}</div></div>";
        string cellTemplate = "<div class=\"cell\">{{left}}{{content}}</div>";
        string cellLeftIconTemplate = "<div class=\"left icon\"><svg width=\"24\" height=\"24\" viewBox=\"0 0 24 24\"><use xlink:href=\"#{{icon}}\"/></svg></div>";
        string cellLeftAvaTemplate = "<div class=\"left\"><div class=\"ava a48\"><img src=\"{{imageUrl}}\" width=\"48\" height=\"48\"/></div></div>";
        string cellMidTemplate = "<div class=\"cellMid\"><span>{{title}}<span class=\"position\">{{pos}}</span></span><h4>{{sub}}</h4></div>";
        string sectionWithGroupTemplate = "<section id=\"{{id}}\"><div class=\"sectionInner\">{{group}}</div></section>";
        string stickersSectionWithGroupTemplate = "<section id=\"{{id}}\" class=\"stickersBase\"><div class=\"sectionInner\">{{group}}</div></section>";
        string stickerGVITemplate = "<div class=\"gridViewItem\"><img src=\"{{imageUrl}}\"/><p>{{count}}</p></div>";
        string wordGVITemplate = "<span class=\"gridViewItem\">{{word}}<span class=\"counter\">{{count}}</span></span>";
        string tabTemplate = "<input type=\"radio\" name=\"{{tabsId}}\" id=\"{{tabId}}\"{{checked}}/><label for=\"{{tabId}}\">{{name}}</label><div class=\"tab\">{{content}}</div>";

        public async Task<Exception> ExportAsync(string chatName, Uri chatAvatar, string miniInfo, StatsResult result) {
            try {
                string fileName = $"{chatName} — {Locale.Get("message_stats")} — Laney";

                var tfile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/MessageStats/template.html"));
                mainTemplate = await FileIO.ReadTextAsync(tfile);

                string resultsHTML = mainTemplate.TReplace("lang", Locale.Get("lang"))
                    .TReplace("pageTitle", fileName)
                    .TReplace("chatName", chatName)
                    .TReplace("chatAvatar", chatAvatar.AbsoluteUri)
                    .TReplace("chatMiniStat", miniInfo)
                    .TReplace("results", BuildCountersGroup(Locale.Get("stats_results/Text"), result.General))
                    .TReplace("attachments", BuildAttachmentsTopGroup(Locale.Get("stats_attachments/Text"), result.Attachments))
                    .TReplace("stickers", BuildStickersSection("stickers", Locale.Get("stats_top_stickers/Text"), result.TopStickers))
                    .TReplace("ugc_stickers", BuildStickersSection("UGCStickers", Locale.Get("stats_top_ugc_stickers/Text"), result.TopUGCStickers))
                    .TReplace("top_members", BuildTopMembersGroup("topMembersCounters", Locale.Get("stats_top_members/Text"), result.TopMembers))
                    .TReplace("top_members_by_reactions", BuildTopMembersGroup("topMembersByReactions", Locale.Get("stats_top_members_by_reactions/Text"), result.TopMembersByReactions))
                    .TReplace("top_words", BuildWordsSection("topWords", Locale.Get("stats_top_words/Text"), result.TopWords))
                    .TReplace("copyright", $"{Locale.Get("stats_html_copyright")} ({ApplicationInfo.GetVersion(true)})")
                    .TReplace("creation_date", $"{DateTime.Now.ToString("M")} {DateTime.Now.Year}.");

                string integrity = Functions.GetSHA256(resultsHTML + "LANEY");
                resultsHTML = resultsHTML.Insert(0, $"<!-- SHA256 {integrity} -->");

                FolderPicker fsp = new FolderPicker();
                fsp.FileTypeFilter.Add(".html");
                fsp.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                var folder = await fsp.PickSingleFolderAsync();
                if (folder != null) {
                    string safeFileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace("statsexport", folder);
                    var sfile = await folder.CreateFileAsync(safeFileName + ".html", CreationCollisionOption.GenerateUniqueName);
                    if (sfile != null) await FileIO.WriteTextAsync(sfile, resultsHTML);
                } else {
                    throw new Exception("No folder choosen!");
                }

                return null;
            } catch (Exception ex) {
                return ex;
            }
        }

        private string BuildCountersGroup(string header, List<Entity> entities) {
            if (entities == null || entities.Count == 0) return string.Empty;
            string content = string.Empty;

            foreach (Entity e in entities) {
                string icon = cellLeftIconTemplate.TReplace("icon", GetSVGIconIdForCounterId(e.Id));
                string cell = cellTemplate.TReplace("left", icon).TReplace("content", e.Subtitle);
                content += cell;
            }

            return groupTemplate.TReplace("id", "counters")
                .TReplace("isFW", "")
                .TReplace("header", header)
                .TReplace("content", content);
        }

        private string BuildAttachmentsTopGroup(string header, List<Entity<string>> entities) {
            if (entities == null || entities.Count == 0) return string.Empty;
            string content = string.Empty;

            foreach (Entity<string> e in entities) {
                string type = e.Object;
                switch (e.Object) {
                    case "curator": type = "artist"; break;
                    case "textpost_publish": type = "textlive"; break;
                }

                string icon = cellLeftIconTemplate.TReplace("icon", type);
                string cell = cellTemplate.TReplace("left", icon).TReplace("content", e.Subtitle);
                content += cell;
            }

            return groupTemplate.TReplace("id", "topAttachments")
                .TReplace("isFW", "")
                .TReplace("header", header)
                .TReplace("content", content);
        }

        private string BuildTopMembersGroup(string id, string header, Dictionary<string, List<Entity>> rTabs) {
            if (rTabs == null || rTabs.Count == 0) return string.Empty;
            string content = string.Empty;

            bool first = true;
            foreach (var rTab in rTabs) {
                string tabContent = string.Empty;
                foreach (var e in rTab.Value) {
                    string mid = cellMidTemplate.TReplace("title", e.Title)
                        .TReplace("pos", $"#{e.Id}")
                        .TReplace("sub", e.Subtitle);

                    string icon = cellLeftAvaTemplate.TReplace("imageUrl", e.Image.AbsoluteUri);
                    string cell = cellTemplate.TReplace("left", icon).TReplace("content", mid);
                    tabContent += cell;
                }

                content += tabTemplate.TReplace("tabsId", id)
                    .TReplace("tabId", Functions.GetSHA256(rTab.Key))
                    .TReplace("checked", first ? " checked" : "")
                    .TReplace("name", rTab.Key)
                    .TReplace("content", tabContent);

                first = false;
            }

            return groupTabsTemplate.TReplace("id", id + "Group")
                .TReplace("isFW", "")
                .TReplace("header", header)
                .TReplace("content", content);
        }

        private string BuildTopMembersGroup(string id, string header, List<Tuple<int, int, List<Entity>>> rTabs) {
            if (rTabs == null || rTabs.Count == 0) return string.Empty;
            string content = string.Empty;

            bool first = true;
            foreach (var rTab in rTabs) {
                string tabContent = string.Empty;
                foreach (var e in rTab.Item3) {
                    string mid = cellMidTemplate.TReplace("title", e.Title)
                        .TReplace("pos", $"#{e.Id}")
                        .TReplace("sub", e.Subtitle);

                    string icon = cellLeftAvaTemplate.TReplace("imageUrl", e.Image?.AbsoluteUri);
                    string cell = cellTemplate.TReplace("left", icon).TReplace("content", mid);
                    tabContent += cell;
                }

                var reactionAssetUrl = Reaction.GetImagePathById(rTab.Item1, true)?.AbsoluteUri;
                var imageTmp = $"<img src=\"{reactionAssetUrl}\" width=\"22\" height=\"22\"/>";
                var textAllTmp = $"<span style=\"margin-right: 4px; font-weight: 500\">{Locale.Get("all")}</span>";
                var textTmp = $"<span style=\"margin-right: 4px; font-weight: 500\">ID{rTab.Item1}</span>";

                var tabText = string.IsNullOrEmpty(reactionAssetUrl) ? textTmp : imageTmp;
                tabText = rTab.Item1 == 0 ? textAllTmp : tabText;
                string tabHeader = $"{tabText}{rTab.Item2}";


                content += tabTemplate.TReplace("tabsId", id)
                    .TReplace("tabId", $"r{rTab.Item1}")
                    .TReplace("checked", first ? " checked" : "")
                    .TReplace("name", tabHeader)
                    .TReplace("content", tabContent);

                first = false;
            }

            return groupTabsTemplate.TReplace("id", id + "Group")
                .TReplace("isFW", "")
                .TReplace("header", header)
                .TReplace("content", content);
        }

        private string BuildStickersSection(string id, string header, List<Entity> entities) {
            if (entities == null || entities.Count == 0) return string.Empty;
            string content = string.Empty;

            foreach (Entity e in entities) {
                string gvi = stickerGVITemplate.TReplace("imageUrl", e.Image.AbsoluteUri).TReplace("count", e.Title);
                content += gvi;
            }

            string group = groupGVTemplate.TReplace("id", id + "G")
                .TReplace("isFW", " fullWidth")
                .TReplace("header", header)
                .TReplace("content", content);

            return stickersSectionWithGroupTemplate.TReplace("id", id)
                .TReplace("group", group);
        }

        private string BuildWordsSection(string id, string header, List<Entity> entities) {
            if (entities == null || entities.Count == 0) return string.Empty;
            string content = string.Empty;

            foreach (Entity e in entities) {
                string gvi = wordGVITemplate.TReplace("word", e.Title).TReplace("count", e.Id.ToString());
                content += gvi;
            }

            string group = groupGVTemplate.TReplace("id", id + "G")
                .TReplace("isFW", " fullWidth")
                .TReplace("header", header)
                .TReplace("content", content);

            return sectionWithGroupTemplate.TReplace("id", id)
                .TReplace("group", group);
        }

        //

        private string GetSVGIconIdForCounterId(long id) {
            switch (id) {
                case 0: return "messages";
                case 1: return "reply_messages";
                case 2: return "bomb";
                case 3: return "reactions";
                case 4: return "audio_message";
                case 5: return "calls";
                case 6: return "money_transfer";
                case 7: return "attachment";
                case 8: return "sticker";
                case 9: return "ugc_sticker";
                case 10: return "palette";
                case 11: return "palette_slash";
                case 12: return "join";
                case 13: return "link";
                case 14: return "leave";
                case 15: return "join";
                case 16: return "kick";
                default: return "wall";
            }
        }
    }
}
