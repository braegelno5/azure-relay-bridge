﻿// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.HybridConnectionManager.Powershell
{
    using System.Globalization;
    using System.Management.Automation;
    using Microsoft.HybridConnectionManager.Configuration;

    ////using Microsoft.HybridConnectionManager.Commands.Properties;
    [Cmdlet(VerbsCommon.Add, Constants.HybridConnectionNounName,
        DefaultParameterSetName = Constants.HybridConnectionManagerDefaultParameterSetName)]
    public class AddHybridConnection : HybridConnectionBaseCmdlet
    {
        #region Parameters

        [Parameter(Mandatory = true,
            ParameterSetName = Constants.HybridConnectionManagerDefaultParameterSetName)]
        public string ConnectionString { get; set; }

        #endregion Parameters

        #region Cmdlet Overrides

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            //var hybridConnectionElement = this.GetHybridConnectionElement(this.ConnectionString);
            //if (hybridConnectionElement != null)
            //{
            //    var exception = new PSArgumentException(
            //        string.Format(CultureInfo.CurrentCulture, Strings.ConfigEntryAlreadyExists, this.ConnectionString));
            //    this.ThrowTerminatingError(
            //        new ErrorRecord(
            //            exception,
            //            string.Empty,
            //            ErrorCategory.InvalidArgument,
            //            null));
            //    throw exception;
            //}
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            //var hybridConnectionElement = new ConnectionTarget(this.ConnectionString, "", 0);
            //this.HybridConnectionsSection.Targets.Add(hybridConnectionElement);
            //this.ConfigurationChanged = true;
        }

        #endregion Overrides
    }
}