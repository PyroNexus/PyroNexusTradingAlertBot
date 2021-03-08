using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PyroNexusTradingAlertBot.Storage.Model;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot.Storage.RavenDB
{
    public class RavenDBServiceOptions
    {
        public string[] ServerUrls;
    }

    class RavenDBService : IRavenDBService
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;

        private bool _disposed = false;

        public RavenDBService(IOptions<RavenDBServiceOptions> options, ILogger<RavenDBService> logger)
        {
            _logger = logger;

            _store = new DocumentStore
            {
                Urls = options.Value.ServerUrls
            };

            _store.Initialize();
        }

        private IDocumentStore Store { get { return _store; } }

        public void Dispose()
        {
            if (!_disposed && Store != null && !Store.WasDisposed)
            {
                Store.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        void IDBService<IDocumentStore>.EnsureDatabaseExists(string databaseName, bool createDatabaseIfNotExists)
        {
            databaseName = databaseName ?? Store.Database;

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be null or whitespace.", nameof(databaseName));

            try
            {
                Store.Maintenance.ForDatabase(databaseName).Send(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                if (createDatabaseIfNotExists == false)
                    throw;

                try
                {
                    Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(databaseName)));
                }
                catch (ConcurrencyException ex)
                {
                    throw new ConcurrencyException("The database was already created before calling CreateDatabaseOperation.", ex);
                }

            }
        }

        void IDBService<IDocumentStore>.Insert<M>(M objects, string databaseName) where M : class
        {
            IRavenDBService ravenDBService = this;
            ravenDBService.IsValidForInsert<M>();
            ravenDBService.EnsureDatabaseExists(databaseName);

            switch (typeof(M))
            {
                case Type symbol when symbol == typeof(Symbols):
                    InsertSymbols(objects as Symbols, databaseName);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }

        private void InsertSymbols(Symbols symbols, string databaseName)
        {
            using var session = Store.OpenSession(databaseName);

            var existing = session.Load<Symbols>("Symbols");
            if (existing == null)
            {
                session.Store(symbols, "Symbols");
            }
            else if(symbols.AllSymbols.Except(existing.AllSymbols).Any())
            {
                existing.AllSymbols = symbols.AllSymbols;

                session.Store(existing, "Symbols");
            }

            session.SaveChanges();

        }


    }
}
