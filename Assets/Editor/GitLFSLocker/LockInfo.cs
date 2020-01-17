using NiceIO;

namespace GitLFSLocker
{
    struct LockInfo
    {
        public NPath path;
        public string user;
        public string id;
    }
}