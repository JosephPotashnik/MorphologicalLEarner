using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorphologicalLearner
{
    public class CommonNeighborsGraph
    {
        private BigramManager bigramMan;

        private Dictionary<string, Dictionary<string, int>> CommonneighborGraphOfLeftWords;
        private Dictionary<string, Dictionary<string, int>> CommonneighborGraphOfRightWords;

        public Dictionary<string, Dictionary<string, int>> LeftWordsNeighborhoods   
        {
            get
            {
                return CommonneighborGraphOfLeftWords;
            }
        }

        public Dictionary<string, Dictionary<string, int>> RightWordsNeighborhoods
        {
            get
            {
                return CommonneighborGraphOfRightWords;
            }
        }

        public CommonNeighborsGraph( BigramManager Man)
        {
            bigramMan = Man;
            CommonneighborGraphOfLeftWords = new Dictionary<string, Dictionary<string, int>>();
            CommonneighborGraphOfRightWords = new Dictionary<string, Dictionary<string, int>>();

        }

        public void ComputeCommonNeighborsGraphs(string[] leftWords, string[] rightWords)
        {
            //there are two common neighbors graphs: the common neighbors of left words and of right words.
            CommonneighborGraphOfLeftWords = ComputeCommonNeighborsGraphOf(leftWords, rightWords, Learner.Direction.Left);
            CommonneighborGraphOfRightWords = ComputeCommonNeighborsGraphOf(rightWords, leftWords, Learner.Direction.Right);
        }

        //this function gets two sets of words as arguments that represent a bipartite graph, and returns a common neighbors graph
        //the neighbors are computed for the argument "theseWords", I do not compute the neighbors of the "otherWords".
        //the direction argument signifies whether "theseWords" are the left or right side of -the bipartite graph-.
        private Dictionary<string, Dictionary<string, int>> ComputeCommonNeighborsGraphOf(string[] theseWords, string[] otherWords, Learner.Direction dir)
        {
            var commonNeighborsGraph  = new Dictionary<string, Dictionary<string, int>>();

            foreach (var word1 in theseWords)
                commonNeighborsGraph[word1] = new Dictionary<string, int>();
            int commonNeighbors = 0;

            foreach (var word1 in theseWords)
            {
                foreach (var word2 in theseWords)
                {
                    if ((word1 == word2) ||
                        commonNeighborsGraph[word1].ContainsKey(word2) )
                        continue;

                    if (dir == Learner.Direction.Left)
                    
                    //take the common neighbors of two left words and intersect them with rightwords argument
                    //(because we may be interested not in all possible words to the right but only in some subset of them).
                        commonNeighbors = bigramMan.IntersectTwoFirstWords(word1, word2).Intersect(otherWords).Count();
                    else
                        commonNeighbors = bigramMan.IntersectTwoSecondWords(word1, word2).Intersect(otherWords).Count();

                    //if no common neighbors, don't create edges/values in dictionary (more economy)
                    if (commonNeighbors != 0)
                    {
                        commonNeighborsGraph[word1][word2] = commonNeighbors;
                        commonNeighborsGraph[word2][word1] = commonNeighbors;
                    }
                }
            }

            return commonNeighborsGraph;
        }

        public List<Dictionary<string, Dictionary<string, int>>> StronglyConnectedComponents(Dictionary<string, Dictionary<string, int>> graph)
        {
            var Components = new List<Dictionary<string,Dictionary<string, int>>>();
            //get all vertices.
            IEnumerable<string> nodes = graph.Keys.Select(node => graph[node].Keys).Aggregate<Dictionary<string, int>.KeyCollection, IEnumerable<string>>(graph.Keys, (current, values) => current.Union(values));

            HashSet<string> unvisitedNodes = new HashSet<string>(nodes);

            Queue<string> ComponentNodesToVisit = new Queue<string>();

            while (unvisitedNodes.Any())
            {
                ComponentNodesToVisit.Enqueue(unvisitedNodes.First());
                var currentComponent = new Dictionary<string, Dictionary<string, int>>();

                while (ComponentNodesToVisit.Any())
                {
                    string currentNode = ComponentNodesToVisit.Dequeue();
                    unvisitedNodes.Remove(currentNode);

                    if (graph.ContainsKey(currentNode))
                    {
                        if (!currentComponent.ContainsKey(currentNode))
                        {
                            var innerDic = new Dictionary<string, int>();
                            currentComponent[currentNode] = innerDic;
                        }
                        foreach (var outNode in graph[currentNode].Keys)
                        {
                            currentComponent[currentNode][outNode] = graph[currentNode][outNode];

                            if (unvisitedNodes.Contains(outNode))
                            {
                                ComponentNodesToVisit.Enqueue(outNode);
                            }
                           
                        }
                    }
                }
                Components.Add(currentComponent);
            }

            return Components;
        }  
    }
}
