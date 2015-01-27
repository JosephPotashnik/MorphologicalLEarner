using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorphologicalLearner
{
    public class StemVector
    {
        //the stem, and list of all suffixes that apply to the stem.
        private Dictionary<string, List<string>> m_dic;
        public int Total { get; set; }

        public StemVector()
	    {
            Total = 0;
            m_dic = new Dictionary<string, List<string>>();
	    }

        public void Add(string stemName, string suffix)
        {
            if (!m_dic.ContainsKey(stemName))
                m_dic[stemName] = new List<string>();

            m_dic[stemName].Add(suffix);
            Total++;
        }
    }
}
