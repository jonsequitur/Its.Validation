// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Linq;

namespace Its.Validation.UnitTests.TestClasses
{
    public class Order
    {
        public Order()
        {
        }

        public Order(string name)
        {
            Name = name;
        }

        public virtual Class Class { get; set; }

        public virtual string Name { get; set; }
    }
}