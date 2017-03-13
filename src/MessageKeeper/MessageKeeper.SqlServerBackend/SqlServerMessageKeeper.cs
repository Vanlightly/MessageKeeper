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

        public void Keep<T>(string keepName, T message)
        {
            var payload = JsonConvert.SerializeObject(message);
            Insert(keepName, DateTimeOffset.Now, DateTimeOffset.Now, 1, payload);
        }

        public async Task KeepAsync<T>(string keepName, T message)
        {
            await Task.Run(() => Keep<T>(keepName, message));
        }

        public void Rekeep<T>(string keepName, IStoredMessage<T> message)
        {
            message.StoreCount++;
            message.LastStoreTime = DateTimeOffset.Now;
            var payload = JsonConvert.SerializeObject(message.Payload);
            Insert(keepName, message.OriginalStoreTime, message.LastStoreTime, message.StoreCount, payload);
        }

        public async Task RekeepAsync<T>(string keepName, IStoredMessage<T> message)
        {
            await Task.Run(() => Rekeep(keepName, message));
        }

        public IStoredMessage<T> RetrieveMessage<T>(string keepName)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = string.Format(@"WITH CTE AS (
    SELECT TOP(1) *
    FROM {0} with (ROWLOCK, READPAST, UPDLOCK)
    ORDER BY MessageId)
DELETE FROM CTE
OUTPUT DELETED.*;", keepName + "Keep");

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

        public async Task<IStoredMessage<T>> RetrieveMessageAsync<T>(string keepName)
        {
            return await Task.Run(() => RetrieveMessage<T>(keepName));
        }

        private void Insert(string keepName, DateTimeOffset originalStoreTime, DateTimeOffset lastStoreTime, short storeCount, string payload)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = string.Format(@"INSERT INTO [dbo].[{0}Keep]([OriginalStoreTime],[LastStoreTime],[StoreCount],[Payload])
VALUES(@OriginalStoreTime,@LastStoreTime,@StoreCount,@Payload)", keepName);

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
