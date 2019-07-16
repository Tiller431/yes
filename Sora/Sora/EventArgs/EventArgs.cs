using Sora.Interfaces;
using Sora.Objects;

namespace Sora.EventArgs
{
    public class EventArgs<T> : INeedPresence, IEventArgs
    {
        public T Data { get; set; }
        public Presence pr { get; set; }
    }
}
