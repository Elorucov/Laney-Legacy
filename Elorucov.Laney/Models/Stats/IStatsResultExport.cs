using System;
using System.Threading.Tasks;

namespace Elorucov.Laney.Models.Stats {
    internal interface IStatsResultExport {
        Task<Exception> ExportAsync(string chatName, Uri chatAvatar, string miniInfo, StatsResult result);
    }
}