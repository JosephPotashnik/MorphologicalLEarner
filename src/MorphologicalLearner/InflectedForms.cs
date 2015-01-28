using System;
using System.Collections.Generic;

namespace MorphologicalLearner
{
    public class InflectedForms
    {
        //the inflected form, and               the stem, the suffix it corresponds to 
        private readonly Dictionary<string, KeyValuePair<string, string>> m_dic;

        public InflectedForms()
	    {
            m_dic = new Dictionary<string, KeyValuePair<string, string>>();
	    }

        public void Add(string inflectedForm, string stemName, string suffix)
        {
            if (m_dic.ContainsKey(inflectedForm))
                throw new Exception("encountered the same derived form in two contexts");

            m_dic[inflectedForm] = new KeyValuePair<string, string>(stemName, suffix);
        }
    }
}
