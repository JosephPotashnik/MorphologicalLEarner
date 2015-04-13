using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;

namespace MorphologicalLearner
{
    public class CommonNeighborsGraph
    {
        private readonly BigramManager bigramMan;

        public CommonNeighborsGraph(BigramManager Man)
        {
            bigramMan = Man;
            LeftWordsNeighborhoods = new Dictionary<string, Dictionary<string, int>>();
            RightWordsNeighborhoods = new Dictionary<string, Dictionary<string, int>>();

        }

        public Dictionary<string, Dictionary<string, int>> LeftWordsNeighborhoods { get; private set; }
        public Dictionary<string, Dictionary<string, int>> RightWordsNeighborhoods { get; private set; }


        public void ComputeCommonNeighborsGraphs(string[] leftWords, string[] rightWords, int MinCommonNeighbors)
        {

            var adjMatrix = CreateAdjacencyMatrix(leftWords, rightWords);
            var neighborMatrix = CreateCommonNeighborsMatrix(adjMatrix);

            //there are two common neighbors graphs: the common neighbors of left words and of right words.
            //LeftWordsNeighborhoods = ComputeCommonNeighborsGraphOf(leftWords, rightWords,
             //   BigramManager.LookupDirection.LookToRight, MinCommonNeighbors);


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

        //this function gets two sets of words as arguments that represent a bipartite graph, and returns a common neighbors graph
        //the neighbors are computed for the argument "theseWords", I do not compute the common neighbor graph of the "otherWords".
        //the direction argument signifies "theseWords" should look to their left neighbors or to their right neighbors.
        private Dictionary<string, Dictionary<string, int>> ComputeCommonNeighborsGraphOf(Matrix<float> neighborMatrix,int IndexStart,int IndexEnd, int MinCommonNeighbors, string[] theseWords)
        {
            var commonNeighborsGraph = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, Dictionary<string, int>> dic = new Dictionary<string, Dictionary<string, int>>();

            for (int i = IndexStart; i < IndexEnd; i++)
            {
                for (int j = i; j < IndexEnd; j++)
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
            var commonNeighborMatrix = Matrix<float>.Build.Sparse(adjacencyMatrix.ColumnCount, adjacencyMatrix.ColumnCount);
            commonNeighborMatrix = adjacencyMatrix.Power(2);
            return commonNeighborMatrix;

        }


    }
}