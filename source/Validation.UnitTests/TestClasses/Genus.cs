// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Collections.Generic;

namespace Its.Validation.UnitTests.TestClasses
{
    public class Genus
    {
        private List<Species> species = new List<Species>();

        public Genus()
        {
        }

        public Genus(string name)
        {
            Name = name;
        }

        public virtual IList<Species> Species
        {
            get
            {
                return species;
            }
            set
            {
                species = new List<Species>(value);
            }
        }

        public virtual Family Family { get; set; }

        public virtual string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}