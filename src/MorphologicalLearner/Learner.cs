using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Smrf.NodeXL.Core;
using Smrf.NodeXL.Algorithms;
using Smrf.NodeXL.Visualization.Wpf;
using System.Drawing;

namespace MorphologicalLearner
{
    public class Learner
    {
        private const string BeginOfSentence = "#beginS# ";
        private const string EndOfSentence = " #endS#";
        public const string StemSymbol = "###";

        private const string TrainingCorpusFileDirectory= @"..\..\..\..\input texts\";
        private const int minCommonNeighbors = 4;
        private const float minimalFreq = 0.005f;
        private readonly BigramManager m_BigramManager;
        private readonly StemVector m_StemVector;
        private readonly SuffixVector m_SuffixVector;
        private readonly Trie m_trie;
        private readonly Dictionary<string, int> WordsInBuckets;
        private MorphologicalBucket[] m_buckets;
        private MorphologicalMatrix m_mat;
        private CommonNeighborsGraph neighborGraph;
        private Dictionary<string, List<int>> WordsInPOS;
        private string m_FileName;
        private int m_bucketNumber;

        public Learner(string fileName)
        {
            m_FileName = fileName;
            m_trie = new Trie();
            m_BigramManager = new BigramManager();
            m_SuffixVector = new SuffixVector();
            m_StemVector = new StemVector();
            m_mat = null;
            m_buckets = null;
            WordsInBuckets = new Dictionary<string, int>();
            WordsInPOS = new Dictionary<string, List<int>>();

            BuildBigramsandTrie();
            BuildMorphologicalMatrix();
        }

        public void Learn(int bucketNumber = -1)
        {
            Search(bucketNumber);
        }

        
        public CommonNeighborsGraph NeighborGraph { get; set; }

        public void BuildBigramsandTrie()
        {

            string TrainingCorpusFileName = TrainingCorpusFileDirectory + m_FileName + ".txt";
            string TrainingCorpusNeighboursToRightFileName = TrainingCorpusFileDirectory + m_FileName + "RightNeighbors.bin";
            string TrainingCorpusNeighboursToLeftFileName = TrainingCorpusFileDirectory + m_FileName + "LeftNeighbors.bin";

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
            m_buckets = m_mat.InitializeMorphologicalBuckets(m_StemVector);
            PutAllWordsIntoBuckets();
        }

        private void PutAllWordsIntoBuckets()
        {
            for (var i = 0; i < m_buckets.Count(); i++)
            {
                var words = m_buckets[i].Words();
                //for each word, assign its morphological bucket index.
                //note: some words were not put into buckets (since their morphological significance was below threshold)
                foreach (var w in words)
                    WordsInBuckets[w] = i;
            }
        }

        public void Search(int bucketNumber)
        {

            int MaxRow = 0, MaxCol = 0;
            if (bucketNumber == -1)
                m_mat.FindSeed(out MaxCol, out MaxRow);

     

            Console.WriteLine("{0}", string.Join(",", m_buckets[MaxCol].Suffixes().ToArray()));

            //col 4 = {stem, ed, ing}
            MaxCol = 4;
            m_bucketNumber = MaxCol;
            //row 0 = stem, row 2 = ing, row 4 = ed.
            //MaxRow = 4; 

            neighborGraph = new CommonNeighborsGraph(m_BigramManager);
            //var rightWords = m_buckets[MaxCol].WordsOfSuffix(MaxRow).ToArray();
            var rightWords = m_buckets[MaxCol].Words().ToArray();

            //var morerightWords =  m_buckets[MaxCol].WordsOfSuffix(2).ToArray();
            //rightWords = rightWords.Union(morerightWords).ToArray();

            Console.WriteLine("{0}", string.Join(",", rightWords));

            var leftWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(rightWords).ToArray();

            neighborGraph.ComputeCommonNeighborsGraphs(leftWords, rightWords, minCommonNeighbors);
            NeighborGraph = neighborGraph;


        }

        private IVertex GetorAddVertex(string word, Dictionary<string, IVertex> addedVertices, IGraph graph, int bucketNumber)
        {
            System.Drawing.Color[] colors = new[]
            {
                System.Drawing.Color.Blue,
                System.Drawing.Color.Red,
                System.Drawing.Color.Green,
                System.Drawing.Color.Black,
                System.Drawing.Color.Brown,
                System.Drawing.Color.CadetBlue,
                System.Drawing.Color.Orange,
                System.Drawing.Color.Violet,
                System.Drawing.Color.Teal,
                System.Drawing.Color.DeepPink,
                System.Drawing.Color.Cyan,
                System.Drawing.Color.Crimson,
                System.Drawing.Color.Coral,
                System.Drawing.Color.MidnightBlue,
            };

            IVertex ver;
            if (!addedVertices.ContainsKey(word))
            {
                ver = new Vertex();
                addedVertices[word] = ver;

                ver.SetValue(ReservedMetadataKeys.PerVertexShape,
           VertexShape.Label);

                ver.SetValue(ReservedMetadataKeys.PerVertexLabel, word);
                graph.Vertices.Add(ver);

                int suffixIndex = m_buckets[bucketNumber].GetSuffixIndex(word);
                ver.SetValue(ReservedMetadataKeys.PerColor, colors[suffixIndex]);

            }
            else
                ver = addedVertices[word];

            return ver;
        }

        public IGraph ReadLogicalGraph()
        {
            var g = NeighborGraph.RightWordsNeighborhoods;

            IGraph graph = new Graph(GraphDirectedness.Undirected);
            try
            {
                var addedVertices = new Dictionary<string, IVertex>();

                Dictionary<string, Dictionary<string, int>> dic = new Dictionary<string, Dictionary<string, int>>();
                foreach (var word1 in g.Keys)
                {
                    foreach (var word2 in g[word1].Keys)
                    {
                        if (word1 == word2)
                            continue;

                        //if we already scanned these words in the opposite order, skip.
                        if (dic.ContainsKey(word2) && dic[word2].ContainsKey(word1))
                            continue;

                        if (!dic.ContainsKey(word1))
                            dic[word1] = new Dictionary<string, int>();

                        //push into dictionary to keep track of scanned pairs.
                        dic[word1][word2] = 1;

                        IVertex ver1 = GetorAddVertex(word1, addedVertices, graph, m_bucketNumber);
                        IVertex ver2 = GetorAddVertex(word2, addedVertices, graph, m_bucketNumber);

                        IEdge e = graph.Edges.Add(ver1, ver2);
                        //e.SetValue(ReservedMetadataKeys.EdgeWeight, g[word1][word2]);

                    }
                }

            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return graph;
        }


    }
}