// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export interface DevSkimSettings {
    enableManualReviewRules: boolean;
    enableInformationalSeverityRules: boolean;
    enableDefenseInDepthSeverityRules: boolean;
    enableBestPracticeRules: boolean;
    enableLowSeverityRules: boolean;
    suppressionDurationInDays: number;
    manualReviewerName: string;
    ignoreFiles: string[];
    ignoreRulesList: string[];
    validateRulesFiles: boolean;
    guidanceBaseURL: string;
    removeFindingsOnClose: boolean;
    enableWarningInfo : boolean;
}

export class DevSkimSettingsObject implements DevSkimSettings {
    enableBestPracticeRules: boolean;
    enableDefenseInDepthSeverityRules: boolean;
    enableInformationalSeverityRules: boolean;
    enableLowSeverityRules: boolean;
    enableManualReviewRules: boolean;
    guidanceBaseURL: string;
    ignoreFiles: string[];
    ignoreRulesList: string[];
    manualReviewerName: string;
    removeFindingsOnClose: boolean;
    suppressionDurationInDays: number;
    validateRulesFiles: boolean;
    enableWarningInfo : boolean;
};