// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

namespace Its.Validation
{
    /// <summary>
    /// Represents the method that will handle a <see cref="ValidationScope.RuleEvaluated" /> event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The <see cref="Its.Validation.RuleEvaluatedEventArgs"/> instance containing the event data.</param>
    public delegate void RuleEvaluatedEventHandler(object sender, RuleEvaluatedEventArgs args);
}