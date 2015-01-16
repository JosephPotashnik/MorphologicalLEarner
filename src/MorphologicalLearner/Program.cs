using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorphologicalLearner
{
    class Program
    {


        static void Main(string[] args)
        {

            Learner learn = new Learner();

            learn.BuildTrie();
            learn.BuildCandidates();

        }
    }
}
