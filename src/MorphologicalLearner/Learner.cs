using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace MorphologicalLearner
{
    public class Learner
    {
        public enum Direction
        {
            Left,
            Right
        };

        private const string BeginOfSentence = "#beginS# ";
        private const string EndOfSentence = " #endS#";
        private const string TrainingCorpusFileName = @"..\..\..\..\input texts\David Copperfield.txt";
        private const float minimalFreq = 0.005f;

        private readonly Trie m_trie;
        private SuffixVector m_SuffixVector;
        private StemVector m_StemVector;
        private MorphologicalBucket[] m_buckets;
        private readonly BigramManager m_BigramManager;
        private MorphologicalMatrix m_mat;
        private Dictionary<string, int> WordsInBuckets;
        private Dictionary<string, List<int>> WordsInPOS;
        private CommonNeighborsGraph neighborGraph;

        public CommonNeighborsGraph NeighborGraph { get; set; }

        public Learner()
        {
            m_trie = new Trie();
            m_BigramManager = new BigramManager();
            m_SuffixVector = new SuffixVector();
            m_StemVector = new StemVector();
            m_mat = null;
            m_buckets = null;
            WordsInBuckets = new Dictionary<string, int>();
            WordsInPOS = new Dictionary<string, List<int>>();

        }

        public void BuildBigramsandTrie()
        {
            //read sentences. (usused delimiters for sentence level: ' ' and '-')
            var sentenceDelimiters = new[] { '\r', '\n', '(', ')', '?', ',', '*', '.', ';', '!', '\\', '/', ':', '"', '—', };
            var filestring = File.ReadAllText(TrainingCorpusFileName);
            var sentences = filestring.Split(sentenceDelimiters, StringSplitOptions.RemoveEmptyEntries);

            //discard empty spaces, lowercase. I will not be concerned with capitalization for now.
            var temp = sentences.Select(sentence => sentence.TrimStart()).Select(sentence => sentence.ToLower());
            //take only sentences that have more than one character (i.e. avoid ". p.s." interpretation as sentences, etc)
            //pad with special being and end of sentences symbols.
            var SentencesWithBeginAndEndSymbols =
                temp.Where(sentence => sentence.Count() > 1).Select(sentence => BeginOfSentence + sentence + EndOfSentence);

            foreach (var sentence in SentencesWithBeginAndEndSymbols)
            {
                //split to words
                var sentenceWords = sentence.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                //add pairs of adjacent words to bigram manager (including begin and end of sentence symbols)
                for (var k = 0; k < sentenceWords.Count() - 1; ++k)
                    m_BigramManager.Add(sentenceWords[k], sentenceWords[k + 1]);
                
                for (var k = 1; k < sentenceWords.Count() - 1; ++k)
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

                foreach (var data in list)
                {
                    fatherName = data.Father.ToString();
                    sonName = data.Son.ToString();
                    //push all found nodes to the queue.
                    queue.Enqueue(data.Son);

                    if (data.Father != m_trie)  //if the father data is the root, don't add. (every string will be a suffix of the empty root string, we're not interested).
                    {
                        //to suffix vector, add the suffix and the stem
                        m_SuffixVector.Add(data.Difference, fatherName);

                        //to stem vector, add the stem, the suffix and the derived form.
                        m_StemVector.Add(fatherName, data.Difference);
                        m_StemVector.AddDerivedForm(fatherName, sonName);

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
            m_buckets = m_mat.InitializeMorphologicalBuckets(m_StemVector);
            PutAllWordsIntoBuckets();
        }

        private void PutAllWordsIntoBuckets()
        {
            for (int i = 0; i < m_buckets.Count(); i++)
            {
                IEnumerable<string> words = m_buckets[i].Words();
                //for each word, assign its morphological bucket index.
                //note: some words were not put into buckets (since their morphological significance was below threshold)
                foreach (var w in words)
                    WordsInBuckets[w] = i;
            }
        }


        public void Search()
        {
            //takes a morphological matrix m_mat and a bigram manager m_BigramManager

            //first, find seed in the morphological matrix.
            int seedBucketIndex = m_mat.FindSeed();

            neighborGraph = new CommonNeighborsGraph(m_BigramManager);

            string[] rightWords = m_buckets[seedBucketIndex].Words().ToArray();
            string[] leftWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(rightWords).ToArray();

            neighborGraph.ComputeCommonNeighborsGraphs(leftWords, rightWords);
            NeighborGraph = neighborGraph;

            var scc = neighborGraph.StronglyConnectedComponents(neighborGraph.RightWordsNeighborhoods);
            scc = neighborGraph.StronglyConnectedComponents(neighborGraph.LeftWordsNeighborhoods);

        }
    }
}


