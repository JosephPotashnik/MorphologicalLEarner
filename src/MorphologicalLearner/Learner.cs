using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorphologicalLearner
{

    class Learner
    {
        private Trie m_trie;
        private RulesCandidatesDictionary m_dic;

        public void BuildTrie()
        {
            Trie tree = new Trie();

            tree.Add("אכלנו");
            tree.Add("אכלנונונו");
            tree.Add("אכלנותה");

            tree.Add("אכלתם");
            tree.Add("אמ");
            tree.Add("רכל");
            tree.Add("רכלנונו");
            tree.Add("אמנונו");

            m_trie = tree;
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
        }

        public Learner()   {}
    }
}
