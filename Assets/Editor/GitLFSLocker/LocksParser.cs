using NiceIO;
using System.Collections.Generic;
using UnityEngine;

namespace GitLFSLocker
{
    static class LocksParser
    {
        public static List<LockInfo> Parse(string output)
        {
            using (var reader = new System.IO.StringReader(output))
            {
                List<LockInfo> locks = new List<LockInfo>();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // path\tUser\tID:####
                    string[] components = line.Split('\t');
                    if (components.Length != 3)
                    {
                        Debug.LogWarning("Less than 3 elements in lock info: " + line);
                        continue;
                    }

                    // ID:####
                    string[] idComponents = components[2].Split(':');
                    if (idComponents.Length != 2)
                    {
                        Debug.LogWarning("Less than 2 elements in ID: " + components[2]);
                        continue;
                    }

                    locks.Add(new LockInfo { path = components[0].ToNPath(), user = components[1], id = idComponents[1] });
                }

                return locks;
            }
        }
    }
}