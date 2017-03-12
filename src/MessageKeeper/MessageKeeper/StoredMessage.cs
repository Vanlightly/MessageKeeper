using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageKeeper
{
    public class StoredMessage<T> : IStoredMessage<T>
    {
        public short StoreCount { get; set; }
        public DateTimeOffset OriginalStoreTime { get; set; }
        public DateTimeOffset LastStoreTime { get; set; }
        public T Payload { get; set; }
    }
}
