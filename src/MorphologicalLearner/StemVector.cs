using System.Collections.Generic;
using System.Linq;

namespace MorphologicalLearner
{
    public class StemVector
    {
        //the stem, and list of all suffixes that apply to the stem.
        private Dictionary<string, List<string>> m_dic;
        public Dictionary<string, List<string>> StemDic() {  return m_dic; }

        public StemVector()
	    {
            m_dic = new Dictionary<string, List<string>>();
	    }

        public int NumberOfStems()
        {

            return m_dic.Keys.Count;
        }

        public void Add(string stemName, string suffix)
        {
            if (!m_dic.ContainsKey(stemName))
                m_dic[stemName] = new List<string>();

            m_dic[stemName].Add(suffix);
        }

        public string[] GetAllStems()
        {
            return m_dic.Select(k => k.Key).ToArray();
        }
    }
}
