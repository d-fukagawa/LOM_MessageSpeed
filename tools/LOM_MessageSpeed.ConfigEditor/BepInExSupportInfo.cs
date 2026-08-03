namespace LOM.MessageSpeed.ConfigEditor
{
    internal static class BepInExSupportInfo
    {
        internal const string VerifiedVersion = "6.0.0-be.692";
        internal const string VerifiedRuntime = "Unity Mono / Windows x64";
        internal const string VerifiedPackageFileName =
            "BepInEx-Unity.Mono-win-x64-6.0.0-be.692+851521c.zip";
        internal const string VerifiedPackageSha256 =
            "9A3472F5EEFB35A84AE8C6DEA16814B728AFF807C67C14FBFD448E20112951A6";

        internal const string OfficialGuideUrl =
            "https://docs.bepinex.dev/master/articles/user_guide/installation/unity_mono.html?tabs=tabid-win";

        internal const string OfficialBuildsUrl =
            "https://builds.bepinex.dev/projects/bepinex_be";

        internal const string ReinstallGuideUrl =
            "https://gist.github.com/d-fukagawa/7557dd9f2128d2ac59fec677a31541f1";

        internal const string RequiredPackage = VerifiedPackageFileName;

        internal const string WhyRequired =
            "LOM_MessageSpeedをゲーム内で動かすには、BepInExが必要です。BepInExは、ゲームを起動したときに" +
            "LOM_MessageSpeedを読み込む役割をします。ゲーム本体やセーブデータを置き換える必要はありません。";

        internal static string VerifiedDisplay =>
            "BepInEx " + VerifiedVersion + "\r\n" + VerifiedRuntime;
    }
}
