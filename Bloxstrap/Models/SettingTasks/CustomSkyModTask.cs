namespace Bloxstrap.Models.SettingTasks
{
    public class CustomSkyModTask : StringBaseTask
    {
        public CustomSkyModTask() : base("ModPreset", "CustomSky")
        {
            OriginalState = App.Settings.Prop.SelectedCustomSkyId ?? "";
        }

        public override void Execute()
        {
            if (!String.IsNullOrEmpty(NewState))
            {
                var pack = RobloxSkybox.ListInstalledPacks().FirstOrDefault(x => x.Id == NewState)
                    ?? throw new InvalidOperationException($"Custom sky pack '{NewState}' was not found");

                RobloxSkybox.ApplyPack(pack);
            }
            else
            {
                RobloxSkybox.RemoveApplied();
            }

            App.Settings.Prop.SelectedCustomSkyId = NewState;
            OriginalState = NewState;
        }
    }
}
