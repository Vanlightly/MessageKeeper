using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageKeeper
{
    public interface IMessageKeeper
    {
        void Keep<T>(string label, T message);
        Task KeepAsync<T>(string label, T message);
        void Rekeep<T>(string label, IStoredMessage<T> message);
        Task RekeepAsync<T>(string label, IStoredMessage<T> message);
        IStoredMessage<T> RetrieveMessage<T>(string label);
        Task<IStoredMessage<T>> RetrieveMessageAsync<T>(string label);
    }
}
