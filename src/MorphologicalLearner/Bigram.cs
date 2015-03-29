using System.Collections.Generic;
using System.Linq;

namespace MorphologicalLearner
{

    public class BigramManager
    {
        //the first dictionary lists the first word as a key, with all bigrams for which it is the first word.
        private readonly Dictionary<string, Dictionary<string, int>> firstWordDictionary;
                          //<word1,                   <word2, count>>
        //the second dictionary lists the second word as a key, with all bigrams for which it is the second word
        private readonly Dictionary<string, Dictionary<string, int>> secondWordDictionary;
                        //<word2,                     <word1, count>>

        public BigramManager()
        {
            firstWordDictionary = new Dictionary<string, Dictionary<string, int>>();
            secondWordDictionary = new Dictionary<string, Dictionary<string, int>>();
        }

        public void Add(string word1, string word2)
        {

            if (firstWordDictionary.ContainsKey(word1))
            {
                var val = firstWordDictionary[word1];
                if (val.ContainsKey(word2))
                    val[word2]++;   
                else
                    val[word2] = 1;
            }
            else
            {
                var newDic = new Dictionary<string, int>();
                firstWordDictionary[word1] = newDic;
                newDic[word2] = 1;
            }

            if (secondWordDictionary.ContainsKey(word2))
            {
                var val = secondWordDictionary[word2];
                if (val.ContainsKey(word1))
                    val[word1]++;
                else
                    val[word1] = 1;
            }
            else
            {
                var newDic = new Dictionary<string, int>();
                secondWordDictionary[word2] = newDic;
                newDic[word1] = 1;
            }
        }

        public bool Exists(string word1, string word2)
        {
            return (firstWordDictionary.ContainsKey(word1) &&
                    firstWordDictionary[word1].ContainsKey(word2));
        }

        public int Count(string word1, string word2)
        {
            return (Exists(word1, word2) ?
                firstWordDictionary[word1][word2] :
            0);
        }

        public List<KeyValuePair<KeyValuePair<string, string>, int>> BigramsAboveCountThresholdN(int n)
        {
            var list = new List<KeyValuePair<KeyValuePair<string, string>, int>>();

            foreach (var word1 in firstWordDictionary.Keys)
            {
                var innerDic = firstWordDictionary[word1];
                foreach (var word2 in innerDic.Keys)
                {
                    var count = innerDic[word2];
                    if (count >= n)
                    {
                        list.Add(new KeyValuePair<KeyValuePair<string, string>, int>(new KeyValuePair<string, string>(word1, word2), count));
                    }
                }
            }
            return list;
        }

        public IEnumerable<string> GetAllWordsBeforeWord(string secondword)
        {
            return ( secondWordDictionary.ContainsKey(secondword) ?
                 secondWordDictionary[secondword].Keys :
                 Enumerable.Empty<string>());
        }

        private IEnumerable<string> GetAllWordsAfterWord(string firstword)
        {
            return (firstWordDictionary.ContainsKey(firstword) ?
                 firstWordDictionary[firstword].Keys :
                 Enumerable.Empty<string>());
        }

        public IEnumerable<string> IntersectTwoFirstWords(string firstWord1, string firstWord2)
        {
            return GetAllWordsAfterWord(firstWord1).Intersect(GetAllWordsAfterWord(firstWord2));
        }

        public IEnumerable<string> IntersectTwoSecondWords(string secondWord1, string secondWord2)
        {
            return GetAllWordsBeforeWord(secondWord1).Intersect(GetAllWordsBeforeWord(secondWord2));
        }

        public IEnumerable<string> GetIntersectOfBigramsWithFirstWords(IEnumerable<string> given)
        {
            var l = GetAllWordsAfterWord(given.First());
            return given.Aggregate(l, (current, str) => current.Intersect(GetAllWordsAfterWord(str)));
        }

        public IEnumerable<string> GetIntersectOfBigramsWithSecondWords(IEnumerable<string> given)
        {
            var l = GetAllWordsBeforeWord(given.First());
            return given.Aggregate(l, (current, str) => current.Intersect(GetAllWordsBeforeWord(str)));
        }

        public IEnumerable<string> GetUnionOfBigramsWithFirstWords(IEnumerable<string> given)
        {
            var l = GetAllWordsAfterWord(given.First());
            return given.Aggregate(l, (current, str) => current.Union(GetAllWordsAfterWord(str)));
        }

        public IEnumerable<string> GetUnionOfBigramsWithSecondWords(IEnumerable<string> given)
        {
            var l = GetAllWordsBeforeWord(given.First());
            return given.Aggregate(l, (current, str) => current.Union(GetAllWordsBeforeWord(str)));
        }
    }
}
