using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorphologicalLearner
{
    public struct StringAlignmentData
    {
        public TrieNode Father { get; set; }
        public TrieNode Son { get; set; }
        public string Difference { get; set; }  //for now it will be just a string, later it will have to be something more complicated.
    }
}
