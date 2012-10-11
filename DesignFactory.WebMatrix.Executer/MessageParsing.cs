using System;
using System.Text.RegularExpressions;

namespace DesignFactory.WebMatrix.Executer
{
    /// <summary>
    /// Parse text lines for the occurance of errors an warnings
    /// Any line containing the word 'error' or 'warning' is reported. We don't only parse output from Visual Studio/MsBuild,
    /// but also from external tools that don't use the Visual Studio/MsBuild guidelines for reporting errors and warnings.
    /// If the word 'error' or 'warning' is used in a filename, it should not be reported.
    /// If the word 'error' or 'warning' is used in a special construction, an ignore list with
    /// regular expressions can be specified, for example to exclude the line 'On [0-9]+ lines an error or warning occured'.
    /// If an error or warning complies to the Visual Studio/MSBuild error format, additional information is returned about the file,
    /// line number where the error or warning occured.
    /// Most important is that filenames containing the word error or warning are not reported as false positives.
    /// The following characters are assumed to be common filename characters: a-zA-Z0-9_-. 
    /// </summary>
    public class MessageParsing
    {
        // Defines the main pattern for a line containing a message to be processed (warning or an error)
        // trying not to match error or warning in filenames
        static string excludeCommonFilenameCharacters = @"[^a-zA-Z0-9_\-.]";
        static private readonly Regex simpleMessageExpression = new Regex
        (
            String.Format(@"(?<CATEGORY>({0}error{0}|{0}warning{0}))", excludeCommonFilenameCharacters),
            RegexOptions.IgnoreCase
        );

        // Defines the main pattern for matching MSBuild compliant messages.
        static private readonly Regex originCategoryCodeTextExpression = new Regex
        (
            // Beginning of line and any amount of whitespace.
            @"^\s*"
            // Match a [optional project number prefix 'ddd>'], single letter + colon + remaining filename, or
            // string with no colon followed by a colon.
            + @"(((?<ORIGIN>(((\d+>)?[a-zA-Z]?:[^:]*)|([^:]*))):)"
            // Origin may also be empty. In this case there's no trailing colon.
            + "|())"
            // Match the empty string or a string without a colon that ends with a space
            + "(?<SUBCATEGORY>(()|([^:]*? )))"
            // Match 'error' or 'warning' followed by a space.
            + @"(?<CATEGORY>(error|warning))\s*"
            // Match anything without a colon, followed by a colon
            + "(?<CODE>[^:]*):"
            // Whatever's left on this line, including colons.
            + "(?<TEXT>.*)$",
            RegexOptions.IgnoreCase
        );

        // Above 


        /// <summary>
        /// Parses the error or warning.
        /// </summary>
        /// <param name="singleLine">Single line of text of the messages file</param>
        /// <param name="messagesFilename">Filename of the messages file for simple errors and warnings in context</param>
        /// <param name="messagesLine">Line number in the messages file</param>
        /// <param name="ignoreList">List of regular expressions for message lines that should be ignored</param>
        /// <param name="category">The category: error or warning.</param>
        /// <param name="message">Resulting message in MSBuild message format</param>
        /// <returns>
        /// True if it was an error or warning line, false otherwise
        /// </returns>
        public static bool ParseErrorOrWarning(string singleLine, string messagesFilename, int messagesLine, Regex[] ignoreList, out TaskCategory category,  out string message)
        {
            string text;
            string code;
            string filename;
            int line;
            int column;

            message = String.Empty;

            bool isMessage = ParseErrorOrWarning(singleLine, messagesFilename, messagesLine, ignoreList, out category,
                                                 out text, out code, out filename, out line, out column);
            if (isMessage)
            {
                // Construct a proper message
                message = MessageGeneration.Generate(category, text, code, filename, line, column);
            }

            return isMessage;
        }

