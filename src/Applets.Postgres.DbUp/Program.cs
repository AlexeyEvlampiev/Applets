using System;
using Applets.Postgres.DbUp.OptionValidators;
using McMaster.Extensions.CommandLineUtils;

namespace Applets.Postgres.DbUp
{
    class Program : CommandLineApplication
    {
        static int Main(string[] args)
        {
            var program = new Program();
            return program.Execute(args);
        }


        public Program()
        {
            Name = "pgApplets";
            Description = "Applets postgres metastore cli";
            this.HelpOption(true);
            this.Command("dbup", ConfigureDbUp);
            OnExecute(() =>
            {
                Out.WriteLine("Specify a sub-command");
                ShowHelp();
                return 1;
            });

            
        }

        

        private void ConfigureDbUp(CommandLineApplication cmd)
        {
            cmd.Description = "Deploys Applets postgres metastore database.";
            var connectionString = CreateConnectionCommandOption(cmd);
            var dropAndRecreate = cmd.Option(
                "-rc|--drop-recreate",
                "Applet postgres metastore connection string",
                CommandOptionType.NoValue);
            cmd.OnExecute(() =>
            {
                if (dropAndRecreate.HasValue() &&
                    false == Prompt.GetYesNo(
                        "Are you sure you want to drop all database object and recreate the metastore from scratch?",
                        false, 
                        ConsoleColor.Yellow))
                {
                    return 1;
                }

                try
                {
                    PgAppletsDbUp.Run(connectionString.Value(), dropAndRecreate.HasValue());
                    return 0;
                }
                catch (Exception e)
                {
                    Error.WriteLine(e.Message);
                    return 1;
                }
     
            });
        }

        private CommandOption CreateConnectionCommandOption(CommandLineApplication cmd)
        {
            return cmd.Option(
                "-cs|--connection-string",
                "Applet postgres metastore connection string",
                CommandOptionType.SingleValue,
                config =>
                {
                    config.IsRequired();
                    config.Validators.Add(new ConnectionStringOptionValidator());
                });
        }
    }
}
