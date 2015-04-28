using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Smrf.NodeXL.Algorithms;
using Smrf.NodeXL.Core;

namespace MorphologicalLearner
{
    public class Learner
    {
        public const string StemSymbol = "###";

        private const string TrainingCorpusFileDirectory= @"..\..\..\..\input texts\";
        private const int minCommonNeighbors = 3;
        private const float minimalFreq = 0.005f;

        private readonly BigramManager m_BigramManager;
        private readonly TrigramManager m_TrigramManager;

        private readonly Trie m_trie;
        private MorphologicalMatrix m_mat;
        private CommonNeighborsGraph commonNeighborsGraph;


        private readonly Dictionary<string, int> WordsInBuckets;
        private Dictionary<string, List<int>> WordsInPOS;

        public Learner(string fileName)
        {
            m_trie = new Trie();
            m_BigramManager = new BigramManager();
            m_TrigramManager = new TrigramManager();

            m_mat = null;
            WordsInBuckets = new Dictionary<string, int>();
            WordsInPOS = new Dictionary<string, List<int>>();
            commonNeighborsGraph = new CommonNeighborsGraph(m_BigramManager);

            BuildBigramsandTrie(fileName);
            BuildMorphologicalMatrix();
        }

        
        public enum LocationInBipartiteGraph
        {
            LeftWords,
            RightWords
        };

        private IEnumerable<string> GetSuperSet(IEnumerable<string> wordSet, LocationInBipartiteGraph location)
        {
            string[] superSetWords;
            if (location == LocationInBipartiteGraph.RightWords)
            {
                var leftWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(wordSet).ToArray();
                superSetWords = m_BigramManager.GetUnionOfBigramsWithFirstWords(leftWords).ToArray();
                commonNeighborsGraph.ComputeCommonNeighborsGraphs(leftWords, superSetWords, minCommonNeighbors);

            }
            else
            {
                var rightWords = m_BigramManager.GetUnionOfBigramsWithFirstWords(wordSet).ToArray();
                superSetWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(rightWords).ToArray();
                commonNeighborsGraph.ComputeCommonNeighborsGraphs(superSetWords, rightWords, minCommonNeighbors);
            }

            return superSetWords;
        }

