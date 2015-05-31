using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Smrf.NodeXL.Core;
using Smrf.NodeXL.Visualization.Wpf;

namespace MorphologicalLearner
{
    public class CommonNeighborsGraphManager
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

        public CommonNeighborsGraphManager(BigramManager Man)
        {
            bigramMan = Man;
        }

        public Matrix<double> LeftMatrix { get; set; }
        public string[] LeftWords { get; set; }
        public Matrix<double> RightMatrix { get; set; }
        public string[] RightWords { get; set; }



        public static Color[] Colors
        {
            get { return colors; }
            set { colors = value; }
        }


        public void ComputeCommonNeighborsGraphFromCoOccurrenceGraph(string[] leftWords, string[] rightWords, int MinCommonNeighbors)
        {

            var adjMatrix = CreateAdjacencyMatrix(leftWords, rightWords);
            var neighborMatrix = CreateCommonNeighborsMatrix(adjMatrix);
            neighborMatrix.CoerceZero(4);    
            //experiment. 
            //1. the edges are unweighted - edge between common neighbors
            //2. the edge are weighted - what would be the formula to weight two common neighbors that each has a weight X1, X2 with a shared second word?

            for (int k = 0; k < neighborMatrix.ColumnCount; ++k)        //zero the diagonal, no self-loops in common neighbors graph
                neighborMatrix[k, k] = 0;
            

            LeftMatrix = neighborMatrix.SubMatrix(0, leftWords.Count(), 0, leftWords.Count());
            RightMatrix = neighborMatrix.SubMatrix(leftWords.Count(), rightWords.Count(), leftWords.Count(), rightWords.Count());
            LeftWords = leftWords;
            RightWords = rightWords;

           // temp
            //var testMat = Matrix<double>.Build.Sparse(7, 7);
            //testMat[1, 2] = 1;
            //testMat[1, 3] = 1;
            //testMat[2, 3] = 1;
            //testMat[4, 5] = 1;
            //testMat[4, 6] = 1;
            //testMat[5, 6] = 1;
            //testMat[3, 4] = 1;

            //testMat[2, 1] = 1;
            //testMat[3, 1] = 1;
            //testMat[3, 2] = 1;
            //testMat[5, 4] = 1;
            //testMat[6, 4] = 1;
            //testMat[6, 5] = 1;
            //testMat[4, 3] = 1;
            //LeftMatrix = testMat;
            //LeftWords = new string[] { "0", "1", "2", "3", "4", "5", "6" };

         }


        public Matrix<double> CreateAdjacencyMatrix(string[] leftwords, string[] rightwords)
        {
            int matrixSize = leftwords.Count() + rightwords.Count();
            var adjacencyMatrix = Matrix<double>.Build.Sparse(matrixSize, matrixSize);

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

                    int weight = bigramMan.Count(leftwords[i], rightwords[j - leftwordsCount]);
                    adjacencyMatrix[i, j] = adjacencyMatrix[j, i] = weight;

                    if (weight > 0 )
                       adjacencyMatrix[i, j] = adjacencyMatrix[j, i] = 1;  //if we want to take only the existence of neighbors

                }
            }
            return adjacencyMatrix;
        }

        private Matrix<double> CreateCommonNeighborsMatrix(Matrix<double> adjacencyMatrix)
        {
            return adjacencyMatrix.Power(2);
        }

        public IGraph ReadLogicalGraph(Learner.LocationInBipartiteGraph loc, List<List<int>> communities, out Vertex[] vertices)
        {
            Matrix<double> g;
            string[] words;


            if (loc == Learner.LocationInBipartiteGraph.LeftWords)
            {
                g = LeftMatrix;
                words = LeftWords;
            }
            else
            {
                g = RightMatrix;
                words = RightWords;
            }


            var nodes = communities.Where(x => x.Count() > 1).SelectMany(x => x).ToArray();
            HashSet<int> nodesInConsideredCommunities = new HashSet<int>(nodes);


            int size = g.ColumnCount;

            IGraph graph = new Graph(GraphDirectedness.Undirected);
            vertices = new Vertex[size];
            var degrees = g.RowSums();
            for (int k = 0; k < size; ++k)
            {
                if (nodesInConsideredCommunities.Contains(k))
                {
                    Vertex ver = new Vertex();
                    ver.SetValue(ReservedMetadataKeys.PerVertexShape, VertexShape.Label);
                    ver.SetValue(ReservedMetadataKeys.PerVertexLabel, words[k]);
                    //ver.SetValue(ReservedMetadataKeys.PerColor, colors[suffixIndex]);

                    if (degrees[k] == 0.0)
                        ver.SetValue(ReservedMetadataKeys.Visibility, VisibilityKeyValue.Hidden);

                    graph.Vertices.Add(ver);
                    vertices[k] = ver;
                }
            }

            for (int i = 0; i < size; i++)
            {
                for (int j = i+1; j < size; j++)
                {
                    if (g[i, j] > 0)
                    {
                        IEdge e = 
                        graph.Edges.Add(vertices[i], vertices[j]);
                        e.SetValue(ReservedMetadataKeys.EdgeWeight, g[i,j]);
                    }
                }
            }
           

            return graph;
        }
    }
}