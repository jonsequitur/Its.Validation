// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Validation.Tests.TestClasses;

namespace Its.Validation.UnitTests.TestClasses
{
    public class Kingdom
    {
        private List<Phylum> phylums = new List<Phylum>();

        public Kingdom()
        {
        }

        public Kingdom(string name)
        {
            Name = name;
        }

        public virtual string Name { get; set; }

        public virtual List<Phylum> Phylums
        {
            get
            {
                return phylums;
            }
        }
    }
}