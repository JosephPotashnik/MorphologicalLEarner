using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace MorphologicalLearner
{
    public class BigramManager
    {
        public enum LookupDirection
        {
            LookToLeft,
            LookToRight
        };

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
            return (Exists(word1, word2)
                ? firstWordDictionary[word1][word2]
                : 0);
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
                        list.Add(
                            new KeyValuePair<KeyValuePair<string, string>, int>(
                                new KeyValuePair<string, string>(word1, word2), count));
                    }
                }
            }
            return list;
        }

        public IEnumerable<string> GetAllWordsBeforeWord(string secondword)
        {
            return (secondWordDictionary.ContainsKey(secondword)
                ? secondWordDictionary[secondword].Keys
                : Enumerable.Empty<string>());
        }

        private IEnumerable<string> GetAllWordsAfterWord(string firstword)
        {
            return (firstWordDictionary.ContainsKey(firstword)
                ? firstWordDictionary[firstword].Keys
                : Enumerable.Empty<string>());
        }

        public IEnumerable<string> IntersectTwoWords(string word1, string word2, LookupDirection dir)
        {
            if (dir == LookupDirection.LookToRight)
                return IntersectTwoFirstWords(word1, word2);
            return IntersectTwoSecondWords(word1, word2);
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

        /*public class CommonNeighbors
        {
            public List<CommonNeighborsEntry> Entries { get; set; }
            public LookupDirection Direction { get; set; }
        }*/
               
        [Serializable()]
        public class NeighborsOfWord
        {
            public string Word { get; set; }
            public int Count { get; set; }
            public CommonNeighborsEntry[] ListOfNeighbors { get; set; } 

        }
        [Serializable()]
        public class CommonNeighborsEntry
        {
            public string Word1 { get; set; }
            public string Word2 { get; set; }
            public int Count { get; set; }
            public string[] CommonNeighbors { get; set; }
        }

        public void ComputeAllCommonNeighbors(LookupDirection direction, string fileName)
        {
            //get all first words.
            string[] words;
            if (direction == LookupDirection.LookToRight)
                words = firstWordDictionary.Keys.ToArray();
            else
                words = secondWordDictionary.Keys.ToArray();

            Dictionary<string, Dictionary<string, int>> dic = new Dictionary<string, Dictionary<string, int>>();
            int i = 0;
            foreach (var word1 in words)
            {
                i++;
                NeighborsOfWord entriesForWord1 = new NeighborsOfWord();
                entriesForWord1.Word = word1;
                var list = new List<CommonNeighborsEntry>();
                Console.WriteLine("writing common neighbors for all neighbors of {0}", word1);
                Console.WriteLine("{0} out of {1}", i.ToString(), words.Count().ToString());

                foreach (var word2 in words)
                {

                    if (word1 == word2) continue;

                    //if we already scanned these words in the opposite order, skip.
                    if (dic.ContainsKey(word2) && dic[word2].ContainsKey(word1))
                        continue;

                    if (!dic.ContainsKey(word1))
                        dic[word1] = new Dictionary<string, int>();

                    //push into dictionary to keep track of scanned pairs.
                    dic[word1][word2] = 1;

                    CommonNeighborsEntry entry = new CommonNeighborsEntry { Word1 = word1, Word2 = word2 };

                    //compute common neighbors
                    entry.CommonNeighbors = IntersectTwoWords(word1, word2, direction).ToArray();
                    entry.Count = entry.CommonNeighbors.Count();

                    //if no common neighbors, don't write
                    if (entry.Count > 0)
                        list.Add(entry);
                }

                entriesForWord1.Count = list.Count;
                entriesForWord1.ListOfNeighbors = list.ToArray();

                WriteToNeighborFile(fileName, entriesForWord1);


                //the next code is possibly useless. 
                {
                    list.Clear();
                    list = null;

                    entriesForWord1.ListOfNeighbors = null;
                    entriesForWord1 = null;
                }

            }
        }

        private static void WriteToNeighborFile(string fileName, NeighborsOfWord entriesForWord1)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Append))
            {
                var bFormatter = new BinaryFormatter();
                bFormatter.Serialize(fileStream, entriesForWord1);
            }
        }
    }
}