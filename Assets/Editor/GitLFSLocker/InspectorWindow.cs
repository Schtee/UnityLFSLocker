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

            string user;
            if (!Session.Instance.IsLockedBySomeoneElse(assetPath, out user))
            {
                message = null;
                return true;
            }

            message = "File is locked by " + user;
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

        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            string user;
            return Session.Instance.IsLockedBySomeoneElse(assetPath, out user) ? AssetDeleteResult.FailedDelete : AssetDeleteResult.DidNotDelete;
        }

        private static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        {
            string user;
            return Session.Instance.IsLockedBySomeoneElse(oldPath, out user) || Session.Instance.IsLockedBySomeoneElse(newPath, out user) ?
                AssetMoveResult.FailedMove : AssetMoveResult.DidNotMove;
        }
    }
}