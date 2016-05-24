// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Collections.Generic;

namespace Its.Validation.UnitTests.TestClasses
{
    public class Family
    {
        public List<Genus> Genuses { get; set; }

        public Order Order { get; set; }

        public string Name { get; set; }

        public override string ToString() => Name;
    }
}