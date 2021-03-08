using PyroNexusTradingAlertBot.Storage.RavenDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot.Storage
{
    interface IDBService<T> : IDisposable where T : class
    {
        //internal T Instance();
        internal void Insert<M>(M objects, string databaseName) where M : class;

        internal void IsValidForInsert<M>()
        {
            if (typeof(M).Namespace != "PyroNexusTradingAlertBot.Storage.Model")
            {
                throw new Exception("Object not supported for insert");
            }
        }

        internal void EnsureDatabaseExists(string database = null, bool createDatabaseIfNotExists = true);
    }
}
