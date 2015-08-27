﻿//*********************************************************************************************************************
// ExtensibleDisturbanceAnalysisEngine.cs
// Version 1.1 and subsequent releases
//
//  Copyright © 2013, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
// --------------------------------------------------------------------------------------------------------------------
//
// Version 1.0
//
// Copyright 2012 ELECTRIC POWER RESEARCH INSTITUTE, INC. All rights reserved.
//
// openXDA ("this software") is licensed under BSD 3-Clause license.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the 
// following conditions are met:
//
// •    Redistributions of source code must retain the above copyright  notice, this list of conditions and 
//      the following disclaimer.
//
// •    Redistributions in binary form must reproduce the above copyright notice, this list of conditions and 
//      the following disclaimer in the documentation and/or other materials provided with the distribution.
//
// •    Neither the name of the Electric Power Research Institute, Inc. (“EPRI”) nor the names of its contributors 
//      may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
// DISCLAIMED. IN NO EVENT SHALL EPRI BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
// OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
//
//
// This software incorporates work covered by the following copyright and permission notice: 
//
// •    TVA Code Library 4.0.4.3 - Tennessee Valley Authority, tvainfo@tva.gov
//      No copyright is claimed pursuant to 17 USC § 105. All Other Rights Reserved.
//
//      Licensed under TVA Custom License based on NASA Open Source Agreement (TVA Custom NOSA); 
//      you may not use TVA Code Library except in compliance with the TVA Custom NOSA. You may  
//      obtain a copy of the TVA Custom NOSA at http://tvacodelibrary.codeplex.com/license.
//
//      TVA Code Library is provided by the copyright holders and contributors "as is" and any express 
//      or implied warranties, including, but not limited to, the implied warranties of merchantability 
//      and fitness for a particular purpose are disclaimed.
//
//*********************************************************************************************************************
//
//  Code Modification History:
//  -------------------------------------------------------------------------------------------------------------------
//  05/16/2012 - J. Ritchie Carroll, Grid Protection Alliance
//       Generated original version of source code.
//  10/02/2014 - Stephen C. Wills, Grid Protection Alliance
//       Adapted from the openFLE project to use the new fault location logic.
//
//*********************************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using FaultData.Configuration;
using FaultData.Database;
using GSF.Annotations;
using GSF.Configuration;
using GSF.IO;
using log4net;
using openXDA.Configuration;
using FileShare = openXDA.Configuration.FileShare;

namespace openXDA
{
    /// <summary>
    /// Represents an engine that processes power quality data
    /// to determine the locations of faults along power lines.
    /// </summary>
    public class ExtensibleDisturbanceAnalysisEngine : IDisposable
    {
        #region [ Members ]

        // Constants

        /// <summary>
        /// Globally unique identifier required by the file processor to identify its cached list of processed files.
        /// </summary>
        private static readonly Guid FileProcessorID = new Guid("4E3D3A90-6E7E-4AB7-96F3-3A5899081D0D");

        // Fields
        private string m_dbConnectionString;
        private SystemSettings m_systemSettings;
        private FileProcessor m_fileProcessor;
        private MeterDataScheduler m_meterDataScheduler;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Starts the fault location engine.
        /// </summary>
        public void Start()
        {
            IEnumerable<string> validExtensions;

            // Get system settings from the database
            ReloadSystemSettings();

            // Get the list of file extensions to be processed by openXDA
            using (SystemInfoDataContext systemInfo = new SystemInfoDataContext(m_dbConnectionString))
            {
                validExtensions = systemInfo.DataReaders
                    .Select(reader => reader.FileExtension)
                    .Select(extension => string.Format("*.{0}", extension))
                    .ToList();
            }

            // Reload configuration at startup
            ReloadConfiguration();

            // Make sure watch directories exist
            foreach (string path in m_systemSettings.WatchDirectoryList)
                TryCreateDirectory(path);

            // Make sure results directory exists
            TryCreateDirectory(m_systemSettings.ResultsPath);

            // Create the scheduler used to schedule when to process meter data
            if ((object)m_meterDataScheduler == null)
                m_meterDataScheduler = new MeterDataScheduler(() => new MeterDataProcessor(LoadSystemSettings()));

            // Set the number of threads used for processing meter data
            m_meterDataScheduler.FilePattern = m_systemSettings.FilePattern;
            m_meterDataScheduler.MaxThreads = m_systemSettings.ProcessingThreadCount;

            // Setup new file processor to monitor the watch directories
            if ((object)m_fileProcessor == null)
            {
                m_fileProcessor = new FileProcessor(FileProcessorID);
                m_fileProcessor.InternalBufferSize = m_systemSettings.FileWatcherBufferSize;
                m_fileProcessor.Filter = string.Join(Path.PathSeparator.ToString(), validExtensions);
                m_fileProcessor.Processing += FileProcessor_Processing;
                m_fileProcessor.Error += FileProcessor_Error;
            }

            foreach (string path in m_systemSettings.WatchDirectoryList)
                m_fileProcessor.AddTrackedDirectory(path);
        }

