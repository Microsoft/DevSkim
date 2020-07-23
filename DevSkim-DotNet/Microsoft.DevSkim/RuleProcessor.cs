﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[assembly: CLSCompliant(true)]

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Heart of DevSkim. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        /// <summary>
        ///     Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(RuleSet rules)
        {
            _ruleset = rules;
            _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            EnableSuppressions = false;
            EnableCache = true;

            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice;
        }

        /// <summary>
        ///     Enables caching of rules queries. Increases performance and memory use!
        /// </summary>
        public bool EnableCache { get; set; }

        /// <summary>
        ///     Enable suppresion syntax checking during analysis
        /// </summary>
        public bool EnableSuppressions { get; set; }

        /// <summary>
        ///     Ruleset to be used for analysis
        /// </summary>
        public RuleSet Rules
        {
            get { return _ruleset; }
            set
            {
                _ruleset = value;
                _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            }
        }

        /// <summary>
        ///     Sets severity levels for analysis
        /// </summary>
        public Severity SeverityLevel { get; set; }

        /// <summary>
        ///     Applies given fix on the provided source code line
        /// </summary>
        /// <param name="text"> Source code line </param>
        /// <param name="fixRecord"> Fix record to be applied </param>
        /// <returns> Fixed source code line </returns>
        public static string Fix(string text, CodeFix fixRecord)
        {
            string result = string.Empty;

            if (fixRecord.FixType == FixType.RegexReplace)
            {
                if (fixRecord.Pattern is { })
                {
                    //TODO: Better pattern search and modifiers
                    Regex regex = new Regex(fixRecord.Pattern.Pattern);
                    result = regex.Replace(text, fixRecord.Replacement);
                }
            }

            return result;
        }

        /// <summary>
        ///     Analyzes given line of code
        /// </summary>
        /// <param name="text"> Source code </param>
        /// <param name="language"> Language </param>
        /// <returns> Array of matches </returns>
        public Issue[] Analyze(string text, string language)
        {
            return Analyze(text, new string[] { language });
        }

        public Issue[] Analyze(string text, int lineNumber, string language)
        {
            return Analyze(text, new string[] { language }, lineNumber);
        }

        /// <summary>
        ///     Analyzes given line of code
        /// </summary>
        /// <param name="text"> Source code </param>
        /// <param name="languages"> List of languages </param>
        /// <returns> Array of matches </returns>
        public Issue[] Analyze(string text, string[] languages, int lineNumber = -1)
        {
            // Get rules for the given content type
            IEnumerable<Rule> rules = GetRulesForLanguages(languages);
            List<Issue> resultsList = new List<Issue>();
            TextContainer textContainer = new TextContainer(text, (languages.Length > 0) ? languages[0] : string.Empty);
            TextContainer line = (lineNumber > 0) ? new TextContainer(textContainer.GetLineContent(lineNumber), (languages.Length > 0) ? languages[0] : string.Empty) : textContainer;

            // Go through each rule
            foreach (Rule rule in rules)
            {
                List<Issue> matchList = new List<Issue>();

                // Skip rules that don't apply based on settings
                if (rule.Disabled || !SeverityLevel.HasFlag(rule.Severity))
                    continue;

                // Go through each matching pattern of the rule
                foreach (SearchPattern pattern in rule.Patterns ?? Array.Empty<SearchPattern>())
                {
                    // Get all matches for the pattern
                    List<Boundary> matches = line.MatchPattern(pattern);

                    if (matches.Count > 0)
                    {
                        foreach (Boundary match in matches)
                        {
                            bool passedConditions = true;
                            var translatedBoundary = match;
                            if (lineNumber >= 0)
                            {
                                translatedBoundary = new Boundary()
                                {
                                    Length = match.Length,
                                    Index = textContainer.GetBoundaryFromLine(lineNumber).Index + match.Index
                                };
                            }

                            if (!textContainer.ScopeMatch(pattern, translatedBoundary))
                            {
                                passedConditions = false;
                            }
                            else
                            {
                                foreach (SearchCondition condition in rule.Conditions.Where(x => x is SearchCondition))
                                {
                                    if (condition.Pattern is { })
                                    {
                                        bool res = textContainer.MatchPattern(condition.Pattern, translatedBoundary, condition);
                                        passedConditions = condition.NegateFinding ? !res : res;
                                    }
                                }
                            }

                            if (passedConditions)
                            {
                                Issue issue = new Issue(Boundary: match, StartLocation: line.GetLocation(match.Index), EndLocation: line.GetLocation(match.Index + match.Length), Rule: rule);

                                matchList.Add(issue);
                            }
                        }
                    }
                }

                // We got matching rule and suppression are enabled, let's see if we have a supression on the line
                if (EnableSuppressions && matchList.Count > 0)
                {
                    Suppression supp;
                    foreach (Issue result in matchList)
                    {
                        supp = new Suppression(textContainer, (lineNumber > 0) ? lineNumber : result.StartLocation.Line);
                        // If rule is NOT being suppressed then report it
                        var supissue = supp.GetSuppressedIssue(result.Rule.Id);
                        if (supissue is null)
                        {
                            resultsList.Add(result);
                        }
                        // Otherwise add the suppression info instead
                        else
                        {
                            result.IsSuppressionInfo = true;

                            if (!resultsList.Any(x => x.Rule.Id == result.Rule.Id && x.Boundary.Index == result.Boundary.Index))
                                resultsList.Add(result);
                        }
                    }
                }
                // Otherwise put matchlist to resultlist
                else
                {
                    resultsList.AddRange(matchList);
                }
            }

            // Deal with overrides
            List<Issue> removes = new List<Issue>();
            foreach (Issue m in resultsList)
            {
                if (m.Rule.Overrides != null && m.Rule.Overrides.Length > 0)
                {
                    foreach (string ovrd in m.Rule.Overrides)
                    {
                        // Find all overriden rules and mark them for removal from issues list
                        foreach (Issue om in resultsList.FindAll(x => x.Rule.Id == ovrd))
                        {
                            if (om.Boundary.Index >= m.Boundary.Index &&
                                om.Boundary.Index <= m.Boundary.Index + m.Boundary.Length)
                                removes.Add(om);
                        }
                    }
                }
            }

            // Remove overriden rules
            resultsList.RemoveAll(x => removes.Contains(x));

            return resultsList.ToArray();
        }

        /// <summary>
        ///     Cache for rules filtered by content type
        /// </summary>
        private Dictionary<string, IEnumerable<Rule>> _rulesCache;

        private RuleSet _ruleset;

        /// <summary>
        ///     Filters the rules for those matching the content type. Resolves all the overrides
        /// </summary>
        /// <param name="languages"> Languages to filter rules for </param>
        /// <returns> List of rules </returns>
        private IEnumerable<Rule> GetRulesForLanguages(string[] languages)
        {
            string langid = string.Empty;

            if (EnableCache)
            {
                Array.Sort(languages);
                // Make language id for cache purposes
                langid = string.Join(":", languages);
                // Do we have the ruleset alrady in cache? If so return it
                if (_rulesCache.ContainsKey(langid))
                    return _rulesCache[langid];
            }

            IEnumerable<Rule> filteredRules = _ruleset.ByLanguages(languages);

            // Add the list to the cache so we save time on the next call
            if (EnableCache && filteredRules.Count() > 0)
            {
                _rulesCache.Add(langid, filteredRules);
            }

            return filteredRules;
        }
    }
}