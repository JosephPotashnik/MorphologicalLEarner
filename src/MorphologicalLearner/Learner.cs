using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Smrf.NodeXL.Core;

namespace MorphologicalLearner
{
    public class Learner
    {
        public const string StemSymbol = "###";

        private const string TrainingCorpusFileDirectory= @"..\..\..\..\input texts\";
        private const int minCommonNeighbors = 4;
        private const float minimalFreq = 0.005f;
        private readonly BigramManager m_BigramManager;
        private readonly StemVector m_StemVector;
        private readonly SuffixVector m_SuffixVector;
        private readonly Trie m_trie;
        private readonly Dictionary<string, int> WordsInBuckets;
        private MorphologicalVector[] _mVectors;
        private MorphologicalMatrix m_mat;
        private CommonNeighborsGraph neighborGraph;
        private Dictionary<string, List<int>> WordsInPOS;
        private string m_FileName;

        public Learner(string fileName)
        {
            m_FileName = fileName;
            m_trie = new Trie();
            m_BigramManager = new BigramManager();
            m_SuffixVector = new SuffixVector();
            m_StemVector = new StemVector();
            m_mat = null;
            _mVectors = null;
            WordsInBuckets = new Dictionary<string, int>();
            WordsInPOS = new Dictionary<string, List<int>>();
            neighborGraph = new CommonNeighborsGraph(m_BigramManager);

            BuildBigramsandTrie();
            BuildMorphologicalMatrix();
        }

        public void Learn()
        {
            var rightWords = m_mat.FindSeed();

            var leftWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(rightWords).ToArray();
            neighborGraph.ComputeCommonNeighborsGraphs(leftWords, rightWords, minCommonNeighbors);
        }


        public void BuildBigramsandTrie()
        {
            string TrainingCorpusFileName = TrainingCorpusFileDirectory + m_FileName + ".txt";
         
            //read sentences. (usused delimiters for sentence level: ' ' and '-')
            var sentenceDelimiters = new[]
            {'\r', '\n', '(', ')', '?', ',', '*', '.', ';', '!', '\\', '/', ':', '"', '—'};
            var filestring = File.ReadAllText(TrainingCorpusFileName);
            var sentences = filestring.Split(sentenceDelimiters, StringSplitOptions.RemoveEmptyEntries);

            //discard empty spaces, lowercase. I will not be concerned with capitalization for now.
            var temp = sentences.Select(sentence => sentence.TrimStart()).Select(sentence => sentence.ToLower());
            //take only sentences that have more than one character (i.e. avoid ". p.s." interpretation as sentences, etc)
  
            var SentencesAboveOneWord =
                temp.Where(sentence => sentence.Count() > 1);
                    //.Select(sentence => BeginOfSentence + sentence + EndOfSentence);

            foreach (var sentence in SentencesAboveOneWord)
            {
                //split to words
                var sentenceWords = sentence.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                //add pairs of adjacent words to bigram manager (including begin and end of sentence symbols)
                for (var k = 0; k < sentenceWords.Count() - 1; ++k)
                    m_BigramManager.Add(sentenceWords[k], sentenceWords[k + 1]);

                for (var k = 0; k < sentenceWords.Count(); ++k)
                {
                    //add each word to trie (skip begin and end of sentence symbols).
                    m_trie.Add(sentenceWords[k]);
                    //init each word into a dictionary that records the morphological bucket the words belongs to.
                    //the bucket index (nonnegative integer) is computed later. -1 = unclassified.
                    WordsInBuckets[sentenceWords[k]] = -1;
                }
            }

        }
       
        private void BuildStemAndSuffixVectors()
        {
            string fatherName, sonName;

            var queue = new Queue<TrieNode>();
            //push the root node into the queue.
            queue.Enqueue(m_trie);

            //searching the trie with BFS.
            while (queue.Any())
            {
                var node = queue.Dequeue();
                var list = node.ExpandSuffixes();

                if (node != m_trie)
                {
                    string stemName = node.ToString();
                    m_SuffixVector.Add(StemSymbol, stemName);
                    m_StemVector.AddDerivedForm(node.ToString(),
                        new KeyValuePair<string, string>(stemName, StemSymbol));
                    m_StemVector.Add(stemName, StemSymbol);
                }

            foreach (var data in list)
                {
                    fatherName = data.Father.ToString();
                    sonName = data.Son.ToString();
                    //push all found nodes to the queue.
                    queue.Enqueue(data.Son);

                    if (data.Father != m_trie)
                        //if the father data is the root, don't add. (every string will be a suffix of the empty root string, we're not interested).
                    {
                        //to suffix vector, add the suffix and the stem
                        m_SuffixVector.Add(data.Difference, fatherName);


                        //to stem vector, add the stem, the suffix and the derived form.
                        m_StemVector.Add(fatherName, data.Difference);
                        m_StemVector.AddDerivedForm(fatherName,
                            new KeyValuePair<string, string>(sonName, data.Difference));
                    }
                }
            }

            //as a working postulate, consider as rules as suffixes that contribute more than 0.5% to the suffix counts.
            //naturally this parameter value should be plotted more rigorously in a later stage of research.
            //(i.e. http://www.wikiwand.com/en/Receiver_operating_characteristic)
            m_SuffixVector.LeaveOnlySuffixesAboveFrequencyThreshold(minimalFreq);
        }

        public void BuildMorphologicalMatrix()
        {
            //fills m_StemVector and m_SuffixVector by BFS search on the trie.
            BuildStemAndSuffixVectors();
            m_mat = new MorphologicalMatrix(m_StemVector, m_SuffixVector);
            //PutAllWordsIntoBuckets();
        }

        private void PutAllWordsIntoBuckets()
        {
            for (var i = 0; i < _mVectors.Count(); i++)
            {
                var words = _mVectors[i].Words();
                //for each word, assign its morphological bucket index.
                //note: some words were not put into Vectors (since their morphological significance was below threshold)
                foreach (var w in words)
                    WordsInBuckets[w] = i;
            }
        }



        public IGraph ReadLogicalGraph()
        {
            return neighborGraph.ReadLogicalGraph();
        }
        
    }
}