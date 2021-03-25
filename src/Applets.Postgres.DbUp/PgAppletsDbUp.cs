using System;
using System.Diagnostics;
using Applets.Postgres.DbUp.Scripts;
using DbUp;
using DbUp.Engine;
using DbUp.Helpers;
using Npgsql;

namespace Applets.Postgres.DbUp
{
    class PgAppletsDbUp
    {
        private string _connectionString;
        private bool _dropAndRecreate;
        private readonly UpgradeEngine _dropAllUpgradeEngine;

        [DebuggerStepThrough]
        private PgAppletsDbUp(string connectionString, bool dropAndRecreate)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Timeout = Convert.ToInt32( TimeSpan.FromSeconds(60).TotalSeconds)
            };
            this._connectionString = connectionStringBuilder.ConnectionString;
            this._dropAndRecreate = dropAndRecreate;
            _dropAllUpgradeEngine =
                DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithScript("Drop all objects", MasterScripts.DropAllObjects)
                    .WithExecutionTimeout(TimeSpan.FromMinutes(10))
                    .JournalTo(new NullJournal())
                    .WithTransactionPerScript()
                    .LogToConsole()
                    .Build();
        }

        [DebuggerStepThrough]
        public static void Run(string connectionString, bool dropAndRecreate)
        {
            var dbUp = new PgAppletsDbUp(connectionString, dropAndRecreate);
            dbUp.Deploy();
        }

        private void InvokeUpgradeEngine(UpgradeEngine engine, string logMessage)
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            var result = engine.PerformUpgrade();
            var stageTitle = $"{logMessage}. Database: {builder.Host}.{builder.Database}, user: {builder.Username}";
            Console.WriteLine(stageTitle);
            if (!result.Successful)
                throw result.Error;
            Console.WriteLine($@"{logMessage} -> OK");
        }

        private void Deploy()
        {
            if (_dropAndRecreate)
            {
                InvokeUpgradeEngine(_dropAllUpgradeEngine, "Dropping all database objects");
            }
        }
    }
}
