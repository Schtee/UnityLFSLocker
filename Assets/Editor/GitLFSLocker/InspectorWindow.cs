using NiceIO;
using UnityEditor;
using UnityEngine;

namespace GitLFSLocker
{
    [InitializeOnLoad]
    public class InspectorWindow : UnityEditor.AssetModificationProcessor
    {
        static InspectorWindow()
        {
            Editor.finishedDefaultHeaderGUI += editor => DrawLockUI(editor);
        }

        public static bool IsOpenForEdit(string assetPath, out string message)
        {
            const string metaExtension = ".meta";
            // if meta file, also check main asset
            if (assetPath.EndsWith(metaExtension))
            {
                string mainAssetPath = assetPath.Substring(0, assetPath.Length - metaExtension.Length);
                if (!IsOpenForEdit(mainAssetPath, out message))
                {
                    return false;
                }
            }

            if (!Session.Instance.Ready)
            {
                message = null;
                return true;
            }

            LockInfo lockInfo;
            if (!Session.Instance.LocksTracker.TryGetLockInfo(assetPath.ToNPath().MakeAbsolute(), out lockInfo))
            {
                message = null;
                return true;
            }

            if (lockInfo.owner.name == Session.Instance.User)
            {
                message = null;
                return true;
            }

            message = "File is locked by " + lockInfo.owner.name;
            return false;
        }

        private static void DrawLockUI(Editor editor)
        {
            string msg;
            if (IsOpenForEdit(AssetDatabase.GetAssetPath(editor.target), out msg))
            {
                return;
            }

            GUILayout.Label(msg);
            GUI.enabled = false;
        }
    }
}