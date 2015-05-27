using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace MorphologicalLearner
{
    public class LouvainMethod
    {
        Matrix<double> graph;
        private int[] communities; //the index of the array is the node index in the graph. the value is the community number
        private double[] degrees; //the index of the array is the node index in the graph. the value is the degree of the node
        private List<int>[] communityMembers; //the index of the array is the community number, the list is the indices of the nodes of its members.
        private int[][] nodeNeighbors; //the index of tha array is the node index in the graph, the list is the indices of its neighbors.
        private double[] communityWeights; //the index of the array is the community number, the value is its weight

        private double totalWeightOfGraph;

        public LouvainMethod(Matrix<double> _graph)
        {
            graph = _graph;
            communities = new int[graph.ColumnCount];
            degrees = new double[graph.ColumnCount];
            var rowsums = graph.RowSums();
            totalWeightOfGraph = 0;

            //initialize: 
            for (int i = 0; i < graph.ColumnCount; i++)
            {
                //at first each node is at a community of its own.
                communities[i] = i;
                var l = new List<int>();
                l.Add(i);
                communityMembers[i] = l;
                //also keep the weighted degrees of one node, i.e. the row sums
                degrees[i] = rowsums[i];

                totalWeightOfGraph += rowsums[i]; 
                //note: total weight of graph is actually twice of its real value because
                //the matrix is symmetric and I go over A(i,j) and A(j,i) which is the same edge.

                var n = new List<int>();
                for (int j = 0; j < graph.ColumnCount; j++)
                {
                    if (graph[i,j] > 0)
                        n.Add(j);
                }
                nodeNeighbors[i] = n.ToArray();

                communityWeights[i] = 0;
            }
        }

        public void FirstStep()
        {
            Queue<int> nodesToBeConsidered = new Queue<int>();
            for (int i = 0; i < graph.ColumnCount; i++)
                nodesToBeConsidered.Enqueue(i);

            while (nodesToBeConsidered.Any())
            {
                int currentNode = nodesToBeConsidered.Dequeue();
                //(note: we will enqueue the node back to the end of the queue if we find a modularity gain)

                //for each of the neighbors of the current node, look for the maximal modularity gain
                var neighbors = nodeNeighbors[currentNode];

                double MaxDeltaQFound = 0; //we allow only positive gains.
                double MaxWeightbetweenNodeAndCommunityFound = 0;
                int MaxCommunityFound = -1;

                foreach (var neighbor in neighbors)
                {
                    //if the neighbor and the node are in the same community, no modularity change, continue.
                    if (communities[neighbor] == communities[currentNode])
                        continue;
                    double weightbetweenNodeAndCommunity = 0;

                    double deltaQ = ComputeGain(currentNode, neighbor, out weightbetweenNodeAndCommunity);

                    //store the community of the maximal gain
                    //we also keep the weights between the node and the community, it will be added to the community weight
                    if (deltaQ > MaxDeltaQFound) 
                    {
                        MaxWeightbetweenNodeAndCommunityFound = weightbetweenNodeAndCommunity;
                        MaxDeltaQFound = deltaQ;
                        MaxCommunityFound = communities[neighbor];
                    }
                }

                //if no relocation of the node leads to modularity gain, continue;
                if (MaxCommunityFound == -1)
                    continue;

                //else, move the current node to the community of the neighbor whose modularity gain was maximal:
                nodesToBeConsidered.Enqueue(currentNode);
                communities[currentNode] = MaxCommunityFound;
                communityMembers[MaxCommunityFound].Add(currentNode);
                communityWeights[MaxCommunityFound] += MaxWeightbetweenNodeAndCommunityFound;
            }
                
           
        }

        private double ComputeGain(int currentNode, int neighbor, out double weightbetweenNodeAndCommunity)
        {
            //C = community of neighbor.
            int c = communities[neighbor];
            //Weights inside community:
            double weightsOfCommunity = communityWeights[c];

            //the sum of weights between the currentNode and the nodes in the community
            weightbetweenNodeAndCommunity = WeightBetweenNodeAndCommunity(currentNode, c);

            //the sum of weights of all edges ending in nodes in the community.
            double weightsEndinginCommunity = WeightsOfAllEdgesEndingInCommunity(c);

            double firstTerm = (weightsOfCommunity + weightbetweenNodeAndCommunity)/totalWeightOfGraph;
            double secondTerm = Math.Pow((weightsEndinginCommunity + degrees[currentNode])/totalWeightOfGraph, 2);
            double thirdTerm = weightsOfCommunity/totalWeightOfGraph;
            double fourthTerm = Math.Pow(weightsEndinginCommunity/totalWeightOfGraph, 2);
            double fifthTerm = Math.Pow(degrees[currentNode]/totalWeightOfGraph, 2);

            double DeltaQ = (firstTerm - secondTerm) - (thirdTerm - fourthTerm - fifthTerm);
            return DeltaQ;
        }

        private double WeightsOfAllEdgesEndingInCommunity(int community)
        {
            //we already have the weights insides the community (e.g. communityWeights[community]), 
            //we need to calculate only the weights of the edges leading from other communities to this one

            double WeightbetweenCommunityAndItsNeighbors = 0;
            foreach (int member in communityMembers[community]) //for every community member, 
            {
                foreach (int currentNeighbor in nodeNeighbors[member])
                {
                    //if the neighbor of the member is in the same community, continue.
                    if (communities[currentNeighbor] == community)
                        continue;

                    //else, add the weight between the member and the neighbor.
                    WeightbetweenCommunityAndItsNeighbors += graph[member, currentNeighbor];
                }
            }

            return WeightbetweenCommunityAndItsNeighbors + communityWeights[community];
        }

        private double WeightBetweenNodeAndCommunity(int currentNode, int community)
        {
            double weightsbetweenNodeAndCommunity = 0;
            for (int i = 0; i < nodeNeighbors[currentNode].Count(); i++)
            {
                int currentNeighborOfNode = nodeNeighbors[currentNode][i];
                //if the neighbor of the node is in the considered community, add to weight:
                if (communities[currentNeighborOfNode] == community)
                    weightsbetweenNodeAndCommunity += graph[currentNode, currentNeighborOfNode];
            }

            return weightsbetweenNodeAndCommunity;
        }
    }
}
