using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public class SqlDbHelper
    {
        private SqlConnection sqlConnection;
        private SqlCommand sqlCommand;
        private string appConfigConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString;
            }
        }

        public string SqlCommandText
        {
            set
            {
                this.sqlCommand.CommandText = value;
            }
        }

        public SqlDbHelper()
        {
            if (string.IsNullOrWhiteSpace(appConfigConnectionString))
            {
                throw new ArgumentException("Key DbConnectionString is not defined in the app.config file");
            }

            this.sqlConnection = new SqlConnection(appConfigConnectionString);
            this.sqlCommand = new SqlCommand(string.Empty, this.sqlConnection);
        }

        public SqlDbHelper(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            this.sqlConnection = new SqlConnection(connectionString);
            this.sqlCommand = new SqlCommand(string.Empty, this.sqlConnection);
        }

        public SqlDbHelper(string sqlCommand, CommandType commandType = CommandType.Text)
        {
            if (string.IsNullOrWhiteSpace(sqlCommand))
            {
                throw new ArgumentException(nameof(sqlCommand));
            }

            this.sqlConnection = new SqlConnection(appConfigConnectionString);
            this.sqlCommand = new SqlCommand(sqlCommand, this.sqlConnection)
            {
                CommandType = commandType
            };
        }

        public void AddParameter(string parameterName, object parameterValue)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException(nameof(parameterName));
            }

            if (parameterValue == null)
            {
                throw new ArgumentException(nameof(parameterValue));
            }

            this.sqlCommand.Parameters.Add(new SqlParameter(parameterName, parameterValue));
        }

        public void AddParameter(SqlParameter sqlParameter)
        {
            if (sqlParameter == null || string.IsNullOrEmpty(sqlParameter.ParameterName) || sqlParameter.Value == null)
            {
                throw new ArgumentException($"Either {nameof(sqlParameter)}, {nameof(sqlParameter.ParameterName)} or {nameof(sqlParameter.Value)} is null or empty");
            }

            this.sqlCommand.Parameters.Add(sqlParameter);
        }

        public async Task<int> ExecuteNonQueryAsync(string sqlCommandText)
        {
            SetCommandText(sqlCommandText);

            return await this.ExecuteNonQueryAsync();
        }

        public async Task<T> ExecuteScalarAsync<T>(string sqlCommandText)
        {
            SetCommandText(sqlCommandText);

            return await ExecuteScalarAsync<T>();
        }

        public async Task<DataSet> ExecuteDataSetAsync(string sqlCommandText)
        {
            SetCommandText(sqlCommandText);

            return await this.ExecuteDataSetAsync();
        }

        public async Task<DataTable> ExecuteDataTableAsync(string sqlCommandText)
        {
            SetCommandText(sqlCommandText);

            return (await this.ExecuteDataSetAsync()).Tables[0];
        }

        public async Task<DataTable> ExecuteDataTableAsync()
        {
            if (string.IsNullOrWhiteSpace(this.sqlCommand.CommandText))
            {
                throw new ArgumentException($"Please specify {nameof(this.sqlCommand.CommandText)} first");
            }

            return (await this.ExecuteDataSetAsync()).Tables[0];
        }

        public async Task<int> ExecuteNonQueryAsync()
        {
            if (string.IsNullOrWhiteSpace(this.sqlCommand.CommandText))
            {
                throw new ArgumentException($"Please specify {nameof(this.sqlCommand.CommandText)} first");
            }

            this.sqlConnection.Open();
            int noOfRowsEffected = await this.sqlCommand.ExecuteNonQueryAsync();
            this.sqlConnection.Close();
            this.sqlCommand.Parameters.Clear();

            return noOfRowsEffected;
        }

        public async Task<T> ExecuteScalarAsync<T>()
        {
            if (string.IsNullOrWhiteSpace(this.sqlCommand.CommandText))
            {
                throw new ArgumentException($"Please specify {nameof(this.sqlCommand.CommandText)} first");
            }

            this.sqlConnection.Open();
            T result = (T)await this.sqlCommand.ExecuteScalarAsync();
            this.sqlConnection.Close();
            this.sqlCommand.Parameters.Clear();

            return result;
        }

        public async Task<DataSet> ExecuteDataSetAsync()
        {
            if (string.IsNullOrWhiteSpace(this.sqlCommand.CommandText))
            {
                throw new ArgumentException($"Please specify {nameof(this.sqlCommand.CommandText)} first");
            }

            DataSet dataSet = new DataSet();

            using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(this.sqlCommand))
            {
                this.sqlConnection.Open();
                await Task.Run(() => sqlDataAdapter.Fill(dataSet));
                this.sqlConnection.Close();
                this.sqlCommand.Parameters.Clear();
            }

            return dataSet;
        }

        private void SetCommandText(string sqlCommandText)
        {
            if (string.IsNullOrWhiteSpace(sqlCommandText))
            {
                throw new ArgumentException(nameof(sqlCommandText));
            }

            this.sqlCommand.CommandText = sqlCommandText;
        }
    }
}