using UnityEditor;
using NiceIO;
using UnityEngine;

namespace GitLFSLocker
{
    public class ProjectWindow
    {
        private const string _lockMenuItem = "Assets/LFS Lock";
        private const string _unlockMenuItem = "Assets/LFS Unlock";

        private static NPath GetFullAssetPath(Object asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }
            return assetPath.ToNPath().MakeAbsolute();
        }

        [MenuItem(_lockMenuItem, false, 999)]
        private static void Lock()
        {
            var selected = Selection.objects;
            foreach (var obj in selected)
            {
                Session.Instance.LocksTracker.Lock(GetFullAssetPath(obj));
            }
        }

        [MenuItem(_lockMenuItem, true)]
        private static bool LockValidate()
        {
            if (!Session.Instance.Ready)
            {
                return false;
            }

            var selected = Selection.objects;
            foreach (var obj in selected)
            {
                NPath fullPath = GetFullAssetPath(obj);
                if (string.IsNullOrEmpty(fullPath))
                {
                    return false;
                }

                if (Session.Instance.LocksTracker.IsLocked(fullPath))
                {
                    return false;
                }
            }

            return true;
        }

        [MenuItem(_unlockMenuItem, false, 999)]
        private static void Unlock()
        {
            var selected = Selection.objects;
            foreach (var obj in selected)
            {
                Session.Instance.LocksTracker.Unlock(GetFullAssetPath(obj));
            }
        }

        [MenuItem(_unlockMenuItem, true)]
        private static bool UnlockValidate()
        {
            if (!Session.Instance.Ready)
            {
                return false;
            }

            var selected = Selection.objects;
            foreach (var obj in selected)
            {
                NPath fullPath = GetFullAssetPath(obj);
                if (string.IsNullOrEmpty(fullPath))
                {
                    return false;
                }

                if (!Session.Instance.LocksTracker.IsLocked(fullPath))
                {
                    return false;
                }
            }

            return true;
        }
    }
}