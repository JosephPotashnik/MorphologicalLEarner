﻿using System.Collections.Generic;
using System.Linq;

namespace MorphologicalLearner
{
    public class RulesCandidatesDictionary
    {
        private Dictionary<string, int> m_dic;
        public RulesCandidatesDictionary ()
	{
            Total = 0;
            m_dic = new Dictionary<string, int>();
	}
        public int Total { get; set; }
        
        public void Add(string diff)
        {
            if (m_dic.ContainsKey(diff))
                m_dic[diff]++;      //increment value.
            else
            {
                //create new key-value with first appearance
                m_dic[diff] = 1;
            }
            Total++;
        }


        //write functions that return rules passing a certain threshold.

        //1. absolute threshold: rules that appear more than N times.
        public List<string> RulesAboveNTimesThreshold(int threshold)
        {
            return m_dic.Where(c => c.Value > threshold).Select(c => c.Key).ToList();
        }

        //2. relative frequency threshold: rules that appear more than N/Total times.
        //3. other distributional threshold: the rules that take the most of the distribution function. 

        //I still do not know how the distribution of the rules candidates looks like. Is it a normal distribution?.. depends on the data of the language.
   }
}
