// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Linq;
using Validation.Tests.TestClasses;

namespace Its.Validation.UnitTests.TestClasses
{
    public class Class
    {
        public virtual Phylum Phylum { get; set; }
        public virtual string Name { get; set; }
    }
}