#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace GyroCue.Editor
{
    public static class LocalNetworkPermissionPostprocessor
    {
        [PostProcessBuild(10)]
        public static void AddLocalNetworkUsageDescription(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            var plistPath = Path.Combine(buildPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString(
                "NSLocalNetworkUsageDescription",
                "GyroCue receives cue motion from your second phone on the local Wi-Fi network.");
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
