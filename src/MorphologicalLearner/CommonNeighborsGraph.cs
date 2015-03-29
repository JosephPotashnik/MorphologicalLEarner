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

        private Dictionary<string, Dictionary<string, int>> neighborGraphOfLeftWords;
        private Dictionary<string, Dictionary<string, int>> neighborGraphOfRightWords;

        public CommonNeighborsGraph( BigramManager Man)
        {
            bigramMan = Man;
            neighborGraphOfLeftWords = new Dictionary<string, Dictionary<string, int>>();
            neighborGraphOfRightWords = new Dictionary<string, Dictionary<string, int>>();

        }

        public void ComputeNeighborsGraphs(IEnumerable<string> leftWords, IEnumerable<string> rightWords)
        {
            //there are two common neighbors graphs: the common neighbors of left words and of right words.
            neighborGraphOfLeftWords = ComputeCommonNeighborsGraphOf(leftWords, rightWords, Learner.Direction.Left);
            neighborGraphOfLeftWords = ComputeCommonNeighborsGraphOf(rightWords, leftWords, Learner.Direction.Right);
        }

        //this function gets two sets of words as arguments that represent a bipartite graph, and returns a common neighbors graph
        //the neighbors are computed for the argument "theseWords", I do not compute the neighbors of the "otherWords".
        //the direction argument signifies whether "theseWords" are the left or right side of the bipartite graph.
        private Dictionary<string, Dictionary<string, int>> ComputeCommonNeighborsGraphOf(IEnumerable<string> theseWords, IEnumerable<string> otherWords, Learner.Direction dir)
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
                        commonNeighbors = bigramMan.IntersectTwoSecondWords(word1, word2).Intersect(otherWords).Count();
                    else
                        commonNeighbors = bigramMan.IntersectTwoFirstWords(word1, word2).Intersect(otherWords).Count();


                    commonNeighborsGraph[word1][word2] = commonNeighbors;
                    commonNeighborsGraph[word2][word1] = commonNeighbors;
                }
            }

            return commonNeighborsGraph;
        }
    }
}
