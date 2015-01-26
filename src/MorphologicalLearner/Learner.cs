using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MorphologicalLearner
{
    class Learner
    {
        private const string BeginOfSentence = "#beginS# ";
        private const string EndOfSentence = " #endS#";
        private const string TrainingCorpusFileName = @"..\..\..\..\input texts\David Copperfield.txt";

        private readonly Trie m_trie;
        private readonly RulesCandidatesDictionary m_dic;
        private readonly BigramManager m_BigramManager;

        public Learner()
        {
            m_trie = new Trie();
            m_BigramManager = new BigramManager();
            m_dic = new RulesCandidatesDictionary();
        }

        private string[] ReadFile(string filename, char[] delimiters)
        {
            var filestring = File.ReadAllText(filename);
            return filestring.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public void BuildTrie()
        {
            var worddelimiters = new[] {'\r', '\n', '(', ')', '?', ',', '*', ' ', '.', ';', '!', '\\', '/', ':', '-', '"', '—', };
            var words = ReadFile(TrainingCorpusFileName, worddelimiters);
          
            foreach (var w in words)
                m_trie.Add(w);
        }

        public void BuildBigrams()
        {
            //read sentences. (usused delimiters for sentence level: ' ' and '-')
            var sentenceDelimiters = new[] { '\r', '\n', '(', ')', '?', ',', '*', '.', ';', '!', '\\', '/', ':', '"', '—', };
            var sentences = ReadFile(TrainingCorpusFileName, sentenceDelimiters);
            
            //pad with special begin and end symbols.
            var SentencesWithBeginAndEndSymbols =
                sentences.Select(sentence => BeginOfSentence + sentence + EndOfSentence);

            foreach (var sentence in SentencesWithBeginAndEndSymbols)
            {
                //split to words
                var sentenceWords = sentence.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                for (var k = 0; k < sentenceWords.Count() - 1; ++k)
                    m_BigramManager.Add(sentenceWords[k], sentenceWords[k + 1]);
            }
        }

        public void BuildCandidates()
        {
            var queue = new Queue<TrieNode>();
            queue.Enqueue(m_trie);

            var candidates = new List<KeyValuePair<string,string>>();

            //searching the trie with BFS.
            while (queue.Any())
            {
                var node = queue.Dequeue();
                var list = node.ExpandSuffixes();

                foreach(var data in list)
                {
                    //push all found nodes to the queue.
                    queue.Enqueue(data.Son);
                    var pair = new KeyValuePair<string, string>(data.Father.ToString(), data.Son.ToString());

                    if (data.Father != m_trie)  //if the father data is the root, don't add. (every string will be a suffix of the empty root string, we're not interested).
                    {
                        m_dic.Add(data.Difference);
                        candidates.Add(pair);
                    }
                }
            }

            //as a working postulate, consider all rules that contribute more than 0.5% to the suffix counts.
            //naturally this parameter value should be plotted more rigorously in a later stage of research.
            //(i.e. http://www.wikiwand.com/en/Receiver_operating_characteristic)
            var l = m_dic.RulesAboveFrequencyThreshold(0.005);
            
            Statistics(l);
        }

        private void Statistics(List<KeyValuePair<string, int>> l)
        {
            var sb = new StringBuilder();
            sb.Append("Suffix \t #Appearances");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var valuepair in l)
            {
                sb.AppendFormat("{0} \t {1}", valuepair.Key, valuepair.Value);
                sb.AppendLine();
            }

            File.WriteAllText(@"d:\David CopperfieldSuffixes.txt", sb.ToString());
        }
    }
}
