using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageKeeper.SqlServerBackend
{
    public class SqlServerMessageKeeper : IMessageKeeper
    {
        private readonly string _connectionString;

        public SqlServerMessageKeeper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Keep<T>(string label, T message)
        {
            var payload = JsonConvert.SerializeObject(message);
            Insert(label, DateTimeOffset.Now, DateTimeOffset.Now, 1, payload);
        }

        public async Task KeepAsync<T>(string label, T message)
        {
            await Task.Run(() => Keep<T>(label, message));
        }

        public void Rekeep<T>(string label, IStoredMessage<T> message)
        {
            message.StoreCount++;
            var payload = JsonConvert.SerializeObject(message.Payload);
            Insert(label, message.OriginalStoreTime, message.LastStoreTime, message.StoreCount, payload);
        }

        public async Task RekeepAsync<T>(string label, IStoredMessage<T> message)
        {
            await Task.Run(() => Rekeep(label, message));
        }

        public IStoredMessage<T> RetrieveMessage<T>(string label)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = string.Format(@"WITH CTE AS (
    SELECT TOP(1) *
    FROM {0} with (ROWLOCK, READPAST, UPDLOCK)
    ORDER BY MessageId)
DELETE FROM CTE
OUTPUT DELETED.*;", "Kept" + label + "s");

                using (var command = new SqlCommand(query, conn))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var message = new StoredMessage<T>();
                            message.LastStoreTime = (DateTimeOffset)reader["LastStoreTime"];
                            message.OriginalStoreTime = (DateTimeOffset)reader["OriginalStoreTime"];
                            message.StoreCount = (short)reader["StoreCount"];
                            message.Payload = JsonConvert.DeserializeObject<T>(reader["Payload"].ToString());

                            return message;
                        }

                        return null;
                    }
                }
            }
        }

        public async Task<IStoredMessage<T>> RetrieveMessageAsync<T>(string label)
        {
            return await Task.Run(() => RetrieveMessage<T>(label));
        }

        private void Insert(string label, DateTimeOffset originalStoreTime, DateTimeOffset lastStoreTime, short storeCount, string payload)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = string.Format(@"INSERT INTO [dbo].[Kept{0}s]([OriginalStoreTime],[LastStoreTime],[StoreCount],[Payload])
VALUES(@OriginalStoreTime,@LastStoreTime,@StoreCount,@Payload)", label);

                using (var command = new SqlCommand(query, conn))
                {
                    command.Parameters.Add("OriginalStoreTime", SqlDbType.DateTimeOffset).Value = originalStoreTime;
                    command.Parameters.Add("LastStoreTime", SqlDbType.DateTimeOffset).Value = lastStoreTime;
                    command.Parameters.Add("StoreCount", SqlDbType.SmallInt).Value = storeCount;
                    command.Parameters.Add("Payload", SqlDbType.NVarChar, -1).Value = payload;

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
