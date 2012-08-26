// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Its.Validation.UnitTests.TestClasses;

namespace Validation.Tests.TestClasses
{
    public class Phylum
    {
        private List<Class> classes = new List<Class>();

        public Phylum()
        {
        }

        public Phylum(string name)
        {
            Name = name;
        }

        public virtual Kingdom Kingdom { get; set; }

        public virtual string Name { get; set; }

        public virtual List<Class> Classes
        {
            get
            {
                return classes;
            }
        }
    }
}