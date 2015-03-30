using System.Collections.Generic;

namespace MorphologicalLearner
{
    public class MorphologicalBucket
    {
        private readonly List<string> suffixes;
        private readonly Dictionary<string, int> words;

        public MorphologicalBucket()
        {
            words = new Dictionary<string, int>();
            suffixes = new List<string>();
        }

        public void AddSuffix(string suffix)
        {
            if (!suffixes.Contains(suffix))
                suffixes.Add(suffix);
        }

        public IEnumerable<string> Suffixes()
        {
            return suffixes;
        }

        public IEnumerable<string> Words()
        {
            return words.Keys;
        }

        public int Count()
        {
            return words.Keys.Count;
        }

        public void Add(string word)
        {
            if (!words.ContainsKey((word)))
                words[word] = 1;
        }
    }
}