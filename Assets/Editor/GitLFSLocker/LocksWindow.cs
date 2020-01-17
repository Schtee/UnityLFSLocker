using UnityEngine;
using UnityEditor;

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
        private LocksTracker _locksTracker;
        private UnityEditorThreadMarshaller _threadMarshaller = new UnityEditorThreadMarshaller();

        private void OnGUI()
        {
            _dir = GUILayout.TextField(_dir);

            if (GUILayout.Button("Test"))
            {
                _locksTracker = new LocksTracker(_dir, _threadMarshaller);
                _locksTracker.Update();
            }

            if (_locksTracker == null)
            {
                GUILayout.Label("No locks tracker");
                return;
            }

            if (_locksTracker.IsBusy)
            {
                GUILayout.Label("Updating...");
                return;
            }

            foreach (var kvp in _locksTracker.Locks)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(kvp.Value.path);
                GUILayout.Label(kvp.Value.user);
                GUILayout.Label(kvp.Value.id);
                if (GUILayout.Button(EditorGUIUtility.IconContent("LockIcon")))
                {
                    _locksTracker.Unlock(kvp.Value.id);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
