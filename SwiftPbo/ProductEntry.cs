/*
 *  This file is part of SwiftPbo.
 *  
 *  Copyright 2015-2016, 2018 by headswe
 *  Copyright 2018 by dedmen
 *  
 *  Licensed under GNU Lesser General Public License 3.0
 *  
 */
using System;
using System.Collections.Generic;

namespace SwiftPbo
{
    [Serializable]
    public class ProductEntry
    {
        private string _name;
        private string _prefix;
        private string _productVersion;
        private List<string> _addtional = new List<string>();

        public ProductEntry()
        {
            _name = _prefix = _productVersion = "";
            Addtional = new List<string>();
        }
        public ProductEntry(string name, string prefix, string productVersion, List<string> addList = null)
        {
            Name = name;
            Prefix = prefix;
            ProductVersion = productVersion;
            if (addList != null)
                Addtional = addList;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        public string ProductVersion
        {
            get { return _productVersion; }
            set { _productVersion = value; }
        }

        public List<string> Addtional
        {
            get { return _addtional; }
            set { _addtional = value; }
        }
    }
}