        /// <summary>
        /// Parse a line of text for error or warning, return the components of the message for construction of new message.
        /// All lines containing error or warning as a whole word are checked against an ignore list. If the line
        /// should not be ignored there are two possibilities:
        /// </summary>
        /// <param name="singleLine">Single line of text of the messages file</param>
        /// <param name="messagesFilename">Filename of the messages file for simple errors and warnings in context</param>
        /// <param name="messagesLine">Line number in the messages file</param>
        /// <param name="ignoreList">List of regular expressions for message lines that should be ignored</param>
        /// <param name="category">The category.</param>
        /// <param name="text">The text.</param>
        /// <param name="code">The code.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        /// <returns>
        /// True if it was an error or warning line, false otherwise
        /// </returns>
        public static bool ParseErrorOrWarning(
            string singleLine, string messagesFilename, int messagesLine, Regex[] ignoreList,
            out TaskCategory category, 
            out string text, 
            out string code, 
            out string filename, 
            out int line, 
            out int column)
        {
            category = TaskCategory.Unknown;
            text = String.Empty;
            code = String.Empty;
            filename = String.Empty;
            line = 0;
            column = 0;

            // Quick test is it might be a message line
            Match simple = simpleMessageExpression.Match(singleLine);
            if (simple.Success)
            {
                // Check against the ignoreList
                if (ignoreList != null)
                {
                    foreach (Regex ignore in ignoreList)
                    {
                        if (ignore.IsMatch(singleLine))
                        {
                            // Get out of here fast
                            return false;
                        }
                    }
                }
                
                // Test for MSBuild compliant message
                Match m = originCategoryCodeTextExpression.Match(singleLine);
                if (m.Success)
                {
                    string origin;
                    string categoryText;

                    // additional values that can come out of the message but which are ignored
                    string subcategory;
                    int endLine, endColumn;

                    // Message is MSBuild format compliant
                    origin = m.Groups[originCategoryCodeTextExpression.GroupNumberFromName("ORIGIN")].Value;
                    categoryText = m.Groups[originCategoryCodeTextExpression.GroupNumberFromName("CATEGORY")].Value;
                    code = m.Groups[originCategoryCodeTextExpression.GroupNumberFromName("CODE")].Value;
                    subcategory = m.Groups[originCategoryCodeTextExpression.GroupNumberFromName("SUBCATEGORY")].Value;
                    text = m.Groups[originCategoryCodeTextExpression.GroupNumberFromName("TEXT")].Value;

                    ParseOrigin(origin, out filename, out line, out column, out endLine, out endColumn);

                    category = GetCategory(categoryText);
                }
                else
                {
                    // Message is not MSBuild message format compliant, return the components in the context of the messages file
                    string categoryText = simple.Groups[simpleMessageExpression.GroupNumberFromName("CATEGORY")].Value;
                    category = GetCategory(categoryText);
                    text = singleLine;
                    code = "NoCode";
                    filename = messagesFilename;
                    line = messagesLine;
                    column = 1;
                }
                return true;
             
            }
            else
            {
                return false;
            }
        }

        private static TaskCategory GetCategory(string categoryText)
        {
            TaskCategory category = TaskCategory.Unknown;
            if (String.Compare(categoryText, "warning", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                category = TaskCategory.Warning;
            }
            if (String.Compare(categoryText, "error", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                category = TaskCategory.Error;
            }
            return category;
        }

        /// <summary>
        /// Parse a filename + linenumber in format file.abc(1), file.abc(1,2) or file.abc(1,2-3,4).
        /// </summary>
        /// <param name="origin">Original string.</param>
        /// <param name="filename">Filename found.</param>
        /// <param name="lineNumber">Line number found. 0 if no line number.</param>
        /// <param name="columnNumber">Column found. 0 if no column.</param>
        /// <param name="endLineNumber">End line number found. 0 if no end line number.</param>
        /// <param name="endColumnNumber">End column found. 0 if no end column.</param>
        private static void ParseOrigin(string origin, out string filename,
                     out int lineNumber, out int columnNumber,
                     out int endLineNumber, out int endColumnNumber)
        {
            int lParen;
            string[] temp;
            string[] left, right;

            origin = origin.Trim();
            if (origin.IndexOf('(') != -1)
            {
                lParen = origin.IndexOf('(');
                filename = origin.Substring(0, lParen).Trim();
                temp = origin.Substring(lParen + 1, origin.Length - lParen - 2).Split(',');
                if (temp.Length == 1)
                {
                    left = temp[0].Split('-');
                    if (left.Length == 1)
                    {
                        lineNumber = Int32.Parse(left[0]);
                        columnNumber = 0;
                        endLineNumber = 0;
                        endColumnNumber = 0;
                    }
                    else if (left.Length == 2)
                    {
                        lineNumber = Int32.Parse(left[0]);
                        columnNumber = 0;
                        endLineNumber = Int32.Parse(left[1]);
                        endColumnNumber = 0;
                    }
                    else
                    {
                        filename = origin;
                        lineNumber = 0;
                        columnNumber = 0;
                        endLineNumber = 0;
                        endColumnNumber = 0;
                    }
                }
                else if (temp.Length == 2)
                {
                    right = temp[1].Split('-');
                    lineNumber = Int32.Parse(temp[0]);
                    endLineNumber = 0;
                    if (right.Length == 1)
                    {
                        columnNumber = Int32.Parse(right[0]);
                        endColumnNumber = 0;
                    }
                    else if (right.Length == 2)
                    {
                        columnNumber = Int32.Parse(right[0]);
                        endColumnNumber = Int32.Parse(right[0]);
                    }
                    else
                    {
                        filename = origin;
                        lineNumber = 0;
                        columnNumber = 0;
                        endLineNumber = 0;
                        endColumnNumber = 0;
                    }
                }
                else if (temp.Length == 4)
                {
                    lineNumber = Int32.Parse(temp[0]);
                    endLineNumber = Int32.Parse(temp[2]);
                    columnNumber = Int32.Parse(temp[1]);
                    endColumnNumber = Int32.Parse(temp[3]);
                }
                else
                {
                    filename = origin;
                    lineNumber = 0;
                    columnNumber = 0;
                    endLineNumber = 0;
                    endColumnNumber = 0;
                }
            }
            else
            {
                filename = origin;
                lineNumber = 0;
                columnNumber = 0;
                endLineNumber = 0;
                endColumnNumber = 0;
            }
        }
 
    }
}
