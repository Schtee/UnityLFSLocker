using NiceIO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GitLFSLocker
{
    static class LocksParser
    {
        [Serializable]
        struct LockInfoIntermediate
        {
            public string id;
            public string path;
            public User owner;
            public string locked_at;
        }

        [Serializable]
        private struct WrappedLockInfo
        {
            public List<LockInfoIntermediate> locks;
        }

        public static List<LockInfo> Parse(string output)
        {
            string wrappedString = "{\"locks\":" + output + "}";
            WrappedLockInfo wrappedLockInfo = JsonUtility.FromJson<WrappedLockInfo>(wrappedString);
            List<LockInfo> locks = new List<LockInfo>(wrappedLockInfo.locks.Count);
            foreach (var l in wrappedLockInfo.locks)
            {
                locks.Add(new LockInfo { id = l.id, locked_at = DateTime.Parse(l.locked_at), owner = l.owner, path = l.path.ToNPath() });
            }
            return locks;
        }
    }
}