using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Store {
        public static async Task<object> GetProducts(string type, string filters, bool extended) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "type", type },
                { "filters", filters },
                { "extended", extended ? "1" : "0" }
            };

            var res = await API.SendRequestAsync("store.getProducts", req);
            return VKResponseHelper.ParseResponse<VKList<StoreProduct>>(res);
        }

        public static async Task<object> GetStickersKeywords(int chunk, string hash, bool allProducts) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "aliases", "1" },
                { "need_stickers", "1" }
            };
            if (allProducts) req.Add("all_products", "1");
            req.Add("chunk", chunk.ToString());
            if (!string.IsNullOrEmpty(hash)) req.Add("hash", hash);

            var res = await API.SendRequestAsync("store.getStickersKeywords", req);
            return VKResponseHelper.ParseResponse<StickersKeywordsResponse>(res);
        }

        public static async Task<object> GetStickerKeywords(long stickersIds) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "aliases", "1" },
                { "stickers_ids", stickersIds.ToString() }
            };

            var res = await API.SendRequestAsync("store.getStickersKeywords", req);
            return VKResponseHelper.ParseResponse<StickersKeywordsResponse>(res);
        }

        public static async Task<object> GetStockItemByProductId(string type, long productId) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "type", type },
                { "product_id", productId.ToString() }
            };

            var res = await API.SendRequestAsync("store.getStockItemByProductId", req);
            return VKResponseHelper.ParseResponse<StockItem>(res);
        }
    }
}