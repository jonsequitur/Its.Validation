// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Linq;

namespace Its.Validation
{
    internal class MessageTemplate : IValidationMessageGenerator
    {
        private readonly string value;

        public MessageTemplate(string value = null)
        {
            this.value = value ?? string.Empty;
        }

        public override string ToString()
        {
            return GetType().Name + ": " + value;
        }

        public virtual string GetMessage(RuleEvaluation evaluation)
        {
            return value;
        }
    }
}