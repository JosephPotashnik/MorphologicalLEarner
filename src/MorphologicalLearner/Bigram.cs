using System.Collections.Generic;

namespace MorphologicalLearner
{

    public class BigramManager
    {
        private Dictionary<string, Dictionary<string, int>> firstWordDictionary;
        private Dictionary<string, Dictionary<string, int>> secondWordDictionary;

        public BigramManager()
        {
            firstWordDictionary = new Dictionary<string, Dictionary<string, int>>();
            secondWordDictionary = new Dictionary<string, Dictionary<string, int>>();
        }

        public void Add(string word1, string word2)
        {

            //the first dictionary lists the first word as a key, with all bigrams for which it is the first word.
            if (firstWordDictionary.ContainsKey(word1))
            {
                Dictionary<string, int> val = firstWordDictionary[word1];
                if (val.ContainsKey(word2))
                    val[word2]++;
                else
                    val[word2] = 1;
            }
            else
            {
                Dictionary<string, int> newDic = new Dictionary<string, int>();
                firstWordDictionary[word1] = newDic;
                newDic[word2] = 1;
            }

            //the second dictionary lists the second word as a key, with all bigrams for which it is the second word
            if (secondWordDictionary.ContainsKey(word2))
            {
                Dictionary<string, int> val = secondWordDictionary[word2];
                if (val.ContainsKey(word1))
                    val[word1]++;
                else
                    val[word1] = 1;
            }
            else
            {
                Dictionary<string, int> newDic = new Dictionary<string, int>();
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

        public List<KeyValuePair<KeyValuePair<string, string>, int>> AllWordsAboveNOccurences(int n)
        {
            List<KeyValuePair<KeyValuePair<string, string>, int>> list =
                new List<KeyValuePair<KeyValuePair<string, string>, int>>();

            foreach (string word1 in firstWordDictionary.Keys)
            {
                var innerDic = firstWordDictionary[word1];
                foreach (string word2 in innerDic.Keys)
                {
                    int count = innerDic[word2];
                    if (count > n)
                    {
                        list.Add(new KeyValuePair<KeyValuePair<string, string>, int>(new KeyValuePair<string, string>(word1, word2), count));
                    }
                }
            }
            return list;
        }


    }

}
