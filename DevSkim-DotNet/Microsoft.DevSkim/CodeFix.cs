﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Code fix class
    /// </summary>
    public class CodeFix
    {
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }
   
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(FixTypeConverter))]
        public FixType? FixType { get; set; }

        [JsonProperty(PropertyName = "pattern")]
        public SearchPattern? Pattern { get; set; }

        [JsonProperty(PropertyName = "replacement")]
        public string? Replacement { get; set; }
    }
}
