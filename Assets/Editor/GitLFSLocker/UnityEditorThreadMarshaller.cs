using System;

namespace GitLFSLocker
{
    class UnityEditorThreadMarshaller : IThreadMarshaller
    {
        public void Marshal(Action action)
        {
            UnityEditor.EditorApplication.delayCall += () => action();
        }
    }
}