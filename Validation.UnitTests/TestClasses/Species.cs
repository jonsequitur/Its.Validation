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
        public Species()
        {
        }

        public Species(string name)
        {
            Name = name;
        }

        public Genus Genus { get; set; }

        public string Name { get; set; }

        public List<Individual> Individuals { get; set; } = new List<Individual>();

        public bool IsExtinct { get; set; }

        public DateTime? ExtinctionDate { get; set; }

        public int NumberOfLegs { get; set; }

        public string SpeciesName => Name;

        public Species AddIndividual(Individual individual)
        {
            Individuals.Add(individual);
            individual.Species = this;
            return this;
        }

        public override string ToString() => Name;
    }
}