using System;
using System.Collections.Generic;
using System.Linq;
using RDotNet;
using Smrf.NodeXL.Algorithms;
using Smrf.NodeXL.Core;

namespace MorphologicalLearner
{
    internal class Program
    {
        public string[] Args { get; set; }

        [STAThread]
        private static void Main(string[] args)
        {
            //string filenname = "David Copperfield";
            string filenname = "CorwinBooks";
            var learner = new Learner(filenname);
            var candidates = learner.LookForSyntacticCategoryCandidates();
            //learner.EvaluateSyntacticCategoryOfCandidates(candidates);
        }
    }
}