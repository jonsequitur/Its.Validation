// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Validation.Tests.TestClasses;

namespace Its.Validation.UnitTests.TestClasses
{
    public class Kingdom
    {
        public string Name { get; set; }

        public List<Phylum> Phylums { get; } = new List<Phylum>();

        public override string ToString() => Name;
    }
}