﻿//******************************************************************************************************
//  IntelliRupterSettings.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  01/21/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System.ComponentModel;
using System.Configuration;

namespace IntelliRupters
{
    /// <summary>
    /// Defines settings used by the modules to process IntelliRupter data.
    /// </summary>
    public class IntelliRupterSettings
    {
        #region [ Members ]

        // Fields
        private string m_waveWinSourcePath;
        private string m_waveWinArchivePath;

        private bool m_useComName;
        private string m_comNameCompany;
        private string m_comNameTimecode;
        private string m_comNameUser1;
        private string m_comNameUser2;

        private string m_soeDataFileDestinationPath;
        private string m_soeDataFileArchivePath;
        private double m_soeDataFileArchiveExpiration;

        private string m_soeSourceFileArchivePath;
        private double m_soeSourceFileArchiveExpiration;

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the path to the directory where WaveWin
        /// is watching for files to be converted to COMTRADE.
        /// </summary>
        [Setting]
        [DefaultValue(@"WaveWin\Source")]
        public string WaveWinSourcePath
        {
            get
            {
                return m_waveWinSourcePath;
            }
            set
            {
                m_waveWinSourcePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the path to the directory where WaveWin is
        /// placing files for archival after converting them to COMTRADE.
        /// </summary>
        [Setting]
        [DefaultValue(@"WaveWin\Archive")]
        public string WaveWinArchivePath
        {
            get
            {
                return m_waveWinArchivePath;
            }
            set
            {
                m_waveWinArchivePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the flag that determines whether to
        /// use the ComName standard for the output files.
        /// </summary>
        [Setting]
        [DefaultValue(true)]
        public bool UseComName
        {
            get
            {
                return m_useComName;
            }
            set
            {
                m_useComName = value;
            }
        }

        /// <summary>
        /// Gets or sets the company name used when renaming files to the ComName format.
        /// </summary>
        [Setting]
        [DefaultValue("GPA")]
        public string ComNameCompany
        {
            get
            {
                return m_comNameCompany;
            }
            set
            {
                m_comNameCompany = value;
            }
        }

        /// <summary>
        /// Gets or sets the timecode used when renaming files to the ComName format.
        /// </summary>
        [Setting]
        [DefaultValue("")]
        public string ComNameTimecode
        {
            get
            {
                return m_comNameTimecode;
            }
            set
            {
                m_comNameTimecode = value;
            }
        }

        /// <summary>
        /// Gets or sets the first user field used when renaming files to the ComName format.
        /// </summary>
        [Setting]
        [DefaultValue("")]
        public string ComNameUser1
        {
            get
            {
                return m_comNameUser1;
            }
            set
            {
                m_comNameUser1 = value;
            }
        }

        /// <summary>
        /// Gets or sets the second user field used when renaming files to the ComName format.
        /// </summary>
        [Setting]
        [DefaultValue("")]
        public string ComNameUser2
        {
            get
            {
                return m_comNameUser2;
            }
            set
            {
                m_comNameUser2 = value;
            }
        }

        /// <summary>
        /// Gets or sets the path to the directory used by SOE to process
        /// COMTRADE data files produced through conversion by WaveWin.
        /// </summary>
        [Setting]
        [DefaultValue(@"Watch")]
        public string SOEDataFileDestinationPath
        {
            get
            {
                return m_soeDataFileDestinationPath;
            }
            set
            {
                m_soeDataFileDestinationPath = value;
            }
        }

        /// <summary>
        /// Gets or sets the path to the directory used by SOE to archive
        /// COMTRADE data files produced through conversion by WaveWin.
        /// </summary>
        [Setting]
        [DefaultValue(@"Archive\COMTRADE")]
        public string SOEDataFileArchivePath
        {
            get
            {
                return m_soeDataFileArchivePath;
            }
            set
            {
                m_soeDataFileArchivePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of days that the archived COMTRADE
        /// data files should be kept before being automatically purged.
        /// </summary>
        [Setting]
        [DefaultValue(0.0D)]
        public double SOEDataFileArchiveExpiration
        {
            get
            {
                return m_soeDataFileArchiveExpiration;
            }
            set
            {
                m_soeDataFileArchiveExpiration = value;
            }
        }

        /// <summary>
        /// Gets or sets the path to the directory used by SOE to archive the source
        /// files produced by the IntelliRupters before the conversion to COMTRADE.
        /// </summary>
        [Setting]
        [DefaultValue(@"Archive\Source")]
        public string SOESourceFileArchivePath
        {
            get
            {
                return m_soeSourceFileArchivePath;
            }
            set
            {
                m_soeSourceFileArchivePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of days that the archived source
        /// files should be kept before being automatically purged.
        /// </summary>
        [Setting]
        [DefaultValue(0.0D)]
        public double SOESourceFileArchiveExpiration
        {
            get
            {
                return m_soeSourceFileArchiveExpiration;
            }
            set
            {
                m_soeSourceFileArchiveExpiration = value;
            }
        }

        #endregion
    }
}
