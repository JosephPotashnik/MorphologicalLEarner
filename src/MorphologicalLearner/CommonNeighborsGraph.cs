using System.Collections.Generic;
using System.Linq;

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
            //there are two common neighbors graphs: the common neighbors of left words and of right words.
            LeftWordsNeighborhoods = ComputeCommonNeighborsGraphOf(leftWords, rightWords,
                BigramManager.LookupDirection.LookToRight, MinCommonNeighbors);
            RightWordsNeighborhoods = ComputeCommonNeighborsGraphOf(rightWords, leftWords,
                BigramManager.LookupDirection.LookToLeft, MinCommonNeighbors);
        }

        //this function gets two sets of words as arguments that represent a bipartite graph, and returns a common neighbors graph
        //the neighbors are computed for the argument "theseWords", I do not compute the common neighbor graph of the "otherWords".
        //the direction argument signifies "theseWords" should look to their left neighbors or to their right neighbors.
        private Dictionary<string, Dictionary<string, int>> ComputeCommonNeighborsGraphOf(string[] theseWords,
            string[] otherWords, BigramManager.LookupDirection dir, int MinCommonNeighbors)
        {
            var commonNeighborsGraph = new Dictionary<string, Dictionary<string, int>>();
            var commonNeighbors = 0;
            Dictionary<string, Dictionary<string, int>> dic = new Dictionary<string, Dictionary<string, int>>();

            foreach (var word1 in theseWords)
            {
                foreach (var word2 in theseWords)
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

                    //(we may be interested not in all possible words to the right/left (which is what IntesectTwoWords() returns)
                    //but only in some morphological subset of them).
                    commonNeighbors = bigramMan.IntersectTwoWords(word1, word2, dir).Intersect(otherWords).Count();

                    //add to common neighbors graph only if meets threshold.
                    if (commonNeighbors >= MinCommonNeighbors)
                    {

                        if (!commonNeighborsGraph.ContainsKey(word1)) 
                            commonNeighborsGraph[word1] = new Dictionary<string, int>();

                        if (!commonNeighborsGraph.ContainsKey(word2))
                            commonNeighborsGraph[word2] = new Dictionary<string, int>();

                        commonNeighborsGraph[word1][word2] = commonNeighbors;
                        commonNeighborsGraph[word2][word1] = commonNeighbors;
                    }
                }
            }

            return commonNeighborsGraph;
        }

        public List<Dictionary<string, Dictionary<string, int>>> StronglyConnectedComponents(
            Dictionary<string, Dictionary<string, int>> graph)
        {
            var Components = new List<Dictionary<string, Dictionary<string, int>>>();
            //get all vertices.
            var unvisitedNodes = new HashSet<string>(graph.Keys);

            var ComponentNodesToVisit = new Queue<string>();

            while (unvisitedNodes.Any())
            {
                ComponentNodesToVisit.Enqueue(unvisitedNodes.First());
                var currentComponent = new Dictionary<string, Dictionary<string, int>>();

                while (ComponentNodesToVisit.Any())
                {
                    var currentNode = ComponentNodesToVisit.Dequeue();
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
                                ComponentNodesToVisit.Enqueue(outNode);
                        }
                    }
                }
                Components.Add(currentComponent);
            }

            return Components;
        }
    }
}