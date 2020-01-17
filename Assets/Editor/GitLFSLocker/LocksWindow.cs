using UnityEngine;
using UnityEditor;
using NiceIO;

namespace GitLFSLocker
{
    public class LocksWindow : EditorWindow
    {
        [MenuItem("Git/Window")]
        private static void OpenWindow()
        {
            GetWindow<LocksWindow>().Show();
        }

        private string _dir = "";

        private void OnGUI()
        {
            _dir = GUILayout.TextField(_dir);

            if (GUILayout.Button("Test"))
            {
                Session.Instance.Start(_dir.ToNPath());
                Session.Instance.LocksTracker.Update();
            }

            if (!Session.Instance.Ready)
            {
                GUILayout.Label("Session not ready");
                return;
            }

            if (Session.Instance.LocksTracker.IsBusy)
            {
                GUILayout.Label("Updating...");
                return;
            }

            foreach (var kvp in Session.Instance.LocksTracker.Locks)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(kvp.Value.path);
                GUILayout.Label(kvp.Value.user);
                GUILayout.Label(kvp.Value.id);
                if (GUILayout.Button(EditorGUIUtility.IconContent("LockIcon")))
                {
                    Session.Instance.LocksTracker.Unlock(kvp.Value.path);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
