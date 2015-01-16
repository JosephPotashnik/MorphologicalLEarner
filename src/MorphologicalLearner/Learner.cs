using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MorphologicalLearner
{

    class Learner
    {
        private Trie m_trie;
        private RulesCandidatesDictionary m_dic;

        public void BuildTrie()
        {

            Trie tree = new Trie();
            string filestring = File.ReadAllText(@"d:\tom sawyer.txt");
            char[] delimiters = new char[] {'\r', '\n', '(', ')', '?', ',', '*', ' ', '.', ';', '!', '\\', '/', ':', '-'};

            string[] words = filestring.Split(delimiters,
				     StringSplitOptions.RemoveEmptyEntries);
          
            foreach (var w in words)
                tree.Add(w);

            m_trie = tree;
        }

        public void BuildCandidates()
        {
            m_dic = new RulesCandidatesDictionary();

            Queue<TrieNode> queue = new Queue<TrieNode>();
            queue.Enqueue(m_trie);

            List<KeyValuePair<string, string>> candidates = new List<KeyValuePair<string,string>>();
            Stopwatch watch1 = new Stopwatch();

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
