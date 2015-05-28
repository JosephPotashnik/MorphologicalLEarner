using System;
using System.Collections.Generic;
using System.Linq;

namespace MorphologicalLearner
{

    public class BiCliqueFinder
    {

        public HashSet<string> BestLeftWords { get; set; }
        public HashSet<string> BestRightWords { get; set; }

        private static BigramManager m_bigramManager;

        public BiCliqueFinder(BigramManager bigramMan)
        {
            BestLeftWords = new HashSet<string>();
            BestRightWords = new HashSet<string>();
            m_bigramManager = bigramMan;
        }

        public void Find(string[] words, Learner.LocationInBipartiteGraph loc)
        {
            HashSet<string> L;
            if (loc == Learner.LocationInBipartiteGraph.RightWords)
                L = new HashSet<string>(m_bigramManager.GetUnionOfBigramsWithSecondWords(words));
            else
            {
                //switch locations.
                L = new HashSet<string>(words);
                words = m_bigramManager.GetUnionOfBigramsWithFirstWords(words).ToArray();
            }

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

                foreach (var v in Q)
                {
                    int k = CountPairswith(LL, v);
                    if (k == LL.Count())
                    {
                        is_Maximal = false;
                        break;
                    }
                    if (k > 0)
                        QQ.Add(v);
                }
                if (is_Maximal)
                {
                    foreach (var v in P)
                    {
                        if (v != candidate)
                        {
                            int k = CountPairswith(LL, v);
                            if (k == LL.Count())
                                RR.Add(v);
                            else if (k > 0)
                                PP.Add(v);
                        }
                    }

                    PrintBiClique(LL, RR);
                    if (PP.Any())
                        FindBiCliques(LL, RR, PP, QQ);
                }

                Q.Add(candidate);
            }
        }

        private void PrintBiClique(HashSet<string> LL, HashSet<string> RR)
        {
            string leftClique = string.Join(",", LL);
            string rightClique = string.Join(",", RR);
            Console.WriteLine("Left Clique: {0}, Right Clique: {1}, EdgeCount: {2}", leftClique, rightClique,
                (int) Math.Sqrt(LL.Count*RR.Count));

            if (RR.Count > BestRightWords.Count)
            {
                BestLeftWords = LL;
                BestRightWords = RR;
            }
        }

        private static int CountPairswith(HashSet<string> L, string v)
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
