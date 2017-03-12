using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageKeeper.SqlServerBackend
{
    public class MessageKeeperFactory
    {
        public static IMessageKeeper GetMessageKeeper(string connectionString)
        {
            return new SqlServerMessageKeeper(connectionString);
        }
    }
}
