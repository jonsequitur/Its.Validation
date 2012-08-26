// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Its.Validation.UnitTests.TestClasses
{
    [DebuggerStepThrough]
    public class Individual
    {
        private List<Individual> children = new List<Individual>();

        public string Name { get; set; }

        public Species Species { get; set; }

        public Individual Parent { get; set; }

        public List<Individual> Children
        {
            get
            {
                return children;
            }
            set
            {
                children = value;
            }
        }

        public override string ToString()
        {
            return GetType() + " (\"" + Name + "\")";
        }
    }
}