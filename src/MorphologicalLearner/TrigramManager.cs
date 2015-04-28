using System.Collections.Generic;
using System.Linq;

namespace MorphologicalLearner
{
    public class TrigramManager
    {

        //the first dictionary lists the first word as a key, with all trigrams for which it is the first word.
        private readonly Dictionary<string, 
                                            Dictionary<string, 
                                                                Dictionary<string, int>>> firstWordDictionary;
        //the second dictionary lists the second word as a key, with all trigrams for which it is the second word
        private readonly Dictionary<string,
                                            Dictionary<string,
                                                                Dictionary<string, int>>> secondWordDictionary;        
        //the third dictionary lists the second word as a key, with all trigrams for which it is the third word.
        private readonly Dictionary<string,
                                    Dictionary<string,
                                                        Dictionary<string, int>>> thirdWordDictionary;

        public TrigramManager()
        {
            firstWordDictionary = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            secondWordDictionary = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            thirdWordDictionary = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
        }

        public void Add(string word1, string word2, string word3)
        {
            if (firstWordDictionary.ContainsKey(word1))
            {
                var val = firstWordDictionary[word1];

                if (val.ContainsKey(word2))
                {
                    var val2 = val[word2];
                    if (val2.ContainsKey(word3))
                        val2[word3]++;
                    else
                        val2[word3] = 1;
                }
                else
                {
                    var newDic = new Dictionary<string, int>();
                    newDic[word3] = 1;
                    val[word2] = newDic;
                }
                    
            }
            else
            {
                var newDic = new Dictionary<string, int>();
                newDic[word3] = 1;

                var outerDic = new Dictionary<string, Dictionary<string, int>>();
                outerDic[word2] = newDic;

                firstWordDictionary[word1] = outerDic;
            }

            //add to second dictionary.
            if (secondWordDictionary.ContainsKey(word2))
            {
                var val = secondWordDictionary[word2];

                if (val.ContainsKey(word1))
                {
                    var val2 = val[word1];
                    if (val2.ContainsKey(word3))
                        val2[word3]++;
                    else
                        val2[word3] = 1;
                }
                else
                {
                    var newDic = new Dictionary<string, int>();
                    newDic[word3] = 1;
                    val[word1] = newDic;
                }

            }
            else
            {
                var newDic = new Dictionary<string, int>();
                newDic[word3] = 1;

                var outerDic = new Dictionary<string, Dictionary<string, int>>();
                outerDic[word1] = newDic;

                secondWordDictionary[word2] = outerDic;
            }

            //add to third dictionary.
            if (thirdWordDictionary.ContainsKey(word3))
            {
                var val = thirdWordDictionary[word3];

                if (val.ContainsKey(word1))
                {
                    var val2 = val[word1];
                    if (val2.ContainsKey(word2))
                        val2[word2]++;
                    else
                        val2[word2] = 1;
                }
                else
                {
                    var newDic = new Dictionary<string, int>();
                    newDic[word2] = 1;
                    val[word1] = newDic;
                }

            }
            else
            {
                var newDic = new Dictionary<string, int>();
                newDic[word2] = 1;

                var outerDic = new Dictionary<string, Dictionary<string, int>>();
                outerDic[word1] = newDic;

                thirdWordDictionary[word3] = outerDic;
            }
        }


        public IEnumerable<string> GetUnionOfSecondWordsFromFirstWords(IEnumerable<string> firstWords)
        {
            var result = Enumerable.Empty<string>();

            foreach(var firstWord in firstWords)
            {
                if (firstWordDictionary.ContainsKey(firstWord))
                {
                    var secondWordsPart = firstWordDictionary[firstWord].Keys;
                    result = result.Union(secondWordsPart);
                }
            }

            return result;
        }

        public IEnumerable<string> GetUnionOfThirdWordsFromFirstWords(IEnumerable<string> firstWords)
        {
            var result = Enumerable.Empty<string>();

            foreach (var firstWord in firstWords)
            {
                if (firstWordDictionary.ContainsKey(firstWord))
                {
                    foreach (var secondWord in firstWordDictionary[firstWord].Keys)
                    {
                        if (firstWordDictionary[firstWord].ContainsKey(secondWord))
                        {
                            var thirdWordsPart = firstWordDictionary[firstWord][secondWord].Keys;
                            result = result.Union(thirdWordsPart);
                        }
                    }
                }
            }

            return result;
        }

        public IEnumerable<string> GetUnionOfFirstWordsFromSecondWords(IEnumerable<string> secondWords)
        {
            var result = Enumerable.Empty<string>();

            foreach (var secondWord in secondWords)
            {
                if (secondWordDictionary.ContainsKey(secondWord))
                {
                    var secondWordsPart = secondWordDictionary[secondWord].Keys;
                    result = result.Union(secondWordsPart);
                }
            }

            return result;
        }

        public IEnumerable<string> GetUnionOfSecondWordsFromThirdWords(IEnumerable<string> thirdWords)
        {
            var result = Enumerable.Empty<string>();

            foreach (var thirdWord in thirdWords)
            {
                if (thirdWordDictionary.ContainsKey(thirdWord))
                {
                    foreach (var firstWord in thirdWordDictionary[thirdWord].Keys)
                    {
                        if (thirdWordDictionary[thirdWord].ContainsKey(firstWord))
                        {
                            var thirdWordsPart = thirdWordDictionary[thirdWord][firstWord].Keys;
                            result = result.Union(thirdWordsPart);
                        }
                    }
                }
            }

            return result;
        }

