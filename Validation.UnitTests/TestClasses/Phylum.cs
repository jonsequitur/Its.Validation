// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Its.Validation.UnitTests.TestClasses;

namespace Validation.Tests.TestClasses
{
    public class Phylum
    {
        public Kingdom Kingdom { get; set; }

        public string Name { get; set; }

        public IList<Class> Classes { get; } = new List<Class>();

        public override string ToString() => Name;
    }
}