// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Collections.Generic;

namespace Its.Validation.UnitTests.TestClasses
{
    public class Family
    {
        public Family()
        {
        }

        public Family(string name)
        {
            Name = name;
        }

        public List<Genus> Genuses { get; set; }

        public virtual Order Order { get; set; }

        public virtual string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}