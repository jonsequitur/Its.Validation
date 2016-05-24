// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Its.Validation.UnitTests.TestClasses
{
    [DebuggerStepThrough]
    public class Individual
    {
        public string Name { get; set; }

        public Species Species { get; set; }

        public Individual Parent { get; set; }

        public List<Individual> Children { get; set; } = new List<Individual>();

        public override string ToString() => Name;
    }
}