        /// <summary>
        /// Reloads system configuration from configuration sources.
        /// </summary>
        public void ReloadConfiguration()
        {
            SystemInfoDataContext systemInfo;
            List<Type> types;
            string connectionString;

            IConfigurationLoader configurationLoader;

            // If system settings is null,
            // attempt to reload system settings
            if ((object)m_systemSettings == null)
                ReloadSystemSettings();

            // If system settings is still null, give up
            if ((object)m_systemSettings == null)
                return;

            using (DbAdapterContainer dbAdapterContainer = new DbAdapterContainer(m_systemSettings.DbConnectionString))
            {
                systemInfo = dbAdapterContainer.GetAdapter<SystemInfoDataContext>();

                types = systemInfo.ConfigurationLoaders
                    .OrderBy(configLoader => configLoader.LoadOrder)
                    .AsEnumerable()
                    .Select(configLoader => LoadType(configLoader.AssemblyName, configLoader.TypeName))
                    .Where(type => (object)type != null)
                    .Where(type => typeof(IConfigurationLoader).IsAssignableFrom(type))
                    .Where(type => (object)type.GetConstructor(Type.EmptyTypes) != null)
                    .ToList();

                connectionString = LoadSystemSettings(systemInfo);

                foreach (Type type in types)
                {
                    try
                    {
                        OnStatusMessage("[{0}] Loading configuration...", type.Name);

                        // Create an instance of the configuration loader
                        configurationLoader = (IConfigurationLoader)Activator.CreateInstance(type);

                        // Use the connection string parser to load system settings into the configuration loader
                        ConnectionStringParser.ParseConnectionString(connectionString, configurationLoader);

                        // Update configuration by calling the configuration loader's UpdateConfiguration method
                        configurationLoader.UpdateConfiguration(dbAdapterContainer);

                        OnStatusMessage("[{0}] Done loading configuration.", type.Name);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("[{0}] Unable to update configuration due to exception: {1}", type.Name, ex.Message);
                        OnProcessException(new InvalidOperationException(message, ex));
                    }
                }
            }
        }

        /// <summary>
        /// Reloads system settings from the database.
        /// </summary>
        public void ReloadSystemSettings()
        {
            ConfigurationFile configurationFile;
            CategorizedSettingsElementCollection category;

            // Reload the configuration file
            configurationFile = ConfigurationFile.Current;
            configurationFile.Reload();

            // Retrieve the connection string from the config file
            category = configurationFile.Settings["systemSettings"];
            category.Add("ConnectionString", "Data Source=localhost; Initial Catalog=openXDA; Integrated Security=SSPI", "Defines the connection to the openXDA database.");
            m_dbConnectionString = category["ConnectionString"].Value;
            
            // Load system settings from the database
            m_systemSettings = new SystemSettings(LoadSystemSettings());

            // Update the limit on the number of processing threads
            if ((object)m_meterDataScheduler != null)
            {
                m_meterDataScheduler.FilePattern = m_systemSettings.FilePattern;
                m_meterDataScheduler.MaxThreads = m_systemSettings.ProcessingThreadCount;
            }

            // Attempt to authenticate to configured file shares
            foreach (FileShare fileShare in m_systemSettings.FileShareList)
            {
                if (!fileShare.TryAuthenticate())
                    OnProcessException(fileShare.AuthenticationException);
            }

            // Update the FileProcessor with the latest system settings
            if ((object)m_fileProcessor != null)
            {
                m_fileProcessor.InternalBufferSize = m_systemSettings.FileWatcherBufferSize;

                foreach (string directory in m_fileProcessor.TrackedDirectories.ToList())
                {
                    if (!m_systemSettings.WatchDirectoryList.Contains(directory, StringComparer.OrdinalIgnoreCase))
                        m_fileProcessor.RemoveTrackedDirectory(directory);
                }

                foreach (string directory in m_systemSettings.WatchDirectoryList)
                    m_fileProcessor.AddTrackedDirectory(directory);
            }
        }

