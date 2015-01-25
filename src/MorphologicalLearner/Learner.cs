using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MorphologicalLearner
{


    class Learner
    {
        private const string BeginOfSentence = "# ";
        private const string EndOfSentence = " #";

        private Trie m_trie;
        private RulesCandidatesDictionary m_dic;

        public void BuildTrie()
        {

            Trie tree = new Trie();
            string filestring = File.ReadAllText(@"d:\tom sawyer.txt");
            char[] worddelimiters = new char[] {'\r', '\n', '(', ')', '?', ',', '*', ' ', '.', ';', '!', '\\', '/', ':', '-', '"',};

            string[] words = filestring.Split(worddelimiters,
				     StringSplitOptions.RemoveEmptyEntries);
          
            foreach (var w in words)
                tree.Add(w);

            m_trie = tree;
        }

        public void BuildBigrams()
        {
            string filestring = File.ReadAllText(@"d:\tom sawyer.txt");

            //read sentences.
            char[] sentenceDelimiters = new char[] { '\r', '\n', '(', ')', '?', ',', '*', '.', ';', '!', '\\', '/', ':', '-', '"', };

            string[] sentences = filestring.Split(sentenceDelimiters,
                     StringSplitOptions.RemoveEmptyEntries);
            
            //pad with special begin and end symbols.
            IEnumerable<string> WithBeginAndEndSymbols =
                sentences.Select(sentence => BeginOfSentence + sentence + EndOfSentence);

            var manager = new BigramManager();

            foreach (string sentence in WithBeginAndEndSymbols)
            {
                string[] sentenceWords = sentence.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                for (int k = 0; k < sentenceWords.Count() - 1; ++k)
                    manager.Add(sentenceWords[k], sentenceWords[k + 1]);
            }
            var r = manager.IntersectTwoFirstWords("the", "I");
            var z = manager.ListAfterGivenWords(new[] {"the", "I"});
            var z2 = manager.ListAfterGivenWords(new[] { "the", "I", "zaza"});

            var r1 = manager.IntersectTwoSecondWords("the", "I");
            var z1 = manager.ListBeforeGivenWords(new[] { "the", "I" });
            var z21 = manager.ListBeforeGivenWords(new[] { "the", "I", "Tom" });
        }

        public void BuildCandidates()
        {
            m_dic = new RulesCandidatesDictionary();

            Queue<TrieNode> queue = new Queue<TrieNode>();
            queue.Enqueue(m_trie);

            List<KeyValuePair<string, string>> candidates = new List<KeyValuePair<string,string>>();

            //searching the trie with BFS.
            while (queue.Any())
            {
                TrieNode node = queue.Dequeue();
                IEnumerable<StringAlignmentData> list = node.ExpandSuffixes();

                foreach(var data in list)
                {

                    //push all found nodes to the queue.
                    queue.Enqueue(data.Son);
                    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(data.Father.ToString(), data.Son.ToString());

                    if (data.Father != m_trie)  //if the father data is the root, don't add. (every string will be a suffix of the empty root string, we're not interested).
                    {
                        m_dic.Add(data.Difference);
                        candidates.Add(pair);
                    }
                }

            }

            List<KeyValuePair<string, int>> l = m_dic.RulesAboveNTimesThreshold(10);
            Statistics(l);
        }

        private void Statistics(List<KeyValuePair<string, int>> l)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Suffix \t #Appearances");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var valuepair in l)
            {
                sb.AppendFormat("{0} \t {1}", valuepair.Key, valuepair.Value);
                sb.AppendLine();
            }

            File.WriteAllText(@"d:\suffixesMorethan10InTomSawyer.txt", sb.ToString());
        }
    }
}