        public IEnumerable<string> GetUnionOfFirstWordsFromThirdWords(IEnumerable<string> thirdWords)
        {
            var result = Enumerable.Empty<string>();

            foreach (var thirdWord in thirdWords)
            {
                if (thirdWordDictionary.ContainsKey(thirdWord))
                {
                    var thirdWordPart = thirdWordDictionary[thirdWord].Keys;
                    result = result.Union(thirdWordPart);
                }
            }

            return result;
        }

        public IEnumerable<string> GetUnionOfThirdWordsFromSecondWords(IEnumerable<string> secondWords)
        {
            var result = Enumerable.Empty<string>();

            foreach (var secondWord in secondWords)
            {
                if (secondWordDictionary.ContainsKey(secondWord))
                {
                    foreach (var firstWord in secondWordDictionary[secondWord].Keys)
                    {
                        if (secondWordDictionary[secondWord].ContainsKey(firstWord))
                        {
                            var thirdWordsPart = secondWordDictionary[secondWord][firstWord].Keys;
                            result = result.Union(thirdWordsPart);
                        }
                    }
                }
            }

            return result;
        }


        public bool Exists(string firstWord, string secondWord, string thirdWord)
        {
            return (firstWordDictionary.ContainsKey(firstWord) &&
                    firstWordDictionary[firstWord].ContainsKey(secondWord) &&
                    firstWordDictionary[firstWord][secondWord].ContainsKey(thirdWord));
        }


        //public List<KeyValuePair<KeyValuePair<string, string>, int>> BigramsAboveCountThresholdN(int n)
        //{
        //    var list = new List<KeyValuePair<KeyValuePair<string, string>, int>>();

        //    foreach (var word1 in firstWordDictionary.Keys)
        //    {
        //        var innerDic = firstWordDictionary[word1];
        //        foreach (var word2 in innerDic.Keys)
        //        {
        //            var count = innerDic[word2];
        //            if (count >= n)
        //            {
        //                list.Add(
        //                    new KeyValuePair<KeyValuePair<string, string>, int>(
        //                        new KeyValuePair<string, string>(word1, word2), count));
        //            }
        //        }
        //    }
        //    return list;
        //}

        //public IEnumerable<string> GetAllWordsBeforeWord(string secondword)
        //{
        //    return (secondWordDictionary.ContainsKey(secondword)
        //        ? secondWordDictionary[secondword].Keys
        //        : Enumerable.Empty<string>());
        //}

        //private IEnumerable<string> GetAllWordsAfterWord(string firstword)
        //{
        //    return (firstWordDictionary.ContainsKey(firstword)
        //        ? firstWordDictionary[firstword].Keys
        //        : Enumerable.Empty<string>());
        //}


        //public string[] IntersectTwoFirstWords(string firstWord1, string firstWord2)
        //{
        //    return
        //        GetAllWordsAfterWord(firstWord1)
        //            .ToArray()
        //            .Intersect(GetAllWordsAfterWord(firstWord2).ToArray())
        //            .ToArray();
        //}

        //public string[] IntersectTwoSecondWords(string secondWord1, string secondWord2)
        //{
        //    return
        //        GetAllWordsBeforeWord(secondWord1)
        //            .ToArray()
        //            .Intersect(GetAllWordsBeforeWord(secondWord2).ToArray())
        //            .ToArray();
        //}

        //public IEnumerable<string> GetIntersectOfBigramsWithFirstWords(IEnumerable<string> given)
        //{
        //    var enumerable = given as string[] ?? given.ToArray();
        //    var l = GetAllWordsAfterWord(enumerable.First());
        //    return enumerable.Aggregate(l, (current, str) => current.Intersect(GetAllWordsAfterWord(str)));
        //}

        //public IEnumerable<string> GetIntersectOfBigramsWithSecondWords(IEnumerable<string> given)
        //{
        //    var enumerable = given as string[] ?? given.ToArray();
        //    var l = GetAllWordsBeforeWord(enumerable.First());
        //    return enumerable.Aggregate(l, (current, str) => current.Intersect(GetAllWordsBeforeWord(str)));
        //}

        //public IEnumerable<string> GetUnionOfBigramsWithFirstWords(IEnumerable<string> given)
        //{
        //    var enumerable = given as string[] ?? given.ToArray();
        //    var l = GetAllWordsAfterWord(enumerable.First());
        //    return enumerable.Aggregate(l, (current, str) => current.Union(GetAllWordsAfterWord(str)));
        //}

        //public IEnumerable<string> GetUnionOfBigramsWithSecondWords(IEnumerable<string> given)
        //{
        //    var enumerable = given as string[] ?? given.ToArray();
        //    var l = GetAllWordsBeforeWord(enumerable.First());
        //    return enumerable.Aggregate(l, (current, str) => current.Union(GetAllWordsBeforeWord(str)));
        //}


    }
}