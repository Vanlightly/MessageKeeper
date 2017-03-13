using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageKeeper
{
    public interface IMessageKeeper
    {
        void Keep<T>(string keepName, T message);
        Task KeepAsync<T>(string keepName, T message);
        void Rekeep<T>(string keepName, IStoredMessage<T> message);
        Task RekeepAsync<T>(string keepName, IStoredMessage<T> message);
        IStoredMessage<T> RetrieveMessage<T>(string keepName);
        Task<IStoredMessage<T>> RetrieveMessageAsync<T>(string keepName);
    }
}
