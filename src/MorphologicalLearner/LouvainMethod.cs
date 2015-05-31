using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace MorphologicalLearner
{

    public class Community
    {
        public int communityIndex { get; set; }
        public List<int> communityMembers { get; set; } //the index of the array is the community number, the list is the indices of the nodes of its members.
        public double inWeights { get; set; }  //the  weights strictly inside the community
        public double totalWeights { get; set; } //the weights of all edges leading to nodes in the community

        public Community(int index, List<int> members, double inweights, double totalweights )
        {
            communityIndex = index;
            communityMembers = members;
            inWeights = inweights;
            totalWeights = totalweights;
        }
        public void RemoveNodeFromCommunity(int nodeIndex, double weightBetweenNodeAndCommunity, double selfLoopWeight, double nodeDegree)
        {
            communityMembers.Remove(nodeIndex);
            inWeights = inWeights - 2 * weightBetweenNodeAndCommunity - selfLoopWeight;
            totalWeights -= nodeDegree;
        }

        public void InsertNodeToCommunity(int nodeIndex, double weightBetweenNodeAndCommunity, double selfLoopWeight, double nodeDegree)
        {
            communityMembers.Add(nodeIndex);
            inWeights = inWeights + 2 * weightBetweenNodeAndCommunity + selfLoopWeight;
            totalWeights += nodeDegree;
        }

        public double Gain(double weightbetweenNodeAndCommunity, double nodeDegree, double totalWeightOfGraph)
        {
            double firstTerm = (inWeights + 2*weightbetweenNodeAndCommunity) / totalWeightOfGraph;
            double secondTerm = Math.Pow( ((totalWeights + nodeDegree) / totalWeightOfGraph), 2);
            double thirdTerm = inWeights / totalWeightOfGraph;
            double fourthTerm = Math.Pow((totalWeights / totalWeightOfGraph), 2);
            double fifthTerm = Math.Pow((nodeDegree / totalWeightOfGraph), 2);

            double DeltaQ = (firstTerm - secondTerm) - (thirdTerm - fourthTerm - fifthTerm);
            return DeltaQ; 

        }
    }
    public class LouvainMethod
    {
        Matrix<double> graph;
        
        private int[] nodeToCommunities; //the index of the array is the node index in the graph. the value is the community number
        private double[] degrees; //the index of the array is the node index in the graph. the value is the degree of the node
        private int[][] nodeNeighbors; //the index of tha array is the node index in the graph, the list is the indices of its neighbors.
        private Community[] communities;   
        private double totalWeightOfGraph;

        public LouvainMethod(Matrix<double> _graph)
        {
            graph = _graph;
            int size = graph.ColumnCount;
            nodeToCommunities = new int[size];
            degrees = new double[size];
            communities = new Community[size];
            nodeNeighbors = new int[size][];

            var rowsums = graph.RowSums();
            totalWeightOfGraph = 0;

            //initialize: 
            for (int i = 0; i < graph.ColumnCount; i++)
            {
                //at first each node is at a community of its own.
                nodeToCommunities[i] = i;
                //also keep the weighted degrees of one node, i.e. the row sums
                degrees[i] = rowsums[i];

                totalWeightOfGraph += rowsums[i]; 
                //note: total weight of graph is actually twice of its real value because
                //the matrix is symmetric and I go over A(i,j) and A(j,i) which is the same edge. (undirected)

                //init communities
                communities[i] = new Community(i, new List<int> {i}, graph[i, i], degrees[i]);

                var n = new List<int>();
                for (int j = 0; j < graph.ColumnCount; j++)
                {
                    if (graph[i,j] > 0)
                        n.Add(j);
                }
                nodeNeighbors[i] = n.ToArray();
            }
        }

        public Matrix<double> FirstStep(out Community[] foundCommunities)
        {

            bool improvement = true;
            HashSet<int> encounteredCommunities = new HashSet<int>();

            while (improvement)
            {
                improvement = false;
                //go over all nodes.
                for (int i = 0; i < graph.ColumnCount; i++)
                {
                    int currentNode = i;

                    var neighbors = nodeNeighbors[currentNode];

                    //if node has no neighbors, don't treat it at all (it cannot be moved to any neighboring community).
                    if (neighbors.Count() == 0)
                        continue;

                    double MaxDeltaQFound = -1000; //arbitrary minus.
                    int MaxCommunityFound = -1;

                    int oldCommunity = nodeToCommunities[currentNode];

                    communities[oldCommunity].RemoveNodeFromCommunity(currentNode,
                        WeightBetweenNodeAndCommunity(currentNode, oldCommunity), graph[currentNode, currentNode],
                        degrees[currentNode]);
                    //remove the node from the old community 
                    nodeToCommunities[currentNode] = -1;

                    //compute list of communities to go over.
                    encounteredCommunities.Add(oldCommunity);

                    foreach (var neighbor in neighbors)
                    {
                        int neighborCommunity = nodeToCommunities[neighbor];
                        if (neighborCommunity!= -1 && !encounteredCommunities.Contains(neighborCommunity))
                            encounteredCommunities.Add(neighborCommunity);
                    }

                    //go over all communities and find the community with the maximal gain
                    foreach (var neighborCommunity in encounteredCommunities)
                    {
                        double weightbetweenNodeAndCommunity = WeightBetweenNodeAndCommunity(currentNode,
                            neighborCommunity);
                        double deltaQ = communities[neighborCommunity].Gain(weightbetweenNodeAndCommunity,
                            degrees[currentNode], totalWeightOfGraph);

                        //store the community of the maximal gain
                        if (deltaQ > MaxDeltaQFound)
                        {
                            MaxDeltaQFound = deltaQ;
                            MaxCommunityFound = neighborCommunity;
                        }

                    }
                    encounteredCommunities.Clear();

                    if (MaxCommunityFound == -1)
                    {
                        throw new Exception();
                    }

                    communities[MaxCommunityFound].InsertNodeToCommunity(currentNode,
                        WeightBetweenNodeAndCommunity(currentNode, MaxCommunityFound), graph[currentNode, currentNode],
                        degrees[currentNode]);
                    nodeToCommunities[currentNode] = MaxCommunityFound;

                    //if the node was relocated
                    if (MaxCommunityFound != oldCommunity)
                        improvement = true;

                }
            }
            //end of first step.

            //preparations for second step.
            foundCommunities = communities.Where(x => x.communityMembers.Count > 0).ToArray();
            Dictionary<int, int> mapFromOldToFoundCommunityIndices = new Dictionary<int, int>();
            int currentIndex = 0;
            foreach (var community in foundCommunities)
            {
                mapFromOldToFoundCommunityIndices[community.communityIndex] = currentIndex++;
            }
            int newSize = foundCommunities.Count();
            var communityMatrix = Matrix<double>.Build.Sparse(newSize, newSize);

            ////for self-loops, the weight is the inweights of the community.
            //foreach (var community in foundCommunities)
            //{
            //    int newIndex = mapFromOldToFoundCommunityIndices[community.communityIndex];
            //    communityMatrix[newIndex, newIndex] = community.inWeights;
            //}
            
            ////for edges belonging to two different communities, add weights.
            for (int j = 0; j < graph.ColumnCount; ++j)
            {
                for (int k = j; k < graph.ColumnCount; ++k)
                {
                    double weight = graph[j, k];
                    if (weight > 0)
                    {
                        int c1 = mapFromOldToFoundCommunityIndices[nodeToCommunities[j]];
                        int c2 = mapFromOldToFoundCommunityIndices[nodeToCommunities[k]];

                            communityMatrix[c1, c2] += weight;
                            communityMatrix[c2, c1] += weight;
                    }
                }
            }


            return communityMatrix;
        }

        public double Modularity() 
        {
          double q  = 0;

          foreach (var community in communities)
          {
                if (community.totalWeights > 0)
                        q += community.inWeights/totalWeightOfGraph - Math.Pow((community.totalWeights/totalWeightOfGraph), 2);
          }
          return q;
        }

        public void SecondStep()
        {
            //now build a new Matrix<double> object from the results of the first step

            //each community is a single node
            //the weights between the nodes are the sum of all weights of edges going between the two communities.
            //there are also self-loops (the weights of the community, we already calculated that in communityWeights[])

        }

        private double WeightBetweenNodeAndCommunity(int currentNode, int community)
        {
            double weightsbetweenNodeAndCommunity = 0;
            for (int i = 0; i < nodeNeighbors[currentNode].Count(); i++)
            {
                int currentNeighborOfNode = nodeNeighbors[currentNode][i];
                //if the neighbor of the node is in the considered community (and is not a self loop), add to weight:
                if (nodeToCommunities[currentNeighborOfNode] == community && currentNeighborOfNode != currentNode)
                    weightsbetweenNodeAndCommunity += graph[currentNode, currentNeighborOfNode];
            }

            return weightsbetweenNodeAndCommunity;
        }
    }
}
