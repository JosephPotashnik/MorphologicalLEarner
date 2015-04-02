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
            var learn = new Learner(filenname);
            learn.Search(-1);
        }
    }
}