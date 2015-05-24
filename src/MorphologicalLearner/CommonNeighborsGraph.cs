using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Smrf.NodeXL.Core;
using Smrf.NodeXL.Visualization.Wpf;

namespace MorphologicalLearner
{
    public class CommonNeighborsGraph
    {
        private static Color[] colors = new[]
        {
            Color.Blue,
            Color.Red,
            Color.Green,
            Color.Black,
            Color.Brown,
            Color.CadetBlue,
            Color.Orange,
            Color.Violet,
            Color.Teal,
            Color.DeepPink,
            Color.Cyan,
            Color.Crimson,
            Color.Coral,
            Color.MidnightBlue,
            Color.Crimson,
            Color.DarkSalmon,
            Color.Firebrick,
            Color.Fuchsia,
            Color.Gold
        };

        private readonly BigramManager bigramMan;

        public CommonNeighborsGraph(BigramManager Man)
        {
            bigramMan = Man;
            LeftWordsNeighborhoods = new Dictionary<string, Dictionary<string, int>>();
            RightWordsNeighborhoods = new Dictionary<string, Dictionary<string, int>>();

        }

        public Dictionary<string, Dictionary<string, int>> LeftWordsNeighborhoods { get; private set; }
        public Dictionary<string, Dictionary<string, int>> RightWordsNeighborhoods { get; private set; }

        public static Color[] Colors
        {
            get { return colors; }
            set { colors = value; }
        }

        public void ComputeCommonNeighborsGraphs(string[] leftWords, string[] rightWords, int MinCommonNeighbors)
        {

            var adjMatrix = CreateAdjacencyMatrix(leftWords, rightWords);
            var neighborMatrix = CreateCommonNeighborsMatrix(adjMatrix);

            LeftWordsNeighborhoods = ComputeCommonNeighborsGraphOf(neighborMatrix,
                                                                    0,
                                                                    leftWords.Count(),
                                                                    MinCommonNeighbors,
                                                                    leftWords);

            RightWordsNeighborhoods = ComputeCommonNeighborsGraphOf(neighborMatrix,
                                                                    leftWords.Count(),
                                                                    leftWords.Count()+rightWords.Count(),
                                                                    MinCommonNeighbors,
                                                                    rightWords);
        }

        //public void GetEdgesList(Dictionary<string, Dictionary<string, int>> dic )
        //{
        //    var list = new List<Tuple<string, string, int>>();

        //    foreach (var word1 in dic.Keys)
        //    {
        //        foreach (var word2 in dic[word1].Keys)
        //        {
        //            var t = new Tuple<string, string, int>(word1, word2, dic[word1][word2]);
        //            list.Add(t);
        //        }

        //    }
        //}

        private Dictionary<string, Dictionary<string, int>> ComputeCommonNeighborsGraphOf(Matrix<float> neighborMatrix,int IndexStart,int IndexEnd, int MinCommonNeighbors, string[] theseWords)
        {
            var commonNeighborsGraph = new Dictionary<string, Dictionary<string, int>>();

            for (var i = IndexStart; i < IndexEnd; i++)
            {
                for (var j = i; j < IndexEnd; j++)
                {
                    if (i == j) continue;

                    int neighbors = (int)neighborMatrix[i, j];

                    if (neighbors >= MinCommonNeighbors)

                    {
                        if (!commonNeighborsGraph.ContainsKey(theseWords[i - IndexStart]))
                            commonNeighborsGraph[theseWords[i - IndexStart]] = new Dictionary<string, int>();

                        commonNeighborsGraph[theseWords[i - IndexStart]][theseWords[j - IndexStart]] = neighbors;
                    }
                }
            }

            return commonNeighborsGraph;
        }


        public Matrix<float> CreateAdjacencyMatrix(string[] leftwords, string[] rightwords)
        {
            int matrixSize = leftwords.Count() + rightwords.Count();
            var adjacencyMatrix = Matrix<float>.Build.Sparse(matrixSize, matrixSize);

            int leftwordsCount = leftwords.Count();
            int rightwordsCount = rightwords.Count();
            for (int i = 0; i < leftwordsCount; i++)
            {
                //the indices of the right words in the neighbor matrix begins after all
                //the words of left words.
                for (int j = leftwordsCount; j < leftwordsCount + rightwordsCount; j++)
                {

                    //if we already scanned the inverse order, skip.
                    if (adjacencyMatrix[i, j] > 0)
                        continue;

                    if (bigramMan.Exists(leftwords[i], rightwords[j - leftwordsCount]))
                    {
                        adjacencyMatrix[i, j] = 1;
                        adjacencyMatrix[j,i] = 1;

                    }
                }
            }
            return adjacencyMatrix;
        }

        private Matrix<float> CreateCommonNeighborsMatrix(Matrix<float> adjacencyMatrix)
        {
            //the common neightbor matrix is the adjacency matrix squared.
            return adjacencyMatrix.Power(2);
        }

        public IGraph ReadLogicalGraph(Learner.LocationInBipartiteGraph loc)
        {
            Dictionary<string, Dictionary<string, int>> g;

            if (loc == Learner.LocationInBipartiteGraph.LeftWords)
                g = LeftWordsNeighborhoods;
            else
                g = RightWordsNeighborhoods;

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

                        IVertex ver1 = GetorAddVertex(word1, addedVertices, graph, 4);
                        IVertex ver2 = GetorAddVertex(word2, addedVertices, graph, 4);

                        //IEdge e = 
                        graph.Edges.Add(ver1, ver2);
                        //e.SetValue(ReservedMetadataKeys.EdgeWeight, g[word1][word2]);
                    }
                }
            }
            catch (Exception)
            {
                //MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return graph;
        }

        private IVertex GetorAddVertex(string word, IDictionary<string, IVertex> addedVertices, IGraph graph, int bucketNumber)
        {

            IVertex ver;
            if (!addedVertices.ContainsKey(word))
            {
                ver = new Vertex();
                addedVertices[word] = ver;

                ver.SetValue(ReservedMetadataKeys.PerVertexShape,
           VertexShape.Label);

                ver.SetValue(ReservedMetadataKeys.PerVertexLabel, word);
                graph.Vertices.Add(ver);

                //int suffixIndex = m_buckets[bucketNumber].GetSuffixIndex(word);
                //ver.SetValue(ReservedMetadataKeys.PerColor, colors[suffixIndex]);

            }
            else
                ver = addedVertices[word];

            return ver;
        }


        public List<Edge> GetEdges(Learner.LocationInBipartiteGraph loc)
        {
            var list = new List<Edge>();

            Dictionary<string, Dictionary<string, int>> g;

            if (loc == Learner.LocationInBipartiteGraph.LeftWords)
                g = LeftWordsNeighborhoods;
            else
                g = RightWordsNeighborhoods;

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

                    var edge = new Edge();
                    edge.Vertex1 = word1;
                    edge.Vertex2 = word2;
                    edge.Weight = g[word1][word2];
                    list.Add(edge);
                }
            }
            return list;
        }
    }
}