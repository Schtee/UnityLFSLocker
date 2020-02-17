using NiceIO;
using System;

namespace GitLFSLocker
{
    [Serializable]
    struct User
    {
        public string name;
    }

    [Serializable]
    struct LockInfo
    {
        public string id;
        public NPath path;
        public User owner;
        public DateTime locked_at;
    }
}