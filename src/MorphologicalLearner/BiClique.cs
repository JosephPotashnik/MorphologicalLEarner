using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorphologicalLearner
{
    public class BiPartiteGraph
    {
        
    }
    public class BiCliqueFinder
    {
        private BigramManager m_bigramManager;

        public BiCliqueFinder(BigramManager bigramMan)
        {
            m_bigramManager = bigramMan;
        }


                              // the set V
        public void Find(string[] words, Learner.LocationInBipartiteGraph loc)
        {
            HashSet<string> L;
            //L is the neighbors of V
            if (loc == Learner.LocationInBipartiteGraph.RightWords)
                L = new HashSet<string>(m_bigramManager.GetUnionOfBigramsWithSecondWords(words));
            else
                L = new HashSet<string>(m_bigramManager.GetIntersectOfBigramsWithFirstWords(words));

            //the set of words currently in the maximal biclique.
            HashSet<string> R = new HashSet<string>();
            //the set of words that may be added to the current biclique.
            HashSet<string> P = new HashSet<string>(words);
            //thse set of words used to determine maximality
            HashSet<string> Q = new HashSet<string>();

            FindBiCliques(L, R, P, Q);

        }

        private void FindBiCliques(HashSet<string> L, HashSet<string> R, HashSet<string> P, HashSet<string> Q)
        {

            while (P.Any())
            {
                //select candidate from P
                string candidate = P.First();
                P.Remove(candidate);

                HashSet<string> RR = new HashSet<string>(R);
                //extend biclique.
                RR.Add(candidate);

                HashSet<string> LL = new HashSet<string>();
                foreach (var u in L)
                {
                    if (m_bigramManager.Exists(u, candidate))
                        LL.Add(u);
                }

                HashSet<string> PP = new HashSet<string>();
                HashSet<string> QQ = new HashSet<string>();

                bool is_Maximal = true;
                //Dictionary<string, int> N = new Dictionary<string, int>();

                foreach (var v in Q)
                {
                    int k = CountPairswith(LL, v);
                    //N[v] = k;

                    if (k == LL.Count())
                    {
                        is_Maximal = false;
                        break;
                    }
                    else if (k > 0)
                        QQ.Add(v);

                }
                if (is_Maximal == true)
                {
                    foreach (var v in P)
                    {
                        if (v != candidate)
                        {
                            int k = CountPairswith(LL, v);
                            //N[v] = k;
                            if (k == LL.Count())
                                RR.Add(v);
                            else if (k > 0)
                                PP.Add(v);

                        }
                    }

                    //print maximal biclique here:
                    string leftClique = string.Join(",", LL);
                    string rightClique = string.Join(",", RR);
                    Console.WriteLine("Left Clique: {0}, Right Clique: {1}", leftClique, rightClique);
                    if (PP.Any())
                        FindBiCliques(LL, RR, PP, QQ);

                }

                Q.Add(candidate);

            }
        }

        private int CountPairswith(HashSet<string> L, string v)
        {
            int k = 0;
            foreach (var u in L)
            {
                if (m_bigramManager.Exists(u, v))
                    k++;
            }
            return k;
        }
    
    }
}
