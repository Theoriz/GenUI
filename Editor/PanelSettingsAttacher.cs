using UnityEditor;

namespace Theoriz.GenUI.Editor
{
    // Puts GenUI's panel settings one click from the Controllable they describe. Nothing adds the
    // component for you: a controllable without one already draws with the defaults, so the entry is
    // for the user who wants to change one of them.
    public static class PanelSettingsAttacher
    {
        [MenuItem("CONTEXT/Component/Add GenUI Panel Settings", true, 10001)]
        private static bool ValidateMenu(MenuCommand command)
        {
            var controllable = command.context as Controllable;
            return controllable != null && controllable.GetComponent<GenUIPanelSettings>() == null;
        }

        //Sits in the Controllable's three-dots menu, where the fields it replaces used to be. Under
        //CONTEXT/Component rather than CONTEXT/Controllable, and one priority step past OCF's two
        //entries: a menu path of its own would be grouped on its own, at the bottom of the menu.
        [MenuItem("CONTEXT/Component/Add GenUI Panel Settings", false, 10001)]
        private static void AddPanelSettings(MenuCommand command)
        {
            var controllable = command.context as Controllable;
            if (controllable != null)
                Attach(controllable);
        }

        /// <summary>
        /// Gives <paramref name="controllable"/> a <see cref="GenUIPanelSettings"/> unless it has
        /// one already, seeded with the colour its panel would have had, and returns it.
        /// </summary>
        public static GenUIPanelSettings Attach(Controllable controllable)
        {
            var existing = controllable.GetComponent<GenUIPanelSettings>();
            if (existing != null)
                return existing;

            //ObjectFactory rather than Undo.AddComponent: it registers the undo entry too, and it
            //runs Reset, so the component is initialized the way the Inspector would.
            var settings = ObjectFactory.AddComponent<GenUIPanelSettings>(controllable.gameObject);

            //Seeded here rather than left to Reset, so the colour the panel keeps does not depend on
            //Reset having run. Adding the component must not change how the panel looks.
            settings.barColor = GenUIPanelSettings.DefaultBarColor(controllable.controllableId);
            EditorUtility.SetDirty(settings);

            return settings;
        }
    }
}