        public void BuildBigramsandTrie(string fileName)
        {
            string TrainingCorpusFileName = TrainingCorpusFileDirectory + fileName + ".txt";
         
            //read sentences. (usused delimiters for sentence level: ' ' and '-')
            var sentenceDelimiters = new[]
            {'\r', '\n', '(', ')', '?', ',', '*', '.', ';', '!', '\\', '/', ':', '"', '“', '—'};
            var filestring = File.ReadAllText(TrainingCorpusFileName);
            var sentences = filestring.Split(sentenceDelimiters, StringSplitOptions.RemoveEmptyEntries);

            //discard empty spaces, lowercase. I will not be concerned with capitalization for now.
            var temp = sentences.Select(sentence => sentence.TrimStart()).Select(sentence => sentence.ToLower());
            //take only sentences that have more than one character (i.e. avoid ". p.s." interpretation as sentences, etc)
  
            var SentencesAboveALetter =
                temp.Where(sentence => sentence.Count() > 1);



            foreach (var sentence in SentencesAboveALetter)
            {
                //split to words
                var sentenceWords = sentence.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                if (sentenceWords.Count() < 2) continue; // don't treat sentences less than two words.
                if (sentenceWords.Count() > 3) continue; // don't treat sentences less than two words.

                //add pairs of adjacent words to bigram manager (including begin and end of sentence symbols)
                for (var k = 0; k < sentenceWords.Count() - 1; ++k)
                    m_BigramManager.Add(sentenceWords[k], sentenceWords[k + 1]);

                for (var k = 0; k < sentenceWords.Count() - 2; ++k)
                    m_TrigramManager.Add(sentenceWords[k], sentenceWords[k + 1], sentenceWords[k+2]);

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
       
        private void BuildStemAndSuffixVectors(out StemVector stemVector, out SuffixVector suffixVec)
        {
            string fatherName, sonName;

            var queue = new Queue<TrieNode>();
            //push the root node into the queue.
            queue.Enqueue(m_trie);

            stemVector = new StemVector();
            suffixVec = new SuffixVector();
            //searching the trie with BFS.
            while (queue.Any())
            {
                var node = queue.Dequeue();
                var list = node.ExpandSuffixes();

                if (node != m_trie)
                {
                    string stemName = node.ToString();
                    suffixVec.Add(StemSymbol, stemName);
                    stemVector.AddDerivedForm(node.ToString(),
                        new KeyValuePair<string, string>(stemName, StemSymbol));
                    stemVector.Add(stemName, StemSymbol);
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
                        suffixVec.Add(data.Difference, fatherName);


                        //to stem vector, add the stem, the suffix and the derived form.
                        stemVector.Add(fatherName, data.Difference);
                        stemVector.AddDerivedForm(fatherName,
                            new KeyValuePair<string, string>(sonName, data.Difference));
                    }
                }
            }

            //as a working postulate, consider as rules as suffixes that contribute more than 0.5% to the suffix counts.
            //naturally this parameter value should be plotted more rigorously in a later stage of research.
            //(i.e. http://www.wikiwand.com/en/Receiver_operating_characteristic)
            suffixVec.LeaveOnlySuffixesAboveFrequencyThreshold(minimalFreq);
        }

        public void BuildMorphologicalMatrix()
        {
            //fills StemVector and SuffixVector by BFS search on the trie.
            StemVector stemVector;
            SuffixVector suffixVec;
            BuildStemAndSuffixVectors(out stemVector, out suffixVec);

            //create morphological matrix from the stems and the suffixes.
            m_mat = new MorphologicalMatrix(stemVector, suffixVec);

            //PutAllWordsIntoBuckets();
        }

        //private void PutAllWordsIntoBuckets()
        //{
        //    for (var i = 0; i < _mVectors.Count(); i++)
        //    {
        //        var words = _mVectors[i].Words();
        //        //for each word, assign its morphological bucket index.
        //        //note: some words were not put into Vectors (since their morphological significance was below threshold)
        //        foreach (var w in words)
        //            WordsInBuckets[w] = i;
        //    }
        //}



        public IGraph ReadLogicalGraph(LocationInBipartiteGraph loc)
        {
            return commonNeighborsGraph.ReadLogicalGraph(loc);
        }

        public ICollection<Community> GetClusters(IGraph graph)
        {
            // Use a ClusterCalculator to partition the graph's vertices into
            // clusters.

            ClusterCalculator clusterCalculator = new ClusterCalculator();
            //clusterCalculator.Algorithm = ClusterAlgorithm.WakitaTsurumi;
            //clusterCalculator.Algorithm = ClusterAlgorithm.GirvanNewman;
            //clusterCalculator.Algorithm = ClusterAlgorithm.Clique;

            return clusterCalculator.CalculateGraphMetrics(graph);
        }

        public void EvaluateSyntacticCategoryOfCandidates(string[] candidates)
        {
            IGraph graph = ReadLogicalGraph(LocationInBipartiteGraph.RightWords);
            ICollection<Community> clusters = GetClusters(graph);
            string[] allWords = commonNeighborsGraph.RightWordsNeighborhoods.Keys.ToArray();

            string[] feasibleCandidates = allWords.Intersect(candidates).ToArray();

            foreach (Community cluster in clusters)
            {
                //Console.WriteLine("---");

                string[] wordsInCluster = new string[cluster.Vertices.Count];
                int i = 0;

                // Populate the group with the cluster's vertices.
                foreach (IVertex vertex in cluster.Vertices)
                    wordsInCluster[i++] = vertex.GetValue(ReservedMetadataKeys.PerVertexLabel) as string;

                //string words = string.Join(",", wordsInCluster);

                //Console.WriteLine("{0}", words);

                string[] candidateWordsInCluster = feasibleCandidates.Intersect(wordsInCluster).ToArray();

                bool contained = !feasibleCandidates.Except(wordsInCluster).Any();

               
            }
        }

        //public string[] LookForSyntacticCategoryCandidates()
        //{
        //    //for now, return just the seed.
        //    string[] rightWords = m_mat.FindSeed();    

        //    //GetSuperSet(rightWords, LocationInBipartiteGraph.RightWords);
        //    var leftWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(rightWords).ToArray();

        //    //rightWords = new string[] { "fixed", "passed", "addressed", "wished", "kissed", "approached" };

        //    commonNeighborsGraph.ComputeCommonNeighborsGraphs(leftWords, rightWords, minCommonNeighbors);

        //    return rightWords;
        //}
        public string[] FindMaximualInformationClusterFromLeft(string[] secondWords)
        {

            var firstWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(secondWords).ToArray();
            commonNeighborsGraph.ComputeCommonNeighborsGraphs(firstWords, secondWords, 4);

            string[] largestClusterSecondWords = GetWordsOfLargestCluster(LocationInBipartiteGraph.RightWords);
            string[] largestClusterFirstWords = GetWordsOfLargestCluster(LocationInBipartiteGraph.LeftWords);

            string[] NeighborsOfLargestClusterFirstWords =
                m_BigramManager.GetUnionOfBigramsWithFirstWords(largestClusterFirstWords).ToArray();

            bool contained = !largestClusterSecondWords.Except(NeighborsOfLargestClusterFirstWords).Any();

            if (contained)
                return largestClusterFirstWords;

            return FindMaximualInformationClusterFromLeft(largestClusterSecondWords);
        }

        private string[] GetWordsOfLargestCluster(LocationInBipartiteGraph loc)
        {
            IGraph graph = ReadLogicalGraph(loc);
            ICollection<Community> clusters = GetClusters(graph);
            Community largestCommunityRight = GetLargestCluster(clusters);

            if (largestCommunityRight != null)
                return 
                    largestCommunityRight.Vertices.Select(vertex => vertex.GetValue(ReservedMetadataKeys.PerVertexLabel).ToString())
                        .ToArray();

            return null;
        }

        private static Community GetLargestCluster(ICollection<Community> clusters)
        {
            int maxVertices = 0;
            Community LargestCommunity = null;

            foreach (Community cluster in clusters)
            {
                if (cluster.Vertices.Count() > maxVertices)
                {
                    maxVertices = cluster.Vertices.Count();
                    LargestCommunity = cluster;
                }
            }

            return LargestCommunity;
        }


        public string[] LookForSyntacticCategoryCandidates()
        {
            //for now, return just the seed.
            string[] secondWords = m_mat.FindSeed();
            secondWords = m_BigramManager.RemoveSecondOrphanedWords(secondWords).ToArray();
            BiCliqueFinder biclique = new BiCliqueFinder(m_BigramManager);
            biclique.Find(secondWords, LocationInBipartiteGraph.RightWords);


            //GetSuperSet(rightWords, LocationInBipartiteGraph.RightWords);
            //var firstWords = FindMaximualInformationClusterFromLeft(secondWords);


            //var firstFirstWords = FindMaximualInformationClusterFromLeft(firstWords);
            //var firstfirstFirstWords = FindMaximualInformationClusterFromLeft(firstFirstWords);

            //commonNeighborsGraph.ComputeCommonNeighborsGraphs(secondWords, thirdWords, 4);
            //commonNeighborsGraph.ComputeCommonNeighborsGraphs(firstWords, secondWords, 4);

    

            return null;
        }
    }
}