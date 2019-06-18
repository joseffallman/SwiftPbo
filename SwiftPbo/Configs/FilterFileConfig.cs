/*
 *  This file is part of SwiftPbo.
 *  
 *  Copyright 2019 by Josef Fällman
 *  Licensed under Lesser GNU General Public License 3.0
 *  
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using SwiftPbo;


namespace SwiftPbo.Config
{
    public class FilterFileConfig
    {
        private List<string> _excludedExtensions = new List<string>();
        private List<string> _excludedSubstringInPath = new List<string>();

        public FilterFileConfig() { }

        public void DefaultFileConfig()
        {
            // All file extension that should be excluded from pbo
            // These are added with the dot
            _excludedExtensions.AddRange( new string[]
            {
                ".sqx",
                ".tproj",
            });

            // All file or folder names that should be excluded from pbo
            _excludedSubstringInPath.AddRange( new string[]
            {
                "CPack.Config",
                ".git"
            });

            // If hidden file/folders should be excluded from pbo
            ExcludeAllHidden = true;
        }

        public string[] ExcludedExtensions {
            get { return _excludedExtensions.ToArray(); }
            set { _excludedExtensions = value.ToList(); }
        }
        public string[] ExcludedSubstringInPath {
            get { return _excludedSubstringInPath.ToArray(); }
            set { _excludedSubstringInPath = value.ToList(); }
        }
        public Boolean ExcludeAllHidden {
            get; set;
        }


        /// <summary>
        /// Check if ext is in the list of excluded/forbidden extensions
        /// </summary>
        /// <param name="ext">File extension.</param>
        /// <returns>Boolean</returns>
        public Boolean forbiddenExtension(string ext)
        {
            foreach (string excludedExt in _excludedExtensions)
            {
                if (excludedExt.ToLower() == ext.ToLower())
                {
                    return true;
                }
            } 
            return false;
        }

        /// <summary>
        /// Check if File path containse any forbiden names,
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>Boolean</returns>
        public Boolean forbiddenSubstringInPath(string path)
        {
            path = path.ToLower();
            foreach (string excludedPart in _excludedSubstringInPath)
            {
                if (path.Contains(excludedPart.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}