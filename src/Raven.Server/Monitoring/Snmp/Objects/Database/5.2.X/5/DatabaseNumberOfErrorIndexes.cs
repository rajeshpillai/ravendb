// -----------------------------------------------------------------------
//  <copyright file="DatabaseOpenedCount.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Lextm.SharpSnmpLib;
using Raven.Client.Documents.Indexes;
using Raven.Server.Documents;

namespace Raven.Server.Monitoring.Snmp.Objects.Database
{
    public class DatabaseNumberOfErrorIndexes : DatabaseScalarObjectBase<Integer32>
    {
        public DatabaseNumberOfErrorIndexes(string databaseName, DatabasesLandlord landlord, int index)
            : base(databaseName, landlord, "5.2.{0}.5.6", index)
        {
        }

        protected override Integer32 GetData(DocumentDatabase database)
        {
            var count = database
                .IndexStore
                .GetIndexes()
                .Count(x => x.State == IndexState.Error);

            return new Integer32(count);
        }
    }
}