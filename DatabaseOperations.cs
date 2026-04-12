using GrokOptions;
using System;
using System.Data;
using System.Data.SqlClient;

namespace DatabaseOperations
{
    public class DatabaseManager : IDisposable
    {
        private readonly string _connectionString;
        private SqlConnection _connection;

        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new SqlConnection(connectionString);
        }

        // Проверка существования таблицы
        public bool TableExists(string tableName)
        {
            string query = "SELECT COUNT(*) FROM sys.tables WHERE name = @tableName";
            try
            {
                OpenConnection();
                using (SqlCommand command = new SqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking table existence: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
        }

        // Открытие соединения
        private void OpenConnection()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        // Закрытие соединения
        private void CloseConnection()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        // Выполнение SELECT-запроса
        public DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            DataTable dataTable = new DataTable();

            try
            {
                OpenConnection();

                using (SqlCommand command = new SqlCommand(query, _connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing query: " + ex.Message);
            }
            finally
            {
                CloseConnection();
            }
        }

        // Выполнение INSERT, UPDATE, DELETE
        public int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            try
            {
                OpenConnection();

                using (SqlCommand command = new SqlCommand(query, _connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing non-query: " + ex.Message);
            }
            finally
            {
                CloseConnection();
            }
        }

        // Выполнение INSERT, UPDATE, DELETE
        public async void SaveToDB(DataTable table, string tableName, SqlParameter[] parameters = null)
        {
         //   TableExists(tableName);
            
            try
            {
                OpenConnection();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_connection))
                {
                    
                        
                    bulkCopy.DestinationTableName = tableName;
                    foreach (DataColumn col in table.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }
                    await bulkCopy.WriteToServerAsync(table);
                   
                   
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing non-query: " + ex.Message);
            }
            finally
            {
                CloseConnection();
            }
        }

        // Выполнение скалярного запроса
        public object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            try
            {
                OpenConnection();

                using (SqlCommand command = new SqlCommand(query, _connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing scalar: " + ex.Message);
            }
            finally
            {
                CloseConnection();
            }
        }

        // Реализация IDisposable
        public void Dispose()
        {
            if (_connection != null)
            {
                CloseConnection();
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}