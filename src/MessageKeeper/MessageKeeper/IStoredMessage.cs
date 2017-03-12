using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageKeeper
{
    public interface IStoredMessage<T>
    {
        short StoreCount { get; set; }
        DateTimeOffset OriginalStoreTime { get; set; }
        DateTimeOffset LastStoreTime { get; set; }
        T Payload { get; set; }
    }
}
