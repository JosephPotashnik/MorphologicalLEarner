using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;

namespace MorphologicalLearner
{
    public class MorphologicalMatrix
    {
        private const string BeginningOfMatrix = @"..\..\..\..\SampleMatrix.csv";
        private const string SuffixDistributions = @"..\..\..\..\David Copperfield Suffix Distributions.txt";
        
        private readonly string[] stemArray;
        private readonly string[] suffixArray;
        private Dictionary<string, int> stemDic;
        private Dictionary<string, int> suffixDic;
        private Matrix<float> matrix;

        public Matrix<float> Matrix {
            get { return matrix; }
        }

        private string StemAt(int index)
        {
            return stemArray[index];
        }

        private string SuffixAt(int index)
        {
            return suffixArray[index];
        }

        public MorphologicalMatrix(StemVector stems, SuffixVector suffixes)
        {
            stemArray = stems.GetAllStems();
            suffixArray = suffixes.GetAllSuffixes();

            stemDic = new Dictionary<string, int>(); //get name of stem, returns index of the column in the matrix.
            suffixDic = new Dictionary<string, int>(); //get name of suffix, return index of the row in the matrix.

            int i = 0;
            foreach (var s in stemArray)
                stemDic[s] = i++;

            i = 0;
            foreach (var s in suffixArray)
                suffixDic[s] = i++;

            matrix = Matrix<float>.Build.Dense(suffixArray.Count(), stemArray.Count());

            //Dictionary<string, List<string>>
            var ListOfStemsAndInflectedForms = stems.StemDic();
            foreach (var kvp in ListOfStemsAndInflectedForms)
            {
                foreach (var suffix in kvp.Value)
                {
                    if (suffixDic.ContainsKey(suffix))
                        matrix[suffixDic[suffix], stemDic[kvp.Key]] = 1;
                }
            }
        }

        public KeyValuePair<int, string>[] ComputerMorphologicalLabels(Matrix<float> mat)
        {
            Vector<float>[] Columns = mat.EnumerateColumns().ToArray();
            var ColumnBasis = Columns.Distinct().ToArray();

            //int is the index of the column basis , string is the suffixes corresponding to that column
            KeyValuePair<int, string>[] MorphologicalLabels = new KeyValuePair<int, string>[Columns.Count()];
            String[] suffixParticipatinginColumn = new string[ColumnBasis.Count()];


            //compute the set of suffixes that this column represents
            //put it into suffixParticipatinginColumn[j].
            int i = 0;

            foreach (var col in ColumnBasis)
            {
               var SuffixNamesForColumn = col.Zip(suffixArray, 
                   (f, s) => new {Number = f, SuffixName = s} );
               suffixParticipatinginColumn[i++] = String.Join(",", SuffixNamesForColumn.Where(c => c.Number > 0).Select(c => c.SuffixName));
            }
           
            //go over all stems in the matrix, and for each of them find which column basis 
            //represents it. for all k columns, choose one of the j column base.
            for (int k = 0; k < Columns.Count(); ++k)
            {
                for (int j = 0; j < ColumnBasis.Count(); ++j)
                {
                    if (!Columns[k].Equals(ColumnBasis[j])) continue;
                    MorphologicalLabels[k] = new KeyValuePair<int, string>(j, suffixParticipatinginColumn[j]);
                    break;
                }
            }
            return MorphologicalLabels;
        }

        public void PrintNColumnsOfMatrix(int NumberOfColumns, float relativeFrequency)
        {
            var ShortStemList = new string[NumberOfColumns];
            Array.Copy(stemArray, ShortStemList, NumberOfColumns);

            var submat = matrix.SubMatrix(0, matrix.RowCount, 0, NumberOfColumns);
            //DelimitedWriter.Write(BeginningOfMatrix, submat, ",", ShortStemList);

            KeyValuePair<int, string>[] MorphologicalLabels = ComputerMorphologicalLabels(submat);
            var StemLabel = ShortStemList.Zip(MorphologicalLabels, (a, b) => new
            {
                StemName = a,
                Labels = b //which are: KeyValuePair<int, string>>
            });
            var groups = StemLabel.GroupBy(c => c.Labels.Value);

            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("The groups for {0} word types (distinct words) of David copperfiled are", NumberOfColumns));
            sb.AppendLine();
            sb.Append(String.Format("Only suffixes with frequency above {0}% were chosen ", relativeFrequency*100));
            sb.AppendLine();
            sb.AppendLine();


            foreach (var g in groups)
            {
                sb.AppendFormat("Ending with {{{0}}} are:{1} {2} {3}", g.Key, Environment.NewLine, String.Join(",", g.Select(c => c.StemName)), Environment.NewLine);
                sb.AppendLine();
            }
            File.WriteAllText(SuffixDistributions, sb.ToString());
        }
    }
}
