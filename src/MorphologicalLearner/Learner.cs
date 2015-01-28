using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MorphologicalLearner
{
    class Learner
    {
        private const string BeginOfSentence = "#beginS# ";
        private const string EndOfSentence = " #endS#";
        private const string TrainingCorpusFileName = @"..\..\..\..\input texts\David Copperfield.txt";


        private readonly Trie m_trie;
        private SuffixVector m_SuffixVector;
        private StemVector m_StemVector;
        private InflectedForms m_InflectedForms;
        private readonly BigramManager m_BigramManager;

        public Learner()
        {
            m_trie = new Trie();
            m_BigramManager = new BigramManager();
            m_SuffixVector = new SuffixVector();
            m_StemVector = new StemVector();
            m_InflectedForms = new InflectedForms();
        }

        private string[] ReadFile(string filename, char[] delimiters)
        {
            var filestring = File.ReadAllText(filename);
            return filestring.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }


        public void BuildBigramsandTrie()
        {
            //read sentences. (usused delimiters for sentence level: ' ' and '-')
            var sentenceDelimiters = new[] { '\r', '\n', '(', ')', '?', ',', '*', '.', ';', '!', '\\', '/', ':', '"', '—', };
            var sentences = ReadFile(TrainingCorpusFileName, sentenceDelimiters);

            //pad with special begin and end symbols.
            //take only sentences that have more than one character (i.e. avoid ". p.s." interpretation as sentences, etc)
            var temp = sentences.Select(sentence => sentence.TrimStart());

            var SentencesWithBeginAndEndSymbols =
                temp.Where(sentence => sentence.Count() > 1).Select(sentence => BeginOfSentence + sentence + EndOfSentence);

            foreach (var sentence in SentencesWithBeginAndEndSymbols)
            {
                //split to words
                var sentenceWords = sentence.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                for (var k = 0; k < sentenceWords.Count() - 1; ++k)
                {
                    m_BigramManager.Add(sentenceWords[k], sentenceWords[k + 1]);
                }
                for (var k = 1; k < sentenceWords.Count() -1; ++k)
                {
                    m_trie.Add(sentenceWords[k]);
                }
                
            }
        }

        public void BuildMorphologicalMatrix()
        {
            string fatherName, sonName;

            var queue = new Queue<TrieNode>();
            queue.Enqueue(m_trie);

            //searching the trie with BFS.
            while (queue.Any())
            {
                var node = queue.Dequeue();
                var list = node.ExpandSuffixes();

                foreach(var data in list)
                {
                    fatherName = data.Father.ToString();
                    sonName = data.Son.ToString();
                    //push all found nodes to the queue.
                    queue.Enqueue(data.Son);

                    if (data.Father != m_trie)  //if the father data is the root, don't add. (every string will be a suffix of the empty root string, we're not interested).
                    {
                        //to suffix vector, add the suffix and the stem
                        m_SuffixVector.Add(data.Difference, fatherName);

                        //to stem vector, add the stem and the suffix.
                        m_StemVector.Add(fatherName, data.Difference);

                        //to the inflected form, add the stem and the suffix.
                        m_InflectedForms.Add(sonName, fatherName, data.Difference);

                    }
                }
            }

            //as a working postulate, consider as rules as suffixes that contribute more than 0.5% to the suffix counts.
            //naturally this parameter value should be plotted more rigorously in a later stage of research.
            //(i.e. http://www.wikiwand.com/en/Receiver_operating_characteristic)

            m_SuffixVector.LeaveOnlySuffixesAboveFrequencyThreshold(0.005);
            MorphologicalMatrix mat = new MorphologicalMatrix(m_StemVector, m_SuffixVector);

            mat.PrintNColumnsOfMatrix(500);
            m_SuffixVector.Statistics();

        }


    }
}


