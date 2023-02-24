﻿using System;
using System.Data.Common;
using System.Threading.Tasks;
using static CSL.DependencyInjection;

namespace CSL.SQL
{
    public class PostgreSQL : SQLDB
    {
        public static bool TrustAllServerCertificates = false;
        public static async Task<PostgreSQL> Connect(string Server, string Database, string username, string password, string? Schema = null, SslMode SslMode = SslMode.Prefer)
        {
            INpgsqlConnectionStringBuilder csb = CreateINpgsqlConnectionStringBuilder();
            if (Server.Contains(":"))
            {
                string[] serversplit = Server.Split(':');
                string dumbtest = serversplit[0].ToLower();
                if (dumbtest is "http" or "https")
                {
                    throw new ArgumentException("Postgres connections are not http connections.");
                }
                csb.Host = serversplit[0];
                if (int.TryParse(serversplit[1], out int port))
                {
                    csb.Port = port;
                }
            }
            else
            {
                csb.Host = Server;
            }
            csb.Database = Database;
            csb.Username = username;
            csb.Password = password;
            csb.SslMode = SslMode;
            csb.Pooling = false;
            csb.TrustServerCertificate = TrustAllServerCertificates;

            PostgreSQL toReturn = new PostgreSQL(CreateNpgsqlConnection(csb.ConnectionString));
            await toReturn.SetSchema(Schema);
            return toReturn;
        }
        public PostgreSQL(DbConnection connection) : base(connection)
        {
            currentTransaction = null;
            InternalConnection.Open();
        }
        public Task SetSchema(string? Schema)
        {
            if (Schema != null)
            {
                return ExecuteNonQuery("CREATE SCHEMA IF NOT EXISTS \"" + Common.NameParser(Schema) + "\"; SET search_path to \"" + Common.NameParser(Schema) + "\";");
            }
            else
            {
                return ExecuteNonQuery("SET search_path to \"$user\", public;");
            }
        }
        #region Abstract Implementations
        public override object? ConvertToFriendlyParameter(object? parameter)
        {
            object? toReturn = parameter;
            Type? ParameterType = toReturn?.GetType();
            if (ParameterType == null) { return toReturn; }
            ParameterType = Nullable.GetUnderlyingType(ParameterType) ?? ParameterType;
            if (ParameterType.IsEnum)
            {
                ParameterType = Enum.GetUnderlyingType(ParameterType);
                toReturn = Convert.ChangeType(toReturn, ParameterType);
            }
            if (ParameterType == typeof(char))
            {
                char? val = (char?)toReturn;
                return val != null ? new string(val.Value, 1) : null;
            }
            if (ParameterType == typeof(ushort)) { return (short?)(ushort?)toReturn; }
            if (ParameterType == typeof(uint)) { return (int?)(uint?)toReturn; }
            if (ParameterType == typeof(ulong)) { return (long?)(ulong?)toReturn; }
            return toReturn;
        }
        #endregion
    }
}
