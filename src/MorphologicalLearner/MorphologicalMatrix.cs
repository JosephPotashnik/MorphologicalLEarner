using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;

namespace MorphologicalLearner
{
    public class MorphologicalMatrix
    {
        private const string BeginningOfMatrix = @"..\..\..\..\SampleMatrix.csv";

        
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
        public void PrintNColumnsOfMatrix(int NumberOfColumns)
        {
            var ShortStemList = new string[NumberOfColumns];
            Array.Copy(stemArray, ShortStemList, NumberOfColumns);

            var submat = matrix.SubMatrix(0, matrix.RowCount, 0, NumberOfColumns);
            DelimitedWriter.Write(BeginningOfMatrix, submat, ",", ShortStemList);
        }
    }
}
