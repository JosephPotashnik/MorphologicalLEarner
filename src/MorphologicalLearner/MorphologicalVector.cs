using System.Collections.Generic;
using System.Linq;

namespace MorphologicalLearner
{
    //this class holds all the words that share a common paradigm appearing in the corpus.
    //(e.g. "mix", "mix-ed", "mix-ing", "pass", "pass-ed", "pass-ing", etc).
    public class MorphologicalVector
    {
        private readonly List<string> suffixes;

        //all words                 //word  //the suffix array index (e.g. "-ed", could be index 2, "-ing" index 3..)
        private readonly Dictionary<string, int> words;

        //words of a given suffix. int = suffix index.
        private readonly Dictionary<int, List<string>> wordsOfSuffix;

        public MorphologicalVector()
        {
            words = new Dictionary<string, int>();
            suffixes = new List<string>();
            wordsOfSuffix = new Dictionary<int, List<string>>();
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

        public IEnumerable<string> WordsOfSuffix(int SuffixIndex)
        {
            return (wordsOfSuffix.ContainsKey(SuffixIndex)
                ? wordsOfSuffix[SuffixIndex]
                : Enumerable.Empty<string>());
        }

        public int Count()
        {
            return words.Keys.Count;
        }

        public int GetSuffixIndex(string word)
        {
            return words[word];
        }

        public void AddWord(string word, int suffixIndex)
        {
            if (!words.ContainsKey((word)))
            {
                words[word] = suffixIndex;

                if (!wordsOfSuffix.ContainsKey(suffixIndex))
                    wordsOfSuffix[suffixIndex] = new List<string>();

                wordsOfSuffix[suffixIndex].Add(word);
            }
        }
    }
}