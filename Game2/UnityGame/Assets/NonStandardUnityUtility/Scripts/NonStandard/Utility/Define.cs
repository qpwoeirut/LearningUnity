#define DEBUG
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace NonStandard.Utility {
#if UNITY_EDITOR
    [InitializeOnLoad] public static class Utility_Define { static Utility_Define() { Utility.Define.Add("NONSTANDARD_UTILITY"); } }
#endif
    
    [InitializeOnLoad]
    public static class ShowDefines {
        public static readonly string[] ForcedDefines = new string[] { };
        static ShowDefines() {
#if SHOW_DEFINES
            IList<string> defines =
#endif
            NonStandard.Utility.Define.Add(ForcedDefines);
#if SHOW_DEFINES
            UnityEngine.Debug.Log("#define:\n" + string.Join("\n", defines.OrderBy(x => x).ToList()));
#endif
        }
    }
    public static class Define {
        public static IList<string> Add(string token) => Add(new string[] { token });
        public static IList<string> Add(IList<string> tokens) {
            // [Edit] -> [Project Settings...] -> [Player] -> [Other Settings] -> Scripting Define Symbols
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> defines = currentDefines.Split(';').ToList();
            defines.AddRange(tokens.Except(defines));
            string newDefines = string.Join(";", defines.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefines);
            return defines;
        }
    }
}
#endif