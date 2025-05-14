using System.Collections.Generic;

namespace ProjectExtensions
{
    public class BoolRequestFlag
    {
        private readonly HashSet<object> _requesters = new();

        public bool IsActive => _requesters.Count > 0;

        public void Request(object requester)
        {
            _requesters.Add(requester);
        }

        public void Release(object requester)
        {
            _requesters.Remove(requester);
        }
    }
}
