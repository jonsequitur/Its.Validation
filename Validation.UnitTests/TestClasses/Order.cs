// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Linq;

namespace Its.Validation.UnitTests.TestClasses
{
    public class Order
    {
        public Class Class { get; set; }

        public string Name { get; set; }

        public override string ToString() => Name;
    }
}