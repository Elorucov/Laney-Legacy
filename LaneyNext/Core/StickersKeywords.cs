using ELOR.VKAPILib.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation.Collections;

namespace Elorucov.Laney.Core
{
    public class StickersKeywords
    {
        private static Dictionary<string, List<Sticker>> LocalDictionary = new Dictionary<string, List<Sticker>>();

        public static void InitDictionary(List<StickerDictionary> dicts)
        {
            LocalDictionary.Clear();
            if (dicts == null)
            {
                Log.General.Info("Keywords is null for unknown reasons!");
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            foreach (var dict in dicts)
            {
                foreach (string word in dict.Words)
                {
                    if (LocalDictionary.ContainsKey(word))
                    {
                        LocalDictionary[word].AddRange(dict.Stickers);
                    }
                    else
                    {
                        LocalDictionary.Add(word, dict.Stickers);
                    }
                }
            }
            sw.Stop();
            Log.General.Info("Keywords loaded.", new ValueSet { { "words", LocalDictionary.Count }, { "elapsed", sw.ElapsedMilliseconds } });
        }

        public static List<Sticker> GetStickersByWord(string word)
        {
            if (String.IsNullOrEmpty(word)) return null;
            word = word.ToLower();
            if (LocalDictionary.ContainsKey(word))
            {
                return LocalDictionary[word];
            }
            return null;
        }
    }
}
