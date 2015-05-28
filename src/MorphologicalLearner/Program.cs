using System;

namespace MorphologicalLearner
{
    internal class Program
    {
        public string[] Args { get; set; }

        [STAThread]
        private static void Main(string[] args)
        {
            string filenname = "David Copperfield";
            //string filenname = "CorwinBooks";
            var learner = new Learner(filenname);
            var candidates = learner.LookForSyntacticCategoryCandidates();
            //learner.EvaluateSyntacticCategoryOfCandidates(candidates);
        }
    }
}