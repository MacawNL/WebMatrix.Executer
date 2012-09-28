namespace DesignFactory.WebMatrix.Executer
{
    public class MessageGeneration
    {
        /// <summary>
        /// Format an error or warning message in standard Microsoft format.
        /// </summary>
        /// <param name="category">Category: Error or Warning</param>
        /// <param name="text">The text of the error or warning</param>
        /// <param name="code">The code of the error or warning</param>
        /// <param name="path">The path to the file containing the error</param>
        /// <param name="line">The line in the file where the error occured</param>
        /// <param name="column">The column in the file where the error occured</param>
        /// <returns>The composed error or warning string</returns>
        public static string Generate(TaskCategory category, string text, string code, string path, int line, int column)
        {
            string categoryString = category == TaskCategory.Error ? "error" : "warning";
            string textline;
            if (string.IsNullOrEmpty(path))
            {
                textline = string.Format(" {0} {1}: {2}", categoryString, code, text);
            }
            else
            {
                if (column == 0)
                {
                    textline = string.Format("{0}({1}): {2} {3}: {4}", path, line, categoryString, code, text);
                }
                else
                {
                    textline = string.Format("{0}({1},{2}): {3} {4}: {5}", path, line, column, categoryString, code, text);
                }
            }

            return textline;
        }
    }
}