        /// <summary>
        /// Stops the fault location engine.
        /// </summary>
        public void Stop()
        {
            if ((object)m_fileProcessor != null)
                m_fileProcessor.ClearTrackedDirectories();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            // Stop file monitor timer
            if ((object)m_fileProcessor != null)
            {
                m_fileProcessor.Processing -= FileProcessor_Processing;
                m_fileProcessor.Error -= FileProcessor_Error;
                m_fileProcessor.Dispose();
                m_fileProcessor = null;
            }
        }

        // Called when the file processor has picked up a file in one of the watch
        // directories. This handler validates the file and processes it if able.
        private void FileProcessor_Processing(object sender, FileProcessorEventArgs fileProcessorEventArgs)
        {
            string filePath;

            filePath = fileProcessorEventArgs.FullPath;

            using (FileInfoDataContext fileInfo = new FileInfoDataContext(m_dbConnectionString))
            {
                // Determine whether the file has already been processed
                if (fileProcessorEventArgs.AlreadyProcessed)
                {
                    if (fileInfo.DataFiles.Any(dataFile => dataFile.FilePathHash == filePath.GetHashCode() && dataFile.FilePath == filePath && dataFile.FileGroup.ProcessingEndTime > DateTime.MinValue))
                    {
                        OnStatusMessage("Skipped file \"{0}\" because it has already been processed.", filePath);
                        return;
                    }
                }
            }

            m_meterDataScheduler.Schedule(filePath);
        }

        // Called when the file processor encounters an unexpected error.
        private void FileProcessor_Error(object sender, ErrorEventArgs args)
        {
            OnProcessException(args.GetException());
        }

        private string LoadSystemSettings()
        {
            using (SystemInfoDataContext systemInfo = new SystemInfoDataContext(m_dbConnectionString))
            {
                return LoadSystemSettings(systemInfo);
            }
        }

        private string LoadSystemSettings(SystemInfoDataContext systemInfo)
        {
            // Convert the Setting table to a dictionary
            Dictionary<string, string> settings = systemInfo.Settings
                .ToDictionary(setting => setting.Name, setting => setting.Value, StringComparer.OrdinalIgnoreCase);

            // Add the database connection string if there is not
            // already one explicitly specified in the Setting table
            if (!settings.ContainsKey("dbConnectionString"))
                settings.Add("dbConnectionString", m_dbConnectionString);

            // Convert dictionary to a connection string and return it
            return SystemSettings.ToConnectionString(settings);
        }

        private Type LoadType(string assemblyName, string typeName)
        {
            Assembly assembly;

            try
            {
                assembly = Assembly.LoadFrom(FilePath.GetAbsolutePath(assemblyName));

                if ((object)assembly != null)
                    return assembly.GetType(typeName);

                return null;
            }
            catch (Exception ex)
            {
                OnProcessException(ex);
                return null;
            }
        }

        // Attempts to create the directory at the given path.
        private void TryCreateDirectory(string path)
        {
            try
            {
                // Make sure results directory exists
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Failed to create directory \"{0}\" due to exception: {1}", FilePath.GetAbsolutePath(path), ex.Message), ex));
            }
        }

        // Displays status message to the console - proxy method for service implementation
        [StringFormatMethod("format")]
        private void OnStatusMessage(string format, params object[] args)
        {
            Log.Info(string.Format(format, args));
        }

        // Displays exception message to the console - proxy method for service implmentation
        private void OnProcessException(Exception ex)
        {
            Log.Error(ex.Message, ex);
        }

        #endregion

        #region [ Static ]

        // Static Fields
        private static readonly ConnectionStringParser<SettingAttribute, CategoryAttribute> ConnectionStringParser = new ConnectionStringParser<SettingAttribute, CategoryAttribute>();
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExtensibleDisturbanceAnalysisEngine));

        #endregion
    }
}
