using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MorphologicalLearner
{

    public class SuffixVector
    {
        private const string SuffixStatistics = @"..\..\..\..\David Copperfield Suffix Statistics.txt";

        //the suffix, and list of all stems that this suffix applies to.
        private Dictionary<string, List<string>> m_dic;
        public SuffixVector ()
	{
            Total = 0;
            m_dic = new Dictionary<string, List<string>>();
	}
        public int Total { get; set; }
        
        public void Add(string diff, string stem)
        {
            if (!m_dic.ContainsKey(diff))
                m_dic[diff] = new List<string>();

            m_dic[diff].Add(stem);
            Total++;
        }


        //write functions that return rules passing a certain threshold.

        //1. absolute threshold: rules that appear more than N times.
        public List<KeyValuePair<string, List<string>>> RulesAboveNTimesThreshold(int threshold)
        {
            return m_dic.Where(c => c.Value.Count >= threshold).Select(c => new KeyValuePair<string,List<string>>(c.Key, c.Value)).ToList();
        }

        //2. relative frequency threshold: rules that appear more than N/Total times.
        public void LeaveOnlySuffixesAboveFrequencyThreshold(double frequency)
        {

            var newdict = m_dic
                .Where(c => (double) c.Value.Count/Total >= frequency)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            m_dic = newdict;
        }
        //3. other distributional threshold: the rules that take the most of the distribution function. 

        //I still do not know how the distribution of the rules candidates looks like. Is it a normal distribution?.. depends on the data of the language.

        public void Statistics()
        {
            var sb = new StringBuilder();
            sb.Append("Suffix \t #Appearances");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var valuepair in m_dic)
            {
                sb.AppendFormat("{0} \t {1}", valuepair.Key, valuepair.Value.Count);
                sb.AppendLine();
            }

            File.WriteAllText(SuffixStatistics, sb.ToString());
        }
   }
}
