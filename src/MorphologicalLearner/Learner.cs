using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RDotNet;
using Smrf.NodeXL.Algorithms;
using Smrf.NodeXL.Core;
using MathNet.Numerics.LinearAlgebra;

namespace MorphologicalLearner
{

    public class Edge
    {
        public string Vertex1 { get; set; }
        public string Vertex2 { get; set; }
        public double Weight { get; set; }
    }

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
        private CommonNeighborsGraphManager _commonNeighborsGraphManager;


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
            _commonNeighborsGraphManager = new CommonNeighborsGraphManager(m_BigramManager);

            BuildBigramsandTrie(fileName);
            BuildMorphologicalMatrix();
        }

        
        public enum LocationInBipartiteGraph
        {
            LeftWords,
            RightWords
        };

 
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
                //if (sentenceWords.Count() > 3) continue; // don't treat sentences less than two words.

                //add pairs of adjacent words to bigram manager 
                for (var k = 0; k < sentenceWords.Count() - 1; ++k)
                    m_BigramManager.Add(sentenceWords[k], sentenceWords[k + 1]);

                //for (var k = 0; k < sentenceWords.Count() - 2; ++k)
                //    m_TrigramManager.Add(sentenceWords[k], sentenceWords[k + 1], sentenceWords[k+2]);

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
            return _commonNeighborsGraphManager.ReadLogicalGraph(loc);
        }

        public ICollection<Smrf.NodeXL.Algorithms.Community> GetClusters(IGraph graph)
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
            ICollection<Smrf.NodeXL.Algorithms.Community> clusters = GetClusters(graph);
            string[] allWords = _commonNeighborsGraphManager.RightWordsNeighborhoods.Graph.Keys.ToArray();

            string[] feasibleCandidates = allWords.Intersect(candidates).ToArray();

            foreach (Smrf.NodeXL.Algorithms.Community cluster in clusters)
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
     
        public string[] FindMaximualInformationClusterFromLeft(string[] secondWords)
        {

            var firstWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(secondWords).ToArray();
            _commonNeighborsGraphManager.ComputeCommonNeighborsGraphFromCoOccurrenceGraph(firstWords, secondWords, 4);

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
            ICollection<Smrf.NodeXL.Algorithms.Community> clusters = GetClusters(graph);
            Smrf.NodeXL.Algorithms.Community largestCommunityRight = GetLargestCluster(clusters);

            if (largestCommunityRight != null)
                return 
                    largestCommunityRight.Vertices.Select(vertex => vertex.GetValue(ReservedMetadataKeys.PerVertexLabel).ToString())
                        .ToArray();

            return null;
        }

        private static Smrf.NodeXL.Algorithms.Community GetLargestCluster(ICollection<Smrf.NodeXL.Algorithms.Community> clusters)
        {
            int maxVertices = 0;
            Smrf.NodeXL.Algorithms.Community LargestCommunity = null;

            foreach (Smrf.NodeXL.Algorithms.Community cluster in clusters)
            {
                if (cluster.Vertices.Count() > maxVertices)
                {
                    maxVertices = cluster.Vertices.Count();
                    LargestCommunity = cluster;
                }
            }

            return LargestCommunity;
        }

        private void PrintClusters(IEnumerable<Smrf.NodeXL.Algorithms.Community> list)
        {
            foreach (var cluster in list)
            {
                string[] names = cluster.Vertices.Select(vertex => vertex.GetValue(ReservedMetadataKeys.PerVertexLabel).ToString())
                        .ToArray();
                string str = string.Join(",", names);
                Console.WriteLine("{0}", str);

            }
        }

        private bool GoodCandidate(string v)
        {

            return true;
        }
        public string[] LookForSyntacticCategoryCandidates()
        {     
            //for now, return just the seed.
            string[] secondWords = m_mat.FindSeed();
            var firstwords = m_BigramManager.GetUnionOfBigramsWithSecondWords(secondWords).ToArray();
            _commonNeighborsGraphManager.ComputeCommonNeighborsGraphFromCoOccurrenceGraph(firstwords, secondWords, 4);

            Community[] foundCommunities = null;

            MathNet.Numerics.LinearAlgebra.Matrix<double> currentMatrix = _commonNeighborsGraphManager.RightMatrix;
            double maxModularity = -1000;
            bool improvement = true;
            int numOfSteps = 0;
            Stack<Community[]> communityHierarchy = new Stack<Community[]>();
            while (improvement)
            {
                improvement = false;
                LouvainMethod louvain = new LouvainMethod(currentMatrix);
                var newMatrix = louvain.FirstStep(out foundCommunities);

                double newModularity = louvain.Modularity();
                //if (newModularity > maxModularity)
                if (foundCommunities.Count() < currentMatrix.ColumnCount)
                {
                    var ttt = foundCommunities.Select(x => x.communityMembers.Count()).ToArray();
                    communityHierarchy.Push(foundCommunities);
                    maxModularity = newModularity;
                    currentMatrix = newMatrix;
                    improvement = true;
                    numOfSteps++;
                }
            }
            var dendogram = communityHierarchy.ToArray();
            var l = new List<List<int>>();
            foreach (var community in dendogram[0])
            {
                var list = GetLeavesOfCommunity(community, 0, dendogram).ToList();
                l.Add(list);

            }



            List<List<string>> listOfCommunities = new List<List<string>>();
            int total = 0;
            foreach (var community in l)
            {
                List<string> currentList = new List<string>();
                for (int i = 0; i < community.Count; i++)
                {
                    int nodeIndex = community[i];
                    currentList.Add(secondWords[nodeIndex]);
                    total++;
                }
                if (currentList.Any())
                    listOfCommunities.Add(currentList);
            }

            //Sefi - remove from CommonNeighborsGraph obsolete functionality. (i,e, CommonNeighborsGraph class).
          
            return null;
        }

        IEnumerable<int> GetLeavesOfCommunity(Community c, int hierarchyIndex, Community[][] dendogram)
        {
            if (hierarchyIndex == dendogram.Count() - 1)
                    return c.communityMembers;

            IEnumerable<int> allExpanded = Enumerable.Empty<int>();

            foreach (var communityMember in c.communityMembers)
            {
                Community expandedCommunity = dendogram[hierarchyIndex + 1][communityMember];
                IEnumerable<int> leavesOfCurrentMember = GetLeavesOfCommunity(expandedCommunity, hierarchyIndex + 1, dendogram);
                allExpanded = allExpanded.Concat(leavesOfCurrentMember);

            }

            return allExpanded;
        }

        public IEnumerable<Smrf.NodeXL.Algorithms.Community> Clusterize(string[] wordSetLeft, string[] wordSetRight, LocationInBipartiteGraph loc)
        {
            var list = new List<Smrf.NodeXL.Algorithms.Community>();
            ICollection<Smrf.NodeXL.Algorithms.Community> clusters = GetClusters(wordSetLeft, wordSetRight, loc);

            if (clusters.Count == 1)
            {
                var cluster = clusters.First();
                list.Add(cluster);
                string[] names = cluster.Vertices.Select(vertex => vertex.GetValue(ReservedMetadataKeys.PerVertexLabel).ToString())
        .ToArray();

                string str = "cluster contains: " + string.Join(",", names);
                Console.WriteLine("{0}", str);
                return list;
            }

            foreach (var cluster in clusters)
            {
                string[] wordsInCluster = new string[cluster.Vertices.Count];
                int i = 0;

                // Populate the group with the cluster's vertices.
                foreach (IVertex vertex in cluster.Vertices)
                    wordsInCluster[i++] = vertex.GetValue(ReservedMetadataKeys.PerVertexLabel) as string;

                var sublist = Clusterize(wordSetLeft, wordsInCluster, loc);
                list = list.Concat(sublist).ToList();
            }
            return list;

        }
        public ICollection<Smrf.NodeXL.Algorithms.Community> GetClusters(string[] wordSetLeft, string[] wordSetRight, LocationInBipartiteGraph loc)
        {
            int commonNeigh = minCommonNeighbors;
            //commonNeigh = 1;

            _commonNeighborsGraphManager.ComputeCommonNeighborsGraphFromCoOccurrenceGraph(wordSetLeft, wordSetRight, commonNeigh);
            IGraph graph = ReadLogicalGraph(loc);
            ClusterCalculator clusterCalculator = new ClusterCalculator();
            //clusterCalculator.Algorithm = ClusterAlgorithm.WakitaTsurumi;
            //clusterCalculator.Algorithm = ClusterAlgorithm.GirvanNewman;
            //clusterCalculator.Algorithm = ClusterAlgorithm.Clique;

            return clusterCalculator.CalculateGraphMetrics(graph);

        }



        private IEnumerable<string> GetSuperSet(IEnumerable<string> wordSet, LocationInBipartiteGraph location)
        {
            string[] superSetWords;
            if (location == LocationInBipartiteGraph.RightWords)
            {
                var leftWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(wordSet).ToArray();
                superSetWords = m_BigramManager.GetUnionOfBigramsWithFirstWords(leftWords).ToArray();
                _commonNeighborsGraphManager.ComputeCommonNeighborsGraphFromCoOccurrenceGraph(leftWords, superSetWords, minCommonNeighbors);

            }
            else
            {
                var rightWords = m_BigramManager.GetUnionOfBigramsWithFirstWords(wordSet).ToArray();
                superSetWords = m_BigramManager.GetUnionOfBigramsWithSecondWords(rightWords).ToArray();
                _commonNeighborsGraphManager.ComputeCommonNeighborsGraphFromCoOccurrenceGraph(superSetWords, rightWords, minCommonNeighbors);
            }

            return superSetWords;
        }


        public void ComputeCommunitiesInR(List<Edge> list)
        {

            string[] edgeArray = new string[list.Count];
            int i = 0;

            foreach (var edge in list)
            {
                edgeArray[i++] = string.Format("\"{0}\",\"{1}\"", edge.Vertex1, edge.Vertex2);
                //add weights later
            }
            var concatArray = string.Join(",", edgeArray);
            string edgeListR = "e <- matrix( c(" + concatArray + "), nc=2, byrow=TRUE)";

            //Console.WriteLine(edgeListR);

            var DegreeDistribution = m_BigramManager.GetFrequencies(LocationInBipartiteGraph.LeftWords).ToArray();
            var concatArray2 = string.Join(",", DegreeDistribution);
            string freqeuncies = "x <- c(" + concatArray2 + ")";

            //load R
            REngine engine = REngine.GetInstance();

            //load iGraph library
            engine.Evaluate("library(igraph)");
            engine.Evaluate(edgeListR);
            engine.Evaluate("g <- graph.edgelist(e)");
            //engine.Evaluate("imc <- infomap.community(g)");
            engine.Evaluate("imc <- edge.betweenness.community(g)");
            //engine.Evaluate("imc <- fastgreedy.community(g)");
            //engine.Evaluate("imc <- leading.eigenvector.community(g)");

            engine.Evaluate(freqeuncies);
            engine.Evaluate("y <- c(1:1500)");
            //engine.Evaluate("plot(density(x))");
            engine.Evaluate("plot(x, y, type=\"h\")");


            var communities = engine.Evaluate("communities <- communities(imc)").AsCharacter().ToArray();
            var graph = engine.Evaluate("V(g)$name").AsCharacter().ToArray();



            engine.Dispose();
        }
    }
}

            //string[] firstwords = m_BigramManager.MostConnectedWords(LocationInBipartiteGraph.LeftWords, 100).ToArray();

            //secondWords =
            //    secondWords.Intersect(m_BigramManager.MostConnectedWords(LocationInBipartiteGraph.RightWords, 100)).ToArray();

            //var divisiveClusteringResults = Clusterize(firstwords, secondWords, LocationInBipartiteGraph.RightWords);


            //var list = commonNeighborsGraph.GetEdges(LocationInBipartiteGraph.LeftWords);
            //ComputeCommunitiesInR(list);