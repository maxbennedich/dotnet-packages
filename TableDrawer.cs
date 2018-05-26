using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace DotNetPackages
{
    public static class TableDrawer
    {
        public static void DrawTable<T>(IEnumerable<T> valuesEnumerable, string[] columnHeaders, Func<T, AnsiColor> colorSelector, params Func<T, object>[] valueSelectors)
        {
            Debug.Assert(columnHeaders.Length == valueSelectors.Length);

            T[] values = valuesEnumerable.ToArray();
            var colors = new AnsiColor[values.Length + 1];
            var matrixValues = new string[values.Length + 1, valueSelectors.Length];

            // headers
            colors[0] = AnsiColor.White;
            for (int column = 0; column < matrixValues.GetLength(1); column++)
                matrixValues[0, column] = columnHeaders[column];

            // table rows
            for (int row = 1; row < matrixValues.GetLength(0); row++)
            {
                colors[row] = colorSelector.Invoke(values[row - 1]);
                for (int column = 0; column < matrixValues.GetLength(1); column++)
                    matrixValues[row, column] = Convert.ToString(valueSelectors[column].Invoke(values[row - 1]));
            }

            DrawStringTable(matrixValues, colors);
        }

        private static void DrawStringTable(this string[,] matrixValues, AnsiColor[] colors)
        {
            int[] columnWidths = GetMaxColumnWidths(matrixValues);
            string headerDivider = new string('-', columnWidths.Sum(i => i + 3) - 1);

            for (int row = 0; row < matrixValues.GetLength(0); row++)
            {
                for (int column = 0; column < matrixValues.GetLength(1); column++)
                {
                    string cell = matrixValues[row, column].PadRight(columnWidths[column]);
                    Console.Write($" | {cell.Color(colors[row])}");
                }

                Console.WriteLine(" |");

                // Print divider after header
                if (row == 0)
                    Console.WriteLine($" |{headerDivider}| ");
            }
        }

        private static int[] GetMaxColumnWidths(string[,] matrixValues)
        {
            var maxColumnWidths = new int[matrixValues.GetLength(1)];

            for (int column = 0; column < matrixValues.GetLength(1); column++)
                for (int row = 0; row < matrixValues.GetLength(0); row++)
                    maxColumnWidths[column] = Math.Max(maxColumnWidths[column], matrixValues[row, column].Length);

            return maxColumnWidths;
        }
    }
}