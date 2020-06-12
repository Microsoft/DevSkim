﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DevSkim.VSExtension
{
    internal class SuppressSuggestedAction : ISuggestedAction
    {
        public SuppressSuggestedAction(DevSkimError error, int days = -1, bool suppressAll = false)
        {
            _error = error;
            _rule = error.Rule;

            _span = _error.ErrorTrackingSpan;
            _snapshot = _error.Snapshot;
            _code = _error.LineTrackingSpan.GetText(_snapshot);

            if (_rule != null)
            {
                if (days > 0)
                {
                    _display = string.Format(Resources.Messages.SuppressIssue, _rule.Id, days);
                    _suppDate = DateTime.Now.AddDays(days);
                }
                else
                    _display = string.Format(Resources.Messages.SuppressIssuePermanently, _rule.Id);
            }
            else
            {
                if (days > 0)
                {
                    _display = string.Format(Resources.Messages.SupressAllIssues, days);
                    _suppDate = DateTime.Now.AddDays(days);
                }
                else
                    _display = string.Format(Resources.Messages.SuppressAllIssuesPermanently);
            }
        }

        public SuppressSuggestedAction(ITrackingSpan span, Rule rule) : this(span, rule, -1)
        {
        }

        public SuppressSuggestedAction(ITrackingSpan span, Rule rule, int days)
        {
            _rule = rule;
            _span = span;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            _code = span.GetText(_snapshot);

            if (_rule != null)
            {
                if (days > 0)
                {
                    _display = string.Format(Resources.Messages.SuppressIssue, rule.Id, days);
                    _suppDate = DateTime.Now.AddDays(days);
                }
                else
                    _display = string.Format(Resources.Messages.SuppressIssuePermanently, rule.Id);
            }
            else
            {
                if (days > 0)
                {
                    _display = string.Format(Resources.Messages.SupressAllIssues, days);
                    _suppDate = DateTime.Now.AddDays(days);
                }
                else
                    _display = string.Format(Resources.Messages.SuppressAllIssuesPermanently);
            }
        }

        public string DisplayText
        {
            get
            {
                return _display;
            }
        }

        public bool HasActionSets
        {
            get
            {
                return false;
            }
        }

        public bool HasPreview
        {
            get
            {
                return false;
            }
        }

        public string IconAutomationText
        {
            get
            {
                return null;
            }
        }

        ImageMoniker ISuggestedAction.IconMoniker
        {
            get
            {
                return default(ImageMoniker);
            }
        }

        public string InputGestureText
        {
            get
            {
                return null;
            }
        }

        public void Dispose()
        {
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            string fixedCode = string.Empty;
            SuppressionEx supp = new SuppressionEx(_error, ContentType.GetLanguages(_snapshot.ContentType.TypeName)[0]);
            if (_rule == null)
            {
                fixedCode = supp.SuppressAll(_suppDate);
            }
            else
            {
                fixedCode = supp.SuppressIssue(_rule.Id, _suppDate);
            }

            _error.Snapshot.TextBuffer.Replace(_error.LineAndSuppressionCommentTrackingSpan.GetSpan(_error.Snapshot), fixedCode);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private readonly string _code;
        private readonly string _display = string.Empty;
        private readonly DevSkimError _error;
        private readonly Rule _rule;
        private readonly ITextSnapshot _snapshot;
        private readonly ITrackingSpan _span;
        private readonly DateTime _suppDate = DateTime.MaxValue;
    }
}