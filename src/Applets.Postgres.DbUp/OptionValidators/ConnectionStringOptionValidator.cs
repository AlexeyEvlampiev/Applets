using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using Npgsql;

namespace Applets.Postgres.DbUp.OptionValidators
{
    class ConnectionStringOptionValidator : IOptionValidator
    {
        public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
        {
            if (false == option.HasValue())
                return ValidationResult.Success;
            try
            {
                using var connection = new NpgsqlConnection(option.Value());
                connection.Open();
                return ValidationResult.Success;
            }
            catch (NpgsqlException e)
            {
                return new ValidationResult(e.Message);
            }
            catch (Exception e)
            {
                return new ValidationResult(e.Message);
            }

        }
    }
}
