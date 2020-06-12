﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    ///     Store for DevSkim settings. Takes care of persistance between Visual Studio restarts
    /// </summary>
    [SettingsEntity(Name = "Microsoft.DevSkim")]
    [Export(typeof(Settings))]
    public class Settings
    {
        [ImportingConstructor]
        public Settings(SVsServiceProvider vsServiceProvider)
        {
            var shellSettingsManager = new ShellSettingsManager(vsServiceProvider);
            writableSettingsStore = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        [SettingsEntity(Name = "CustomRulesPath")]
        public string CustomRulesPath { get; set; } = "";

        [SettingsEntity(Name = "EnableBestPracticeRules")]
        public bool EnableBestPracticeRules { get; set; } = true;

        [SettingsEntity(Name = "EnableImportantRules")]
        public bool EnableImportantRules { get; set; } = true;

        [SettingsEntity(Name = "EnableManualReviewRules")]
        public bool EnableManualReviewRules { get; set; } = false;

        [SettingsEntity(Name = "EnableModerateRules")]
        public bool EnableModerateRules { get; set; } = true;

        [SettingsEntity(Name = "SuppressDays")]
        public int SuppressDays { get; set; } = 30;

        [SettingsEntity(Name = "Use Block comment style suppression messages.")]
        public bool UseBlockSuppression { get; set; } = false;

        [SettingsEntity(Name = "UseCustomRules")]
        public bool UseCustomRules { get; set; } = false;

        [SettingsEntity(Name = "UseDefaultRules")]
        public bool UseDefaultRules { get; set; } = true;

        [SettingsEntity(Name = "Insert DevSkim suppression messages on the line before the issue.")]
        public bool UsePreviousLineSuppression { get; set; } = false;

        /// <summary>
        ///     Get instnace of settings
        /// </summary>
        /// <returns> </returns>
        public static Settings GetSettings()
        {
            if (_instance == null)
            {
                var componentModel = (IComponentModel)(Package.GetGlobalService(typeof(SComponentModel)));
                _instance = componentModel.DefaultExportProvider.GetExportedValue<Settings>();
            }

            _instance.Load();

            return _instance;
        }

        /// <summary>
        ///     Load settings from store. Generally there is no need to call this method. Settings are
        ///     autoloaded in GetSettings method
        /// </summary>
        public void Load()
        {
            try
            {
                Type type = typeof(Settings);

                // Get name of the persistant store from the class attribute
                string collectionName = (type.GetCustomAttribute(typeof(SettingsEntity)) as SettingsEntity).Name;

                // Go through properties. Those marked with SettingsAttribute will be loaded
                foreach (PropertyInfo property in type.GetProperties())
                {
                    if (Attribute.IsDefined(property, typeof(SettingsEntity)))
                    {
                        SettingsEntity entity = (SettingsEntity)Attribute.GetCustomAttribute(property, typeof(SettingsEntity));

                        string val = writableSettingsStore.GetString(collectionName, entity.Name, property.GetValue(this).ToString());
                        object o = Convert.ChangeType(val, property.PropertyType);
                        property.SetValue(this, o);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
            }
        }

        /// <summary>
        ///     Save settings to persistant store
        /// </summary>
        public void Save()
        {
            try
            {
                Type type = typeof(Settings);

                // Get name of the persistant store from the class attribute
                string collectionName = (type.GetCustomAttribute(typeof(SettingsEntity)) as SettingsEntity).Name;

                // Check is the store already exists, otherwise we will create it
                if (!writableSettingsStore.CollectionExists(collectionName))
                {
                    writableSettingsStore.CreateCollection(collectionName);
                }

                // Go through properties. Those marked with SettingsAttribute will be saved
                foreach (PropertyInfo property in type.GetProperties())
                {
                    if (Attribute.IsDefined(property, typeof(SettingsEntity)))
                    {
                        SettingsEntity entity = (SettingsEntity)Attribute.GetCustomAttribute(property, typeof(SettingsEntity));

                        writableSettingsStore.SetString(collectionName, entity.Name, property.GetValue(this).ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
            }
        }

        private static Settings _instance;
        private readonly WritableSettingsStore writableSettingsStore;
    }

    public class SettingsEntity : Attribute
    {
        public string Name { get; set; }
    }
}