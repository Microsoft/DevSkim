﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DevSkim.CLI
{
    class ErrorMessage
    {
        public string File { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }
        public string RuleID { get; set; }
        public bool Warning { get; set; }
    }
}
