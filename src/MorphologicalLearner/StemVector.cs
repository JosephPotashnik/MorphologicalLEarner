using System.Collections.Generic;
using System.Linq;

namespace MorphologicalLearner
{
    public class StemVector
    {
        private readonly Dictionary<string, List<KeyValuePair<string, string>>> m_derivedFormsDic;
        //the stem, and list of all suffixes that apply to the stem.
        private readonly Dictionary<string, List<string>> m_dic;

        public StemVector()
        {
            m_dic = new Dictionary<string, List<string>>();
            m_derivedFormsDic = new Dictionary<string, List<KeyValuePair<string, string>>>();
        }

        public Dictionary<string, List<string>> StemDic()
        {
            return m_dic;
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

        public void AddDerivedForm(string stemName, KeyValuePair<string, string> derivedFormPair)
        {
            if (!m_derivedFormsDic.ContainsKey(stemName))
                m_derivedFormsDic[stemName] = new List<KeyValuePair<string, string>>();

            m_derivedFormsDic[stemName].Add(derivedFormPair);
        }

        public string[] GetAllStems()
        {
            return m_dic.Select(k => k.Key).ToArray();
        }

        public List<KeyValuePair<string, string>> GetAllDerivedForms(string stem)
        {
            return m_derivedFormsDic[stem];
        }
    }
}