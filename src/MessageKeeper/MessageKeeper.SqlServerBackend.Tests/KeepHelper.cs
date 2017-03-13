using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageKeeper.SqlServerBackend.Tests
{
    public class KeepHelper
    {
        private const string ConnectionString = "Server=(local);Database=MessageKeep;Trusted_Connection=true;";

        public static void WipeKeep(string keepName)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                string query = string.Format(@"TRUNCATE TABLE {0}", keepName + "Keep");

                using (var command = new SqlCommand(query, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
