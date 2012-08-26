// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Its.Validation.UnitTests.TestClasses
{
    [DebuggerStepThrough]
    public class Species : IOrganism
    {
        private List<Individual> individuals = new List<Individual>();

        public Species()
        {
            Name = "";
        }

        public Species(string name)
        {
            Name = name;
        }

        public virtual Genus Genus { get; set; }

        public virtual string Name { get; set; }

        public virtual List<Individual> Individuals
        {
            get
            {
                return individuals;
            }
            set
            {
                individuals = value;
            }
        }

        public virtual bool IsExtinct { get; set; }

        public virtual DateTime? ExtinctionDate { get; set; }

        public virtual int NumberOfLegs { get; set; }

        public virtual string SpeciesName
        {
            get
            {
                return Name;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public Species AddIndividual(Individual individual)
        {
            Individuals.Add(individual);
            individual.Species = this;
            return this;
        }
    }
}