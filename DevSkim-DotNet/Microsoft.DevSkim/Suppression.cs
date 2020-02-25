﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Processor for rule suppressions
    /// </summary>
    public class Suppression
    {
        const string KeywordPrefix = "DevSkim:";
        const string KeywordIgnore = "ignore";        
        const string KeywordAll = "all";
        const string KeywordUntil = "until";
        public const string pattern = KeywordPrefix + @"\s+" + KeywordIgnore + @"\s([a-zA-Z\d,:]+)(\s+" + KeywordUntil + @"\s\d{4}-\d{2}-\d{2}|)";
        Regex reg = new Regex(pattern);

        TextContainer _text;
        int _lineNumber;
        string _lineText;
        Language _language;

        /// <summary>
        /// Creates new instance of Supressor
        /// </summary>
        /// <param name="text">Text to work with</param>        
        public Suppression(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            _lineText = text;

            ParseLine();
        }

        public Suppression(TextContainer text, int lineNumber)
        {
            _text = text;
            _lineNumber = lineNumber;

            ParseLine();
        }

        /// <summary>
        /// Test if given rule Id is being suppressed
        /// </summary>
        /// <param name="issueId">Rule ID</param>
        /// <returns>True if rule is suppressed</returns>
        public SuppressedIssue GetSuppressedIssue(string issueId)
        {
            bool result = false;
            SuppressedIssue issue = _issues.FirstOrDefault(x => x.ID == issueId || x.ID == KeywordAll);
            if (issue != null)
                result = true;

            if (DateTime.Now < _expirationDate && result)
                return issue;
            else
                return null;
        }

        /// <summary>
        /// Parse the line of code to find rule suppressors
        /// </summary>
        private void ParseLine()
        {
            if (_text != null)
            {
                _lineText = _text.GetLineContent(_lineNumber);
                // If the line with the issue doesn't contain a suppression check the lines above it
                if (!_lineText.Contains(KeywordPrefix))
                {
                    if (_lineNumber > 1)
                    {
                        var content = _text.GetLineContent(--_lineNumber);
                        if (content.Contains(Language.GetCommentSuffix(_text.Language)))
                        {
                            while (_lineNumber >= 1)
                            {
                                if (reg.IsMatch(_text.GetLineContent(_lineNumber)))
                                {
                                    _lineText = _text.GetLineContent(_lineNumber);
                                    break;
                                }
                                else if (_text.GetLineContent(_lineNumber).Contains(Language.GetCommentPrefix(_text.Language)))
                                {
                                    break;
                                }
                                _lineNumber--;
                            }
                        }
                        else if (content.Contains(Language.GetCommentInline(_text.Language)))
                        {
                            _lineText = content;
                        }
                    }
                }
            }

            Match match = reg.Match(_lineText);

            if (match.Success)
            {
                _suppressStart = match.Index;
                _suppressLength = match.Length;

                string idString = match.Groups[1].Value.Trim();
                IssuesListIndex = match.Groups[1].Index;

                // Parse date
                if (match.Groups.Count > 2)
                {
                    string date = match.Groups[2].Value;
                    reg = new Regex(@"(\d{4}-\d{2}-\d{2})");
                    Match m = reg.Match(date);
                    if (m.Success)
                    {
                        try
                        {
                            _expirationDate = DateTime.ParseExact(m.Value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (FormatException)
                        {
                            _expirationDate = DateTime.MinValue;
                        }
                    }
                }

                // parse Ids.                
                if (idString == KeywordAll)
                {
                    _issues.Add(new SuppressedIssue()
                    {
                        ID = KeywordAll,
                        Boundary = new Boundary()
                        {
                            Index = IssuesListIndex,
                            Length = KeywordAll.Length
                        }
                    });
                }
                else
                {
                    string[] ids = idString.Split(',');
                    int index = IssuesListIndex;
                    foreach (string id in ids)
                    {

                        _issues.Add(new SuppressedIssue()
                        {
                            ID = id,
                            Boundary = new Boundary()
                            {
                                Index = index,
                                Length = id.Length
                            }
                        });
                        index += id.Length + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Get issue IDs for the suppression
        /// </summary>
        /// <returns>List of issue IDs</returns>
        public virtual SuppressedIssue[] GetIssues() 
        {
            return _issues.ToArray();
        }
        
        /// <summary>
        /// Validity of suppression expresion
        /// </summary>
        /// <returns>True if suppression is in effect</returns>
        public bool IsInEffect {
            get
            {
                bool doesItExists = (Index >= 0 && _issues.Count > 0);
                return (doesItExists && DateTime.Now < _expirationDate);
            }
        }

        /// <summary>
        /// Suppression expiration date
        /// </summary>
        public DateTime ExpirationDate { get { return _expirationDate; } }

        /// <summary>
        /// Suppression expresion start index on the given line
        /// </summary>
        public int Index { get { return _suppressStart; } }        

        /// <summary>
        /// Suppression expression length
        /// </summary>
        public int Length { get { return _suppressLength; } }

        /// <summary>
        /// Position of issues list
        /// </summary>
        public int IssuesListIndex { get; set; } = -1;

        private List<SuppressedIssue> _issues = new List<SuppressedIssue>();
        private DateTime _expirationDate = DateTime.MaxValue;

        private int _suppressStart = -1;
        private int _suppressLength = -1;        
    }
}
