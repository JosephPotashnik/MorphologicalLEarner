using System;

namespace MorphologicalLearner
{
    internal class Program
    {
        public string[] Args { get; set; }

        [STAThread]
        private static void Main(string[] args)
        {
            var learn = new Learner();

            learn.BuildBigramsandTrie();
            learn.BuildMorphologicalMatrix();
            learn.Search();
        }
    }
}